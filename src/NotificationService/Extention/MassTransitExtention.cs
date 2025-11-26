using MassTransit;
using NotificationService.Consumer;

namespace NotificationService.Extention
{
    public static class MassTransitExtention
    {
        public static IServiceCollection AddMassTransitConfiguration(this IServiceCollection services)
        {
            services.AddMassTransit(x =>
            {

                x.AddConsumer<PaymentSuccessConsumer>();
                
                x.AddConsumer<PointsNotificationConsumer>();

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseMessageRetry(r =>
                      r.Incremental(
                          retryLimit: 5,
                          initialInterval: TimeSpan.FromSeconds(1),
                          intervalIncrement: TimeSpan.FromSeconds(2)
                      )
  );
                    cfg.ReceiveEndpoint("notification-queue", e =>
                    {
                        e.ConfigureConsumer<PaymentSuccessConsumer>(context);
                    });

                 

                    cfg.ReceiveEndpoint("notification-points-queue", e =>
                    {
                        e.ConfigureConsumer<PointsNotificationConsumer>(context);
                    });
                });
            });
            return services;
        }
    }
}
