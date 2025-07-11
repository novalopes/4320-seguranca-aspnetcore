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
    options.SignIn.RequireConfirmedPhoneNumber = false;     // Não exigir confirmação de número de telefone
});

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;                   // Exigir pelo menos um número
    options.Password.RequireLowercase = true;               // Exigir pelo menos uma letra minúscula
    options.Password.RequireUppercase = true;               // Exigir pelo menos uma letra maiúscula
    options.Password.RequireNonAlphanumeric = true;         // Exigir caracteres especiais
    options.Password.RequiredLength = 8;                    // Tamanho mínimo da senha
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login"; // Redireciona para login se não autenticado
    options.LogoutPath = "/Identity/Account/Logout"; // Caminho para logout
    options.AccessDeniedPath = "/Identity/Account/AccessDenied"; // Caminho para acesso negado
    options.ExpireTimeSpan = TimeSpan.FromMinutes(2); // Tempo de expiração
    options.SlidingExpiration = true; // Renova o cookie automaticamente

    options.Cookie.HttpOnly = true; // Impede acesso via JavaScript
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Exige HTTPS
    options.Cookie.SameSite = SameSiteMode.Strict; // Restringe envio de cookies entre sites
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
