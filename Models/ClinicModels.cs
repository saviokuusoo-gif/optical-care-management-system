using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace optical_care_management_system.Models;

public enum UserRole
{
    Admin,
    Patient
}

public enum AppointmentStatus
{
    Pending,
    Approved,
    Canceled
}

public class UserAccount
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Patient
{
    public int Id { get; set; }

    [Required]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    public string Gender { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string EmergencyContact { get; set; } = string.Empty;

    public int? UserAccountId { get; set; }
    public UserAccount? UserAccount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<ExaminationRecord> ExaminationRecords { get; set; } = new List<ExaminationRecord>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}

public class Doctor
{
    public int Id { get; set; }

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string Specialty { get; set; } = string.Empty;

    [Required]
    public string LicenseNumber { get; set; } = string.Empty;

    public int? UserAccountId { get; set; }
    public UserAccount? UserAccount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<ExaminationRecord> ExaminationRecords { get; set; } = new List<ExaminationRecord>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}

public class Appointment
{
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    public int? DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    [Required]
    public DateTime AppointmentDate { get; set; }

    [Required]
    public string TimeSlot { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ExaminationRecord
{
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    [Required]
    public int DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    public DateTime ExamDate { get; set; } = DateTime.UtcNow;

    [Required]
    public string LeftEye { get; set; } = string.Empty;

    [Required]
    public string RightEye { get; set; } = string.Empty;

    [Required]
    public string VisualAcuity { get; set; } = string.Empty;

    [Required]
    public string EyePressure { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}

public class Prescription
{
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    [Required]
    public int DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    [Required]
    public int ExaminationRecordId { get; set; }
    public ExaminationRecord? ExaminationRecord { get; set; }

    public DateTime PrescriptionDate { get; set; } = DateTime.UtcNow;

    [Required]
    public string LensType { get; set; } = string.Empty;

    public string LeftEye { get; set; } = string.Empty;
    public string RightEye { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class InventoryItem
{
    public int Id { get; set; }

    [Required]
    public string ItemName { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int StockQuantity { get; set; }

    public decimal UnitPrice { get; set; }

    public int LowStockThreshold { get; set; } = 5;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<ExaminationRecord> ExaminationRecords => Set<ExaminationRecord>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Frame> Frames => Set<Frame>();
public DbSet<FrameBooking> FrameBookings => Set<FrameBooking>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Doctor)
            .WithMany(d => d.Appointments)
            .HasForeignKey(a => a.DoctorId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExaminationRecord>()
            .HasOne(e => e.Patient)
            .WithMany(p => p.ExaminationRecords)
            .HasForeignKey(e => e.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExaminationRecord>()
            .HasOne(e => e.Doctor)
            .WithMany(d => d.ExaminationRecords)
            .HasForeignKey(e => e.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Prescription>()
            .HasOne(p => p.Patient)
            .WithMany(p => p.Prescriptions)
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Prescription>()
            .HasOne(p => p.Doctor)
            .WithMany(d => d.Prescriptions)
            .HasForeignKey(p => p.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Prescription>()
            .HasOne(p => p.ExaminationRecord)
            .WithMany(e => e.Prescriptions)
            .HasForeignKey(p => p.ExaminationRecordId)
            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FrameBooking>()
    .HasOne(f => f.Patient)
    .WithMany()
    .HasForeignKey(f => f.PatientId)
    .OnDelete(DeleteBehavior.Restrict);


modelBuilder.Entity<FrameBooking>()
    .HasOne(f => f.Frame)
    .WithMany()
    .HasForeignKey(f => f.FrameId)
    .OnDelete(DeleteBehavior.Restrict);
    }
}
