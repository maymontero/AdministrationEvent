using System;
using System.Linq;
using System.Security.Claims;
namespace AdministrationEvent.Services
{
    public class InvitationPermissions
    {
        private readonly InvitationDbContext db;
        public InvitationPermissions(InvitationDbContext db) { this.db = db; }
        public bool CanView(ClaimsPrincipal principal, Guid invitationId, DateTime utcNow)
        {
            if (!long.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return false;
            var user = db.AdminUsers.SingleOrDefault(x => x.Id == userId && x.IsActive);
            if (user == null) return false;
            if (user.IsOwner) return true;
            return db.InvitationAccesses.Any(a => a.UserId == userId &&
                db.Invitations.Any(i => i.Id == a.InvitationId && i.PublicId == invitationId && i.IsPublished) &&
                (a.AvailableFromUtc == null || a.AvailableFromUtc <= utcNow));
        }
    }
}
