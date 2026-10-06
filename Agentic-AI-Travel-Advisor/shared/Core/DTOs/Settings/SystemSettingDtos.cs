namespace TravelAdvisor.Core.DTOs.Settings;

public class SystemSettingDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateSystemSettingRequest
{
    public string Value { get; set; } = string.Empty;
}

/// <summary>Settings any client may read, e.g. to show a maintenance banner or hide the AI assistant.</summary>
public class PublicSettingsDto
{
    public string DefaultCurrency { get; set; } = "LKR";
    public bool AiAssistantEnabled { get; set; } = true;
    public string? MaintenanceMessage { get; set; }
    public int MaxAdvanceBookingDays { get; set; }
    public int GuestCancellationCutoffHours { get; set; }
}
