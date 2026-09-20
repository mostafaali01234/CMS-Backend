using CMS_Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections.Generic;

namespace CMS_Backend.Helpers
{
    public class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Entities that were Added in this save, captured in SavingChangesAsync
        // so we can log a "created" marker in SavedChangesAsync — by then
        // identity-generated primary keys have been populated by the DB.
        private readonly List<EntityEntry> _pendingAdded = new List<EntityEntry>();
        // Tables that should never be audited
        private static readonly HashSet<string> ExcludedTables = new(StringComparer.OrdinalIgnoreCase)
        {
            "AspNetRoles",
             "AspNetUserRoles",
             "AspNetRoleClaims",
             "AspNetUserClaims",
             "AspNetUserLogins",
             "AspNetUsers",
        };

        public AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private static bool ShouldAudit(EntityEntry e)
        {
            var type = e.Entity.GetType();

            if (type == typeof(ApiDataChangeLog)
                || type == typeof(RefreshToken)
                || type == typeof(ApiActivityLog))
                return false;

            // Matches IdentityRole and any subclass (e.g. ApplicationRole)
            if (e.Entity is IdentityRole)
                return false;

            // Or exclude by table name
            if (ExcludedTables.Contains(e.Metadata.GetTableName() ?? string.Empty))
                return false;

            return true;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (context == null) return result;

            var logId = _httpContextAccessor.HttpContext?.Items["CurrentLogId"] as long?;

            var entries = context.ChangeTracker.Entries().ToList();

            // Modified rows: full field-level diff, logged now (old/new values
            // are both already known before the update executes).
            var changeRows = entries
                .Where(e => e.State == EntityState.Modified)
                .Where(ShouldAudit)
                //.Where(e => e.Entity.GetType() != typeof(ApiDataChangeLog)
                //            && e.Entity.GetType() != typeof(RefreshToken)
                //            && e.Entity.GetType() != typeof(ApiActivityLog))
                .SelectMany(e => EntityDiffHelper.GetChanges(e, logId))
                .ToList();

            if (changeRows.Count > 0)
            {
                context.Set<ApiDataChangeLog>().AddRange(changeRows);
            }

            // Added rows: remember them; their primary key isn't known yet
            // if it's DB-generated (identity), so the marker row is built
            // after the save completes, in SavedChangesAsync.
           _pendingAdded.Clear();
           _pendingAdded.AddRange(entries.Where(e =>
                e.State == EntityState.Added
                && ShouldAudit(e)
                //&& e.Entity.GetType() != typeof(ApiDataChangeLog)
                //&& e.Entity.GetType() != typeof(RefreshToken)
                //&& e.Entity.GetType() != typeof(ApiActivityLog)
                ));

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            if (_pendingAdded.Count > 0 && eventData.Context != null)
            {
                var logId = _httpContextAccessor.HttpContext?.Items["CurrentLogId"] as long?;
                var markers = _pendingAdded
                    .Select(e => EntityDiffHelper.GetCreatedMarker(e, logId))
                    .ToList();

                eventData.Context.Set<ApiDataChangeLog>().AddRange(markers);
                _pendingAdded.Clear();

                // A second, small save just for the marker rows — their PKs
                // are only known now that the first save has completed.
                await eventData.Context.SaveChangesAsync(cancellationToken);
            }

            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }
    }
}
