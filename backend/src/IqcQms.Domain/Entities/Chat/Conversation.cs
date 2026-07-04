using System;
using System.Collections.Generic;

namespace IqcQms.Domain.Entities.Chat
{
    public class Conversation
    {
        public int Id { get; set; }
        public string? Title { get; set; } // Null for 1-on-1, populated for Group
        public bool IsGroup { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
