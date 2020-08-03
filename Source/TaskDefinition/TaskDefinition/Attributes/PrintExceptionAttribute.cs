using Hangfire.Common;
using Hangfire.States;
using System;

namespace TaskDefinition {
    /// <summary>
    /// 發生異常將訊息輸出至主控台
    /// </summary>
    public class PrintExceptionAttribute : JobFilterAttribute, IElectStateFilter {
        public void OnStateElection(ElectStateContext context) {
            var failedState = context.CandidateState as FailedState;

            if (failedState != null) {
                Console.WriteLine("工作[" + context.BackgroundJob.Id + "]發生異常:" + Environment.NewLine + failedState.Exception.ToString());
            }
        }
    }
}
