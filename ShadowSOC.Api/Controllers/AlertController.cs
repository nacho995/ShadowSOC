using Microsoft.AspNetCore.Mvc;
using ShadowSOC.Shared.Models;
using ShadowSOC.Api.Services;
namespace ShadowSOC.Api.Controllers;


[ApiController]
[Route("api/alerts")]
public class AlertController : ControllerBase
{
    private RabbitMQService RabbitMq;

    public AlertController(RabbitMQService rabbitMq)
    {
        RabbitMq = rabbitMq;
    }

    [HttpPost]
    public async Task<IActionResult> PublishAlertAsync([FromBody] SecurityEvent securityEvent)
    {
        if (securityEvent == null)
        {
            return BadRequest();
        }
        securityEvent.Id = Guid.NewGuid();
        await RabbitMq.PublishAlertAsync(securityEvent);
        return Ok();
    }
}
