using FluentValidation;
using LucidMicro.BuildingBlocks.Application.Validation;
using LucidMicro.Services.Identity.Application.Features.Roles.Dtos.Requests;

namespace LucidMicro.Services.Identity.Application.Features.Roles.Validators;

public sealed class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
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
