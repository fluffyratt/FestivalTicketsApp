using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using FestivalTicketsApp.Shared;

namespace FestivalTicketsApp.IdentityServer.Models;

public class RegisterViewModel
{ 
    [Required] 
    [RegularExpression(RegexTemplates.UsernameTemplate,
        ErrorMessage = """
                       Username must contains only letters, numbers and underscore.
                       Min length is 4, max length is 29!
                       """)] 
    public string? Username { get; set; }
    
    [Required]
    [MinLength(8)]
    [RegularExpression(RegexTemplates.PasswordTemplate, 
        ErrorMessage = """
                       Password must contain only letters, numbers and special symbols.
                       Required combination with eight symbols which contains
                       at least one number, one lowercase and one uppercase!
                       """)]
    public string? Password { get; set; }
    
    [Required]
    [DisplayName("Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "Mismatch confirm password")]
    public string? ConfirmPassword { get; set; }
    
    [Required, EmailAddress] 
    public string? Email { get; set; }
    
    [Required]
    [RegularExpression(RegexTemplates.UkrPhoneNumberTemplate, 
        ErrorMessage = "Input your phone number like +380xxxxxxxxxx")] 
    public string? Phone { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string? Name { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string? Surname { get; set; }
    
    [Required]
    public string? Role { get; set; }
    
    [DisplayName("Staff code")]
    public string? StaffCode { get; set; }
    
    public string? PageHandler { get; set; }

    public bool? ShowRoleSelect { get; set; }

    public List<string> RoleList { get; } =
    [
        UserRolesConstants.Client,
        UserRolesConstants.Manager,
        UserRolesConstants.Admin
    ];
    
    public string? ReturnUrl { get; set; }
}