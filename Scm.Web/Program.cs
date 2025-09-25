using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scm.Application.Services;
using Scm.Application.Validators;
using Scm.Infrastructure.Identity;
using Scm.Infrastructure.Persistence;

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

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IMessageService, MessageService>();

builder.Services.AddScoped<CreateOrderDtoValidator>();
builder.Services.AddScoped<AddQuoteLineDtoValidator>();
builder.Services.AddScoped<ReceivePartDtoValidator>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

await ScmDbSeeder.SeedAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

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

await app.RunAsync();
