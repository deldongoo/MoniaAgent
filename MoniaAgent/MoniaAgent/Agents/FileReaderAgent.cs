using MoniaAgent.Configuration;
using MoniaAgent.Core;
using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Outputs;
using System.ComponentModel;

namespace MoniaAgent.Agents
{
    public class FileReaderAgent : TypedAgent<FileInput, FileOutput>
    {
        public FileReaderAgent(LLM llm) : base(llm)
        {
        }

        protected override AgentConfig Configure() => new()
        {
            Name = "FileReaderAgent",
            Specialty = "Reading files and extracting content",
            Keywords = new[] { "read", "file", "content", "open", "show", "display" },
            ToolMethods = new Delegate[] { ReadFileContent },
            Goal = @"You are a file reading specialist. You can read files and extract their content.
                    Use the read_file_content tool to read files when requested.
                    Always call task_complete when finished reading the file."
        };

        [Description("Reads the content of a file. Parameters: filePath (string: path to the file to read)")]
        private static string ReadFileContent(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return $"Error: File not found at path: {filePath}";
                }

                var content = File.ReadAllText(filePath);
                var fileInfo = new FileInfo(filePath);
                
                return $"File: {filePath}\nSize: {fileInfo.Length} bytes\nLast Modified: {fileInfo.LastWriteTime}\n\nContent:\n{content}";
            }
            catch (Exception ex)
            {
                return $"Error reading file: {ex.Message}";
            }
        }

        // Override input conversion to handle FileInput
        protected override string ConvertInputToPrompt(AgentInput input)
        {
            if (input is FileInput fileInput)
            {
                return $"Read the file at {fileInput.FilePath} and return its contents. Use the read_file_content tool with the file path.";
            }
            return base.ConvertInputToPrompt(input);
        }

        // Implement typed conversion
        protected override FileOutput ConvertStringToOutput(string textResult, ExecutionMetadata metadata)
        {
            var result = new FileOutput
            {
                Operation = FileOperation.Read,
                Metadata = metadata
            };

            // Parse the action summary format
            if (textResult.StartsWith("Actions performed:"))
            {
                // Extract ReadFileContent action result
                var readFileContentMatch = System.Text.RegularExpressions.Regex.Match(
                    textResult, 
                    @"ReadFileContent\([^)]+\) --> (.+?)(?=- TaskComplete|\z)", 
                    System.Text.RegularExpressions.RegexOptions.Singleline);

                if (readFileContentMatch.Success)
                {
                    var fileContentResult = readFileContentMatch.Groups[1].Value.Trim();
                    
                    // Parse file metadata
                    var lines = fileContentResult.Split('\n');
                    
                    // Extract file path
                    var filePathLine = lines.FirstOrDefault(l => l.StartsWith("File: "));
                    if (filePathLine != null)
                    {
                        result.FilePath = filePathLine.Substring(6).Trim();
                    }

                    // Extract file size
                    var sizeLine = lines.FirstOrDefault(l => l.StartsWith("Size: "));
                    if (sizeLine != null)
                    {
                        var sizeMatch = System.Text.RegularExpressions.Regex.Match(sizeLine, @"Size: (\d+)");
                        if (sizeMatch.Success && long.TryParse(sizeMatch.Groups[1].Value, out var size))
                        {
                            result.FileSize = size;
                        }
                    }

                    // Extract last modified date
                    var modifiedLine = lines.FirstOrDefault(l => l.StartsWith("Last Modified: "));
                    if (modifiedLine != null && DateTime.TryParse(modifiedLine.Substring(15).Trim(), out var lastModified))
                    {
                        result.LastModified = lastModified;
                    }

                    // Extract actual file content
                    var contentIndex = Array.FindIndex(lines, l => l.StartsWith("Content:"));
                    if (contentIndex >= 0 && contentIndex < lines.Length - 1)
                    {
                        var contentLines = lines.Skip(contentIndex + 1).ToArray();
                        result.Content = string.Join("\n", contentLines).Trim();
                    }

                    // Success if we have valid file data
                    result.Success = !string.IsNullOrEmpty(result.FilePath) && !string.IsNullOrEmpty(result.Content);
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = "Could not parse file reading result from action summary";
                    result.Content = textResult;
                }
            }
            else
            {
                // Fallback to original logic for backward compatibility
                result.Success = !textResult.Contains("Error");
                result.Content = textResult;
                
                if (!result.Success)
                {
                    result.ErrorMessage = textResult.Contains("Error") ? textResult : "Unknown error occurred";
                }
            }

            return result;
        }
    }
}