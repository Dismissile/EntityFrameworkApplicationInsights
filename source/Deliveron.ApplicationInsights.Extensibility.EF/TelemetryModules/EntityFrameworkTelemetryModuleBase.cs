using Microsoft.ApplicationInsights.Extensibility;
using System;

namespace Deliveron.ApplicationInsights.Extensibility.EF.TelemetryModules
{
    internal abstract class EntityFrameworkTelemetryModuleBase : ISupportConfiguration, IDisposable
    {
        public abstract void Initialize(TelemetryConfiguration configuration);
        public abstract void Dispose();
    }
}
