namespace FestivalTicketsApp.WebUI.Models.Client;

public class TicketInfoViewModel
{
    public int Id { get; set; }

    // TODO: коли подивишся на сутність Ticket,
    // можна додати сюди інші поля: EventName, StartTime, SeatNumber, Price і т.д.
}

public class UserProfileViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public List<TicketInfoViewModel> Tickets { get; set; } = new();
}
