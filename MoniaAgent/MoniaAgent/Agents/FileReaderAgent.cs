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
                Success = !textResult.Contains("Error"),
                Content = textResult,
                Operation = FileOperation.Read,
                Metadata = metadata
            };

            // Try to extract file path from the result or metadata
            if (textResult.StartsWith("File: "))
            {
                var lines = textResult.Split('\n');
                if (lines.Length > 0)
                {
                    result.FilePath = lines[0].Substring(6); // Remove "File: " prefix
                }

                // Try to extract file size
                var sizeLine = lines.FirstOrDefault(l => l.StartsWith("Size: "));
                if (sizeLine != null && long.TryParse(sizeLine.Split(' ')[1], out var size))
                {
                    result.FileSize = size;
                }

                // Try to extract last modified date
                var modifiedLine = lines.FirstOrDefault(l => l.StartsWith("Last Modified: "));
                if (modifiedLine != null && DateTime.TryParse(modifiedLine.Substring(15), out var lastModified))
                {
                    result.LastModified = lastModified;
                }
            }

            if (!result.Success)
            {
                result.ErrorMessage = textResult.Contains("Error") ? textResult : "Unknown error occurred";
            }

            return result;
        }
    }
}