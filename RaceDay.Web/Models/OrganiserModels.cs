using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class CategoryItem
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public decimal Price { get; set; }
}

public class CategoryFormModel
{
    public int EventId { get; set; }

    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, ErrorMessage = "Category name can be at most 100 characters")]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0, 1000, ErrorMessage = "Distance must be between 0 and 1000")]
    public decimal Distance { get; set; }

    [Range(0, 100000, ErrorMessage = "Price must be 0 or more")]
    public decimal Price { get; set; }
}

public class CategoriesViewModel
{
    public EventItem Event { get; set; } = new();
    public List<CategoryItem> Categories { get; set; } = new();
}

public class EnrolmentItem
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class EnrolmentsViewModel
{
    public EventItem Event { get; set; } = new();
    public List<EnrolmentItem> Enrolments { get; set; } = new();
}

public class ResultItem
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
}

public class ResultFormModel
{
    public int EventId { get; set; }
    public int EnrolmentId { get; set; }

    [Required(ErrorMessage = "Finish time is required")]
    public string FinishTime { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "Position must be 1 or more")]
    public int Position { get; set; }
}

public class ResultsViewModel
{
    public EventItem Event { get; set; } = new();
    public List<EnrolmentItem> Enrolments { get; set; } = new();
    public List<ResultItem> Results { get; set; } = new();
}

public class DashboardRow
{
    public EventItem Event { get; set; } = new();
    public int Enrolments { get; set; }
}

public class DashboardViewModel
{
    public List<DashboardRow> Rows { get; set; } = new();
}