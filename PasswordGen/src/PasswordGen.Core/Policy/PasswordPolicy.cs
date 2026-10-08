using System.Collections.Generic;

namespace PasswordGen.Core.Policy
{
    /// <summary>
    /// Regole che la password deve rispettare (tipiche delle policy aziendali).
    /// I valori predefiniti sono quelli più comuni: almeno 10 caratteri con maiuscole, minuscole, numeri e speciali.
    /// </summary>
    public sealed class PasswordPolicy
    {
        public const int MinAllowedLength = 6;
        public const int MaxAllowedLength = 64;
        public const string DefaultSpecials = "!@#$%&*?+=-_.";

        public int MinLength { get; set; } = 10;
        public bool RequireUpper { get; set; } = true;
        public bool RequireLower { get; set; } = true;
        public bool RequireDigit { get; set; } = true;
        public bool RequireSpecial { get; set; } = true;

        /// <summary>Evita i caratteri facili da confondere (0/O, 1/l/I) nelle cifre e nei caratteri casuali.</summary>
        public bool AvoidAmbiguous { get; set; } = true;

        /// <summary>Massimo numero di caratteri uguali consecutivi (0 = nessun limite).</summary>
        public int MaxRepeatRun { get; set; } = 2;

        /// <summary>Caratteri speciali ammessi.</summary>
        public string Specials { get; set; } = DefaultSpecials;

        public PasswordPolicy Clone()
        {
            return (PasswordPolicy)MemberwiseClone();
        }

        /// <summary>Riporta i valori entro limiti sensati.</summary>
        public PasswordPolicy Normalized()
        {
            var copy = Clone();
            copy.MinLength = System.Math.Max(MinAllowedLength, System.Math.Min(MaxAllowedLength, MinLength));
            copy.MaxRepeatRun = System.Math.Max(0, MaxRepeatRun);
            if (string.IsNullOrEmpty(copy.Specials))
            {
                copy.Specials = DefaultSpecials;
            }

            return copy;
        }

        /// <summary>Elenco delle regole violate (vuoto se la password è valida).</summary>
        public IReadOnlyList<string> Validate(string password)
        {
            var problems = new List<string>();
            password = password ?? string.Empty;

            if (password.Length < MinLength)
            {
                problems.Add("almeno " + MinLength + " caratteri");
            }

            if (RequireUpper && !Contains(password, char.IsUpper))
            {
                problems.Add("una lettera maiuscola");
            }

            if (RequireLower && !Contains(password, char.IsLower))
            {
                problems.Add("una lettera minuscola");
            }

            if (RequireDigit && !Contains(password, char.IsDigit))
            {
                problems.Add("una cifra");
            }

            if (RequireSpecial && !Contains(password, c => Specials.IndexOf(c) >= 0))
            {
                problems.Add("un carattere speciale");
            }

            if (MaxRepeatRun > 0 && LongestRun(password) > MaxRepeatRun)
            {
                problems.Add("non più di " + MaxRepeatRun + " caratteri uguali consecutivi");
            }

            return problems;
        }

        public bool IsValid(string password)
        {
            return Validate(password).Count == 0;
        }

        private static bool Contains(string text, System.Func<char, bool> predicate)
        {
            foreach (var c in text)
            {
                if (predicate(c))
                {
                    return true;
                }
            }

            return false;
        }

        private static int LongestRun(string text)
        {
            var best = 0;
            var run = 0;
            for (var i = 0; i < text.Length; i++)
            {
                run = i > 0 && text[i] == text[i - 1] ? run + 1 : 1;
                if (run > best)
                {
                    best = run;
                }
            }

            return best;
        }
    }
}
