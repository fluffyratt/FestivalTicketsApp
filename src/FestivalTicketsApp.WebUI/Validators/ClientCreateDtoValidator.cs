using FestivalTicketsApp.Application.ClientService.DTO;
using FestivalTicketsApp.Infrastructure.Data.Configs;
using FestivalTicketsApp.Shared;
using FluentValidation;

namespace FestivalTicketsApp.WebUI.Validators;

public class ClientCreateDtoValidator : AbstractValidator<ClientCreateDto>
{
    public ClientCreateDtoValidator()
    {
        RuleFor(dto => dto.Email)
            .EmailAddress();

        RuleFor(dto => dto.Phone)
            .Matches(RegexTemplates.UkrPhoneNumberTemplate);

        RuleFor(dto => dto.Name)
            .NotEmpty()
            .MaximumLength(DataSchemeConstants.DefaultNameLength);
        
        RuleFor(dto => dto.Surname)
            .NotEmpty()
            .MaximumLength(DataSchemeConstants.DefaultNameLength);

        RuleFor(dto => dto.Subject)
            .NotEmpty();
    }
}