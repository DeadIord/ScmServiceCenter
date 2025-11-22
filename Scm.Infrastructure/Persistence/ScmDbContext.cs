using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Scm.Domain.Entities;
using Scm.Infrastructure.Configurations;
using Scm.Domain.Identity;

namespace Scm.Infrastructure.Persistence;

public class ScmDbContext(DbContextOptions<ScmDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<QuoteLine> QuoteLines => Set<QuoteLine>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();
    public DbSet<ReportExecutionLog> ReportExecutionLogs => Set<ReportExecutionLog>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<TechnicianTask> TechnicianTasks => Set<TechnicianTask>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new OrderConfiguration());
        builder.ApplyConfiguration(new QuoteLineConfiguration());
        builder.ApplyConfiguration(new PartConfiguration());
        builder.ApplyConfiguration(new MessageConfiguration());
        builder.ApplyConfiguration(new InvoiceConfiguration());
        builder.ApplyConfiguration(new AccountConfiguration());
        builder.ApplyConfiguration(new ContactConfiguration());
        builder.ApplyConfiguration(new ReportDefinitionConfiguration());
        builder.ApplyConfiguration(new ReportExecutionLogConfiguration());
        builder.ApplyConfiguration(new TicketConfiguration());
        builder.ApplyConfiguration(new TicketMessageConfiguration());
        builder.ApplyConfiguration(new TicketAttachmentConfiguration());
        builder.ApplyConfiguration(new TechnicianTaskConfiguration());
    }
}
