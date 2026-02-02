namespace glnc_webpart.Services
{
    public record InvoiceOcrResult(bool Success, string? ClientName, double? Weight, string? ErrorMessage);

    public interface IInvoiceOcrService
    {
        Task<InvoiceOcrResult> ExtractFromImageAsync(Stream imageStream, string contentType, CancellationToken cancellationToken = default);
    }
}
