using System.Globalization;
using System.Security.Claims;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

using PontoApp.Application.Contracts;          // seus contratos atuais (se usados)
using PontoApp.Application.Services;          // seus services atuais (ReportService etc.)

// NOVOS contratos/serviços do setup/multi-empresa
using PontoApp.Infrastructure.Services;

using PontoApp.Domain.Interfaces;
using PontoApp.Infrastructure.EF;
using PontoApp.Infrastructure.Repositories;
using PontoApp.Web.Validation;

// Options do Setup (Controller que te passei)
using PontoApp.Web.Controllers;

var builder = WebApplication.CreateBuilder(args);

/* MVC + validação */
builder.Services
    .AddControllersWithViews(options =>
    {
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddViewOptions(o => o.HtmlHelperOptions.ClientValidationEnabled = true);

builder.Services.AddLocalization();

/* EF Core */
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

/* Acesso ao HttpContext para filtros globais por CompanyId */
builder.Services.AddHttpContextAccessor();

/* Data Protection (chaves no filesystem) */
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "keys")))
    .SetApplicationName("PontoApp");

/* FluentValidation */
builder.Services.AddFluentValidationAutoValidation(o =>
{
    o.DisableDataAnnotationsValidation = true;
});
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterEmployeeViewModelValidator>();
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("pt-BR");

/* Auth Cookie */
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.LogoutPath = "/Account/Logout";
        o.AccessDeniedPath = "/Account/AccessDenied";
        o.Cookie.Name = "pontoapp.auth";
        o.ExpireTimeSpan = TimeSpan.FromDays(14);
        o.SlidingExpiration = true;
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        // Em DEV, se necessário, troque para SameAsRequest
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

/* Autorização */
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdmin", p => p.RequireClaim(ClaimTypes.Role, "Admin"))
    .AddPolicy("OwnEmployee", p => p.AddRequirements(new OwnEmployeeRequirement()));
builder.Services.AddSingleton<IAuthorizationHandler, OwnEmployeeHandler>();

/* Regras de trabalho (config) */
var rules = builder.Configuration.GetSection("WorkRules");
var rounding = rules.GetValue<int>("RoundingMinutes", 5);
var minLunch = rules.GetValue<int>("MinLunchMinutes", 60);
var lunchStart = TimeSpan.Parse(rules.GetValue<string>("LunchWindowStart", "11:00"));
var lunchEnd = TimeSpan.Parse(rules.GetValue<string>("LunchWindowEnd", "15:00"));
var maxDaily = rules.GetValue<double>("MaxDailyHours", 10);

/* DI dos seus repositórios/serviços existentes */
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IPunchRepository, PunchRepository>();
builder.Services.AddScoped<IClock, Clock>();
builder.Services.AddScoped<IPunchService, PunchService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<ITimeBankService, TimeBankService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IOnCallService, OnCallService>();
builder.Services.AddScoped<IReportQueries, ReportQueries>();
builder.Services.AddScoped<IReportService>(sp =>
    new ReportService(
        sp.GetRequiredService<IPunchRepository>(),
        sp.GetRequiredService<IEmployeeRepository>(),
        new WorkRules(rounding, minLunch, lunchStart, lunchEnd, maxDaily)
    ));

/* === NOVO: DI para multitenancy/setup/login === */
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IInviteService, InviteService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<SetupOrchestrator>();

/* Options do Setup (para endpoint /internal/setup/invite) */
builder.Services.Configure<SetupOptions>(
    builder.Configuration.GetSection("Setup")
);

var app = builder.Build();

/* Localization */
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("pt-BR"),
    SupportedCultures = new[] { new CultureInfo("pt-BR") },
    SupportedUICultures = new[] { new CultureInfo("pt-BR") }
});

/* Encaminhamento de headers (proxy/CDN) — coloque cedo no pipeline */
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

/* Inicialização do banco: migrar e seedar Roles */
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Seed de roles (Admin/Employee)
    if (!await db.Roles.AnyAsync())
    {
        db.Roles.AddRange(new PontoApp.Domain.Entities.Role { Name = "Admin" },
                          new PontoApp.Domain.Entities.Role { Name = "Employee" });
        await db.SaveChangesAsync();
    }
}

/* Erros/HSTS */
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

/* HTTPS + arquivos estáticos */
app.UseHttpsRedirection();
app.UseStaticFiles();

/* Whitelist opcional por IP */
var ipSection = app.Configuration.GetSection("IpAllowList");
if (ipSection.GetValue<bool>("Enforce", false))
{
    var allowed = ipSection.GetSection("Allowed").Get<string[]>() ?? Array.Empty<string>();
    app.Use(async (ctx, next) =>
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        if (ip.StartsWith("::ffff:", StringComparison.OrdinalIgnoreCase))
            ip = ip[7..];

        var ok = ip != string.Empty && allowed.Any(p => ip.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        if (!ok)
        {
            ctx.Response.StatusCode = 403;
            await ctx.Response.WriteAsync("Forbidden");
            return;
        }
        await next();
    });
}

/* Roteamento + SW/Manifest */
app.UseRouting();

app.MapGet("/service-worker.js", async ctx =>
{
    ctx.Response.ContentType = "application/javascript";
    await ctx.Response.SendFileAsync("wwwroot/pwa/service-worker.js");
});
app.MapGet("/manifest.webmanifest", async ctx =>
{
    ctx.Response.ContentType = "application/manifest+json";
    await ctx.Response.SendFileAsync("wwwroot/pwa/manifest.webmanifest");
});

/* AuthN/AuthZ */
app.UseAuthentication();
app.UseAuthorization();

/* Rotas MVC */
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
