using MediAlert.DTOs.Profile;
using MediAlert.Models;
using MediAlert.Data;
using MediAlert.Constants;
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
    private readonly ApplicationDbContext _db;

    public ProfileController(UserManager<ApplicatioUser> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var profileDto = new UserProfileDto
        {
            FullName = user.FullName,
            Email = user.Email ?? "",
            PhoneNumber = user.PhoneNumber,
            Role = user.Role
        };

        if (user.Role == UserRoles.Doctor)
        {
            var doctor = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                _db.Doctors.Where(d => d.UserId == user.Id));
            if (doctor != null)
            {
                profileDto.LicenseNumber = doctor.LicenseNumber;
                profileDto.Specialization = doctor.Specialization;
                profileDto.Qualifications = doctor.Qualifications;
                profileDto.YearsOfExperience = doctor.YearsOfExperience;
                profileDto.ClinicName = doctor.ClinicName;
                profileDto.ContactInfo = doctor.ContactInfo;
                profileDto.Biography = doctor.Biography;
                profileDto.ProfilePhotoUrl = doctor.ProfilePhotoUrl;
            }
        }

        return Ok(profileDto);
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

        if (user.Role == UserRoles.Doctor)
        {
            var doctor = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                _db.Doctors.Where(d => d.UserId == user.Id));
            if (doctor != null)
            {
                if (request.LicenseNumber != null && request.LicenseNumber.Trim() != doctor.LicenseNumber)
                {
                    var licExists = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
                        _db.Doctors.Where(d => d.LicenseNumber == request.LicenseNumber.Trim() && d.DoctorId != doctor.DoctorId));
                    if (licExists)
                    {
                        return Conflict(new { Message = "License number already registered by another doctor." });
                    }
                    doctor.LicenseNumber = request.LicenseNumber.Trim();
                }

                doctor.Specialization = request.Specialization ?? doctor.Specialization;
                doctor.Qualifications = request.Qualifications ?? doctor.Qualifications;
                doctor.YearsOfExperience = request.YearsOfExperience ?? doctor.YearsOfExperience;
                doctor.ClinicName = request.ClinicName;
                doctor.ContactInfo = request.ContactInfo;
                doctor.Biography = request.Biography;
                doctor.ProfilePhotoUrl = request.ProfilePhotoUrl;
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Profile updated successfully." });
    }
}

