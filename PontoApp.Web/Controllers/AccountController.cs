using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PontoApp.Application.Contracts;
using PontoApp.Domain.Interfaces;
using PontoApp.Infrastructure.Security;
using PontoApp.Web.ViewModels;

namespace PontoApp.Web.Controllers
{
    public class AccountController(
        IUserRepository users,
        IEmployeeRepository emps,
        IEmailSender email,
        IConfiguration cfg,
        ILogger<AccountController> logger) : Controller
    {
        private readonly IUserRepository _users = users;
        private readonly IEmployeeRepository _emps = emps;
        private readonly IEmailSender _email = email;
        private readonly string _masterEmail = cfg.GetSection("Auth")["MasterAdminEmail"] ?? "";
        private readonly ILogger<AccountController> _logger = logger;

        [HttpGet, AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return User.IsInRole("Admin")
                    ? RedirectToAction("Index", "Home")
                    : RedirectToAction("Meu", "Punch");
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(vm);

            var email = (vm.Email ?? string.Empty).Trim().ToLowerInvariant();
            var user = await _users.GetByEmailAsync(email, ct);

            if (user is null || user.IsDeleted ||
                !PasswordHasher.Verify(vm.Password, user.PasswordHash, user.PasswordSalt))
            {
                ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
                return View(vm);
            }

            var isAdmin = !string.IsNullOrWhiteSpace(_masterEmail) &&
                          string.Equals(user.Email, _masterEmail, StringComparison.OrdinalIgnoreCase);

            var displayName = user.Email;

            if (!isAdmin && user.EmployeeId.HasValue)
            {
                var empName = await _emps.Query()
                    .Where(e => e.Id == user.EmployeeId.Value && e.Ativo && !e.IsDeleted)
                    .Select(e => e.Nome)
                    .FirstOrDefaultAsync(ct);

                if (!string.IsNullOrWhiteSpace(empName))
                    displayName = empName!;
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, displayName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, isAdmin ? "Admin" : "Employee")
            };

            if (user.EmployeeId.HasValue)
                claims.Add(new Claim("EmployeeId", user.EmployeeId.Value.ToString()));

            if (user.EmployeeId.HasValue)
            {
                var active = await _emps.Query()
                    .Where(e => e.Id == user.EmployeeId.Value)
                    .Select(e => e.Ativo && !e.IsDeleted)
                    .FirstOrDefaultAsync(ct);

                claims.Add(new Claim("employee_active", active ? "true" : "false"));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = vm.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14),
                    AllowRefresh = true
                });

            if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                return Redirect(vm.ReturnUrl);

            return isAdmin
                ? RedirectToAction("Index", "Home")
                : RedirectToAction("Meu", "Punch");
        }

        [HttpGet, Authorize(Policy = "RequireAdmin")]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost, Authorize(Policy = "RequireAdmin"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(vm);

            var email = vm.Email.Trim().ToLowerInvariant();
            var existing = await _users.GetByEmailAsync(email, ct);
            if (existing != null)
            {
                ModelState.AddModelError(nameof(vm.Email), "E-mail já cadastrado.");
                return View(vm);
            }

            var (hash, salt) = PasswordHasher.Hash(vm.Password);

            var user = new Domain.Entities.AppUser
            {
                Email = email,
                PasswordHash = hash,
                PasswordSalt = salt,
                EmployeeId = vm.EmployeeId
            };

            await _users.AddAsync(user, ct);
            await _users.SaveAsync(ct);

            TempData["ok"] = "Usuário cadastrado.";
            return RedirectToAction("Register");
        }

        [HttpGet, AllowAnonymous]
        public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(vm);

            try
            {
                var email = vm.Email.Trim().ToLowerInvariant();
                var user = await _users.GetByEmailAsync(email, ct);

                if (user != null && !user.IsDeleted)
                {
                    var now = DateTimeOffset.UtcNow;

                    var recentlySent = user.ResetCodeExpiresAt.HasValue &&
                                       user.ResetCodeExpiresAt.Value - now > TimeSpan.FromMinutes(14);

                    var code = recentlySent && !string.IsNullOrWhiteSpace(user.ResetCode)
                        ? user.ResetCode!
                        : Random.Shared.Next(100000, 999999).ToString();

                    user.ResetCode = code;
                    user.ResetCodeExpiresAt = now.AddMinutes(15);
                    await _users.SaveAsync(ct);

                    var html =
                        $@"<p>Seu código de redefinição é <strong>{code}</strong>.</p>
                   <p>Ele expira em 15 minutos.</p>";

                    var (success, message) =
                        await _email.SendAsync(user.Email, "PontoApp - Código de Redefinição", html, ct);

                    if (!success)
                    {
                        _logger.LogWarning("Falha ao enviar e-mail de reset para {Email}: {Message}", user.Email, message);
                    }
                }

                TempData["ok"] = "Se o e-mail existir, enviamos um código de validação.";
                return RedirectToAction(nameof(ResetPassword), new { email = vm.Email });
            }
            catch (OperationCanceledException)
            {
                TempData["ok"] = "Se o e-mail existir, enviamos um código de validação.";
                return RedirectToAction(nameof(ForgotPassword));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no fluxo de ForgotPassword.");
                TempData["ok"] = "Se o e-mail existir, enviamos um código de validação.";
                return RedirectToAction(nameof(ForgotPassword));
            }
        }

        [HttpGet, AllowAnonymous]
        public IActionResult ResetPassword(string? email = null)
            => View(new ResetPasswordViewModel { Email = email ?? string.Empty });

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(vm);

            var email = vm.Email.Trim().ToLowerInvariant();
            var user = await _users.GetByEmailAsync(email, ct);

            var now = DateTimeOffset.Now;
            var isInvalid =
                user is null ||
                user.IsDeleted ||
                string.IsNullOrWhiteSpace(user.ResetCode) ||
                !user.ResetCodeExpiresAt.HasValue ||
                user.ResetCodeExpiresAt.Value < now ||
                !string.Equals(user.ResetCode, vm.Code, StringComparison.Ordinal);

            if (isInvalid)
            {
                ModelState.AddModelError("", "Código inválido ou expirado.");
                return View(vm);
            }

            var (hash, salt) = PasswordHasher.Hash(vm.NewPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.ResetCode = null;
            user.ResetCodeExpiresAt = null;

            await _users.SaveAsync(ct);
            TempData["ok"] = "Senha redefinida com sucesso.";
            return RedirectToAction("Login");
        }

        [HttpGet, AllowAnonymous]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> SetupAdmin()
        {
            var hasUsers = await _users.Query().AnyAsync();
            if (hasUsers) return NotFound();

            if (User?.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            if (string.IsNullOrWhiteSpace(_masterEmail))
                return BadRequest("MasterAdminEmail não configurado no appsettings.");

            var vm = new AdminBootstrapViewModel { Email = _masterEmail };
            return View(vm);
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetupAdmin(AdminBootstrapViewModel vm, CancellationToken ct)
        {
            var hasUsers = _users.Query().Any();
            if (hasUsers) return NotFound();

            if (!ModelState.IsValid) return View(vm);

            if (!string.Equals(vm.Email.Trim().ToLowerInvariant(), _masterEmail.Trim().ToLowerInvariant()))
            {
                ModelState.AddModelError(nameof(vm.Email), "O e-mail deve ser o MasterAdmin configurado.");
                return View(vm);
            }

            var existing = await _users.GetByEmailAsync(_masterEmail.ToLowerInvariant(), ct);
            if (existing != null)
            {
                TempData["ok"] = "Administrador já existe. Faça login.";
                return RedirectToAction(nameof(Login));
            }

            var (hash, salt) = PasswordHasher.Hash(vm.Password);
            var user = new Domain.Entities.AppUser
            {
                Email = _masterEmail.ToLowerInvariant(),
                PasswordHash = hash,
                PasswordSalt = salt,
                EmployeeId = null
            };

            await _users.AddAsync(user, ct);
            await _users.SaveAsync(ct);

            TempData["ok"] = "Administrador criado com sucesso. Faça login.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet, AllowAnonymous]
        public IActionResult RegisterEmployee() => View(new RegisterEmployeeViewModel());

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterEmployee(RegisterEmployeeViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(vm);

            var email = vm.Email.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(_masterEmail) &&
                string.Equals(email, _masterEmail, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(vm.Email), "E-mail reservado para administrador.");
                return View(vm);
            }

            var exists = await _users.GetByEmailAsync(email, ct);
            if (exists != null)
            {
                ModelState.AddModelError(nameof(vm.Email), "E-mail já cadastrado.");
                return View(vm);
            }

            var emp = _emps.Query().FirstOrDefault(e => e.Pin == vm.Pin && e.Ativo && !e.IsDeleted);
            if (emp is null)
            {
                ModelState.AddModelError(nameof(vm.Pin), "PIN inválido ou colaborador inativo.");
                return View(vm);
            }

            var (hash, salt) = PasswordHasher.Hash(vm.Password);
            var user = new Domain.Entities.AppUser
            {
                Email = email,
                PasswordHash = hash,
                PasswordSalt = salt,
                EmployeeId = emp.Id
            };

            await _users.AddAsync(user, ct);
            await _users.SaveAsync(ct);

            TempData["ok"] = "Cadastro realizado. Faça login.";
            return RedirectToAction(nameof(Login));
        }
    }
}