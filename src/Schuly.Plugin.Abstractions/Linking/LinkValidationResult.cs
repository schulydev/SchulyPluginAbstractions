namespace Schuly.Plugin.Abstractions.Linking
{
    /// <summary>Outcome of a link validation. Error messages name field keys but never contain submitted values.</summary>
    public sealed record LinkValidationResult(bool IsValid, IReadOnlyList<string> Errors)
    {
        public static LinkValidationResult Valid { get; } = new(true, []);

        public static LinkValidationResult FromErrors(IReadOnlyList<string> errors) => errors.Count == 0 ? Valid : new LinkValidationResult(false, errors);
    }
}
