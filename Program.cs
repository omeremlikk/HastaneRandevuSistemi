using Microsoft.EntityFrameworkCore;
using hastane.Data;
using hastane.Services;
using System;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Türkiye kültür ayarını yapılandır
var turkishCulture = new CultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = turkishCulture;
CultureInfo.DefaultThreadCurrentUICulture = turkishCulture;

// Add services to the container.
builder.Services.AddControllersWithViews();

// HttpClient servisini ekle (PolTahmin API için)
builder.Services.AddHttpClient();

// Flask API otomatik başlatma servisi
builder.Services.AddHostedService<FlaskApiService>();

// Oturum yönetimini ekle
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// SQLite veritabanı servisini ekle
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=hastane.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

// Oturum middleware'ini ekle
app.UseSession();

// Veritabanını oluştur
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        // Uygulama başladığında saat dilimi farkını logla
        var localNow = DateTime.Now;
        var utcNow = DateTime.UtcNow;
        Console.WriteLine($"Uygulama başladı. Yerel saat: {localNow:dd.MM.yyyy HH:mm:ss}, UTC saat: {utcNow:dd.MM.yyyy HH:mm:ss}");
        Console.WriteLine($"Saat dilimi farkı: {localNow - utcNow}");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı oluşturulurken bir hata oluştu.");
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
