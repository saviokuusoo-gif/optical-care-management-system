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

public class DoctorDashboardViewModel
{
    public Doctor? Doctor { get; set; }
    public int TodayAppointmentsCount { get; set; }
    public int TotalExaminationsCount { get; set; }
    public int TotalPrescriptionsCount { get; set; }
    public int TotalPatientsTreatedCount { get; set; }
    public List<Appointment> TodayAppointments { get; set; } = new();
    public List<ExaminationRecord> RecentExaminations { get; set; } = new();
    public List<Prescription> RecentPrescriptions { get; set; } = new();
}

public class DoctorFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string Specialty { get; set; } = string.Empty;

    [Required]
    [Display(Name = "License Number")]
    public string LicenseNumber { get; set; } = string.Empty;

    [Display(Name = "Doctor Login Username")]
    public string? Username { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Login Password")]
    public string? Password { get; set; }
}

public class DoctorDetailsViewModel
{
    public Doctor Doctor { get; set; } = null!;
    public List<Appointment> Appointments { get; set; } = new();
    public List<ExaminationRecord> ExaminationRecords { get; set; } = new();
    public List<Prescription> Prescriptions { get; set; } = new();
    public List<Patient> TreatedPatients { get; set; } = new();
}
