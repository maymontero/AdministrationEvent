using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using AdministrationEvent.Models;
using AdministrationEvent.Services;
namespace AdministrationEvent.Controllers
{
    [Authorize, Route("admin/accounts")]
    public class AccountsController : Controller
    {
        private readonly InvitationDbContext db;
        private readonly AdminAccounts accounts;
        public AccountsController(InvitationDbContext db, AdminAccounts accounts) { this.db = db; this.accounts = accounts; }
        private AccountPage Page(CreateAccount editor = null) => new() { Users = db.AdminUsers.OrderBy(x => x.DisplayName).ToList(), Editor = editor ?? new() };
        [Authorize(Policy = "ManageAccounts"), HttpGet("")] public IActionResult Index() => View(Page());
        [Authorize(Policy = "ManageAccounts"), HttpPost("create"), ValidateAntiForgeryToken]
        public IActionResult Create(CreateAccount editor) {
            if (ModelState.IsValid) { var error = accounts.Create(editor.Username, editor.DisplayName, editor.Password); if (error == null) { TempData["Success"] = "Cuenta creada. Compartí los datos de acceso en privado."; return RedirectToAction(nameof(Index)); } ModelState.AddModelError("", error); }
            editor.Password = ""; return View("Index", Page(editor));
        }
        [Authorize(Policy = "ManageAccounts"), HttpPost("toggle/{id:long}"), ValidateAntiForgeryToken]
        public IActionResult Toggle(long id) {
            var user = db.AdminUsers.SingleOrDefault(x => x.Id == id); if (user == null) return NotFound();
            if (user.IsOwner) return BadRequest("La cuenta de administradora se conserva activa.");
            user.IsActive = !user.IsActive; user.SecurityStamp = Guid.NewGuid().ToString(); db.SaveChanges(); return RedirectToAction(nameof(Index));
        }
        [Authorize(Policy = "ManageAccounts"), HttpPost("reset/{id:long}"), ValidateAntiForgeryToken]
        public IActionResult Reset(long id, string password) {
            var user = db.AdminUsers.SingleOrDefault(x => x.Id == id); if (user == null) return NotFound();
            if (user.IsOwner) return BadRequest("Usá Cambiar contraseña para tu cuenta.");
            if (password == null || password.Length < 12 || password.Length > 128) TempData["Error"] = "La contraseña debe tener entre 12 y 128 caracteres.";
            else { accounts.SetPassword(user, password); TempData["Success"] = "Contraseña actualizada. Las sesiones anteriores se cerrarán."; }
            return RedirectToAction(nameof(Index));
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
