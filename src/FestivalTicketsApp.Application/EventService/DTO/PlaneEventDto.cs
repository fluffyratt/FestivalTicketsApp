namespace FestivalTicketsApp.Application.EventService.DTO;

public record PlaneEventDto(string Title,
                            int EventTypeId,
                            int GenreId,
                            int HostId,
                            string Description,
                            DateTime StartTime,
                            int Duration,
                            List<CreateTicketTypeDto>? TicketTypes,
                            List<string>? TypesMapping);

public record CreateTicketTypeDto(string Name,
                                  decimal Price);
