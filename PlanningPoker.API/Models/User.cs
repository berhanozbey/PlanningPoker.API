namespace PlanningPoker.API.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } // Kullanıcının adı
        public UserRole Role { get; set; } // Rolü (Scrum Master, Developer vb.)
        public string? CurrentVote { get; set; } // O anki verdiği oy (Boş olabilir diye ? koyduk)
        public string? SignalRConnectionId { get; set; } // Canlı bağlantı kimliği

        // Bu kullanıcı hangi odaya ait? (İlişki kuruyoruz)
        public Guid RoomId { get; set; }
        public Room Room { get; set; }

        public bool IsEdited { get; set; } = false;
    }
}