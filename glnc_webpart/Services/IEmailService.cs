namespace glnc_webpart.Services
{
    public interface IEmailService
    {
        Task SendDeliveryNotificationAsync(string toEmail, string subject, string body);
    }
}

