using System.Diagnostics;
using FestivalTicketsApp.Application.ClientService;
using Microsoft.AspNetCore.Mvc;
using FestivalTicketsApp.WebUI.Models.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;

namespace FestivalTicketsApp.WebUI.Controllers;

public class HomeController(IClientService clientService) 
    : Controller
{
    public Task<IActionResult> Index()
    {
        return Task.FromResult((IActionResult)View());
    }
    
    [Authorize]
    public Task<IActionResult> Login(string? returnUrl)
    {
        return Task.FromResult((IActionResult)Redirect(returnUrl ?? "/"));
    }
    
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();

        return SignOut(new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}