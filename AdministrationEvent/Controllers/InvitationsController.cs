using System;
using System.Linq;
using System.Text.RegularExpressions;
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

        [HttpGet("{slug:regex(^[[a-z0-9]]+(?:-[[a-z0-9]]+)*$)}")]
        public IActionResult Show(string slug)
        {
            var invitation = store.FindPublished(slug);
            if (invitation == null || invitation.SharingMode != "public") return NotFound();
            ViewData["PublicPath"] = "/" + invitation.Slug;
            return View(invitation);
        }

        // Conserva los enlaces anteriores para quienes los hayan guardado, pero la URL
        // visible y canónica de las invitaciones públicas es /{slug}.
        [HttpGet("invitacion/{slug}")]
        public IActionResult LegacyShow(string slug)
        {
            var invitation = store.FindPublished(slug);
            return invitation != null && invitation.SharingMode == "public"
                ? RedirectPermanent("/" + invitation.Slug)
                : NotFound();
        }

        [HttpGet("{slug:regex(^[[a-z0-9]]+(?:-[[a-z0-9]]+)*$)}/acceso/{key}")]
        public IActionResult Shared(string slug, string key)
        {
            var invitation = store.FindPublished(slug);
            if (!IsValidShareKey(key) || invitation == null || invitation.SharingMode != "link" || !string.Equals(invitation.ShareKey, key, StringComparison.Ordinal)) return NotFound();
            ViewData["PublicPath"] = "/" + invitation.Slug + "/acceso/" + key;
            return View("Show", invitation);
        }


        [HttpPost("{slug:regex(^[[a-z0-9]]+(?:-[[a-z0-9]]+)*$)}/asistencia"), ValidateAntiForgeryToken]
        public IActionResult Respond(string slug, RsvpForm form)
        {
            var invitation = store.FindPublished(slug);
            if (invitation == null || invitation.SharingMode != "public" || !invitation.RsvpEnabled) return NotFound();
            return HandleResponse(invitation, form, false, "/" + slug);
        }

        [HttpPost("invitacion/{slug}/asistencia"), ValidateAntiForgeryToken]
        public IActionResult LegacyRespond(string slug, RsvpForm form)
        {
            var invitation = store.FindPublished(slug);
            if (invitation == null || invitation.SharingMode != "public" || !invitation.RsvpEnabled) return NotFound();
            return HandleResponse(invitation, form, false, "/" + slug);
        }

        [HttpPost("{slug:regex(^[[a-z0-9]]+(?:-[[a-z0-9]]+)*$)}/acceso/{key}/asistencia"), ValidateAntiForgeryToken]
        public IActionResult SharedRespond(string slug, string key, RsvpForm form)
        {
            var invitation = store.FindPublished(slug);
            if (!IsValidShareKey(key) || invitation == null || invitation.SharingMode != "link" || !string.Equals(invitation.ShareKey, key, StringComparison.Ordinal) || !invitation.RsvpEnabled) return NotFound();
            return HandleResponse(invitation, form, false, "/" + slug + "/acceso/" + key);
        }

        [Authorize, HttpPost("admin/preview/{id:guid}/asistencia"), ValidateAntiForgeryToken]
        public IActionResult PreviewResponse(Guid id, RsvpForm form)
        {
            var invitation = store.Find(id);
            if (invitation == null || !invitation.RsvpEnabled) return NotFound();
            var denied = CheckAccess(invitation.Id);
            return denied ?? HandleResponse(invitation, form, true, "/admin/preview/" + invitation.Id);
        }

        private IActionResult CheckAccess(Guid id)
        {
            if (User.Identity?.IsAuthenticated != true) return Challenge();
            return new InvitationPermissions(db).CanView(User, id, DateTime.UtcNow) ? null : NotFound();
        }

        private static bool IsValidShareKey(string key) => !string.IsNullOrWhiteSpace(key) && Regex.IsMatch(key, "^[a-f0-9]{48}$");

        private IActionResult HandleResponse(InvitationConfiguration invitation, RsvpForm form, bool preview, string returnPath)
        {
            if (string.IsNullOrWhiteSpace(form.Name)) ModelState.AddModelError("Name", "Ingresá tu nombre.");
            if (form.Attending == true && (!form.GuestCount.HasValue || form.GuestCount < 1 || form.GuestCount > 10))
                ModelState.AddModelError("GuestCount", "Indicá cuántas personas asistirán en total, incluyéndote (entre 1 y 10).");
            if (!ModelState.IsValid)
            {
                if (preview) ViewData["Preview"] = true;
                ViewData["PublicPath"] = returnPath;
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
            return Redirect(returnPath + "#asistencia");
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
