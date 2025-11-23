using FestivalTicketsApp.Application.TicketService.DTO;
using FestivalTicketsApp.Shared;

namespace FestivalTicketsApp.Application.TicketService;

public interface ITicketService
{
    public Task<Result<List<TicketDto>>> GetEventTicketsAsync(int eventId);

    public Task<Result<List<TicketTypeDto>>> GetEventTicketTypesAsync(int eventId);

    public Task<Result<List<TicketWithPriceDto>>> GetTicketsWithPriceByIdAsync(List<int> ticketsId);

    public Task<Result> ChangeEventTicketsStatusAsync(string statusName, List<int> ticketsId, int? clientId);
}
