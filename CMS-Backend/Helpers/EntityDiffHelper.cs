using CMS_Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CMS_Backend.Helpers
{
    public static class EntityDiffHelper
    {
        // Fields you never want to log — audit columns, nav properties,
        // and anything sensitive (payment data, password hashes, etc.).
        // Extend this per-entity if some tables need extra exclusions.
        private static readonly HashSet<string> DefaultIgnoredFields = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "RowVersion", "CreatedDate", "ModifiedDate", "PasswordHash",
            "CardNumber", "CVV"
        };

        /// <summary>
        /// Builds change rows from a tracked EntityEntry that EF Core is
        /// about to save as Modified. Uses EF's own OriginalValues /
        /// CurrentValues — no reflection, no reloading an untracked copy.
        /// </summary>
        public static List<ApiDataChangeLog> GetChanges(
            EntityEntry entry,
            long? logId,
            IEnumerable<string> extraIgnoredFields = null)
        {
            var ignored = extraIgnoredFields == null
                ? DefaultIgnoredFields
                : new HashSet<string>(DefaultIgnoredFields.Concat(extraIgnoredFields), StringComparer.OrdinalIgnoreCase);

            var changes = new List<ApiDataChangeLog>();
            var tableName = entry.Metadata.GetTableName();
            var pkValue = GetPrimaryKeyValue(entry);

            foreach (var property in entry.Properties)
            {
                var propName = property.Metadata.Name;
                if (ignored.Contains(propName)) continue;
                if (!property.IsModified) continue; // EF already knows what changed

                var oldVal = property.OriginalValue?.ToString();
                var newVal = property.CurrentValue?.ToString();
                if (oldVal == newVal) continue;

                changes.Add(new ApiDataChangeLog
                {
                    LogId = (long)logId,
                    TableName = tableName,
                    PrimaryKeyValue = pkValue,
                    FieldName = propName,
                    OldValue = oldVal,
                    NewValue = newVal,
                    ChangedDate = DateTime.UtcNow
                });
            }

            return changes;
        }

        private static string GetPrimaryKeyValue(EntityEntry entry)
        {
            var key = entry.Metadata.FindPrimaryKey();
            if (key == null) return null;

            var values = key.Properties
                .Select(p => entry.Property(p.Name).CurrentValue?.ToString());

            return string.Join(",", values);
        }

        /// <summary>
        /// For newly inserted rows (EntityState.Added): a single marker row
        /// instead of a per-field diff, since there's no "old value" to
        /// compare against. NewValue holds the new row's primary key.
        /// </summary>
        public static ApiDataChangeLog GetCreatedMarker(EntityEntry entry, long? logId)
        {
            return new ApiDataChangeLog
            {
                LogId = (long)logId,
                TableName = entry.Metadata.GetTableName(),
                PrimaryKeyValue = GetPrimaryKeyValue(entry),
                FieldName = "(created)",
                OldValue = null,
                NewValue = GetPrimaryKeyValue(entry),
                ChangedDate = DateTime.UtcNow
            };
        }
    }
}
