using MediAlert.Constants;
using MediAlert.DTOs.HealthProfile;
using MediAlert.Extensions;
using MediAlert.Services.HealthProfile.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediAlert.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Patient)]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class HealthProfileController : ControllerBase
{
    private readonly IHealthProfileService _healthProfileService;

    public HealthProfileController(IHealthProfileService healthProfileService)
    {
        _healthProfileService = healthProfileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetConditions(CancellationToken cancellationToken)
    {
        var patientId = User.GetUserId();
        var conditions = await _healthProfileService.GetConditionsAsync(patientId, cancellationToken);
        return Ok(conditions);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCondition(Guid id, CancellationToken cancellationToken)
    {
        var patientId = User.GetUserId();
        var condition = await _healthProfileService.GetConditionByIdAsync(patientId, id, cancellationToken);
        
        if (condition == null)
            return NotFound();

        return Ok(condition);
    }

    [HttpPost]
    public async Task<IActionResult> AddCondition([FromBody] CreateHealthConditionRequest request, CancellationToken cancellationToken)
    {
        var patientId = User.GetUserId();
        var created = await _healthProfileService.AddConditionAsync(patientId, request, cancellationToken);
        return CreatedAtAction(nameof(GetCondition), new { id = created.ConditionId }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCondition(Guid id, [FromBody] UpdateHealthConditionRequest request, CancellationToken cancellationToken)
    {
        var patientId = User.GetUserId();
        var updated = await _healthProfileService.UpdateConditionAsync(patientId, id, request, cancellationToken);
        
        if (updated == null)
            return NotFound();

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCondition(Guid id, CancellationToken cancellationToken)
    {
        var patientId = User.GetUserId();
        var success = await _healthProfileService.DeleteConditionAsync(patientId, id, cancellationToken);
        
        if (!success)
            return NotFound();

        return NoContent();
    }
}
