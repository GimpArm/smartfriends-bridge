using SmartFriends.Api.Models;
using SmartFriends.Api.Models.Commands;

namespace SmartFriends.Api.Interfaces;

public interface IClient : IDisposable
{
    bool Connected { get; }
    string? GatewayDevice { get; }
    event EventHandler<DeviceValue> DeviceUpdated;
    event EventHandler<Message> MessageReceived;
    Task Close();
    Task<bool> Open();
    Task SendAndReceiveCommand<T>(CommandBase command, Action<T?> action, int timeout = 2500);
    Task SendAndReceiveCommand<T>(CommandBase command, Func<T?, Task> action, int timeout = 2500);
    Task SendCommand(CommandBase command);
}