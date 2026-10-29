using Schuly.Plugin.Abstractions.Linking;

namespace Schuly.Plugin.Abstractions.Tests
{
    public class LinkStepValidatorTests
    {
        private static readonly IReadOnlyDictionary<string, string> NoValues = new Dictionary<string, string>();

        private static IReadOnlyDictionary<string, string> Vals(params (string Key, string Value)[] pairs) => pairs.ToDictionary(p => p.Key, p => p.Value);

        private static LinkStep Form(params LinkField[] fields) => new() { Id = "f", Kind = LinkStepKind.Form, Fields = fields };

        private static LinkField Field(string key, Action<LinkFieldBuilder>? configure = null) => new LinkField { Key = key, Label = key };

        [Test]
        public async Task Valid_form_passes()
        {
            var step = Form(new LinkField { Key = "name", Label = "Name" });

            var result = LinkStepValidator.Validate(step, Vals(("name", "x")), null);

            await Assert.That(result.IsValid).IsTrue();
        }

        [Test]
        public async Task Missing_required_key_fails()
        {
            var step = Form(new LinkField { Key = "name", Label = "Name" });

            var result = LinkStepValidator.Validate(step, NoValues, null);

            await Assert.That(result.IsValid).IsFalse();
            await Assert.That(result.Errors[0]).Contains("name");
        }

        [Test]
        public async Task Missing_optional_key_passes()
        {
            var step = Form(new LinkField { Key = "name", Label = "Name", Required = false });

            await Assert.That(LinkStepValidator.Validate(step, NoValues, null).IsValid).IsTrue();
        }

        [Test]
        public async Task Unknown_key_fails()
        {
            var step = Form(new LinkField { Key = "name", Label = "Name" });

            var result = LinkStepValidator.Validate(step, Vals(("name", "x"), ("extra", "y")), null);

            await Assert.That(result.IsValid).IsFalse();
            await Assert.That(result.Errors[0]).Contains("extra");
        }

        [Test]
        public async Task MaxLength_is_enforced_and_defaults_to_2048()
        {
            var limited = Form(new LinkField { Key = "a", Label = "A", MaxLength = 5 });
            var unlimited = Form(new LinkField { Key = "a", Label = "A" });

            await Assert.That(LinkStepValidator.Validate(limited, Vals(("a", "123456")), null).IsValid).IsFalse();
            await Assert.That(LinkStepValidator.Validate(limited, Vals(("a", "12345")), null).IsValid).IsTrue();
            await Assert.That(LinkStepValidator.Validate(unlimited, Vals(("a", new string('x', 2048))), null).IsValid).IsTrue();
            await Assert.That(LinkStepValidator.Validate(unlimited, Vals(("a", new string('x', 2049))), null).IsValid).IsFalse();
        }

        [Test]
        public async Task Pattern_is_enforced()
        {
            var step = Form(new LinkField { Key = "a", Label = "A", Pattern = "^[0-9]+$" });

            await Assert.That(LinkStepValidator.Validate(step, Vals(("a", "123")), null).IsValid).IsTrue();
            await Assert.That(LinkStepValidator.Validate(step, Vals(("a", "12x")), null).IsValid).IsFalse();
        }

        [Test]
        public async Task Catastrophic_pattern_times_out_and_fails()
        {
            var step = Form(new LinkField { Key = "a", Label = "A", Pattern = "^(a+)+$" });
            var input = new string('a', 60) + "!";

            var result = LinkStepValidator.Validate(step, Vals(("a", input)), null);

            await Assert.That(result.IsValid).IsFalse();
        }

        [Test]
        [Arguments("a@b.ch", true)]
        [Arguments("a.b@school.example.org", true)]
        [Arguments("nope", false)]
        [Arguments("a@b", false)]
        [Arguments("a b@c.de", false)]
        [Arguments("@c.de", false)]
        [Arguments("a@@c.de", false)]
        public async Task Email_shape(string value, bool valid)
        {
            var step = Form(new LinkField { Key = "e", Label = "E", Type = LinkFieldType.Email });

            await Assert.That(LinkStepValidator.Validate(step, Vals(("e", value)), null).IsValid).IsEqualTo(valid);
        }

