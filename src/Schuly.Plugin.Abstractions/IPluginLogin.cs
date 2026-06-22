namespace Schuly.Plugin.Abstractions
{
    /// <summary>
    /// A plugin's account-connect contract. The backend (a dumb CRM) exposes a
    /// single unified login endpoint; it resolves the <see cref="IPluginLogin"/>
    /// whose <see cref="SystemKey"/> matches the requested school system and calls
    /// <see cref="ConnectAsync"/> with the login-field values the app collected
    /// from the catalog descriptor. The plugin reads the current user from
    /// <see cref="IPluginUserContext"/>, authenticates against its provider,
    /// persists the account, and returns its id. No provider auth lives in the CRM.
    /// </summary>
    public interface IPluginLogin
    {
        /// <summary>The catalog system key this login handles, e.g. "schulnetz".</summary>
        string SystemKey { get; }

        /// <summary>
        /// Connect an account from the collected login fields, keyed by the
        /// catalog's <c>loginFields</c> keys (e.g. "email", "password", "baseUrl").
        /// </summary>
        /// <param name="fields">The submitted field values.</param>
        /// <param name="displayName">Optional friendly name for the account.</param>
        Task<PluginLoginResult> ConnectAsync(
            IReadOnlyDictionary<string, string> fields,
            string? displayName,
            CancellationToken cancellationToken = default);
    }

    /// <summary>Outcome of <see cref="IPluginLogin.ConnectAsync"/>.</summary>
    public record PluginLoginResult(bool Success, Guid? AccountId = null, string? Message = null);
}
