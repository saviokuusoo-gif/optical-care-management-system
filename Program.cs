using Microsoft.AspNetCore.Authentication.Cookies; // 
using Microsoft.EntityFrameworkCore;
using System.Linq;
using optical_care_management_system.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Authentication Service
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Authentication iyo Authorization (Waa inay ku jiraan meeshan)
app.UseAuthentication(); 
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();

    if (!dbContext.Frames.Any())
    {
        dbContext.Frames.AddRange(
            new Frame
            {
                Name = "Luna Luxe",
                Brand = "ClearVision",
                Code = "CLV-102",
                Material = "Titanium",
                Shape = "Round",
                Color = "Matte Gold",
                Price = 169.99m,
                ImageUrl = "frame1.jpg",
                IsAvailable = true
            },
            new Frame
            {
                Name = "Aeris Modern",
                Brand = "Optique Studio",
                Code = "OPS-221",
                Material = "Acetate",
                Shape = "Square",
                Color = "Black",
                Price = 129.50m,
                ImageUrl = "frame2.jpg",
                IsAvailable = true
            },
            new Frame
            {
                Name = "Solara Edge",
                Brand = "Visionary Craft",
                Code = "VCR-308",
                Material = "Mixed Metal",
                Shape = "Rectangle",
                Color = "Gunmetal",
                Price = 189.00m,
                ImageUrl = "frame3.jpg",
                IsAvailable = false
            });

        dbContext.SaveChanges();
    }
}

app.Run();