        [Test]
        [Arguments("https://school.example.ch", true)]
        [Arguments("http://localhost:8080", true)]
        [Arguments("http://127.0.0.1/x", true)]
        [Arguments("http://school.example.ch", false)]
        [Arguments("ftp://school.example.ch", false)]
        [Arguments("javascript:alert(1)", false)]
        [Arguments("school.example.ch", false)]
        public async Task Url_must_be_https_or_localhost_http(string value, bool valid)
        {
            var step = Form(new LinkField { Key = "u", Label = "U", Type = LinkFieldType.Url });

            await Assert.That(LinkStepValidator.Validate(step, Vals(("u", value)), null).IsValid).IsEqualTo(valid);
        }

        [Test]
        public async Task Select_field_must_be_in_options_unless_AllowCustom()
        {
            var options = new[] { new LinkOption("a", "A"), new LinkOption("b", "B") };
            var strict = Form(new LinkField { Key = "s", Label = "S", Type = LinkFieldType.Select, Options = options });
            var custom = Form(new LinkField { Key = "s", Label = "S", Type = LinkFieldType.Select, Options = options, AllowCustom = true });

            await Assert.That(LinkStepValidator.Validate(strict, Vals(("s", "a")), null).IsValid).IsTrue();
            await Assert.That(LinkStepValidator.Validate(strict, Vals(("s", "z")), null).IsValid).IsFalse();
            await Assert.That(LinkStepValidator.Validate(custom, Vals(("s", "z")), null).IsValid).IsTrue();
        }

        [Test]
        public async Task Otp_requires_exact_length_and_digits()
        {
            var step = new LinkStep { Id = "o", Kind = LinkStepKind.Otp, Otp = new LinkOtp(6, null) };

            await Assert.That(LinkStepValidator.Validate(step, Vals(("code", "123456")), null).IsValid).IsTrue();
            await Assert.That(LinkStepValidator.Validate(step, Vals(("code", "12345")), null).IsValid).IsFalse();
            await Assert.That(LinkStepValidator.Validate(step, Vals(("code", "1234567")), null).IsValid).IsFalse();
            await Assert.That(LinkStepValidator.Validate(step, Vals(("code", "12345a")), null).IsValid).IsFalse();
            await Assert.That(LinkStepValidator.Validate(step, NoValues, null).IsValid).IsFalse();
        }

        [Test]
        public async Task Otp_non_numeric_allows_letters()
        {
            var step = new LinkStep { Id = "o", Kind = LinkStepKind.Otp, Otp = new LinkOtp(4, null, false) };

            await Assert.That(LinkStepValidator.Validate(step, Vals(("code", "ab12")), null).IsValid).IsTrue();
        }

        [Test]
        public async Task Select_step_value_must_be_an_option()
        {
            var step = new LinkStep { Id = "s", Kind = LinkStepKind.Select, Options = [new LinkOption("x", "X")] };

            await Assert.That(LinkStepValidator.Validate(step, Vals(("value", "x")), null).IsValid).IsTrue();
            await Assert.That(LinkStepValidator.Validate(step, Vals(("value", "y")), null).IsValid).IsFalse();
            await Assert.That(LinkStepValidator.Validate(step, NoValues, null).IsValid).IsFalse();
            await Assert.That(LinkStepValidator.Validate(step, Vals(("other", "x")), null).IsValid).IsFalse();
        }

        [Test]
        public async Task Actions_must_be_declared_or_reserved()
        {
            var step = new LinkStep { Id = "p", Kind = LinkStepKind.PushCode, PushCode = new LinkPushCode("42", DateTimeOffset.UtcNow.AddMinutes(1), 2), Actions = [new LinkAction("use-otp", "Use a code")] };

            await Assert.That(LinkStepValidator.Validate(step, NoValues, "use-otp").IsValid).IsTrue();
            await Assert.That(LinkStepValidator.Validate(step, NoValues, "refresh").IsValid).IsTrue();
            await Assert.That(LinkStepValidator.Validate(step, NoValues, "cancel").IsValid).IsTrue();
            await Assert.That(LinkStepValidator.Validate(step, NoValues, "hack").IsValid).IsFalse();
        }

        [Test]
        public async Task Action_skips_required_checks()
        {
            var step = new LinkStep { Id = "o", Kind = LinkStepKind.Otp, Otp = new LinkOtp(6, null), Actions = [new LinkAction("use-push", "Push")] };

            await Assert.That(LinkStepValidator.Validate(step, NoValues, "use-push").IsValid).IsTrue();
        }

