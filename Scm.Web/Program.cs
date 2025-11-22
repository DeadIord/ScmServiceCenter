using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scm.Application.Services;
using Scm.Application.Validators;
using Scm.Domain.Identity;
using Scm.Infrastructure.Persistence;
using Scm.Web.Authorization;
using Scm.Web.Localization;
using Scm.Web.HealthChecks;
using Scm.Web.Security;
using Scm.Web.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("Не указана строка подключения Default");

builder.Services.AddDbContext<ScmDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.MigrationsAssembly("Scm.Infrastructure")));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ScmDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Home/AccessDenied";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyNames.AdministrationAccess, policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy(PolicyNames.OrdersAccess, policy =>
        policy.RequireRole("Admin", "Manager", "Technician"));
    options.AddPolicy(PolicyNames.StockAccess, policy =>
        policy.RequireRole("Admin", "Manager", "Storekeeper"));
    options.AddPolicy(PolicyNames.ReportsAccess, policy =>
        policy.RequireRole("Admin", "Manager", "Accountant"));
    options.AddPolicy(PolicyNames.CrmAccess, policy =>
        policy.RequireRole("Admin", "Manager", "Technician", "Support"));
    options.AddPolicy(PolicyNames.MessagesAccess, policy =>
        policy.RequireRole("Admin", "Manager", "Technician", "Support"));
});

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IReportBuilderService, ReportBuilderService>();
builder.Services.AddSingleton<IMoneyConverter, MoneyConverter>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<ITechnicianTaskService, TechnicianTaskService>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.StockAccess, policy =>
        policy.RequireRole(AuthorizationPolicies.s_stockAccessRoles));
    options.AddPolicy(AuthorizationPolicies.ReportsAccess, policy =>
        policy.RequireRole(AuthorizationPolicies.s_reportsAccessRoles));
});
builder.Services.AddSingleton<IValidateOptions<MailOptions>, MailOptionsValidator>();
builder.Services.AddOptions<MailOptions>()
    .Bind(builder.Configuration.GetSection("Mail"))
    .ValidateOnStart();
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddSingleton<IValidateOptions<TicketInboxOptions>, TicketInboxOptionsValidator>();
builder.Services.AddOptions<TicketInboxOptions>()
    .Bind(builder.Configuration.GetSection("TicketInbox"))
    .ValidateOnStart();
builder.Services.AddSingleton<ITicketInboxPoller, TicketInboxPoller>();
builder.Services.AddHostedService<TicketInboxHostedService>();
builder.Services.AddOptions<ReportBuilderOptions>()
    .Bind(builder.Configuration.GetSection("ReportBuilder"))
    .ValidateOnStart();
builder.Services.AddOptions<CurrencyOptions>()
    .Bind(builder.Configuration.GetSection("Currency"));

builder.Services.AddHealthChecks()
    .AddCheck<MailConfigurationHealthCheck>("mail_delivery");

builder.Services.AddScoped<ReceivePartDtoValidator>();

builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) => factory.Create(typeof(SharedResource));
    });
builder.Services.AddRazorPages()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) => factory.Create(typeof(SharedResource));
    });

var defaultCulture = new CultureInfo("ru-RU");
var supportedCultures = new[] { defaultCulture };
CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(defaultCulture);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.ApplyCurrentCultureToResponseHeaders = true;
    options.RequestCultureProviders = new IRequestCultureProvider[]
    {
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

var app = builder.Build();

await ScmDbSeeder.SeedAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;

app.UseRequestLocalization(localizationOptions);

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Orders}/{action=Index}/{id?}");

app.MapRazorPages();

app.MapHealthChecks("/health");

await app.RunAsync();
