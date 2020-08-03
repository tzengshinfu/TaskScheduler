using Hangfire;
using Hangfire.Dashboard.Management.Metadata;
using Hangfire.Dashboard.Management.Support;
using Hangfire.Storage;
using Jil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Web.Hosting;

namespace TaskScheduler {
    [ManagementPage("管理功能")]
    public static class ManagementPage {
        private static List<TypeInfo> taskDefinitionTypes;

        private static List<TypeInfo> GetTaskDefinitionTypes() {
            var taskDefinitionTypes = new List<TypeInfo>();

            foreach (var dllPath in Directory.GetFiles(HostingEnvironment.MapPath(@"~/TaskDefinitions"), "*.dll")) {
                using (FileStream fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read, FileShare.Delete)) {
                    BinaryReader br = new BinaryReader(fs);
                    byte[] bin = br.ReadBytes(Convert.ToInt32(fs.Length));
                    fs.Close();
                    br.Close();

                    Assembly assembly = Assembly.Load(bin);
                    foreach (var type in assembly.DefinedTypes) {
                        var ManagementPageAttributes = (ManagementPageAttribute[])Attribute.GetCustomAttributes(type, typeof(ManagementPageAttribute));

                        if (ManagementPageAttributes.Length > 0) {
                            taskDefinitionTypes.Add(type);
                        }
                    }
                }
            }

            return taskDefinitionTypes;
        }

        [Job]
        [DisplayName("更新工作排程")]
        [Description("從工作排程文字檔載入最新工作排程")]
        public static void ReloadTaskSchedule() {
            string[] taskScheduleLines = File.ReadAllLines(System.Configuration.ConfigurationManager.AppSettings["TaskScheduleFilePath"]);

            using (var connection = JobStorage.Current.GetConnection()) {
                foreach (var recurringTask in StorageConnectionExtensions.GetRecurringJobs(connection)) {
                    RecurringJob.RemoveIfExists(recurringTask.Id);
                }
            }

            foreach (var taskScheduleLine in taskScheduleLines) {
                if (taskScheduleLine.TrimStart().StartsWith("//") == false && string.IsNullOrWhiteSpace(taskScheduleLine) == false) {
                    var taskGroups = JSON.Deserialize<dynamic[]>(taskScheduleLine);
                    string taskId = taskGroups[0];
                    string taskDescription = taskGroups[1];
                    string[] taskParameters = JSON.Deserialize<string[]>(taskGroups[2].ToString());
                    string cronExpression = taskGroups[3];

                    RecurringJob.AddOrUpdate(taskId, () => RunTask(taskId, taskDescription, taskParameters), cronExpression, TimeZoneInfo.Local);
                }
            }
        }

        [DisplayName("{1}")]
        public static void RunTask(string taskName, string taskDescription, object[] taskParameters) {
            foreach (var type in taskDefinitionTypes) {
                var task = type.GetMethod(taskName);

                if (task != null) {
                    task.Invoke(type, taskParameters);

                    return;
                }
            }

            throw new Exception("工作定義不存在[" + taskName + "]方法");
        }

        [Job]
        [DisplayName("產生工作排程範例檔")]
        [Description("從最新工作定義產生工作排程範例檔")]
        public static void CreateTaskScheduleSampleFile() {
            var taskLines = new List<string>();

            taskLines.Add(@"//工作排程格式如下");
            taskLines.Add(@"//[""工作代號"",""工作敘述"",[""參數1"",""參數2"",...],""Cron表達式""]");

            foreach (var type in taskDefinitionTypes) {
                var methods = type.GetMethods();

                foreach (var method in methods) {
                    var taskGroups = new List<string>();

                    if (method.GetCustomAttribute(typeof(JobAttribute)) != null) {
                        taskLines.Add("");

                        var taskDescription = method.GetCustomAttribute(typeof(DescriptionAttribute)) != null ? (method.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute).Description : "";

                        if (taskDescription != "") {
                            taskLines.Add("//" + taskDescription);
                        }

                        var taskId = method.Name;
                        var taskName = method.GetCustomAttribute(typeof(DisplayNameAttribute)) != null ? (method.GetCustomAttribute(typeof(DisplayNameAttribute)) as DisplayNameAttribute).DisplayName : "";

                        var taskParameterGroups = new List<string>();

                        foreach (var taskParameter in method.GetParameters()) {
                            var taskParameterDescription = taskParameter.GetCustomAttribute(typeof(DisplayDataAttribute)) != null ? (taskParameter.GetCustomAttribute(typeof(DisplayDataAttribute)) as DisplayDataAttribute).LabelText : taskParameter.Name;
                            var taskParameterDefaultValue = taskParameter.GetCustomAttribute(typeof(DisplayDataAttribute)) != null ? (taskParameter.GetCustomAttribute(typeof(DisplayDataAttribute)) as DisplayDataAttribute).PlaceholderText : "";

                            taskParameterGroups.Add(taskParameterDescription + ((taskParameterDefaultValue != "") ? $",預設值{{{taskParameterDefaultValue}}}" : ""));
                        }

                        var taskParameters = JSON.Serialize(taskParameterGroups);

                        taskGroups.Add(taskId);
                        taskGroups.Add(taskName);
                        taskGroups.Add(taskParameters);
                        taskGroups.Add("* * * * *");

                        var taskLine = JSON.Serialize(taskGroups);
                        taskLine = taskLine.Replace("\\\"", "\"").Replace("\"[", "[").Replace("]\"", "]");
                        taskLine = "//" + taskLine;
                        taskLines.Add(taskLine);
                    }
                }
            }

            File.WriteAllLines(System.Configuration.ConfigurationManager.AppSettings["TaskScheduleFilePath"] + ".Sample", taskLines);
        }

        [Job]
        [DisplayName("更新工作定義")]
        [Description("從TaskDefinitions資料夾載入最新工作定義")]
        public static void ReloadTaskDefinitions() {
            taskDefinitionTypes = GetTaskDefinitionTypes();
        }
    }
}
