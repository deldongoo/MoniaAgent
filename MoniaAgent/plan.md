# MoniaAgent I/O and Workflow Implementation Guide

## Overview

This document provides a comprehensive plan for implementing typed inputs/outputs and workflow capabilities in the MoniaAgent framework. The goal is to enable agents to work in complex workflows with type-safe data exchange.

## Current State Analysis

The current implementation has agents returning simple `string` values, which is limiting for complex workflows. The `TaskCompleteTool` exists but is insufficient for sophisticated data pipelines.

## Implementation Plan

### 1. Core Result Types System

Create a hierarchy of result types that agents can return:

#### Base Result Type (Core/Results/AgentResult.cs)

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MoniaAgent.Core.Results
{
    /// <summary>
    /// Base class for all agent execution results
    /// </summary>
    public abstract class AgentResult
    {
        /// <summary>
        /// Indicates if the agent completed successfully
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Error message if Success is false
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Metadata about the execution (duration, tokens used, etc.)
        /// </summary>
        public ExecutionMetadata Metadata { get; set; } = new();
        
        /// <summary>
        /// Actions performed during execution
        /// </summary>
        public List<PerformedAction> Actions { get; set; } = new();
        
        /// <summary>
        /// The type of result for deserialization
        /// </summary>
        [JsonPropertyName("$type")]
        public abstract string ResultType { get; }
    }

    /// <summary>
    /// Simple text result
    /// </summary>
    public class TextResult : AgentResult
    {
        public string Content { get; set; } = string.Empty;
        public override string ResultType => "text";
    }

    /// <summary>
    /// Boolean decision result
    /// </summary>
    public class BooleanResult : AgentResult
    {
        public bool Value { get; set; }
        public string? Reasoning { get; set; }
        public override string ResultType => "boolean";
    }

    /// <summary>
    /// Structured data result
    /// </summary>
    public class StructuredResult<T> : AgentResult where T : class
    {
        public T? Data { get; set; }
        public override string ResultType => $"structured:{typeof(T).Name}";
    }

    /// <summary>
    /// Collection result
    /// </summary>
    public class CollectionResult<T> : AgentResult
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public override string ResultType => $"collection:{typeof(T).Name}";
    }

    /// <summary>
    /// File operation result
    /// </summary>
    public class FileResult : AgentResult
    {
        public string FilePath { get; set; } = string.Empty;
        public string? Content { get; set; }
        public FileOperation Operation { get; set; }
        public override string ResultType => "file";
    }

    /// <summary>
    /// Validation result with details
    /// </summary>
    public class ValidationResult : AgentResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new();
        public Dictionary<string, object> ValidatedData { get; set; } = new();
        public override string ResultType => "validation";
    }

    /// <summary>
    /// Workflow continuation result
    /// </summary>
    public class WorkflowResult : AgentResult
    {
        public string NextAgentName { get; set; } = string.Empty;
        public Dictionary<string, object> OutputData { get; set; } = new();
        public WorkflowDecision Decision { get; set; }
        public override string ResultType => "workflow";
    }

    // Supporting types
    public class ExecutionMetadata
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public int TokensUsed { get; set; }
        public int ToolCallsCount { get; set; }
        public string AgentName { get; set; } = string.Empty;
    }

    public class PerformedAction
    {
        public string ToolName { get; set; } = string.Empty;
        public Dictionary<string, object?> Arguments { get; set; } = new();
        public string Result { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public enum FileOperation
    {
        Created,
        Modified,
        Deleted,
        Read,
        Moved
    }

    public class ValidationError
    {
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Code { get; set; }
    }

    public enum WorkflowDecision
    {
        Continue,
        Branch,
        Loop,
        Complete,
        Error
    }
}
```

### 2. Input Types System

Create typed inputs that agents can accept:

#### Base Input Types (Core/Inputs/AgentInput.cs)

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MoniaAgent.Core.Inputs
{
    /// <summary>
    /// Base class for all agent inputs
    /// </summary>
    public abstract class AgentInput
    {
        /// <summary>
        /// Context from previous agent executions in the workflow
        /// </summary>
        public WorkflowContext? Context { get; set; }
        
        /// <summary>
        /// The type of input for deserialization
        /// </summary>
        [JsonPropertyName("$type")]
        public abstract string InputType { get; }
    }

    /// <summary>
    /// Simple text prompt input
    /// </summary>
    public class TextInput : AgentInput
    {
        public string Prompt { get; set; } = string.Empty;
        public override string InputType => "text";
        
        public TextInput() { }
        public TextInput(string prompt) => Prompt = prompt;
        
        // Implicit conversion from string for backward compatibility
        public static implicit operator TextInput(string prompt) => new(prompt);
    }

    /// <summary>
    /// Structured query input
    /// </summary>
    public class QueryInput : AgentInput
    {
        public string Query { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public List<string> RequiredFields { get; set; } = new();
        public override string InputType => "query";
    }

    /// <summary>
    /// File processing input
    /// </summary>
    public class FileInput : AgentInput
    {
        public string FilePath { get; set; } = string.Empty;
        public string? Content { get; set; }
        public FileProcessingOptions Options { get; set; } = new();
        public override string InputType => "file";
    }

    /// <summary>
    /// Batch processing input
    /// </summary>
    public class BatchInput<T> : AgentInput where T : class
    {
        public List<T> Items { get; set; } = new();
        public ProcessingMode Mode { get; set; } = ProcessingMode.Sequential;
        public int MaxConcurrency { get; set; } = 1;
        public override string InputType => $"batch:{typeof(T).Name}";
    }

    /// <summary>
    /// Validation input
    /// </summary>
    public class ValidationInput : AgentInput
    {
        public Dictionary<string, object> Data { get; set; } = new();
        public List<ValidationRule> Rules { get; set; } = new();
        public override string InputType => "validation";
    }

    /// <summary>
    /// Pipeline input from previous agent
    /// </summary>
    public class PipelineInput : AgentInput
    {
        public AgentResult? PreviousResult { get; set; }
        public string AdditionalPrompt { get; set; } = string.Empty;
        public Dictionary<string, object> Transformations { get; set; } = new();
        public override string InputType => "pipeline";
    }

    // Supporting types
    public class WorkflowContext
    {
        public string WorkflowId { get; set; } = Guid.NewGuid().ToString();
        public Dictionary<string, object> SharedData { get; set; } = new();
        public List<string> ExecutionPath { get; set; } = new();
        public Dictionary<string, AgentResult> PreviousResults { get; set; } = new();
    }

    public class FileProcessingOptions
    {
        public bool PreserveOriginal { get; set; } = true;
        public string? OutputDirectory { get; set; }
        public FileFormat? TargetFormat { get; set; }
    }

    public enum ProcessingMode
    {
        Sequential,
        Parallel,
        Adaptive
    }

    public class ValidationRule
    {
        public string Field { get; set; } = string.Empty;
        public string RuleType { get; set; } = string.Empty;
        public object? Value { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public enum FileFormat
    {
        Text,
        Json,
        Xml,
        Csv,
        Binary
    }
}
```

### 3. Enhanced Agent Interface

Update the agent interface to support typed I/O:

#### Updated IAgent Interface (Core/IAgent.cs)

```csharp
using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Results;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MoniaAgent.Core
{
    /// <summary>
    /// Enhanced agent interface supporting typed inputs and outputs
    /// </summary>
    public interface IAgent
    {
        string Name { get; }
        string Specialty { get; }
        
        /// <summary>
        /// Indicates supported input types
        /// </summary>
        Type[] SupportedInputTypes { get; }
        
        /// <summary>
        /// Indicates expected output type
        /// </summary>
        Type ExpectedOutputType { get; }
        
        /// <summary>
        /// Legacy execute method for backward compatibility
        /// </summary>
        Task<string> Execute(string prompt);
        
        /// <summary>
        /// New typed execute method
        /// </summary>
        Task<AgentResult> ExecuteAsync(AgentInput input, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Checks if agent can handle the input
        /// </summary>
        bool CanHandle(AgentInput input);
        
        /// <summary>
        /// Legacy method for backward compatibility
        /// </summary>
        bool CanHandle(string task);
        
        /// <summary>
        /// Validates input before execution
        /// </summary>
        ValidationResult ValidateInput(AgentInput input);
    }

    /// <summary>
    /// Generic agent interface for strongly typed agents
    /// </summary>
    public interface IAgent<TInput, TOutput> : IAgent
        where TInput : AgentInput
        where TOutput : AgentResult
    {
        Task<TOutput> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);
    }
}
```

### 4. Base Typed Agent Implementation

Create a base class for typed agents:

#### TypedAgent Base Class

```csharp
public abstract class TypedAgent<TInput, TOutput> : Agent, IAgent<TInput, TOutput>
    where TInput : AgentInput
    where TOutput : AgentResult, new()
{
    protected TypedAgent(LLM llm) : base(llm) { }
    
    public override Type[] SupportedInputTypes => new[] { typeof(TInput) };
    public override Type ExpectedOutputType => typeof(TOutput);
    
    public override async Task<AgentResult> ExecuteAsync(AgentInput input, CancellationToken cancellationToken = default)
    {
        if (input is TInput typedInput)
        {
            return await ExecuteAsync(typedInput, cancellationToken);
        }
        
        return new TOutput
        {
            Success = false,
            ErrorMessage = $"Invalid input type. Expected {typeof(TInput).Name}, got {input.GetType().Name}"
        };
    }
    
    public abstract Task<TOutput> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);
    
    public override bool CanHandle(AgentInput input) => input is TInput;
    
    public override ValidationResult ValidateInput(AgentInput input)
    {
        var result = new ValidationResult { Success = true, IsValid = true };
        
        if (!(input is TInput))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError
            {
                Field = "InputType",
                Message = $"Expected {typeof(TInput).Name}, got {input.GetType().Name}"
            });
        }
        
        return result;
    }
}
```

### 5. Refactored SpecializedAgent

As discussed, `SpecializedAgent` should inherit from `TypedAgent<TextInput, TextResult>`:

```csharp
public abstract class SpecializedAgent : TypedAgent<TextInput, TextResult>
{
    private readonly AgentConfig config;

    protected SpecializedAgent(LLM llm) : base(llm)
    {
        config = Configure();
        var tools = BuildTools(config);
        Initialize(tools, config.Goal);
    }

    protected abstract AgentConfig Configure();

    public override string Name => config.Name;
    public override string Specialty => config.Specialty;

    public override bool CanHandle(string task)
    {
        return config.Keywords.Any(k => task.ToLower().Contains(k.ToLower()));
    }
    
    public override async Task<TextResult> ExecuteAsync(TextInput input, CancellationToken cancellationToken = default)
    {
        // Use existing Execute logic but return TextResult
        var result = await Execute(input.Prompt);
        
        return new TextResult
        {
            Success = !result.Contains("Error") && !result.Contains("Failed"),
            Content = result,
            Metadata = new ExecutionMetadata
            {
                AgentName = Name,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow
            }
        };
    }

    // Rest of implementation...
}
```

### 6. Workflow System

#### Workflow Builder (Workflows/WorkflowBuilder.cs)

```csharp
using MoniaAgent.Core;
using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MoniaAgent.Workflows
{
    /// <summary>
    /// Fluent builder for creating agent workflows
    /// </summary>
    public class WorkflowBuilder
    {
        private readonly List<WorkflowStepBase> steps = new();
        private readonly Dictionary<string, IAgent> agents = new();
        private string? name;
        private WorkflowErrorHandler? errorHandler;
        
        public WorkflowBuilder WithName(string workflowName)
        {
            name = workflowName;
            return this;
        }
        
        public WorkflowBuilder RegisterAgent(IAgent agent)
        {
            agents[agent.Name] = agent;
            return this;
        }
        
        public WorkflowBuilder AddStep(string agentName, Action<StepConfiguration>? configure = null)
        {
            var config = new StepConfiguration();
            configure?.Invoke(config);
            
            steps.Add(new WorkflowStep
            {
                AgentName = agentName,
                Configuration = config
            });
            
            return this;
        }
        
        public WorkflowBuilder AddConditionalStep(
            string agentName, 
            Func<AgentResult, bool> condition,
            Action<StepConfiguration>? configure = null)
        {
            var config = new StepConfiguration();
            configure?.Invoke(config);
            
            steps.Add(new ConditionalStep
            {
                AgentName = agentName,
                Condition = condition,
                Configuration = config
            });
            
            return this;
        }
        
        public WorkflowBuilder AddParallelSteps(params (string agentName, Action<StepConfiguration>? configure)[] parallelSteps)
        {
            var parallelGroup = new ParallelStepGroup();
            
            foreach (var (agentName, configure) in parallelSteps)
            {
                var config = new StepConfiguration();
                configure?.Invoke(config);
                
                parallelGroup.Steps.Add(new WorkflowStep
                {
                    AgentName = agentName,
                    Configuration = config
                });
            }
            
            steps.Add(parallelGroup);
            return this;
        }
        
        public WorkflowBuilder WithErrorHandler(WorkflowErrorHandler handler)
        {
            errorHandler = handler;
            return this;
        }
        
        public Workflow Build()
        {
            if (string.IsNullOrEmpty(name))
                throw new InvalidOperationException("Workflow name is required");
                
            return new Workflow(name, steps, agents, errorHandler);
        }
    }
    
    /// <summary>
    /// Configuration for a workflow step
    /// </summary>
    public class StepConfiguration
    {
        public Func<AgentResult?, AgentInput>? InputTransformer { get; set; }
        public Func<AgentResult, bool>? RetryCondition { get; set; }
        public int MaxRetries { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
        public bool ContinueOnError { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
    
    // Additional workflow classes...
}
```

#### Main Workflow Class (Workflows/Workflow.cs)

```csharp
using MoniaAgent.Core;
using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Results;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MoniaAgent.Workflows
{
    /// <summary>
    /// Represents an executable workflow of agents
    /// </summary>
    public class Workflow
    {
        private static readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        private static readonly ILogger logger = loggerFactory.CreateLogger<Workflow>();
        
        private readonly string name;
        private readonly List<WorkflowStepBase> steps;
        private readonly Dictionary<string, IAgent> agents;
        private readonly WorkflowErrorHandler? errorHandler;
        
        internal Workflow(
            string name,
            List<WorkflowStepBase> steps,
            Dictionary<string, IAgent> agents,
            WorkflowErrorHandler? errorHandler)
        {
            this.name = name;
            this.steps = steps;
            this.agents = agents;
            this.errorHandler = errorHandler;
        }
        
        /// <summary>
        /// Execute the workflow with text input
        /// </summary>
        public Task<WorkflowExecutionResult> ExecuteAsync(string initialPrompt, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(new TextInput(initialPrompt), cancellationToken);
        }
        
        /// <summary>
        /// Execute the workflow with typed input
        /// </summary>
        public async Task<WorkflowExecutionResult> ExecuteAsync(AgentInput initialInput, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var context = new WorkflowContext();
            var executionResult = new WorkflowExecutionResult
            {
                WorkflowName = name,
                StartTime = DateTime.UtcNow
            };
            
            logger.LogInformation("Starting workflow '{WorkflowName}'", name);
            
            try
            {
                // Execute workflow steps...
                // Implementation details as shown in the artifacts
                
                return executionResult;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Workflow '{WorkflowName}' failed", name);
                
                executionResult.Success = false;
                executionResult.ErrorMessage = ex.Message;
                executionResult.EndTime = DateTime.UtcNow;
                executionResult.TotalDuration = stopwatch.Elapsed;
                
                return executionResult;
            }
        }
    }
    
    /// <summary>
    /// Result of workflow execution
    /// </summary>
    public class WorkflowExecutionResult
    {
        public string WorkflowName { get; set; } = string.Empty;
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public List<StepExecutionResult> StepResults { get; set; } = new();
        public WorkflowContext? Context { get; set; }
        
        /// <summary>
        /// Get the final result from the last step
        /// </summary>
        public AgentResult? FinalResult => StepResults.LastOrDefault()?.Result;
        
        /// <summary>
        /// Get typed final result
        /// </summary>
        public T? GetFinalResult<T>() where T : AgentResult
        {
            return FinalResult as T;
        }
    }
}
```

### 7. Usage Examples

#### Example 1: File Processing Workflow

```csharp
// Create specialized agents
var fileReader = new FileReaderAgent(llm);
var validator = new DataValidatorAgent(llm);
var transformer = new DataTransformerAgent(llm);
var fileWriter = new FileWriterAgent(llm);

// Build workflow
var workflow = new WorkflowBuilder()
    .WithName("FileProcessingWorkflow")
    .RegisterAgent(fileReader)
    .RegisterAgent(validator)
    .RegisterAgent(transformer)
    .RegisterAgent(fileWriter)
    .AddStep("FileReaderAgent", config =>
    {
        config.InputTransformer = _ => new FileInput
        {
            FilePath = "input.csv",
            Options = new FileProcessingOptions { TargetFormat = FileFormat.Json }
        };
    })
    .AddConditionalStep("DataValidatorAgent", 
        result => result.Success,
        config =>
        {
            config.InputTransformer = prev => new ValidationInput
            {
                Data = ParseFileContent(prev),
                Rules = GetValidationRules()
            };
        })
    .AddStep("DataTransformerAgent", config =>
    {
        config.InputTransformer = prev => new PipelineInput
        {
            PreviousResult = prev,
            AdditionalPrompt = "Transform validated data to normalized format"
        };
    })
    .AddStep("FileWriterAgent", config =>
    {
        config.InputTransformer = prev => new FileInput
        {
            FilePath = "output.json",
            Content = SerializeResult(prev)
        };
    })
    .Build();

// Execute workflow
var result = await workflow.ExecuteAsync("Process the customer data file");
```

#### Example 2: Decision Workflow with Branching

```csharp
var workflow = new WorkflowBuilder()
    .WithName("ContentModerationWorkflow")
    .RegisterAgent(analyzer)
    .RegisterAgent(approver)
    .RegisterAgent(rejector)
    .RegisterAgent(notifier)
    .AddStep("ContentAnalyzerAgent")
    .AddConditionalStep("ApprovalAgent",
        result => result is BooleanResult br && br.Value)
    .AddConditionalStep("RejectionAgent",
        result => result is BooleanResult br && !br.Value)
    .AddStep("NotificationAgent")
    .Build();
```

#### Example 3: Parallel Processing Workflow

```csharp
var workflow = new WorkflowBuilder()
    .WithName("DataEnrichmentWorkflow")
    .RegisterAgent(dataFetcher)
    .RegisterAgent(enricher1)
    .RegisterAgent(enricher2)
    .RegisterAgent(enricher3)
    .RegisterAgent(aggregator)
    .AddStep("DataFetcherAgent")
    .AddParallelSteps(
        ("GeoEnrichmentAgent", config => config.MaxRetries = 2),
        ("DemographicEnrichmentAgent", config => config.RetryDelay = TimeSpan.FromSeconds(2)),
        ("SocialEnrichmentAgent", config => config.ContinueOnError = true)
    )
    .AddStep("DataAggregatorAgent")
    .Build();
```

## Implementation Guidelines

### 1. Migration Strategy
- Implement new types alongside existing code
- Use implicit operators for backward compatibility
- Gradually migrate agents to typed versions

### 2. Serialization
- Use `JsonPropertyName("$type")` for polymorphic serialization
- Consider MessagePack for high-performance scenarios
- Implement custom converters as needed

### 3. Error Handling
- Every `AgentResult` includes success/failure information
- Workflows support global error handlers
- Built-in retry with exponential backoff

### 4. Observability
- `ExecutionMetadata` captures important metrics
- `PerformedAction` tracks all tool invocations
- Easy integration with telemetry systems

### 5. Performance Considerations
- Workflow context enables result caching
- Native support for parallel execution
- Agent pooling to avoid reconnections

### 6. Testing
```csharp
// Easy mocking
public class MockAgent : IAgent<TextInput, TextResult>
{
    public Task<TextResult> ExecuteAsync(TextInput input, CancellationToken ct)
    {
        return Task.FromResult(new TextResult 
        { 
            Success = true, 
            Content = $"Processed: {input.Prompt}" 
        });
    }
}
```

## Benefits of This Architecture

1. **Type Safety**: Compile-time errors instead of runtime failures
2. **Composability**: Easy to combine agents in complex workflows
3. **Testability**: Each component is isolated and mockable
4. **Observability**: Built-in metrics and logging
5. **Flexibility**: Support for sequential, parallel, and conditional workflows
6. **Backward Compatibility**: Existing agents continue to work

## Next Steps

1. Implement base types in a new branch
2. Create unit tests for type conversions
3. Migrate one agent as proof of concept
4. Build a simple workflow to validate the design
5. Refactor remaining agents incrementally