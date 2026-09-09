namespace Schuly.Plugin.Abstractions.Tests
{
    public class PluginScheduleTests
    {
        [Test]
        [Arguments(1, "*/1 * * * *")]
        [Arguments(5, "*/5 * * * *")]
        [Arguments(15, "*/15 * * * *")]
        [Arguments(30, "*/30 * * * *")]
        [Arguments(60, "0 * * * *")]
        public async Task Every_minutes_produces_expected_cron(int minutes, string expected)
        {
            var schedule = PluginSchedule.Every(TimeSpan.FromMinutes(minutes));

            await Assert.That(schedule.Cron).IsEqualTo(expected);
        }

        [Test]
        [Arguments(2, "0 */2 * * *")]
        [Arguments(6, "0 */6 * * *")]
        [Arguments(12, "0 */12 * * *")]
        [Arguments(24, "0 0 * * *")]
        public async Task Every_hours_produces_expected_cron(int hours, string expected)
        {
            var schedule = PluginSchedule.Every(TimeSpan.FromHours(hours));

            await Assert.That(schedule.Cron).IsEqualTo(expected);
        }

        [Test]
        public async Task Every_zero_throws()
        {
            await Assert.That(() => PluginSchedule.Every(TimeSpan.Zero)).Throws<ArgumentOutOfRangeException>();
        }

        [Test]
        public async Task Every_negative_throws()
        {
            await Assert.That(() => PluginSchedule.Every(TimeSpan.FromMinutes(-5))).Throws<ArgumentOutOfRangeException>();
        }

        [Test]
        public async Task Every_ninety_seconds_throws()
        {
            await Assert.That(() => PluginSchedule.Every(TimeSpan.FromSeconds(90))).Throws<ArgumentOutOfRangeException>();
        }

        [Test]
        [Arguments(7)]
        [Arguments(45)]
        public async Task Every_minutes_not_dividing_sixty_throws(int minutes)
        {
            await Assert.That(() => PluginSchedule.Every(TimeSpan.FromMinutes(minutes))).Throws<ArgumentOutOfRangeException>();
        }

        [Test]
        public async Task Every_five_hours_throws()
        {
            await Assert.That(() => PluginSchedule.Every(TimeSpan.FromHours(5))).Throws<ArgumentOutOfRangeException>();
        }

        [Test]
        public async Task Every_twentyfive_hours_throws()
        {
            await Assert.That(() => PluginSchedule.Every(TimeSpan.FromHours(25))).Throws<ArgumentOutOfRangeException>();
        }

        [Test]
        public async Task Daily_defaults_minute_to_zero()
        {
            var schedule = PluginSchedule.Daily(6);

            await Assert.That(schedule.Cron).IsEqualTo("0 6 * * *");
        }

        [Test]
        public async Task Daily_with_explicit_minute()
        {
            var schedule = PluginSchedule.Daily(6, 30);

            await Assert.That(schedule.Cron).IsEqualTo("30 6 * * *");
        }

        [Test]
        [Arguments(-1, 0)]
        [Arguments(24, 0)]
        public async Task Daily_out_of_range_hour_throws(int hour, int minute)
        {
            await Assert.That(() => PluginSchedule.Daily(hour, minute)).Throws<ArgumentOutOfRangeException>();
        }

        [Test]
        [Arguments(6, -1)]
        [Arguments(6, 60)]
        public async Task Daily_out_of_range_minute_throws(int hour, int minute)
        {
            await Assert.That(() => PluginSchedule.Daily(hour, minute)).Throws<ArgumentOutOfRangeException>();
        }

        [Test]
        public async Task Hourly_defaults_minute_to_zero()
        {
            var schedule = PluginSchedule.Hourly();

            await Assert.That(schedule.Cron).IsEqualTo("0 * * * *");
        }

        [Test]
        public async Task Hourly_with_explicit_minute()
        {
            var schedule = PluginSchedule.Hourly(15);

            await Assert.That(schedule.Cron).IsEqualTo("15 * * * *");
        }

        [Test]
        [Arguments(-1)]
        [Arguments(60)]
        public async Task Hourly_out_of_range_minute_throws(int minute)
        {
            await Assert.That(() => PluginSchedule.Hourly(minute)).Throws<ArgumentOutOfRangeException>();
        }

        [Test]
        [Arguments("*/5 * * * *")]
        [Arguments("0 6 * * *")]
        [Arguments("0 0 1 * *")]
        [Arguments("0 8 * * MON-FRI")]
        [Arguments("15,45 * * * *")]
        [Arguments("0 0 1 JAN *")]
        [Arguments("0 0 ? * SUN")]
        [Arguments("0 0/2 * * *")]
        [Arguments("0 0 * * 7")]
        public async Task Valid_cron_expressions_are_accepted(string cron)
        {
            var schedule = new PluginSchedule(cron);

            await Assert.That(schedule.Cron).IsEqualTo(cron);
        }

        [Test]
        [Arguments("")]
        [Arguments("   ")]
        [Arguments("* * * *")]
        [Arguments("* * * * * *")]
        [Arguments("60 * * * *")]
        [Arguments("* 24 * * *")]
        [Arguments("* * 0 * *")]
        [Arguments("* * * 13 *")]
        [Arguments("* * * * 8")]
        [Arguments("*/0 * * * *")]
        [Arguments("0 0 * * L")]
        [Arguments("? * * * *")]
        [Arguments("abc * * * *")]
        [Arguments("5-1 * * * *")]
        public async Task Invalid_cron_expressions_are_rejected(string cron)
        {
            await Assert.That(() => new PluginSchedule(cron)).Throws<ArgumentException>();
        }

        [Test]
        public async Task Negative_retries_throws()
        {
            await Assert.That(() => new PluginSchedule("0 0 * * *", Retries: -1)).Throws<ArgumentOutOfRangeException>();
        }

        [Test]
        public async Task Negative_retry_interval_throws()
        {
            IReadOnlyList<TimeSpan> intervals = [TimeSpan.FromSeconds(-1)];

            await Assert.That(() => new PluginSchedule("0 0 * * *", RetryIntervals: intervals)).Throws<ArgumentOutOfRangeException>();
        }

        [Test]
        public async Task With_expression_setting_invalid_cron_still_throws()
        {
            var schedule = new PluginSchedule("0 0 * * *");

            await Assert.That(() => schedule with { Cron = "not a cron" }).Throws<ArgumentException>();
        }
    }
}
