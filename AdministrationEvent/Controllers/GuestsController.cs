using System;
using System.Linq;
using AdministrationEvent.Models;
using AdministrationEvent.Services;
using Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministrationEvent.Controllers
{
    [Authorize, Route("admin/guests/{id:guid}")]
    public class GuestsController : Controller
    {
        private readonly InvitationDbContext db;
        private readonly InvitationStore store;
        public GuestsController(InvitationDbContext db, InvitationStore store) { this.db = db; this.store = store; }
        private long? EntityId(Guid id) => db.Invitations.Where(x => x.PublicId == id).Select(x => (long?)x.Id).SingleOrDefault();
        private GuestAdministration Page(Guid id, GuestEditor editor = null, string search = "", string status = "active")
        {
            var invitation = store.Find(id);
            if (invitation == null) return null;
            var entityId = EntityId(id).Value;
            return new GuestAdministration { Invitation = invitation, Editor = editor ?? new GuestEditor(), Search = search ?? "", Status = status ?? "active",
                Guests = db.Guests.AsNoTracking().Where(x => x.InvitationId == entityId).OrderBy(x => x.Name).ToList(),
                Responses = db.GuestResponses.AsNoTracking().Where(x => x.InvitationId == entityId).OrderByDescending(x => x.SubmittedAtUtc).ThenByDescending(x => x.Id).ToList() };
        }
        [HttpGet("")]
        public IActionResult Index(Guid id, string search, string status = "active")
        { var model = Page(id, search: search, status: status); return model == null ? NotFound() : View(model); }
        [HttpGet("edit/{guestId:long}")]
        public IActionResult Edit(Guid id, long guestId)
        {
            var guest = db.Guests.AsNoTracking().SingleOrDefault(x => x.Id == guestId && x.InvitationId == EntityId(id));
            if (guest == null) return NotFound();
            return View("Index", Page(id, new GuestEditor { Id = guest.Id, Name = guest.Name, Contact = guest.Contact, InvitedCount = guest.InvitedCount, Notes = guest.Notes, IsWaitlisted = guest.IsWaitlisted }));
        }
        [HttpPost("save"), ValidateAntiForgeryToken]
        public IActionResult Save(Guid id, GuestEditor editor)
        {
            var entityId = EntityId(id);
            if (!entityId.HasValue) return NotFound();
            if (string.IsNullOrWhiteSpace(editor.Name)) ModelState.AddModelError("Name", "Ingresá el nombre del invitado o familia.");
            var guest = editor.Id == 0 ? new Guest { InvitationId = entityId.Value } : db.Guests.SingleOrDefault(x => x.Id == editor.Id && x.InvitationId == entityId);
            if (guest == null) return NotFound();
            if (!ModelState.IsValid) return View("Index", Page(id, editor));
            guest.IsWaitlisted = editor.IsWaitlisted; guest.Name = editor.Name.Trim(); guest.Contact = editor.Contact?.Trim() ?? ""; guest.InvitedCount = editor.InvitedCount; guest.Notes = editor.Notes?.Trim() ?? "";
            if (editor.Id == 0) db.Guests.Add(guest);
            db.SaveChanges(); TempData["Success"] = "Invitado guardado.";
            return RedirectToAction(nameof(Index), new { id });
        }
        [HttpPost("archive/{guestId:long}"), ValidateAntiForgeryToken]
        public IActionResult Archive(Guid id, long guestId, bool restore = false)
        {
            var guest = db.Guests.SingleOrDefault(x => x.Id == guestId && x.InvitationId == EntityId(id));
            if (guest == null) return NotFound();
            guest.IsArchived = !restore; db.SaveChanges();
            TempData["Success"] = restore ? "Invitado recuperado." : "Invitado quitado de la lista. Podés recuperarlo en Archivados.";
            return RedirectToAction(nameof(Index), new { id });
        }
        [HttpPost("membership/{guestId:long}"), ValidateAntiForgeryToken]
        public IActionResult Membership(Guid id, long guestId, bool waitlisted)
        {
            var guest = db.Guests.SingleOrDefault(x => x.Id == guestId && x.InvitationId == EntityId(id) && !x.IsArchived);
            if (guest == null) return NotFound();
            guest.IsWaitlisted = waitlisted; db.SaveChanges();
            TempData["Success"] = waitlisted ? "Invitado movido a lista de espera. Ya no suma en la lista fija." : "Invitado agregado a la lista fija. Ya podés enviarle la invitación.";
            return RedirectToAction(nameof(Index), new { id, status = waitlisted ? "waitlist" : "active" });
        }
        [HttpPost("link/{responseId:long}"), ValidateAntiForgeryToken]
        public IActionResult Link(Guid id, long responseId, long? guestId)
        {
            var entityId = EntityId(id);
            if (!entityId.HasValue) return NotFound();
            var response = db.GuestResponses.SingleOrDefault(x => x.Id == responseId && x.InvitationId == entityId);
            if (response == null) return NotFound();
            if (guestId.HasValue && !db.Guests.Any(x => x.Id == guestId && x.InvitationId == entityId && !x.IsArchived && !x.IsWaitlisted)) return BadRequest();
            response.GuestId = guestId; db.SaveChanges(); TempData["Success"] = "Asociación actualizada. Se conserva el historial de respuestas.";
            return RedirectToAction(nameof(Index), new { id });
        }
        [HttpPost("response/{guestId:long}"), ValidateAntiForgeryToken]
        public IActionResult Record(Guid id, long guestId, RsvpForm form)
        {
            var guest = db.Guests.SingleOrDefault(x => x.Id == guestId && x.InvitationId == EntityId(id) && !x.IsArchived && !x.IsWaitlisted);
            if (guest == null) return NotFound();
            // The administrator records the name and contact already held in the guest list.
            ModelState.Remove(nameof(RsvpForm.Name));
            if (!form.Attending.HasValue || (form.Attending.Value && (!form.GuestCount.HasValue || form.GuestCount < 1 || form.GuestCount > guest.InvitedCount)) || !ModelState.IsValid)
            { TempData["Error"] = "Revisá la asistencia y la cantidad: no puede superar las personas invitadas."; return RedirectToAction(nameof(Index), new { id }); }
            db.GuestResponses.Add(new GuestResponse { InvitationId = guest.InvitationId, GuestId = guest.Id, Name = guest.Name, Contact = guest.Contact,
                Attending = form.Attending.Value, GuestCount = form.Attending.Value ? form.GuestCount.Value : 0, Notes = form.Notes?.Trim() ?? "", SubmittedAtUtc = DateTime.UtcNow });
            db.SaveChanges(); TempData["Success"] = "Respuesta registrada. La última respuesta define el estado actual.";
            return RedirectToAction(nameof(Index), new { id });
        }
    }
}
