namespace Schuly.Plugin.Abstractions
{
    /// <summary>
    /// Accounts a user linked with an outside identity provider (e.g. Microsoft), shared by every
    /// plugin: one plugin links the account, another signs in with it. Every call is scoped to the
    /// given user; secrets are encrypted at rest.
    /// </summary>
    public interface ILinkedAccounts
    {
        Task<IReadOnlyList<LinkedAccount>> ListAsync(Guid applicationUserId, string provider, CancellationToken cancellationToken = default);
        Task<LinkedAccount?> GetAsync(Guid applicationUserId, Guid linkedAccountId, CancellationToken cancellationToken = default);
        /// <summary>Adds the account, or updates it when the user already linked this <see cref="LinkedAccountRecord.ExternalId"/> at the provider.</summary>
        Task<LinkedAccount> SaveAsync(Guid applicationUserId, LinkedAccountRecord account, CancellationToken cancellationToken = default);
        /// <summary>Removes the account and its secrets.</summary>
        Task<bool> RemoveAsync(Guid applicationUserId, Guid linkedAccountId, CancellationToken cancellationToken = default);
        Task<string?> GetSecretAsync(Guid applicationUserId, Guid linkedAccountId, string name, CancellationToken cancellationToken = default);
        Task SetSecretAsync(Guid applicationUserId, Guid linkedAccountId, string name, string value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores the account's profile picture (PNG, JPEG or WebP, at most 2 MB) so the user can tell their accounts apart.
        /// Null <paramref name="content"/> removes it. The host keeps it in its own encrypted storage, shows it only to the
        /// account owner and deletes it with the account. Returns false when the host rejects the picture or does not
        /// support pictures (hosts before 0.11.3 run this default).
        /// </summary>
        Task<bool> SetPictureAsync(Guid applicationUserId, Guid linkedAccountId, Stream? content, string? contentType, CancellationToken cancellationToken = default) => Task.FromResult(false);

        /// <summary>
        /// Stores the picture of a school account the user owns, the student profile a plugin syncs into (for example the
        /// photo on a student ID card), under the same rules as <see cref="SetPictureAsync"/>. The host shows it on account rows
        /// that point at <paramref name="schoolUserId"/> (<c>LinkedAccountSummary.SchoolUserId</c>), ahead of any linked account's
        /// picture. Returns false when the school user is not the user's, the host rejects the picture or does not support
        /// school account pictures (hosts before 0.11.4 run this default).
        /// </summary>
        Task<bool> SetSchoolUserPictureAsync(Guid applicationUserId, Guid schoolUserId, Stream? content, string? contentType, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    public sealed record LinkedAccountRecord(string Provider, string ExternalId, string Email, string? DisplayName);

    public sealed record LinkedAccount(Guid Id, string Provider, string ExternalId, string Email, string? DisplayName, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

    public static class LinkedProviders
    {
        public const string Microsoft = "microsoft";
    }

    public static class LinkedSecrets
    {
        /// <summary>The provider's OAuth refresh token.</summary>
        public const string RefreshToken = "refresh_token";

        /// <summary>The provider's sign-in session as a JSON array of <c>{domain, name, path, value}</c> cookies.</summary>
        public const string SessionCookies = "session_cookies";
    }
}
