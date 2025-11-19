using System.Security.Claims;
using FestivalTicketsApp.Application;
using FestivalTicketsApp.Application.ClientService;
using FestivalTicketsApp.Application.ClientService.DTO;
using FestivalTicketsApp.Application.ClientService.Filters;
using FestivalTicketsApp.Application.EventService.DTO;
using FestivalTicketsApp.Shared;
using FestivalTicketsApp.WebUI.Models.Client;
using FestivalTicketsApp.WebUI.Models.Shared;
using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FestivalTicketsApp.WebUI.Controllers;

[Authorize(Roles = UserRolesConstants.Client)]
public class ClientController(IClientService clientService) : Controller
{
    private readonly IClientService _clientService = clientService;

    public async Task<IActionResult> Favourite([FromQuery, Bind(Prefix = "QueryState")] ClientFavouriteEventsQuery query)
    {
        int.TryParse(User.FindFirstValue(JwtClaimTypes.Actor), out int userId);

        ClientFavouriteEventsFilter filter = new
        (
            new PagingFilter(query.PageNum, query.PageSize)
        );

        Result<Paginated<EventDto>> getEventsResult = await _clientService.GetFavouriteEvents(userId, filter);

        ClientFavouriteEventsViewModel viewModel = new();

        viewModel.QueryState = query;

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

    public async Task<IActionResult> Tickets([FromQuery, Bind(Prefix = "QueryState")] ClientTicketsQuery query)
    {
        int.TryParse(User.FindFirstValue(JwtClaimTypes.Actor), out int userId);

        ClientTicketsFilter filter = new
        (
            new PagingFilter(query.PageNum, query.PageSize)
        );

        Result<Paginated<FullInfoTicketDto>> getTicketsResult =
            await _clientService.GetPurchasedTickets(userId, filter);

        ClientTicketsViewModel viewModel = new();
        viewModel.QueryState = query;

        if (getTicketsResult.IsSuccess)
        {
            viewModel.PurchasedTickets = getTicketsResult.Value!.Value;
            viewModel.CurrentPageNum = getTicketsResult.Value.CurrentPage;
            viewModel.NextPagesAmount = getTicketsResult.Value.NextPagesAmount;
        }
        else
        {
            viewModel.CurrentPageNum = RequestDefaults.PageNum;
            viewModel.NextPagesAmount = RequestDefaults.NextPagesAmount;
        }

        return View(viewModel);
    }
    
    public async Task<IActionResult> ChangeFavouriteStatus(int id, bool newStatus)
    {
        int eventId = id;
        
        int.TryParse(User.FindFirstValue(JwtClaimTypes.Actor), out int userId);

        Result result = await _clientService.ChangeFavouriteStatusAsync(eventId, userId, newStatus);

        switch (result.IsSuccess)
        {
            case true:
                return Ok();
            case false when result.Error == DomainErrors.RelatedEntityNotFound:
                return NotFound(result.Error.Identifier);
            case false when result.Error == DomainErrors.SameFavouriteStatusSet:
                return BadRequest(result.Error.Identifier);
            default:
                return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] ClientCreateDto clientInfo)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState.Values.SelectMany(v => v.Errors));

        Result<int> result = await _clientService.CreateClientAsync(clientInfo);

        switch (result.IsSuccess)
        {
            case true:
                return Ok(result.Value);
            case false when result.Error == DomainErrors.UserEmailNotUnique:
                return BadRequest(result.Error.Identifier);
            default:
                return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
    
    [HttpPost]
    public async Task<IActionResult> DeleteClient(int id)
    {
        int clientId = id;

        Result result = await _clientService.DeleteClientByIdAsync(clientId);

        switch (result.IsSuccess)
        {
            case true:
                return Ok();
            case false when result.Error == DomainErrors.EntityNotFound:
                return BadRequest(result.Error.Identifier);
            default:
                return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
