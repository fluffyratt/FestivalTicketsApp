using System.Security.Claims;
using FestivalTicketsApp.Core.Entities;
using FestivalTicketsApp.Infrastructure.Data;
using FestivalTicketsApp.WebUI.Models.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FestivalTicketsApp.WebUI.Controllers;

public class ProfileController : Controller
{
    private readonly AppDbContext _context;

    public ProfileController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // дістаємо Id клієнта з кукі (ми його записували в ClaimTypes.NameIdentifier)
        string? userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 🔹 Якщо користувач не залогінений – показуємо спеціальну сторінку
        if (string.IsNullOrEmpty(userIdRaw) || !int.TryParse(userIdRaw, out int clientId))
        {
            // View: Views/Profile/NotAuthenticated.cshtml
            return View("NotAuthenticated");
        }

        // 🔹 Якщо залогінений – вантажимо його дані з БД
        Client? client = await _context.Clients
            .Include(c => c.PurchasedTickets)
            .FirstOrDefaultAsync(c => c.Id == clientId);

        if (client is null)
        {
            return NotFound();
        }

        var viewModel = new UserProfileViewModel
        {
            Name = client.Name,
            Surname = client.Surname,
            Email = client.Email,
            Phone = client.Phone,
            Tickets = client.PurchasedTickets
                .Select(t => new TicketInfoViewModel
                {
                    Id = t.Id
                    // тут згодом можна додати EventName, Date, Seat, Price тощо
                })
                .ToList()
        };

        // View: Views/Profile/Index.cshtml
        return View(viewModel);
    }
}
