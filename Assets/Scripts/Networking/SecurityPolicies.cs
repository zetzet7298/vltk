using System;
using System.Text.RegularExpressions;

namespace VLTK.Production.Networking
{
    public static class RealtimeEndpointPolicy
    {
        public static bool IsProductionWss(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                return false;
            Uri uri;
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out uri))
                return false;
            return string.Equals(uri.Scheme, "wss", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(uri.Host);
        }
    }

    public static class DigestPolicy
    {
        private static readonly Regex Sha256Hex = new Regex("\\A[0-9a-fA-F]{64}\\z", RegexOptions.Compiled);

        public static bool IsSha256Hex(string value)
        {
            return !string.IsNullOrEmpty(value) && Sha256Hex.IsMatch(value);
        }
    }

    public static class SecretRedactor
    {
        private static readonly string[] SecretKeys =
        {
            "password", "token", "ticket", "secret", "authorization", "admission", "refresh", "access"
        };

        public static string RedactKeyValue(string key, string value)
        {
            if (IsSecretKey(key))
                return "[REDACTED]";
            return value ?? string.Empty;
        }

        public static string RedactMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return string.Empty;

            string output = message;
            for (int i = 0; i < SecretKeys.Length; i++)
                output = Regex.Replace(output, "(?i)([A-Za-z0-9_]*" + SecretKeys[i] + "[A-Za-z0-9_]*\\s*[=:]\\s*)[^\\s,;]+", "$1[REDACTED]");
            return output;
        }

        private static bool IsSecretKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;
            for (int i = 0; i < SecretKeys.Length; i++)
                if (key.IndexOf(SecretKeys[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }
    }
}
