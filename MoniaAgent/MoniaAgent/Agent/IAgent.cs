using MoniaAgent.Agent.Inputs;
using MoniaAgent.Agent.Outputs;

namespace MoniaAgent.Agent
{
    internal interface IAgent
    {
        string Name { get; }
        string Specialty { get; }
        Task<AgentOutput> ExecuteAsync(AgentInput input, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Generic agent interface for strongly typed agents
    /// </summary>
    internal interface IAgent<TInput, TOutput> : IAgent
        where TInput : AgentInput
        where TOutput : AgentOutput
    {
        Task<TOutput> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);
    }
}