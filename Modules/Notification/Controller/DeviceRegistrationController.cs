using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using starterapi.Models;
using starterapi.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace starterapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeviceRegistrationController : ControllerBase
    {
        private readonly ITenantDbContextAccessor _contextAccessor;

        public DeviceRegistrationController(ITenantDbContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterDevice([FromBody] DeviceRegistrationRequest request)
        {
            var userId = int.Parse(User.Identity.Name);

            var existingDevice = await _contextAccessor.TenantDbContext.RegisteredDevices
                .FirstOrDefaultAsync(d => d.DeviceToken == request.DeviceToken);

            if (existingDevice != null)
            {
                existingDevice.DeviceType = request.DeviceType;
                _contextAccessor.TenantDbContext.RegisteredDevices.Update(existingDevice);
            }
            else
            {
                var newDevice = new RegisteredDevice
                {
                    DeviceToken = request.DeviceToken,
                    DeviceType = request.DeviceType,
                    CreatedAt = DateTime.UtcNow,
                    Subscriptions = new List<DeviceSubscription>
                    {
                        new DeviceSubscription
                        {
                            TopicName = "default",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        }
                    }
                };
                _contextAccessor.TenantDbContext.RegisteredDevices.Add(newDevice);
            }

            await _contextAccessor.TenantDbContext.SaveChangesAsync();
            return Ok(new { message = "Device registered successfully" });
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscriptionRequest request)
        {
            var device = await _contextAccessor.TenantDbContext.RegisteredDevices
                .Include(d => d.Subscriptions)
                .FirstOrDefaultAsync(d => d.DeviceToken == request.DeviceToken);

            if (device == null)
            {
                return NotFound("Device not found");
            }

            var subscription = device.Subscriptions.FirstOrDefault(s => s.TopicName == request.TopicName);
            if (subscription != null)
            {
                subscription.IsActive = true;
                subscription.ModifiedAt = DateTime.UtcNow;
            }
            else
            {
                device.Subscriptions.Add(new DeviceSubscription
                {
                    TopicName = request.TopicName,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _contextAccessor.TenantDbContext.SaveChangesAsync();
            return Ok(new { message = "Subscribed successfully" });
        }

        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] SubscriptionRequest request)
        {
            var device = await _contextAccessor.TenantDbContext.RegisteredDevices
                .Include(d => d.Subscriptions)
                .FirstOrDefaultAsync(d => d.DeviceToken == request.DeviceToken);

            if (device == null)
            {
                return NotFound("Device not found");
            }

            var subscription = device.Subscriptions.FirstOrDefault(s => s.TopicName == request.TopicName);
            if (subscription != null)
            {
                subscription.IsActive = false;
                subscription.ModifiedAt = DateTime.UtcNow;
                await _contextAccessor.TenantDbContext.SaveChangesAsync();
                return Ok(new { message = "Unsubscribed successfully" });
            }

            return NotFound("Subscription not found");
        }
    }

    public class DeviceRegistrationRequest
    {
        public string DeviceToken { get; set; }
        public string DeviceType { get; set; }
    }

    public class SubscriptionRequest
    {
        public string DeviceToken { get; set; }
        public string TopicName { get; set; }
    }
}