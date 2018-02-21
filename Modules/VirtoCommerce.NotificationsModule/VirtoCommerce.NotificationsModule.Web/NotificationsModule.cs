using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VirtoCommerce.NotificationsModule.Core.Abstractions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.NotificationsModule.Data.Services;
using VirtoCommerce.NotificationsModule.Notifications.NotificationTypes;
using VirtoCommerce.NotificationsModule.Notifications.Rendering;
using VirtoCommerce.NotificationsModule.Notifications.Senders;
using VirtoCommerce.NotificationsModule.Web.Infrastructure;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Modularity;

namespace VirtoCommerce.NotificationsModule.Web
{
    public class NotificationsModule : IModule
    {
        public ManifestModuleInfo ModuleInfo { get; set; }

        public void Initialize(IServiceCollection serviceCollection)
        {
            var snapshot = serviceCollection.BuildServiceProvider();
            var configuration = snapshot.GetService<IConfiguration>();
            serviceCollection.AddDbContext<NotificationDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("VirtoCommerce")));
            serviceCollection.AddTransient<INotificationRepository, NotificationRepositoryImpl>();
            serviceCollection.AddSingleton<Func<INotificationRepository>>(provider => () => provider.CreateScope().ServiceProvider.GetService<INotificationRepository>());
            serviceCollection.AddScoped<INotificationService, NotificationService>();
            serviceCollection.AddScoped<INotificationRegistrar, NotificationService>();
            serviceCollection.AddScoped<INotificationSearchService, NotificationSearchService>();
            serviceCollection.AddScoped<INotificationMessageService, NotificationMessageService>();
            serviceCollection.AddTransient<INotificationSender, NotificationSender>();
            serviceCollection.AddTransient<INotificationTemplateRender, LiquidTemplateRenderer>();
            serviceCollection.AddTransient<INotificationMessageSender, SmtpEmailNotificationMessageSender>();
        }

        public void PostInitialize(IServiceProvider serviceProvider)
        {
            AbstractTypeFactory<Notification>.RegisterType<EmailNotification>().MapToType<NotificationEntity>();
            AbstractTypeFactory<Notification>.RegisterType<SmsNotification>().MapToType<NotificationEntity>();
            AbstractTypeFactory<NotificationTemplate>.RegisterType<EmailNotificationTemplate>().MapToType<NotificationTemplateEntity>();
            AbstractTypeFactory<NotificationTemplate>.RegisterType<SmsNotificationTemplate>().MapToType<NotificationTemplateEntity>();

            var mvcJsonOptions = serviceProvider.GetService<IOptions<MvcJsonOptions>>();
            mvcJsonOptions.Value.SerializerSettings.Converters.Add(new PolymorphicNotificationJsonConverter());
            mvcJsonOptions.Value.SerializerSettings.Converters.Add(new PolymorphicNotificationTemplateJsonConverter());

            //Force migrations
            using (var serviceScope = serviceProvider.CreateScope())
            {
                var notificationDbContext = serviceScope.ServiceProvider.GetRequiredService<NotificationDbContext>();
                notificationDbContext.Database.Migrate();
                notificationDbContext.EnsureSeeded();
            }

            var notificationRegistrar = serviceProvider.GetService<INotificationRegistrar>();
            notificationRegistrar.RegisterNotification<RegistrationEmailNotification>();
            notificationRegistrar.RegisterNotification<ResetPasswordEmailNotification>();
            notificationRegistrar.RegisterNotification<TwoFactorEmailNotification>();
            notificationRegistrar.RegisterNotification<TwoFactorSmsNotification>();
            notificationRegistrar.RegisterNotification<ConfirmationEmailNotification>();
            notificationRegistrar.RegisterNotification<StoreDynamicEmailNotification>();
            notificationRegistrar.RegisterNotification<OrderCreateEmailNotification>();
            notificationRegistrar.RegisterNotification<OrderPaidEmailNotification>();
            notificationRegistrar.RegisterNotification<OrderSentEmailNotification>();
            notificationRegistrar.RegisterNotification<NewOrderStatusEmailNotification>();
            notificationRegistrar.RegisterNotification<CancelOrderEmailNotification>();
            notificationRegistrar.RegisterNotification<InvoiceEmailNotification>();
            notificationRegistrar.RegisterNotification<NewSubscriptionEmailNotification>();
            notificationRegistrar.RegisterNotification<SubscriptionCanceledEmailNotification>();
        }

        public void Uninstall()
        {
        }
    }
}
