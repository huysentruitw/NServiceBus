namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Pipeline;
    using Transport;

    class MainPipelineExecutor : IPipelineExecutor
    {
        public MainPipelineExecutor(IBuilder builder, IEventAggregator eventAggregator, IPipelineCache pipelineCache, IPipeline<ITransportReceiveContext> mainPipeline, MessageComponent messageComponent)
        {
            this.mainPipeline = mainPipeline;
            this.pipelineCache = pipelineCache;
            this.builder = builder;
            this.eventAggregator = eventAggregator;
            this.messageComponent = messageComponent;
        }

        public async Task Invoke(MessageContext messageContext)
        {
            var pipelineStartedAt = DateTime.UtcNow;

            using (var childBuilder = builder.CreateChildBuilder())
            {
                var rootContext = new RootContext(childBuilder, pipelineCache, eventAggregator, messageComponent);

                var message = new IncomingMessage(messageContext.MessageId, messageContext.Headers, messageContext.Body);
                var context = new TransportReceiveContext(message, messageContext.TransportTransaction, messageContext.ReceiveCancellationTokenSource, rootContext);

                context.Extensions.Merge(messageContext.Extensions);

                await mainPipeline.Invoke(context).ConfigureAwait(false);

                await context.RaiseNotification(new ReceivePipelineCompleted(message, pipelineStartedAt, DateTime.UtcNow)).ConfigureAwait(false);
            }
        }

        IEventAggregator eventAggregator;
        IBuilder builder;
        IPipelineCache pipelineCache;
        IPipeline<ITransportReceiveContext> mainPipeline;
        MessageComponent messageComponent;
    }
}