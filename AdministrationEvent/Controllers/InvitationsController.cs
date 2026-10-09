using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AdministrationEvent.Services;
using AdministrationEvent.Models;
using Application;
namespace AdministrationEvent.Controllers
{
    public class InvitationsController : Controller
    {
        private readonly InvitationStore store;
        private readonly InvitationDbContext db;
        public InvitationsController(InvitationStore store, InvitationDbContext db) { this.store = store; this.db = db; }

        [HttpGet("invitacion/{slug}")]
        public IActionResult Show(string slug)
        {
            var invitation = store.FindPublished(slug);
            return invitation == null ? (IActionResult)NotFound() : View(invitation);
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
            return HandleResponse(invitation, form, true);
        }

        private IActionResult HandleResponse(InvitationConfiguration invitation, RsvpForm form, bool preview)
        {
            if (string.IsNullOrWhiteSpace(form.Name)) ModelState.AddModelError("Name", "Ingresá tu nombre.");
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
                    InvitationId = entity.Id, Name = form.Name.Trim(), Contact = form.Contact?.Trim() ?? "",
                    Attending = form.Attending.Value, GuestCount = form.Attending.Value ? form.GuestCount : 0,
                    Notes = form.Notes?.Trim() ?? "", SubmittedAtUtc = DateTime.UtcNow
                });
                db.SaveChanges();
            }
            TempData["RsvpSuccess"] = preview ? "¡El formulario funciona! Esta prueba no se guardó porque estás en vista previa." : "¡Gracias! Recibimos tu respuesta.";
            return Redirect((preview ? "/admin/preview/" + invitation.Id : "/invitacion/" + invitation.Slug) + "#asistencia");
        }

        [Authorize, HttpGet("admin/preview/{id:guid}")]
        public IActionResult Preview(Guid id)
        {
            var invitation = store.Find(id);
            ViewData["Preview"] = true;
            return invitation == null ? (IActionResult)NotFound() : View("Show", invitation);
        }
    }
}
