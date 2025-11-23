using System.Data;
using System.Security.Claims;
using FestivalTicketsApp.Application.ClientService;
using FestivalTicketsApp.Application.ClientService.DTO;
using FestivalTicketsApp.Shared;
using FluentValidation;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace FestivalTicketsApp.WebUI.Helpers;

public static class OpenIdEventHandlers
{
    public static readonly Func<UserInformationReceivedContext, Task> OnUserInformationReceived =
        async context =>
        {
            var userInfo = context.User.RootElement;
            var userRole = userInfo.TryGetValue(JwtClaimTypes.Role);

            if (userRole.ValueEquals(UserRolesConstants.Client))
            {
                var clientService = context.HttpContext
                    .RequestServices
                    .GetRequiredService<IClientService>();

                var clientCreateValidator = context.HttpContext
                    .RequestServices
                    .GetRequiredService<IValidator<ClientCreateDto>>();

                int? clientId;

                Result<int> findClientResult = await clientService
                    .GetClientIdBySubjectAsync(
                        userInfo.TryGetValue(JwtClaimTypes.Subject).ToString());

                if (!findClientResult.IsSuccess)
                {
                    ClientCreateDto newUserDto = new(
                        userInfo.TryGetValue(JwtClaimTypes.GivenName).ToString(),
                        userInfo.TryGetValue(JwtClaimTypes.FamilyName).ToString(),
                        userInfo.TryGetValue(JwtClaimTypes.Email).ToString(),
                        userInfo.TryGetValue(JwtClaimTypes.PhoneNumber).ToString(),
                        userInfo.TryGetValue(JwtClaimTypes.Subject).ToString());

                    var validationResult = await clientCreateValidator.ValidateAsync(newUserDto);

                    if (!validationResult.IsValid)
                        throw new ConstraintException("Invalid client data");

                    var createClientResult = await clientService.CreateClientAsync(newUserDto);

                    if (!createClientResult.IsSuccess)
                        throw new ConstraintException("Invalid client data");

                    clientId = createClientResult.Value;
                }
                else
                    clientId = findClientResult.Value;

                var identity = context.Principal!.Identity as ClaimsIdentity;
                identity.AddClaim(new Claim(JwtClaimTypes.Actor, clientId.ToString()));
            }
        };

    public static readonly Func<RemoteFailureContext, Task> OnRemoteFailure = context =>
    {
        context.Response.Redirect("/");
        context.HandleResponse();
        return Task.CompletedTask;
    };
}