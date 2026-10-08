using System;

namespace ExeBuilder.ViewModels
{
    public enum LogKind
    {
        Normal,
        Command,
        Warning,
        Error,
        Success
    }

    public sealed class LogLine
    {
        public LogLine(string text, LogKind kind)
        {
            Text = text;
            Kind = kind;
        }

        public string Text { get; private set; }

        public LogKind Kind { get; private set; }

        /// <summary>Classifica le righe di MSBuild/dotnet (": error XX1234:" / ": warning XX1234:").</summary>
        public static LogLine FromBuildOutput(string text)
        {
            if (text.StartsWith("> ", StringComparison.Ordinal))
            {
                return new LogLine(text, LogKind.Command);
            }

            if (text.IndexOf(": error ", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("error MSB", StringComparison.Ordinal) >= 0)
            {
                return new LogLine(text, LogKind.Error);
            }

            if (text.IndexOf(": warning ", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new LogLine(text, LogKind.Warning);
            }

            return new LogLine(text, LogKind.Normal);
        }
    }
}
