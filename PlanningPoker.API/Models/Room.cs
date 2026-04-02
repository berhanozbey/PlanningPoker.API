namespace PlanningPoker.API.Models
{
    public class Room
    {
        public Guid Id { get; set; } = Guid.NewGuid(); // Odanın benzersiz şifreli ID'si
        public string Name { get; set; } // Odanın adı (Örn: Sprint 1 Planlama)
        public bool IsVotingRevealed { get; set; } = false; // Oylar açıldı mı?

        // Bir odada birden fazla kullanıcı olabilir
        public ICollection<User> Users { get; set; }
        public string CurrentTaskName { get; internal set; }
    }
    namespace PlanningPoker.API.Models
    {
        public class Room
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public string Name { get; set; }
            public bool IsVotingRevealed { get; set; } = false;

            // YENİ EKLENEN KISIM: Odanın o anki konusu/görevi
            
            public string CurrentTaskName { get; set; } = "Henüz bir görev belirlenmedi";

            public ICollection<User> Users { get; set; }
        }
    }
}