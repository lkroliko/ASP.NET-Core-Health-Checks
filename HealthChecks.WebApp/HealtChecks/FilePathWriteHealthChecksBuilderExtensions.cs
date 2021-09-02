using HealthChecks.WebApp.HealtChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class FilePathWriteHealthChecksBuilderExtensions
    {
        public static IHealthChecksBuilder AddFilePathWrite(this IHealthChecksBuilder builder, string path, HealthStatus? status = null, IEnumerable<string> tags = null, TimeSpan? timeout = null)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path can't be null or empty", nameof(path));
            return builder.AddCheck("File Path Writer Health Check", new FilePathWriteHealtCheck(path), status, tags, timeout);
        }
    }
}
