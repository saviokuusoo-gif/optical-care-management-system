using System.Security.Claims;
using optical_care_management_system.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace optical_care_management_system.Controllers;

[Authorize(Roles = "Patient")]
public class PatientController : Controller
{
    private readonly ApplicationDbContext _context;

    public PatientController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Dashboard()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient is null) return RedirectToAction("Logout", "Account");

        var appointments = await _context.Appointments
            .Where(a => a.PatientId == patient.Id)
            .Include(a => a.Doctor)
            .OrderByDescending(a => a.AppointmentDate)
            .Take(5)
            .ToListAsync();

        var totalAppointments = await _context.Appointments.CountAsync(a => a.PatientId == patient.Id);
        var totalPrescriptions = await _context.Prescriptions.CountAsync(p => p.PatientId == patient.Id);
        var totalVisits = await _context.ExaminationRecords.CountAsync(e => e.PatientId == patient.Id);

        ViewBag.Patient = patient;
        ViewBag.TotalAppointments = totalAppointments;
        ViewBag.TotalPrescriptions = totalPrescriptions;
        ViewBag.TotalVisits = totalVisits;

        return View(appointments);
    }

    [HttpGet]
    public async Task<IActionResult> BookAppointment()
    {
        await PopulateDoctorsAsync();
        return View(new BookingViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BookAppointment(BookingViewModel model)
    {
        // Guard against a stale or hand-crafted doctor id before saving.
        if (model.DoctorId is not null &&
            !await _context.Doctors.AnyAsync(d => d.Id == model.DoctorId))
        {
            ModelState.AddModelError(nameof(model.DoctorId), "The selected doctor is no longer available.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDoctorsAsync();
            return View(model);
        }

        var patient = await GetCurrentPatientAsync();
        if (patient is null) return RedirectToAction("Logout", "Account");

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = model.DoctorId,
            AppointmentDate = model.AppointmentDate,
            TimeSlot = model.TimeSlot,
            Notes = model.Notes,
            Status = AppointmentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Appointment booked successfully.";
        return RedirectToAction(nameof(Appointments));
    }

    public async Task<IActionResult> Appointments()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient is null) return RedirectToAction("Logout", "Account");

        var appointments = await _context.Appointments
            .Where(a => a.PatientId == patient.Id)
            .Include(a => a.Doctor)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();

        return View(appointments);
    }

    public async Task<IActionResult> ExaminationResults()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient is null) return RedirectToAction("Logout", "Account");

        var records = await _context.ExaminationRecords
            .Where(e => e.PatientId == patient.Id)
            .Include(e => e.Doctor)
            .OrderByDescending(e => e.ExamDate)
            .ToListAsync();

        return View(records);
    }

    public async Task<IActionResult> Prescriptions()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient is null) return RedirectToAction("Logout", "Account");

        var prescriptions = await _context.Prescriptions
            .Where(p => p.PatientId == patient.Id)
            .Include(p => p.Doctor)
            .Include(p => p.ExaminationRecord)
            .OrderByDescending(p => p.PrescriptionDate)
            .ToListAsync();

        return View(prescriptions);
    }

    public async Task<IActionResult> PrintPrescription(int id)
    {
        var patient = await GetCurrentPatientAsync();
        if (patient is null) return RedirectToAction("Logout", "Account");

        var prescription = await _context.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.ExaminationRecord)
            .FirstOrDefaultAsync(p => p.Id == id && p.PatientId == patient.Id);

        if (prescription is null)
        {
            return NotFound();
        }

        return View("~/Views/Shared/PrintPrescription.cshtml", prescription);
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient is null) return RedirectToAction("Logout", "Account");

        return View(patient);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(Patient patient)
    {
        if (!ModelState.IsValid)
            return View(patient);

        var currentPatient = await GetCurrentPatientAsync();

        if (currentPatient is null)
            return RedirectToAction("Logout", "Account");

        currentPatient.FullName = patient.FullName;
        currentPatient.Email = patient.Email;
        currentPatient.Phone = patient.Phone;
        currentPatient.DateOfBirth = patient.DateOfBirth;
        currentPatient.Gender = patient.Gender;
        currentPatient.Address = patient.Address;
        currentPatient.EmergencyContact = patient.EmergencyContact;

        _context.Patients.Update(currentPatient);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Profile updated successfully.";

        return RedirectToAction(nameof(Profile));
    }

    private async Task PopulateDoctorsAsync()
    {
        ViewBag.Doctors = new SelectList(
            await _context.Doctors.OrderBy(d => d.FullName).ToListAsync(),
            "Id",
            "FullName");
    }

    private async Task<Patient?> GetCurrentPatientAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim))
            return null;

        // A malformed claim should log the user out, not throw a 500.
        if (!int.TryParse(userIdClaim, out var userId))
            return null;

        return await _context.Patients
            .FirstOrDefaultAsync(p => p.UserAccountId == userId);
    }
}