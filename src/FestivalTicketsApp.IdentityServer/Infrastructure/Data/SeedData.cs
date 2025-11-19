using System.Security.Claims;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using FestivalTicketsApp.IdentityServer.Infrastructure.Configuration;
using FestivalTicketsApp.IdentityServer.Infrastructure.Data.Entities;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FestivalTicketsApp.IdentityServer.Infrastructure.Data;

public static class SeedData
{
    private static readonly List<IdentityRole> Roles = 
    [ 
        new IdentityRole("admin"), 
        new IdentityRole("manager"), 
        new IdentityRole("client")
    ];

    private static readonly Dictionary<ApplicationUser, (string roleName, List<Claim> claims)> Users = new()
    {
        {
            new ApplicationUser
            {
                Id = "62f8d6a0-1fce-4cc9-a6be-4c486ee92522",
                UserName = "alice",
                Email = "AliceSmith@email.com",
                PhoneNumber = "+380950000001",
                EmailConfirmed = true
            },
            (
                "client", 
                [
                    new Claim(JwtClaimTypes.Name, "Alice Smith"),
                    new Claim(JwtClaimTypes.GivenName, "Alice"),
                    new Claim(JwtClaimTypes.FamilyName, "Smith"),
                    new Claim(JwtClaimTypes.Email, "AliceSmith@email.com"),
                    new Claim(JwtClaimTypes.PhoneNumber, "+380950000001")
                ]
            )
        },
        {
            new ApplicationUser
            {
                Id = "52f16653-5583-4e40-b4e9-42ad6064d482",
                UserName = "bob",
                Email = "BobSmith@email.com",
                PhoneNumber = "+380950000002",
                EmailConfirmed = true
            },
            (
                "client",
                [
                    new Claim(JwtClaimTypes.Name, "Bob Smith"),
                    new Claim(JwtClaimTypes.GivenName, "Bob"),
                    new Claim(JwtClaimTypes.FamilyName, "Smith"),
                    new Claim(JwtClaimTypes.Email, "BobSmith@email.com"),
                    new Claim(JwtClaimTypes.PhoneNumber, "+380950000002"),
                ]
            )
        },
        {
            new ApplicationUser
            {
                UserName = "manager1",
                Email = "manager1@email.com",
                PhoneNumber = "+380660000001",
                EmailConfirmed = true,
            },
            (
                "manager",
                [
                    new Claim(JwtClaimTypes.Name, "Daniel Johnson"),
                    new Claim(JwtClaimTypes.GivenName, "Daniel"),
                    new Claim(JwtClaimTypes.FamilyName, "Johnson"),
                    new Claim(JwtClaimTypes.Email, "manager1@email.com"),
                    new Claim(JwtClaimTypes.PhoneNumber, "+380660000001")
                ]
            )
        },
        {
            new ApplicationUser
            {
                UserName = "admin1",
                Email = "admin1@email.com",
                PhoneNumber = "+380990000001",
                EmailConfirmed = true
            },
            (
                "admin",
                [
                    new Claim(JwtClaimTypes.Name, "Artur Saiian"),
                    new Claim(JwtClaimTypes.GivenName, "Artur"),
                    new Claim(JwtClaimTypes.FamilyName, "Saiian"),
                    new Claim(JwtClaimTypes.Email, "admin1@email.com"),
                    new Claim(JwtClaimTypes.PhoneNumber, "+380990000001")
                ]
            )
        }
    };
    
    public static async Task EnsureSeedData(WebApplication app)
    {
        using var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();

        var usersContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await usersContext.Database.EnsureDeletedAsync();
        await SeedUsers(usersContext, userManager, roleManager);
        
        var configContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        var grantContext = scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();
        
        await SeedConfiguration(configContext, grantContext);
    }

    private static async Task SeedUsers(ApplicationDbContext usersContext,
                                        UserManager<ApplicationUser> userManager,
                                        RoleManager<IdentityRole> roleManager)
    {
        await usersContext.Database.MigrateAsync();
        
        foreach (var role in Roles)
        {
            var result = await roleManager.CreateAsync(role);
            
            if (result.Succeeded)
                Log.Debug($"Role {role.Name} created");
            else
                throw new Exception(result.Errors.First().Description);
        }
        
        foreach (var user in Users)
        {
            var result = await userManager.CreateAsync(user.Key, "Pass123$");
            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            result = await userManager.AddClaimsAsync(user.Key, user.Value.claims);
            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }
            
            result = await userManager.AddToRoleAsync(user.Key, user.Value.roleName);
            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }
            
            Log.Debug($"{user.Key.UserName} created");
        }
    }


    private static async Task SeedConfiguration(ConfigurationDbContext configContext,
                                                PersistedGrantDbContext grantContext)
    {
        await configContext.Database.MigrateAsync();
        await grantContext.Database.MigrateAsync();
        
        foreach (var client in Config.Clients)
        {
            await configContext.Clients.AddAsync(client.ToEntity());
        }
        
        foreach (var resource in Config.IdentityResources)
        {
            await configContext.IdentityResources.AddAsync(resource.ToEntity());
        }
        
        foreach (var resource in Config.ApiScopes)
        {
            await configContext.ApiScopes.AddAsync(resource.ToEntity());
        }
        
        await configContext.SaveChangesAsync();
    }
}
