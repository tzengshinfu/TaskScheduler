using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard;
using Hangfire.Dashboard.Management;
using Hangfire.Storage.SQLite;
using Microsoft.Owin;
using Owin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Web.Hosting;
using TaskScheduler.Attributes;
using TaskScheduler.Filters;

[assembly: OwinStartup(typeof(TaskScheduler.Startup))]
namespace TaskScheduler {
    /// <summary>
    /// 參考來源 https://blog.darkthread.net/blog/hangfire-recurringjob-notes/
    /// </summary>
    public class Startup : IRegisteredObject {
        private static BackgroundJobServer backgroundJobServer = null;

        public Startup() {
            HostingEnvironment.RegisterObject(this);
        }

        public static IEnumerable<IDisposable> GetHangfireConfiguration() {
            GlobalConfiguration.Configuration.UseSQLiteStorage(HostingEnvironment.MapPath("~/App_Data/hangfire.db"), new SQLiteStorageOptions() { QueuePollInterval = TimeSpan.FromSeconds(1) });
            GlobalConfiguration.Configuration.UseConsole();
            GlobalConfiguration.Configuration
                .UseDashboardMetric(DashboardMetrics.EnqueuedAndQueueCount)
                .UseDashboardMetric(DashboardMetrics.ScheduledCount)
                .UseDashboardMetric(DashboardMetrics.ProcessingCount)
                .UseDashboardMetric(DashboardMetrics.SucceededCount)
                .UseDashboardMetric(DashboardMetrics.FailedCount)
                .UseDashboardMetric(DashboardMetrics.AwaitingCount);

            backgroundJobServer = new BackgroundJobServer(
                new BackgroundJobServerOptions {
                    ServerName =
                    $"JobServer-{Process.GetCurrentProcess().Id}"
                });

            yield return backgroundJobServer;
        }

        public void Configuration(IAppBuilder app) {
            GlobalConfiguration.Configuration.UseFilter(new PrintExceptionAttribute());
            GlobalJobFilters.Filters.Add(new ProlongExpirationTimeAttribute());
            app.UseHangfireAspNet(GetHangfireConfiguration);
            app.UseHangfireDashboard("/Dashboard", new DashboardOptions() {
                Authorization = new[] { new DashboardAuthorizationFilter() }
            });
            GlobalConfiguration.Configuration.UseManagementPages(new Assembly[] { typeof(ManagementPage).Assembly });
            ManagementPage.ReloadTaskDefinitions();
            ManagementPage.ReloadTaskSchedule();
        }

        public void Stop(bool immediate) {
            if (backgroundJobServer != null) {
                backgroundJobServer.Dispose();
            }

            HostingEnvironment.UnregisterObject(this);
        }
    }
}
