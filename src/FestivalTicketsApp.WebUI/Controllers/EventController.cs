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

    private static readonly Dictionary<int, DateTime> HoldSeats = new();


    // --------------------------------------------
    //  HOLD SEAT
    // --------------------------------------------
    [HttpPost]
    public async Task<IActionResult> HoldSeat(int seatId)
    {
        var seatResult = await _eventService.GetSeatByIdAsync(seatId);
        if (!seatResult.IsSuccess)
            return NotFound();

        var seat = seatResult.Value!;

        if (seat.Status == "Sold")
            return BadRequest("Seat already sold.");

        if (HoldSeats.ContainsKey(seatId) && HoldSeats[seatId] > DateTime.UtcNow)
            return BadRequest("Seat already reserved.");

        HoldSeats[seatId] = DateTime.UtcNow.AddMinutes(5);

        await _eventService.UpdateSeatStatusAsync(seatId, "Hold");

        return Ok(new { success = true, until = HoldSeats[seatId] });
    }


    // --------------------------------------------
    //  GET EVENT SEATS with HOLD logic
    // --------------------------------------------
    public async Task<IActionResult> GetEventSeats(int id)
    {
        var result = await _eventService.GetEventSeatsAsync(id);

        if (!result.IsSuccess)
            return BadRequest();

        var seats = result.Value!;

        foreach (var s in seats)
        {
            if (HoldSeats.TryGetValue(s.Id, out DateTime expires))
            {
                if (expires < DateTime.UtcNow)
                {
                    HoldSeats.Remove(s.Id);
                }
                else
                {
                    s.Status = "Hold";
                }
            }
        }

        return Json(seats);
    }



    // --------------------------------------------
    // ORIGINAL
    // --------------------------------------------

    public async Task<IActionResult> List(int id,
        [FromQuery, Bind(Prefix = "QueryState")] EventListQuery query)
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

        Result<Paginated<EventDto>> getEventsResult =
            await _eventService.GetEventsAsync(eventFilter);

        EventListViewModel viewModel = new()
        {
            QueryState = query,
            CityNames = getCityNamesResult.Value!,
            Genres = getGenresResult.Value!
        };

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

        Result<EventWithDetailsDto> getEventResult =
            await _eventService.GetEventWithDetailsAsync(eventId);

        if (!getEventResult.IsSuccess)
            throw new RequiredDataNotFoundException();

        EventDetailsViewModel viewModel = new();

        string? userRaw = User.FindFirstValue(JwtClaimTypes.Actor);
        if (userRaw is not null && int.TryParse(userRaw, out int userId))
        {
            Result<bool> isInFavourite =
                await _clientService.IsInFavouriteAsync(eventId, userId);

            if (isInFavourite.IsSuccess)
                viewModel.IsInFavourite = isInFavourite.Value;
        }

        viewModel.Event = getEventResult.Value!;

        Result<List<HostedEventDto>> getHostedEventsResult =
            await _hostService.GetHostedEventsAsync(getEventResult.Value!.HostId);

        if (getHostedEventsResult.IsSuccess)
            viewModel.HostedEvents = getHostedEventsResult.Value;

        return View(viewModel);
    }



    public IActionResult SeatMap(int id)
    {
        return View("SeatMap", id);
    }




    [Authorize(Roles = UserRolesConstants.Manager)]
    public async Task<IActionResult> Plane(int id)
    {
        int hostId = id;

        PlaneEventViewModel viewModel =
            await BuildPlaneEventModel(hostId);

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
            Result<int> planeResult =
                await _eventService.PlaneEventAsync(newEventInfo);

            if (!planeResult.IsSuccess)
            {
                ModelState.AddModelError("", "Something went wrong, try later");
                viewModel = await BuildPlaneEventModel(hostId, newEventInfo);
            }
            else
            {
                ModelState.Clear();
                viewModel = await BuildPlaneEventModel(hostId);

                await _backgroundJobsService
                    .ScheduleEventArchiveTask(
                        planeResult.Value,
                        newEventInfo.StartTime);

                viewModel.IsSucceed = true;
            }
        }

        return View(viewModel);
    }




    [Authorize(Roles = UserRolesConstants.Manager)]
    public async Task<IActionResult> GenresOptions(int id)
    {
        int eventTypeId = id;

        Result<List<GenreDto>> getGenres =
            await _eventService.GetGenresAsync(eventTypeId);

        return getGenres.IsSuccess switch
        {
            true => PartialView("PlanePartials/GenresOptions", getGenres),
            false when getGenres.Error == DomainErrors.QueryEmptyResult => NoContent(),
            false when getGenres.Error == DomainErrors.RelatedEntityNotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }




    private async Task<PlaneEventViewModel> BuildPlaneEventModel(
        int hostId,
        PlaneEventDto? input = default)
    {
        PlaneEventViewModel viewModel = new();

        Result<List<EventTypeDto>> getEventTypes =
            await _eventService.GetEventTypesAsync();

        Result<HostHallDetailsDto> getHostHallDetails =
            await _hostService.GetHostHallDetailsAsync(hostId);

        if (!getEventTypes.IsSuccess || !getHostHallDetails.IsSuccess)
            throw new RequiredDataNotFoundException();

        int eventTypeId = input?.EventTypeId ?? getEventTypes.Value![0].Id;

        Result<List<GenreDto>> getGenres =
            await _eventService.GetGenresAsync(eventTypeId);

        if (!getGenres.IsSuccess)
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
