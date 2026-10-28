using Application.ApiResult;
using Application.DTOs.Event;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class EventService : IEventService
{
    private readonly AppDbContext _context;

    public EventService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<EventDto>>> GetAllAsync(
        int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 10, 100);

        var query = _context.Events
            .AsNoTracking()
            .Where(e => e.StartsAt > DateTime.UtcNow);

        var totalCount = await query.CountAsync();

        var events = await query
            .OrderBy(e => e.StartsAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                Location = e.Location,
                StartsAt = e.StartsAt,
                Price = e.Price,
                TotalSeats = e.TotalSeats,
                AvailableSeats = e.AvailableSeats,
                CreatedAt = e.CreatedAt,
                CategoryId = e.CategoryId,
                CategoryName = _context.Categories
                    .Where(c => c.Id == e.CategoryId)
                    .Select(c => c.Name)
                    .FirstOrDefault() ?? "",                OrganizerId = e.OrganizerId
            })
            .ToListAsync();

        var result = new PagedResult<EventDto>
        {
            Items = events,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Result<PagedResult<EventDto>>.Success(result);
    }

    public async Task<Result<EventDto>> GetByIdAsync(int id)
    {
        var e = await _context.Events
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new EventDto
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                Location = x.Location,
                StartsAt = x.StartsAt,
                Price = x.Price,
                TotalSeats = x.TotalSeats,
                AvailableSeats = x.AvailableSeats,
                CreatedAt = x.CreatedAt,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.Name,
                OrganizerId = x.OrganizerId
            })
            .FirstOrDefaultAsync();

        if (e == null)
            return Result<EventDto>.Fail(
                "Мероприятие не найдено.", ErrorType.NotFound);

        return Result<EventDto>.Success(e);
    }

    public async Task<Result<EventDto>> CreateAsync(
        CreateEventDto dto, int organizerId)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) ||
            dto.Title.Trim().Length < 3 ||
            dto.Title.Trim().Length > 200 ||
            string.IsNullOrWhiteSpace(dto.Location) ||
            dto.Price < 0 ||
            dto.TotalSeats < 1 ||
            dto.TotalSeats > 100000 ||
            dto.StartsAt <= DateTime.UtcNow)
        {
            return Result<EventDto>.Fail(
                "Проверьте данные мероприятия.", ErrorType.Validation);
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == dto.CategoryId);

        if (category == null)
            return Result<EventDto>.Fail(
                "Категория не найдена.", ErrorType.NotFound);

        var e = new Domain.Entities.Event
        {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            Location = dto.Location.Trim(),
            StartsAt = dto.StartsAt,
            Price = dto.Price,
            TotalSeats = dto.TotalSeats,
            AvailableSeats = dto.TotalSeats,
            CreatedAt = DateTime.UtcNow,
            CategoryId = dto.CategoryId,
            OrganizerId = organizerId
        };

        _context.Events.Add(e);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(e.Id);
    }

    public async Task<Result<EventDto>> UpdateAsync(
        int id, UpdateEventDto dto, int userId, bool isAdmin)
    {
        var e = await _context.Events
            .FirstOrDefaultAsync(x => x.Id == id);

        if (e == null)
            return Result<EventDto>.Fail(
                "Мероприятие не найдено.", ErrorType.NotFound);

        if (!isAdmin && e.OrganizerId != userId)
            return Result<EventDto>.Fail(
                "Нет доступа к изменению этого мероприятия.",
                ErrorType.Forbidden);

        if (string.IsNullOrWhiteSpace(dto.Title) ||
            dto.Title.Trim().Length < 3 ||
            dto.Title.Trim().Length > 200 ||
            string.IsNullOrWhiteSpace(dto.Location) ||
            dto.Price < 0 ||
            dto.TotalSeats < 1 ||
            dto.TotalSeats > 100000 ||
            dto.StartsAt <= DateTime.UtcNow)
        {
            return Result<EventDto>.Fail(
                "Проверьте данные мероприятия.", ErrorType.Validation);
        }

        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == dto.CategoryId);

        if (!categoryExists)
            return Result<EventDto>.Fail(
                "Категория не найдена.", ErrorType.NotFound);

        var occupiedSeats = e.TotalSeats - e.AvailableSeats;

        if (dto.TotalSeats < occupiedSeats)
            return Result<EventDto>.Fail(
                "Нельзя уменьшить количество мест ниже уже занятого.",
                ErrorType.Validation);

        e.Title = dto.Title.Trim();
        e.Description = dto.Description?.Trim();
        e.Location = dto.Location.Trim();
        e.StartsAt = dto.StartsAt;
        e.Price = dto.Price;
        e.AvailableSeats = dto.TotalSeats - occupiedSeats;
        e.TotalSeats = dto.TotalSeats;
        e.CategoryId = dto.CategoryId;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(e.Id);
    }
}
