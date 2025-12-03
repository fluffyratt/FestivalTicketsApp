namespace FestivalTicketsApp.Application.EventService.DTO;

public class TicketSeatDto
{
    public int Id { get; set; }
    public int? Row { get; set; }
    public int? Seat { get; set; }
    public string? Status { get; set; }
    public decimal Price { get; set; }

    public TicketSeatDto(int id, int? row, int? seat, string? status, decimal price)
    {
        Id = id;
        Row = row;
        Seat = seat;
        Status = status;
        Price = price;
    }
}
