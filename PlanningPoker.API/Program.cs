using Microsoft.EntityFrameworkCore;
using PlanningPoker.API.Data;
using PlanningPoker.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

// 1. VERÝTABANI: SQL Server yerine In-Memory Database (Render Ücretsiz Plan Dostu)
// Bu satýr sayesinde uygulama kendi içinde sanal bir DB oluþturur, dýþarýdan SQL aramaz.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("PlanningPokerDb"));

// 2. CORS POLÝTÝKASI: Hem yerel (Localhost) hem canlý (Render) eriþimi saðlar.
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://vbplanningpokerb.onrender.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // SignalR canlý baðlantýsý için hayati!
    });
});

// 3. Servis Kayýtlarý
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 4. MÝDDLEWARE SIRALAMASI (Bu sýra bozulmamalý!)
// Swagger'ý her zaman aktif ettik ki canlýda test edebilesin.
app.UseSwagger();
app.UseSwaggerUI();

// KRÝTÝK: Önce CORS, sonra Authorization
app.UseCors("CorsPolicy");

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// 5. SignalR Kapýsýný Açýyoruz
app.MapHub<PlanningPokerHub>("/pokerhub");

app.Run();