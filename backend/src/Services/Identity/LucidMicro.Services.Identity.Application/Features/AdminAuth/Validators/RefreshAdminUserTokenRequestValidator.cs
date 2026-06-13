using FluentValidation;
using LucidMicro.BuildingBlocks.Application.Validation;
using LucidMicro.Services.Identity.Application.Features.AdminAuth.Dtos.Requests;

namespace LucidMicro.Services.Identity.Application.Features.AdminAuth.Validators;

public sealed class RefreshAdminUserTokenRequestValidator : AbstractValidator<RefreshAdminUserTokenRequest>
{
    public RefreshAdminUserTokenRequestValidator()
    {
        RuleFor(request => request.RefreshToken)
            .Must(TextValidationRules.BeRequired)
            .WithMessage("refreshToken is required.")
            .Must(value => TextValidationRules.HaveTrimmedMaxLength(value, 4096))
            .WithMessage("refreshToken exceeds max length 4096.");
    }
}
