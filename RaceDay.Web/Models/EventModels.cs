using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class EventItem
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
}

public class EventFormModel
{
    public int EventId { get; set; }

    [Required(ErrorMessage = "Event name is required")]
    [StringLength(150, ErrorMessage = "Event name can be at most 150 characters")]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description can be at most 1000 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Date and time are required")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Date and time")]
    public DateTime EventDate { get; set; } = DateTime.Today.AddDays(14).AddHours(7);

    [Required(ErrorMessage = "Location is required")]
    [StringLength(200, ErrorMessage = "Location can be at most 200 characters")]
    public string Location { get; set; } = string.Empty;

    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000")]
    [Display(Name = "Distance (km)")]
    public decimal Distance { get; set; }

    [Required(ErrorMessage = "Event type is required")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "run";
}