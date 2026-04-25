using System.ComponentModel.DataAnnotations;
using proekt_za_6ca.Data.Enums;

namespace proekt_za_6ca.Data.Entities
{
    public class Reservation
    {
        public Guid Id { get; set; }
        
        [Required(ErrorMessage = "Reservation date and time is required")]
        [Display(Name = "Reservation Date & Time")]
        [DataType(DataType.DateTime)]
        public DateTime ReservationTime { get; set; }
        
        [Required]
        [Range(1, 20, ErrorMessage = "Number of people must be between 1 and 20")]
        [Display(Name = "Number of People")]
        public int PeopleCount { get; set; }    
        
        [Required]
        public Guid RestaurantId { get; set; }
        public Restaurants? Restaurants { get; set; }
        
        [StringLength(500, ErrorMessage = "Comment cannot exceed 500 characters")]
        [Display(Name = "Comment")]
        public string Comment { get; set; } = string.Empty;
        
        [Required]
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
        
        [Required]
        public string OwnerId { get; set; } = string.Empty;
        public User? Owner { get; set; }
        
        [Display(Name = "Created On")]
        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}