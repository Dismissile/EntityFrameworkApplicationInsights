using System;
using System.Configuration;
using System.Data.Entity.Infrastructure.Interception;
using Deliveron.ApplicationInsights.Extensibility.EF.Tracing;
using Microsoft.ApplicationInsights.Extensibility;

namespace Deliveron.ApplicationInsights.Extensibility.EF.TelemetryModules
{
    /// <summary>
    /// Captures Application Insights telemetry for Entity Framework queries that exceed a configurable threshold.
    /// </summary>
    internal sealed class EntityFrameworkQueryThresholdModule : EntityFrameworkTelemetryModuleBase
    {
        private readonly QueryThresholdCommandInterceptor interceptor;

        public EntityFrameworkQueryThresholdModule()
        {
            long queryThreshold;
            long.TryParse(ConfigurationManager.AppSettings["ai:EFQueryThreshold"], out queryThreshold);

            interceptor = new QueryThresholdCommandInterceptor(queryThreshold);
        }

        /// <summary>
        /// Initializes the module.
        /// </summary>
        /// <param name="configuration">The active telemetry configuration.</param>
        public override void Initialize(TelemetryConfiguration configuration)
        {
            try
            {
                DbInterception.Add(interceptor);
            }
            catch (Exception ex)
            {
                EntityFrameworkEventSource.Log.QueryModuleInitializationExceptionEvent(ex.Message);
            }
        }

        /// <summary>
        /// Disposes the module
        /// </summary>
        public override void Dispose()
        {
            try
            {
                DbInterception.Remove(interceptor);
            }
            catch (Exception ex)
            {
                EntityFrameworkEventSource.Log.QueryModuleDisposalExceptionEvent(ex.Message);
            }
        }
    }
}
