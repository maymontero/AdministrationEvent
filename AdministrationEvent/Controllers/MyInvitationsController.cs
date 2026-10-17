using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdministrationEvent.Services;
namespace AdministrationEvent.Controllers
{
    [Authorize, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class MyInvitationsController : Controller
    {
        [HttpGet("mis-invitaciones")]
        public IActionResult Index([FromServices] InvitationStore store, [FromServices] InvitationPermissions permissions)
        {
            var now = DateTime.UtcNow;
            return View(store.All().Where(i => permissions.CanView(User, i.Id, now)).ToList());
        }
    }
}
