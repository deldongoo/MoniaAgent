using MoniaAgent.Configuration;
using MoniaAgent.Agent;
using MoniaAgent.Agent.Inputs;
using MoniaAgent.Agent.Outputs;
using MoniaAgent.Tools;
using MoniaAgentTest.Inputs;
using MoniaAgentTest.Outputs;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MoniaAgentTest.Agents
{
    [AgentMetadata(
        Name = "FileReaderAgent",
        Specialty = "Reading files and extracting content",
        Keywords = new[] { "read", "file", "content", "open", "show", "display", "load", "view", "text", "document" },
        Goal = "You are a file reading specialist. You can read files and extract their content. Use the read_file_content tool to read files when requested."
    )]
    public class FileReaderAgent : TypedAgent<FileInput, FileOutput>
    {
        public FileReaderAgent(LLM llm) : base(llm)
        {
        }

        protected override AgentConfig ConfigureTools() => new()
        {
            ToolMethods = new Delegate[] { ReadFileContent }
        };

        [Description("Reads the content of a file. Parameters: filePath (string: path to the file to read)")]
        private static FileOutput ReadFileContent(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return new FileOutput
                    {
                        FilePath = filePath,
                        Content = $"File not found: {filePath}",
                        Success = false,
                        ErrorMessage = "File not found",
                        Operation = FileOperation.Read
                    };
                }

                var content = File.ReadAllText(filePath);
                var fileInfo = new FileInfo(filePath);
                
                return new FileOutput
                {
                    FilePath = filePath,
                    Content = content,
                    FileSize = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime,
                    Operation = FileOperation.Read,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new FileOutput
                {
                    FilePath = filePath,
                    Content = $"Error reading file {filePath}: {ex.Message}",
                    Success = false,
                    ErrorMessage = ex.Message,
                    Operation = FileOperation.Read
                };
            }
        }

        // Override input conversion to handle both FileInput and TextInput
        protected override string ConvertInputToPrompt(AgentInput input)
        {
            if (input is FileInput fileInput)
            {
                return $"Read the file: {fileInput.FilePath}";
            }
           
            return base.ConvertInputToPrompt(input);
        }
    

        // Implement typed conversion
        protected override FileOutput ConvertResultToOutput(string finalLLMAnswer, ExecutionMetadata metadata)
        {
            var result = new FileOutput
            {
                Operation = FileOperation.Read,
                Metadata = metadata
            };

            // Use the new helper method to find ReadFileContent tool result
            var toolResult = metadata.FindToolResult("ReadFileContent");
            if (!string.IsNullOrEmpty(toolResult))
            {
                try
                {
                    // Deserialize directly to FileOutput with case-insensitive options and enum converter
                    var options = new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true,
                        Converters = { new JsonStringEnumConverter() }
                    };
                    var data = JsonSerializer.Deserialize<FileOutput>(toolResult, options);
                    if (data != null)
                    {
                        // Framework handles metadata assignment
                        // Preserve Content from tool - don't overwrite with LLM response
                        return data;
                    }
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Could not parse tool result: {ex.Message}";
                    result.Content = toolResult;
                    return result;
                }
            }

            // Fallback: no tool result found
            result.Success = false;
            result.ErrorMessage = "No ReadFileContent tool result found";
            result.Content = finalLLMAnswer;
            return result;
        }
    }
}