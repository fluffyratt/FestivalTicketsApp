using FestivalTicketsApp.Application.HostService.DTO;
using FestivalTicketsApp.Application.HostService.Filters;
using FestivalTicketsApp.Core.Entities;
using FestivalTicketsApp.Infrastructure.Data;
using FestivalTicketsApp.Shared;
using Microsoft.EntityFrameworkCore;

namespace FestivalTicketsApp.Application.HostService;

public class HostService(AppDbContext context) : PaginatedService,
                                                 IHostService
{
    private readonly AppDbContext _context = context;

    public async Task<Result<Paginated<HostDto>>> GetHostsAsync(HostFilter filter)
    {
        IQueryable<Host> hostsQuery = _context.Hosts
            .AsNoTracking()
            .Include(h => h.Location)
            .Include(h => h.HostType);

        int nextPagesAmount = 0;

        hostsQuery = await ProcessHostFilter(hostsQuery, filter, ref nextPagesAmount);

        List<HostDto> values = await hostsQuery
            .Select(h => new HostDto(h.Id, h.Name))
            .ToListAsync();

        if (values.Count == 0)
            return Result<Paginated<HostDto>>.Failure(DomainErrors.QueryEmptyResult);

        Paginated<HostDto> result = new(
            values,
            filter.Pagination?.PageNum ?? 1,
            nextPagesAmount);

        return Result<Paginated<HostDto>>.Success(result);
    }

    public async Task<Result<HostWithDetailsDto>> GetHostWithDetailsAsync(int id)
    {
        IQueryable<Host> hostsQuery = _context.Hosts
            .AsNoTracking()
            .Include(h => h.Location)
            .Include(h => h.Details);

        Host? hostEntity = await hostsQuery.FirstOrDefaultAsync(h => h.Id == id);

        if (hostEntity is null)
            return Result<HostWithDetailsDto>.Failure(DomainErrors.EntityNotFound);

        HostWithDetailsDto result = new(
            hostEntity.Id,
            hostEntity.Name,
            hostEntity.Details.Description,
            new LocationDto(
                hostEntity.Location.Id,
                hostEntity.Location.CityName,
                hostEntity.Location.StreetName,
                hostEntity.Location.BuildingNumber,
                hostEntity.Location.Latitude,
                hostEntity.Location.Longitude));
        
        return Result<HostWithDetailsDto>.Success(result);
    }

    public async Task<Result<HostHallDetailsDto>> GetHostHallDetailsWithEventIdAsync(int eventId)
    {
        IQueryable<Event> eventQuery = _context.Events
            .Include(e => e.Host)
            .ThenInclude(h => h.Details)
            .AsNoTracking();

        Event? eventEntity = await eventQuery.FirstOrDefaultAsync(e => e.Id == eventId);
    
        if (eventEntity is null)
            return Result<HostHallDetailsDto>.Failure(DomainErrors.RelatedEntityNotFound);

        if (eventEntity?.Host?.Details is null)
            return Result<HostHallDetailsDto>.Failure(DomainErrors.EntityNotFound);
        
        HostHallDetailsDto result = new(
            eventEntity.Host.Id,
            eventEntity.Host.Details.RowAmount,
            eventEntity.Host.Details.SeatsInRow,
            eventEntity.Host.Details.IsDividedBySeats);

        return Result<HostHallDetailsDto>.Success(result);
    }
    
    public async Task<Result<HostHallDetailsDto>> GetHostHallDetailsAsync(int id)
    {
        IQueryable<Host> hostQuery = _context.Hosts
            .AsNoTracking()
            .Include(h => h.Details);

        Host? hostEntity = await hostQuery.FirstOrDefaultAsync(h => h.Id == id);
        
        if (hostEntity is null)
            return Result<HostHallDetailsDto>.Failure(DomainErrors.EntityNotFound);

        HostHallDetailsDto result = new(
            hostEntity.Id,
            hostEntity.Details.RowAmount,
            hostEntity.Details.SeatsInRow,
            hostEntity.Details.IsDividedBySeats);

        return Result<HostHallDetailsDto>.Success(result);
    }

    public async Task<Result<List<HostedEventDto>>> GetHostedEventsAsync(int id)
    {
        IQueryable<Host> hostsQuery = _context.Hosts
            .AsNoTracking()
            .Include(h => h.HostedEvents).ThenInclude(e => e.EventDetails);

        Host? hostEntity = await hostsQuery.FirstOrDefaultAsync(h => h.Id == id);

        if (hostEntity is null)
            return Result<List<HostedEventDto>>.Failure(DomainErrors.EntityNotFound);

        List<HostedEventDto> result = hostEntity.HostedEvents
            .Select(he =>
                new HostedEventDto(
                    he.Id,
                    he.Title,
                    he.EventDetails.StartDate))
            .ToList();

        if (result.Count == 0)
            return Result<List<HostedEventDto>>.Failure(DomainErrors.QueryEmptyResult);

        return Result<List<HostedEventDto>>.Success(result);
    }

    public async Task<Result<List<HostTypeDto>>> GetHostTypesAsync()
    {
        IQueryable<HostType> hostTypesQuery = _context.HostTypes
            .AsNoTracking();

        List<HostTypeDto> result = await hostTypesQuery
            .Select(ht => new HostTypeDto(ht.Id, ht.Name))
            .ToListAsync();

        if (result.Count == 0)
            return Result<List<HostTypeDto>>.Failure(DomainErrors.QueryEmptyResult);

        return Result<List<HostTypeDto>>.Success(result);
    }

    public async Task<Result<List<string>>> GetCitiesAsync()
    {
        IQueryable<Location> locationsQuery = _context.Locations
            .AsNoTracking();
        
        List<string> result = await locationsQuery
            .GroupBy(l => l.CityName)
            .Select(cg => cg.Key)
            .ToListAsync();

        if (result.Count == 0)
            return Result<List<string>>.Failure(DomainErrors.QueryEmptyResult);

        return Result<List<string>>.Success(result);
    }

    private Task<IQueryable<Host>> ProcessHostFilter(
        IQueryable<Host> hostsQuery, 
        HostFilter filter,
        ref int nextPagesAmount)
    {
        if (filter.CityName is not null)
            hostsQuery = hostsQuery.Where(h => h.Location.CityName == filter.CityName);
        
        if (filter.HostTypeId is not null)
            hostsQuery = hostsQuery.Where(h => h.HostTypeId == filter.HostTypeId);

        hostsQuery = hostsQuery.OrderBy(h => h.Id);
        
        if (filter.Pagination is not null)
        {
            hostsQuery = ProcessPagination(hostsQuery,
                                           filter.Pagination.PageNum,
                                           filter.Pagination.PageSize,
                                           ref nextPagesAmount);
        }
        
        return Task.FromResult(hostsQuery);
    }
}
