using System;
using System.Collections.Generic;

namespace IqcQms.Domain.Entities.Chat
{
    public class Message
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;

        public int SenderId { get; set; }
        // public Auth.User Sender { get; set; } = null!;

        public string Content { get; set; } = string.Empty; // text content, can include markup for mentions
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } // if edited
        public DateTime? DeletedAt { get; set; } // soft delete

        public ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
        public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
    }
}
