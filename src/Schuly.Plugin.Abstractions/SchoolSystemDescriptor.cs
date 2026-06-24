using System.Collections.Generic;

namespace Schuly.Plugin.Abstractions
{
    /// <summary>
    /// A plugin's self-description of one login system it serves. The backend
    /// collects these from every loaded plugin and seeds the school-systems catalog
    /// (seed-if-missing by <see cref="Key"/>), so the app can render the system
    /// picker and each system's login form without the operator supplying catalog
    /// config. Everything the app needs to offer a system lives here.
    /// </summary>
    public record SchoolSystemDescriptor
    {
        /// <summary>
        /// Stable key the app branches its login flow on; must match the plugin's
        /// <see cref="IPluginLogin.SystemKey"/>, e.g. "schulnetz".
        /// </summary>
        public required string Key { get; init; }

        /// <summary>Name shown in the system picker.</summary>
        public required string DisplayName { get; init; }

        /// <summary>How the app drives login: "credentials" or "oauth-webview".</summary>
        public string LoginMethod { get; init; } = "credentials";

        /// <summary>Optional logo shown in the picker.</summary>
        public string? LogoUrl { get; init; }

        /// <summary>
        /// Private-mode fetch strategy: "token" (a headless login mints a bearer
        /// token + refreshable session) or "scrape" (credentials replayed per fetch).
        /// </summary>
        public string? PrivateAuthStrategy { get; init; }

        /// <summary>
        /// Base path of this system's stateless plugin endpoints used by private mode,
        /// e.g. "/api/plugins/schulware/stateless".
        /// </summary>
        public string? StatelessBasePath { get; init; }

        /// <summary>
        /// Base path of this system's plugin endpoints used by account mode for
        /// accounts/sync/status, e.g. "/api/plugins/schulware".
        /// </summary>
        public string? PluginBasePath { get; init; }

        /// <summary>Whether the system is offered. Defaults to true.</summary>
        public bool Enabled { get; init; } = true;

        /// <summary>Display order in the picker (ascending).</summary>
        public int SortOrder { get; init; }

        /// <summary>Inputs the app renders to collect what <see cref="IPluginLogin.ConnectAsync"/> needs.</summary>
        public IReadOnlyList<SchoolSystemLoginFieldDescriptor> LoginFields { get; init; } = [];
    }

    /// <summary>One input the app renders on a system's login form.</summary>
    public record SchoolSystemLoginFieldDescriptor
    {
        /// <summary>Field identifier submitted with the collected value, e.g. "baseUrl".</summary>
        public required string Key { get; init; }

        /// <summary>Label shown next to the input.</summary>
        public required string Label { get; init; }

        /// <summary>Input type hint: "url", "text" or "password".</summary>
        public string Type { get; init; } = "text";

        /// <summary>Optional placeholder shown in the empty input.</summary>
        public string? Placeholder { get; init; }

        /// <summary>Optional value pre-filled into the input.</summary>
        public string? DefaultValue { get; init; }

        /// <summary>Whether the field must be filled. Defaults to true.</summary>
        public bool Required { get; init; } = true;
    }
}
