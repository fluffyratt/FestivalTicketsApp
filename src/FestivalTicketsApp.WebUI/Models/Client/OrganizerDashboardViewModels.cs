namespace FestivalTicketsApp.WebUI.Models.Client;

public class OrganizerEventViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class OrganizerDashboardViewModel
{
    public List<OrganizerEventViewModel> Events { get; set; } = new();
}
