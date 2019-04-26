namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;
    using MessageInterfaces;
    using MessageInterfaces.MessageMapper.Reflection;
    using ObjectBuilder;
    using Pipeline;
    using Settings;
    using Unicast.Messages;

    class MessageComponent
    {
        public MessageComponent(IConfigureComponents container, SettingsHolder settings)
        {
            this.container = container;
            this.settings = settings;
        }

        public IMessageMapper MessageMapper { get; private set; }

        public LogicalMessageFactory LogicalMessageFactory { get; private set; }

        public void Initialize()
        {
            var conventions = settings.Get<Conventions>();
            messageMetadataRegistry = new MessageMetadataRegistry(conventions.IsMessageType);

            messageMetadataRegistry.RegisterMessageTypesFoundIn(settings.GetAvailableTypes());

            settings.Set(messageMetadataRegistry);

            var foundMessages = messageMetadataRegistry.GetAllMessages().ToList();

            MessageMapper = new MessageMapper();

            MessageMapper.Initialize(foundMessages.Select(m => m.MessageType));

            settings.Set(MessageMapper);

            LogicalMessageFactory = new LogicalMessageFactory(messageMetadataRegistry, MessageMapper);

            settings.Set(LogicalMessageFactory);

            //we need to keep this as long as we allow users to inject `IMessageCreator` to create interface messages
            container.ConfigureComponent(_ => MessageMapper, DependencyLifecycle.SingleInstance);

            ApplyContainerRegistrationsForBackwardsCompatibility();

            settings.AddStartupDiagnosticsSection("Messages", new
            {
                CustomConventionUsed = conventions.CustomMessageTypeConventionUsed,
                NumberOfMessagesFoundAtStartup = foundMessages.Count,
                Messages = foundMessages.Select(m => m.MessageType.FullName)
            });

            //todo: remove given that we already dump this into startup diagnostics?
            LogFoundMessages(foundMessages);
        }

        [ObsoleteEx(
            Message = "Metadata registry and message factory no longer available via DI. Use setting instead.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        void ApplyContainerRegistrationsForBackwardsCompatibility()
        {
            container.ConfigureComponent(_ => messageMetadataRegistry, DependencyLifecycle.SingleInstance);
            container.ConfigureComponent(_ => LogicalMessageFactory, DependencyLifecycle.SingleInstance);
        }
        
        MessageMetadataRegistry messageMetadataRegistry;
        readonly IConfigureComponents container;
        readonly SettingsHolder settings;
    }
}