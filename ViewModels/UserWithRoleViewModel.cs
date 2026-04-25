using proekt_za_6ca.Data.Entities;

namespace proekt_za_6ca.ViewModels
{
    public class UserWithRoleViewModel
    {
        public User User { get; set; } = null!;
        public IList<string> Roles { get; set; } = new List<string>();
    }
}