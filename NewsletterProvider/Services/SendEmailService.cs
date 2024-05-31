using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Data.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace NewsletterProvider.Services
{
    public class SendEmailService : ISendEmailService
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ILogger<SendEmailService> _logger;

        public SendEmailService(ServiceBusClient serviceBusClient, ILogger<SendEmailService> logger)
        {
            _serviceBusClient = serviceBusClient;
            _logger = logger;
        }

        public async Task SendEmailRequest(SubsribeEntity subscribeEntity)
        {
            string htmlContent;
            string plainTextContent;

            // Check if any subscription option is selected
            if (subscribeEntity.DailyNewsletter || subscribeEntity.AdvertisingUpdates || subscribeEntity.WeekinReview ||
                subscribeEntity.EventUpdates || subscribeEntity.StartupWeekly || subscribeEntity.Podcasts)
            {
                var subscriptionDetails = new StringBuilder();
                subscriptionDetails.Append("<html><p>You are now subscribed to:</p><ul>");

                if (subscribeEntity.DailyNewsletter)
                    subscriptionDetails.Append("<li>Daily Newsletter</li>");
                if (subscribeEntity.AdvertisingUpdates)
                    subscriptionDetails.Append("<li>Advertising Updates</li>");
                if (subscribeEntity.WeekinReview)
                    subscriptionDetails.Append("<li>Week in Review</li>");
                if (subscribeEntity.EventUpdates)
                    subscriptionDetails.Append("<li>Event Updates</li>");
                if (subscribeEntity.StartupWeekly)
                    subscriptionDetails.Append("<li>Startup Weekly</li>");
                if (subscribeEntity.Podcasts)
                    subscriptionDetails.Append("<li>Podcasts</li>");

                subscriptionDetails.Append("</ul></html>");

                htmlContent = subscriptionDetails.ToString();
                plainTextContent = "You are now subscribed to the selected newsletters.";
            }
            else
            {
                htmlContent = "<html><p>You are now subscribed to the newsletter!</p></html>";
                plainTextContent = "You are now subscribed to the newsletter!";
            }

            var emailContent = new
            {
                RecipientAddress = subscribeEntity.Email,
                Subject = "Subscription Confirmation",
                HtmlContent = htmlContent,
                PlainTextContent = plainTextContent
            };

            var messageBody = JsonConvert.SerializeObject(emailContent);
            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBody));

            try
            {
                ServiceBusSender sender = _serviceBusClient.CreateSender("email_request");
                await sender.SendMessageAsync(message);
                _logger.LogInformation($"Email request sent for {subscribeEntity.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send email request: {ex.Message}");
                _logger.LogError($"Stack Trace: {ex.StackTrace}");
            }
        }
    }
}
