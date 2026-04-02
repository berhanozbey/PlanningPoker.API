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

        // 1. Odaya Katılma ve Bağlantıyı Eşleştirme
        public async Task JoinRoom(Guid roomId, Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.SignalRConnectionId = Context.ConnectionId;
                await _context.SaveChangesAsync();

                await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
                await Clients.Group(roomId.ToString()).SendAsync("UserUpdated");
            }
        }

        // 2. Oy Verme İşlemi (Düzenleme Takibi Dahil)
        public async Task SubmitVote(Guid roomId, Guid userId, string vote)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                // Eğer kullanıcının zaten bir oyu varsa ve bu yeni oy eskisinden farklıysa, düzenlendi işaretini (IsEdited) koy
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

        // 8. ✨ BAĞLANTISI KOPAN KULLANICIYI VE BOŞ ODALARI OTOMATİK TEMİZLE ✨
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Bağlantı ID'si üzerinden düşen kullanıcıyı bul
            var user = await _context.Users.FirstOrDefaultAsync(u => u.SignalRConnectionId == Context.ConnectionId);

            if (user != null)
            {
                var roomId = user.RoomId;

                // Önce düşen kullanıcıyı masadan kaldır
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                // DİKKAT: Odada başka "GERÇEK" (Bot olmayan) insan kaldı mı kontrol et
                var remainingHumans = await _context.Users.AnyAsync(u => u.RoomId == roomId && !u.Name.StartsWith("[Bot]"));

                if (!remainingHumans)
                {
                    // Odada hiç insan kalmadıysa odayı bul
                    var roomToDelete = await _context.Rooms.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == roomId);

                    if (roomToDelete != null)
                    {
                        // Odayı sil (İçinde kalan sahipsiz botlar da Entity Framework tarafından otomatik silinir)
                        _context.Rooms.Remove(roomToDelete);
                        await _context.SaveChangesAsync();
                        // Herkes çıktığı için SignalR ile gruba mesaj atmaya gerek kalmadı
                    }
                }
                else
                {
                    // Odada hala gerçek insanlar varsa, sadece listeyi güncellemeleri için sinyal gönder
                    await Clients.Group(roomId.ToString()).SendAsync("UserUpdated");
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}