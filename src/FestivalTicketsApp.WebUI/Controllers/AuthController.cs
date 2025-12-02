using FestivalTicketsApp.WebUI.Models;
using FestivalTicketsApp.WebUI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FestivalTicketsApp.WebUI.Controllers;

public class AuthController : Controller
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterClientViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterClientViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var ok = await _authService.RegisterAsync(
            model.Email!,
            model.Password!,
            model.Name!,
            model.Surname!,
            model.Phone!,
            model.Role!);   // передаємо роль

        if (!ok)
        {
            ModelState.AddModelError("", "User with this email already exists");
            return View(model);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginClientViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginClientViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // LoginAsync тепер повертає роль ("User" / "Organizer") або null
        string? role = await _authService.LoginAsync(model.Email!, model.Password!);

        if (role == null)
        {
            ModelState.AddModelError("", "Invalid email or password");
            return View(model);
        }

        // Якщо був ReturnUrl – повертаємо туди
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        // Якщо це звичайний користувач – ведемо на сторінку подій з фільтрами
        if (role == "User")
        {
            // контролер з подіями у тебе називається Event
            return RedirectToAction("Index", "Event");
        }

        // Якщо це організатор – ведемо в кабінет організатора
        if (role == "Organizer")
        {
            return RedirectToAction("Index", "Organizer");
        }

        // Запасний варіант
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return RedirectToAction("Index", "Home");
    }
}

