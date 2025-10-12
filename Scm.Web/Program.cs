using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Scm.Application.Services;
using Scm.Application.Validators;
using Scm.Infrastructure.Identity;
using Scm.Infrastructure.Persistence;
using Scm.Web.Localization;
using Scm.Web.HealthChecks;
using System.Globalization;
using Microsoft.Extensions.Options;

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

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IReportBuilderService, ReportBuilderService>();
builder.Services.AddSingleton<IValidateOptions<MailOptions>, MailOptionsValidator>();
builder.Services.AddOptions<MailOptions>()
    .Bind(builder.Configuration.GetSection("Mail"))
    .ValidateOnStart();
builder.Services.AddScoped<IMailService, MailService>();

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
var supportedCultures = new[] { defaultCulture, new CultureInfo("en-US") };
CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(defaultCulture);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.ApplyCurrentCultureToResponseHeaders = true;

    var queryProvider = new QueryStringRequestCultureProvider();
    var cookieProvider = new CookieRequestCultureProvider();
    var acceptLanguageProvider = new AcceptLanguageHeaderRequestCultureProvider();

    options.RequestCultureProviders = new IRequestCultureProvider[]
    {
        queryProvider,
        cookieProvider,
        acceptLanguageProvider
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