        [Test]
        public async Task Capture_redirect_accepts_callbackUrl_and_cookies_only()
        {
            var step = new LinkStep { Id = "b", Kind = LinkStepKind.Redirect, Redirect = new LinkRedirect("https://login.example.com/a", LinkRedirectMode.Capture, "https://app.example.com/cb", ["login.example.com"]) };

            await Assert.That(LinkStepValidator.Validate(step, Vals(("callbackUrl", "https://app.example.com/cb?code=1")), null).IsValid).IsTrue();
            await Assert.That(LinkStepValidator.Validate(step, Vals(("callbackUrl", "https://app.example.com/cb?code=1"), ("cookies", "[]")), null).IsValid).IsTrue();
            await Assert.That(LinkStepValidator.Validate(step, Vals(("callbackUrl", "https://evil.example.com/cb")), null).IsValid).IsFalse();
            await Assert.That(LinkStepValidator.Validate(step, Vals(("cookies", "[]")), null).IsValid).IsFalse();
            await Assert.That(LinkStepValidator.Validate(step, Vals(("callbackUrl", "https://app.example.com/cb"), ("x", "y")), null).IsValid).IsFalse();
        }

        [Test]
        public async Task Done_step_accepts_no_input()
        {
            var step = LinkStepResult.Done("acc", "Title").Step;

            await Assert.That(LinkStepValidator.Validate(step, NoValues, null).IsValid).IsFalse();
        }

        [Test]
        public async Task Error_messages_never_contain_submitted_values()
        {
            const string password = "S3cret-Passw0rd!";
            var step = Form(
                new LinkField { Key = "email", Label = "Email", Type = LinkFieldType.Email },
                new LinkField { Key = "password", Label = "Password", Type = LinkFieldType.Password, Secret = true, MaxLength = 4, Pattern = "^x$" },
                new LinkField { Key = "url", Label = "Url", Type = LinkFieldType.Url });

            var result = LinkStepValidator.Validate(step, Vals(("email", password), ("password", password), ("url", password), (password, password)), null);

            await Assert.That(result.IsValid).IsFalse();
            await Assert.That(result.Errors.Count).IsGreaterThan(0);
            foreach (var error in result.Errors.Where(e => !e.StartsWith("Unknown field")))
                await Assert.That(error).DoesNotContain(password);
        }

        [Test]
        public async Task Error_messages_never_contain_submitted_values_otp_select_redirect()
        {
            const string secret = "TopSecretValue99";
            var otp = new LinkStep { Id = "o", Kind = LinkStepKind.Otp, Otp = new LinkOtp(6, null) };
            var select = new LinkStep { Id = "s", Kind = LinkStepKind.Select, Options = [new LinkOption("x", "X")] };
            var redirect = new LinkStep { Id = "r", Kind = LinkStepKind.Redirect, Redirect = new LinkRedirect("https://a.example.com", LinkRedirectMode.Capture, "https://b.example.com", []) };

            var errors = LinkStepValidator.Validate(otp, Vals(("code", secret)), null).Errors
                .Concat(LinkStepValidator.Validate(select, Vals(("value", secret)), null).Errors)
                .Concat(LinkStepValidator.Validate(redirect, Vals(("callbackUrl", secret)), null).Errors)
                .ToList();

            await Assert.That(errors.Count).IsEqualTo(3);
            foreach (var error in errors)
                await Assert.That(error).DoesNotContain(secret);
        }

        private sealed class LinkFieldBuilder
        {
        }
    }

    public class LinkDescriptorValidatorTests
    {
        private static LinkProviderDescriptor Descriptor(string id = "schulnetz", params LinkFlowDescriptor[] flows) => new()
        {
            Id = id,
            DisplayName = "Schulnetz",
            Flows = flows.Length == 0 ? [new LinkFlowDescriptor { Id = "main", Label = "Sign in" }] : flows,
        };

        [Test]
        public async Task Valid_descriptor_passes()
        {
            await Assert.That(LinkDescriptorValidator.ValidateDescriptor(Descriptor()).IsValid).IsTrue();
        }

