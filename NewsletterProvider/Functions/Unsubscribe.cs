using Data.Contexts;
using Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Messaging.ServiceBus;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NewsletterProvider.Functions
{
    public class Unsubscribe
    {
        private readonly ILogger<Unsubscribe> _logger;
        private readonly DataContext _context;
        private readonly ServiceBusClient _serviceBusClient;

        public Unsubscribe(ILogger<Unsubscribe> logger, DataContext context, ServiceBusClient serviceBusClient)
        {
            _logger = logger;
            _context = context;
            _serviceBusClient = serviceBusClient;
        }

        [Function("Unsubscribe")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            if (!string.IsNullOrEmpty(body))
            {
                var subscribeEntity = JsonConvert.DeserializeObject<SubsribeEntity>(body);
                if (subscribeEntity != null)
                {
                    var existingSubscriber = await _context.Subscribers.FirstOrDefaultAsync(x => x.Email == subscribeEntity.Email);
                    if (existingSubscriber != null)
                    {
                        _context.Remove(existingSubscriber);
                        await _context.SaveChangesAsync();
                        await SendUnsubscribeEmail(subscribeEntity.Email);
                        return new OkObjectResult(new { Status = 200, Message = "Subscriber Unsubscribed" });
                    }
                    else
                    {
                        return new NotFoundObjectResult(new { Status = 404, Message = "Subscriber not found" });
                    }
                }
            }
            return new BadRequestObjectResult(new { Status = 400, Message = "Unable to Unsubscribe" });
        }

        private async Task SendUnsubscribeEmail(string email)
        {
            var emailContent = new
            {
                RecipientAddress = email,
                Subject = "Unsubscription Confirmation",
                HtmlContent = "<html><p>You have been unsubscribed from our newsletters.</p></html>",
                PlainTextContent = "You have been unsubscribed from our newsletters."
            };

            var messageBody = JsonConvert.SerializeObject(emailContent);
            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBody));

            try
            {
                ServiceBusSender sender = _serviceBusClient.CreateSender("email_request");
                await sender.SendMessageAsync(message);
                _logger.LogInformation($"Unsubscribe confirmation email sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send unsubscribe confirmation email: {ex.Message}");
                _logger.LogError($"Stack Trace: {ex.StackTrace}");
            }
        }
    }
}
