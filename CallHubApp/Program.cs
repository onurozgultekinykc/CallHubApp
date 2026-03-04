using CallHubApp;
using CallHubApp.Models;
using CallHubApp.Hubs; // 👈 Hub klasörünü ekledik
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR(); // 👈 SignalR servisini ekledik

// appsettings.json'dan Servers al (eğer hâlâ kullanıyorsan)
var servers = builder.Configuration
                     .GetSection("Servers")
                     .Get<List<ServerConfig>>();
builder.Services.AddSingleton(servers);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true); // test için tüm originlere izin
    });
});

builder.WebHost.UseUrls("http://+:5199");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();

// MVC routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 👇 SignalR Hub endpoint
app.MapHub<CallHub>("/hubs/phone");


app.Run();
