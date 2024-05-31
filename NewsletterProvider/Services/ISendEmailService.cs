using Data.Entities;

namespace NewsletterProvider.Services
{
    public interface ISendEmailService
    {
        Task SendEmailRequest(SubsribeEntity subscribeEntity);
    }
}