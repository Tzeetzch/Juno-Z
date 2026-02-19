namespace JunoBank.Application.DTOs;

/// <summary>
/// Data collected during the setup wizard.
/// </summary>
public class SetupData
{
    public required AdminData Admin { get; set; }
    public PartnerData? Partner { get; set; }
    public List<ChildData> Children { get; set; } = new();
    public EmailConfigData? Email { get; set; }
}

public class AdminData
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class PartnerData
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class ChildData
{
    public required string Name { get; set; }
    public required DateTime Birthday { get; set; }
    public required decimal StartingBalance { get; set; }
    public required string[] PictureSequence { get; set; }
}

public class SetupResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public int? AdminUserId { get; init; }

    public static SetupResult Succeeded(int adminUserId) => new() { Success = true, AdminUserId = adminUserId };
    public static SetupResult Failed(string error) => new() { Success = false, Error = error };
}
