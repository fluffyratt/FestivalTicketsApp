using FestivalTicketsApp.Application.EventService.DTO;
using FestivalTicketsApp.Application.EventService.Filters;
using FestivalTicketsApp.Shared;

namespace FestivalTicketsApp.Application.EventService;

public interface IEventService
{
    Task<Result<SeatDto>> GetSeatByIdAsync(int seatId);
    Task<Result> UpdateSeatStatusAsync(int seatId, string? status);

    Task<Result<Paginated<EventDto>>> GetEventsAsync(EventFilter filter);

    Task<Result<EventDto>> GetEventByIdAsync(int id);

    Task<Result<EventWithDetailsDto>> GetEventWithDetailsAsync(int id);

    Task<Result<List<GenreDto>>> GetGenresAsync(int eventTypeId);

    Task<Result<List<EventTypeDto>>> GetEventTypesAsync();

    Task<Result<int>> PlaneEventAsync(PlaneEventDto caseContext);

    Task<Result> ArchiveEventAsync(int eventId);

    Task<Result<List<TicketSeatDto>>> GetEventSeatsAsync(int eventId);
}
