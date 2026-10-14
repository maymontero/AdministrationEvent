using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace AdministrationEvent.Models
{
    public class RsvpForm : IValidatableObject
    {
        [Required(ErrorMessage = "Ingresá tu nombre."), StringLength(120)] public string Name { get; set; }
        [StringLength(200)] public string Contact { get; set; }
        [Required(ErrorMessage = "Indicá si vas a asistir.")] public bool? Attending { get; set; }
        [Range(1, 10, ErrorMessage = "Indicá entre 1 y 10 personas.")] public int? GuestCount { get; set; }
        [StringLength(500)] public string DietaryRestrictions { get; set; }
        [StringLength(200)] public string SongRequest { get; set; }
        [StringLength(500)] public string Notes { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Attending == true && !GuestCount.HasValue)
                yield return new ValidationResult("Indicá cuántas personas asistirán en total, incluyéndote.", new[] { nameof(GuestCount) });
        }
    }
    public class GuestResponseList
    {
        public InvitationConfiguration Invitation { get; set; }
        public List<Application.GuestResponse> Responses { get; set; }
    }
}
