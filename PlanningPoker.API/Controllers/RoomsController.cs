using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanningPoker.API.Data;
using PlanningPoker.API.Models;

namespace PlanningPoker.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RoomsController(AppDbContext context)
        {
            _context = context;
        }

        // Angular'dan gelen veriyi karşılamak için özel sınıf (DTO)
        public class CreateRoomRequest
        {
            public string RoomName { get; set; }
        }

        // 1. YENİ ODA OLUŞTURMA API'Sİ (POST: api/rooms/create)
        [HttpPost("create")]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
        {
            // Eğer request null ise veya isim boşsa hata dön
            if (request == null || string.IsNullOrWhiteSpace(request.RoomName))
                return BadRequest("Oda adı boş olamaz.");

            try
            {
                var newRoom = new Room
                {
                    Name = request.RoomName,
                    IsVotingRevealed = false,
                    // Veritabanındaki "NOT NULL" hatasını önlemek için varsayılan değer
                    CurrentTaskName = "Henüz bir görev belirlenmedi"
                };

                _context.Rooms.Add(newRoom);
                await _context.SaveChangesAsync();

                // Angular tarafına beklediği formatta (id ve name) dönüyoruz
                return Ok(new { id = newRoom.Id, name = newRoom.Name });
            }
            catch (Exception ex)
            {
                // Bir hata olursa detayını terminale yazdır
                Console.WriteLine("Oda oluşturma hatası: " + ex.Message);
                return StatusCode(500, "Sunucu iç hatası oluştu.");
            }
        }

        // 2. ODA BİLGİLERİNİ GETİRME (GET: api/rooms/{id})
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoom(Guid id)
        {
            // Odayı ve içindeki Kullanıcıları birlikte çekiyoruz
            var room = await _context.Rooms
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null) return NotFound("Oda bulunamadı.");

            // Sonsuz döngü (Cycle) olmaması için temiz nesne dönüyoruz
            return Ok(new
            {
                id = room.Id,
                name = room.Name,
                isVotingRevealed = room.IsVotingRevealed,
                currentTaskName = room.CurrentTaskName,
                users = room.Users.Select(u => new {
                    id = u.Id,
                    name = u.Name,
                    role = u.Role,
                    currentVote = u.CurrentVote
                }).ToList()
            });
        }
    }
}