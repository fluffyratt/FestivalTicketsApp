using System.Security.Claims;
using Duende.IdentityServer.Services;
using FestivalTicketsApp.IdentityServer.Infrastructure.Data.Entities;
using FestivalTicketsApp.IdentityServer.Infrastructure.Options.RegistrationCodesOptions;
using FestivalTicketsApp.IdentityServer.Models;
using FestivalTicketsApp.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FestivalTicketsApp.IdentityServer.Pages.Account.Register;

[SecurityHeaders]
[AllowAnonymous]
public class Index(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IIdentityServerInteractionService interactionService,
    IOptions<RegistrationCodesOptions> staffCodes)
    : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly IIdentityServerInteractionService _interactionService = interactionService;
    private readonly IOptions<RegistrationCodesOptions> _staffCodes = staffCodes;

    [BindProperty] 
    public RegisterViewModel RegisterData { get; set; } = new();
    
    public Task<IActionResult> OnGet(string? returnUrl)
    {
        RegisterData.ReturnUrl = returnUrl ?? "~/";
        
        return Task.FromResult((IActionResult)Page());
    }

    public async Task<IActionResult> OnPost(RegisterViewModel registerData)
    {
        if (!ModelState.IsValid) 
            return Page();

        if (!await IsUserUnique(registerData))
        {
            ModelState.AddModelError(String.Empty, "User with such username, email or phone number already exists");
            return Page();
        }

        if (registerData.Role != UserRolesConstants.Client && 
            !await IsStaffCodeValid(registerData.Role!, registerData.StaffCode!))
        {
            ModelState.AddModelError(String.Empty, "Staff code is invalid");
            return Page();
        }
        
        ApplicationUser? newUser = await CreateIdentityUser(registerData);
        if (newUser is not null)
        {
            await _signInManager.PasswordSignInAsync(newUser, registerData.Password!, false, false);
            var context = await _interactionService.GetAuthorizationContextAsync(RegisterData.ReturnUrl);
            
            
            if (context != null)
            {
                // This "can't happen", because if the ReturnUrl was null, then the context would be null
                ArgumentNullException.ThrowIfNull(RegisterData.ReturnUrl, nameof(RegisterData.ReturnUrl));

                if (context.IsNativeClient())
                {
                    // The client is native, so this change in how to
                    // return the response is for better UX for the end user.
                    return this.LoadingPage(RegisterData.ReturnUrl);
                }

                // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                return Redirect(RegisterData.ReturnUrl ?? "~/");
            }

            // request for a local page
            if (Url.IsLocalUrl(RegisterData.ReturnUrl))
            {
                return Redirect(RegisterData.ReturnUrl);
            }
            else if (string.IsNullOrEmpty(RegisterData.ReturnUrl))
            {
                return Redirect("~/");
            }
            else
            {
                // user might have clicked on a malicious link - should be logged
                throw new ArgumentException("invalid return URL");
            }
        }
        
        ModelState.AddModelError(String.Empty, "Something went wrong. Try later");
        return Page();
    }

    private async Task<bool> IsUserUnique(RegisterViewModel registerData)
    {
        bool result = !await _userManager.Users
            .AnyAsync(u => u.NormalizedEmail == registerData.Email!.ToUpper() || 
                           u.PhoneNumber == registerData.Phone || 
                           u.NormalizedUserName == registerData.Username!.ToUpper());
        return result;
    }
    
    private async Task<ApplicationUser?> CreateIdentityUser(RegisterViewModel registerData)
    {
        (ApplicationUser newUser, List<Claim> newUserClaims) = await Extensions.CreateUserFromInput(registerData);
        
        await _userManager.CreateAsync(newUser, registerData.Password!);
        await _userManager.AddClaimsAsync(newUser, newUserClaims);
        await _userManager.AddToRoleAsync(newUser, registerData.Role!);
        
        return newUser;
    }

    private Task<bool> IsStaffCodeValid(string role, string staffCode)
    {
        return role switch
        {
            UserRolesConstants.Manager when staffCode == _staffCodes.Value.ManagerCode
                => Task.FromResult(true),
            UserRolesConstants.Admin when staffCode == _staffCodes.Value.AdminCode
                => Task.FromResult(true),
            _ => Task.FromResult(false)
        };
    }
}