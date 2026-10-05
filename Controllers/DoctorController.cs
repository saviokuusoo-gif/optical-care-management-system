using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using optical_care_management_system.Models;

namespace optical_care_management_system.Controllers;

[Authorize(Roles = "Doctor")]
public class DoctorController : Controller
{
    private readonly ApplicationDbContext _context;

    public DoctorController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Dashboard()
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor is null)
        {
            return RedirectToAction("Logout", "Account");
        }

        var today = DateTime.UtcNow.Date;
        var todayAppointments = await _context.Appointments
            .Include(a => a.Patient)
            .Where(a => a.DoctorId == doctor.Id && a.AppointmentDate.Date == today)
            .OrderBy(a => a.TimeSlot)
            .ToListAsync();

        var recentExaminations = await _context.ExaminationRecords
            .Include(e => e.Patient)
            .Where(e => e.DoctorId == doctor.Id)
            .OrderByDescending(e => e.ExamDate)
            .Take(5)
            .ToListAsync();

        var recentPrescriptions = await _context.Prescriptions
            .Include(p => p.Patient)
            .Where(p => p.DoctorId == doctor.Id)
            .OrderByDescending(p => p.PrescriptionDate)
            .Take(5)
            .ToListAsync();

        var treatedPatientIds = await _context.ExaminationRecords
            .Where(e => e.DoctorId == doctor.Id)
            .Select(e => e.PatientId)
            .Union(_context.Prescriptions.Where(p => p.DoctorId == doctor.Id).Select(p => p.PatientId))
            .Distinct()
            .CountAsync();

        var viewModel = new DoctorDashboardViewModel
        {
            Doctor = doctor,
            TodayAppointmentsCount = todayAppointments.Count,
            TotalExaminationsCount = await _context.ExaminationRecords.CountAsync(e => e.DoctorId == doctor.Id),
            TotalPrescriptionsCount = await _context.Prescriptions.CountAsync(p => p.DoctorId == doctor.Id),
            TotalPatientsTreatedCount = treatedPatientIds,
            TodayAppointments = todayAppointments,
            RecentExaminations = recentExaminations,
            RecentPrescriptions = recentPrescriptions
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Appointments(string? status)
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor is null) return RedirectToAction("Logout", "Account");

        var query = _context.Appointments
            .Include(a => a.Patient)
            .Where(a => a.DoctorId == doctor.Id);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AppointmentStatus>(status, true, out var statusEnum))
        {
            query = query.Where(a => a.Status == statusEnum);
        }

        ViewBag.SelectedStatus = status;

        var appointments = await query
            .OrderByDescending(a => a.AppointmentDate)
            .ThenBy(a => a.TimeSlot)
            .ToListAsync();

        return View(appointments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAppointmentStatus(int id, AppointmentStatus status)
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor is null) return RedirectToAction("Logout", "Account");

        var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id && a.DoctorId == doctor.Id);
        if (appointment is null)
        {
            return NotFound();
        }

        appointment.Status = status;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Appointment status updated to {status}.";
        return RedirectToAction(nameof(Appointments));
    }

    public async Task<IActionResult> ExaminationRecords()
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor is null) return RedirectToAction("Logout", "Account");

        var records = await _context.ExaminationRecords
            .Include(e => e.Patient)
            .Include(e => e.Prescriptions)
            .Where(e => e.DoctorId == doctor.Id)
            .OrderByDescending(e => e.ExamDate)
            .ToListAsync();

        return View(records);
    }

    [HttpGet]
    public async Task<IActionResult> ExaminationRecordForm(int? id, int? patientId, int? appointmentId)
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor is null) return RedirectToAction("Logout", "Account");

        var record = id is null
            ? new ExaminationRecord
            {
                DoctorId = doctor.Id,
                ExamDate = DateTime.UtcNow,
                PatientId = patientId ?? 0
            }
            : await _context.ExaminationRecords.FirstOrDefaultAsync(e => e.Id == id.Value && e.DoctorId == doctor.Id);

        if (record is null)
        {
            return NotFound();
        }

        ViewBag.Patients = new SelectList(await _context.Patients.OrderBy(p => p.FullName).ToListAsync(), "Id", "FullName", record.PatientId);
        ViewBag.AppointmentId = appointmentId;

        return View(record);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExaminationRecordForm(int? id, ExaminationRecord record, int? appointmentId)
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor is null) return RedirectToAction("Logout", "Account");

        record.DoctorId = doctor.Id;

        if (!ModelState.IsValid)
        {
            ViewBag.Patients = new SelectList(await _context.Patients.OrderBy(p => p.FullName).ToListAsync(), "Id", "FullName", record.PatientId);
            ViewBag.AppointmentId = appointmentId;
            return View(record);
        }

        var isNew = (id == null || id == 0) && record.Id == 0;
        if (isNew)
        {
            _context.ExaminationRecords.Add(record);
            await _context.SaveChangesAsync();

            // If this examination was triggered from an appointment, mark it Approved/Completed
            if (appointmentId.HasValue)
            {
                var appt = await _context.Appointments.FindAsync(appointmentId.Value);
                if (appt != null && appt.DoctorId == doctor.Id)
                {
                    appt.Status = AppointmentStatus.Approved;
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Success"] = "Eye examination record created successfully. You can now write a prescription if needed.";
        }
        else
        {
            var targetId = (id != null && id > 0) ? id.Value : record.Id;
            var existing = await _context.ExaminationRecords.FirstOrDefaultAsync(e => e.Id == targetId && e.DoctorId == doctor.Id);
            if (existing is null)
            {
                existing = await _context.ExaminationRecords.FindAsync(targetId);
                if (existing is null) return NotFound();
            }

            existing.PatientId = record.PatientId;
            existing.ExamDate = record.ExamDate;
            existing.LeftEye = record.LeftEye;
            existing.RightEye = record.RightEye;
            existing.VisualAcuity = record.VisualAcuity;
            existing.EyePressure = record.EyePressure;
            existing.Notes = record.Notes;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Examination record updated successfully.";
        }

        return RedirectToAction(nameof(ExaminationRecords));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteExaminationRecord(int id)
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor is null) return RedirectToAction("Logout", "Account");

        var record = await _context.ExaminationRecords.FirstOrDefaultAsync(e => e.Id == id && e.DoctorId == doctor.Id);
        if (record is null)
        {
            return NotFound();
        }

        var linked = await _context.Prescriptions.CountAsync(p => p.ExaminationRecordId == id);
        if (linked > 0)
        {
            TempData["Error"] = $"Cannot delete this examination: {linked} prescription(s) are linked to it.";
            return RedirectToAction(nameof(ExaminationRecords));
        }

        _context.ExaminationRecords.Remove(record);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Examination record removed.";
        return RedirectToAction(nameof(ExaminationRecords));
    }

    public async Task<IActionResult> Prescriptions()
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor is null) return RedirectToAction("Logout", "Account");

        var prescriptions = await _context.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.ExaminationRecord)
            .Where(p => p.DoctorId == doctor.Id)
            .OrderByDescending(p => p.PrescriptionDate)
            .ToListAsync();

        return View(prescriptions);
    }

    [HttpGet]
    public async Task<IActionResult> PrescriptionForm(int? id, int? patientId, int? examId)
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor is null) return RedirectToAction("Logout", "Account");

        var prescription = id is null
            ? new Prescription
            {
                DoctorId = doctor.Id,
                PrescriptionDate = DateTime.UtcNow,
                PatientId = patientId ?? 0,
                ExaminationRecordId = examId ?? 0,
                IsActive = true
            }
            : await _context.Prescriptions.FirstOrDefaultAsync(p => p.Id == id.Value && p.DoctorId == doctor.Id);

        if (prescription is null)
        {
            return NotFound();
        }

        // If examId is provided, prefill LeftEye & RightEye notes if available
        if (id is null && examId.HasValue)
        {
            var exam = await _context.ExaminationRecords.Include(e => e.Patient).FirstOrDefaultAsync(e => e.Id == examId.Value);
            if (exam != null)
            {
                prescription.LeftEye = string.IsNullOrWhiteSpace(prescription.LeftEye) ? exam.LeftEye : prescription.LeftEye;
                prescription.RightEye = string.IsNullOrWhiteSpace(prescription.RightEye) ? exam.RightEye : prescription.RightEye;
                prescription.PatientId = exam.PatientId;
                prescription.ExaminationRecordId = exam.Id;
                ViewBag.SelectedExam = exam;
            }
        }
        else if (prescription.ExaminationRecordId > 0)
        {
            ViewBag.SelectedExam = await _context.ExaminationRecords
                .Include(e => e.Patient)
                .FirstOrDefaultAsync(e => e.Id == prescription.ExaminationRecordId);
        }

        await PopulatePrescriptionDropdownsAsync(doctor.Id, prescription.PatientId, prescription.ExaminationRecordId);
        return View(prescription);
    }

    public async Task<IActionResult> PrintPrescription(int id)
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor is null) return RedirectToAction("Logout", "Account");

        var prescription = await _context.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.ExaminationRecord)
            .FirstOrDefaultAsync(p => p.Id == id && p.DoctorId == doctor.Id);

        if (prescription is null)
        {
            return NotFound();
        }

        return View("~/Views/Shared/PrintPrescription.cshtml", prescription);
    }

    public async Task<IActionResult> PrintExamination(int id)
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor is null) return RedirectToAction("Logout", "Account");

        var exam = await _context.ExaminationRecords
            .Include(e => e.Patient)
            .Include(e => e.Doctor)
            .Include(e => e.Prescriptions)
            .FirstOrDefaultAsync(e => e.Id == id && e.DoctorId == doctor.Id);

        if (exam is null)
        {
            return NotFound();
        }

        return View("~/Views/Shared/PrintExamination.cshtml", exam);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PrescriptionForm(int? id, Prescription prescription)
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor is null) return RedirectToAction("Logout", "Account");

        prescription.DoctorId = doctor.Id;

        if (!ModelState.IsValid)
        {
            await PopulatePrescriptionDropdownsAsync(doctor.Id, prescription.PatientId, prescription.ExaminationRecordId);
            return View(prescription);
        }

        var isNew = (id == null || id == 0) && prescription.Id == 0;
        if (isNew)
        {
            _context.Prescriptions.Add(prescription);
            TempData["Success"] = "Prescription written and saved successfully. Admin can now view and print it.";
        }
        else
        {
            var targetId = (id != null && id > 0) ? id.Value : prescription.Id;
            var existing = await _context.Prescriptions.FirstOrDefaultAsync(p => p.Id == targetId && p.DoctorId == doctor.Id);
            if (existing is null)
            {
                existing = await _context.Prescriptions.FindAsync(targetId);
                if (existing is null) return NotFound();
            }

            existing.PatientId = prescription.PatientId;
            existing.ExaminationRecordId = prescription.ExaminationRecordId;
            existing.PrescriptionDate = prescription.PrescriptionDate;
            existing.LensType = prescription.LensType;
            existing.LeftEye = prescription.LeftEye;
            existing.RightEye = prescription.RightEye;
            existing.Notes = prescription.Notes;
            existing.IsActive = prescription.IsActive;

            TempData["Success"] = "Prescription updated successfully.";
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Prescriptions));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePrescription(int id)
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor is null) return RedirectToAction("Logout", "Account");

        var prescription = await _context.Prescriptions.FirstOrDefaultAsync(p => p.Id == id && p.DoctorId == doctor.Id);
        if (prescription is null)
        {
            return NotFound();
        }

        _context.Prescriptions.Remove(prescription);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Prescription deleted.";
        return RedirectToAction(nameof(Prescriptions));
    }

    public async Task<IActionResult> Patients()
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor is null) return RedirectToAction("Logout", "Account");

        // Patients assigned or examined by this doctor
        var patientIds = await _context.Appointments
            .Where(a => a.DoctorId == doctor.Id)
            .Select(a => a.PatientId)
            .Union(_context.ExaminationRecords.Where(e => e.DoctorId == doctor.Id).Select(e => e.PatientId))
            .Union(_context.Prescriptions.Where(p => p.DoctorId == doctor.Id).Select(p => p.PatientId))
            .ToListAsync();

        var patients = await _context.Patients
            .Where(p => patientIds.Contains(p.Id))
            .OrderBy(p => p.FullName)
            .ToListAsync();

        return View(patients);
    }

    public async Task<IActionResult> PatientDetails(int id)
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor is null) return RedirectToAction("Logout", "Account");

        var patient = await _context.Patients
            .Include(p => p.Appointments.Where(a => a.DoctorId == doctor.Id))
            .Include(p => p.ExaminationRecords.Where(e => e.DoctorId == doctor.Id))
            .Include(p => p.Prescriptions.Where(p => p.DoctorId == doctor.Id))
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient is null)
        {
            return NotFound();
        }

        return View(patient);
    }

    private async Task PopulatePrescriptionDropdownsAsync(int doctorId, int selectedPatientId, int selectedExamId)
    {
        ViewBag.Patients = new SelectList(
            await _context.Patients.OrderBy(p => p.FullName).ToListAsync(),
            "Id",
            "FullName",
            selectedPatientId);

        var exams = await _context.ExaminationRecords
            .Include(e => e.Patient)
            .Where(e => e.DoctorId == doctorId)
            .OrderByDescending(e => e.ExamDate)
            .Select(e => new
            {
                e.Id,
                DisplayText = $"Exam #{e.Id} - {(e.Patient != null ? e.Patient.FullName : "Patient")} ({e.ExamDate:dd MMM yyyy})"
            })
            .ToListAsync();

        ViewBag.ExaminationRecords = new SelectList(exams, "Id", "DisplayText", selectedExamId);
    }

    private async Task<Doctor?> GetCurrentDoctorAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return await _context.Doctors.FirstOrDefaultAsync(d => d.UserAccountId == userId);
    }
}
