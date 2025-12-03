namespace FestivalTicketsApp.Application.EventService.DTO;

public class SeatDto
{
    public int Id { get; set; }
    public int? Row { get; set; }
    public int? Seat { get; set; }
    public string? Status { get; set; }
}
