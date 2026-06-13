using FluentValidation;
using LucidMicro.BuildingBlocks.Application.Validation;
using LucidMicro.Services.__ServiceName__.Application.Features.__FeatureName__.Dtos.Requests;

namespace LucidMicro.Services.__ServiceName__.Application.Features.__FeatureName__.Validators;

public sealed class Create__EntityName__RequestValidator : AbstractValidator<Create__EntityName__Request>
{
    public Create__EntityName__RequestValidator()
    {
        RuleFor(request => request.Name)
            .Must(TextValidationRules.BeRequired)
            .WithMessage("name is required.")
            .Must(value => TextValidationRules.HaveTrimmedMaxLength(value, __NameMaxLength__))
            .WithMessage("name exceeds max length __NameMaxLength__.");
    }
}
