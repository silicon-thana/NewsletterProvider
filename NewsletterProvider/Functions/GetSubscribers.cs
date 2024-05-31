using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Entities;
using Microsoft.EntityFrameworkCore;
using Data.Contexts;

namespace NewsletterProvider.Functions
{
    public class GetSubscribers
    {
        private readonly ILogger<GetSubscribers> _logger;
        private readonly DataContext _context;

        public GetSubscribers(ILogger<GetSubscribers> logger, DataContext context)
        {
            _logger = logger;
            _context = context;
        }

        [Function("GetSubscribers")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var subscribers = await _context.Subscribers
                .Select(s => new
                {
                    s.Email,
                    s.DailyNewsletter,
                    s.AdvertisingUpdates,
                    s.WeekinReview,
                    s.EventUpdates,
                    s.StartupWeekly,
                    s.Podcasts
                })
                .ToListAsync();

            return new OkObjectResult(subscribers);
        }
    }
}
