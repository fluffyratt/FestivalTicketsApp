using System.ComponentModel.DataAnnotations;

namespace FestivalTicketsApp.WebUI.Models;

public class RegisterClientViewModel
{
    [Required]
    [MaxLength(50)]
    public string? Name { get; set; }

    [Required]
    [MaxLength(50)]
    public string? Surname { get; set; }

    [Required, EmailAddress]
    public string? Email { get; set; }

    [Required]
    public string? Phone { get; set; }

    [Required, MinLength(6)]
    public string? Password { get; set; }

    [Required]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string? ConfirmPassword { get; set; }

    // НОВЕ: роль користувача ("User" або "Organizer")
    [Required]
    public string? Role { get; set; }
}

public class LoginClientViewModel
{
    [Required, EmailAddress]
    public string? Email { get; set; }

    [Required]
    public string? Password { get; set; }

    public string? ReturnUrl { get; set; }
}
