using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rebel.Infrastructure.Data;
using Rebel.Web.Services;
using Rebel.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>();

// EMAIL SETTINGS
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings")
);

// EMAIL SERVICE
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

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
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var dbContext =
        services.GetRequiredService<AppDbContext>();

    await dbContext.Database.MigrateAsync();

    var userManager =
        services.GetRequiredService<UserManager<IdentityUser>>();

    var roleManager =
        services.GetRequiredService<RoleManager<IdentityRole>>();

    var adminEmail = app.Configuration["AdminUser:Email"]
    ?? throw new InvalidOperationException(
        "AdminUser:Email is missing from configuration.");

    var adminPassword = app.Configuration["AdminUser:Password"]
        ?? throw new InvalidOperationException(
            "AdminUser:Password is missing from configuration.");

    const string adminRole = "Admin";

    if (!await roleManager.RoleExistsAsync(adminRole))
    {
        await roleManager.CreateAsync(
            new IdentityRole(adminRole)
        );
    }

    var adminUser =
        await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(
            adminUser,
            adminPassword
        );

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(
                adminUser,
                adminRole
            );
        }
    }
}
app.MapHub<NotificationHub>("/notificationHub");

app.Run();
