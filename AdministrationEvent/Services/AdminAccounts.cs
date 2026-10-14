using System;
using System.Linq;
using Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
namespace AdministrationEvent.Services
{
    public class AdminAccounts
    {
        private readonly InvitationDbContext db;
        private readonly IConfiguration config;
        private readonly PasswordHasher<AdminUser> hasher = new();
        public AdminAccounts(InvitationDbContext db, IConfiguration config) { this.db = db; this.config = config; }
        public void BootstrapOwner()
        {
            if (db.Set<AdminUser>().Any(x => x.IsOwner)) return;
            var password = config["Admin:Password"];
            if (string.IsNullOrWhiteSpace(password)) return;
            var owner = new AdminUser { Username = "admin", NormalizedUsername = "ADMIN", DisplayName = "Administradora", IsOwner = true };
            owner.PasswordHash = hasher.HashPassword(owner, password);
            db.Add(owner);
            try { db.SaveChanges(); }
            catch (DbUpdateException e) when (e.InnerException is MySql.Data.MySqlClient.MySqlException m && m.Number == 1062) { db.Entry(owner).State = EntityState.Detached; }
        }
        public AdminUser Authenticate(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || password.Length > 128) return null;
            var user = db.Set<AdminUser>().SingleOrDefault(x => x.NormalizedUsername == username.Trim().ToUpperInvariant());
            if (user == null || !user.IsActive || user.LockedUntilUtc > DateTime.UtcNow) return null;
            var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed) {
                user.FailedAttempts++;
                if (user.FailedAttempts >= 5) { user.LockedUntilUtc = DateTime.UtcNow.AddMinutes(15); user.FailedAttempts = 0; }
                db.SaveChanges(); return null;
            }
            user.FailedAttempts = 0; user.LockedUntilUtc = null;
            if (result == PasswordVerificationResult.SuccessRehashNeeded) user.PasswordHash = hasher.HashPassword(user, password);
            db.SaveChanges(); return user;
        }
        public string Create(string username, string name, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || !System.Text.RegularExpressions.Regex.IsMatch(username, "^[a-zA-Z0-9._-]{3,40}$") || string.IsNullOrWhiteSpace(name) || name.Length > 100) return "Revisá el nombre y el usuario (3 a 40 letras, números, punto, guion o guion bajo).";
            if (password == null || password.Length < 12 || password.Length > 128) return "La contraseña debe tener entre 12 y 128 caracteres.";
            var normalized = username.Trim().ToUpperInvariant();
            if (db.Set<AdminUser>().Any(x => x.NormalizedUsername == normalized)) return "Ese usuario ya existe.";
            var user = new AdminUser { Username = username.Trim().ToLowerInvariant(), NormalizedUsername = normalized, DisplayName = name.Trim() };
            user.PasswordHash = hasher.HashPassword(user, password); db.Add(user);
            try { db.SaveChanges(); } catch (DbUpdateException e) when (e.InnerException is MySql.Data.MySqlClient.MySqlException m && m.Number == 1062) { db.Entry(user).State = EntityState.Detached; return "Ese usuario ya existe."; }
            return null;
        }
        public void SetPassword(AdminUser user, string password)
        {
            user.PasswordHash = hasher.HashPassword(user, password); user.SecurityStamp = Guid.NewGuid().ToString();
            user.FailedAttempts = 0; user.LockedUntilUtc = null; db.SaveChanges();
        }
    }
}
