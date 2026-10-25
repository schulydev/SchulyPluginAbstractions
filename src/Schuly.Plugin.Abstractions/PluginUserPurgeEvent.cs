namespace Schuly.Plugin.Abstractions
{
    /// <summary>
    /// Dispatched when a Schuly user is being deleted. A plugin handles it with
    /// <see cref="IPluginEventHandler{TCommand}"/> to wipe its own data for that user —
    /// its database rows and vault secrets keyed by the user. The host deletes core-owned
    /// data itself; a plugin's own storage is only reachable through the plugin.
    /// </summary>
    public sealed record PluginUserPurgeEvent(Guid ApplicationUserId);
}
