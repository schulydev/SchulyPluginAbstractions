namespace Schuly.Plugin.Abstractions
{
    public sealed record SchoolRecord(string Name, string? Website = null, string? LogoUrl = null);

    public sealed record StudentRecord
    {
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Email { get; init; }
        public string? PrivateEmail { get; init; }
        public string? PhoneNumber { get; init; }
        public string? Street { get; init; }
        public string? City { get; init; }
        public string? Zip { get; init; }
        public DateOnly? Birthday { get; init; }
        public DateOnly? EntryDate { get; init; }
        public DateOnly? LeaveDate { get; init; }
        public string? ProfilePictureUrl { get; init; }
    }

    public enum AgendaKind
    {
        Event = 0,
        Lesson = 1,
        Test = 2,
        Holiday = 3,
    }

    /// <summary>A timetable entry. A date-only entry (e.g. a holiday) starts at UTC midnight of its day.</summary>
    public sealed record AgendaRecord(string ExternalId, AgendaKind Kind, string Title, DateTimeOffset Start, DateTimeOffset? End = null, string? Place = null, string? Description = null);

    /// <summary>A grade for one exam. <see cref="Subject"/> becomes the class, <see cref="ExamName"/> the exam within it.</summary>
    public sealed record GradeRecord(string ExternalId, string Subject, string ExamName, decimal Score, decimal Weight = 1, DateOnly? ExamDate = null);

    public sealed record AbsenceRecord(string ExternalId, DateTimeOffset From, DateTimeOffset Until, string? Reason = null, bool IsLate = false);

    /// <summary>A student document; <see cref="File"/> is what <see cref="IPluginFileStore.SaveAsync"/> returned, if the file was downloaded.</summary>
    public sealed record DocumentRecord(string ExternalId, string Title, string? FileName = null, string? Category = null, string? Comment = null, string? EnteredBy = null, StoredFile? File = null);
}
