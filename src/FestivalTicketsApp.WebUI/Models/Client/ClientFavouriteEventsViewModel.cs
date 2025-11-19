using FestivalTicketsApp.Application.EventService.DTO;

namespace FestivalTicketsApp.WebUI.Models.Client;

public class ClientFavouriteEventsViewModel
{
    public List<EventDto>? Events { get; set; }

    public ClientFavouriteEventsQuery QueryState { get; set; }

    public int CurrentPageNum { get; set; }

    public int NextPagesAmount { get; set; }
}
