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
        [Required, RegularExpression("^[a-zA-Z0-9._-]{3,40}$")] public string Username { get; set; } = "chea";
        [Required, StringLength(100)] public string DisplayName { get; set; } = "Chea";
        [Required, StringLength(128, MinimumLength = 12)] public string Password { get; set; } = "";
    }
    public class AccountPage
    {
        public List<AdminUser> Users { get; set; } = new();
        public CreateAccount Editor { get; set; } = new();
    }
    public class ChangeAccountPassword
    {
        [Required, StringLength(128)] public string CurrentPassword { get; set; } = "";
        [Required, StringLength(128, MinimumLength = 12)] public string NewPassword { get; set; } = "";
        [Compare(nameof(NewPassword))] public string ConfirmPassword { get; set; } = "";
    }
}
