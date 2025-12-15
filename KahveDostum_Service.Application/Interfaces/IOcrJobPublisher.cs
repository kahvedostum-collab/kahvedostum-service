using KahveDostum_Service.Application.Dtos;

namespace KahveDostum_Service.Application.Interfaces;

public interface IOcrJobPublisher
{
    Task PublishAsync(OcrJobMessage msg, CancellationToken ct = default);
}