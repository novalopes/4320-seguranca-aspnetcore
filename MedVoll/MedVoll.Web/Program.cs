using MedVoll.Web.Data;
using MedVoll.Web.Filters;
using MedVoll.Web.Interfaces;
using MedVoll.Web.Repositories;
using MedVoll.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ExceptionHandlerFilter>();

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ExceptionHandlerFilter>();
});

var connectionString = builder.Configuration.GetConnectionString("SqliteConnection");
builder.Services.AddDbContext<ApplicationDbContext>(x => x.UseSqlite(connectionString));

builder.Services
    .AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;            // Exigir e-mails confirmados para login
    options.SignIn.RequireConfirmedPhoneNumber = false;     // N�o exigir confirma��o de n�mero de telefone
});

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2);
    options.Lockout.MaxFailedAccessAttempts = 2;
});

builder.Services.AddTransient<IMedicoRepository, MedicoRepository>();
builder.Services.AddTransient<IConsultaRepository, ConsultaRepository>();
builder.Services.AddTransient<IMedicoService, MedicoService>();
builder.Services.AddTransient<IConsultaService, ConsultaService>();

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "VollMed.AntiForgery";
    options.Cookie.HttpOnly = true;
    options.HeaderName = "X-CSRF-TOKEN";
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/erro/500");
    app.UseStatusCodePagesWithReExecute("/erro/{0}");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages().WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    
    try
    {
        await IdentitySeeder.SeedUsersAsync(services);
    }
    catch (Exception ex)
    {
        // Log the error (uncomment ex variable name and write a log.)
        throw new Exception("An error occurred seeding the DB.", ex);
        //Console.WriteLine($"Erro ao executar o Seeder: {ex.Message}");
    }
}

app.Run();
