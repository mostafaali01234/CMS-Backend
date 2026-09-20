using System.Text.Json;
using System.Text.Json.Nodes;

namespace CMS_Backend.Helpers
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
