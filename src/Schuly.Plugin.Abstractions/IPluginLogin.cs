namespace Schuly.Plugin.Abstractions
{
    /// <summary>
    /// A plugin's account-connect contract. The backend resolves the implementation whose
    /// <see cref="SystemKey"/> matches the requested system and calls
    /// <see cref="ConnectAsync"/>; the plugin authenticates against its own provider and
    /// persists the account. No provider auth lives in the backend.
    /// </summary>
    public interface IPluginLogin
    {
        /// <summary>The catalog entry this login serves; the backend seeds the catalog from it.</summary>
        SchoolSystemDescriptor SchoolSystem { get; }

        string SystemKey => SchoolSystem.Key;

        /// <summary>Connect an account from values keyed by the descriptor's <c>loginFields</c> keys.</summary>
        Task<PluginLoginResult> ConnectAsync(IReadOnlyDictionary<string, string> fields, string? displayName, CancellationToken cancellationToken = default);
    }

    public record PluginLoginResult(bool Success, Guid? AccountId = null, string? Message = null);
}
