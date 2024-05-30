using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Data.Contexts;
using Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace NewsletterProvider.Functions
{
    public class Subscribe
    {
        private readonly ILogger<Subscribe> _logger;
        private readonly DataContext _context;
        private readonly ServiceBusClient _serviceBusClient;

        public Subscribe(ILogger<Subscribe> logger, DataContext context, ServiceBusClient serviceBusClient)
        {
            _logger = logger;
            _context = context;
            _serviceBusClient = serviceBusClient;
        }

        [Function("Subscribe")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("Subscribe function started.");

            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation($"Request body: {body}");

                if (!string.IsNullOrEmpty(body))
                {
                    var subscribeEntity = JsonConvert.DeserializeObject<SubscribeEntity>(body);
                    if (subscribeEntity != null)
                    {
                        var existingSubscriber = await _context.Subscribers.FirstOrDefaultAsync(x => x.Email == subscribeEntity.Email);
                        if (existingSubscriber != null)
                        {
                            _context.Entry(existingSubscriber).CurrentValues.SetValues(subscribeEntity);
                            await _context.SaveChangesAsync();
                            await SendEmailRequest(subscribeEntity);
                            return new OkObjectResult(new { Status = 200, Message = "Subscriber Updated" });
                        }

                        _context.Subscribers.Add(subscribeEntity);
                        await _context.SaveChangesAsync();
                        await SendEmailRequest(subscribeEntity);
                        return new OkObjectResult(new { Status = 200, Message = "Subscriber added" });
                    }
                }
                return new BadRequestObjectResult(new { Status = 400, Message = "Unable to subscribe" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR : Subscribe.Run :: {ex.Message}");
                _logger.LogError($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner Exception: {ex.InnerException.Message}");
                    _logger.LogError($"Inner Exception Stack Trace: {ex.InnerException.StackTrace}");
                }
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private async Task SendEmailRequest(SubscribeEntity subscribeEntity)
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

            var emailContent = new
            {
                RecipientAddress = subscribeEntity.Email,
                Subject = "Subscription Confirmation",
                HtmlContent = subscriptionDetails.ToString(),
                PlainTextContent = "You are now subscribed to our newsletters."
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
