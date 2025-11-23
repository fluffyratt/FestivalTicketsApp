using FestivalTicketsApp.Application.ClientService.DTO;
using FestivalTicketsApp.Application.ClientService.Filters;
using FestivalTicketsApp.Application.EventService.DTO;
using FestivalTicketsApp.Core.Entities;
using FestivalTicketsApp.Infrastructure.Data;
using FestivalTicketsApp.Shared;
using Microsoft.EntityFrameworkCore;

namespace FestivalTicketsApp.Application.ClientService;

public class ClientService(AppDbContext context) : PaginatedService,
                                                   IClientService
{
    private readonly AppDbContext _context = context;
    
    public async Task<Result<bool>> IsInFavouriteAsync(int eventId, int clientId)
    {
        Client? clientEntity = await _context.Clients
            .AsNoTracking()
            .Include(c => c.FavouriteEvents)
            .FirstOrDefaultAsync(c => c.Id == clientId);

        if (clientEntity is null)
            return Result<bool>.Failure(DomainErrors.RelatedEntityNotFound);

        bool result = clientEntity.FavouriteEvents.Exists(fe => fe.Id == eventId);

        return Result<bool>.Success(result);
    }

    public async Task<Result> ChangeFavouriteStatusAsync(int eventId, int clientId, bool newStatus)
    {
        Client? clientEntity = await _context.Clients
            .Include(c => c.FavouriteEvents)
            .FirstOrDefaultAsync(c => c.Id == clientId);

        Event? eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
        
        if (clientEntity is null || eventEntity is null)
            return Result.Failure(DomainErrors.RelatedEntityNotFound);

        bool isInFavourite = clientEntity.FavouriteEvents.Exists(fe => fe.Id == eventId);
        
        if (isInFavourite == newStatus)
            return Result.Failure(DomainErrors.SameFavouriteStatusSet);

        if (newStatus)
            clientEntity.FavouriteEvents.Add(eventEntity);
        else
            clientEntity.FavouriteEvents.Remove(eventEntity);

        await _context.SaveChangesAsync();
        
        return Result.Success();
    }

    public async Task<Result<int>> CreateClientAsync(ClientCreateDto newClient)
    {
        if (await _context.Clients.AnyAsync(
                c => c.Email.ToUpper() == newClient.Email.ToUpper()))
            return Result<int>.Failure(DomainErrors.UserEmailNotUnique);
        
        if (await _context.Clients.AnyAsync(
                c => c.Phone == newClient.Phone))
            return Result<int>.Failure(DomainErrors.UserPhoneNotUnique);
        
        if (await _context.Clients.AnyAsync(
                c => c.Subject == newClient.Subject))
            return Result<int>.Failure(DomainErrors.UserSubjectNotUnique);
        

        Client newClientEntity = new()
        {
            Email = newClient.Email,
            Name = newClient.Name,
            Surname = newClient.Surname,
            Phone = newClient.Phone,
            Subject = newClient.Subject
        };

        await _context.Clients.AddAsync(newClientEntity);
        await _context.SaveChangesAsync();

        return Result<int>.Success(newClientEntity.Id);
    }

    public async Task<Result> DeleteClientByIdAsync(int id)
    {
        int affectedRows = await _context.Clients
            .Where(c => c.Id == id)
            .ExecuteDeleteAsync();

        if (affectedRows == 0)
            return Result.Failure(DomainErrors.EntityNotFound);

        return Result.Success();
    }

    public async Task<Result<int>> GetClientIdBySubjectAsync(string subject)
    {
        Client? clientEntity = await _context.Clients
            .FirstOrDefaultAsync(c => c.Subject == subject);

        if (clientEntity is not null)
            return Result<int>.Success(clientEntity.Id);
        else
            return Result<int>.Failure(DomainErrors.EntityNotFound);
    }

    public async Task<Result<Paginated<EventDto>>> GetFavouriteEvents(int clientId,
                                                                      ClientFavouriteEventsFilter filter)
    {
        Client? clientEntity = await _context.Clients
            .Include(c => c.FavouriteEvents).ThenInclude(e => e.EventStatus)
            .Include(c => c.FavouriteEvents).ThenInclude(e => e.EventDetails)
            .Include(c => c.FavouriteEvents).ThenInclude(e => e.Host)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clientId);

        if (clientEntity is null)
            return Result<Paginated<EventDto>>.Failure(DomainErrors.RelatedEntityNotFound);

        // Nonsense, just for compatibility with private method
        IQueryable<Event> eventsQuery = clientEntity.FavouriteEvents
            .OrderBy(e => e.EventDetails.StartDate)
            .AsQueryable();

        int nextPagesAmount = 0;

        if (filter.Pagination is not null)
        {
            eventsQuery = ProcessPagination(eventsQuery,
                filter.Pagination.PageNum,
                filter.Pagination.PageSize,
                ref nextPagesAmount);
        }

        List<EventDto> values = eventsQuery
            .Select(e => new EventDto(
                e.Id,
                e.Title,
                e.EventDetails.StartDate,
                e.Host.Name,
                e.EventStatus.Status))
            .ToList();

        if (values.Count == 0)
            return Result<Paginated<EventDto>>.Failure(DomainErrors.QueryEmptyResult);

        Paginated<EventDto> result = new(
            values,
            filter.Pagination?.PageNum ?? 1,
            nextPagesAmount);

        return Result<Paginated<EventDto>>.Success(result);
    }

    public async Task<Result<Paginated<FullInfoTicketDto>>> GetPurchasedTickets(int clientId,
        ClientTicketsFilter filter)
    {
        bool isClientExist = await _context.Clients
            .AnyAsync(c => c.Id == clientId);

        if (!isClientExist)
            return Result<Paginated<FullInfoTicketDto>>.Failure(DomainErrors.RelatedEntityNotFound);

        IQueryable<Ticket> ticketsQuery = _context.Tickets
            .Include(t => t.TicketStatus)
            .Include(t => t.TicketType)
            .ThenInclude(t => t.Event)
            .ThenInclude(e => e.EventDetails)
            .Where(t => t.ClientId == clientId)
            .OrderBy(t => t.Id)
            .AsNoTracking();

        int nextPagesAmount = 0;

        if (filter.Pagination is not null)
        {
            ticketsQuery = ProcessPagination(ticketsQuery,
                                             filter.Pagination.PageNum,
                                             filter.Pagination.PageSize,
                                             ref nextPagesAmount);
        }

        List<FullInfoTicketDto> values = await ticketsQuery
            .Select(t =>
                new FullInfoTicketDto
                (
                    t.Id,
                    t.RowNum,
                    t.SeatNum,
                    t.TicketType.Name,
                    t.TicketType.Price,
                    t.TicketType.EventId,
                    t.TicketType.Event.Title,
                    t.TicketType.Event.EventDetails.StartDate
                )
            )
            .ToListAsync();

        if (values.Count == 0)
            return Result<Paginated<FullInfoTicketDto>>.Failure(DomainErrors.QueryEmptyResult);

        Paginated<FullInfoTicketDto> result = new(
            values,
            filter.Pagination?.PageNum ?? 1,
            nextPagesAmount);

        return Result<Paginated<FullInfoTicketDto>>.Success(result);
    }
}
