namespace Denudey.Api.Domain.Entities
{
    public class Role
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = null!;

        // Optional: navigation property to UserRoles
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}