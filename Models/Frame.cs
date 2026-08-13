using System.ComponentModel.DataAnnotations;

namespace optical_care_management_system.Models
{
    public class Frame
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Brand { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string Gender { get; set; } = string.Empty;

        public string Material { get; set; } = string.Empty;

        public string Shape { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; } = "default-frame.jpg";

        public bool IsAvailable { get; set; } = true;
    }
}