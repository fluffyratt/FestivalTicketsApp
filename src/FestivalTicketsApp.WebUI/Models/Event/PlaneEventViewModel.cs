using FestivalTicketsApp.Application.EventService.DTO;
using FestivalTicketsApp.Application.HostService.DTO;

namespace FestivalTicketsApp.WebUI.Models.Event;

public class PlaneEventViewModel
{
    public List<EventTypeDto> EventTypes { get; set; }

    public List<GenreDto> TypeGenres { get; set; }

    public HostHallDetailsDto HallDetails { get; set; }

    public PlaneEventDto? NewEventInfo { get; set; }

    public bool IsSucceed { get; set; }
}
