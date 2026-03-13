using ClinicBookingSystem.Models;
using DatLichKham.Hubs;
using DatLichKham.Services;
using Microsoft.AspNetCore.SignalR;
using DatLichKham.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
// ── BƯỚC 1: Thêm using ở đầu Program.cs ──────────────────────

// ── BƯỚC 2: Đăng ký SignalR (trước builder.Build()) ──────────
builder.Services.AddSignalR();

// ── BƯỚC 3: Map Hub (sau app.MapControllerRoute) ──────────────
// Add services to the container.
builder.Services.AddControllersWithViews();
// Thêm vào phần builder.Services
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<SessionCheckFilter>();
});
builder.Services.AddDbContext<ApplicationDbContext>(optione =>
{
    optione.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();


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
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");
app.MapHub<ChatHub>("/chatHub");

app.Run();
