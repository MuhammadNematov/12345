
using Application.ApiResult;
using Application.DTOs.Booking;
using Application.Interfaces;
using Domain.Enums;
using Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _context;
    private readonly IMessagePublisher _publisher;

    public BookingService(
        AppDbContext context,
        IMessagePublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task<Result<BookingDto>> CreateAsync(
        CreateBookingDto dto, int userId)
    {
        if (dto.Seats < 1 || dto.Seats > 10)
            return Result<BookingDto>.Fail(
                "Можно забронировать от 1 до 10 мест.",
                ErrorType.Validation);

        var e = await _context.Events
            .FirstOrDefaultAsync(x => x.Id == dto.EventId);

        if (e == null)
            return Result<BookingDto>.Fail(
                "Мероприятие не найдено.", ErrorType.NotFound);

        if (e.StartsAt <= DateTime.UtcNow)
            return Result<BookingDto>.Fail(
                "Мероприятие уже началось.", ErrorType.Conflict);

        if (e.OrganizerId == userId)
            return Result<BookingDto>.Fail(
                "Нельзя бронировать собственное мероприятие.",
                ErrorType.Forbidden);

        if (e.AvailableSeats < dto.Seats)
            return Result<BookingDto>.Fail(
                "Недостаточно свободных мест.", ErrorType.Conflict);

        e.AvailableSeats -= dto.Seats;

        var booking = new Domain.Entities.Booking
        {
            EventId = e.Id,
            UserId = userId,
            Seats = dto.Seats,
            TotalPrice = e.Price * dto.Seats,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        await _publisher.PublishAsync(new BookingMessage
        {
            BookingId = booking.Id,
            Type = "Created"
        });

        return await GetBookingDtoAsync(booking.Id);
    }

    public async Task<Result<PagedResult<BookingDto>>> GetMyAsync(
        int userId, int page, int pageSize, string? status)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 10, 100);

        var query = _context.Bookings
            .AsNoTracking()
            .Where(b => b.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<BookingStatus>(
                    status, true, out var parsedStatus))
                return Result<PagedResult<BookingDto>>.Fail(
                    "Неизвестный статус бронирования.",
                    ErrorType.Validation);

            query = query.Where(b => b.Status == parsedStatus);
        }

        var count = await query.CountAsync();

        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BookingDto
            {
                Id = b.Id,
                EventId = b.EventId,
                EventTitle = b.Event.Title,
                Seats = b.Seats,
                TotalPrice = b.TotalPrice,
                Status = b.Status.ToString(),
                CreatedAt = b.CreatedAt,
                ExpiresAt = b.ExpiresAt
            })
            .ToListAsync();

        return Result<PagedResult<BookingDto>>.Success(
            new PagedResult<BookingDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = count
            });
    }

    public async Task<Result<BookingDto>> PayAsync(
        int id, int userId)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
            return Result<BookingDto>.Fail(
                "Бронь не найдена.", ErrorType.NotFound);

        if (booking.UserId != userId)
            return Result<BookingDto>.Fail(
                "Это не ваша бронь.", ErrorType.Forbidden);

        if (booking.Status != BookingStatus.Pending ||
            booking.ExpiresAt <= DateTime.UtcNow)
            return Result<BookingDto>.Fail(
                "Бронь уже нельзя оплатить.", ErrorType.Conflict);

        booking.Status = BookingStatus.Confirmed;
        await _context.SaveChangesAsync();

        await _publisher.PublishAsync(new BookingMessage
        {
            BookingId = booking.Id,
            Type = "Confirmed"
        });

        return await GetBookingDtoAsync(booking.Id);
    }

    public async Task<Result<BookingDto>> CancelAsync(
        int id, int userId, bool isAdmin)
    {
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
            return Result<BookingDto>.Fail(
                "Бронь не найдена.", ErrorType.NotFound);

        if (booking.UserId != userId && !isAdmin)
            return Result<BookingDto>.Fail(
                "Нет доступа к этой брони.", ErrorType.Forbidden);

        if (booking.Status != BookingStatus.Pending &&
            booking.Status != BookingStatus.Confirmed)
            return Result<BookingDto>.Fail(
                "Эту бронь нельзя отменить.", ErrorType.Conflict);

        var e = await _context.Events
            .FirstAsync(x => x.Id == booking.EventId);

        e.AvailableSeats += booking.Seats;
        booking.Status = BookingStatus.Cancelled;

        await _context.SaveChangesAsync();

        return await GetBookingDtoAsync(booking.Id);
    }

    private async Task<Result<BookingDto>> GetBookingDtoAsync(int id)
    {
        var dto = await _context.Bookings
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BookingDto
            {
                Id = b.Id,
                EventId = b.EventId,
                EventTitle = b.Event.Title,
                Seats = b.Seats,
                TotalPrice = b.TotalPrice,
                Status = b.Status.ToString(),
                CreatedAt = b.CreatedAt,
                ExpiresAt = b.ExpiresAt
            })
            .FirstAsync();

        return Result<BookingDto>.Success(dto);
    }
}
