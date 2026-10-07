namespace Catalog.Domain.Common
{
    internal static class Guard
    {
        public static string NotNullOrWhiteSpace(string value, string name, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DomainException(name + " è obbligatorio.");
            }

            value = value.Trim();
            if (value.Length > maxLength)
            {
                throw new DomainException(name + " non può superare " + maxLength + " caratteri.");
            }

            return value;
        }
    }
}
