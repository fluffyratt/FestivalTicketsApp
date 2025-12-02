using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FestivalTicketsApp.Core.Entities;
using FestivalTicketsApp.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace FestivalTicketsApp.WebUI.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    /// <summary>
    /// Реєстрація клієнта з роллю: "User" або "Organizer".
    /// Register client with role: "User" or "Organizer".
    /// </summary>
    public async Task<bool> RegisterAsync(
        string email,
        string password,
        string name,
        string surname,
        string phone,
        string role)       // NEW: role
    {
        if (await _context.Clients.AnyAsync(c => c.Email == email))
            return false;

        // захист від дивних значень ролі
        if (string.IsNullOrWhiteSpace(role))
            role = "User";

        role = role switch
        {
            "Organizer" => "Organizer",
            _ => "User"
        };

        var client = new Client
        {
            Name = name,
            Surname = surname,
            Email = email,
            Phone = phone,
            Subject = "",
            PasswordHash = HashPassword(password),
            Role = role     // NEW: зберігаємо роль у БД
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        await SignInAsync(client);

        return true;
    }

    /// <summary>
    /// Логін: повертає роль ("User" / "Organizer") або null, якщо невірні дані.
    /// Login: returns role ("User" / "Organizer") or null if invalid credentials.
    /// </summary>
    public async Task<string?> LoginAsync(string email, string password)
    {
        string hash = HashPassword(password);

        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Email == email && c.PasswordHash == hash);

        if (client is null)
            return null;

        await SignInAsync(client);

        // повертаємо роль для контролера
        return client.Role;
    }

    private async Task SignInAsync(Client client)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, client.Id.ToString()),
            new Claim(ClaimTypes.Name, $"{client.Name} {client.Surname}"),
            new Claim(ClaimTypes.Email, client.Email),

            // NEW: роль беремо з клієнта ("User" або "Organizer")
            new Claim(ClaimTypes.Role, client.Role)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await _httpContextAccessor.HttpContext!.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);
    }

    public async Task LogoutAsync()
    {
        await _httpContextAccessor.HttpContext!
            .SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
