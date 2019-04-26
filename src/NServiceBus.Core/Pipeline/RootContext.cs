namespace NServiceBus
{
    using ObjectBuilder;

    class RootContext : BehaviorContext
    {
        public RootContext(IBuilder builder, IPipelineCache pipelineCache, IEventAggregator eventAggregator, MessageComponent messageComponent) : base(null)
        {
            Set(builder);
            Set(pipelineCache);
            Set(eventAggregator);
            Set(messageComponent.MessageMapper);
            Set(messageComponent.LogicalMessageFactory);
        }
    }
}