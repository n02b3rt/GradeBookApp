using GradeBookApp.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GradeBookApp.Components;
using GradeBookApp.Components.Account;
using GradeBookApp.Data;
using GradeBookApp.Data.Entities;
using GradeBookApp.Data.Seed;
using GradeBookApp.Services;
using Microsoft.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

// === Services ===
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ** Jednorazowa konfiguracja Identity przed builder.Build() **
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;       // Jeśli chcesz wyłączyć potwierdzenie maila
    options.Lockout.AllowedForNewUsers = false;            // Wyłącz blokadę konta
    options.Tokens.AuthenticatorTokenProvider = null;      // Wyłącz 2FA
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireDigit = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddScoped<ClassService>();
builder.Services.AddScoped<SubjectService>();
builder.Services.AddScoped<TeacherSubjectService>();
builder.Services.AddScoped<StudentClassService>();
builder.Services.AddScoped<UserService>();

// Dodaj usługi do kontenera
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();


// Dodaj HttpClient (ważne!)
builder.Services.AddHttpClient();

builder.Services.AddScoped(sp =>
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    return new HttpClient
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };
});

var app = builder.Build();

// === Middleware pipeline ===
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints(); // jeśli masz endpointy dla konta
app.MapControllers(); 
// === Seeder wywoływany przy starcie ===
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    // Jeśli chcesz to robić tylko w Development:
    if (app.Environment.IsDevelopment())
    {
        // 1. Usuń obecną bazę
        await dbContext.Database.EnsureDeletedAsync();

        // 2. Utwórz ją od nowa na podstawie migracji
        await dbContext.Database.MigrateAsync();

        // 3. Uruchom seeder, żeby zasypać świeżo stworzonymi danymi
        await DbSeeder.SeedAsync(dbContext, userManager, roleManager);
    }
    else
    {
        // W środowisku produkcyjnym lub testowym
        // możesz po prostu wgrać brakujące migracje i _nie_ usuwać wszystkiego:
        await dbContext.Database.MigrateAsync();
        await DbSeeder.SeedAsync(dbContext, userManager, roleManager);
    }
}

app.Run();
