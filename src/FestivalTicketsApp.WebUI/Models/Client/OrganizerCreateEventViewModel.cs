using System.ComponentModel.DataAnnotations;

namespace FestivalTicketsApp.WebUI.Models.Client;

public class OrganizerCreateEventViewModel
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = default!;

    [Required]
    [Display(Name = "Genre")]
    public int EventGenreId { get; set; }

    [Required]
    [Display(Name = "Location")]
    public int LocationId { get; set; }

    [Required]
    [Display(Name = "Status")]
    public int EventStatusId { get; set; }

    [Required]
    [Display(Name = "Start date")]
    public DateTime StartDate { get; set; }

    [Required]
    [Range(1, 1000)]
    [Display(Name = "Duration (minutes)")]
    public int Duration { get; set; }

    public string? Description { get; set; }

    public List<(int Id, string Name)> Genres { get; set; } = new();
    public List<(int Id, string Name)> Locations { get; set; } = new();
    public List<(int Id, string Name)> Statuses { get; set; } = new();
}
