using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RoboOps.Api.Models;
using RoboOps.Api.Services;

namespace RoboOps.Api.Realtime;

[Authorize]
public sealed class TelemetryHub(RoboOpsStore store) : Hub
{
    public async Task WatchDashboard()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "dashboard");
    }

    public async Task WatchRobot(Guid robotId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(robotId));
    }

    public async Task SendCommand(RobotCommand command)
    {
        var user = Context.User;
        if (user is null || (!user.IsInRole("Operator") && !user.IsInRole("Admin")))
        {
            throw new HubException("Only operators and admins can send robot commands.");
        }

        var issuedBy = user.Identity?.Name ?? command.IssuedBy;
        var enrichedCommand = command with { IssuedBy = issuedBy };
        store.ApplyCommand(enrichedCommand);

        await Clients.Group(GroupName(command.RobotId)).SendAsync("commandAccepted", enrichedCommand);
    }

    public static string GroupName(Guid robotId) => $"robot:{robotId}";
}