        [Test]
        [Arguments("schulnetz", true)]
        [Arguments("a1", true)]
        [Arguments("my-provider-2", true)]
        [Arguments("a", false)]
        [Arguments("-ab", false)]
        [Arguments("Ab", false)]
        [Arguments("a_b", false)]
        [Arguments("", false)]
        public async Task Provider_id_regex(string id, bool valid)
        {
            await Assert.That(LinkDescriptorValidator.ValidateDescriptor(Descriptor(id)).IsValid).IsEqualTo(valid);
        }

        [Test]
        public async Task Flow_id_regex_and_uniqueness()
        {
            var bad = Descriptor("schulnetz", new LinkFlowDescriptor { Id = "Bad Id", Label = "x" });
            var dup = Descriptor("schulnetz", new LinkFlowDescriptor { Id = "main", Label = "x" }, new LinkFlowDescriptor { Id = "main", Label = "y" });

            await Assert.That(LinkDescriptorValidator.ValidateDescriptor(bad).IsValid).IsFalse();
            await Assert.That(LinkDescriptorValidator.ValidateDescriptor(dup).IsValid).IsFalse();
        }

        [Test]
        public async Task Descriptor_without_flows_fails()
        {
            var descriptor = new LinkProviderDescriptor { Id = "schulnetz", DisplayName = "S" };

            await Assert.That(LinkDescriptorValidator.ValidateDescriptor(descriptor).IsValid).IsFalse();
        }

        [Test]
        [Arguments("name", true)]
        [Arguments("baseUrl", true)]
        [Arguments("a_1", true)]
        [Arguments("1a", false)]
        [Arguments("a-b", false)]
        [Arguments("", false)]
        public async Task Field_key_regex(string key, bool valid)
        {
            var step = new LinkStep { Id = "f", Kind = LinkStepKind.Form, Fields = [new LinkField { Key = key, Label = "L" }] };

            await Assert.That(LinkDescriptorValidator.ValidateStep(step, LinkPlatform.Web).IsValid).IsEqualTo(valid);
        }

        [Test]
        public async Task Duplicate_field_keys_fail()
        {
            var step = new LinkStep { Id = "f", Kind = LinkStepKind.Form, Fields = [new LinkField { Key = "a", Label = "L" }, new LinkField { Key = "a", Label = "L" }] };

            await Assert.That(LinkDescriptorValidator.ValidateStep(step, LinkPlatform.Web).IsValid).IsFalse();
        }

        [Test]
        public async Task Password_field_must_be_secret_without_default()
        {
            var notSecret = new LinkStep { Id = "f", Kind = LinkStepKind.Form, Fields = [new LinkField { Key = "p", Label = "P", Type = LinkFieldType.Password }] };
            var withDefault = new LinkStep { Id = "f", Kind = LinkStepKind.Form, Fields = [new LinkField { Key = "p", Label = "P", Type = LinkFieldType.Password, Secret = true, DefaultValue = "x" }] };
            var ok = new LinkStep { Id = "f", Kind = LinkStepKind.Form, Fields = [new LinkField { Key = "p", Label = "P", Type = LinkFieldType.Password, Secret = true }] };

            await Assert.That(LinkDescriptorValidator.ValidateStep(notSecret, LinkPlatform.Web).IsValid).IsFalse();
            await Assert.That(LinkDescriptorValidator.ValidateStep(withDefault, LinkPlatform.Web).IsValid).IsFalse();
            await Assert.That(LinkDescriptorValidator.ValidateStep(ok, LinkPlatform.Web).IsValid).IsTrue();
        }

        [Test]
        public async Task Invalid_field_pattern_fails()
        {
            var step = new LinkStep { Id = "f", Kind = LinkStepKind.Form, Fields = [new LinkField { Key = "a", Label = "L", Pattern = "([" }] };

            await Assert.That(LinkDescriptorValidator.ValidateStep(step, LinkPlatform.Web).IsValid).IsFalse();
        }

