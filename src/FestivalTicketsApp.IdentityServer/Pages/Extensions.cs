// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Security.Claims;
using Duende.IdentityServer.Models;
using FestivalTicketsApp.IdentityServer.Infrastructure.Data.Entities;
using FestivalTicketsApp.IdentityServer.Models;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FestivalTicketsApp.IdentityServer.Pages;

public static class Extensions
{
    /// <summary>
    /// Determines if the authentication scheme support signout.
    /// </summary>
    internal static async Task<bool> GetSchemeSupportsSignOutAsync(this HttpContext context, string scheme)
    {
        var provider = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
        var handler = await provider.GetHandlerAsync(context, scheme);
        return (handler is IAuthenticationSignOutHandler);
    }

    /// <summary>
    /// Checks if the redirect URI is for a native client.
    /// </summary>
    internal static bool IsNativeClient(this AuthorizationRequest context)
    {
        return !context.RedirectUri.StartsWith("https", StringComparison.Ordinal)
            && !context.RedirectUri.StartsWith("http", StringComparison.Ordinal);
    }

    /// <summary>
    /// Renders a loading page that is used to redirect back to the redirectUri.
    /// </summary>
    internal static IActionResult LoadingPage(this PageModel page, string? redirectUri)
    {
        page.HttpContext.Response.StatusCode = 200;
        page.HttpContext.Response.Headers["Location"] = "";

        return page.RedirectToPage("/Redirect/Index", new { RedirectUri = redirectUri });
    }

    internal static Task<(
        ApplicationUser user, 
        List<Claim> claims)> CreateUserFromInput(RegisterViewModel registerData)
    {
        ApplicationUser newUser = new()
        {
            UserName = registerData.Username,
            Email = registerData.Email,
            EmailConfirmed = true,
            PhoneNumber = registerData.Phone,
        };
        
        List<Claim> newUserClaims =
        [
            new Claim(JwtClaimTypes.Name, string.Join(' ', registerData.Name, registerData.Surname)),
            new Claim(JwtClaimTypes.GivenName, registerData.Name!),
            new Claim(JwtClaimTypes.FamilyName, registerData.Surname!),
            new Claim(JwtClaimTypes.Email, registerData.Email!),
            new Claim(JwtClaimTypes.PhoneNumber, registerData.Phone!)
        ];

        return Task.FromResult((newUser, newUserClaims));
    }
}