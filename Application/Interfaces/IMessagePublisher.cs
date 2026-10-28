
using Application.DTOs.Booking;

namespace Application.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync(
        BookingMessage message,
        CancellationToken cancellationToken = default);
}
