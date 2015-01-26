using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Deliveron.ApplicationInsights.Extensibility.EF.Tracing;
using Microsoft.ApplicationInsights;

namespace Deliveron.ApplicationInsights.Extensibility.EF.TelemetryModules
{
    internal sealed class QueryThresholdCommandInterceptor : IDbCommandInterceptor
    {
        private readonly TelemetryClient _telemetryClient = new TelemetryClient();
        private readonly Stopwatch _stopWatch = new Stopwatch();
        private long _threshold;

        public QueryThresholdCommandInterceptor(long threshold)
        {
            _threshold = threshold > 0 ? threshold : 1000L;
        }

        public TelemetryClient Telemetry
        {
            get { return _telemetryClient; }
        }

        public Stopwatch Stopwatch
        {
            get { return _stopWatch; }
        }

        public long Threshold
        {
            get { return _threshold; }
        }

        public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            Executing(command, interceptionContext);
        }

        public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            Executed(command, interceptionContext);
        }

        public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            Executing(command, interceptionContext);
        }

        public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            Executed(command, interceptionContext);
        }

        public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            Executing(command, interceptionContext);
        }

        public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            Executed(command, interceptionContext);
        }

        private void Executing<TResult>(DbCommand command, DbCommandInterceptionContext<TResult> interceptionContext)
        {
            Stopwatch.Restart();
        }

        private void Executed<TResult>(DbCommand command, DbCommandInterceptionContext<TResult> interceptionContext)
        {
            Stopwatch.Stop();
            EntityFrameworkEventSource.Log.QueryModuleExecutedEvent(command.CommandText, Stopwatch.ElapsedMilliseconds);
            if (Stopwatch.ElapsedMilliseconds >= Threshold)
            {
                Telemetry.TrackEvent("Query Exceeded Threshold",
                    properties: this.CreateTelemetryProperties(command, interceptionContext),
                    metrics: this.CreateTelemetryMetrics(command, interceptionContext));
            }
        }

        private IDictionary<string, string> CreateTelemetryProperties<TResult>(DbCommand command, DbCommandInterceptionContext<TResult> interceptionContext)
        {
            var properties = new Dictionary<string, string>();

            properties.Add("Command", command.CommandText ?? "<null>");
            LogParameters(command, interceptionContext, properties);
            LogCommandStatus(command, interceptionContext, properties);
            properties.Add("Asynchronous", interceptionContext.IsAsync.ToString());

            return properties;
        }

        private IDictionary<string, double> CreateTelemetryMetrics<TResult>(DbCommand command, DbCommandInterceptionContext<TResult> interceptionContext)
        {
            var metrics = new Dictionary<string, double>();

            metrics.Add("Elapsed Milliseconds", Stopwatch.ElapsedMilliseconds);

            return metrics;
        }

        private static void LogCommandStatus<TResult>(DbCommand command, DbCommandInterceptionContext<TResult> interceptionContext, IDictionary<string, string> properties)
        {
            string commandStatus;
            if (interceptionContext.Exception != null)
                commandStatus = "Failed";
            else if (interceptionContext.TaskStatus.HasFlag(TaskStatus.Canceled))
                commandStatus = "Cancelled";
            else
                commandStatus = "Completed";

            properties.Add("Command Status", commandStatus);
        }

        private static void LogParameters<TResult>(DbCommand command, DbCommandInterceptionContext<TResult> interceptionContext, IDictionary<string, string> properties)
        {
            if (command.Parameters != null)
            {
                StringBuilder sb = new StringBuilder();

                foreach (var parameter in command.Parameters.OfType<DbParameter>())
                {
                    sb.Append(LogParameter(parameter));
                }

                if (sb.Length > 0)
                    properties.Add("Command Parameters", sb.ToString());
            }
        }
        private static string LogParameter(DbParameter parameter)
        {
            // -- Name: [Value] (Type = {}, Direction = {}, IsNullable = {}, Size = {}, Precision = {} Scale = {})
            var builder = new StringBuilder();
            builder.Append("-- ")
                .Append(parameter.ParameterName)
                .Append(": '")
                .Append((parameter.Value == null || parameter.Value == DBNull.Value) ? "null" : parameter.Value)
                .Append("' (Type = ")
                .Append(parameter.DbType);

            if (parameter.Direction != ParameterDirection.Input)
            {
                builder.Append(", Direction = ").Append(parameter.Direction);
            }

            if (!parameter.IsNullable)
            {
                builder.Append(", IsNullable = false");
            }

            if (parameter.Size != 0)
            {
                builder.Append(", Size = ").Append(parameter.Size);
            }

            if (((IDbDataParameter)parameter).Precision != 0)
            {
                builder.Append(", Precision = ").Append(((IDbDataParameter)parameter).Precision);
            }

            if (((IDbDataParameter)parameter).Scale != 0)
            {
                builder.Append(", Scale = ").Append(((IDbDataParameter)parameter).Scale);
            }

            builder.Append(")").Append(Environment.NewLine);

            return builder.ToString();
        }
    }
}
