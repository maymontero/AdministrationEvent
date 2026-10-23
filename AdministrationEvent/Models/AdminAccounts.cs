using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Application;
namespace AdministrationEvent.Models
{
    public class AccountLogin
    {
        [Required, StringLength(80)] public string Username { get; set; } = "";
        [Required, StringLength(128)] public string Password { get; set; } = "";
    }
    public class CreateAccount
    {
        [Required, RegularExpression("^[a-zA-Z0-9._-]{3,40}$")] public string Username { get; set; } = "";
        [Required, StringLength(100)] public string DisplayName { get; set; } = "";
        [Required, StringLength(128, MinimumLength = 12)] public string Password { get; set; } = "";
    }
    public class AccountPage
    {
        public List<AdminUser> Users { get; set; } = new();
        public Dictionary<long, int> InvitationCounts { get; set; } = new();
        public string Search { get; set; } = "";
        public string Status { get; set; } = "all";
        public int Page { get; set; } = 1;
        public int PageCount { get; set; } = 1;
        public int Total { get; set; }
        public int PageSize { get; set; } = 20;
    }
    public class AccountPermissionsPage
    {
        public AdminUser User { get; set; }
        public List<InvitationChoice> Invitations { get; set; } = new();
        public List<InvitationAccess> Accesses { get; set; } = new();
        public string Search { get; set; } = "";
        public int Page { get; set; } = 1;
        public int PageCount { get; set; } = 1;
        public int Total { get; set; }
    }
    public class AccountPasswordReset
    {
        public long Id { get; set; }
        public string DisplayName { get; set; }
        public string Username { get; set; }
    }
    public class InvitationChoice { public long Id { get; set; } public string Title { get; set; } public bool IsPublished { get; set; } }
    public class ChangeAccountPassword
    {
        [Required, StringLength(128)] public string CurrentPassword { get; set; } = "";
        [Required, StringLength(128, MinimumLength = 12)] public string NewPassword { get; set; } = "";
        [Compare(nameof(NewPassword))] public string ConfirmPassword { get; set; } = "";
    }
}
