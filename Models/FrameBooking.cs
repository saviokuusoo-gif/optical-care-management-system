using System.ComponentModel.DataAnnotations;

namespace optical_care_management_system.Models
{
    public class FrameBooking
    {
        public int Id { get; set; }


        [Required]
        public int PatientId { get; set; }

        public Patient? Patient { get; set; }


        [Required]
        public int FrameId { get; set; }

        public Frame? Frame { get; set; }


        public DateTime BookingDate { get; set; } = DateTime.Now;


        /// <summary>Pending, Approved, Collected or Canceled.</summary>
        public string Status { get; set; } = "Pending";


        /// <summary>Optional note the patient adds when requesting the frame.</summary>
        public string Notes { get; set; } = string.Empty;
    }
}