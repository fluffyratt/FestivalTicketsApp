using FestivalTicketsApp.WebUI.Models.Shared;

namespace FestivalTicketsApp.WebUI.Models.Host;

public class HostListQuery : QueryBase
{
    public string? CityName { get; set; } = RequestDefaults.CityName;
}
