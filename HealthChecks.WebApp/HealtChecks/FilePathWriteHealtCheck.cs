using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HealthChecks.WebApp.HealtChecks
{
    public class FilePathWriteHealtCheck : IHealthCheck
    {
        private readonly string _path;
        private IReadOnlyDictionary<string, object> _healthChecksData;
        public FilePathWriteHealtCheck(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException($"'{nameof(path)}' cannot be null or empty.", nameof(path));
            }

            _path = path;
            _healthChecksData = new Dictionary<string, object>()
            {
                { "path", path }
            };
        }
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            string filePath = Path.Combine(_path, "HealtCheck.file");
            try
            {
                var file = File.Create(filePath);
                file.Close();
                File.Delete(filePath);
                return Task.FromResult(HealthCheckResult.Healthy());
            }
            catch (Exception ex)
            {
                switch (context.Registration.FailureStatus)
                {
                    case HealthStatus.Degraded:
                        return Task.FromResult(HealthCheckResult.Degraded($"Issue to writing to file path", ex, _healthChecksData));
                    case HealthStatus.Healthy:
                        return Task.FromResult(HealthCheckResult.Healthy($"Issue to writing to file path", _healthChecksData));
                    default:
                        return Task.FromResult(HealthCheckResult.Unhealthy($"Issue to writing to file path", ex, _healthChecksData));
                }
            }
        }
    }
}
