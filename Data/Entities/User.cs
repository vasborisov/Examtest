using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace proekt_za_6ca.Data.Entities
{
    public class User : IdentityUser

    {
        [MaxLength(255)]
        public string FirstName { get; set; } = string.Empty;
        [MaxLength(255)]
        public string LastName { get; set; } = string.Empty ;
        public List<Reservation>? Reservations { get; set; } = new List<Reservation>();
        public List<Restaurants>? Restaurants { get; set; } = new List<Restaurants>();
    }
}