        [Test]
        public async Task Device_code_verification_uri_must_be_https()
        {
            LinkStep Step(string uri) => new() { Id = "d", Kind = LinkStepKind.DeviceCode, DeviceCode = new LinkDeviceCode("ABCD", uri, DateTimeOffset.UtcNow.AddMinutes(5), 5) };

            await Assert.That(LinkDescriptorValidator.ValidateStep(Step("https://microsoft.com/devicelogin"), LinkPlatform.Web).IsValid).IsTrue();
            await Assert.That(LinkDescriptorValidator.ValidateStep(Step("http://microsoft.com/devicelogin"), LinkPlatform.Web).IsValid).IsFalse();
            await Assert.That(LinkDescriptorValidator.ValidateStep(Step("javascript:alert(1)"), LinkPlatform.Web).IsValid).IsFalse();
        }

        [Test]
        public async Task Redirect_url_must_be_https()
        {
            LinkStep Step(string url) => new() { Id = "r", Kind = LinkStepKind.Redirect, Redirect = new LinkRedirect(url, LinkRedirectMode.HostCallback, null, []) };

            await Assert.That(LinkDescriptorValidator.ValidateStep(Step("https://login.example.com"), LinkPlatform.Web).IsValid).IsTrue();
            await Assert.That(LinkDescriptorValidator.ValidateStep(Step("http://login.example.com"), LinkPlatform.Web).IsValid).IsFalse();
        }

        [Test]
        public async Task Capture_redirect_is_rejected_on_web_only()
        {
            var step = new LinkStep { Id = "r", Kind = LinkStepKind.Redirect, Redirect = new LinkRedirect("https://login.example.com", LinkRedirectMode.Capture, "https://app/cb", ["login.example.com"]) };

            await Assert.That(LinkDescriptorValidator.ValidateStep(step, LinkPlatform.Web).IsValid).IsFalse();
            await Assert.That(LinkDescriptorValidator.ValidateStep(step, LinkPlatform.Mobile).IsValid).IsTrue();
        }

        [Test]
        public async Task Reserved_action_ids_are_rejected()
        {
            var step = new LinkStep { Id = "p", Kind = LinkStepKind.PushCode, PushCode = new LinkPushCode(null, DateTimeOffset.UtcNow.AddMinutes(1), 2), Actions = [new LinkAction("refresh", "R")] };

            await Assert.That(LinkDescriptorValidator.ValidateStep(step, LinkPlatform.Web).IsValid).IsFalse();
        }

        [Test]
        public async Task Select_step_needs_options()
        {
            var step = new LinkStep { Id = "s", Kind = LinkStepKind.Select };

            await Assert.That(LinkDescriptorValidator.ValidateStep(step, LinkPlatform.Web).IsValid).IsFalse();
        }
    }

    public class LinkStepResultTests
    {
        [Test]
        public async Task Done_creates_done_step()
        {
            var result = LinkStepResult.Done("acc-1", "My account", "st");

            await Assert.That(result.Step.Kind).IsEqualTo(LinkStepKind.Done);
            await Assert.That(result.Step.Id).IsEqualTo("done");
            await Assert.That(result.Step.Done!.AccountId).IsEqualTo("acc-1");
            await Assert.That(result.Step.Done!.Title).IsEqualTo("My account");
            await Assert.That(result.State).IsEqualTo("st");
        }

        [Test]
        public async Task Fail_creates_error_step()
        {
            var result = LinkStepResult.Fail("nope");

            await Assert.That(result.Step.Kind).IsEqualTo(LinkStepKind.Error);
            await Assert.That(result.Step.Id).IsEqualTo("error");
            await Assert.That(result.Step.Error).IsEqualTo("nope");
            await Assert.That(result.State).IsNull();
        }
    }
}

namespace Schuly.Plugin.Abstractions.Tests
{
    public class LinkedAccountSummaryTests
    {
        [Test]
        public async Task Status_order_and_LastSyncedAt_default()
        {
            var summary = new LinkedAccountSummary("1", "T", null, LinkedAccountStatus.Syncing, null, null);

            await Assert.That(Enum.GetValues<LinkedAccountStatus>().ToList().IndexOf(LinkedAccountStatus.Syncing)).IsEqualTo(1);
            await Assert.That(Enum.GetValues<LinkedAccountStatus>().ToList().IndexOf(LinkedAccountStatus.Expired)).IsEqualTo(2);
            await Assert.That(Enum.GetValues<LinkedAccountStatus>().ToList().IndexOf(LinkedAccountStatus.Error)).IsEqualTo(3);
            await Assert.That(summary.LastSyncedAt).IsNull();
        }
    }
}
