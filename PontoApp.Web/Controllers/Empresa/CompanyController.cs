using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoApp.Application.DTOs;
using PontoApp.Web.Support;
using PontoApp.Web.ViewModels;

namespace PontoApp.Web.Controllers.Empresa
{
    [Authorize(Roles = "Admin")]
    [Route("empresa/company")]
    public class CompanyController(ICompanyService companies) : EmpresaControllerBase
    {
        private readonly ICompanyService _companies = companies;

        [HttpGet("create")]
        public IActionResult Create() => View(new CompanyCreateViewModel());

        [HttpPost("create"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CompanyCreateViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(vm);

            var dto = new CreateCompanyDto(
                vm.Name, vm.CNPJ, vm.IE, vm.IM,
                vm.Logradouro, vm.Numero, vm.Complemento,
                vm.Bairro, vm.Cidade, vm.UF, vm.CEP, vm.Pais,
                vm.Telefone, vm.EmailContato,
                await vm.Logo.ToBytesAsync(ct)
            );

            var companyId = await _companies.CreateAsync(dto, ct);

            await AddOrReplaceCompanyClaimAsync(companyId);

            TempData["ok"] = "Empresa criada com sucesso.";
            return RedirectToAction(nameof(Details));
        }

        [HttpGet("")]
        public async Task<IActionResult> Details(CancellationToken ct)
        {
            if (CompanyId <= 0) return Redirect("/account/login");

            var c = await _companies.GetByIdAsync(CompanyId, ct);
            if (c is null) return NotFound();

            var vm = new CompanyDetailsViewModel
            {
                Id = c.Id,
                Name = c.Name,
                CNPJ = c.CNPJ,
                IE = c.IE,
                IM = c.IM,
                Logradouro = c.Logradouro,
                Numero = c.Numero,
                Complemento = c.Complemento,
                Bairro = c.Bairro,
                Cidade = c.Cidade,
                UF = c.UF,
                CEP = c.CEP,
                Pais = c.Pais,
                Telefone = c.Telefone,
                EmailContato = c.EmailContato,
                LogoBase64 = c.Logo is { Length: > 0 } ? $"data:image/png;base64,{Convert.ToBase64String(c.Logo)}" : null
            };

            return View(vm);
        }

        [HttpGet("edit")]
        public async Task<IActionResult> Edit(CancellationToken ct)
        {
            if (CompanyId <= 0) return Redirect("/account/login");

            var c = await _companies.GetByIdAsync(CompanyId, ct);
            if (c is null) return NotFound();

            return View(new CompanyEditViewModel
            {
                Id = c.Id,
                Name = c.Name,
                CNPJ = c.CNPJ,
                IE = c.IE,
                IM = c.IM,
                Logradouro = c.Logradouro,
                Numero = c.Numero,
                Complemento = c.Complemento,
                Bairro = c.Bairro,
                Cidade = c.Cidade,
                UF = c.UF,
                CEP = c.CEP,
                Pais = c.Pais,
                Telefone = c.Telefone,
                EmailContato = c.EmailContato,
                ExistingLogoBase64 = c.Logo is { Length: > 0 } ? $"data:image/png;base64,{Convert.ToBase64String(c.Logo)}" : null
            });
        }

        [HttpPost("edit"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CompanyEditViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(vm);
            if (CompanyId <= 0) return Redirect("/account/login");
            if (vm.Id != CompanyId) return Forbid();

            byte[]? logoBytes = await vm.Logo?.ToBytesAsync(ct)!;

            var dto = new UpdateCompanyDto(
                vm.Id,
                vm.Name, vm.CNPJ, vm.IE, vm.IM,
                vm.Logradouro, vm.Numero, vm.Complemento,
                vm.Bairro, vm.Cidade, vm.UF, vm.CEP, vm.Pais,
                vm.Telefone, vm.EmailContato,
                logoBytes, keepExistingLogo: logoBytes is null, vm.Active == "on"
            );

            await _companies.UpdateAsync(dto, ct);

            TempData["ok"] = "Empresa atualizada.";
            return RedirectToAction(nameof(Details));
        }

        private async Task AddOrReplaceCompanyClaimAsync(int companyId)
        {
            var claims = User.Claims.ToList();
            var existing = claims.FirstOrDefault(c => c.Type == "CompanyId");
            if (existing != null) claims.Remove(existing);
            claims.Add(new System.Security.Claims.Claim("CompanyId", companyId.ToString()));

            var identity = new System.Security.Claims.ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new System.Security.Claims.ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true, AllowRefresh = true });
        }
    }
}
