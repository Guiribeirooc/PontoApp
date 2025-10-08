using System.Globalization;
using FluentValidation;
using FluentValidation.Validators;
using PontoApp.Application.Services;
using PontoApp.Domain.Enums;
using PontoApp.Web.Utils;
using PontoApp.Web.ViewModels;

namespace PontoApp.Web.Validation
{
    public class EmployeeFormViewModelValidator : AbstractValidator<EmployeeFormViewModel>
    {
        public EmployeeFormViewModelValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O nome é obrigatório.")
                .MaximumLength(80);

            RuleFor(x => x.Pin)
                .NotEmpty().WithMessage("O PIN é obrigatório.")
                .Length(4, 6)
                .Matches(@"^\d+$").WithMessage("O PIN deve conter apenas dígitos.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O e-mail é obrigatório.")
                .EmailAddress(EmailValidationMode.Net4xRegex);

            RuleFor(x => x.Cpf)
                .NotEmpty().WithMessage("Informe o CPF.")
                .Must(CpfUtils.IsValid).WithMessage("CPF inválido.");

            RuleFor(x => x.BirthDate)
                .Must(d => d <= DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("A data de nascimento não pode ser no futuro.")
                .Must(d => d >= DateOnly.FromDateTime(DateTime.Today.AddYears(-110)))
                .WithMessage("A idade não pode ser superior a 110 anos.");

            RuleFor(x => x.ShiftStart)
                .Matches(@"^\d{2}:\d{2}$").When(x => !string.IsNullOrWhiteSpace(x.ShiftStart))
                .WithMessage("Informe no formato HH:mm");
            RuleFor(x => x.ShiftEnd)
                .Matches(@"^\d{2}:\d{2}$").When(x => !string.IsNullOrWhiteSpace(x.ShiftEnd))
                .WithMessage("Informe no formato HH:mm");

            RuleFor(x => x)
                .Must(x => IsEndAfterStart(x.ShiftStart, x.ShiftEnd))
                .When(x => !string.IsNullOrWhiteSpace(x.ShiftStart) && !string.IsNullOrWhiteSpace(x.ShiftEnd))
                .WithMessage("Hora término deve ser maior que hora início.");

            RuleFor(x => x.Jornada).IsInEnum();

            // Jornadas com horário obrigatório (integral, parcial, remota, noturna, 12x36)
            When(x => x.Jornada is WorkScheduleKind.Integral
                                or WorkScheduleKind.Parcial
                                or WorkScheduleKind.Remota
                                or WorkScheduleKind.Noturna
                                or WorkScheduleKind.DozePorTrintaSeis, () =>
                                {
                                    RuleFor(x => x.ShiftStart).NotEmpty().WithMessage("Informe o início do turno.");
                                    RuleFor(x => x.ShiftEnd).NotEmpty().WithMessage("Informe o fim do turno.");

                                    RuleFor(x => new { x.ShiftStart, x.ShiftEnd, x.Jornada })
                                    .Must(s =>
                    {
                        if (!TryParseTime(s.ShiftStart, out var start)) return false;
                        if (!TryParseTime(s.ShiftEnd, out var end)) return false;

                        var dur = DurationHours(start, end); // considera virada de dia
                        var (dailyMax, _) = WorkScheduleRules.Caps(s.Jornada);
                        return dur <= dailyMax + 0.01; // tolerância
                    })
                                    .WithMessage("A duração do turno excede o limite diário para essa jornada.");
                                });

            // Intermitente: horários opcionais
            When(x => x.Jornada == WorkScheduleKind.Intermitente, () => { /* sem exigência */ });

            // Estagiário: se informar, não pode passar de 6h
            When(x => x.Jornada == WorkScheduleKind.Estagiario, () =>
            {
                RuleFor(x => new { x.ShiftStart, x.ShiftEnd })
                    .Must(s =>
                    {
                        // se não informou os dois, ok
                        if (string.IsNullOrWhiteSpace(s.ShiftStart) ||
                            string.IsNullOrWhiteSpace(s.ShiftEnd)) return true;

                        if (!TryParseTime(s.ShiftStart, out var start)) return false;
                        if (!TryParseTime(s.ShiftEnd, out var end)) return false;

                        return DurationHours(start, end) <= 6.0 + 0.01;
                    })
                    .WithMessage("Estagiário não pode ultrapassar 6h diárias.");
            });
        }

        // Tenta parsear "HH:mm" (e aceita "HH:mm:ss" se vier)
        private static bool TryParseTime(string? value, out TimeSpan time)
        {
            time = default;
            if (string.IsNullOrWhiteSpace(value)) return false;

            // Alguns navegadores enviam "HH:mm", outros podem enviar "HH:mm:ss"
            return TimeSpan.TryParseExact(value, new[] { "hh\\:mm", "hh\\:mm\\:ss", "HH\\:mm", "HH\\:mm\\:ss" },
                                          CultureInfo.InvariantCulture, out time);
        }

        // Calcula duração em horas, considerando virada de dia
        private static double DurationHours(TimeSpan start, TimeSpan end)
        {
            var dur = end >= start ? end - start : (TimeSpan.FromHours(24) - start) + end;
            return dur.TotalHours;
        }

        private static bool IsEndAfterStart(string? start, string? end)
        {
            if (!TimeOnly.TryParse(start, out var s)) return true;
            if (!TimeOnly.TryParse(end, out var e)) return true;
            return e > s;
        }
    }
}