using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using System;

namespace TaskScheduler.Attributes {
    /// <summary>
    /// 工作紀錄保留天數
    /// </summary>
    public class ProlongExpirationTimeAttribute : JobFilterAttribute, IApplyStateFilter {
        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction) {
            context.JobExpirationTimeout = TimeSpan.FromDays(Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["TaskHistoryRemainDays"]));
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction) {
            context.JobExpirationTimeout = TimeSpan.FromDays(Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["TaskHistoryRemainDays"]));
        }
    }
}
