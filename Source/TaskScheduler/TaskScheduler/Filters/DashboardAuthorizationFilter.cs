using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace TaskScheduler.Filters {
    public class DashboardAuthorizationFilter : IDashboardAuthorizationFilter {
        public bool Authorize([NotNull] DashboardContext context) {
            //TODO:開放遠端主機連線,有需求再實作驗證機制
            return true;
        }
    }
}
