using MoniaAgent.Configuration;
using MoniaAgent.Core;
using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Outputs;
using MoniaAgent.Tools;
using System.ComponentModel;
using System.Text.Json;

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
            Goal = $@"You are a file reading specialist. You can read files and extract their content.
                    Use the read_file_content tool to read files when requested."
        };

        [Description("Reads the content of a file. Parameters: filePath (string: path to the file to read)")]
        private static string ReadFileContent(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return "ERROR: File not found";
                }

                var content = File.ReadAllText(filePath);
                var fileInfo = new FileInfo(filePath);
                
                return JsonSerializer.Serialize(new {
                    filePath,
                    content,
                    size = fileInfo.Length,
                    lastModified = fileInfo.LastWriteTime
                });
            }
            catch (Exception ex)
            {
                return $"ERROR: {ex.Message}";
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

            // Handle direct error responses
            if (textResult.StartsWith("ERROR:"))
            {
                result.Success = false;
                result.ErrorMessage = textResult;
                return result;
            }

            // Look for JSON in action summary format
            if (textResult.Contains("ReadFileContent"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    textResult, 
                    @"ReadFileContent\([^)]+\) --> ({.*?})(?=\s*-|\z)", 
                    System.Text.RegularExpressions.RegexOptions.Singleline);
                
                if (match.Success)
                {
                    var jsonResult = match.Groups[1].Value.Trim();
                    try
                    {
                        var data = JsonSerializer.Deserialize<JsonElement>(jsonResult);
                        result.FilePath = data.GetProperty("filePath").GetString();
                        result.Content = data.GetProperty("content").GetString();
                        result.FileSize = data.GetProperty("size").GetInt64();
                        result.LastModified = data.GetProperty("lastModified").GetDateTime();
                        result.Success = true;
                        return result;
                    }
                    catch
                    {
                        // Fall through to error handling
                    }
                }
            }

            // Fallback: couldn't parse
            result.Success = false;
            result.ErrorMessage = "Could not parse tool result";
            result.Content = textResult;
            return result;
        }
    }
}