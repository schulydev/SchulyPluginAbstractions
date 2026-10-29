using System.Text.RegularExpressions;

namespace Schuly.Plugin.Abstractions.Linking
{
    /// <summary>
    /// Pure validation of what a plugin declares (descriptor) and returns (steps). Used by the host before
    /// rendering anything to clients. Messages never contain user-entered values.
    /// </summary>
    public static class LinkDescriptorValidator
    {
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);
        private static readonly Regex IdRegex = new("^[a-z0-9][a-z0-9-]{1,40}$", RegexOptions.None, RegexTimeout);
        private static readonly Regex KeyRegex = new("^[A-Za-z][A-Za-z0-9_]{0,40}$", RegexOptions.None, RegexTimeout);
        private static readonly Regex LucideRegex = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.None, RegexTimeout);

        public static bool IsValidId(string? id) => id is not null && IdRegex.IsMatch(id);

        public static bool IsValidFieldKey(string? key) => key is not null && KeyRegex.IsMatch(key);

        public static LinkValidationResult ValidateDescriptor(LinkProviderDescriptor descriptor)
        {
            var errors = new List<string>();

            if (!IsValidId(descriptor.Id))
                errors.Add("Provider id must match ^[a-z0-9][a-z0-9-]{1,40}$.");
            if (string.IsNullOrWhiteSpace(descriptor.DisplayName))
                errors.Add("Provider display name is required.");
            if (descriptor.Icon.Lucide is { } lucide && !LucideRegex.IsMatch(lucide))
                errors.Add("Icon name must be kebab-case.");
            if (descriptor.Flows.Count == 0)
                errors.Add("Provider must declare at least one flow.");

            var flowIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var flow in descriptor.Flows)
            {
                if (!IsValidId(flow.Id))
                    errors.Add("Flow id must match ^[a-z0-9][a-z0-9-]{1,40}$.");
                else if (!flowIds.Add(flow.Id))
                    errors.Add($"Flow id '{flow.Id}' is declared twice.");
                if (string.IsNullOrWhiteSpace(flow.Label))
                    errors.Add($"Flow '{flow.Id}' needs a label.");
                if (flow.Platforms == LinkPlatforms.None)
                    errors.Add($"Flow '{flow.Id}' must support at least one platform.");
            }

            foreach (var capability in descriptor.Capabilities)
            {
                if (string.IsNullOrWhiteSpace(capability.Kind))
                    errors.Add("Capability kind is required.");
                if (string.IsNullOrWhiteSpace(capability.Label))
                    errors.Add($"Capability '{capability.Kind}' needs a label.");
                if (capability.BasePath is { } basePath && !basePath.StartsWith('/'))
                    errors.Add($"Capability '{capability.Kind}' base path must start with '/'.");
            }

            return LinkValidationResult.FromErrors(errors);
        }

        /// <summary>Validates a step a plugin returned for the given platform.</summary>
        public static LinkValidationResult ValidateStep(LinkStep step, LinkPlatform platform)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(step.Id))
                errors.Add("Step id is required.");

            switch (step.Kind)
            {
                case LinkStepKind.Form:
                    ValidateFields(step.Fields, errors);
                    break;
                case LinkStepKind.Otp:
                    if (step.Otp is null)
                        errors.Add("Otp step needs otp details.");
                    else if (step.Otp.Length is < 1 or > 16)
                        errors.Add("Otp length must be between 1 and 16.");
                    break;
                case LinkStepKind.PushCode:
                    if (step.PushCode is null)
                        errors.Add("PushCode step needs push details.");
                    else if (step.PushCode.PollIntervalSeconds < 1)
                        errors.Add("Poll interval must be at least 1 second.");
                    break;
                case LinkStepKind.DeviceCode:
                    if (step.DeviceCode is null)
                        errors.Add("DeviceCode step needs device code details.");
                    else
                    {
                        if (!LinkStepValidator.IsHttpsUrl(step.DeviceCode.VerificationUri))
                            errors.Add("Verification URI must be an absolute https URL.");
                        if (string.IsNullOrWhiteSpace(step.DeviceCode.UserCode))
                            errors.Add("User code is required.");
                        if (step.DeviceCode.PollIntervalSeconds < 1)
                            errors.Add("Poll interval must be at least 1 second.");
                    }
                    break;
                case LinkStepKind.Redirect:
                    ValidateRedirect(step.Redirect, platform, errors);
                    break;
                case LinkStepKind.Select:
                    if (step.Options.Count == 0)
                        errors.Add("Select step needs at least one option.");
                    ValidateOptions(step.Options, errors);
                    break;
                case LinkStepKind.Done:
                    if (step.Done is null || string.IsNullOrWhiteSpace(step.Done.AccountId))
                        errors.Add("Done step needs an account id.");
                    break;
                case LinkStepKind.Error:
                    break;
                default:
                    errors.Add("Unknown step kind.");
                    break;
            }

            var actionIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var action in step.Actions)
            {
                if (string.IsNullOrWhiteSpace(action.Id))
                    errors.Add("Action id is required.");
                else if (action.Id is LinkStepValidator.RefreshAction or LinkStepValidator.CancelAction)
                    errors.Add($"Action id '{action.Id}' is reserved.");
                else if (!actionIds.Add(action.Id))
                    errors.Add($"Action id '{action.Id}' is declared twice.");
            }

            return LinkValidationResult.FromErrors(errors);
        }

        private static void ValidateRedirect(LinkRedirect? redirect, LinkPlatform platform, List<string> errors)
        {
            if (redirect is null)
            {
                errors.Add("Redirect step needs redirect details.");
                return;
            }

            if (!LinkStepValidator.IsHttpsUrl(redirect.Url))
                errors.Add("Redirect URL must be an absolute https URL.");

            if (redirect.Mode == LinkRedirectMode.Capture)
            {
                if (platform == LinkPlatform.Web)
                    errors.Add("Capture redirects are only available on the mobile platform.");
                if (string.IsNullOrWhiteSpace(redirect.CallbackPrefix))
                    errors.Add("Capture redirect needs a callback prefix.");
            }
        }

        private static void ValidateFields(IReadOnlyList<LinkField> fields, List<string> errors)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in fields)
            {
                if (!IsValidFieldKey(field.Key))
                    errors.Add("Field key must match ^[A-Za-z][A-Za-z0-9_]{0,40}$.");
                else if (!keys.Add(field.Key))
                    errors.Add($"Field key '{field.Key}' is declared twice.");

                if (string.IsNullOrWhiteSpace(field.Label))
                    errors.Add($"Field '{field.Key}' needs a label.");
                if ((field.Type is LinkFieldType.Password or LinkFieldType.TotpSecret) && !field.Secret)
                    errors.Add($"Field '{field.Key}' must be marked secret.");
                if (field.Secret && field.DefaultValue is not null)
                    errors.Add($"Secret field '{field.Key}' must not have a default value.");
                if (field.MaxLength is < 1)
                    errors.Add($"Field '{field.Key}' has an invalid max length.");
                if (field.Type == LinkFieldType.Select && field.Options.Count == 0 && !field.AllowCustom)
                    errors.Add($"Select field '{field.Key}' needs options.");
                if (field.Pattern is not null)
                {
                    try
                    {
                        _ = new Regex(field.Pattern, RegexOptions.None, RegexTimeout);
                    }
                    catch (ArgumentException)
                    {
                        errors.Add($"Field '{field.Key}' has an invalid pattern.");
                    }
                }

                ValidateOptions(field.Options, errors);
            }
        }

        private static void ValidateOptions(IReadOnlyList<LinkOption> options, List<string> errors)
        {
            foreach (var option in options)
            {
                if (string.IsNullOrEmpty(option.Value))
                    errors.Add("Option value is required.");
                if (option.LinkProviderId is { } id && !IsValidId(id))
                    errors.Add("Option link provider id is invalid.");
            }
        }
    }
}
