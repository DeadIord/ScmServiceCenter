namespace Scm.Domain.Entities;

public enum AccountType
{
    Company = 0,
    Person = 1
}

public sealed class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; }
        = AccountType.Company;

    public string? Inn { get; set; }
        = null;

    public string? Address { get; set; }
        = null;

    public string? Tags { get; set; }
        = null;

    public string? ManagerUserId { get; set; }
        = null;

    public ICollection<Contact> Contacts { get; set; }
        = new List<Contact>();
}
