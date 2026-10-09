using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace AdministrationEvent.Models
{
    public class GuestEditor
    {
        public long Id { get; set; }
        public bool IsWaitlisted { get; set; }
        [Required(ErrorMessage = "Ingresá el nombre del invitado o familia."), StringLength(120)] public string Name { get; set; }
        [StringLength(200)] public string Contact { get; set; }
        [Range(1, 10, ErrorMessage = "Indicá entre 1 y 10 personas.")] public int InvitedCount { get; set; } = 1;
        [StringLength(500)] public string Notes { get; set; }
    }
    public class GuestAdministration
    {
        public InvitationConfiguration Invitation { get; set; }
        public List<Application.Guest> Guests { get; set; } = new();
        public List<Application.GuestResponse> Responses { get; set; } = new();
        public GuestEditor Editor { get; set; } = new();
        public string Search { get; set; } = "";
        public string Status { get; set; } = "active";
    }
}
