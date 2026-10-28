using System.Security.Claims;
using Application.DTOs.Event;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        int page = 1, int pageSize = 10)
    {
        var result = await _eventService.GetAllAsync(page, pageSize);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _eventService.GetByIdAsync(id);

        if (!result.IsSuccess)
            return NotFound(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    [HttpPost]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<IActionResult> Create(CreateEventDto dto)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var result = await _eventService.CreateAsync(dto, userId);

        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                Application.ApiResult.ErrorType.Validation =>
                    BadRequest(new { error = result.ErrorMessage }),
                Application.ApiResult.ErrorType.NotFound =>
                    NotFound(new { error = result.ErrorMessage }),
                _ => BadRequest(new { error = result.ErrorMessage })
            };
        }

        return Ok(result.Data);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organizer,Admin")]
    public async Task<IActionResult> Update(
        int id, UpdateEventDto dto)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var isAdmin = User.IsInRole("Admin");

        var result = await _eventService.UpdateAsync(
            id, dto, userId, isAdmin);

        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                Application.ApiResult.ErrorType.Validation =>
                    BadRequest(new { error = result.ErrorMessage }),
                Application.ApiResult.ErrorType.NotFound =>
                    NotFound(new { error = result.ErrorMessage }),
                Application.ApiResult.ErrorType.Forbidden =>
                    Forbid(),
                _ => BadRequest(new { error = result.ErrorMessage })
            };
        }

        return Ok(result.Data);
    }
}
