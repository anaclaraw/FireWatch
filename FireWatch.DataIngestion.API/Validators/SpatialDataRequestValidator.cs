using FireWatch.DataIngestion.API.DTOs;
using FluentValidation;

namespace FireWatch.DataIngestion.API.Validators;

public class SpatialDataRequestValidator : AbstractValidator<SpatialDataRequest>
{
    private static readonly string[] ValidSources =
        ["NasaFirms", "Inpe", "OpenMeteo", "OpenAQ"];

    private static readonly string[] ValidDayNight = ["D", "N"];

    public SpatialDataRequestValidator()
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

        RuleFor(x => x.Source)
            .NotEmpty()
            .Must(s => ValidSources.Contains(s))
            .WithMessage($"Source inválido. Valores aceitos: {string.Join(", ", ValidSources)}");

        RuleFor(x => x.DayNight)
            .Must(d => ValidDayNight.Contains(d?.ToUpper()))
            .WithMessage("DayNight deve ser 'D' (day) ou 'N' (night).");

        RuleFor(x => x.AcquiredAt)
            .NotEmpty()
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("AcquiredAt não pode ser uma data futura.");
    }
}