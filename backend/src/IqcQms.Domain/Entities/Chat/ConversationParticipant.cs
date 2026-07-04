using System;

namespace IqcQms.Domain.Entities.Chat
{
    public class ConversationParticipant
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;
        
        public int UserId { get; set; }
        // public Auth.User User { get; set; } = null!;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastReadAt { get; set; } // For read receipts
    }
}
