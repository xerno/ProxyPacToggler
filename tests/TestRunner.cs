using System;
using System.IO;

namespace ProxyPacToggler.Tests
{
    internal static class TestRunner
    {
        public static int Main()
        {
            Console.WriteLine("ProxyPacToggler tests");
            Console.WriteLine();

            string scratch = Path.Combine(Path.GetTempPath(),
                "ProxyPacToggler.Tests." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(scratch);
            Console.WriteLine("scratch directory: " + scratch);
            Console.WriteLine();

            try
            {
                PacUrlValidatorTests.Run();
                PacDecisionTests.Run();
                VpnMatchingTests.Run();
                ProxyControllerTests.Run();
                ExternalChangeTests.Run();
                CommandLineTests.Run();
                ProblemLogTests.Run();
                TrayTooltipTests.Run();
                TunnelReadinessTests.Run();
                WindowsIntegrationTests.Run(scratch);
            }
            catch (Exception ex)
            {
                Assert.Failures.Add("unhandled exception during the run: " + ex);
            }
            finally
            {
                TryDelete(scratch);
            }

            return ReportResults();
        }

        private static void TryDelete(string directory)
        {
            try
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
            catch (IOException)
            {
                Console.WriteLine("note: could not remove " + directory);
            }
        }

        private static int ReportResults()
        {
            Console.WriteLine();
            if (Assert.Failures.Count == 0)
            {
                Console.WriteLine("All " + Assert.Passed + " assertions passed.");
                return 0;
            }

            Console.WriteLine("==> " + Assert.Failures.Count + " failure(s), "
                              + Assert.Passed + " assertion(s) passed:");
            foreach (string failure in Assert.Failures) Console.WriteLine("  x " + failure);
            return 1;
        }
    }
}
