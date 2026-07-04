using System;

namespace IqcQms.Domain.Entities.Chat
{
    public class MessageReaction
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        public Message Message { get; set; } = null!;

        public int UserId { get; set; } // The user who reacted
        // public Auth.User User { get; set; } = null!;

        public string EmojiCode { get; set; } = string.Empty; // e.g., '👍', '❤️'
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
