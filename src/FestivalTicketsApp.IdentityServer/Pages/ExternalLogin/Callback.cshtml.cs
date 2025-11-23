// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Security.Claims;
using Duende.IdentityServer;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using FestivalTicketsApp.IdentityServer.Infrastructure.Data.Entities;
using FestivalTicketsApp.IdentityServer.Models;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FestivalTicketsApp.IdentityServer.Pages.ExternalLogin;

[AllowAnonymous]
[SecurityHeaders]
public class Callback : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IIdentityServerInteractionService _interaction;
    private readonly ILogger<Callback> _logger;
    private readonly IEventService _events;
    
    [BindProperty]
    public RegisterViewModel ViewModel { get; set; } = new();

    public Callback(
        IIdentityServerInteractionService interaction,
        IEventService events,
        ILogger<Callback> logger,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _interaction = interaction;
        _logger = logger;
        _events = events;
    }

    public async Task<IActionResult> OnGet()
    {
        // read external identity from the temporary cookie
        var result = await HttpContext.AuthenticateAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"External authentication error: {result.Failure}");
        }
        
        var returnUrl = result.Properties.Items["returnUrl"] ?? "~/";

        var externalUser = result.Principal ??
                           throw new InvalidOperationException("External authentication produced a null Principal");

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var externalClaims = externalUser.Claims.Select(c => $"{c.Type}: {c.Value}");
            _logger.ExternalClaims(externalClaims);
        }

        // lookup our user and external provider info
        // try to determine the unique id of the external user (issued by the provider)
        // the most common claim type for that are the sub claim and the NameIdentifier
        // depending on the external provider, some other claim type might be used
        var userIdClaim = externalUser.FindFirst(JwtClaimTypes.Subject) ??
                          externalUser.FindFirst(ClaimTypes.NameIdentifier) ??
                          throw new InvalidOperationException("Unknown userid");

        var provider = result.Properties.Items["scheme"] ??
                       throw new InvalidOperationException("Null scheme in authentiation properties");
        var providerUserId = userIdClaim.Value;

        // find external user
        var user = await _userManager.FindByLoginAsync(provider, providerUserId);
        if (user == null)
        {
            // this might be where you might initiate a custom workflow for user registration
            // in this sample we don't show how that would be done, as our sample implementation
            // simply auto-provisions new external user
            await PutUserDataToViewModel(externalUser.Claims.ToList(), returnUrl);
            return Page();
        }

        await SignInExternalUser(result, externalUser, user, returnUrl);
        
        return Redirect(returnUrl);
    }

    public async Task<IActionResult> OnPost()
    {
        if (ModelState.IsValid)
        {
            var externalAuthResult = await HttpContext.AuthenticateAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);
            if (!externalAuthResult.Succeeded)
            {
                throw new InvalidOperationException($"External authentication error: {externalAuthResult.Failure}");
            }
            
            var externalUser = externalAuthResult.Principal;
            var userIdClaim = externalUser.FindFirst(JwtClaimTypes.Subject) ??
                              externalUser.FindFirst(ClaimTypes.NameIdentifier);
            var provider = externalAuthResult.Properties.Items["scheme"];
            var providerUserId = userIdClaim.Value;

            try
            {
                ApplicationUser? user = await _userManager.Users
                    .SingleOrDefaultAsync(u =>
                        u.NormalizedEmail == ViewModel.Email!.ToUpper() ||
                        u.PhoneNumber == ViewModel.Phone! ||
                        u.NormalizedUserName == ViewModel.Username!.ToUpper());

                if (user is null)
                {
                    (user, List<Claim> newUserClaims) = await Extensions.CreateUserFromInput(ViewModel);
                    await _userManager.CreateAsync(user, ViewModel.Password!);
                    await _userManager.AddClaimsAsync(user, newUserClaims);
                    await _userManager.AddToRoleAsync(user, ViewModel.Role!);
                }
                else if (!await _userManager.CheckPasswordAsync(user, ViewModel.Password!))
                {
                    ModelState.AddModelError(String.Empty,
                        "Related user has been found, however password mismatches");
                    return Page();
                }

                await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerUserId, provider));
                await SignInExternalUser(externalAuthResult, externalUser, user!, ViewModel.ReturnUrl!);

                return Redirect(ViewModel.ReturnUrl!);
            }
            catch (InvalidOperationException)
            {
                ModelState.AddModelError(String.Empty,
                    "Related user has been found, however mandatory credentials mismatch");
                return Page();
            }
        }

        return Page();
    }

    private async Task SignInExternalUser(AuthenticateResult externalAuthResult,
                                          ClaimsPrincipal externalUser,
                                          ApplicationUser user,
                                          string returnUrl)
    {
        // For logging
        var userIdClaim = externalUser.FindFirst(JwtClaimTypes.Subject) ??
                          externalUser.FindFirst(ClaimTypes.NameIdentifier) ??
                          throw new InvalidOperationException("Unknown userid");

        var provider = externalAuthResult.Properties.Items["scheme"] ??
                       throw new InvalidOperationException("Null scheme in authentiation properties");
        var providerUserId = userIdClaim.Value;
        
        // this allows us to collect any additional claims or properties
        // for the specific protocols used and store them in the local auth cookie.
        // this is typically used to store data needed for signout from those protocols.
        var additionalLocalClaims = new List<Claim>();
        var localSignInProps = new AuthenticationProperties();
        await CaptureExternalLoginContext(externalAuthResult, additionalLocalClaims, localSignInProps);

        // issue authentication cookie for user
        await _signInManager.SignInWithClaimsAsync(user, localSignInProps, additionalLocalClaims);

        // delete temporary cookie used during external authentication
        await HttpContext.SignOutAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme);
        
        // check if external login is in the context of an OIDC request
        var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
        await _events.RaiseAsync(new UserLoginSuccessEvent(externalAuthResult.Properties.Items["scheme"], 
            providerUserId, user.Id, user.UserName, true,
            context?.Client.ClientId));
        Telemetry.Metrics.UserLogin(context?.Client.ClientId, provider!);
    }

    private Task PutUserDataToViewModel(List<Claim> externalUserClaims, string returnUrl)
    {
        ViewModel.Username = externalUserClaims.Find(x => x.Type == JwtClaimTypes.Name)?.Value ??
                              externalUserClaims.Find(x => x.Type == ClaimTypes.Name)?.Value ??
                              "";
        ViewModel.Username = ViewModel.Username.Replace(" ", "_");
        
        ViewModel.Email = externalUserClaims.Find(
                               x => x.Type == JwtClaimTypes.Email)?.Value ?? 
                           externalUserClaims.Find(
                               x => x.Type == ClaimTypes.Email)?.Value ?? 
                           "";
        
        ViewModel.Name = externalUserClaims.Find(x => x.Type == JwtClaimTypes.GivenName)?.Value ??
                          externalUserClaims.Find(x => x.Type == ClaimTypes.GivenName)?.Value ??
                          "";
        
        ViewModel.Surname = externalUserClaims.Find(x => x.Type == JwtClaimTypes.FamilyName)?.Value ?? 
                             externalUserClaims.Find(x => x.Type == ClaimTypes.Surname)?.Value ?? 
                             "";
        ViewModel.ReturnUrl = returnUrl;

        return Task.CompletedTask;
    }
    
    // if the external login is OIDC-based, there are certain things we need to preserve to make logout work
    // this will be different for WS-Fed, SAML2p or other protocols
    private Task CaptureExternalLoginContext(AuthenticateResult externalResult, List<Claim> localClaims,
                                                    AuthenticationProperties localSignInProps)
    {
        ArgumentNullException.ThrowIfNull(externalResult.Principal, nameof(externalResult.Principal));

        // capture the idp used to login, so the session knows where the user came from
        localClaims.Add(new Claim(JwtClaimTypes.IdentityProvider,
            externalResult.Properties?.Items["scheme"] ?? "unknown identity provider"));

        // if the external system sent a session id claim, copy it over
        // so we can use it for single sign-out
        var sid = externalResult.Principal.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId);
        if (sid != null)
        {
            localClaims.Add(new Claim(JwtClaimTypes.SessionId, sid.Value));
        }

        // if the external provider issued an id_token, we'll keep it for signout
        var idToken = externalResult.Properties?.GetTokenValue("id_token");
        if (idToken != null)
        {
            localSignInProps.StoreTokens(new[] { new AuthenticationToken { Name = "id_token", Value = idToken } });
        }

        return Task.CompletedTask;
    }
}