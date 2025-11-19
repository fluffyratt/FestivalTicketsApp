using FestivalTicketsApp.Application.EventService.DTO;
using FluentValidation;
using FluentValidation.Results;

namespace FestivalTicketsApp.WebUI.Validators;

public class PlaneEventDtoValidator : AbstractValidator<PlaneEventDto>
{
    public PlaneEventDtoValidator(IValidator<CreateTicketTypeDto> ticketTypeValidator)
    {
        RuleFor(e => e.Description)
            .NotEmpty()
            .WithMessage(ValidationErrorMessages.EmptyEventDescriptionMessage);

        RuleFor(e => e.Title)
            .NotEmpty()
            .WithMessage(ValidationErrorMessages.EmptyEventTitleMessage);

        RuleFor(e => e.Duration)
            .NotEmpty()
            .GreaterThan(0)
            .WithMessage(ValidationErrorMessages.EventDurationRangeMessage);

        RuleFor(e => e.TicketTypes)
            .NotEmpty()
            .WithMessage(ValidationErrorMessages.EmptyTicketTypeCollectionMessage);

        RuleFor(e => e)
            .Must(e =>
            {
                HashSet<string> ticketTypeNames = e?.TicketTypes
                    ?.Select(tt => tt.Name)
                    .ToHashSet() ?? new();

                HashSet<string> hallRowTicketTypes = e?.TypesMapping
                    ?.ToHashSet() ?? new();

                return ticketTypeNames.SetEquals(hallRowTicketTypes);
            })
            .WithMessage(ValidationErrorMessages.TicketTypeAssingMessage);

        RuleForEach(e => e.TicketTypes).SetValidator(ticketTypeValidator);
    }

    public override ValidationResult Validate(ValidationContext<PlaneEventDto> context)
    {
        ValidationResult? result = base.Validate(context);

        ArgumentNullException.ThrowIfNull(result);

        if (result.Errors.Count > 0)
        {
            result.Errors = result.Errors.DistinctBy(err => err.ErrorMessage).ToList();
        }

        return result;
    }
}

public class CreateTicketTypeDtoValidator : AbstractValidator<CreateTicketTypeDto>
{
    public CreateTicketTypeDtoValidator()
    {
        RuleFor(tt => tt.Name)
            .NotEmpty()
            .WithMessage(ValidationErrorMessages.EmptyTicketTypeNameMessage);

        RuleFor(tt => tt.Price)
            .GreaterThan(0)
            .WithMessage(ValidationErrorMessages.PriceRangeMessage);
    }
}
