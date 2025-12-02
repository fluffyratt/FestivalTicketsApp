using FestivalTicketsApp.Core.Entities;
using FestivalTicketsApp.Infrastructure.Data;
using FestivalTicketsApp.WebUI.Models.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FestivalTicketsApp.WebUI.Controllers;

[Authorize(Roles = "Organizer")]
public class OrganizerController : Controller
{
    private readonly AppDbContext _context;

    public OrganizerController(AppDbContext context)
    {
        _context = context;
    }

    // ====== КАБІНЕТ ОРГАНІЗАТОРА (список івентів) ======
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var events = await _context.Events
            .OrderBy(e => e.Id)
            .ToListAsync();

        var vm = new OrganizerDashboardViewModel
        {
            Events = events
                .Select(e => new OrganizerEventViewModel
                {
                    Id = e.Id,
                    Title = e.Title
                })
                .ToList()
        };

        return View(vm);
    }

    // ====== СТВОРЕННЯ ІВЕНТУ (GET) ======
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new OrganizerCreateEventViewModel
        {
            // просто дефолтні значення для форми
            StartDate = DateTime.Today.AddHours(18),
            Duration = 60
        };

        await LoadSelectListsAsync(vm);

        return View(vm);
    }

    // ====== СТВОРЕННЯ ІВЕНТУ (POST) ======
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrganizerCreateEventViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync(model);
            return View(model);
        }

        // шукаємо зал (Host) за вибраною локацією
        var host = await _context.Hosts
            .FirstOrDefaultAsync(h => h.LocationId == model.LocationId);

        if (host is null)
        {
            ModelState.AddModelError(nameof(model.LocationId),
                "Для цієї локації ще не створено залу (Host).");
            await LoadSelectListsAsync(model);
            return View(model);
        }

        // створюємо ТІЛЬКИ Event
        var ev = new Event
        {
            Title = model.Title,
            EventGenreId = model.EventGenreId,
            HostId = host.Id,
            EventStatusId = model.EventStatusId
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        // TODO: окремо додати збереження EventDetails,
        // коли буде зрозуміла сутність EventDetails у Core.Entities.

        return RedirectToAction(nameof(Index));
    }

    // ====== РЕДАГУВАННЯ ІВЕНТУ (GET) ======
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var ev = await _context.Events
            .Include(e => e.Host)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ev is null)
            return NotFound();

        var vm = new OrganizerCreateEventViewModel
        {
            Id = ev.Id,
            Title = ev.Title,
            EventGenreId = ev.EventGenreId,
            LocationId = ev.Host.LocationId,
            EventStatusId = ev.EventStatusId,

            // Поки що немає EventDetails — ставимо якісь дефолтні
            StartDate = DateTime.Today.AddHours(18),
            Duration = 60,
            Description = null
        };

        await LoadSelectListsAsync(vm);

        return View(vm);
    }

    // ====== РЕДАГУВАННЯ ІВЕНТУ (POST) ======
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(OrganizerCreateEventViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync(model);
            return View(model);
        }

        var ev = await _context.Events
            .Include(e => e.Host)
            .FirstOrDefaultAsync(e => e.Id == model.Id);

        if (ev is null)
            return NotFound();

        var host = await _context.Hosts
            .FirstOrDefaultAsync(h => h.LocationId == model.LocationId);

        if (host is null)
        {
            ModelState.AddModelError(nameof(model.LocationId),
                "Для цієї локації ще не створено залу (Host).");
            await LoadSelectListsAsync(model);
            return View(model);
        }

        // оновлюємо ТІЛЬКИ Event
        ev.Title = model.Title;
        ev.EventGenreId = model.EventGenreId;
        ev.HostId = host.Id;
        ev.EventStatusId = model.EventStatusId;

        await _context.SaveChangesAsync();

        // TODO: аналогічно — коли буде сутність EventDetails,
        // тут можна буде оновлювати опис/дату/тривалість.

        return RedirectToAction(nameof(Index));
    }

    // ====== ХЕЛПЕР: заповнити списки для селектів ======
    private async Task LoadSelectListsAsync(OrganizerCreateEventViewModel vm)
    {
        vm.Genres = await _context.EventGenres
            .OrderBy(g => g.Genre)
            .Select(g => new ValueTuple<int, string>(g.Id, g.Genre))
            .ToListAsync();

        vm.Locations = await _context.Locations
            .OrderBy(l => l.CityName)
            .Select(l => new ValueTuple<int, string>(
                l.Id,
                l.CityName + ", " + l.StreetName + " " + l.BuildingNumber))
            .ToListAsync();

        vm.Statuses = await _context.EventStatuses
            .OrderBy(s => s.Status)
            .Select(s => new ValueTuple<int, string>(s.Id, s.Status))
            .ToListAsync();
    }
}
