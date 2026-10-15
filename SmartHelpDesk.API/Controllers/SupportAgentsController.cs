using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartHelpdesk.Common.DTOs.SupportAgents;
using SmartHelpdesk.DAL.Entities;

namespace SmartHelpdesk.API.Controllers;

#region Controller

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdministratorOnly")]
public sealed class SupportAgentsController : ControllerBase
{
    #region Fields

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    #endregion

    #region Ctor

    public SupportAgentsController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    #endregion

    #region Get All Support Agents

    [HttpGet]
    public async Task<IActionResult> GetAllAsync()
    {
        var agents = await _userManager.GetUsersInRoleAsync("SupportAgent");

        var result = agents
            .Where(user => user.IsActive)
            .Select(user => new
            {
                user.Id,
                user.SupportAgentCode,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.IsActive,
                user.CreatedAt
            })
            .ToList();

        return Ok(result);
    }

    #endregion

    #region Get Support Agent By Email

    [HttpGet("by-email")]
    public async Task<IActionResult> GetByEmailAsync(
        [FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new
            {
                message = "Email is required."
            });
        }

        var user = await _userManager.FindByEmailAsync(email.Trim());

        if (user is null ||
            !await _userManager.IsInRoleAsync(user, "SupportAgent"))
        {
            return NotFound(new
            {
                message = "Support agent not found."
            });
        }

