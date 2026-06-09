using MediAlert.DTOs.Profile;
using MediAlert.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MediAlert.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ProfileController : ControllerBase
{
    private readonly UserManager<ApplicatioUser> _userManager;

    public ProfileController(UserManager<ApplicatioUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        return Ok(new UserProfileDto
        {
            FullName = user.FullName,
            Email = user.Email ?? "",
            PhoneNumber = user.PhoneNumber,
            Role = user.Role
        });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;

        if (user.Email != request.Email)
        {
            var emailExists = await _userManager.FindByEmailAsync(request.Email);
            if (emailExists != null && emailExists.Id != user.Id)
            {
                return Conflict(new { Message = "Email is already taken by another user." });
            }
            user.Email = request.Email;
            user.UserName = request.Email;
        }

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new { Message = "Failed to update profile.", Errors = result.Errors.Select(e => e.Description) });
        }

        return Ok(new { Message = "Profile updated successfully." });
    }
}
