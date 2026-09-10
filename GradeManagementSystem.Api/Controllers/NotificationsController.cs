using System.Security.Claims;
using GradeManagementSystem.Core.DTOs.Notification;
using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Repository.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GradeManagementSystem.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly GradeDbContext _context;

    public NotificationsController(GradeDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        if (!TryGetUser(out var userId, out var role))
        {
            return Unauthorized(new { message = "Unauthenticated" });
        }

        var roleAliases = GetRoleAliases(role);

        var notifications = await _context.Notifications
            .AsNoTracking()
            .Where(item => item.TargetRole == null || roleAliases.Contains(item.TargetRole.ToLower()))
            .OrderByDescending(item => item.CreatedAt)
            .Take(50)
            .Select(item => new
            {
                id = item.NotificationID.ToString(),
                type = item.Type,
                title = item.Title,
                message = item.Message,
                timestamp = item.CreatedAt,
                read = item.Reads.Any(read => read.UserID == userId),
                priority = item.Priority
            })
            .ToListAsync();

        return Ok(new
        {
            notifications,
            unreadCount = notifications.Count(item => !item.read)
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Student Affairs,StudentAffairs")]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequestDto request)
    {
        if (!ModelState.IsValid || !TryGetUser(out var userId, out _))
        {
            return BadRequest(new { message = "A valid notification is required." });
        }

        if (!string.IsNullOrWhiteSpace(request.TargetRole))
        {
            var trimmed = request.TargetRole.Trim().ToLower();
            var roleExists = await _context.Roles.AnyAsync(item =>
                (item.RoleName != null && item.RoleName.ToLower() == trimmed) ||
                (item.Name != null && item.Name.ToLower() == trimmed));
            if (!roleExists && !GetRoleAliases(trimmed).Any(a => a is "student affairs" or "studentaffairs" or "vice" or "staff"))
            {
                return BadRequest(new { message = "Target role does not exist." });
            }
        }

        var notification = new AppNotification
        {
            Type = request.Type,
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            Priority = request.Priority,
            TargetRole = string.IsNullOrWhiteSpace(request.TargetRole) ? null : request.TargetRole.Trim(),
            CreatedByUserID = userId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetNotifications), new { id = notification.NotificationID }, new
        {
            id = notification.NotificationID.ToString(),
            type = notification.Type,
            title = notification.Title,
            message = notification.Message,
            timestamp = notification.CreatedAt,
            read = false,
            priority = notification.Priority
        });
    }

    [HttpPatch]
    public async Task<IActionResult> MarkRead([FromBody] UpdateNotificationReadRequestDto request)
    {
        if (!TryGetUser(out var userId, out var role))
        {
            return Unauthorized(new { message = "Unauthenticated" });
        }

        var roleAliases = GetRoleAliases(role);
        var query = _context.Notifications
            .Where(item => item.TargetRole == null || roleAliases.Contains(item.TargetRole.ToLower()));
        if (request.MarkAllRead)
        {
            var unreadIds = await query
                .Where(item => !item.Reads.Any(read => read.UserID == userId))
                .Select(item => item.NotificationID)
                .ToListAsync();
            _context.NotificationReads.AddRange(unreadIds.Select(notificationId => new AppNotificationRead
            {
                NotificationID = notificationId,
                UserID = userId,
                ReadAt = DateTime.UtcNow
            }));
            await _context.SaveChangesAsync();
            return NoContent();
        }

        if (!request.Id.HasValue || request.Id <= 0)
        {
            return BadRequest(new { message = "A notification id is required." });
        }

        var exists = await query.AnyAsync(item => item.NotificationID == request.Id.Value);
        if (!exists)
        {
            return NotFound(new { message = "Notification not found." });
        }

        var alreadyRead = await _context.NotificationReads
            .AnyAsync(item => item.NotificationID == request.Id.Value && item.UserID == userId);
        if (!alreadyRead)
        {
            _context.NotificationReads.Add(new AppNotificationRead
            {
                NotificationID = request.Id.Value,
                UserID = userId,
                ReadAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }

    private static string[] GetRoleAliases(string role)
    {
        var normalized = role.Trim().ToLowerInvariant();
        if (normalized is "student affairs" or "studentaffairs" or "vice" or "staff")
        {
            return new[] { "student affairs", "studentaffairs", "vice", "staff" };
        }
        return new[] { normalized };
    }

    private bool TryGetUser(out int userId, out string role)
    {
        userId = 0;
        role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdValue, out userId) && !string.IsNullOrWhiteSpace(role);
    }
}
