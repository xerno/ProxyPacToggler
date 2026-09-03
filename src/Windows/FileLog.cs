using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using ProxyPacToggler.Core;

namespace ProxyPacToggler.Windows
{
    internal sealed class FileLog : ILog
    {
        private const string FileName = "log.txt";
        private const long MaxBytes = 200 * 1024;
        private const int LinesKeptOnTrim = 300;
        private const int WriteAttempts = 5;
        private const int RetryDelayMs = 40;

        private readonly object gate = new object();
        private readonly string directory;
        private readonly string path;

        public FileLog(string directory)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentNullException("directory");
            this.directory = directory;
            path = Path.Combine(directory, FileName);
        }

        public string Location { get { return path; } }

        public void Write(string message)
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)
                          + "  " + message + Environment.NewLine;

            lock (gate)
            {
                // The tray and a command-line run share this file, so a lock is expected.
                for (int attempt = 0; attempt < WriteAttempts; attempt++)
                {
                    if (TryAppend(line)) return;
                    Thread.Sleep(RetryDelayMs);
                }
            }
        }

        private bool TryAppend(string line)
        {
            try
            {
                Directory.CreateDirectory(directory);
                using (FileStream file = new FileStream(Location, FileMode.Append, FileAccess.Write,
                                                        FileShare.ReadWrite))
                using (StreamWriter writer = new StreamWriter(file, Encoding.UTF8))
                {
                    writer.Write(line);
                }
                TrimIfOversized();
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        private void TrimIfOversized()
        {
            try
            {
                if (new FileInfo(Location).Length <= MaxBytes) return;

                string[] lines = File.ReadAllLines(Location, Encoding.UTF8);
                if (lines.Length <= LinesKeptOnTrim) return;

                string[] tail = new string[LinesKeptOnTrim];
                Array.Copy(lines, lines.Length - LinesKeptOnTrim, tail, 0, LinesKeptOnTrim);
                File.WriteAllLines(Location, tail, Encoding.UTF8);
            }
            // Trimming is housekeeping; failing to trim must not lose the log line.
            catch (IOException) { }
        }
    }
}
