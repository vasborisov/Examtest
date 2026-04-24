using proekt_za_6ca.Data.Enums;

namespace proekt_za_6ca.Data.Entities
{
    public class Reservation
    {
        public Guid Id { get; set; }
        public DateTime ReservationTime { get; set; }
        public int PeopleCount { get; set; }    
        public Guid RestaurantId { get; set; }
        public Restaurants? Restaurants { get; set; }
        public string Commend {  get; set; }= string.Empty;
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
        public string OwnerId { get; set; } = string.Empty;
        public User Owner { get; set; }
    }
}