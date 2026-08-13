using optical_care_management_system.Models;
using optical_care_management_system.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace optical_care_management_system.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Dashboard()
    {
        var viewModel = new DashboardViewModel
        {
            TotalPatients = await _context.Patients.CountAsync(),
            TodayAppointments = await _context.Appointments.CountAsync(a => a.AppointmentDate.Date == DateTime.Today),
            ActiveDoctors = await _context.Doctors.CountAsync(),
            LowInventoryItems = await _context.InventoryItems.CountAsync(i => i.StockQuantity <= i.LowStockThreshold)
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Patients()
    {
        var patients = await _context.Patients
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(patients);
    }

    [HttpGet]
    public async Task<IActionResult> PatientForm(int? id)
    {
        var patient = id is null 
            ? new Patient() 
            : await _context.Patients.FindAsync(id.Value);

        if (patient is null)
        {
            return NotFound();
        }

        return View(patient);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PatientForm(int? id, Patient patient)
    {
        if (!ModelState.IsValid)
        {
            return View(patient);
        }

        if (id is null)
        {
            patient.CreatedAt = DateTime.UtcNow;
            _context.Patients.Add(patient);
        }
        else
        {
            // Copy only the editable fields so the linked login account and the
            // original registration date survive an edit.
            var existing = await _context.Patients.FindAsync(id.Value);

            if (existing is null)
            {
                return NotFound();
            }

            existing.FullName = patient.FullName;
            existing.Email = patient.Email;
            existing.Phone = patient.Phone;
            existing.DateOfBirth = patient.DateOfBirth;
            existing.Gender = patient.Gender;
            existing.Address = patient.Address;
            existing.EmergencyContact = patient.EmergencyContact;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Patient record saved successfully.";

        return RedirectToAction(nameof(Patients));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePatient(int id)
    {
        var patient = await _context.Patients.FindAsync(id);

        if (patient is null)
        {
            return NotFound();
        }

        // Related rows use DeleteBehavior.Restrict, so block the delete with a
        // readable message instead of letting SQL raise an FK violation.
        var linked = await _context.Appointments.CountAsync(a => a.PatientId == id)
                     + await _context.ExaminationRecords.CountAsync(e => e.PatientId == id)
                     + await _context.Prescriptions.CountAsync(p => p.PatientId == id);

        if (linked > 0)
        {
            TempData["Error"] = $"Cannot delete {patient.FullName}: {linked} linked record(s) exist. Remove them first.";
            return RedirectToAction(nameof(Patients));
        }

        _context.Patients.Remove(patient);

        await _context.SaveChangesAsync();

        TempData["Success"] = "Patient deleted.";

        return RedirectToAction(nameof(Patients));
    }


    // NEW: Patient Details
    public async Task<IActionResult> PatientDetails(int id)
    {
        var patient = await _context.Patients
            .Include(p => p.Appointments)
            .Include(p => p.ExaminationRecords)
            .Include(p => p.Prescriptions)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient == null)
        {
            return NotFound();
        }

        return View(patient);
    }

    public async Task<IActionResult> Doctors()
    {
        var doctors = await _context.Doctors.OrderByDescending(d => d.CreatedAt).ToListAsync();
        return View(doctors);
    }

    [HttpGet]
    public async Task<IActionResult> DoctorForm(int? id)
    {
        var doctor = id is null ? new Doctor() : await _context.Doctors.FindAsync(id.Value);
        if (doctor is null)
        {
            return NotFound();
        }

        return View(doctor);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoctorForm(int? id, Doctor doctor)
    {
        if (!ModelState.IsValid)
        {
            return View(doctor);
        }

        if (id is null)
        {
            doctor.CreatedAt = DateTime.UtcNow;
            _context.Doctors.Add(doctor);
        }
        else
        {
            // Preserve the linked login account and original join date on edit.
            var existing = await _context.Doctors.FindAsync(id.Value);

            if (existing is null)
            {
                return NotFound();
            }

            existing.FullName = doctor.FullName;
            existing.Email = doctor.Email;
            existing.Phone = doctor.Phone;
            existing.Specialty = doctor.Specialty;
            existing.LicenseNumber = doctor.LicenseNumber;
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Doctor profile saved successfully.";
        return RedirectToAction(nameof(Doctors));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDoctor(int id)
    {
        var doctor = await _context.Doctors.FindAsync(id);
        if (doctor is null)
        {
            return NotFound();
        }

        // Related rows use DeleteBehavior.Restrict, so block the delete with a
        // readable message instead of letting SQL raise an FK violation.
        var linked = await _context.Appointments.CountAsync(a => a.DoctorId == id)
                     + await _context.ExaminationRecords.CountAsync(e => e.DoctorId == id)
                     + await _context.Prescriptions.CountAsync(p => p.DoctorId == id);

        if (linked > 0)
        {
            TempData["Error"] = $"Cannot delete {doctor.FullName}: {linked} linked record(s) exist. Reassign or remove them first.";
            return RedirectToAction(nameof(Doctors));
        }

        _context.Doctors.Remove(doctor);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Doctor deleted.";
        return RedirectToAction(nameof(Doctors));
    }

    public async Task<IActionResult> Appointments()
    {
        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();

        return View(appointments);
    }

    [HttpGet]
    public async Task<IActionResult> AppointmentForm(int? id)
    {
        var appointment = id is null ? new Appointment() : await _context.Appointments.FindAsync(id.Value);
        if (appointment is null)
        {
            return NotFound();
        }

        ViewBag.Patients = new SelectList(await _context.Patients.ToListAsync(), "Id", "FullName");
        ViewBag.Doctors = new SelectList(await _context.Doctors.ToListAsync(), "Id", "FullName");
        return View(appointment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AppointmentForm(int? id, Appointment appointment)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Patients = new SelectList(await _context.Patients.ToListAsync(), "Id", "FullName");
            ViewBag.Doctors = new SelectList(await _context.Doctors.ToListAsync(), "Id", "FullName");
            return View(appointment);
        }

        if (id is null)
        {
            _context.Appointments.Add(appointment);
        }
        else
        {
            _context.Appointments.Update(appointment);
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Appointment saved.";
        return RedirectToAction(nameof(Appointments));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAppointmentStatus(int id, AppointmentStatus status)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment is null)
        {
            return NotFound();
        }

        appointment.Status = status;
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Appointment marked {status.ToString().ToLowerInvariant()}.";
        return RedirectToAction(nameof(Appointments));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment is null)
        {
            return NotFound();
        }

        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Appointment removed.";
        return RedirectToAction(nameof(Appointments));
    }

    public async Task<IActionResult> ExaminationRecords()
    {
        var records = await _context.ExaminationRecords
            .Include(e => e.Patient)
            .Include(e => e.Doctor)
            .OrderByDescending(e => e.ExamDate)
            .ToListAsync();

        return View(records);
    }

    [HttpGet]
    public async Task<IActionResult> ExaminationRecordForm(int? id)
    {
        var record = id is null ? new ExaminationRecord() : await _context.ExaminationRecords.FindAsync(id.Value);
        if (record is null)
        {
            return NotFound();
        }

        ViewBag.Patients = new SelectList(await _context.Patients.ToListAsync(), "Id", "FullName");
        ViewBag.Doctors = new SelectList(await _context.Doctors.ToListAsync(), "Id", "FullName");
        return View(record);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExaminationRecordForm(int? id, ExaminationRecord record)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Patients = new SelectList(await _context.Patients.ToListAsync(), "Id", "FullName");
            ViewBag.Doctors = new SelectList(await _context.Doctors.ToListAsync(), "Id", "FullName");
            return View(record);
        }

        if (id is null)
        {
            _context.ExaminationRecords.Add(record);
        }
        else
        {
            _context.ExaminationRecords.Update(record);
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Examination record saved.";
        return RedirectToAction(nameof(ExaminationRecords));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteExaminationRecord(int id)
    {
        var record = await _context.ExaminationRecords.FindAsync(id);
        if (record is null)
        {
            return NotFound();
        }

        // Prescriptions reference this record with DeleteBehavior.Restrict.
        var linked = await _context.Prescriptions.CountAsync(p => p.ExaminationRecordId == id);

        if (linked > 0)
        {
            TempData["Error"] = $"Cannot delete this record: {linked} prescription(s) reference it.";
            return RedirectToAction(nameof(ExaminationRecords));
        }

        _context.ExaminationRecords.Remove(record);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Examination record deleted.";
        return RedirectToAction(nameof(ExaminationRecords));
    }

    public async Task<IActionResult> Prescriptions()
    {
        var prescriptions = await _context.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.ExaminationRecord)
            .OrderByDescending(p => p.PrescriptionDate)
            .ToListAsync();

        return View(prescriptions);
    }

    [HttpGet]
    public async Task<IActionResult> PrescriptionForm(int? id)
    {
        var prescription = id is null ? new Prescription() : await _context.Prescriptions.FindAsync(id.Value);
        if (prescription is null)
        {
            return NotFound();
        }

        ViewBag.Patients = new SelectList(await _context.Patients.ToListAsync(), "Id", "FullName");
        ViewBag.Doctors = new SelectList(await _context.Doctors.ToListAsync(), "Id", "FullName");
        ViewBag.ExaminationRecords = new SelectList(await _context.ExaminationRecords.ToListAsync(), "Id", "Id");
        return View(prescription);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PrescriptionForm(int? id, Prescription prescription)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Patients = new SelectList(await _context.Patients.ToListAsync(), "Id", "FullName");
            ViewBag.Doctors = new SelectList(await _context.Doctors.ToListAsync(), "Id", "FullName");
            ViewBag.ExaminationRecords = new SelectList(await _context.ExaminationRecords.ToListAsync(), "Id", "Id");
            return View(prescription);
        }

        if (id is null)
        {
            _context.Prescriptions.Add(prescription);
        }
        else
        {
            _context.Prescriptions.Update(prescription);
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Prescription saved.";
        return RedirectToAction(nameof(Prescriptions));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePrescription(int id)
    {
        var prescription = await _context.Prescriptions.FindAsync(id);
        if (prescription is null)
        {
            return NotFound();
        }

        _context.Prescriptions.Remove(prescription);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Prescription deleted.";
        return RedirectToAction(nameof(Prescriptions));
    }

    public async Task<IActionResult> Inventory()
    {
        var inventory = await _context.InventoryItems.OrderByDescending(i => i.CreatedAt).ToListAsync();
        return View(inventory);
    }

    [HttpGet]
    public async Task<IActionResult> InventoryForm(int? id)
    {
        var item = id is null ? new InventoryItem() : await _context.InventoryItems.FindAsync(id.Value);
        if (item is null)
        {
            return NotFound();
        }

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InventoryForm(int? id, InventoryItem item)
    {
        if (!ModelState.IsValid)
        {
            return View(item);
        }

        if (id is null)
        {
            _context.InventoryItems.Add(item);
        }
        else
        {
            _context.InventoryItems.Update(item);
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Inventory item saved.";
        return RedirectToAction(nameof(Inventory));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteInventoryItem(int id)
    {
        var item = await _context.InventoryItems.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        _context.InventoryItems.Remove(item);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Inventory item removed.";
        return RedirectToAction(nameof(Inventory));
    }

    public async Task<IActionResult> Reports()
    {
        var viewModel = new DashboardViewModel
        {
            TotalPatients = await _context.Patients.CountAsync(),
            TodayAppointments = await _context.Appointments.CountAsync(a => a.AppointmentDate.Date == DateTime.Today),
            ActiveDoctors = await _context.Doctors.CountAsync(),
            LowInventoryItems = await _context.InventoryItems.CountAsync(i => i.StockQuantity <= i.LowStockThreshold)
        };

        var appointments = await _context.Appointments.Include(a => a.Patient).Include(a => a.Doctor).ToListAsync();
        var inventory = await _context.InventoryItems.OrderBy(i => i.StockQuantity).ToListAsync();

        ViewBag.Appointments = appointments;
        ViewBag.Inventory = inventory;
        return View(viewModel);
    }

    // ---------------------------------------------------------------------
    // Frame bookings
    // ---------------------------------------------------------------------

    public async Task<IActionResult> FrameBookings(string? status)
    {
        var query = _context.FrameBookings
            .Include(b => b.Patient)
            .Include(b => b.Frame)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(b => b.Status == status);
        }

        ViewBag.Status = status;
        ViewBag.PendingCount = await _context.FrameBookings.CountAsync(b => b.Status == "Pending");

        var bookings = await query
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

        return View(bookings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateFrameBookingStatus(int id, string status)
    {
        var allowed = new[] { "Pending", "Approved", "Collected", "Canceled" };

        if (!allowed.Contains(status))
        {
            TempData["Error"] = "Unknown booking status.";
            return RedirectToAction(nameof(FrameBookings));
        }

        var booking = await _context.FrameBookings
            .Include(b => b.Frame)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking is null)
        {
            return NotFound();
        }

        booking.Status = status;

        // Once a frame is handed over it leaves the catalogue as available stock.
        if (status == "Collected" && booking.Frame is not null)
        {
            booking.Frame.IsAvailable = false;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Booking marked {status.ToLowerInvariant()}.";

        return RedirectToAction(nameof(FrameBookings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFrameBooking(int id)
    {
        var booking = await _context.FrameBookings.FindAsync(id);

        if (booking is null)
        {
            return NotFound();
        }

        _context.FrameBookings.Remove(booking);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Frame booking removed.";

        return RedirectToAction(nameof(FrameBookings));
    }

    // ---------------------------------------------------------------------
    // Excel exports (Reports and Examination Records only)
    // ---------------------------------------------------------------------

    /// <summary>Exports the Examination Records list as a single-sheet workbook.</summary>
    public async Task<IActionResult> ExportExaminationRecords()
    {
        var records = await _context.ExaminationRecords
            .Include(e => e.Patient)
            .Include(e => e.Doctor)
            .OrderByDescending(e => e.ExamDate)
            .ToListAsync();

        var file = ExcelExportService.Build(
            "Examination Records",
            "Examination Records Report",
            new[] { "#", "Patient", "Doctor", "Exam Date", "Left Eye", "Right Eye", "Visual Acuity", "Eye Pressure", "Notes" },
            records.Select((e, index) => new object?[]
            {
                index + 1,
                e.Patient?.FullName ?? "-",
                e.Doctor?.FullName ?? "-",
                e.ExamDate.ToString("dd MMM yyyy"),
                e.LeftEye,
                e.RightEye,
                e.VisualAcuity,
                e.EyePressure,
                e.Notes
            }));

        return File(file, ExcelExportService.ContentType, ExcelExportService.FileName("ExaminationRecords"));
    }

    /// <summary>Exports the full system report as a workbook: summary, inventory and appointments.</summary>
    public async Task<IActionResult> ExportReports()
    {
        var totalPatients = await _context.Patients.CountAsync();
        var todayAppointments = await _context.Appointments.CountAsync(a => a.AppointmentDate.Date == DateTime.Today);
        var activeDoctors = await _context.Doctors.CountAsync();
        var lowInventoryItems = await _context.InventoryItems.CountAsync(i => i.StockQuantity <= i.LowStockThreshold);

        var inventory = await _context.InventoryItems
            .OrderBy(i => i.StockQuantity)
            .ToListAsync();

        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();

        var summarySheet = new ExcelSheet(
            "Summary",
            "System Report Summary",
            new[] { "Metric", "Value" },
            new List<object?[]>
            {
                new object?[] { "Total Patients", totalPatients },
                new object?[] { "Today's Appointments", todayAppointments },
                new object?[] { "Active Doctors", activeDoctors },
                new object?[] { "Low Stock Items", lowInventoryItems },
                new object?[] { "Total Appointments", appointments.Count },
                new object?[] { "Total Inventory Items", inventory.Count }
            });

        var inventorySheet = new ExcelSheet(
            "Inventory",
            "Inventory Status",
            new[] { "#", "Item Name", "Category", "Stock Quantity", "Unit Price", "Low Stock Threshold", "Stock Status" },
            inventory.Select((i, index) => new object?[]
            {
                index + 1,
                i.ItemName,
                i.Category,
                i.StockQuantity,
                i.UnitPrice,
                i.LowStockThreshold,
                i.StockQuantity <= i.LowStockThreshold ? "Low Stock" : "In Stock"
            }));

        var appointmentsSheet = new ExcelSheet(
            "Appointments",
            "Appointments Summary",
            new[] { "#", "Date", "Patient", "Doctor", "Time Slot", "Status" },
            appointments.Select((a, index) => new object?[]
            {
                index + 1,
                a.AppointmentDate.ToString("dd MMM yyyy"),
                a.Patient?.FullName ?? "-",
                a.Doctor?.FullName ?? "Not assigned",
                a.TimeSlot,
                a.Status.ToString()
            }));

        var file = ExcelExportService.Build(new[] { summarySheet, inventorySheet, appointmentsSheet });

        return File(file, ExcelExportService.ContentType, ExcelExportService.FileName("SystemReport"));
    }
}
