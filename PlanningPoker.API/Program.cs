using Microsoft.EntityFrameworkCore;
using PlanningPoker.API.Data;
using PlanningPoker.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabaný Baðlantýsý
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. GÜNCELLENEN CORS POLÝTÝKASI (Hem Yerel Hem Canlý Link Eklendi)
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://vbplanningpokerb.onrender.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // SignalR için hayati önem taþýr
    });
});

// 3. SignalR ve Controller Servisleri
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 4. SIRALAMA (Pipeline Yapýlandýrmasý)
// Not: Swagger'ý canlýda da görmek istersen if (app.Environment.IsDevelopment()) kýsmýný kaldýrabilirsin.
app.UseSwagger();
app.UseSwaggerUI();

// EN ÖNEMLÝ SIRALAMA: Önce CORS, sonra Authorization!
app.UseCors("CorsPolicy");

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// 5. SignalR Kapýsýný Aç
app.MapHub<PlanningPokerHub>("/pokerhub");

app.Run();