
using Application.ApiResult;
using Application.DTOs.Booking;

namespace Application.Interfaces;

public interface IBookingService
{
    Task<Result<BookingDto>> CreateAsync(
        CreateBookingDto dto,
        int userId);

    Task<Result<PagedResult<BookingDto>>> GetMyAsync(
        int userId,
        int page,
        int pageSize,
        string? status);

    Task<Result<BookingDto>> PayAsync(int id, int userId);

    Task<Result<BookingDto>> CancelAsync(
        int id,
        int userId,
        bool isAdmin);
}
