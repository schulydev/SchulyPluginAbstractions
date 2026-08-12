using System.Collections.Generic;

namespace Schuly.Plugin.Abstractions
{
    /// <summary>
    /// A plugin's self-description of one login system it serves. The backend collects
    /// these from every loaded plugin and seeds the catalog (seed-if-missing by
    /// <see cref="Key"/>), so no operator catalog config is needed.
    /// </summary>
    public record SchoolSystemDescriptor
    {
        /// <summary>Must match the plugin's <see cref="IPluginLogin.SystemKey"/>, e.g. "schulnetz".</summary>
        public required string Key { get; init; }

        public required string DisplayName { get; init; }

        /// <summary>"credentials" or "oauth-webview".</summary>
        public string LoginMethod { get; init; } = "credentials";

        public string? LogoUrl { get; init; }

        /// <summary>"token" (headless login mints a bearer token) or "scrape" (credentials replayed per fetch).</summary>
        public string? PrivateAuthStrategy { get; init; }

        /// <summary>Private-mode endpoints, e.g. "/api/plugins/schulware/stateless".</summary>
        public string? StatelessBasePath { get; init; }

        /// <summary>Account-mode endpoints, e.g. "/api/plugins/schulware".</summary>
        public string? PluginBasePath { get; init; }

        public bool Enabled { get; init; } = true;

        public int SortOrder { get; init; }

        /// <summary>Inputs the app renders to collect what <see cref="IPluginLogin.ConnectAsync"/> needs.</summary>
        public IReadOnlyList<SchoolSystemLoginFieldDescriptor> LoginFields { get; init; } = [];
    }

    public record SchoolSystemLoginFieldDescriptor
    {
        /// <summary>Submitted with the collected value, e.g. "baseUrl".</summary>
        public required string Key { get; init; }

        public required string Label { get; init; }

        /// <summary>"url", "text" or "password".</summary>
        public string Type { get; init; } = "text";

        public string? Placeholder { get; init; }

        public string? DefaultValue { get; init; }

        public bool Required { get; init; } = true;
    }
}
