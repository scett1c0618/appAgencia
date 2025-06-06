using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using app1.Data;
using app1.Servicios;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<app1.Servicios.ClimaService>();
builder.Services.AddHttpClient<app1.Servicios.HotelService>();
builder.Services.AddHttpClient<AmadeusHotelService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton(sp =>
    new AmadeusHotelService(
        sp.GetRequiredService<HttpClient>(),
        "z6eKUqkG90QUuhGvNLf54mycASX5Pj6S", // <-- Reemplaza aquí
        "QAiAciYoFMNnu02Y" // <-- Reemplaza aquí
    )
);
builder.Services.AddHttpClient<app1.Servicios.WikipediaService>();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Registrar SentimentService como singleton (sin argumentos)
builder.Services.AddSingleton<SentimentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Configurar variable de entorno para Google Cloud Natural Language
var googleCreds = builder.Configuration["GoogleCloud:CredentialsPath"];
var googleCredsJson = builder.Configuration["GoogleCloud:CredentialsJson"];
if (!string.IsNullOrEmpty(googleCredsJson))
{
    // Si la variable contiene el contenido del JSON, escribirlo a un archivo temporal
    var tempPath = Path.Combine(Path.GetTempPath(), $"google-creds-{Guid.NewGuid()}.json");
    File.WriteAllText(tempPath, googleCredsJson);
    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", tempPath);
}
else if (!string.IsNullOrEmpty(googleCreds))
{
    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", googleCreds);
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    // Crear el rol 'Cliente' si no existe
    if (!await roleManager.RoleExistsAsync("Cliente"))
    {
        await roleManager.CreateAsync(new IdentityRole("Cliente"));
    }
    // Asignar el rol 'Cliente' a todos los usuarios que no tengan rol
    var users = userManager.Users.ToList();
    foreach (var user in users)
    {
        var roles = await userManager.GetRolesAsync(user);
        if (roles.Count == 0)
        {
            await userManager.AddToRoleAsync(user, "Cliente");
        }
    }
}

app.Run();
