using System;
using System.IO;
using System.Security;
using System.Text;
using ProxyPacToggler.Core;

namespace ProxyPacToggler.Windows
{
    // Hand-editable, so values read back get the same scrutiny as typed input.
    internal sealed class IniSettingsStore : ISettingsStore
    {
        private const string FileName = "config.ini";
        private const long MaxFileBytes = 64 * 1024;
        private const int MaxLineLength = PacUrlValidator.MaxLength + 64;

        private readonly string directory;
        private readonly string path;
        private readonly ILog log;

        public IniSettingsStore(string directory, ILog log)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentNullException("directory");
            if (log == null) throw new ArgumentNullException("log");

            this.directory = directory;
            this.log = log;
            path = Path.Combine(directory, FileName);
        }

        public string Location { get { return path; } }

        public Settings Load()
        {
            Settings settings = new Settings();
            try
            {
                if (!File.Exists(Location)) return settings;

                if (new FileInfo(Location).Length > MaxFileBytes)
                {
                    log.Write("settings ignored: larger than " + MaxFileBytes + " bytes");
                    return settings;
                }

                foreach (string line in File.ReadAllLines(Location, Encoding.UTF8))
                {
                    if (line.Length <= MaxLineLength) ApplyLine(settings, line.Trim());
                }
            }
            catch (IOException ex) { log.Write("settings could not be read: " + ex.Message); }
            catch (UnauthorizedAccessException ex) { log.Write("settings could not be read: " + ex.Message); }
            catch (SecurityException ex) { log.Write("settings could not be read: " + ex.Message); }

            return settings;
        }

        private void ApplyLine(Settings settings, string line)
        {
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) return;

            int separator = line.IndexOf('=');
            if (separator <= 0) return;

            string key = line.Substring(0, separator).Trim();
            string value = line.Substring(separator + 1).Trim();

            if (Is(key, "PacUrl")) ApplyPacUrl(settings, value);
            else if (Is(key, "Auto")) settings.FollowVpn = value != "0";
            else if (Is(key, "Invert")) settings.Inverted = value != "0";
            else if (Is(key, "AdapterMatch") && value.Length > 0) settings.AdapterMatch = value;
        }

        private void ApplyPacUrl(Settings settings, string value)
        {
            if (value.Length == 0) return;

            Result validated = PacUrlValidator.Validate(value);
            if (validated.Succeeded) settings.PacUrl = value;
            else log.Write("stored PacUrl ignored, " + validated.Error);
        }

        private static bool Is(string key, string name)
        {
            return string.Equals(key, name, StringComparison.OrdinalIgnoreCase);
        }

        // Swapped in via a temporary file; a truncated one would read as no PAC URL.
        public Result Save(Settings settings)
        {
            string temporary = Location + ".tmp";
            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(temporary, Render(settings), Encoding.UTF8);

                if (File.Exists(Location)) File.Replace(temporary, Location, null);
                else File.Move(temporary, Location);

                return Result.Ok();
            }
            catch (IOException ex) { return Failure(ex, temporary); }
            catch (UnauthorizedAccessException ex) { return Failure(ex, temporary); }
            catch (SecurityException ex) { return Failure(ex, temporary); }
        }

        private static string Render(Settings settings)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("# ProxyPacToggler configuration");
            text.AppendLine("PacUrl=" + settings.PacUrl);
            text.AppendLine("Auto=" + (settings.FollowVpn ? "1" : "0"));
            text.AppendLine("Invert=" + (settings.Inverted ? "1" : "0"));
            text.AppendLine("AdapterMatch=" + settings.AdapterMatch);
            return text.ToString();
        }

        private Result Failure(Exception ex, string temporary)
        {
            try
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            string detail = "settings could not be saved: " + ex.Message;
            log.Write(detail);
            return Result.Fail(detail);
        }
    }
}
