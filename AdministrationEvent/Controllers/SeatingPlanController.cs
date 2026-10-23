using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AdministrationEvent.Services;
using Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministrationEvent.Controllers
{
    [Authorize(Roles = "Owner"), Route("admin/guests/{id:guid}/plan")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class SeatingPlanController : Controller
    {
        private readonly InvitationDbContext db;
        private readonly InvitationStore store;
        public SeatingPlanController(InvitationDbContext db, InvitationStore store) { this.db = db; this.store = store; }
        private long? EntityId(Guid id) => db.Invitations.Where(x => x.PublicId == id && x.SeatingPlanEnabled).Select(x => (long?)x.Id).SingleOrDefault();
        [HttpGet("")]
        public IActionResult Index(Guid id)
        {
            var invitation = store.Find(id); if (invitation == null || !invitation.SeatingPlanEnabled) return NotFound();
            var entityId = EntityId(id).Value;
            ViewData["Plan"] = db.SeatingPlans.AsNoTracking().SingleOrDefault(x => x.InvitationId == entityId) ?? new SeatingPlan();
            ViewData["Tables"] = db.SeatingTables.AsNoTracking().Where(x => x.InvitationId == entityId).OrderBy(x => x.Name).ToList();
            return View(invitation);
        }
        [HttpGet("image")]
        public IActionResult Image(Guid id)
        {
            var plan = db.SeatingPlans.AsNoTracking().SingleOrDefault(x => x.InvitationId == EntityId(id));
            return plan?.Image == null ? NotFound() : File(plan.Image, plan.ImageType);
        }
        [HttpPost("image"), ValidateAntiForgeryToken, RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> Upload(Guid id, IFormFile file)
        {
            var entityId = EntityId(id); if (!entityId.HasValue) return NotFound();
            if (file == null || file.Length == 0 || file.Length > 5 * 1024 * 1024) return BadRequest(new { error = "Elegí una imagen PNG o JPG de hasta 5 MB." });
            using var stream = new MemoryStream(); await file.CopyToAsync(stream); var bytes = stream.ToArray();
            var png = bytes.Length > 24 && bytes.Take(8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10});
            var jpg = bytes.Length > 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255;
            if (!png && !jpg) return BadRequest(new { error = "El plano debe ser una imagen PNG o JPG." });
            var plan = db.SeatingPlans.SingleOrDefault(x => x.InvitationId == entityId) ?? new SeatingPlan { InvitationId = entityId.Value };
            plan.Image = bytes; plan.ImageType = png ? "image/png" : "image/jpeg";
            if (plan.Id == 0) db.SeatingPlans.Add(plan);
            await db.SaveChangesAsync(); return Ok();
        }
        [HttpPost("table"), ValidateAntiForgeryToken]
        public IActionResult AddTable(Guid id, string name, int capacity)
        {
            var entityId = EntityId(id); if (!entityId.HasValue) return NotFound(); name = name?.Trim();
            if (string.IsNullOrEmpty(name) || name.Length > 80 || capacity < 1 || capacity > 100 || db.SeatingTables.Any(x => x.InvitationId == entityId && x.Name == name))
                return BadRequest(new { error = "Usá un número o nombre único y entre 1 y 100 lugares." });
            var table = new SeatingTable { InvitationId = entityId.Value, Name = name, Capacity = capacity };
            db.SeatingTables.Add(table); db.SaveChanges(); return Json(new { id = table.Id, name = table.Name, capacity = table.Capacity });
        }
        public class Position
        {
            public long TableId { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public string Shape { get; set; } = "round";
            public string Description { get; set; } = "";
            public int Rotation { get; set; }
        }
        public class PlanInput { public List<Position> Tables { get; set; } = new(); public bool UseVenueImage { get; set; } }
        [HttpPost(""), ValidateAntiForgeryToken]
        public IActionResult Save(Guid id, [FromBody] PlanInput input)
        {
            var entityId = EntityId(id); if (!entityId.HasValue) return NotFound();
            if (!ModelState.IsValid || input?.Tables == null || input.Tables.Count > 200) return BadRequest(new { error = "Revisá la distribución de las mesas." });
            var ids = db.SeatingTables.Where(x => x.InvitationId == entityId).Select(x => x.Id).ToHashSet();
            if (input.Tables.Select(x => x.TableId).Distinct().Count() != input.Tables.Count || input.Tables.Any(x => (x.Description?.Length ?? 0) > 200 || !ids.Contains(x.TableId) || !double.IsFinite(x.X) || !double.IsFinite(x.Y) || x.X < 6 || x.X > 94 || x.Y < 7 || x.Y > 93 || (x.Shape != "round" && x.Shape != "rectangle") || x.Rotation < 0 || x.Rotation >= 360))
                return BadRequest(new { error = "Hay una mesa o posición inválida." });
            var plan = db.SeatingPlans.SingleOrDefault(x => x.InvitationId == entityId) ?? new SeatingPlan { InvitationId = entityId.Value };
            if (input.UseVenueImage && plan.Image == null) return BadRequest(new { error = "Subí primero el plano del lugar." });
            plan.LayoutJson = JsonSerializer.Serialize(input.Tables); plan.UseVenueImage = input.UseVenueImage;
            if (plan.Id == 0) db.SeatingPlans.Add(plan); db.SaveChanges(); return Ok();
        }
    }
}
