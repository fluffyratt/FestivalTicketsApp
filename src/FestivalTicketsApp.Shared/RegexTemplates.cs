namespace FestivalTicketsApp.Shared;

public static class RegexTemplates
{
    public const string UkrPhoneNumberTemplate = @"\+380\d{9}";
    public const string UsernameTemplate = "^[A-Za-z][A-Za-z0-9_]{4,29}$";
    public const string PasswordTemplate = @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d\S]{8,}$";
}