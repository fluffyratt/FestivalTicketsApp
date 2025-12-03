using FestivalTicketsApp.Application.EventService.DTO;
using FestivalTicketsApp.Application.EventService.Filters;
using FestivalTicketsApp.Core.Entities;
using FestivalTicketsApp.Infrastructure.Data;
using FestivalTicketsApp.Shared;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace FestivalTicketsApp.Application.EventService;

public class EventService(AppDbContext context) : PaginatedService,
                                                  IEventService
{
    private readonly AppDbContext _context = context;

    public async Task<Result<Paginated<EventDto>>> GetEventsAsync(EventFilter filter)
    {
        IQueryable<Event> eventsQuery = _context.Events
            .AsNoTracking()
            .Include(e => e.EventDetails)
            .Include(e => e.EventGenre)
                .ThenInclude(eg => eg.EventType)
            .Include(e => e.EventStatus)
            .Include(e => e.Host)
                .ThenInclude(h => h.Location);

        int nextPagesAmount = 0;

        eventsQuery = await ProcessEventFilter(eventsQuery, filter, ref nextPagesAmount);

        List<EventDto> values = await eventsQuery
            .Select(e =>
                new EventDto(
                    e.Id,
                    e.Title,
                    e.EventDetails.StartDate,
                    e.Host != null ? e.Host.Name : "Unknown host",
                    e.EventStatus != null ? e.EventStatus.Status : "Unknown"))
            .ToListAsync();

        if (values.Count == 0)
            return Result<Paginated<EventDto>>.Failure(DomainErrors.QueryEmptyResult);

        Paginated<EventDto> result = new(
            values,
            filter.Pagination?.PageNum ?? 1,
            nextPagesAmount);

        return Result<Paginated<EventDto>>.Success(result);
    }

    public async Task<Result<EventDto>> GetEventByIdAsync(int id)
    {
        IQueryable<Event> eventQuery = _context.Events
            .AsNoTracking()
            .Include(e => e.EventDetails)
            .Include(e => e.Host)
            .Include(e => e.EventStatus);

        Event? eventEntity = await eventQuery.FirstOrDefaultAsync(e => e.Id == id);

        if (eventEntity is null)
            return Result<EventDto>.Failure(DomainErrors.EntityNotFound);

        EventDto result = new(
            eventEntity.Id,
            eventEntity.Title,
            eventEntity.EventDetails.StartDate,
            eventEntity.Host?.Name ?? "Unknown host",
            eventEntity.EventStatus?.Status ?? "Unknown");

        return Result<EventDto>.Success(result);
    }

    public async Task<Result<EventWithDetailsDto>> GetEventWithDetailsAsync(int id)
    {
        IQueryable<Event> eventsQuery = _context.Events
            .AsNoTracking()
            .Include(e => e.EventDetails)
            .Include(e => e.Host);

        Event? eventEntity = await eventsQuery.FirstOrDefaultAsync(e => e.Id == id);

        if (eventEntity is null)
            return Result<EventWithDetailsDto>.Failure(DomainErrors.EntityNotFound);

        EventWithDetailsDto result = new(
            eventEntity.Id,
            eventEntity.Title,
            eventEntity.EventDetails.StartDate,
            eventEntity.HostId,
            eventEntity.Host?.Name ?? "Unknown host",
            eventEntity.EventDetails.Description,
            eventEntity.EventDetails.Duration);

        return Result<EventWithDetailsDto>.Success(result);
    }

    public async Task<Result<List<GenreDto>>> GetGenresAsync(int eventTypeId)
    {
        IQueryable<EventGenre> genresQuery = _context.EventGenres
            .AsNoTracking();

        bool isEventTypeExist = await _context.EventTypes.AnyAsync(et => et.Id == eventTypeId);
        if (!isEventTypeExist)
            return Result<List<GenreDto>>.Failure(DomainErrors.RelatedEntityNotFound);

        genresQuery = genresQuery
            .Where(g => g.EventTypeId == eventTypeId)
            .OrderBy(g => g.Id);

        List<GenreDto> result = await genresQuery
            .Select(g =>
                new GenreDto(g.Id, g.Genre))
            .ToListAsync();

        if (result.Count == 0)
            return Result<List<GenreDto>>.Failure(DomainErrors.QueryEmptyResult);

        return Result<List<GenreDto>>.Success(result);
    }

    public async Task<Result<List<EventTypeDto>>> GetEventTypesAsync()
    {
        IQueryable<EventType> eventTypeQuery = _context.EventTypes
            .AsNoTracking();

        List<EventTypeDto> result = await eventTypeQuery
            .Select(et =>
                new EventTypeDto(et.Id, et.Name))
            .ToListAsync();

        if (result.Count == 0)
            return Result<List<EventTypeDto>>.Failure(DomainErrors.QueryEmptyResult);

        return Result<List<EventTypeDto>>.Success(result);
    }

    public async Task<Result<int>> PlaneEventAsync(PlaneEventDto caseContext)
    {
        (Result? intermediateResult, Event? eventEntity) createEventResult = await CreateEvent(caseContext);

        Result intermediateResult = createEventResult.intermediateResult
                     ?? await CreateEventTickets(caseContext, createEventResult.eventEntity!)
                     ?? Result.Success();

        Result<int> result;
        if (intermediateResult.IsSuccess)
        {
            await _context.SaveChangesAsync();
            result = Result<int>.Success(createEventResult.eventEntity!.Id);
        }
        else
        {
            result = Result<int>.Failure(intermediateResult.Error!);
        }

        return result;
    }

    public async Task<Result> ArchiveEventAsync(int eventId)
    {
        Event? eventEntity = await _context.Events
            .Include(e => e.TicketTypes)
                .ThenInclude(tt => tt.TicketsWithType)
                    .ThenInclude(t => t.TicketStatus)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == eventId);

        EventStatus? eventStatusEntity = await _context.EventStatuses
            .FirstOrDefaultAsync(es => es.Status == ServicesEnums.EndedEventStatus);

        TicketStatus? ticketStatusEntity = await _context.TicketStatuses
            .FirstOrDefaultAsync(ts => ts.Status == ServicesEnums.TicketOutOfDateStatus);

        if (eventEntity is null)
            return Result.Failure(DomainErrors.EntityNotFound);

        if (eventStatusEntity is null || ticketStatusEntity is null)
            return Result.Failure(DomainErrors.RelatedEntityNotFound);

        eventEntity.EventStatus = eventStatusEntity;
        eventEntity.TicketTypes
            .SelectMany(tt => tt.TicketsWithType)
            .ToList()
            .ForEach(t => t.TicketStatus = ticketStatusEntity);

        await _context.SaveChangesAsync();

        return Result.Success();
    }


    private async Task<(Result? intermediateResult, Event? eventEntity)> CreateEvent(PlaneEventDto caseContext)
    {
        EventStatus? plannedStatusEntity = await _context.EventStatuses
            .FirstOrDefaultAsync(es => es.Status == ServicesEnums.PlannedEventStatus);

        Host? host = await _context.Hosts
            .Include(h => h.Details)
            .FirstOrDefaultAsync(hd => hd.Id == caseContext.HostId);

        if (plannedStatusEntity is null || host is null)
            return (Result.Failure(DomainErrors.RelatedEntityNotFound), null);

        EventDetails newEventDetailsEntity = new()
        {
            StartDate = caseContext.StartTime,
            Description = caseContext.Description,
            Duration = caseContext.Duration
        };

        Event eventEntity = new()
        {
            Title = caseContext.Title,
            EventGenreId = caseContext.GenreId,
            HostId = caseContext.HostId,
            EventDetails = newEventDetailsEntity,
            EventStatusId = plannedStatusEntity.Id,
            Host = host
        };

        return (null, eventEntity);
    }



    private async Task<Result?> CreateEventTickets(PlaneEventDto caseContext, Event eventEntity)
    {
        TicketStatus? availableStatusEntity = await _context.TicketStatuses
            .FirstOrDefaultAsync(ts => ts.Status == ServicesEnums.TicketAvailableStatus);

        if (availableStatusEntity is null)
            return Result.Failure(DomainErrors.RelatedEntityNotFound);

        if (eventEntity.Host.Details.RowAmount != caseContext.TypesMapping!.Count)
            return Result.Failure(DomainErrors.TicketTypeMappingMismatchHall);

        List<Ticket> newTicketEntities = new();

        List<TicketType> newTicketTypes = caseContext.TicketTypes!
            .Select(tt => new TicketType
            {
                Event = eventEntity,
                Name = tt.Name,
                Price = tt.Price
            })
            .ToList();

        if (eventEntity.Host.Details.IsDividedBySeats)
        {
            for (int rowIndex = 0; rowIndex < eventEntity.Host.Details.RowAmount; rowIndex++)
            {
                for (int seatNum = 1; seatNum <= eventEntity.Host.Details.SeatsInRow; seatNum++)
                {
                    Ticket ticketEntity = new()
                    {
                        RowNum = rowIndex + 1,
                        SeatNum = seatNum,
                        TicketStatusId = availableStatusEntity.Id,
                        TicketType = newTicketTypes.First(tt => tt.Name == caseContext.TypesMapping[rowIndex])
                    };

                    newTicketEntities.Add(ticketEntity);
                }
            }
        }
        else
        {
            for (int i = 0; i < eventEntity.Host.Details.SeatsInRow; i++)
            {
                Ticket ticketEntity = new()
                {
                    TicketStatusId = availableStatusEntity.Id,
                    TicketType = newTicketTypes[0]
                };

                newTicketEntities.Add(ticketEntity);
            }
        }

        await _context.Tickets.AddRangeAsync(newTicketEntities);

        return null;
    }


    private Task<IQueryable<Event>> ProcessEventFilter(
        IQueryable<Event> eventsQuery,
        EventFilter filter,
        ref int nextPagesAmount)
    {
        if (filter.CityName is not null)
            eventsQuery = eventsQuery.Where(e => e.Host != null &&
                                                 e.Host.Location.CityName == filter.CityName);

        if (filter.StartDate is not null)
            eventsQuery = eventsQuery.Where(e => e.EventDetails.StartDate >= filter.StartDate);

        if (filter.EndDate is not null)
            eventsQuery = eventsQuery.Where(e => e.EventDetails.StartDate.Date <= filter.EndDate);

        if (filter.HostId is not null)
            eventsQuery = eventsQuery.Where(e => e.HostId == filter.HostId);

        if (filter.EventTypeId is not null)
            eventsQuery = eventsQuery.Where(e => e.EventGenre.EventTypeId == filter.EventTypeId);

        if (filter.GenreId is not null)
            eventsQuery = eventsQuery.Where(e => e.EventGenreId == filter.GenreId);

        if (filter.StatusName is not null)
            eventsQuery = eventsQuery.Where(e => e.EventStatus != null &&
                                                 e.EventStatus.Status == filter.StatusName);

        eventsQuery = eventsQuery.OrderBy(e => e.EventDetails.StartDate);

        if (filter.Pagination is not null)
        {
            eventsQuery = ProcessPagination(eventsQuery,
                                            filter.Pagination.PageNum,
                                            filter.Pagination.PageSize,
                                            ref nextPagesAmount);
        }

        return Task.FromResult(eventsQuery);
    }


    // -----------------------------------------------------------
    //  GetEventSeatsAsync → correct TicketSeatDto projection
    // -----------------------------------------------------------
    public async Task<Result<List<TicketSeatDto>>> GetEventSeatsAsync(int eventId)
    {
        var tickets = await _context.Tickets
            .AsNoTracking()
            .Include(t => t.TicketStatus)
            .Include(t => t.TicketType)
            .Where(t => t.TicketType.EventId == eventId)
            .ToListAsync();

        if (!tickets.Any())
            return Result<List<TicketSeatDto>>.Failure(DomainErrors.QueryEmptyResult);

        var result = tickets.Select(t =>
            new TicketSeatDto(
                t.Id,
                t.RowNum,
                t.SeatNum,
                t.TicketStatus.Status,
                t.TicketType.Price
            )
        ).ToList();

        return Result<List<TicketSeatDto>>.Success(result);
    }


    // -----------------------------------------------------------
    //  Get single seat by ID
    // -----------------------------------------------------------
    public async Task<Result<SeatDto>> GetSeatByIdAsync(int seatId)
    {
        var ticket = await _context.Tickets
            .Include(t => t.TicketStatus)
            .Include(t => t.TicketType)
            .FirstOrDefaultAsync(t => t.Id == seatId);

        if (ticket == null)
            return Result<SeatDto>.Failure(DomainErrors.QueryEmptyResult);

        return Result<SeatDto>.Success(
            new SeatDto
            {
                Id = ticket.Id,
                Row = ticket.RowNum,
                Seat = ticket.SeatNum,
                Status = ticket.TicketStatus.Status
            }
        );
    }


    // -----------------------------------------------------------
    //  UpdateSeatStatusAsync
    // -----------------------------------------------------------
    public async Task<Result> UpdateSeatStatusAsync(int seatId, string? status)
    {
        var ticket = await _context.Tickets
            .Include(t => t.TicketStatus)
            .FirstOrDefaultAsync(t => t.Id == seatId);

        if (ticket == null)
            return Result.Failure(DomainErrors.QueryEmptyResult);

        var statusEntity = await _context.TicketStatuses
            .FirstOrDefaultAsync(ts => ts.Status == status);

        if (status != null && statusEntity == null)
            return Result.Failure(DomainErrors.RelatedEntityNotFound);

        ticket.TicketStatus = statusEntity;

        await _context.SaveChangesAsync();

        return Result.Success();
    }
}
