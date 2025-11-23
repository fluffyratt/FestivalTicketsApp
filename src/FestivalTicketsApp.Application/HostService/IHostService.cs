using FestivalTicketsApp.Application.HostService.DTO;
using FestivalTicketsApp.Application.HostService.Filters;
using FestivalTicketsApp.Shared;

namespace FestivalTicketsApp.Application.HostService;

public interface IHostService
{
    public Task<Result<Paginated<HostDto>>> GetHostsAsync(HostFilter filter);

    public Task<Result<HostWithDetailsDto>> GetHostWithDetailsAsync(int id);

    public Task<Result<List<HostedEventDto>>> GetHostedEventsAsync(int id);
    
    public Task<Result<List<HostTypeDto>>> GetHostTypesAsync();
    
    public Task<Result<List<string>>> GetCitiesAsync();

    public Task<Result<HostHallDetailsDto>> GetHostHallDetailsAsync(int id);

    public Task<Result<HostHallDetailsDto>> GetHostHallDetailsWithEventIdAsync(int eventId);
}
