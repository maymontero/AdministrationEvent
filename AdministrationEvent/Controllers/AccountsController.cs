using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using AdministrationEvent.Models;
using AdministrationEvent.Services;
namespace AdministrationEvent.Controllers
{
    [Authorize, Route("admin/accounts")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class AccountsController : Controller
    {
        private readonly InvitationDbContext db;
        private readonly AdminAccounts accounts;
        public AccountsController(InvitationDbContext db, AdminAccounts accounts) { this.db = db; this.accounts = accounts; }
        private IQueryable<Application.AdminUser> UserSummaries() => db.AdminUsers.AsNoTracking().Select(u => new Application.AdminUser { Id=u.Id, DisplayName=u.DisplayName, Username=u.Username, IsOwner=u.IsOwner, IsActive=u.IsActive });
        [Authorize(Policy = "ManageAccounts"), HttpGet("")]
        public IActionResult Index(string search = "", string status = "all", int page = 1)
        {
            search=(search??"").Trim();if(search.Length>120)search=search.Substring(0,120);
            if(status!="active" && status!="inactive")status="all";
            var query=UserSummaries();
            if(search.Length>0)query=query.Where(u=>u.DisplayName.Contains(search)||u.Username.Contains(search));
            if(status=="active")query=query.Where(u=>u.IsActive);
            if(status=="inactive")query=query.Where(u=>!u.IsActive);
            var total=query.Count();var pages=Math.Max(1,(int)Math.Ceiling(total/20d));page=Math.Clamp(page,1,pages);
            var users=query.OrderBy(u=>u.DisplayName).ThenBy(u=>u.Id).Skip((page-1)*20).Take(20).ToList();var ids=users.Select(u=>u.Id).ToList();
            var counts=db.InvitationAccesses.Where(a=>ids.Contains(a.UserId)).GroupBy(a=>a.UserId).Select(g=>new {Id=g.Key,Count=g.Count()}).ToDictionary(x=>x.Id,x=>x.Count);
            return View(new AccountPage {Users=users,InvitationCounts=counts,Search=search,Status=status,Page=page,PageCount=pages,Total=total});
        }
        [Authorize(Policy = "ManageAccounts"), HttpGet("new")]
        public IActionResult New() => View("Create",new CreateAccount());
        [Authorize(Policy = "ManageAccounts"), HttpGet("permissions/{id:long}")]
        public IActionResult Permissions(long id,string search="",int page=1)
        {
            var user=UserSummaries().SingleOrDefault(u=>u.Id==id&&!u.IsOwner);if(user==null)return NotFound();
            search=(search??"").Trim();if(search.Length>120)search=search.Substring(0,120);
            var query=db.Invitations.AsNoTracking();if(search.Length>0)query=query.Where(i=>i.Title.Contains(search));
            var total=query.Count();var pages=Math.Max(1,(int)Math.Ceiling(total/20d));page=Math.Clamp(page,1,pages);
            var invitations=query.OrderBy(i=>i.Title).ThenBy(i=>i.Id).Skip((page-1)*20).Take(20).Select(i=>new InvitationChoice {Id=i.Id,Title=i.Title,IsPublished=i.IsPublished}).ToList();var ids=invitations.Select(i=>i.Id).ToList();
            return View(new AccountPermissionsPage {User=user,Invitations=invitations,Accesses=db.InvitationAccesses.AsNoTracking().Where(a=>a.UserId==id&&ids.Contains(a.InvitationId)).ToList(),Search=search,Page=page,PageCount=pages,Total=total});
        }
        [Authorize(Policy = "ManageAccounts"), HttpGet("reset/{id:long}")]
        public IActionResult ResetPassword(long id)
        {
            var user=UserSummaries().SingleOrDefault(u=>u.Id==id&&!u.IsOwner);if(user==null)return NotFound();
            return View("Reset",new AccountPasswordReset {Id=user.Id,DisplayName=user.DisplayName,Username=user.Username});
        }
        [Authorize(Policy = "ManageAccounts"), HttpPost("create"), ValidateAntiForgeryToken]
        public IActionResult Create(CreateAccount editor) {
            if (ModelState.IsValid) { var error = accounts.Create(editor.Username, editor.DisplayName, editor.Password); if (error == null) { TempData["Success"] = "Cuenta creada. Compartí los datos de acceso en privado."; return RedirectToAction(nameof(Index)); } ModelState.AddModelError("", error); }
            editor.Password = ""; return View("Create", editor);
        }
        [Authorize(Policy = "ManageAccounts"), HttpPost("toggle/{id:long}"), ValidateAntiForgeryToken]
        public IActionResult Toggle(long id, string search="", string status="all", int page=1) {
            var user = db.AdminUsers.SingleOrDefault(x => x.Id == id); if (user == null) return NotFound();
            if (user.IsOwner) return BadRequest("La cuenta de administradora se conserva activa.");
            user.IsActive = !user.IsActive; user.SecurityStamp = Guid.NewGuid().ToString(); db.SaveChanges(); TempData["Success"] = user.IsActive ? "Acceso activado." : "Acceso desactivado. Sus permisos se conservan."; return RedirectToAction(nameof(Index),new {search,status,page});
        }
        [Authorize(Policy = "ManageAccounts"), HttpPost("reset/{id:long}"), ValidateAntiForgeryToken]
        public IActionResult Reset(long id, string password) {
            var user = db.AdminUsers.SingleOrDefault(x => x.Id == id); if (user == null) return NotFound();
            if (user.IsOwner) return BadRequest("Usá Cambiar contraseña para tu cuenta.");
            if (password == null || password.Length < 12 || password.Length > 128) { ModelState.AddModelError("password", "La contraseña debe tener entre 12 y 128 caracteres."); return View("Reset", new AccountPasswordReset {Id=user.Id,DisplayName=user.DisplayName,Username=user.Username}); }
            else { accounts.SetPassword(user, password); TempData["Success"] = "Contraseña actualizada. Las sesiones anteriores se cerrarán."; }
            return RedirectToAction(nameof(Index));
        }
        [Authorize(Policy = "ManageAccounts"), HttpPost("access/{id:long}"), ValidateAntiForgeryToken]
        public IActionResult Access(long id, long invitationId, bool enabled, DateTime? availableFrom, bool canDesign = false, string search="", int page=1)
        {
            var user = db.AdminUsers.SingleOrDefault(x => x.Id == id && !x.IsOwner);
            if (user == null || !db.Invitations.Any(x => x.Id == invitationId)) return NotFound();
            if (!ModelState.IsValid) { TempData["Error"] = "Revisá la fecha de habilitación."; return RedirectToAction(nameof(Permissions),new {id,search,page}); }
            DateTime? utc = availableFrom.HasValue ? TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(availableFrom.Value, DateTimeKind.Unspecified), TimeZoneInfo.FindSystemTimeZoneById("America/Montevideo")) : null;
            var access = db.InvitationAccesses.SingleOrDefault(x => x.UserId == id && x.InvitationId == invitationId);
            if (!enabled) { if (access != null) db.InvitationAccesses.Remove(access); }
            else { if (access == null) { access = new Application.InvitationAccess { UserId = id, InvitationId = invitationId }; db.InvitationAccesses.Add(access); } access.AvailableFromUtc = utc; access.CanDesign = canDesign; }
            db.SaveChanges(); TempData["Success"] = "Permiso actualizado."; return RedirectToAction(nameof(Permissions),new {id,search,page});
        }
        [HttpGet("password")] public IActionResult Password() => View(new ChangeAccountPassword());
        [HttpPost("password"), ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<IActionResult> Password(ChangeAccountPassword form) {
            var id = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)); var user = db.AdminUsers.Single(x => x.Id == id);
            if (ModelState.IsValid && accounts.Authenticate(user.Username, form.CurrentPassword) == null) ModelState.AddModelError("", "La contraseña actual no es correcta.");
            if (!ModelState.IsValid) { ModelState.Clear(); ModelState.AddModelError("", "Revisá la contraseña actual y confirmá una nueva de 12 a 128 caracteres."); return View(new ChangeAccountPassword()); }
            accounts.SetPassword(user, form.NewPassword); await HttpContext.SignOutAsync(); TempData["Success"] = "Contraseña actualizada. Ingresá nuevamente."; return Redirect("/admin/login");
        }
    }
}
