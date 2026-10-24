namespace Schuly.Plugin.Abstractions
{
    /// <summary>Stores files a plugin downloads (e.g. report cards) in Schuly's encrypted document storage.</summary>
    public interface IPluginFileStore
    {
        Task<StoredFile> SaveAsync(Stream content, string fileName, string? contentType, CancellationToken cancellationToken = default);
    }

    public sealed record StoredFile(string Key, long SizeBytes);
}
