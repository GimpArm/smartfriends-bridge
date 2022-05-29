using SmartFriends.Api.Models;
using SmartFriends.Api.Models.Commands;
using System;
using System.Threading.Tasks;

namespace SmartFriends.Api.Interfaces
{
    public interface IClient: IDisposable
    {
        bool Connected { get; }
        string GatewayDevice { get; }
        event EventHandler<DeviceValue> DeviceUpdated;
        Task Close();
        Task<bool> Open();
        Task<T> SendAndReceiveCommand<T>(CommandBase command, int timeout = 2500);
        Task<bool> SendCommand(CommandBase command, int timeout = 2500);
    }
}