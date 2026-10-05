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

    var existingFrameNames = dbContext.Frames.Select(f => f.Name).ToHashSet();
    var sampleFrames = new List<Frame>
    {
        new Frame
        {
            Name = "Elegant Cat Eye Rose",
            Brand = "Vogue Eyewear",
            Code = "VOG-401",
            Gender = "Women",
            Material = "Plastic",
            Shape = "Cat Eye",
            Color = "Rose Pink",
            Price = 145.00m,
            ImageUrl = "cateye-pink-plastic.jpg",
            IsAvailable = true
        },
        new Frame
        {
            Name = "Elegant Cat Eye Pearl",
            Brand = "Dolce & Gabbana",
            Code = "DG-502",
            Gender = "Women",
            Material = "Plastic",
            Shape = "Cat Eye",
            Color = "Pearl White",
            Price = 165.00m,
            ImageUrl = "cateye-white-plastic.jpg",
            IsAvailable = true
        },
        new Frame
        {
            Name = "Luna Luxe",
            Brand = "ClearVision",
            Code = "CLV-102",
            Gender = "Unisex",
            Material = "Titanium",
            Shape = "Round",
            Color = "Matte Gold",
            Price = 169.99m,
            ImageUrl = "round-gold-matte.jpg",
            IsAvailable = true
        },
        new Frame
        {
            Name = "Aeris Modern",
            Brand = "Optique Studio",
            Code = "OPS-221",
            Gender = "Men",
            Material = "Acetate",
            Shape = "Square",
            Color = "Classic Black",
            Price = 129.50m,
            ImageUrl = "square-black-acetate.jpg",
            IsAvailable = true
        },
        new Frame
        {
            Name = "Solara Edge",
            Brand = "Visionary Craft",
            Code = "VCR-308",
            Gender = "Men",
            Material = "Mixed Metal",
            Shape = "Rectangle",
            Color = "Gunmetal",
            Price = 189.00m,
            ImageUrl = "rectangle-gunmetal.jpg",
            IsAvailable = true
        },
        new Frame
        {
            Name = "Royal Gold Luxury Round",
            Brand = "Ray-Ban",
            Code = "RB-710",
            Gender = "Unisex",
            Material = "Metal",
            Shape = "Round",
            Color = "Polished Gold",
            Price = 199.00m,
            ImageUrl = "round-gold-luxury.jpg",
            IsAvailable = true
        },
        new Frame
        {
            Name = "Executive Titanium Rectangle",
            Brand = "Silhouette",
            Code = "SIL-880",
            Gender = "Men",
            Material = "Titanium",
            Shape = "Rectangle",
            Color = "Space Gray",
            Price = 210.00m,
            ImageUrl = "rectangle-gray-titanium.jpg",
            IsAvailable = true
        },
        new Frame
        {
            Name = "Nordic Silver Titanium",
            Brand = "Lindberg",
            Code = "LND-334",
            Gender = "Unisex",
            Material = "Titanium",
            Shape = "Round",
            Color = "Silver",
            Price = 230.00m,
            ImageUrl = "round-silver-titanium.jpg",
            IsAvailable = true
        },
        new Frame
        {
            Name = "Sapphire Blue Square",
            Brand = "Armani Exchange",
            Code = "AX-609",
            Gender = "Men",
            Material = "Metal",
            Shape = "Square",
            Color = "Ocean Blue",
            Price = 139.00m,
            ImageUrl = "square-blue-metal.jpg",
            IsAvailable = true
        },
        new Frame
        {
            Name = "Crystal Clear Heritage",
            Brand = "Oliver Peoples",
            Code = "OP-912",
            Gender = "Unisex",
            Material = "TR90 Ultra-Light",
            Shape = "Round",
            Color = "Transparent Crystal",
            Price = 179.00m,
            ImageUrl = "round-clear-plastic.jpg",
            IsAvailable = true
        },
        new Frame
        {
            Name = "Professional Black Metal",
            Brand = "Oakley",
            Code = "OK-450",
            Gender = "Men",
            Material = "Metal",
            Shape = "Rectangle",
            Color = "Satin Black",
            Price = 155.00m,
            ImageUrl = "rectangle-black-metal.jpg",
            IsAvailable = true
        },
        new Frame
        {
            Name = "Signature Brown Tortoise",
            Brand = "Prada",
            Code = "PRD-204",
            Gender = "Women",
            Material = "Acetate",
            Shape = "Round",
            Color = "Amber Brown",
            Price = 185.00m,
            ImageUrl = "round-brown-plastic.jpg",
            IsAvailable = true
        }
    };

    bool hasNewFrames = false;
    foreach (var frame in sampleFrames)
    {
        if (!existingFrameNames.Contains(frame.Name))
        {
            dbContext.Frames.Add(frame);
            hasNewFrames = true;
        }
    }
    if (hasNewFrames)
    {
        dbContext.SaveChanges();
    }

    var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<UserAccount>();
    var safaaUser = dbContext.UserAccounts.FirstOrDefault(u => u.Username == "safaa");
    if (safaaUser != null && safaaUser.Role != UserRole.Admin)
    {
        safaaUser.Role = UserRole.Admin;
        dbContext.SaveChanges();
    }

    var legacyAdmin = dbContext.UserAccounts.FirstOrDefault(u =>
        u.Username == "admin" || u.Email == "admin@opticalcare.com");
    if (legacyAdmin != null)
    {
        dbContext.UserAccounts.Remove(legacyAdmin);
        dbContext.SaveChanges();
    }

    var previousSafaAdmin = dbContext.UserAccounts.FirstOrDefault(u =>
        u.Username == "safa" && u.Role == UserRole.Admin);
    if (previousSafaAdmin != null)
    {
        previousSafaAdmin.Role = UserRole.Patient;
        dbContext.SaveChanges();
    }

    // Hubi in Doctor default ah jiro (dr.ali / Doctor@123)
    var doctorUser = dbContext.UserAccounts.FirstOrDefault(u => u.Username == "dr.ali");
    if (doctorUser == null)
    {
        doctorUser = new UserAccount
        {
            Username = "dr.ali",
            Email = "dr.ali@opticalcare.com",
            FullName = "Dr. Ali Hassan",
            Role = UserRole.Doctor,
            CreatedAt = DateTime.UtcNow
        };
        doctorUser.PasswordHash = passwordHasher.HashPassword(doctorUser, "Doctor@123");
        dbContext.UserAccounts.Add(doctorUser);
        dbContext.SaveChanges();
    }
    else if (doctorUser.Role != UserRole.Doctor)
    {
        doctorUser.Role = UserRole.Doctor;
        doctorUser.PasswordHash = passwordHasher.HashPassword(doctorUser, "Doctor@123");
        dbContext.SaveChanges();
    }

    var doctorProfile = dbContext.Doctors.FirstOrDefault(d => d.UserAccountId == doctorUser.Id || d.Email == doctorUser.Email);
    if (doctorProfile == null)
    {
        doctorProfile = new Doctor
        {
            FullName = "Dr. Ali Hassan",
            Email = "dr.ali@opticalcare.com",
            Phone = "+252 61 5000000",
            Specialty = "Optometrist / Eye Specialist",
            LicenseNumber = "OPT-9921",
            UserAccountId = doctorUser.Id,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Doctors.Add(doctorProfile);
        dbContext.SaveChanges();
    }
    else if (doctorProfile.UserAccountId == null)
    {
        doctorProfile.UserAccountId = doctorUser.Id;
        dbContext.SaveChanges();
    }
}

app.Run();