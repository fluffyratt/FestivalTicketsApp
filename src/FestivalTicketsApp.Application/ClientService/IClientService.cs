using FestivalTicketsApp.Application.ClientService.DTO;
using FestivalTicketsApp.Application.ClientService.Filters;
using FestivalTicketsApp.Application.EventService.DTO;
using FestivalTicketsApp.Shared;

namespace FestivalTicketsApp.Application.ClientService;

public interface IClientService
{
    Task<Result<bool>> IsInFavouriteAsync(int eventId, int clientId);

    Task<Result> ChangeFavouriteStatusAsync(int eventId, int clientId, bool newStatus);

    Task<Result<int>> CreateClientAsync(ClientCreateDto newClient);

    Task<Result> DeleteClientByIdAsync(int id);

    Task<Result<int>> GetClientIdBySubjectAsync(string subject);

    Task<Result<Paginated<EventDto>>> GetFavouriteEvents(int clientId, ClientFavouriteEventsFilter filter);

    Task<Result<Paginated<FullInfoTicketDto>>> GetPurchasedTickets(int clientId, ClientTicketsFilter filter);
}
