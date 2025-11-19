namespace FestivalTicketsApp.Application.ClientService.DTO;

public record ClientCreateDto(string Name, 
                              string Surname, 
                              string Email,
                              string Phone,
                              string Subject);