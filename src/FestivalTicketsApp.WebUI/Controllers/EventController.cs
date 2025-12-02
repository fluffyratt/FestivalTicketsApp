using System.Security.Claims;
using FestivalTicketsApp.Application;
using FestivalTicketsApp.Application.ClientService;
using FestivalTicketsApp.Application.EventService;
using FestivalTicketsApp.Application.EventService.DTO;
using FestivalTicketsApp.Application.EventService.Filters;
using FestivalTicketsApp.Application.HostService;
using FestivalTicketsApp.Application.HostService.DTO;
using FestivalTicketsApp.Infrastructure.BackgroundJobsService;
using FestivalTicketsApp.Shared;
using FestivalTicketsApp.WebUI.Models.Event;
using FestivalTicketsApp.WebUI.Models.Shared;
using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FestivalTicketsApp.WebUI.Controllers;

public class EventController(
    IEventService eventService,
    IHostService hostService,
    IClientService clientService,
    IBackgroundJobsService backgroundJobsService)
    : Controller
{
    private readonly IEventService _eventService = eventService;
    private readonly IHostService _hostService = hostService;
    private readonly IClientService _clientService = clientService;
    private readonly IBackgroundJobsService _backgroundJobsService = backgroundJobsService;

    // НОВЕ: стартова сторінка подій (для юзера після логіну)
    // Вона вибирає "дефолтний" тип події (перший у списку) і віддає ту ж саму сторінку, що і List.
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery, Bind(Prefix = "QueryState")] EventListQuery query)
    {
        // Отримуємо всі типи подій
        Result<List<EventTypeDto>> getEventTypesResult = await _eventService.GetEventTypesAsync();

        if (!getEventTypesResult.IsSuccess || getEventTypesResult.Value is null || getEventTypesResult.Value.Count == 0)
            throw new RequiredDataNotFoundException();

        // За замовчуванням беремо перший тип події як стартовий
        int defaultEventTypeId = getEventTypesResult.Value[0].Id;

        // Використовуємо вже існуючу логіку списку
        return await List(defaultEventTypeId, query);
    }

    public async Task<IActionResult> List(int id, [FromQuery, Bind(Prefix = "QueryState")] EventListQuery query)
    {
        int eventTypeId = id;

        Result<List<string>> getCityNamesResult = await _hostService.GetCitiesAsync();

        Result<List<GenreDto>> getGenresResult = await _eventService.GetGenresAsync(eventTypeId);

        if (!getCityNamesResult.IsSuccess || !getGenresResult.IsSuccess)
            throw new RequiredDataNotFoundException();

        EventFilter eventFilter = new
        (
            new PagingFilter(query.PageNum, query.PageSize),
            query.StartDate,
            query.EndDate,
            null,
            eventTypeId,
            query.GenreId,
            query.CityName,
            ServicesEnums.PlannedEventStatus
        );

        Result<Paginated<EventDto>> getEventsResult = await _eventService.GetEventsAsync(eventFilter);

        EventListViewModel viewModel = new();

        viewModel.QueryState = query;

        viewModel.CityNames = getCityNamesResult.Value!;

        viewModel.Genres = getGenresResult.Value!;

        if (getEventsResult.IsSuccess)
        {
            viewModel.Events = getEventsResult.Value!.Value;

            viewModel.CurrentPageNum = getEventsResult.Value.CurrentPage;

            viewModel.NextPagesAmount = getEventsResult.Value.NextPagesAmount;
        }
        else
        {
            viewModel.CurrentPageNum = RequestDefaults.PageNum;

            viewModel.NextPagesAmount = RequestDefaults.NextPagesAmount;
        }

        return View(viewModel);
    }

    public async Task<IActionResult> Details(int id)
    {
        int eventId = id;

        Result<EventWithDetailsDto> getEventResult = await _eventService.GetEventWithDetailsAsync(eventId);

        if (!getEventResult.IsSuccess)
            throw new RequiredDataNotFoundException();

        EventDetailsViewModel viewModel = new();

        string? userIdRaw = User.FindFirstValue(JwtClaimTypes.Actor);
        if (userIdRaw is not null
          && int.TryParse(User.FindFirstValue(JwtClaimTypes.Actor), out int userId))
        {
            Result<bool> isInFavourite = await _clientService.IsInFavouriteAsync(eventId, userId);

            if (isInFavourite.IsSuccess)
                viewModel.IsInFavourite = isInFavourite.Value;
        }

        viewModel.Event = getEventResult.Value!;

        Result<List<HostedEventDto>> getHostedEventsResult =
            await _hostService.GetHostedEventsAsync(getEventResult.Value!.HostId);

        if (getHostedEventsResult.IsSuccess)
        {
            viewModel.HostedEvents = getHostedEventsResult.Value;
        }

        return View(viewModel);
    }

    [Authorize(Roles = UserRolesConstants.Manager)]
    public async Task<IActionResult> Plane(int id)
    {
        int hostId = id;

        PlaneEventViewModel viewModel = await BuildPlaneEventModel(hostId);

        return View(viewModel);
    }

    [Authorize(Roles = UserRolesConstants.Manager)]
    [HttpPost]
    public async Task<IActionResult> Plane(int id, PlaneEventDto newEventInfo)
    {
        int hostId = id;
        PlaneEventViewModel? viewModel;

        if (!ModelState.IsValid)
        {
            viewModel = await BuildPlaneEventModel(hostId, newEventInfo);
        }
        else
        {
            Result<int> planeResult = await _eventService.PlaneEventAsync(newEventInfo);

            if (!planeResult.IsSuccess)
            {
                ModelState.AddModelError("", "Something went wrong, try later");
                viewModel = await BuildPlaneEventModel(hostId, newEventInfo);
            }
            else
            {
                ModelState.Clear();
                viewModel = await BuildPlaneEventModel(hostId);
                await _backgroundJobsService.ScheduleEventArchiveTask(planeResult.Value, newEventInfo.StartTime);
                viewModel.IsSucceed = true;
            }
        }
        return View(viewModel);
    }

    [Authorize(Roles = UserRolesConstants.Manager)]
    public async Task<IActionResult> GenresOptions(int id)
    {
        int eventTypeId = id;

        Result<List<GenreDto>> getGenres = await _eventService.GetGenresAsync(eventTypeId);

        return getGenres.IsSuccess switch
        {
            true => PartialView("PlanePartials/GenresOptions", getGenres),
            false when getGenres.Error == DomainErrors.QueryEmptyResult => NoContent(),
            false when getGenres.Error == DomainErrors.RelatedEntityNotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private async Task<PlaneEventViewModel> BuildPlaneEventModel(int hostId, PlaneEventDto? input = default)
    {
        PlaneEventViewModel viewModel = new();

        Result<List<EventTypeDto>> getEventTypes = await _eventService.GetEventTypesAsync();
        Result<HostHallDetailsDto> getHostHallDetails =
            await _hostService.GetHostHallDetailsAsync(hostId);
        if (!getEventTypes.IsSuccess || !getHostHallDetails.IsSuccess)
            throw new RequiredDataNotFoundException();

        int eventTypeId = input?.EventTypeId ?? getEventTypes.Value![0].Id;
        Result<List<GenreDto>> getGenres = await _eventService.GetGenresAsync(eventTypeId);
        if (!getEventTypes.IsSuccess)
            throw new RequiredDataNotFoundException();

        viewModel.EventTypes = getEventTypes.Value!;
        viewModel.TypeGenres = getGenres.Value!;
        viewModel.HallDetails = getHostHallDetails.Value!;

        int defaultGenreId = getGenres.Value![0].Id;
        DateTime defaultDate = DateTime.Today;
        viewModel.NewEventInfo = input ?? new
        (
            "",
            eventTypeId,
            defaultGenreId,
            default,
            "",
            defaultDate,
            1,
            default,
            default
        );

        return viewModel;
    }
}
