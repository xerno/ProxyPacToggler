using System;
using System.Globalization;

namespace ProxyPacToggler.Ui
{
    // A toast is fire-and-forget, so the last problem is kept for the icon and menu.
    internal sealed class ProblemLog
    {
        // Without this, a stuck failure would notify on every safety tick.
        private static readonly TimeSpan RepeatNotificationAfter = TimeSpan.FromMinutes(10);

        private DateTime notifiedAt;
        private string notifiedText;

        public bool HasProblem { get { return Summary != null; } }

        public string Summary { get; private set; }

        public string Detail { get; private set; }

        public DateTime OccurredAt { get; private set; }

        // Returns true when this problem is worth a notification right now.
        public bool Record(string summary, string detail, DateTime now)
        {
            Summary = summary;
            Detail = detail;
            OccurredAt = now;

            string text = summary + detail;
            bool stale = now - notifiedAt > RepeatNotificationAfter;
            if (notifiedText == text && !stale) return false;

            notifiedText = text;
            notifiedAt = now;
            return true;
        }

        public void Acknowledge()
        {
            Summary = null;
            Detail = null;
            notifiedText = null;
        }

        public string ShortLine()
        {
            return HasProblem
                ? "! " + Summary + " (" + OccurredAt.ToString("HH:mm", CultureInfo.CurrentCulture) + ")"
                : "";
        }

        public string FullText()
        {
            return HasProblem
                ? Summary + "." + Environment.NewLine + Environment.NewLine + Detail
                : "";
        }
    }
}
