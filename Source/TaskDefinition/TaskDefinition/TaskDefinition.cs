using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.Win32.TaskScheduler;
using System;
using System.ComponentModel;
using System.Linq;
using System.Net.Mail;

namespace TaskDefinition {
    /// <summary>
    /// 工作定義
    /// 1.類別型態限定靜態類別
    /// 2.函數參數限定string型態,以利使用文字檔或DB撰寫排程規則
    /// </summary>
    [ManagementPage("工作定義")]
    public static class Class1 {
        private static readonly string mailServerAddress = "電子郵件主機位址";
        private static readonly string sqlServerName = "MSSQL 主機位址";
        private static readonly string taskServerPath = "工作排程器主機路徑";
        private static readonly string taskUserName = "工作排程器使用者名稱";
        private static readonly string taskUserDomainName = "工作排程器使用者網域";
        private static readonly string taskUserPassword = "工作排程器使用者密碼";

        [Job]
        [DisplayName("寄信功能")]
        [Description(nameof(SendMail))]
        [PrintException]
        public static void SendMail([DisplayData("寄信人(用分號區隔)", "")] string mailFrom, [DisplayData("收信人(用分號區隔)", "")] string mailTo, [DisplayData("副本(用分號區隔)", "")] string mailcc, [DisplayData("密件副本(用分號區隔)", "")] string mailBcc, [DisplayData("附件(用分號區隔)", "")] string attachment, [DisplayData("主旨", "")] string mailSubject, [DisplayData("內文", "")] string mailBody, [DisplayData("寄信人顯示名稱(選填)", "")] string displayName = "") {
            var symbol = ';';//分割符號

            using (var mail = new MailMessage()) {
                if (!string.IsNullOrWhiteSpace(mailFrom)) {
                    mail.From = string.IsNullOrEmpty(displayName)
                        ? new MailAddress(mailFrom)
                        : new MailAddress(mailFrom, displayName);
                }

                if (!string.IsNullOrWhiteSpace(mailTo)) {
                    foreach (var sendTo in mailTo.Split(symbol).Where(x => !string.IsNullOrEmpty(x))) {
                        mail.To.Add(new MailAddress(sendTo));
                    }
                }

                mail.Subject = mailSubject;
                mail.Body = mailBody;
                mail.BodyEncoding = System.Text.Encoding.UTF8;//解決中文變亂碼

                if (!string.IsNullOrWhiteSpace(mailcc)) {
                    foreach (var cc in mailcc.Split(symbol).Where(x => !string.IsNullOrEmpty(x))) {
                        mail.CC.Add(cc);
                    }
                }

                if (!string.IsNullOrWhiteSpace(mailBcc)) {
                    foreach (var bcc in mailBcc.Split(symbol).Where(x => !string.IsNullOrEmpty(x))) {
                        mail.Bcc.Add(bcc);
                    }
                }

                if (!string.IsNullOrWhiteSpace(attachment)) {
                    foreach (var att in attachment.Split(symbol).Where(x => !string.IsNullOrEmpty(x))) {
                        mail.Attachments.Add(new Attachment(att));
                    }
                }

                var smtpClient = new SmtpClient(mailServerAddress);

                smtpClient.Send(mail);
            }
        }

        [Job]
        [DisplayName("執行SQL Server Agent 作業")]
        [Description(nameof(StartSqlServerAgentJob))]
        [PrintException]
        public static void StartSqlServerAgentJob([DisplayData("作業名稱", "")] string jobName) {
            var sqlServer = new Server(sqlServerName);
            var job = sqlServer.JobServer.Jobs[jobName];
            var doesJobExist = job != null;

            if (!doesJobExist) {
                throw new Exception($"SQL Server Agent 作業[{jobName}]不存在");
            }

            var isJobIdle = job.CurrentRunStatus == Microsoft.SqlServer.Management.Smo.Agent.JobExecutionStatus.Idle;

            if (!isJobIdle) {
                throw new Exception($"SQL Server Agent 作業[{jobName}]狀態必須是'閒置'才可被執行");
            }

            job.Start();
        }

        [Job]
        [DisplayName("執行Windows 工作排程器 工作")]
        [Description(nameof(StartTaskSchedulerTask))]
        [PrintException]
        public static void StartTaskSchedulerTask([DisplayData("工作路徑", "")] string taskPath) {
            using (var ts = new TaskService(taskServerPath, taskUserName, taskUserDomainName, taskUserPassword)) {
                var task = ts.GetTask(taskPath);
                var doesTaskExist = task != null;

                if (!doesTaskExist) {
                    throw new Exception($"Windows 工作排程器 工作[{taskPath}]路徑不存在");
                }

                var isTaskReady = task.State == TaskState.Ready;

                if (!isTaskReady) {
                    throw new Exception($"Windows 工作排程器 工作[{taskPath}]狀態必須是'就緒'才可被執行");
                }

                task.Run();
            }
        }
    }
}

public static partial class ExtensionMethod {
    public static string AppendMessage(this string runMessage, string text) {
        var result = "";

        runMessage += ((runMessage == "") ? "" : Environment.NewLine) + text;
        result = runMessage;

        return result;
    }
}
