using System.ComponentModel.DataAnnotations;

namespace optical_care_management_system.Models;

public class LoginViewModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class BookingViewModel
{
    [Required]
    public DateTime AppointmentDate { get; set; } = DateTime.Today;

    [Required]
    public string TimeSlot { get; set; } = string.Empty;

    /// <summary>Optional preferred doctor; admin can assign one later if left blank.</summary>
    [Display(Name = "Preferred Doctor")]
    public int? DoctorId { get; set; }

    public string Notes { get; set; } = string.Empty;
}

public class DashboardViewModel
{
    public int TotalPatients { get; set; }
    public int TodayAppointments { get; set; }
    public int ActiveDoctors { get; set; }
    public int LowInventoryItems { get; set; }
}
