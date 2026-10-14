using System;
namespace Application
{
    public class AdminUser
    {
        public long Id { get; set; }
        public string Username { get; set; } = "";
        public string NormalizedUsername { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public bool IsOwner { get; set; }
        public bool IsActive { get; set; } = true;
        public int FailedAttempts { get; set; }
        public DateTime? LockedUntilUtc { get; set; }
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
    }
}
