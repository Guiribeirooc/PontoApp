using PontoApp.Domain.Entities;

namespace PontoApp.Application.DTOs
{
    public sealed class EspelhoDiaDto
    {
        public DateOnly Dia { get; set; }
        public List<EspelhoMarcacaoDto> Marcas { get; set; } = new();
    }

    public sealed class EspelhoMarcacaoDto
    {
        public PunchType Tipo { get; set; }
        public TimeSpan Hora { get; set; }
    }
}