using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExeBuilder.Core.Execution
{
    /// <summary>
    /// Quoting degli argomenti secondo le regole di CommandLineToArgvW / MSVCRT
    /// (backslash prima delle virgolette e in coda vanno raddoppiati).
    /// </summary>
    public static class CommandLine
    {
        public static string Join(IEnumerable<string> arguments)
        {
            return string.Join(" ", arguments.Select(Quote));
        }

        public static string Quote(string argument)
        {
            if (argument == null)
            {
                return "\"\"";
            }

            if (argument.Length > 0 && argument.IndexOfAny(new[] { ' ', '\t', '\n', '\v', '"' }) < 0)
            {
                return argument;
            }

            var sb = new StringBuilder();
            sb.Append('"');
            for (var i = 0; i < argument.Length; i++)
            {
                var backslashes = 0;
                while (i < argument.Length && argument[i] == '\\')
                {
                    backslashes++;
                    i++;
                }

                if (i == argument.Length)
                {
                    sb.Append('\\', backslashes * 2);
                    break;
                }

                if (argument[i] == '"')
                {
                    sb.Append('\\', (backslashes * 2) + 1);
                    sb.Append('"');
                }
                else
                {
                    sb.Append('\\', backslashes);
                    sb.Append(argument[i]);
                }
            }

            sb.Append('"');
            return sb.ToString();
        }
    }
}
