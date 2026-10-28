namespace RaceDay.Api.Security;

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
    public static readonly string[] All = { Organiser, Participant };
}

public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
}
