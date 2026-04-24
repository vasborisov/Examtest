using System.ComponentModel.DataAnnotations;

namespace proekt_za_6ca.Data.Entities
{
    public class Restaurants
    {
        public Guid Id { get; set; }
        [MaxLength(64)]
        public string Title { get; set; } = string.Empty;
        [MaxLength(255)]
        public string Description { get; set; } = string.Empty ;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        public List<Reservation>? Reservations { get; set; } = new List<Reservation>();
        public string OwnerId { get; set; } = string.Empty;
        public User Owner { get; set; }  

    }
}
