namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Features;
    using MessageInterfaces;
    using Pipeline;
    using Serialization;
    using Settings;
    using Unicast.Messages;

    class SerializationFeature : Feature
    {
        public SerializationFeature()
        {
            EnableByDefault();
        }

        protected internal sealed override void Setup(FeatureConfigurationContext context)
        {
            var settings = context.Settings;
            var mapper = settings.Get<IMessageMapper>();
            var messageMetadataRegistry = settings.Get<MessageMetadataRegistry>();
            var logicalMessageFactory = settings.Get<LogicalMessageFactory>();
            var defaultSerializerAndDefinition = settings.GetMainSerializer();

            var defaultSerializer = CreateMessageSerializer(defaultSerializerAndDefinition, mapper, settings);

            var additionalDeserializerDefinitions = context.Settings.GetAdditionalSerializers();
            var additionalDeserializers = new List<IMessageSerializer>();

            var additionalDeserializerDiagnostics = new List<object>();
            foreach (var definitionAndSettings in additionalDeserializerDefinitions)
            {
                var deserializer = CreateMessageSerializer(definitionAndSettings, mapper, settings);
                additionalDeserializers.Add(deserializer);

                var deserializerType = definitionAndSettings.Item1.GetType();

                additionalDeserializerDiagnostics.Add(new
                {
                    Type = deserializerType.FullName,
                    Version = FileVersionRetriever.GetFileVersion(deserializerType),
                    deserializer.ContentType
                });
            }

            var resolver = new MessageDeserializerResolver(defaultSerializer, additionalDeserializers);

            context.Pipeline.Register(new DeserializeLogicalMessagesConnector(resolver, logicalMessageFactory, messageMetadataRegistry, mapper), "Deserializes the physical message body into logical messages");
            context.Pipeline.Register(new SerializeMessageConnector(defaultSerializer, messageMetadataRegistry), "Converts a logical message into a physical message");

            context.Settings.AddStartupDiagnosticsSection("Serialization", new
            {
                DefaultSerializer = new
                {
                    Type = defaultSerializerAndDefinition.Item1.GetType().FullName,
                    Version = FileVersionRetriever.GetFileVersion(defaultSerializerAndDefinition.Item1.GetType()),
                    defaultSerializer.ContentType
                },
                AdditionalDeserializers = additionalDeserializerDiagnostics
            });
        }

        static IMessageSerializer CreateMessageSerializer(Tuple<SerializationDefinition, SettingsHolder> definitionAndSettings, IMessageMapper mapper, ReadOnlySettings mainSettings)
        {
            var definition = definitionAndSettings.Item1;
            var deserializerSettings = definitionAndSettings.Item2;
            deserializerSettings.Merge(mainSettings);
            deserializerSettings.PreventChanges();

            var serializerFactory = definition.Configure(deserializerSettings);
            var serializer = serializerFactory(mapper);
            return serializer;
        }
    }
}