        return Ok(new
        {
            user.Id,
            user.SupportAgentCode,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.IsActive,
            user.CreatedAt
        });
    }

    #endregion

    #region Create Support Agent

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateSupportAgentDto dto)
    {
        var roleExists = await _roleManager.RoleExistsAsync("SupportAgent");

        if (!roleExists)
        {
            return StatusCode(500, new
            {
                message = "SupportAgent role is missing. Run database seeding first."
            });
        }

        var email = dto.Email.Trim();

        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            return Conflict(new
            {
                message = "A user with this email already exists. Use the promote API if this user is an existing Employee."
            });
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = dto.FullName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber)
                ? null
                : dto.PhoneNumber.Trim(),
            SupportAgentCode = await GenerateNextSupportAgentCodeAsync(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = false
        };

        var createResult = await _userManager.CreateAsync(
            user,
            dto.Password);

        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                message = "Unable to create support agent.",
                errors = createResult.Errors.Select(
                    error => error.Description)
            });
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            "SupportAgent");

        if (!roleResult.Succeeded)
        {
            var deleteResult = await _userManager.DeleteAsync(user);

            return BadRequest(new
            {
                message = "Unable to assign the SupportAgent role.",
                errors = roleResult.Errors.Select(
                    error => error.Description),
                rollbackSucceeded = deleteResult.Succeeded
            });
        }

        return Created(
            $"/api/SupportAgents/by-email?email={Uri.EscapeDataString(email)}",
            new
            {
                user.Id,
                user.SupportAgentCode,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.IsActive,
                user.CreatedAt
            });
    }

    #endregion

    #region Update Support Agent

    [HttpPut("by-email")]
    public async Task<IActionResult> UpdateAsync(
        [FromQuery] string email,
        [FromBody] UpdateSupportAgentDto dto)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new
            {
                message = "Current email is required."
            });
        }

        var user = await _userManager.FindByEmailAsync(email.Trim());

        if (user is null ||
            !await _userManager.IsInRoleAsync(user, "SupportAgent"))
        {
            return NotFound(new
            {
                message = "Support agent not found."
            });
        }

        var newEmail = dto.Email.Trim();

        var existingUser = await _userManager.FindByEmailAsync(newEmail);

        if (existingUser is not null && existingUser.Id != user.Id)
        {
            return Conflict(new
            {
                message = "Another user already uses this email."
            });
        }

        user.FullName = dto.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber)
            ? null
            : dto.PhoneNumber.Trim();

        var usernameResult = await _userManager.SetUserNameAsync(
            user,
            newEmail);

        if (!usernameResult.Succeeded)
        {
            return BadRequest(new
            {
                message = "Unable to update username.",
                errors = usernameResult.Errors.Select(
                    error => error.Description)
            });
        }

        var emailResult = await _userManager.SetEmailAsync(user, newEmail);

        if (!emailResult.Succeeded)
        {
            return BadRequest(new
            {
                message = "Unable to update email.",
                errors = emailResult.Errors.Select(
                    error => error.Description)
            });
        }

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            return BadRequest(new
            {
                message = "Unable to update support agent.",
                errors = updateResult.Errors.Select(
                    error => error.Description)
            });
        }

        return Ok(new
        {
            user.Id,
            user.SupportAgentCode,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.IsActive,
            user.CreatedAt
        });
    }

    #endregion

    #region Deactivate Support Agent

    [HttpPut("deactivate")]
    public async Task<IActionResult> DeactivateAsync(
        [FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new
            {
                message = "Email is required."
            });
        }

        var user = await _userManager.FindByEmailAsync(email.Trim());

        if (user is null ||
            !await _userManager.IsInRoleAsync(user, "SupportAgent"))
        {
            return NotFound(new
            {
                message = "Support agent not found."
            });
        }

        if (!user.IsActive)
        {
            return Ok(new
            {
                message = "Support agent is already inactive.",
                user.SupportAgentCode,
                user.Email,
                user.IsActive
            });
        }

        user.IsActive = false;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Unable to deactivate support agent.",
                errors = result.Errors.Select(
                    error => error.Description)
            });
        }

        return Ok(new
        {
            message = "Support agent deactivated successfully.",
            user.SupportAgentCode,
            user.FullName,
            user.Email,
            user.IsActive
        });
    }

    #endregion

    #region Activate Support Agent

    [HttpPut("activate")]
    public async Task<IActionResult> ActivateAsync(
        [FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new
            {
                message = "Email is required."
            });
        }

        var user = await _userManager.FindByEmailAsync(email.Trim());

        if (user is null ||
            !await _userManager.IsInRoleAsync(user, "SupportAgent"))
        {
            return NotFound(new
            {
                message = "Support agent not found."
            });
        }

        if (user.IsActive)
        {
            return Ok(new
            {
                message = "Support agent is already active.",
                user.SupportAgentCode,
                user.Email,
                user.IsActive
            });
        }

        user.IsActive = true;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Unable to reactivate support agent.",
                errors = result.Errors.Select(
                    error => error.Description)
            });
        }

        return Ok(new
        {
            message = "Support agent reactivated successfully.",
            user.SupportAgentCode,
            user.FullName,
            user.Email,
            user.IsActive
        });
    }

    #endregion

    #region Promote Employee To Support Agent

    [HttpPut("promote")]
    public async Task<IActionResult> PromoteToSupportAgentAsync(
        [FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new
            {
                message = "Email is required."
            });
        }

        var user = await _userManager.FindByEmailAsync(email.Trim());

        if (user is null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        if (!user.IsActive)
        {
            return BadRequest(new
            {
                message = "Cannot promote an inactive user. Activate the user first."
            });
        }

        if (await _userManager.IsInRoleAsync(user, "SupportAgent"))
        {
            return Conflict(new
            {
                message = "User is already a SupportAgent.",
                user.SupportAgentCode,
                user.Email
            });
        }

        if (!await _userManager.IsInRoleAsync(user, "Employee"))
        {
            return BadRequest(new
            {
                message = "Only users with the Employee role can be promoted."
            });
        }

        if (!await _roleManager.RoleExistsAsync("SupportAgent"))
        {
            return StatusCode(500, new
            {
                message = "SupportAgent role is missing. Run database seeding first."
            });
        }

        var oldCode = user.SupportAgentCode;
        var newCode = string.IsNullOrWhiteSpace(oldCode)
            ? await GenerateNextSupportAgentCodeAsync()
            : oldCode;

        var removeResult = await _userManager.RemoveFromRoleAsync(
            user,
            "Employee");

        if (!removeResult.Succeeded)
        {
            return BadRequest(new
            {
                message = "Unable to remove the Employee role.",
                errors = removeResult.Errors.Select(
                    error => error.Description)
            });
        }

        var addResult = await _userManager.AddToRoleAsync(
            user,
            "SupportAgent");

        if (!addResult.Succeeded)
        {
            var rollbackResult = await _userManager.AddToRoleAsync(
                user,
                "Employee");

            return BadRequest(new
            {
                message = "Unable to promote user to SupportAgent.",
                errors = addResult.Errors.Select(
                    error => error.Description),
                rollbackSucceeded = rollbackResult.Succeeded
            });
        }

        user.SupportAgentCode = newCode;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var removeAgentRoleResult =
                await _userManager.RemoveFromRoleAsync(
                    user,
                    "SupportAgent");

            var restoreEmployeeRoleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    "Employee");

            user.SupportAgentCode = oldCode;

            return StatusCode(500, new
            {
                message = "Unable to save support agent details.",
                errors = updateResult.Errors.Select(
                    error => error.Description),
                rollbackSucceeded =
                    removeAgentRoleResult.Succeeded &&
                    restoreEmployeeRoleResult.Succeeded
            });
        }

        return Ok(new
        {
            message = "Employee promoted to SupportAgent successfully.",
            user.SupportAgentCode,
            user.FullName,
            user.Email,
            user.IsActive,
            role = "SupportAgent"
        });
    }

    #endregion

    #region Generate Support Agent Code

    private async Task<string> GenerateNextSupportAgentCodeAsync()
    {
        var agents = await _userManager.GetUsersInRoleAsync("SupportAgent");

        var highestNumber = agents
            .Select(agent => agent.SupportAgentCode)
            .Where(code =>
                !string.IsNullOrWhiteSpace(code) &&
                code.StartsWith("#SP", StringComparison.OrdinalIgnoreCase))
            .Select(code =>
            {
                var numberPart = code![3..];

                return int.TryParse(numberPart, out var number)
                    ? number
                    : 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        return $"#SP{highestNumber + 1}";
    }

    #endregion
}

#endregion