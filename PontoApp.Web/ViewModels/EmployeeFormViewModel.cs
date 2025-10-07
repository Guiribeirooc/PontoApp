namespace PontoApp.Web.ViewModels
{
    public class EmployeeFormViewModel
    {
        public int? Id { get; set; }
        public string Nome { get; set; } = "";
        public string Pin { get; set; } = "";
        public string Cpf { get; set; } = "";
        public string Email { get; set; } = "";
        public DateOnly BirthDate { get; set; }
        public bool Ativo { get; set; } = true;
        public string? ShiftStart { get; set; }
        public string? ShiftEnd { get; set; }
        public IFormFile? Foto { get; set; }
        public string? FotoAtualPath { get; set; }
    }
}
