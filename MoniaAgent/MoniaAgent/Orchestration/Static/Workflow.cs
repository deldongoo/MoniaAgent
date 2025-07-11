using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MoniaAgent.Agent;
using MoniaAgent.Agent.Inputs;
using MoniaAgent.Agent.Outputs;
using Agent = MoniaAgent.Agent.Agent;

namespace MoniaAgent.Orchestration.Static
{
    public class Workflow
    {
        private static readonly ILogger logger = LoggerFactory
            .Create(builder => builder.AddConsole())
            .CreateLogger<Workflow>();
        
        private readonly string name;
        private readonly List<WorkflowStepBase> steps;
        private readonly Dictionary<string, MoniaAgent.Agent.Agent> agents;
        
        internal Workflow(
            string name,
            List<WorkflowStepBase> steps,
            Dictionary<string, MoniaAgent.Agent.Agent> agents)
        {
            this.name = name;
            this.steps = steps;
            this.agents = agents;
        }
        
        public async Task<WorkflowExecutionResult> ExecuteAsync(
            string initialPrompt, 
            CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(new TextInput(initialPrompt), cancellationToken);
        }
        
        public async Task<WorkflowExecutionResult> ExecuteAsync(
            AgentInput initialInput, 
            CancellationToken cancellationToken = default)
        {
            var context = new WorkflowContext();
            var result = new WorkflowExecutionResult
            {
                WorkflowName = name,
                StartTime = DateTime.UtcNow,
                Context = context
            };
            
            logger.LogInformation("Starting workflow '{WorkflowName}'", name);
            
            try
            {
                AgentOutput? previousResult = null;
                
                for (int i = 0; i < steps.Count; i++)
                {
                    var step = steps[i];
                    var stepResult = await ExecuteStepAsync(step, previousResult, context, i, cancellationToken);
                    
                    result.StepResults.Add(stepResult);
                    context.ExecutionPath.Add($"{step.AgentName}:{stepResult.Success}");
                    
                    if (stepResult.Success && stepResult.Result != null)
                    {
                        previousResult = stepResult.Result;
                        context.PreviousResults[step.AgentName] = stepResult.Result;
                    }
                    else if (!step.Configuration.ContinueOnError)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Step {i + 1} failed: {stepResult.ErrorMessage}";
                        break;
                    }
                }
                
                result.EndTime = DateTime.UtcNow;
                result.TotalDuration = result.EndTime - result.StartTime;
                
                logger.LogInformation("Workflow '{WorkflowName}' completed with success: {Success}", 
                    name, result.Success);
                
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Workflow '{WorkflowName}' failed", name);
                
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.UtcNow;
                
                
                return result;
            }
        }
        
        private async Task<StepExecutionResult> ExecuteStepAsync(
            WorkflowStepBase step,
            AgentOutput? previousResult,
            WorkflowContext context,
            int stepIndex,
            CancellationToken cancellationToken)
        {
            var stepResult = new StepExecutionResult
            {
                Step = step,
                StepName = $"Step{stepIndex + 1}",
                StartTime = DateTime.UtcNow
            };
            
            try
            {
                // Check if it's a conditional step
                if (step is ConditionalStep conditionalStep && conditionalStep.Condition != null)
                {
                    if (previousResult == null || !conditionalStep.Condition(previousResult))
                    {
                        stepResult.Success = true;
                        stepResult.EndTime = DateTime.UtcNow;
                        logger.LogInformation("Skipping conditional step Step{StepIndex} for agent {AgentName}", 
                            stepIndex + 1, step.AgentName);
                        return stepResult;
                    }
                }
                
                // Get the agent
                if (!agents.TryGetValue(step.AgentName, out var agent))
                {
                    throw new InvalidOperationException($"Agent '{step.AgentName}' not found");
                }
                
                // Prepare input
                AgentInput input;
                if (step.Configuration.InputTransformer != null)
                {
                    input = step.Configuration.InputTransformer(previousResult);
                }
                else if (previousResult is TextOutput textOutput)
                {
                    input = new TextInput(textOutput.Content);
                }
                else
                {
                    input = new TextInput("Continue with the workflow");
                }
                
                // Execute with retry logic
                for (int attempt = 1; attempt <= step.Configuration.MaxRetries; attempt++)
                {
                    stepResult.AttemptNumber = attempt;
                    
                    try
                    {
                        logger.LogInformation("Executing Step{StepIndex} with agent {AgentName} (attempt {Attempt})", 
                            stepIndex + 1, step.AgentName, attempt);
                        
                        stepResult.Result = await agent.ExecuteAsync(input, cancellationToken);
                        stepResult.Success = stepResult.Result.Success;
                        
                        if (stepResult.Success)
                            break;
                        
                        if (attempt < step.Configuration.MaxRetries)
                        {
                            await Task.Delay(step.Configuration.RetryDelay, cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        stepResult.ErrorMessage = ex.Message;
                        stepResult.Success = false;
                        
                        if (attempt < step.Configuration.MaxRetries)
                        {
                            logger.LogWarning("Step{StepIndex} attempt {Attempt} failed, retrying: {Error}", 
                                stepIndex + 1, attempt, ex.Message);
                            await Task.Delay(step.Configuration.RetryDelay, cancellationToken);
                        }
                        else
                        {
                            logger.LogError("Step{StepIndex} failed after {Attempts} attempts: {Error}", 
                                stepIndex + 1, attempt, ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                stepResult.Success = false;
                stepResult.ErrorMessage = ex.Message;
                logger.LogError(ex, "Error executing step Step{StepIndex}", stepIndex + 1);
            }
            
            stepResult.EndTime = DateTime.UtcNow;
            return stepResult;
        }
    }
}