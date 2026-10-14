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
        private GuestAdministration Page([FromRoute] Guid id, GuestEditor editor = null, string search = "", string status = "active")
        {
            var invitation = store.Find(id);
            if (invitation == null) return null;
            var entityId = EntityId(id).Value;
            return new GuestAdministration { Invitation = invitation, Editor = editor ?? new GuestEditor(), Search = search ?? "", Status = status ?? "active",
                Guests = db.Guests.AsNoTracking().Where(x => x.InvitationId == entityId).OrderBy(x => x.Name).ToList(),
                Tags = db.GuestTags.AsNoTracking().Where(x => x.InvitationId == entityId).OrderBy(x => x.Name).ToList(),
                Tables = db.SeatingTables.AsNoTracking().Where(x => x.InvitationId == entityId).OrderBy(x => x.Name).ToList(),
                TagAssignments = db.GuestTagAssignments.AsNoTracking().Where(x => db.Guests.Any(g => g.Id == x.GuestId && g.InvitationId == entityId)).ToList(),
                Responses = db.GuestResponses.AsNoTracking().Where(x => x.InvitationId == entityId).OrderByDescending(x => x.SubmittedAtUtc).ThenByDescending(x => x.Id).ToList() };
        }
        [HttpGet("")]
        public IActionResult Index([FromRoute] Guid id, string search, string status = "active", long? tag = null, long? table = null)
        { var model = Page(id, search: search, status: status); if (model != null) { model.Tag = tag; model.Table = table; } return model == null ? NotFound() : View(model); }
        [HttpGet("edit/{guestId:long}")]
        public IActionResult Edit([FromRoute] Guid id, long guestId)
        {
            var guest = db.Guests.AsNoTracking().SingleOrDefault(x => x.Id == guestId && x.InvitationId == EntityId(id));
            if (guest == null) return NotFound();
            return View("Index", Page(id, new GuestEditor { Id = guest.Id, Name = guest.Name, Contact = guest.Contact, InvitedCount = guest.InvitedCount, Notes = guest.Notes, IsWaitlisted = guest.IsWaitlisted, TableId = guest.TableId, TagIds = db.GuestTagAssignments.Where(x => x.GuestId == guest.Id).Select(x => x.TagId).ToList() }));
        }
        [HttpPost("save"), ValidateAntiForgeryToken]
        public IActionResult Save([FromRoute] Guid id, [FromForm] GuestEditor editor)
        {
            var entityId = EntityId(id);
            if (!entityId.HasValue) return NotFound();
            if (string.IsNullOrWhiteSpace(editor.Name)) ModelState.AddModelError("Name", "Ingresá el nombre del invitado o familia.");
            var guest = editor.Id == 0 ? new Guest { InvitationId = entityId.Value } : db.Guests.SingleOrDefault(x => x.Id == editor.Id && x.InvitationId == entityId);
            if (guest == null) return NotFound();
            editor.TagIds = (editor.TagIds ?? new()).Distinct().ToList();
            if (editor.TableId.HasValue && !db.SeatingTables.Any(x => x.Id == editor.TableId && x.InvitationId == entityId)) ModelState.AddModelError("TableId", "Elegí una mesa de esta invitación.");
            if (editor.TagIds.Any(t => !db.GuestTags.Any(x => x.Id == t && x.InvitationId == entityId))) ModelState.AddModelError("TagIds", "Elegí etiquetas de esta invitación.");
            if (!ModelState.IsValid) return View("Index", Page(id, editor));
            guest.TableId = editor.TableId; guest.IsWaitlisted = editor.IsWaitlisted; guest.Name = editor.Name.Trim(); guest.Contact = editor.Contact?.Trim() ?? ""; guest.InvitedCount = editor.InvitedCount; guest.Notes = editor.Notes?.Trim() ?? "";
            if (editor.Id == 0) db.Guests.Add(guest);
            db.SaveChanges();
            var previousTags = db.GuestTagAssignments.Where(x => x.GuestId == guest.Id).ToList();
            db.GuestTagAssignments.RemoveRange(previousTags.Where(x => !editor.TagIds.Contains(x.TagId)));
            db.GuestTagAssignments.AddRange(editor.TagIds.Where(t => !previousTags.Any(x => x.TagId == t)).Select(t => new GuestTagAssignment { GuestId = guest.Id, TagId = t }));
            db.SaveChanges(); TempData["Success"] = "Invitado guardado.";
            return RedirectToAction(nameof(Index), new { id });
        }
        [HttpGet("organization")]
        public IActionResult Organization([FromRoute] Guid id)
        { var model = Page(id); return model == null ? NotFound() : View(model); }
        [HttpPost("tag"), ValidateAntiForgeryToken]
        public IActionResult SaveTag([FromRoute] Guid id, long tagId, string name, string color)
        {
            var entityId = EntityId(id); if (!entityId.HasValue) return NotFound();
            var tag = tagId == 0 ? new GuestTag { InvitationId = entityId.Value } : db.GuestTags.SingleOrDefault(x => x.Id == tagId && x.InvitationId == entityId);
            if (tag == null) return NotFound();
            name = name?.Trim();
            if (string.IsNullOrEmpty(name) || name.Length > 80 || color == null || !System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$") || db.GuestTags.Any(x => x.InvitationId == entityId && x.Id != tagId && x.Name == name))
            { TempData["Error"] = "Revisá el nombre (único, hasta 80 caracteres) y el color de la etiqueta."; return RedirectToAction(nameof(Organization), new { id }); }
            tag.Name = name; tag.Color = color; if (tagId == 0) db.GuestTags.Add(tag);
            db.SaveChanges(); TempData["Success"] = "Etiqueta guardada."; return RedirectToAction(nameof(Organization), new { id });
        }
        [HttpPost("table"), ValidateAntiForgeryToken]
        public IActionResult SaveTable([FromRoute] Guid id, long tableId, string name, int capacity)
        {
            var entityId = EntityId(id); if (!entityId.HasValue) return NotFound();
            var table = tableId == 0 ? new SeatingTable { InvitationId = entityId.Value } : db.SeatingTables.SingleOrDefault(x => x.Id == tableId && x.InvitationId == entityId);
            if (table == null) return NotFound(); name = name?.Trim();
            if (string.IsNullOrEmpty(name) || name.Length > 80 || capacity < 1 || capacity > 100 || db.SeatingTables.Any(x => x.InvitationId == entityId && x.Id != tableId && x.Name == name))
            { TempData["Error"] = "Revisá el nombre (único, hasta 80 caracteres) y la capacidad (1–100)."; return RedirectToAction(nameof(Organization), new { id }); }
            table.Name = name; table.Capacity = capacity; if (tableId == 0) db.SeatingTables.Add(table);
            db.SaveChanges(); TempData["Success"] = "Mesa guardada."; return RedirectToAction(nameof(Organization), new { id });
        }
        [HttpPost("assign/{guestId:long}"), ValidateAntiForgeryToken]
        public IActionResult AssignTable([FromRoute] Guid id, long guestId, long? tableId)
        {
            var entityId = EntityId(id); if (!entityId.HasValue) return NotFound();
            var guest = db.Guests.SingleOrDefault(x => x.Id == guestId && x.InvitationId == entityId && !x.IsArchived && !x.IsWaitlisted);
            if (guest == null) return NotFound();
            if (tableId.HasValue && !db.SeatingTables.Any(x => x.Id == tableId && x.InvitationId == entityId)) return BadRequest();
            guest.TableId = tableId; db.SaveChanges(); TempData["Success"] = "Mesa asignada.";
            return RedirectToAction(nameof(Organization), new { id });
        }
        [HttpPost("remove-tag/{tagId:long}"), ValidateAntiForgeryToken]
        public IActionResult RemoveTag([FromRoute] Guid id, long tagId)
        {
            var tag = db.GuestTags.SingleOrDefault(x => x.Id == tagId && x.InvitationId == EntityId(id)); if (tag == null) return NotFound();
            db.GuestTags.Remove(tag); db.SaveChanges(); TempData["Success"] = "Etiqueta eliminada. Los invitados se conservan."; return RedirectToAction(nameof(Organization), new { id });
        }
        [HttpPost("remove-table/{tableId:long}"), ValidateAntiForgeryToken]
        public IActionResult RemoveTable([FromRoute] Guid id, long tableId)
        {
            var table = db.SeatingTables.SingleOrDefault(x => x.Id == tableId && x.InvitationId == EntityId(id)); if (table == null) return NotFound();
            // Explicitly clear tracked assignments as well as the database SetNull relationship.
            foreach (var guest in db.Guests.Where(x => x.TableId == table.Id)) guest.TableId = null;
            db.SeatingTables.Remove(table); db.SaveChanges(); TempData["Success"] = "Mesa eliminada. Sus invitados quedan sin mesa."; return RedirectToAction(nameof(Organization), new { id });
        }
        [HttpPost("archive/{guestId:long}"), ValidateAntiForgeryToken]
        public IActionResult Archive([FromRoute] Guid id, long guestId, bool restore = false)
        {
            var guest = db.Guests.SingleOrDefault(x => x.Id == guestId && x.InvitationId == EntityId(id));
            if (guest == null) return NotFound();
            guest.IsArchived = !restore; db.SaveChanges();
            TempData["Success"] = restore ? "Invitado recuperado." : "Invitado quitado de la lista. Podés recuperarlo en Archivados.";
            return RedirectToAction(nameof(Index), new { id });
        }
        [HttpPost("membership/{guestId:long}"), ValidateAntiForgeryToken]
        public IActionResult Membership([FromRoute] Guid id, long guestId, bool waitlisted)
        {
            var guest = db.Guests.SingleOrDefault(x => x.Id == guestId && x.InvitationId == EntityId(id) && !x.IsArchived);
            if (guest == null) return NotFound();
            guest.IsWaitlisted = waitlisted; db.SaveChanges();
            TempData["Success"] = waitlisted ? "Invitado movido a lista de espera. Ya no suma en la lista fija." : "Invitado agregado a la lista fija. Ya podés enviarle la invitación.";
            return RedirectToAction(nameof(Index), new { id, status = waitlisted ? "waitlist" : "active" });
        }
        [HttpPost("link/{responseId:long}"), ValidateAntiForgeryToken]
        public IActionResult Link([FromRoute] Guid id, long responseId, long? guestId)
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
        public IActionResult Record([FromRoute] Guid id, long guestId, RsvpForm form)
        {
            var guest = db.Guests.SingleOrDefault(x => x.Id == guestId && x.InvitationId == EntityId(id) && !x.IsArchived && !x.IsWaitlisted);
            if (guest == null) return NotFound();
            // The administrator records the name and contact already held in the guest list.
            ModelState.Remove(nameof(RsvpForm.Name));
            if (!form.Attending.HasValue || (form.Attending.Value && (!form.GuestCount.HasValue || form.GuestCount < 1 || form.GuestCount > guest.InvitedCount)) || !ModelState.IsValid)
            { TempData["Error"] = "Revisá la asistencia y la cantidad: no puede superar las personas invitadas."; return RedirectToAction(nameof(Index), new { id }); }
            db.GuestResponses.Add(new GuestResponse { InvitationId = guest.InvitationId, GuestId = guest.Id, Name = guest.Name, Contact = guest.Contact,
                Attending = form.Attending.Value, GuestCount = form.Attending.Value ? form.GuestCount.Value : 0, DietaryRestrictions = form.DietaryRestrictions?.Trim() ?? "", SongRequest = form.SongRequest?.Trim() ?? "", Notes = form.Notes?.Trim() ?? "", SubmittedAtUtc = DateTime.UtcNow });
            db.SaveChanges(); TempData["Success"] = "Respuesta registrada. La última respuesta define el estado actual.";
            return RedirectToAction(nameof(Index), new { id });
        }
    }
}
