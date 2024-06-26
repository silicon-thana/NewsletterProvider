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
    public class Subscribe
    {
        private readonly ILogger<Subscribe> _logger;
        private readonly DataContext _context;
        private readonly ISendEmailService _emailService;

        public Subscribe(ILogger<Subscribe> logger, DataContext context, ISendEmailService emailService)
        {
            _logger = logger;
            _context = context;
            _emailService = emailService;
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
                    var subscribeEntity = JsonConvert.DeserializeObject<SubsribeEntity>(body);
                    if (subscribeEntity != null)
                    {
                        var existingSubscriber = await _context.Subscribers.FirstOrDefaultAsync(x => x.Email == subscribeEntity.Email);
                        if (existingSubscriber != null)
                        {
                            _context.Entry(existingSubscriber).CurrentValues.SetValues(subscribeEntity);
                            await _context.SaveChangesAsync();
                            await _emailService.SendEmailRequest(subscribeEntity);
                            return new OkObjectResult(new { Status = 200, Message = "Subscriber Updated" });
                        }

                        _context.Subscribers.Add(subscribeEntity);
                        await _context.SaveChangesAsync();
                        await _emailService.SendEmailRequest(subscribeEntity);
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
    }
}
