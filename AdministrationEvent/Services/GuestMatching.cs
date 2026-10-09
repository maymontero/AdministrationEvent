using System.Globalization;
using System.Linq;
using System.Text;
namespace AdministrationEvent.Services
{
    public static class GuestMatching
    {
        public static string NormalizeName(string name)
        {
            var characters = (name ?? "").Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
            return string.Join(" ", new string(characters.ToArray()).ToUpperInvariant().Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries));
        }
        public static long? FindUnique(InvitationDbContext db, long invitationId, string name)
        {
            var normalized = NormalizeName(name);
            if (normalized.Length == 0) return null;
            var matches = db.Guests.Where(x => x.InvitationId == invitationId && !x.IsArchived && !x.IsWaitlisted).ToList()
                .Where(x => NormalizeName(x.Name) == normalized).Take(2).ToList();
            return matches.Count == 1 ? matches[0].Id : null;
        }
    }
}
