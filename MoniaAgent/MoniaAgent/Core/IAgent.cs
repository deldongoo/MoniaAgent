using MoniaAgent.Core.Inputs;
using MoniaAgent.Core.Outputs;

namespace MoniaAgent.Core
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