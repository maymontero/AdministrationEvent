using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace AdministrationEvent
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            var connection = Configuration.GetConnectionString("Invitations");
            if (string.IsNullOrWhiteSpace(connection))
                throw new InvalidOperationException("Configurá ConnectionStrings:Invitations con la conexión a MySQL. Consultá README.md.");
            services.AddDbContext<InvitationDbContext>(options => options.UseMySQL(connection));
            services.AddScoped<AdministrationEvent.Services.InvitationStore>();
            services.AddScoped<AdministrationEvent.Services.AdminAccounts>();
            services.AddScoped<AdministrationEvent.Services.InvitationPermissions>();
            services.AddAuthorization(options => options.AddPolicy("ManageAccounts", policy => policy.RequireRole("Owner")));
            services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options => { options.LoginPath = "/admin/login"; options.AccessDeniedPath = "/admin/denied"; options.ExpireTimeSpan = TimeSpan.FromHours(8); options.Cookie.HttpOnly = true; options.Cookie.SecurePolicy = Configuration["ASPNETCORE_ENVIRONMENT"] == "Development" ? Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest : Microsoft.AspNetCore.Http.CookieSecurePolicy.Always; options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
                    options.Events.OnValidatePrincipal = async context => {
                        var id = context.Principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        var database = context.HttpContext.RequestServices.GetRequiredService<InvitationDbContext>();
                        var user = long.TryParse(id, out var userId) ? await database.AdminUsers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId) : null;
                        if (user == null || !user.IsActive || user.SecurityStamp != context.Principal.FindFirst("stamp")?.Value) {
                            context.RejectPrincipal();
                            await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(context.HttpContext);
                        }
                        else {
                            var identity = (System.Security.Claims.ClaimsIdentity)context.Principal.Identity;
                            foreach (var role in identity.FindAll(System.Security.Claims.ClaimTypes.Role).ToList()) identity.RemoveClaim(role);
                            identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.IsOwner ? "Owner" : "Viewer"));
                        }
                    }; });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
