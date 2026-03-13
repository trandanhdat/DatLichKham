using Microsoft.AspNetCore.SignalR;

namespace DatLichKham.Hubs
{
    public class ChatHub : Hub
    {
        // Mỗi user join vào group riêng theo userId
        // Để server có thể push đúng người nhận
        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        public async Task LeaveUserGroup(string userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
    }
}
