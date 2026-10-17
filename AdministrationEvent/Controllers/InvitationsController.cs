using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AdministrationEvent.Services;
using AdministrationEvent.Models;
using Application;
namespace AdministrationEvent.Controllers
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class InvitationsController : Controller
    {
        private readonly InvitationStore store;
        private readonly InvitationDbContext db;
        public InvitationsController(InvitationStore store, InvitationDbContext db) { this.store = store; this.db = db; }

        [HttpGet("invitacion/{slug}")]
        public IActionResult Show(string slug)
        {
            var invitation = store.FindPublished(slug);
            if (invitation == null) return NotFound();
            return View(invitation);
        }


        [HttpPost("invitacion/{slug}/asistencia"), ValidateAntiForgeryToken]
        public IActionResult Respond(string slug, RsvpForm form)
        {
            var invitation = store.FindPublished(slug);
            if (invitation == null || !invitation.RsvpEnabled) return NotFound();
            return HandleResponse(invitation, form, false);
        }

        [Authorize, HttpPost("admin/preview/{id:guid}/asistencia"), ValidateAntiForgeryToken]
        public IActionResult PreviewResponse(Guid id, RsvpForm form)
        {
            var invitation = store.Find(id);
            if (invitation == null || !invitation.RsvpEnabled) return NotFound();
            var denied = CheckAccess(invitation.Id);
            return denied ?? HandleResponse(invitation, form, true);
        }

        private IActionResult CheckAccess(Guid id)
        {
            if (User.Identity?.IsAuthenticated != true) return Challenge();
            return new InvitationPermissions(db).CanView(User, id, DateTime.UtcNow) ? null : NotFound();
        }

        private IActionResult HandleResponse(InvitationConfiguration invitation, RsvpForm form, bool preview)
        {
            if (string.IsNullOrWhiteSpace(form.Name)) ModelState.AddModelError("Name", "Ingresá tu nombre.");
            if (form.Attending == true && (!form.GuestCount.HasValue || form.GuestCount < 1 || form.GuestCount > 10))
                ModelState.AddModelError("GuestCount", "Indicá cuántas personas asistirán en total, incluyéndote (entre 1 y 10).");
            if (!ModelState.IsValid)
            {
                if (preview) ViewData["Preview"] = true;
                ViewData["RsvpForm"] = form;
                return View("Show", invitation);
            }
            if (!preview)
            {
                var entity = db.Invitations.Single(x => x.PublicId == invitation.Id);
                db.GuestResponses.Add(new GuestResponse {
                    InvitationId = entity.Id, GuestId = GuestMatching.FindUnique(db, entity.Id, form.Name), Name = form.Name.Trim(), Contact = form.Contact?.Trim() ?? "",
                    Attending = form.Attending.Value, GuestCount = form.Attending.Value ? form.GuestCount.Value : 0,
                    DietaryRestrictions = form.DietaryRestrictions?.Trim() ?? "", SongRequest = form.SongRequest?.Trim() ?? "", Notes = form.Notes?.Trim() ?? "", SubmittedAtUtc = DateTime.UtcNow
                });
                db.SaveChanges();
            }
            TempData["RsvpSuccess"] = preview ? "¡El formulario funciona! Esta prueba no se guardó porque estás en vista previa." : "¡Gracias! Recibimos tu respuesta.";
            return Redirect((preview ? "/admin/preview/" + invitation.Id : "/invitacion/" + invitation.Slug) + "#asistencia");
        }

        [Authorize, HttpGet("admin/preview/{id:guid}")]
        public IActionResult Preview(Guid id, bool spotifyTest = false)
        {
            var invitation = store.Find(id);
            if (invitation == null) return NotFound();
            var denied = CheckAccess(invitation.Id);
            if (denied != null) return denied;
            ViewData["Preview"] = true;
            if (User.IsInRole("Owner") && spotifyTest && invitation != null) { invitation.Theme.SpotifyTrackUrl = "https://open.spotify.com/track/2TD8Bhc5hFPKh52lFJ2Ec5"; }
            return invitation == null ? (IActionResult)NotFound() : View("Show", invitation);
        }
    }
}
