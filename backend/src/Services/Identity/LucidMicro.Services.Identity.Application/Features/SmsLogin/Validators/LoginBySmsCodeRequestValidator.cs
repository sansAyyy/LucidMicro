using FluentValidation;
using LucidMicro.BuildingBlocks.Application.Validation;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Dtos.Requests;

namespace LucidMicro.Services.Identity.Application.Features.SmsLogin.Validators;

public sealed class LoginBySmsCodeRequestValidator : AbstractValidator<LoginBySmsCodeRequest>
{
    public LoginBySmsCodeRequestValidator()
    {
        RuleFor(request => request.PhoneNumber)
            .Must(TextValidationRules.BeRequired)
            .WithMessage("phoneNumber is required.")
            .Must(value => TextValidationRules.HaveTrimmedMaxLength(value, 32))
            .WithMessage("phoneNumber exceeds max length 32.");

        RuleFor(request => request.Code)
            .Must(TextValidationRules.BeRequired)
            .WithMessage("code is required.")
            .Must(value => TextValidationRules.HaveTrimmedMaxLength(value, 16))
            .WithMessage("code exceeds max length 16.");
    }
}
