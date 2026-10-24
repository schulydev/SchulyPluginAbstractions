using System.Diagnostics.CodeAnalysis;

namespace Schuly.Plugin.Abstractions
{
    /// <summary>
    /// The plugin's own encrypted secret store (tokens, credentials). Each plugin gets an
    /// isolated namespace; values are encrypted at rest and only readable through this vault.
    /// </summary>
    public interface IPluginVault
    {
        void Set(string key, string value);

        string? Get(string key);

        bool TryGet(string key, [NotNullWhen(true)] out string? value);

        bool Contains(string key);

        bool Remove(string key);

        void Clear();

        int Count { get; }
    }
}
