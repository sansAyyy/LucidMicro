using FluentValidation;
using LucidMicro.BuildingBlocks.Application.Validation;
using LucidMicro.Services.Identity.Application.Features.SmsLogin.Dtos.Requests;

namespace LucidMicro.Services.Identity.Application.Features.SmsLogin.Validators;

public sealed class SendSmsLoginCodeRequestValidator : AbstractValidator<SendSmsLoginCodeRequest>
{
    public SendSmsLoginCodeRequestValidator()
    {
        RuleFor(request => request.PhoneNumber)
            .Must(TextValidationRules.BeRequired)
            .WithMessage("phoneNumber is required.")
            .Must(value => TextValidationRules.HaveTrimmedMaxLength(value, 32))
            .WithMessage("phoneNumber exceeds max length 32.");
    }
}
