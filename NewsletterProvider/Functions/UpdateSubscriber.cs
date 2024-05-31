using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Data.Entities;
using Data.Contexts;
using NewsletterProvider.Services;

namespace NewsletterProvider.Functions
{
    public class UpdateSubscriber
    {
        private readonly ILogger<UpdateSubscriber> _logger;
        private readonly DataContext _context;
        private readonly ISendEmailService _emailService;

        public UpdateSubscriber(ILogger<UpdateSubscriber> logger, DataContext context, ISendEmailService emailService)
        {
            _logger = logger;
            _context = context;
            _emailService = emailService;
        }

        [Function("UpdateSubscriber")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "put", Route = "UpdateSubscriber/{email}")] HttpRequest req, string email)
        {
            _logger.LogInformation("UpdateSubscriber function started.");

            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation($"Request body: {body}");

                if (!string.IsNullOrEmpty(body))
                {
                    var updatedSubscriber = JsonConvert.DeserializeObject<SubsribeEntity>(body);
                    if (updatedSubscriber != null)
                    {
                        var existingSubscriber = await _context.Subscribers.FirstOrDefaultAsync(x => x.Email == email);
                        if (existingSubscriber != null)
                        {
                            existingSubscriber.DailyNewsletter = updatedSubscriber.DailyNewsletter;
                            existingSubscriber.AdvertisingUpdates = updatedSubscriber.AdvertisingUpdates;
                            existingSubscriber.WeekinReview = updatedSubscriber.WeekinReview;
                            existingSubscriber.EventUpdates = updatedSubscriber.EventUpdates;
                            existingSubscriber.StartupWeekly = updatedSubscriber.StartupWeekly;
                            existingSubscriber.Podcasts = updatedSubscriber.Podcasts;

                            await _context.SaveChangesAsync();
                            await _emailService.SendEmailRequest(existingSubscriber);

                            return new OkObjectResult(new { Status = 200, Message = "Subscriber Updated" });
                        }

                        return new NotFoundObjectResult(new { Status = 404, Message = "Subscriber not found" });
                    }
                }
                return new BadRequestObjectResult(new { Status = 400, Message = "Invalid request" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR : UpdateSubscriber.Run :: {ex.Message}");
                _logger.LogError($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner Exception: {ex.InnerException.Message}");
                    _logger.LogError($"Inner Exception Stack Trace: {ex.InnerException.StackTrace}");
                }
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
