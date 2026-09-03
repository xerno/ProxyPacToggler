using System;
using ProxyPacToggler.Ui;

namespace ProxyPacToggler.Tests
{
    internal static class ProblemLogTests
    {
        public static void Run()
        {
            RecordsAndClears();
            SuppressesRepeats();
            NotifiesAgainAfterAWhile();
            NotifiesAboutADifferentProblem();
        }

        private static void RecordsAndClears()
        {
            ProblemLog problems = new ProblemLog();
            DateTime now = new DateTime(2026, 9, 3, 10, 30, 0);

            Assert.Case("ProblemLog starts clean");
            Assert.False(problems.HasProblem, "no problem to begin with");
            Assert.Equal("", problems.ShortLine(), "and nothing to show in the menu");

            Assert.Case("ProblemLog records a problem");
            Assert.True(problems.Record("Could not switch the PAC script", "access denied", now),
                        "the first occurrence is worth notifying about");
            Assert.True(problems.HasProblem, "the problem is remembered");
            Assert.True(problems.ShortLine().Contains("Could not switch"), "the menu line names it");
            Assert.True(problems.ShortLine().Contains("10:30"), "and says when");
            Assert.True(problems.FullText().Contains("access denied"), "the detail is kept");

            Assert.Case("ProblemLog forgets once acknowledged");
            problems.Acknowledge();
            Assert.False(problems.HasProblem, "acknowledging clears it");
            Assert.Equal("", problems.ShortLine(), "nothing left in the menu");
        }

        private static void SuppressesRepeats()
        {
            ProblemLog problems = new ProblemLog();
            DateTime start = new DateTime(2026, 9, 3, 10, 0, 0);

            Assert.Case("the same problem does not notify every minute");
            Assert.True(problems.Record("Could not switch", "access denied", start),
                        "the first one notifies");

            int notifications = 0;
            for (int minute = 1; minute <= 9; minute++)
            {
                if (problems.Record("Could not switch", "access denied", start.AddMinutes(minute)))
                    notifications++;
            }
            Assert.Equal(0, notifications, "the same problem stays quiet for the next nine minutes");
            Assert.True(problems.HasProblem, "but it is still visible in the tray");
        }

        private static void NotifiesAgainAfterAWhile()
        {
            ProblemLog problems = new ProblemLog();
            DateTime start = new DateTime(2026, 9, 3, 10, 0, 0);

            Assert.Case("a lasting problem is repeated eventually");
            problems.Record("Could not switch", "access denied", start);
            Assert.False(problems.Record("Could not switch", "access denied", start.AddMinutes(5)),
                         "still quiet after five minutes");
            Assert.True(problems.Record("Could not switch", "access denied", start.AddMinutes(11)),
                        "notifies again after eleven");
        }

        private static void NotifiesAboutADifferentProblem()
        {
            ProblemLog problems = new ProblemLog();
            DateTime now = new DateTime(2026, 9, 3, 10, 0, 0);

            Assert.Case("a different problem notifies straight away");
            problems.Record("Could not switch", "access denied", now);
            Assert.True(problems.Record("Could not save settings", "disk full", now.AddSeconds(1)),
                        "a new problem is not suppressed");
            Assert.True(problems.FullText().Contains("disk full"), "and it replaces the old detail");
        }
    }
}
