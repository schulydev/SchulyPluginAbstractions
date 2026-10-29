using System.Text.RegularExpressions;

namespace Schuly.Plugin.Abstractions.Linking
{
    /// <summary>
    /// Pure validation of a client submit against the step the plugin returned. Used by the host. Error messages
    /// mention field keys only, never submitted values (which may be passwords).
    /// </summary>
    public static class LinkStepValidator
    {
        public const int DefaultMaxLength = 2048;
        public const string RefreshAction = "refresh";
        public const string CancelAction = "cancel";
        public const string SelectValueKey = "value";
        public const string OtpValueKey = "code";
        public const string CallbackUrlKey = "callbackUrl";
        public const string CookiesKey = "cookies";
        public static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

        public static LinkValidationResult Validate(LinkStep step, IReadOnlyDictionary<string, string> values, string? action)
        {
            var errors = new List<string>();
            var hasAction = !string.IsNullOrEmpty(action);

            if (hasAction && action != RefreshAction && action != CancelAction && !step.Actions.Any(a => a.Id == action))
            {
                errors.Add("Unknown action.");
                return LinkValidationResult.FromErrors(errors);
            }

            switch (step.Kind)
            {
                case LinkStepKind.Form:
                    ValidateForm(step, values, hasAction, errors);
                    break;
                case LinkStepKind.Otp:
                    ValidateOtp(step, values, hasAction, errors);
                    break;
                case LinkStepKind.Select:
                    ValidateSelect(step, values, hasAction, errors);
                    break;
                case LinkStepKind.Redirect:
                    ValidateRedirect(step, values, hasAction, errors);
                    break;
                case LinkStepKind.PushCode:
                case LinkStepKind.DeviceCode:
                    RejectUnknownKeys(values, [], errors);
                    break;
                default:
                    errors.Add("This step does not accept input.");
                    break;
            }

            return LinkValidationResult.FromErrors(errors);
        }

        private static void ValidateForm(LinkStep step, IReadOnlyDictionary<string, string> values, bool hasAction, List<string> errors)
        {
            RejectUnknownKeys(values, step.Fields.Select(f => f.Key), errors);

            foreach (var field in step.Fields)
            {
                values.TryGetValue(field.Key, out var value);
                if (string.IsNullOrEmpty(value))
                {
                    if (field.Required && !hasAction)
                        errors.Add($"Field '{field.Key}' is required.");
                    continue;
                }

                ValidateField(field, value, errors);
            }
        }

        private static void ValidateField(LinkField field, string value, List<string> errors)
        {
            var key = field.Key;
            var max = field.MaxLength ?? DefaultMaxLength;
            if (value.Length > max)
            {
                errors.Add($"Field '{key}' is too long (max {max}).");
                return;
            }

            if (field.Required && string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"Field '{key}' is required.");
                return;
            }

            if (field.Pattern is not null)
            {
                try
                {
                    if (!Regex.IsMatch(value, field.Pattern, RegexOptions.None, RegexTimeout))
                        errors.Add($"Field '{key}' has an invalid format.");
                }
                catch (RegexMatchTimeoutException)
                {
                    errors.Add($"Field '{key}' could not be checked.");
                }
                catch (ArgumentException)
                {
                    errors.Add($"Field '{key}' could not be checked.");
                }
            }

            switch (field.Type)
            {
                case LinkFieldType.Email:
                    if (!LooksLikeEmail(value))
                        errors.Add($"Field '{key}' must be a valid email address.");
                    break;
                case LinkFieldType.Url:
                    if (!IsAllowedUrl(value))
                        errors.Add($"Field '{key}' must be an https URL.");
                    break;
                case LinkFieldType.Select:
                    if (!field.AllowCustom && !field.Options.Any(o => o.Value == value))
                        errors.Add($"Field '{key}' must be one of the offered options.");
                    break;
            }
        }

