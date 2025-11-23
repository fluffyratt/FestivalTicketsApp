using FestivalTicketsApp.WebUI.Models.Shared;

namespace FestivalTicketsApp.WebUI.Models.Event;

public class EventListQuery : QueryBase
{
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int? GenreId { get; set; }

    public string CityName { get; set; } = RequestDefaults.CityName;
}
