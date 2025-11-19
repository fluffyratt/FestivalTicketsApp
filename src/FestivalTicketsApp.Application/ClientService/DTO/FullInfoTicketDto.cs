namespace FestivalTicketsApp.Application.ClientService.DTO;

public record FullInfoTicketDto
(
    int Id,
    int? RowNum,
    int? SeatNum,
    string TypeName,
    decimal Price,
    int EventId,
    string EventTitle,
    DateTime StartDate
);
