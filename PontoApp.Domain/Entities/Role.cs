namespace PontoApp.Domain.Entities
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!; 
        public ICollection<UserRole> Users { get; set; } = new List<UserRole>();
    }
}
