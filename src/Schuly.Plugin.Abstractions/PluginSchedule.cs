using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Schuly.Plugin.Abstractions
{
    /// <summary>
    /// A plugin's declared <em>default</em> schedule for an <see cref="IPluginBackgroundTask"/>.
    /// This type is scheduler-agnostic and has no dependency beyond the BCL - it only describes
    /// intent; the host maps it onto its own scheduler (TickerQ). The host operator can override
    /// the cadence per deployment, so treat these values as a sensible default, not a guarantee.
    /// </summary>
    public sealed record PluginSchedule(string Cron, int Retries = 0, IReadOnlyList<TimeSpan>? RetryIntervals = null, bool RunOnStartup = false)
    {
        private static readonly IReadOnlyDictionary<string, int> MonthNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["JAN"] = 1, ["FEB"] = 2, ["MAR"] = 3, ["APR"] = 4, ["MAY"] = 5, ["JUN"] = 6, ["JUL"] = 7, ["AUG"] = 8, ["SEP"] = 9, ["OCT"] = 10, ["NOV"] = 11, ["DEC"] = 12 };

        private static readonly IReadOnlyDictionary<string, int> DayOfWeekNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["SUN"] = 0, ["MON"] = 1, ["TUE"] = 2, ["WED"] = 3, ["THU"] = 4, ["FRI"] = 5, ["SAT"] = 6 };

        // Backed by an explicit field with a custom init accessor - rather than a plain
        // auto-property field initializer - so validation also runs on with-expressions. A field
        // initializer alone only runs once, from the primary constructor; the copy performed by
        // with bypasses it, so re-validation has to live in the init accessor itself.
        private readonly string _cron = ValidateCron(Cron);
        private readonly int _retries = ValidateRetries(Retries);
        private readonly IReadOnlyList<TimeSpan>? _retryIntervals = ValidateRetryIntervals(RetryIntervals);

        /// <summary>
        /// Standard 5-field cron expression (<c>minute hour day-of-month month day-of-week</c>).
        /// Validated on construction and on every <see langword="with"/>-expression; an invalid
        /// expression throws <see cref="ArgumentException"/>.
        /// </summary>
        public string Cron { get => _cron; init => _cron = ValidateCron(value); }

        /// <summary>Number of times the host retries a failed execution.</summary>
        public int Retries { get => _retries; init => _retries = ValidateRetries(value); }

        /// <summary>
        /// Delay before each retry attempt. When there are more retries than intervals, the host
        /// reuses the last interval for the remaining attempts. <see langword="null"/> means the
        /// host's own default backoff applies.
        /// </summary>
        public IReadOnlyList<TimeSpan>? RetryIntervals { get => _retryIntervals; init => _retryIntervals = ValidateRetryIntervals(value); }

        /// <summary>Whether the host should also run the task once immediately at startup.</summary>
        public bool RunOnStartup { get; init; } = RunOnStartup;

        /// <summary>
        /// Builds a schedule that fires at a fixed interval. Only intervals that map onto a true
        /// fixed-cadence cron are accepted: whole minutes that evenly divide 60 (1, 2, 3, 4, 5, 6,
        /// 10, 12, 15, 20, 30, 60), or whole hours that evenly divide 24 (1, 2, 3, 4, 6, 8, 12,
        /// 24). A step like <c>*/7</c> in the minute field is rejected on purpose: it fires at
        /// :00, :07, ... :56 and then jumps only 4 minutes at the hour boundary, so it does not
        /// actually repeat every 7 minutes.
        /// </summary>
        /// <param name="interval">The fixed interval between executions.</param>
        /// <returns>A schedule whose <see cref="Cron"/> fires at the given interval.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="interval"/> is zero or negative, or cannot be expressed exactly as a
        /// 5-field cron. Use the <see cref="PluginSchedule(string, int, IReadOnlyList{TimeSpan}, bool)"/>
        /// constructor directly for an arbitrary schedule.
        /// </exception>
        public static PluginSchedule Every(TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "Interval must be greater than zero.");
            }

            if (interval.Ticks % TimeSpan.TicksPerMinute == 0)
            {
                var totalMinutes = interval.Ticks / TimeSpan.TicksPerMinute;

                if (totalMinutes == 60)
                {
                    return new PluginSchedule("0 * * * *");
                }

                if (totalMinutes < 60 && 60 % totalMinutes == 0)
                {
                    return new PluginSchedule(FormattableString.Invariant($"*/{totalMinutes} * * * *"));
                }

                if (totalMinutes % 60 == 0)
                {
                    var totalHours = totalMinutes / 60;

                    if (totalHours == 24)
                    {
                        return new PluginSchedule("0 0 * * *");
                    }

                    if (totalHours < 24 && 24 % totalHours == 0)
                    {
                        return new PluginSchedule(FormattableString.Invariant($"0 */{totalHours} * * *"));
                    }
                }
            }

            throw new ArgumentOutOfRangeException(nameof(interval), interval, "Interval must be a whole number of minutes dividing 60 (1, 2, 3, 4, 5, 6, 10, 12, 15, 20, 30, 60) or a whole number of hours dividing 24 (1, 2, 3, 4, 6, 8, 12, 24) - anything else cannot be expressed as a true fixed-cadence cron. Use the PluginSchedule(string) constructor directly for an arbitrary schedule.");
        }

        /// <summary>Builds a schedule that fires once a day at the given time.</summary>
        /// <param name="hour">The hour of day, 0-23.</param>
        /// <param name="minute">The minute of the hour, 0-59. Defaults to 0.</param>
        /// <returns>A schedule whose <see cref="Cron"/> fires daily at <paramref name="hour"/>:<paramref name="minute"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="hour"/> or <paramref name="minute"/> is out of range.</exception>
        public static PluginSchedule Daily(int hour, int minute = 0)
        {
            if (hour < 0 || hour > 23)
            {
                throw new ArgumentOutOfRangeException(nameof(hour), hour, "Hour must be between 0 and 23.");
            }

            if (minute < 0 || minute > 59)
            {
                throw new ArgumentOutOfRangeException(nameof(minute), minute, "Minute must be between 0 and 59.");
            }

            return new PluginSchedule(FormattableString.Invariant($"{minute} {hour} * * *"));
        }

        /// <summary>Builds a schedule that fires once every hour at the given minute.</summary>
        /// <param name="minute">The minute of the hour, 0-59. Defaults to 0.</param>
        /// <returns>A schedule whose <see cref="Cron"/> fires hourly at <paramref name="minute"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minute"/> is out of range.</exception>
        public static PluginSchedule Hourly(int minute = 0)
        {
            if (minute < 0 || minute > 59)
            {
                throw new ArgumentOutOfRangeException(nameof(minute), minute, "Minute must be between 0 and 59.");
            }

            return new PluginSchedule(FormattableString.Invariant($"{minute} * * * *"));
        }

        private static string ValidateCron(string cron)
        {
            if (string.IsNullOrWhiteSpace(cron))
            {
                throw new ArgumentException("Cron expression must not be null, empty, or whitespace.", nameof(Cron));
            }

            var fields = cron.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length != 5)
            {
                throw new ArgumentException($"Invalid cron expression \"{cron}\": found {fields.Length} field(s), expected 5 (minute hour day-of-month month day-of-week).", nameof(Cron));
            }

            ValidateField(cron, fields[0], 1, "minute", 0, 59, allowQuestionMark: false, names: null);
            ValidateField(cron, fields[1], 2, "hour", 0, 23, allowQuestionMark: false, names: null);
            ValidateField(cron, fields[2], 3, "day-of-month", 1, 31, allowQuestionMark: true, names: null);
            ValidateField(cron, fields[3], 4, "month", 1, 12, allowQuestionMark: false, names: MonthNames);
            ValidateField(cron, fields[4], 5, "day-of-week", 0, 7, allowQuestionMark: true, names: DayOfWeekNames);

            return cron;
        }

        private static void ValidateField(string cron, string field, int fieldIndex, string fieldName, int min, int max, bool allowQuestionMark, IReadOnlyDictionary<string, int>? names)
        {
            foreach (var token in field.Split(','))
            {
                ValidateToken(cron, token, fieldIndex, fieldName, min, max, allowQuestionMark, names);
            }
        }

        private static void ValidateToken(string cron, string token, int fieldIndex, string fieldName, int min, int max, bool allowQuestionMark, IReadOnlyDictionary<string, int>? names)
        {
            if (token.Length == 0)
            {
                ThrowInvalidToken(cron, fieldIndex, fieldName, token);
            }

            var slashIndex = token.IndexOf('/');
            var basePart = slashIndex >= 0 ? token[..slashIndex] : token;

            if (slashIndex >= 0)
            {
                var stepText = token[(slashIndex + 1)..];
                if (!int.TryParse(stepText, NumberStyles.None, CultureInfo.InvariantCulture, out var step) || step < 1)
                {
                    ThrowInvalidToken(cron, fieldIndex, fieldName, token);
                }
            }

            if (basePart == "*")
            {
                return;
            }

            if (basePart == "?")
            {
                if (!allowQuestionMark)
                {
                    ThrowInvalidToken(cron, fieldIndex, fieldName, token);
                }

                return;
            }

            var dashIndex = basePart.IndexOf('-');
            if (dashIndex >= 0)
            {
                var low = ResolveValue(basePart[..dashIndex], names);
                var high = ResolveValue(basePart[(dashIndex + 1)..], names);
                if (low is null || high is null || low < min || low > max || high < min || high > max || low > high)
                {
                    ThrowInvalidToken(cron, fieldIndex, fieldName, token);
                }

                return;
            }

            var value = ResolveValue(basePart, names);
            if (value is null || value < min || value > max)
            {
                ThrowInvalidToken(cron, fieldIndex, fieldName, token);
            }
        }

        private static int? ResolveValue(string text, IReadOnlyDictionary<string, int>? names)
        {
            if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var numeric))
            {
                return numeric;
            }

            return names is not null && names.TryGetValue(text, out var named) ? named : null;
        }

        [DoesNotReturn]
        private static void ThrowInvalidToken(string cron, int fieldIndex, string fieldName, string token) => throw new ArgumentException($"Invalid cron expression \"{cron}\": field {fieldIndex} ({fieldName}) has invalid token \"{token}\".", nameof(Cron));

        private static int ValidateRetries(int retries)
        {
            if (retries < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Retries), retries, "Retries must be zero or greater.");
            }

            return retries;
        }

        private static IReadOnlyList<TimeSpan>? ValidateRetryIntervals(IReadOnlyList<TimeSpan>? retryIntervals)
        {
            if (retryIntervals is null)
            {
                return retryIntervals;
            }

            foreach (var interval in retryIntervals)
            {
                if (interval < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(RetryIntervals), interval, "Retry intervals must not be negative.");
                }
            }

            return retryIntervals;
        }
    }
}
