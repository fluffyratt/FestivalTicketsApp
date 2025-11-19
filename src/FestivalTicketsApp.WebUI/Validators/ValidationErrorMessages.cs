namespace FestivalTicketsApp.WebUI.Validators;

public static class ValidationErrorMessages
{
    public const string EmptyEventDescriptionMessage = "Details about event must be provided";
    public const string EmptyEventTitleMessage = "Title must be provided";
    public const string EventDurationRangeMessage = "Duration time must be defined in minutes and be greater than 0";

    public const string EmptyTicketTypeCollectionMessage = "At least one ticket type must be created";
    public const string EmptyTicketTypeNameMessage = "Provide name for ticket type";
    public const string PriceRangeMessage = "Price must be greater than 0";
    public const string TicketTypeAssingMessage = "Type must be assigned to seats";
}
