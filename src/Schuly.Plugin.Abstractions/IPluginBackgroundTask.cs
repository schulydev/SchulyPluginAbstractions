namespace Schuly.Plugin.Abstractions
{
    /// <summary>
    /// Recurring background work a plugin registers with the host. The backend's background
    /// task host discovers implementations and runs <see cref="ExecuteAsync"/> on the declared
    /// <see cref="Schedule"/>.
    /// </summary>
    public interface IPluginBackgroundTask
    {
        /// <summary>Task identifier, for logging and diagnostics.</summary>
        string Name { get; }

        /// <summary>The task's default schedule. The host operator can override the cadence.</summary>
        PluginSchedule Schedule { get; }

        /// <summary>One execution of the work. Resolve scoped services from <paramref name="serviceProvider"/>.</summary>
        Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }
}
