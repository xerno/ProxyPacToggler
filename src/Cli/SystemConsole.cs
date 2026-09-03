using System;
using System.IO;
using System.Text;
using ProxyPacToggler.Windows;

namespace ProxyPacToggler.Cli
{
    internal sealed class SystemConsole : IConsole
    {
        // A GUI binary starts with no console and Console.Out bound to nothing.
        public static SystemConsole AttachedToCaller()
        {
            try
            {
                Native.AttachConsole(Native.AttachParentProcess);
                Console.SetOut(AutoFlushing(Console.OpenStandardOutput()));
                Console.SetError(AutoFlushing(Console.OpenStandardError()));
            }
            catch (IOException)
            {
                // No console and no pipe: output goes nowhere, which is not fatal.
            }
            return new SystemConsole();
        }

        private static StreamWriter AutoFlushing(Stream stream)
        {
            StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false));
            writer.AutoFlush = true;
            return writer;
        }

        public void Write(string line)
        {
            Console.WriteLine(line);
        }

        public void WriteRaw(string text)
        {
            Console.Write(text);
        }

        public void WriteError(string line)
        {
            Console.Error.WriteLine(line);
        }
    }
}
