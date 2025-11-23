using FestivalTicketsApp.Application.ClientService.DTO;

namespace FestivalTicketsApp.WebUI.Models.Client;

public class ClientTicketsViewModel
{
    public List<FullInfoTicketDto>? PurchasedTickets { get; set; }

    public ClientTicketsQuery QueryState { get; set; }

    public int CurrentPageNum { get; set; }

    public int NextPagesAmount { get; set; }
}
