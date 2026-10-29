namespace Schuly.Plugin.Abstractions.Linking
{
    /// <summary>
    /// A plugin's account-linking flow. The host renders whatever steps the provider returns (forms, OTP, push
    /// approval, device code, redirects, selections) so no client hardcodes plugin-specific screens.
    /// Register with <c>services.AddScoped&lt;ILinkProvider, XLinkProvider&gt;()</c>.
    /// Security rules: user-entered secrets (passwords, TOTP secrets, OTP codes, refresh tokens) must never be
    /// logged, persisted in plain text or placed in <see cref="LinkStepResult.State"/>.
    /// </summary>
    public interface ILinkProvider
    {
        LinkProviderDescriptor Descriptor { get; }
        Task<LinkStepResult> StartAsync(LinkStartContext context, CancellationToken cancellationToken = default);
        Task<LinkStepResult> SubmitAsync(LinkSubmitContext context, CancellationToken cancellationToken = default);
        Task<LinkStepResult> PollAsync(LinkPollContext context, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<LinkedAccountSummary>> ListAccountsAsync(Guid applicationUserId, CancellationToken cancellationToken = default);
        Task<bool> UnlinkAsync(Guid applicationUserId, string accountId, CancellationToken cancellationToken = default);
    }

    [Flags]
    public enum LinkPlatforms { None = 0, Web = 1, Mobile = 2, All = Web | Mobile }

    public enum LinkPlatform { Web, Mobile }

    public sealed record LinkProviderDescriptor
    {
        /// <summary>Matches <c>^[a-z0-9][a-z0-9-]{1,40}$</c>, e.g. "schulnetz".</summary>
        public required string Id { get; init; }
        public required string DisplayName { get; init; }
        public string? Description { get; init; }
        public LinkIcon Icon { get; init; } = new();
        public IReadOnlyList<ProviderCapability> Capabilities { get; init; } = [];
        public IReadOnlyList<LinkFlowDescriptor> Flows { get; init; } = [];
        public int SortOrder { get; init; }
    }

    /// <summary>
    /// Either a Lucide icon name (kebab-case, e.g. "graduation-cap") or inline SVG markup.
    /// The host sanitizes the SVG and serves it from its own URL with a strict CSP.
    /// </summary>
    public sealed record LinkIcon
    {
        public string? Lucide { get; init; }
        public string? Svg { get; init; }
    }

    /// <summary>
    /// A feature the provider's linked accounts unlock. Clients ship generic UIs per kind (not per plugin) and call
    /// the plugin at <paramref name="BasePath"/>. Known kinds: "mail", "chat", "drive", "notes", "calendar",
    /// "absence_reporting", "school_data".
    /// </summary>
    public sealed record ProviderCapability(string Kind, string Label, string? Icon, string? BasePath);

    public sealed record LinkFlowDescriptor
    {
        /// <summary>Matches <c>^[a-z0-9][a-z0-9-]{1,40}$</c>.</summary>
        public required string Id { get; init; }
        public required string Label { get; init; }
        public string? Description { get; init; }
        public LinkPlatforms Platforms { get; init; } = LinkPlatforms.All;
    }

    public sealed record LinkStartContext(Guid ApplicationUserId, string FlowId, LinkPlatform Platform, string? ReauthAccountId, string HostCallbackUrl);

    /// <summary>
    /// <paramref name="Values"/> hold what the user entered, including secrets. Pass them on, but never log or store them.
    /// </summary>
    public sealed record LinkSubmitContext(Guid ApplicationUserId, string FlowId, LinkPlatform Platform, string StepId, string? State, IReadOnlyDictionary<string, string> Values, string? Action, string? ReauthAccountId, string HostCallbackUrl);

    public sealed record LinkPollContext(Guid ApplicationUserId, string FlowId, LinkPlatform Platform, string StepId, string? State, string? ReauthAccountId);

    /// <summary>
    /// What a plugin returns from Start/Submit/Poll: the next step plus opaque plugin state. State is carried by the
    /// host between calls (encrypted, in memory, at most 10 minutes). State MUST NOT contain passwords or other
    /// user-entered secrets, and is never sent to clients.
    /// </summary>
    public sealed record LinkStepResult(LinkStep Step, string? State)
    {
        public const string DoneStepId = "done";
        public const string ErrorStepId = "error";

        /// <summary>Terminal success: the account <paramref name="accountId"/> is linked.</summary>
        public static LinkStepResult Done(string accountId, string? title, string? state = null) => new(new LinkStep { Id = DoneStepId, Kind = LinkStepKind.Done, Title = title, Done = new LinkDone(accountId, title) }, state);

        /// <summary>Terminal failure with a user-facing message. The message must not contain user-entered values.</summary>
        public static LinkStepResult Fail(string message) => new(new LinkStep { Id = ErrorStepId, Kind = LinkStepKind.Error, Error = message }, null);
    }

    public enum LinkStepKind { Form, Otp, PushCode, DeviceCode, Redirect, Select, Done, Error }

    public sealed record LinkStep
    {
        /// <summary>Step id; the client submit must echo it.</summary>
        public required string Id { get; init; }
        public required LinkStepKind Kind { get; init; }
        public string? Title { get; init; }
        public string? Description { get; init; }
        /// <summary>Inline, non-terminal error shown on this step (e.g. wrong password). Never include submitted values.</summary>
        public string? Error { get; init; }
        public IReadOnlyList<LinkField> Fields { get; init; } = [];
        public LinkOtp? Otp { get; init; }
        public LinkPushCode? PushCode { get; init; }
        public LinkDeviceCode? DeviceCode { get; init; }
        public LinkRedirect? Redirect { get; init; }
        public IReadOnlyList<LinkOption> Options { get; init; } = [];
        /// <summary>Alternative buttons ("Use a code instead"). Submitted as the action id.</summary>
        public IReadOnlyList<LinkAction> Actions { get; init; } = [];
        public string? SubmitLabel { get; init; }
        public LinkDone? Done { get; init; }
    }

    public enum LinkFieldType { Text, Email, Password, Select, Url, TotpSecret }

    public sealed record LinkField
    {
        /// <summary>Matches <c>^[A-Za-z][A-Za-z0-9_]{0,40}$</c>.</summary>
        public required string Key { get; init; }
        public required string Label { get; init; }
        public LinkFieldType Type { get; init; } = LinkFieldType.Text;
        public string? Placeholder { get; init; }
        public string? Help { get; init; }
        public string? DefaultValue { get; init; }
        public bool Required { get; init; } = true;
        /// <summary>True for Password/TotpSecret: the value is never echoed, logged or stored by the host.</summary>
        public bool Secret { get; init; }
        /// <summary>Maximum length; the host default is 2048.</summary>
        public int? MaxLength { get; init; }
        /// <summary>.NET regex, validated by the host with a timeout.</summary>
        public string? Pattern { get; init; }
        public IReadOnlyList<LinkOption> Options { get; init; } = [];
        /// <summary>Select: any value may be typed besides the options.</summary>
        public bool AllowCustom { get; init; }
    }

    public sealed record LinkOption(string Value, string Label, string? Detail = null, string? Icon = null)
    {
        /// <summary>
        /// When set, picking this option means "link an account at that provider first". The client runs that
        /// provider's link flow, then resubmits this step with action "refresh".
        /// </summary>
        public string? LinkProviderId { get; init; }
    }

    /// <summary>Submitted as the action id. Reserved ids: "refresh", "cancel".</summary>
    public sealed record LinkAction(string Id, string Label);

    public sealed record LinkOtp(int Length, string? Label, bool NumericOnly = true);

    public sealed record LinkPushCode(string? Number, DateTimeOffset ExpiresAt, int PollIntervalSeconds);

    /// <summary><paramref name="VerificationUri"/> must be https.</summary>
    public sealed record LinkDeviceCode(string UserCode, string VerificationUri, DateTimeOffset ExpiresAt, int PollIntervalSeconds);

    public enum LinkRedirectMode { HostCallback, Capture }

    /// <summary>
    /// HostCallback: the client navigates to Url; the provider must send the browser back to
    /// <see cref="LinkStartContext.HostCallbackUrl"/>. Use it only when the redirect really returns to the host.
    /// Capture: an embedded browser (mobile WebView) opens Url and intercepts the first navigation whose URL starts
    /// with CallbackPrefix, then submits {"callbackUrl": url, "cookies": json array of {domain,name,path,value} for
    /// CaptureCookieDomains}. Capture is mobile-only; the host rejects it for the web platform. Captured cookies are
    /// secrets: never log them.
    /// </summary>
    public sealed record LinkRedirect(string Url, LinkRedirectMode Mode, string? CallbackPrefix, IReadOnlyList<string> CaptureCookieDomains);

    public sealed record LinkDone(string AccountId, string? Title);

    /// <summary>Syncing: initial or ongoing sync in progress; clients show a syncing state and poll /api/linking/accounts until it leaves Syncing, then reload dependent data.</summary>
    public enum LinkedAccountStatus { Ok, Syncing, Expired, Error }

    public sealed record LinkedAccountSummary(string Id, string Title, string? Detail, LinkedAccountStatus Status, DateTimeOffset? LinkedAt, string? Message, DateTimeOffset? LastSyncedAt = null);
}
