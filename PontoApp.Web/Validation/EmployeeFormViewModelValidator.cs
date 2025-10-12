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

                        var dur = DurationHours(start, end);             
                        var lunch = GetLunchHours(s.Jornada);            
                        var worked = Math.Max(0.0, dur - lunch);       

                        var (dailyMax, _) = WorkScheduleRules.Caps(s.Jornada);
                        return worked <= dailyMax + 0.01;                
                    })
                            .WithMessage(x =>
                    {
                        TryParseTime(x.ShiftStart, out var start);
                        TryParseTime(x.ShiftEnd, out var end);
                        var dur = DurationHours(start, end);
                        var lunch = GetLunchHours(x.Jornada);
                        var worked = Math.Max(0.0, dur - lunch);
                        var (dailyMax, _) = WorkScheduleRules.Caps(x.Jornada);

                        return $"A duração líquida do turno ({worked:0.##}h, já descontando {lunch:0.##}h de almoço) " +
                               $"excede o limite diário da jornada ({dailyMax:0.##}h).";
                    });
                                });

            When(x => x.Jornada == WorkScheduleKind.Intermitente, () => { /* sem exigência */ });

            When(x => x.Jornada == WorkScheduleKind.Estagiario, () =>
            {
                RuleFor(x => new { x.ShiftStart, x.ShiftEnd })
                    .Must(s =>
                    {
                        if (string.IsNullOrWhiteSpace(s.ShiftStart) ||
                            string.IsNullOrWhiteSpace(s.ShiftEnd)) return true;

                        if (!TryParseTime(s.ShiftStart, out var start)) return false;
                        if (!TryParseTime(s.ShiftEnd, out var end)) return false;

                        return DurationHours(start, end) <= 6.0 + 0.01;
                    })
                    .WithMessage("Estagiário não pode ultrapassar 6h diárias.");
            });

            RuleFor(x => x.HourlyRate).GreaterThanOrEqualTo(0).When(x => x.HourlyRate.HasValue);

            RuleFor(x => x.AdmissionDate).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
                .When(x => x.AdmissionDate.HasValue)
                .WithMessage("A data de admissão não pode ser no futuro.");

            RuleFor(x => x.TrackingStart).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today));

            RuleFor(x => x.TrackingEnd).GreaterThanOrEqualTo(x => x.TrackingStart)
                .When(x => x.TrackingStart.HasValue && x.TrackingEnd.HasValue)
                .WithMessage("A data final do acompanhamento deve ser maior ou igual à inicial.");

            RuleFor(x => x.VacationAccrualStart).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today));

        }

        private static bool TryParseTime(string? value, out TimeSpan time)
        {
            time = default;
            if (string.IsNullOrWhiteSpace(value)) return false;

            return TimeSpan.TryParseExact(value, new[] { "hh\\:mm", "hh\\:mm\\:ss", "HH\\:mm", "HH\\:mm\\:ss" },
                                          CultureInfo.InvariantCulture, out time);
        }

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

        private static double GetLunchHours(WorkScheduleKind kind)
        {
            return kind switch
            {
                WorkScheduleKind.Integral => 1.0, 
                WorkScheduleKind.Parcial => 0.5, 
                WorkScheduleKind.Noturna => 1.0,
                WorkScheduleKind.Remota => 1.0,
                WorkScheduleKind.DozePorTrintaSeis => 1.0, 
                _ => 0.0
            };
        }
    }
}