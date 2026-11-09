using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CMS.Infrastructure.Helpers
{
    public static class SensitiveDataRedactor
    {
        private const string MaskValue = "***REDACTED***";

        public static readonly HashSet<string> DefaultSensitiveFields = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "Password", "NewPassword", "OldPassword", "ConfirmPassword",
            "Token", "AccessToken", "RefreshToken",
            "CardNumber", "CVV", "Cvv", "Secret"
        };

        /// <summary>
        /// Masks the value of any property whose name matches
        /// DefaultSensitiveFields (or the extra fields passed in),
        /// anywhere in the JSON, including nested objects and arrays.
        /// Returns the original string unchanged if it isn't valid JSON
        /// (some endpoints log plain text/empty bodies).
        /// </summary>
        public static string Mask(string json, IEnumerable<string> extraFields = null)
        {
            if (string.IsNullOrWhiteSpace(json)) return json;

            var fields = extraFields == null
                ? DefaultSensitiveFields
                : new HashSet<string>(DefaultSensitiveFields.Concat(extraFields), StringComparer.OrdinalIgnoreCase);

            try
            {
                var node = JsonNode.Parse(json);
                MaskNode(node, fields);
                return node.ToJsonString();
            }
            catch (JsonException)
            {
                // Not JSON (plain text, empty, malformed) — leave as-is.
                return json;
            }
        }

        // ---- Cheap masking for log storage -------------------------------------
        // Works on the already-truncated text, so cost is bounded, original
        // formatting/Arabic text is kept, and it still masks when truncation
        // has cut the JSON in half (which JsonNode.Parse could not handle).
        private static readonly string FieldAlternation =
            string.Join("|", DefaultSensitiveFields.Select(Regex.Escape));

        private static readonly Regex JsonFieldRegex = new(
            "(\"(?:" + FieldAlternation + ")\"\\s*:\\s*)(?:\"(?:[^\"\\\\]|\\\\.)*\"?|[^,}\\]\\s]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(200));

        private static readonly Regex FormFieldRegex = new(
            "(?<=^|&)((?:" + FieldAlternation + ")=)[^&]*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(200));

        /// <summary>Truncates to <paramref name="maxChars"/> first, then masks sensitive fields.</summary>
        public static string MaskForLog(string? text, int maxChars)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length > maxChars) text = text[..maxChars];

            try
            {
                text = JsonFieldRegex.Replace(text, "$1\"" + MaskValue + "\"");
                text = FormFieldRegex.Replace(text, "$1" + MaskValue);
                return text;
            }
            catch (RegexMatchTimeoutException)
            {
                return "[body omitted: could not be safely masked]";
            }
        }

        private static void MaskNode(JsonNode node, HashSet<string> fields)
        {
            if (node is JsonObject obj)
            {
                foreach (var key in obj.Select(kv => kv.Key).ToList())
                {
                    if (fields.Contains(key))
                    {
                        obj[key] = MaskValue;
                    }
                    else
                    {
                        MaskNode(obj[key], fields);
                    }
                }
            }
            else if (node is JsonArray arr)
            {
                foreach (var item in arr)
                {
                    MaskNode(item, fields);
                }
            }
        }
    }
}
