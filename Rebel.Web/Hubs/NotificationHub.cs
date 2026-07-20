using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Rebel.Web.Hubs
{
    [Authorize(Roles = "Admin")]
    public class NotificationHub : Hub
    {
    }
}