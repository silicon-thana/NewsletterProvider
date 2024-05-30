    using Azure.Messaging.ServiceBus;
    using Data.Contexts;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    var host = new HostBuilder()
        .ConfigureFunctionsWebApplication()
        .ConfigureServices(services =>
        {
            services.AddApplicationInsightsTelemetryWorkerService();
            services.ConfigureFunctionsApplicationInsights();

            var sqlServerConnectionString = Environment.GetEnvironmentVariable("SqlServer");
            // Register the DbContext with the retrieved connection string
            services.AddDbContext<DataContext>(options =>
                options.UseSqlServer(sqlServerConnectionString));

            // Retrieve the Service Bus connection string from the environment variables
            var serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnection");

            // Register the ServiceBusClient with the retrieved connection string
            services.AddSingleton(new ServiceBusClient(serviceBusConnectionString));
        })
        .Build();

    host.Run();
