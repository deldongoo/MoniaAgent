namespace MoniaAgent
{
    public interface IAgent
    {
        string Name { get; }
        string Specialty { get; }
        Task<string> Execute(string prompt);
        bool CanHandle(string task);
    }
}