        private static void ValidateOtp(LinkStep step, IReadOnlyDictionary<string, string> values, bool hasAction, List<string> errors)
        {
            RejectUnknownKeys(values, [OtpValueKey], errors);
            values.TryGetValue(OtpValueKey, out var code);
            if (string.IsNullOrEmpty(code))
            {
                if (!hasAction)
                    errors.Add($"Field '{OtpValueKey}' is required.");
                return;
            }

            var otp = step.Otp;
            if (otp is null)
            {
                errors.Add("This step does not accept a code.");
                return;
            }

            if (code.Length != otp.Length)
                errors.Add($"Field '{OtpValueKey}' must be exactly {otp.Length} characters.");
            else if (otp.NumericOnly && !code.All(char.IsAsciiDigit))
                errors.Add($"Field '{OtpValueKey}' must contain digits only.");
        }

        private static void ValidateSelect(LinkStep step, IReadOnlyDictionary<string, string> values, bool hasAction, List<string> errors)
        {
            RejectUnknownKeys(values, [SelectValueKey], errors);
            values.TryGetValue(SelectValueKey, out var value);
            if (string.IsNullOrEmpty(value))
            {
                if (!hasAction)
                    errors.Add($"Field '{SelectValueKey}' is required.");
                return;
            }

            if (!step.Options.Any(o => o.Value == value))
                errors.Add($"Field '{SelectValueKey}' must be one of the offered options.");
        }

        private static void ValidateRedirect(LinkStep step, IReadOnlyDictionary<string, string> values, bool hasAction, List<string> errors)
        {
            if (step.Redirect?.Mode != LinkRedirectMode.Capture)
            {
                RejectUnknownKeys(values, [], errors);
                return;
            }

            RejectUnknownKeys(values, [CallbackUrlKey, CookiesKey], errors);
            values.TryGetValue(CallbackUrlKey, out var callback);
            if (string.IsNullOrEmpty(callback))
            {
                if (!hasAction)
                    errors.Add($"Field '{CallbackUrlKey}' is required.");
                return;
            }

            if (callback.Length > 16384)
                errors.Add($"Field '{CallbackUrlKey}' is too long.");
            else if (!Uri.TryCreate(callback, UriKind.Absolute, out _))
                errors.Add($"Field '{CallbackUrlKey}' must be an absolute URL.");
            else if (step.Redirect.CallbackPrefix is { Length: > 0 } prefix && !callback.StartsWith(prefix, StringComparison.Ordinal))
                errors.Add($"Field '{CallbackUrlKey}' does not match the expected callback.");

            if (values.TryGetValue(CookiesKey, out var cookies) && cookies.Length > 1_000_000)
                errors.Add($"Field '{CookiesKey}' is too long.");
        }

        private static void RejectUnknownKeys(IReadOnlyDictionary<string, string> values, IEnumerable<string> allowed, List<string> errors)
        {
            var set = new HashSet<string>(allowed, StringComparer.Ordinal);
            foreach (var key in values.Keys)
            {
                if (!set.Contains(key))
                    errors.Add($"Unknown field '{key}'.");
            }
        }

        internal static bool LooksLikeEmail(string value)
        {
            if (value.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)))
                return false;
            var at = value.IndexOf('@');
            if (at <= 0 || at != value.LastIndexOf('@') || at == value.Length - 1)
                return false;
            var domain = value[(at + 1)..];
            var dot = domain.LastIndexOf('.');
            return dot > 0 && dot < domain.Length - 1 && !domain.StartsWith('.');
        }

        /// <summary>Absolute https URL; http is allowed only for localhost.</summary>
        internal static bool IsAllowedUrl(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                return false;
            if (uri.Scheme == Uri.UriSchemeHttps)
                return !string.IsNullOrEmpty(uri.Host);
            return uri.Scheme == Uri.UriSchemeHttp && (uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase));
        }

        internal static bool IsHttpsUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && !string.IsNullOrEmpty(uri.Host);
    }
}
