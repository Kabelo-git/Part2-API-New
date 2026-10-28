using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs;

// ---------- Auth and profile ----------
public class RegisterDto
{
    [Required, StringLength(100)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Password { get; set; } = string.Empty;
    [Required] public string Role { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class UpdateProfileDto
{
    [Required, StringLength(100)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = string.Empty;
}

public record UserProfileDto(int UserId, string FullName, string Email, string Role);

// ---------- Events ----------
public class EventDto
{
    [Required, StringLength(150)] public string EventName { get; set; } = string.Empty;
    [StringLength(1000)] public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    [Required, StringLength(200)] public string Location { get; set; } = string.Empty;
    [Range(0.1, 1000)] public decimal Distance { get; set; }
    [Required, RegularExpression("(?i)^(run|walk|cycle)$", ErrorMessage = "EventType must be run, walk or cycle.")]
    public string EventType { get; set; } = string.Empty;
}

public record EventResponseDto(int EventId, int OrganiserId, string EventName, string Description,
    DateTime EventDate, string Location, decimal Distance, string EventType);

// ---------- Categories ----------
public class CategoryDto
{
    [Required, StringLength(100)] public string CategoryName { get; set; } = string.Empty;
    [Range(0, 1000)] public decimal Distance { get; set; }
    [Range(0, 100000)] public decimal Price { get; set; }
}

public record CategoryResponseDto(int CategoryId, int EventId, string CategoryName, decimal Distance, decimal Price);

// ---------- Enrolments ----------
public class EnrolDto
{
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
}

public record EnrolmentResponseDto(int EnrolmentId, int ParticipantId, string ParticipantName, int EventId,
    string EventName, int CategoryId, string CategoryName, DateTime EnrolmentDate, string Status);

// ---------- Results ----------
public class CreateResultDto
{
    [Range(1, int.MaxValue)] public int EnrolmentId { get; set; }
    public TimeSpan FinishTime { get; set; }
    [Range(1, int.MaxValue)] public int Position { get; set; }
}

public record ResultResponseDto(int ResultId, int EnrolmentId, int ParticipantId, int EventId,
    string EventName, TimeSpan FinishTime, int Position);