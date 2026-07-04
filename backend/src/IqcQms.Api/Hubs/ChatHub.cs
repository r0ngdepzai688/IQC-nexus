using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace IqcQms.Api.Hubs
{
    public class ChatHub : Hub
    {
        // Join a conversation group (room)
        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }

        // Leave a conversation group
        public async Task LeaveConversation(string conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
        }

        // Send a message to a specific conversation
        public async Task SendMessage(string conversationId, int senderId, string content)
        {
            // Here you would typically also save the message to the database via a service
            
            // Broadcast to everyone in the conversation
            await Clients.Group(conversationId).SendAsync("ReceiveMessage", new
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Content = content,
                CreatedAt = System.DateTime.UtcNow
            });
        }
    }
}
