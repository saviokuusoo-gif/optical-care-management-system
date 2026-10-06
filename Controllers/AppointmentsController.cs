using Microsoft.AspNetCore.Mvc;
using optical_care_management_system.Models;
using optical_care_management_system;

public class AppointmentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AppointmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Appointment appointment)
    {
        if (!ModelState.IsValid)
        {
            return View(appointment);
        }

        // Leave the doctor unassigned rather than pointing at a hardcoded id that
        // may not exist - an admin assigns the doctor when confirming the booking.
        if (appointment.DoctorId is not null &&
            !_context.Doctors.Any(d => d.Id == appointment.DoctorId))
        {
            appointment.DoctorId = null;
        }

        appointment.AppointmentDate = DateTime.SpecifyKind(appointment.AppointmentDate, DateTimeKind.Utc);
        appointment.Status = AppointmentStatus.Pending;
        appointment.CreatedAt = DateTime.UtcNow;

        _context.Appointments.Add(appointment);
        _context.SaveChanges();

        TempData["Success"] = "Appointment request submitted.";

        return RedirectToAction("Create");
    }
}