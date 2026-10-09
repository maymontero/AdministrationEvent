using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace AdministrationEvent.Models
{
    public class RsvpForm
    {
        [Required(ErrorMessage = "Ingresá tu nombre."), StringLength(120)] public string Name { get; set; }
        [StringLength(200)] public string Contact { get; set; }
        [Required(ErrorMessage = "Indicá si vas a asistir.")] public bool? Attending { get; set; }
        [Range(1, 10, ErrorMessage = "Indicá entre 1 y 10 personas.")] public int GuestCount { get; set; } = 1;
        [StringLength(500)] public string Notes { get; set; }
    }
    public class GuestResponseList
    {
        public InvitationConfiguration Invitation { get; set; }
        public List<Application.GuestResponse> Responses { get; set; }
    }
}
