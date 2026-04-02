using Microsoft.EntityFrameworkCore;
using PlanningPoker.API.Data;
using PlanningPoker.API.Hubs; // Hub klasörünü tanýmasý için

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabaný Baðlantýsý
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. KRÝTÝK: CORS Politikasýný Tanýmla
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Angular adresi
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // SignalR için EN ÖNEMLÝ satýr
    });
});

// 3. SignalR ve Controller Servisleri
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 4. SIRALAMA ÇOK ÖNEMLÝ: Pipeline (Boru Hattý) Yapýlandýrmasý
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ÖNCE Cors, SONRA Authorization!
app.UseCors("CorsPolicy");

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// 5. SignalR Kapýsýný Aç
app.MapHub<PlanningPokerHub>("/pokerhub");

app.Run();