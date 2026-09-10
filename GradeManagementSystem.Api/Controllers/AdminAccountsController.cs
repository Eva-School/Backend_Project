using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using GradeManagementSystem.Core.DTOs.Admin.Account;
using GradeManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/admin/accounts")]
    [Authorize(Roles = "Admin")]
    public class AdminAccountsController : ControllerBase
    {
        private readonly IAdminAccountService _accountService;

        public AdminAccountsController(IAdminAccountService accountService)
        {
            _accountService = accountService;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out var id))
            {
                return id;
            }
            throw new UnauthorizedAccessException("Current user ID could not be resolved from authentication claims.");
        }

        private string? GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        // GET: /api/admin/accounts
        [HttpGet]
        public async Task<IActionResult> GetAccounts([FromQuery] AccountListQueryDto query, CancellationToken cancellationToken)
        {
            var result = await _accountService.GetAccountsAsync(query, cancellationToken);
            return Ok(result);
        }

        // GET: /api/admin/accounts/roles
        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
        {
            var roles = await _accountService.GetRolesAsync(cancellationToken);
            return Ok(roles);
        }

        // GET: /api/admin/accounts/{id:int}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAccountById(int id, CancellationToken cancellationToken)
        {
            var account = await _accountService.GetAccountByIdAsync(id, cancellationToken);
            if (account == null)
            {
                return NotFound(new { message = $"Account with ID {id} was not found." });
            }
            return Ok(account);
        }

        // POST: /api/admin/accounts
        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _accountService.CreateAccountAsync(request, currentUserId, GetClientIpAddress(), cancellationToken);

                // If an initial password was generated, prevent proxy/browser caching of the response
                if (!string.IsNullOrEmpty(result.GeneratedInitialPassword))
                {
                    Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
                    Response.Headers["Pragma"] = "no-cache";
                }

                return CreatedAtAction(nameof(GetAccountById), new { id = result.Account.UserId }, result);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already taken") || ex.Message.Contains("already registered"))
            {
                return Conflict(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: /api/admin/accounts/{id:int}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateAccount(int id, [FromBody] UpdateAccountProfileDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _accountService.UpdateAccountAsync(id, request, currentUserId, GetClientIpAddress(), cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already registered"))
            {
                return Conflict(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PATCH: /api/admin/accounts/{id:int}/role
        [HttpPatch("{id:int}/role")]
        public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeUserRoleDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                await _accountService.ChangeRoleAsync(id, request, currentUserId, GetClientIpAddress(), cancellationToken);
                return Ok(new { message = "Role updated successfully. Active sessions have been invalidated." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("cannot remove the Admin role from their own account"))
            {
                return StatusCode(403, new { message = ex.Message, code = "SELF_DEMOTION_FORBIDDEN" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PATCH: /api/admin/accounts/{id:int}/status
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> SetStatus(int id, [FromBody] SetAccountStatusDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                await _accountService.SetStatusAsync(id, request.IsActive, currentUserId, GetClientIpAddress(), cancellationToken);
                return Ok(new { message = $"Account status set to {(request.IsActive ? "Active" : "Inactive")}." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("cannot deactivate their own account"))
            {
                return StatusCode(403, new { message = ex.Message, code = "SELF_DEACTIVATION_FORBIDDEN" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: /api/admin/accounts/{id:int}/reset-password
        [HttpPost("{id:int}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                await _accountService.ResetPasswordAsync(id, request, currentUserId, GetClientIpAddress(), cancellationToken);
                return Ok(new { message = "Password reset successfully. Active sessions have been invalidated." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while resetting the password: " + ex.Message });
            }
        }
    }
}
