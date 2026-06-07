using FluentValidation;

namespace FireWatch.Gateway.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress()
            .WithMessage("E-mail inválido.");

        RuleFor(x => x.Password)
            .NotEmpty().MinimumLength(6)
            .WithMessage("Senha deve ter ao menos 6 caracteres.");
    }
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(150)
            .WithMessage("Nome é obrigatório.");

        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress()
            .WithMessage("E-mail inválido.");

        RuleFor(x => x.Password)
            .NotEmpty().MinimumLength(6)
            .WithMessage("Senha deve ter ao menos 6 caracteres.");
    }
}