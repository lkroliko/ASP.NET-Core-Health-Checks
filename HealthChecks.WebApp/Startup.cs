using AspNetCoreRateLimit;
using HealthChecks.UI.Client;
using HealthChecks.WebApp.HealtChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthChecks.WebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

            services.AddMemoryCache();

            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
            //services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));
            services.AddInMemoryRateLimiting();

            services.AddControllersWithViews();

            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();   
             
            services.AddHealthChecks()
                .AddSqlServer(Configuration.GetConnectionString("bad"), name: "MsSql connection", tags: new[] { "ready" }, failureStatus: HealthStatus.Degraded)
                .AddUrlGroup(new Uri("https://jsonplaceholder.typicode.com/posts/1"), "Url Group Health Check", HealthStatus.Degraded, new[] { "ready" })
                .AddFilePathWrite("d:\\", HealthStatus.Degraded, new[] { "ready" });

            services.AddHealthChecksUI()
                .AddInMemoryStorage();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseIpRateLimiting();
             
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions()
                {
                    ResultStatusCodes =
                    {
                        [HealthStatus.Healthy] = StatusCodes.Status200OK,
                        [HealthStatus.Degraded] = StatusCodes.Status500InternalServerError,
                        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
                    },
                    ResponseWriter = WriteHealtCheckReadyResponse,
                    Predicate = (check) => check.Tags.Contains("ready"),
                    AllowCachingResponses = false
                });

                endpoints.MapHealthChecks("/health/live", new HealthCheckOptions()
                {
                    ResponseWriter = WriteHealtCheckLiveResponse,
                    Predicate = (check) => check.Tags.Count() == 0,
                    AllowCachingResponses = false
                });

                endpoints.MapHealthChecks("/healthz", new HealthCheckOptions
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });//.RequireAuthorization();

                app.UseHealthChecksUI();

            });
        }

        private Task WriteHealtCheckLiveResponse(HttpContext context, HealthReport result)
        {
            context.Response.ContentType = "application/json";
            var json = new JObject(
                new JProperty("OverallStatus", result.Status.ToString()),
                new JProperty("TotalDuration", result.TotalDuration.TotalSeconds.ToString("0:0.00"))
                );

            return context.Response.WriteAsync(json.ToString());
        }

        private Task WriteHealtCheckReadyResponse(HttpContext context, HealthReport result)
        {
            context.Response.ContentType = "application/json";
            var json = new JObject(
                new JProperty("OverallStatus", result.Status.ToString()),
                new JProperty("TotalDuration", result.TotalDuration.TotalSeconds.ToString("0:0.00")),
                new JProperty("DependencyHealtChecks", new JObject(result.Entries.Select(s =>
                    new JProperty(s.Key, new JObject(
                    new JProperty("Status", s.Value.Status.ToString()),
                         new JProperty("Duration", s.Value.Duration.TotalSeconds.ToString("0:0.00")),
                         new JProperty("Exception", s.Value.Exception?.Message),
                         new JProperty("Data", new JObject(s.Value.Data.Select(dicData => new JProperty(dicData.Key, dicData.Value))))
                        ))
                ))));

            return context.Response.WriteAsync(json.ToString());
        }
    }
}
