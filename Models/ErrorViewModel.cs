namespace optical_care_management_system.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public string? ErrorMessage { get; set; }

    public string? ErrorPath { get; set; }

    public string? StackTrace { get; set; }

    public bool ShowDetails => !string.IsNullOrEmpty(ErrorMessage);
}
