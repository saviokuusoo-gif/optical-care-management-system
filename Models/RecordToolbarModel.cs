namespace optical_care_management_system.Models;

/// <summary>
/// Backing model for the shared _RecordToolbar partial (Print + Export to Excel).
/// </summary>
public class RecordToolbarModel
{
    /// <summary>Report name shown in the printed page header.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Admin controller action that streams the .xlsx file.</summary>
    public string ExportAction { get; set; } = string.Empty;

    /// <summary>Optional row count printed alongside the title.</summary>
    public int? RecordCount { get; set; }
}
