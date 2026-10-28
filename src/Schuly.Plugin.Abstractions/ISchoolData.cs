namespace Schuly.Plugin.Abstractions
{
    /// <summary>
    /// The plugin's write access to Schuly's school data. The host stamps every row with the
    /// calling plugin as its source and only lets a plugin write to students it provisioned.
    /// Sync calls upsert by <c>ExternalId</c>: an unknown id is added, a known one is updated.
    /// </summary>
    public interface ISchoolData
    {
        /// <summary>Finds a school by <see cref="SchoolRecord.Website"/> (or name when there is none), creating it if missing.</summary>
        Task<Guid> EnsureSchoolAsync(SchoolRecord school, CancellationToken cancellationToken = default);

        /// <summary>Finds the user's student profile at the school, creating it if missing, and fills blank fields from <paramref name="student"/>.</summary>
        Task<Guid> EnsureStudentAsync(Guid schoolId, Guid applicationUserId, StudentRecord student, CancellationToken cancellationToken = default);

        Task<SyncResult> SyncAgendaAsync(Guid schoolUserId, IReadOnlyCollection<AgendaRecord> entries, CancellationToken cancellationToken = default);

        /// <summary>Creates the subject (class) and exam for each grade as needed and enrols the student.</summary>
        Task<SyncResult> SyncGradesAsync(Guid schoolUserId, IReadOnlyCollection<GradeRecord> grades, CancellationToken cancellationToken = default);

        /// <summary>Creates or updates exams (e.g. upcoming ones without a grade) in the student's subjects and enrols the student.</summary>
        Task<SyncResult> SyncExamsAsync(Guid schoolUserId, IReadOnlyCollection<ExamRecord> exams, CancellationToken cancellationToken = default) => Task.FromResult(new SyncResult(0, 0));

        Task<SyncResult> SyncAbsencesAsync(Guid schoolUserId, IReadOnlyCollection<AbsenceRecord> absences, CancellationToken cancellationToken = default);

        Task<SyncResult> SyncDocumentsAsync(Guid schoolUserId, IReadOnlyCollection<DocumentRecord> documents, CancellationToken cancellationToken = default);

        /// <summary>Upserts semester reports by program, school year and half-year; a report's subjects are replaced as a whole.</summary>
        Task<SyncResult> SyncSemesterReportsAsync(Guid schoolUserId, IReadOnlyCollection<SemesterReportRecord> reports, CancellationToken cancellationToken = default);

        /// <summary>External ids of the documents this plugin already synced for the student, to skip re-downloading them.</summary>
        Task<IReadOnlySet<string>> GetDocumentIdsAsync(Guid schoolUserId, CancellationToken cancellationToken = default);
    }

    public sealed record SyncResult(int Added, int Updated);
}
