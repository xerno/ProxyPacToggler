using System.Collections.Generic;

namespace ProxyPacToggler.Tests
{
    internal static class Assert
    {
        public static int Passed;
        public static readonly List<string> Failures = new List<string>();

        private static string current = "";

        public static void Case(string name)
        {
            current = name;
        }

        public static void True(bool condition, string what)
        {
            if (condition) Passed++;
            else Failures.Add(current + ": " + what + " (expected true)");
        }

        public static void False(bool condition, string what)
        {
            True(!condition, what);
        }

        public static void Equal(string expected, string actual, string what)
        {
            if (string.Equals(expected, actual)) Passed++;
            else Failures.Add(current + ": " + what
                              + "\n      expected: \"" + expected + "\""
                              + "\n      actual:   \"" + actual + "\"");
        }

        public static void Equal(int expected, int actual, string what)
        {
            if (expected == actual) Passed++;
            else Failures.Add(current + ": " + what
                              + " (expected " + expected + ", got " + actual + ")");
        }
    }
}
