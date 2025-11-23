using FestivalTicketsApp.Application.EventService.DTO;
using FestivalTicketsApp.Application.EventService.Filters;
using FestivalTicketsApp.Shared;

namespace FestivalTicketsApp.Application.EventService;

public interface IEventService
{
    public Task<Result<Paginated<EventDto>>> GetEventsAsync(EventFilter filter);
    
    public Task<Result<EventDto>> GetEventByIdAsync(int id);

    public Task<Result<EventWithDetailsDto>> GetEventWithDetailsAsync(int id);

    public Task<Result<List<GenreDto>>> GetGenresAsync(int eventTypeId);

    public Task<Result<List<EventTypeDto>>> GetEventTypesAsync();

    public Task<Result<int>> PlaneEventAsync(PlaneEventDto caseContext);

    public Task<Result> ArchiveEventAsync(int eventId);
}
