namespace Scm.Domain.Entities;

public sealed class Contact
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AccountId { get; set; }
        = Guid.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Position { get; set; }
        = null;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public Account? Account { get; set; }
        = null;

    public ICollection<Order> Orders { get; set; }
        = new List<Order>();
}
