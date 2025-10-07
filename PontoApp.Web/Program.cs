using System.Globalization;
using System.Security.Claims;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using PontoApp.Application.Contracts;
using PontoApp.Application.Services;
using PontoApp.Domain.Interfaces;
using PontoApp.Infrastructure.EF;
using PontoApp.Infrastructure.Repositories;
using PontoApp.Infrastructure.Services;
using PontoApp.Web.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews(options =>
    {
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddViewOptions(o => o.HtmlHelperOptions.ClientValidationEnabled = true);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IPunchRepository, PunchRepository>();
builder.Services.AddScoped<IClock, Clock>();
builder.Services.AddScoped<IPunchService, PunchService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "keys")))
    .SetApplicationName("PontoApp");

builder.Services.AddFluentValidationAutoValidation(o =>
{
    o.DisableDataAnnotationsValidation = true;
});
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterEmployeeViewModelValidator>();

ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("pt-BR");

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
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdmin", p => p.RequireClaim(ClaimTypes.Role, "Admin"))
    .AddPolicy("OwnEmployee", p => p.AddRequirements(new OwnEmployeeRequirement()));
builder.Services.AddSingleton<IAuthorizationHandler, OwnEmployeeHandler>();

var rules = builder.Configuration.GetSection("WorkRules");
var rounding = rules.GetValue<int>("RoundingMinutes", 5);
var minLunch = rules.GetValue<int>("MinLunchMinutes", 60);
var lunchStart = TimeSpan.Parse(rules.GetValue<string>("LunchWindowStart", "11:00"));
var lunchEnd = TimeSpan.Parse(rules.GetValue<string>("LunchWindowEnd", "15:00"));
var maxDaily = rules.GetValue<double>("MaxDailyHours", 10);

builder.Services.AddScoped<IReportService>(sp =>
    new ReportService(
        sp.GetRequiredService<IPunchRepository>(),
        sp.GetRequiredService<IEmployeeRepository>(),
        new WorkRules(rounding, minLunch, lunchStart, lunchEnd, maxDaily)
    ));

builder.Services.AddLocalization();

var app = builder.Build();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("pt-BR"),
    SupportedCultures = new[] { new CultureInfo("pt-BR") },
    SupportedUICultures = new[] { new CultureInfo("pt-BR") }
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.Employees.Any())
    {
        var cfg = app.Configuration.GetSection("BootstrapAdmin");
        if (cfg.GetValue<bool>("Enabled", true))
        {
            db.Employees.Add(new PontoApp.Domain.Entities.Employee
            {
                Nome = cfg.GetValue<string>("Name", "Admin"),
                Pin = cfg.GetValue<string>("Pin", "9999"),
                Ativo = true,
                IsAdmin = true
            });
            db.SaveChanges();
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
