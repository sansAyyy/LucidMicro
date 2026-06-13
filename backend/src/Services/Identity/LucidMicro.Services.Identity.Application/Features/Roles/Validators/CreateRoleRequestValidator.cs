using FluentValidation;
using LucidMicro.BuildingBlocks.Application.Validation;
using LucidMicro.Services.Identity.Application.Features.Roles.Dtos.Requests;

namespace LucidMicro.Services.Identity.Application.Features.Roles.Validators;

public sealed class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(request => request.Code)
            .Must(TextValidationRules.BeRequired)
            .WithMessage("code is required.")
            .Must(value => TextValidationRules.HaveTrimmedMaxLength(value, 64))
            .WithMessage("code exceeds max length 64.");

        RuleFor(request => request.Name)
            .Must(TextValidationRules.BeRequired)
            .WithMessage("name is required.")
            .Must(value => TextValidationRules.HaveTrimmedMaxLength(value, 128))
            .WithMessage("name exceeds max length 128.");

        RuleFor(request => request.Description)
            .Must(value => TextValidationRules.BeOptionalWithTrimmedMaxLength(value, 512))
            .WithMessage("description exceeds max length 512.");
    }
}
