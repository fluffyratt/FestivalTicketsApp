namespace FestivalTicketsApp.Core.Entities;

public class Client : BaseEntity
{
    public string Name { get; set; } = default!;

    public string Surname { get; set; } = default!;

    public string Email { get; set; } = default!;

    public string Phone { get; set; } = default!;

    public string Subject { get; set; } = default!;

    public string PasswordHash { get; set; } = default!;

    // НОВЕ: роль користувача ("User" або "Organizer")
    public string Role { get; set; } = "User";

    public List<Ticket> PurchasedTickets { get; set; } = [];

    public List<Event> FavouriteEvents { get; set; } = [];
}
