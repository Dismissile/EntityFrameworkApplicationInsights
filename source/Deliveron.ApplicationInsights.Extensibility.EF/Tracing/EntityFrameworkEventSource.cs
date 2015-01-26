using System.Diagnostics.Tracing;

namespace Deliveron.ApplicationInsights.Extensibility.EF.Tracing
{
    [EventSource(Name = "Deliveron-ApplicationInsights-Extensibility-EF")]
    internal sealed class EntityFrameworkEventSource : EventSource
    {
        public static readonly EntityFrameworkEventSource Log = new EntityFrameworkEventSource();

        private EntityFrameworkEventSource()
        {
        }

        [Event(10, Level = EventLevel.Error, Message = "EntityFrameworkQueryThresholdModule failed at initialization with exception: {0}")]
        public void QueryModuleInitializationExceptionEvent(string exceptionMessage)
        {
            this.WriteEvent(10, exceptionMessage);
        }

        [Event(20, Level = EventLevel.Error, Message = "EntityFrameworkQueryThresholdModule failed at disposal with exception: {0}")]
        public void QueryModuleDisposalExceptionEvent(string exceptionMessage)
        {
            this.WriteEvent(20, exceptionMessage);
        }

        [Event(30, Level = EventLevel.Informational, Message = "QueryThresholdCommandInterceptor completed command {0} after {1} milliseconds")]
        public void QueryModuleExecutedEvent(string commandText, long elapsedMilliseconds)
        {
            this.WriteEvent(30, commandText, elapsedMilliseconds);
        }
    }
}
