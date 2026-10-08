namespace PasswordGen.Core.Generation
{
    public enum StrengthLevel
    {
        Weak = 0,
        Fair = 1,
        Good = 2,
        Excellent = 3
    }

    /// <summary>Giudizio sull'entropia (in bit) di una password generata a caso.</summary>
    public static class PasswordStrength
    {
        public static StrengthLevel Classify(double bits)
        {
            if (bits < 40)
            {
                return StrengthLevel.Weak;
            }

            if (bits < 60)
            {
                return StrengthLevel.Fair;
            }

            return bits < 80 ? StrengthLevel.Good : StrengthLevel.Excellent;
        }

        public static string Describe(StrengthLevel level)
        {
            switch (level)
            {
                case StrengthLevel.Weak: return "Debole";
                case StrengthLevel.Fair: return "Accettabile";
                case StrengthLevel.Good: return "Buona";
                default: return "Ottima";
            }
        }
    }
}
