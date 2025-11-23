namespace FestivalTicketsApp.Core.Entities;

public class TicketStatus : BaseEntity
{
    public string Status { get; set; } = default!;

    public List<Ticket> TicketsWithStatus { get; set; } = [];
}