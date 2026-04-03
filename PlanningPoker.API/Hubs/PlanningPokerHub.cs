using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PlanningPoker.API.Data;
using PlanningPoker.API.Models;

namespace PlanningPoker.API.Hubs
{
    public class PlanningPokerHub : Hub
    {
        private readonly AppDbContext _context;

        public PlanningPokerHub(AppDbContext context)
        {
            _context = context;
        }

        // 1. Odaya Katılma ve KUSURSUZ YENİDEN BAĞLANMA (Auto-Rejoin)
        public async Task JoinRoom(Guid roomId, Guid userId, string userName, int role, string currentVote)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                // Adam F5 atmış ve silinmiş! Onu hafızasındaki bilgilerle masaya GİZLİCE geri ekliyoruz:
                user = new User
                {
                    Id = userId,
                    Name = userName,
                    Role = (UserRole)role,
                    RoomId = roomId,
                    CurrentVote = string.IsNullOrEmpty(currentVote) ? null : currentVote,
                    IsEdited = false
                };
                _context.Users.Add(user);
            }

            user.SignalRConnectionId = Context.ConnectionId;
            await _context.SaveChangesAsync();

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
            await Clients.Group(roomId.ToString()).SendAsync("UserUpdated");
        }

        // 2. Oy Verme İşlemi (Düzenleme Takibi ve Hile Koruması)
        public async Task SubmitVote(Guid roomId, Guid userId, string vote)
        {
            var user = await _context.Users.FindAsync(userId);
            var room = await _context.Rooms.FindAsync(roomId);

            if (user != null && room != null)
            {
                // ✨ UYANIK DEVELOPER KORUMASI: Oylar açıldıysa oy atılamaz!
                if (room.IsVotingRevealed) return;

                if (!string.IsNullOrEmpty(user.CurrentVote) && user.CurrentVote != vote)
                {
                    user.IsEdited = true;
                }

                user.CurrentVote = vote;
                await _context.SaveChangesAsync();

                await Clients.Group(roomId.ToString()).SendAsync("UserUpdated");
            }
        }

        // 3. Oyları Açma
        public async Task RevealVotes(Guid roomId)
        {
            // ✨ GÜVENLİK DUVARI: İsteği atan kişiyi bul ve İzleyici (2) ise reddet
            var caller = await _context.Users.FirstOrDefaultAsync(u => u.SignalRConnectionId == Context.ConnectionId);
            if (caller == null || (int)caller.Role == 2) return;

            var room = await _context.Rooms.FindAsync(roomId);
            if (room != null)
            {
                room.IsVotingRevealed = true;
                await _context.SaveChangesAsync();

                await Clients.Group(roomId.ToString()).SendAsync("VotesRevealed");
            }
        }

        // 4. Yeni Tur Başlatma (Oyları ve Kalem İkonlarını Temizler)
        public async Task ClearVotes(Guid roomId)
        {
            // ✨ GÜVENLİK DUVARI: İzleyici (2) ise reddet
            var caller = await _context.Users.FirstOrDefaultAsync(u => u.SignalRConnectionId == Context.ConnectionId);
            if (caller == null || (int)caller.Role == 2) return;

            var room = await _context.Rooms.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == roomId);
            if (room != null)
            {
                room.IsVotingRevealed = false;

                foreach (var user in room.Users)
                {
                    user.IsEdited = false; // Kalem ikonunu sıfırla

                    if (user.Name.StartsWith("[Bot]"))
                    {
                        string[] cards = { "1", "2", "3", "5", "8", "13", "21", "34" };
                        user.CurrentVote = cards[new Random().Next(cards.Length)];
                    }
                    else
                    {
                        user.CurrentVote = null;
                    }
                }

                await _context.SaveChangesAsync();
                await Clients.Group(roomId.ToString()).SendAsync("VotesCleared");
            }
        }

        // 5. Masaya Bot Ekleme
        public async Task AddBot(Guid roomId)
        {
            // ✨ GÜVENLİK DUVARI: İzleyici (2) ise reddet
            var caller = await _context.Users.FirstOrDefaultAsync(u => u.SignalRConnectionId == Context.ConnectionId);
            if (caller == null || (int)caller.Role == 2) return;

            var room = await _context.Rooms.FindAsync(roomId);
            if (room != null)
            {
                string[] botNames = { "Yusuf Talha", "Ege", "Oğuz", "Berhan", "Mert", "Olcay" };
                string[] cards = { "1", "2", "3", "5", "8", "13", "21", "34" };
                var random = new Random();

                var botUser = new User
                {
                    Name = "[Bot] " + botNames[random.Next(botNames.Length)],
                    Role = UserRole.Developer,
                    RoomId = roomId,
                    CurrentVote = cards[random.Next(cards.Length)],
                    IsEdited = false
                };

                _context.Users.Add(botUser);
                await _context.SaveChangesAsync();
                await Clients.Group(roomId.ToString()).SendAsync("UserUpdated");
            }
        }

        // 6. Botları Kovma
        public async Task RemoveBots(Guid roomId)
        {
            // ✨ GÜVENLİK DUVARI: İzleyici (2) ise reddet
            var caller = await _context.Users.FirstOrDefaultAsync(u => u.SignalRConnectionId == Context.ConnectionId);
            if (caller == null || (int)caller.Role == 2) return;

            var room = await _context.Rooms.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == roomId);
            if (room != null)
            {
                var bots = room.Users.Where(u => u.Name.StartsWith("[Bot]")).ToList();
                _context.Users.RemoveRange(bots);

                await _context.SaveChangesAsync();
                await Clients.Group(roomId.ToString()).SendAsync("UserUpdated");
            }
        }

        // 7. Görev Değiştirme
        public async Task ChangeTask(Guid roomId, string newTaskName)
        {
            // ✨ GÜVENLİK DUVARI: İzleyici (2) ise reddet
            var caller = await _context.Users.FirstOrDefaultAsync(u => u.SignalRConnectionId == Context.ConnectionId);
            if (caller == null || (int)caller.Role == 2) return;

            var room = await _context.Rooms.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == roomId);
            if (room != null)
            {
                room.CurrentTaskName = newTaskName;
                room.IsVotingRevealed = false;

                foreach (var user in room.Users)
                {
                    user.IsEdited = false;

                    if (user.Name.StartsWith("[Bot]"))
                    {
                        string[] cards = { "1", "2", "3", "5", "8", "13", "21", "34" };
                        user.CurrentVote = cards[new Random().Next(cards.Length)];
                    }
                    else
                    {
                        user.CurrentVote = null;
                    }
                }

                await _context.SaveChangesAsync();

                await Clients.Group(roomId.ToString()).SendAsync("TaskChanged", newTaskName);
                await Clients.Group(roomId.ToString()).SendAsync("VotesCleared");
            }
        }

        // 8. ✨ SEKMEYİ KAPATANLARI VE BOŞ ODALARI ANINDA TEMİZLE ✨
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.SignalRConnectionId == Context.ConnectionId);

            if (user != null)
            {
                var roomId = user.RoomId;

                // Sekmeyi kapatanı (veya yenileyeni) anında sil
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                var remainingHumans = await _context.Users.AnyAsync(u => u.RoomId == roomId && !u.Name.StartsWith("[Bot]"));

                if (!remainingHumans)
                {
                    // Odada gerçek insan kalmadıysa odayı yok et
                    var roomToDelete = await _context.Rooms.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == roomId);

                    if (roomToDelete != null)
                    {
                        _context.Rooms.Remove(roomToDelete);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // Kalanlara listeyi güncelle mesajı at
                    await Clients.Group(roomId.ToString()).SendAsync("UserUpdated");
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}