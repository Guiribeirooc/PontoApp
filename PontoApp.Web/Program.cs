using System.Globalization;
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
using PontoApp.Infrastructure.Security;
using PontoApp.Infrastructure.Seed;
using PontoApp.Infrastructure.Services;
using PontoApp.Web.Authorization;
using PontoApp.Web.Controllers;
using PontoApp.Web.Middleware;
using PontoApp.Web.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews(o =>
    {
        o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddViewOptions(o => o.HtmlHelperOptions.ClientValidationEnabled = true);

builder.Services.AddLocalization();

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));

    if (builder.Environment.IsDevelopment())
    {
        opt.EnableDetailedErrors();
        opt.EnableSensitiveDataLogging();
        opt.LogTo(Console.WriteLine, LogLevel.Information);
    }
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "keys")))
    .SetApplicationName("PontoApp");

builder.Services.AddFluentValidationAutoValidation(o =>
{
    o.DisableDataAnnotationsValidation = true;
});
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterEmployeeViewModelValidator>();
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
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
    .AddPolicy("RequireAdmin", p => p
        .RequireAuthenticatedUser()
        .RequireRole("Admin")
        .RequireClaim("CompanyId"))  
    .AddPolicy("OwnEmployee", p => p.AddRequirements(new OwnEmployeeRequirement()));
builder.Services.AddSingleton<IAuthorizationHandler, OwnEmployeeHandler>();

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
builder.Services.AddScoped<IOnboardingService, OnboardingService>();
builder.Services.AddScoped<IReportService>(sp =>
    new ReportService(
        sp.GetRequiredService<IPunchRepository>(),
        sp.GetRequiredService<IEmployeeRepository>(),
        new WorkRules(
            builder.Configuration.GetValue<int>("WorkRules:RoundingMinutes", 5),
            builder.Configuration.GetValue<int>("WorkRules:MinLunchMinutes", 60),
            TimeSpan.Parse(builder.Configuration.GetValue<string>("WorkRules:LunchWindowStart", "11:00")),
            TimeSpan.Parse(builder.Configuration.GetValue<string>("WorkRules:LunchWindowEnd", "15:00")),
            builder.Configuration.GetValue<double>("WorkRules:MaxDailyHours", 10)
        )
    ));

builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IInviteService, InviteService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<SetupOrchestrator>();
builder.Services.Configure<IpAllowListOptions>(builder.Configuration.GetSection("IpAllowList"));
builder.Services.Configure<SetupOptions>(builder.Configuration.GetSection("Setup"));

var app = builder.Build();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("pt-BR"),
    SupportedCultures = new[] { new CultureInfo("pt-BR") },
    SupportedUICultures = new[] { new CultureInfo("pt-BR") }
});

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await RoleSeeder.SeedAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMiddleware<IpAllowListMiddleware>();
app.UseStaticFiles();
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
