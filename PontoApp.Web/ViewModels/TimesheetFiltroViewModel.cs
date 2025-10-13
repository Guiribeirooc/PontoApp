using Microsoft.AspNetCore.Mvc.Rendering;

namespace PontoApp.Web.ViewModels;

public sealed class TimesheetFiltroViewModel
{
    public DateOnly Inicio { get; set; }
    public DateOnly Fim { get; set; }
    public int? EmployeeId { get; set; }
    public List<SelectListItem> Employees { get; set; } = new();

    public bool IsAdmin { get; set; }

    public string? EmployeeName { get; set; }
    public List<EspelhoDiaViewModel> Dias { get; set; } = new();
}