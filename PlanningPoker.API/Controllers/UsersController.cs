using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using PlanningPoker.API.Data;
using PlanningPoker.API.Models;
using PlanningPoker.API.Hubs;

namespace PlanningPoker.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<PlanningPokerHub> _hubContext;

        public class JoinRoomRequest
        {
            public Guid RoomId { get; set; }
            public string UserName { get; set; }
            public UserRole Role { get; set; }
        }

        public UsersController(AppDbContext context, IHubContext<PlanningPokerHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // 1. KULLANICIYI ODAYA KAYDETME
        [HttpPost("join")]
        public async Task<IActionResult> JoinRoom([FromBody] JoinRoomRequest request)
        {
            // ODA KONTROLÜ: Eğer oda silinmişse eski linkle girmeyi engeller (404 döner)
            var room = await _context.Rooms.FindAsync(request.RoomId);
            if (room == null)
            {
                return NotFound("Böyle bir oda bulunamadı veya oda silinmiş.");
            }

            var newUser = new User
            {
                Name = request.UserName,
                Role = request.Role,
                RoomId = request.RoomId
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = newUser.Id,
                name = newUser.Name,
                role = newUser.Role
            });
        }

        // 2. ODADAN AYRILMA (✨ GÜNCELLEME: BOŞ ODAYI SİLME MANTIĞI EKLENDİ)
        [HttpDelete("leave/{roomId}/{userId}")]
        public async Task<IActionResult> LeaveRoom(Guid roomId, Guid userId)
        {
            // Ayrılan kullanıcıyı bul
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.RoomId == roomId);

            if (user != null)
            {
                // Önce kullanıcıyı masadan kaldır
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                // ✨ KONTROL: Masada başka "GERÇEK" insan kaldı mı?
                var remainingHumans = await _context.Users.AnyAsync(u => u.RoomId == roomId && !u.Name.StartsWith("[Bot]"));

                if (!remainingHumans)
                {
                    // Odada hiç insan kalmadıysa odayı bul
                    var roomToDelete = await _context.Rooms.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == roomId);

                    if (roomToDelete != null)
                    {
                        // Odayı ve içindeki sahipsiz botları tamamen sil
                        _context.Rooms.Remove(roomToDelete);
                        await _context.SaveChangesAsync();
                        // Herkes çıktığı için SignalR ile mesaj atmaya gerek kalmadı
                    }
                }
                else
                {
                    // Masada hala insanlar varsa, onlara "biri çıktı" diye haber ver
                    await _hubContext.Clients.Group(roomId.ToString()).SendAsync("UserUpdated");
                }
            }

            return Ok();
        }
    }
}