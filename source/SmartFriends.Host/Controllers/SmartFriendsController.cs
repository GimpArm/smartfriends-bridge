using Logging.Memory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartFriends.Api;
using SmartFriends.Api.Models;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartFriends.Host.Controllers
{
    [ApiController]
    [Route("[action]")]
    [Produces("application/json")]
    public class SmartFriendsController : ControllerBase
    {
        private readonly ILogger<SmartFriendsController> _logger;
        private readonly Session _session;

        public SmartFriendsController(ILogger<SmartFriendsController> logger, Session session)
        {
            _logger = logger;
            _session = session;
        }

        [HttpGet]
        public IEnumerable<DeviceMaster> List()
        {
            return _session.DeviceMasters;
        }

        [HttpGet]
        public object Raw()
        {
            return _session.RawDevices;
        }

        [HttpGet]
        public IEnumerable<string> Log()
        {
            return MemoryLogger.LogList;
        }

        [HttpGet("{id}")]
        public DeviceMaster Get(int id)
        {
            return _session.GetDevice(id);
        }

        [HttpGet("{id}/{value}")]
        public async Task<bool> Set(int id, string value)
        {
            _logger.LogInformation($"Set: {id}: {value}");
            if (int.TryParse(value, out var intValue))
            {
                return await _session.SetDeviceValue(id, intValue);
            }
            if (bool.TryParse(value, out var boolValue))
            {
                return await _session.SetDeviceValue(id, boolValue);
            }
            return await _session.SetDeviceValue(id, value);
        }

        [HttpGet("{id}/{value}")]
        public async Task<bool> Hsv(int id, string value)
        {
            var values = Regex.Split(value, "[^\\d]+");

            if (values.Length != 3) return false;

            var hsv = new HsvValue
            {
                H = int.Parse(values[0]),
                S = int.Parse(values[1]),
                V = int.Parse(values[2])
            };
            return await _session.SetDeviceValue(id, hsv);
        }
    }
}
