using FireWatch.RiskAnalysis.DTOs;
using FluentValidation;

namespace FireWatch.RiskAnalysis.Validators;

public class ManualRiskRequestValidator : AbstractValidator<ManualRiskRequest>
{
    public ManualRiskRequestValidator()
    {
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Latitude deve estar entre -90 e 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Longitude deve estar entre -180 e 180.");

        RuleFor(x => x.Brightness)
            .GreaterThan(0)
            .WithMessage("Brightness deve ser maior que zero.");

        RuleFor(x => x.Confidence)
            .InclusiveBetween(0, 100)
            .WithMessage("Confidence deve estar entre 0 e 100.");

        RuleFor(x => x.DayNight)
            .Must(d => d is "D" or "N")
            .WithMessage("DayNight deve ser 'D' ou 'N'.");
    }
}