
using winCron; // ProjectInstaller
using Util; // FileHelper
using ExtensionMethods; // DateExtensions.FirstDayOfMonth

using System; // Exception
using System.ServiceProcess; // ServiceBase
//using System.Linq; // Count()
using System.Collections.Generic; // List<string>
using System.Security.Permissions; // SecurityPermission
using System.Text; // StringBuilder
using System.IO; // File, FileInfo

using ProcessUtilitiesNamespace; // ProcessUtilities
using murrayju.ProcessExtensions; // StartProcessAsCurrentUser

// ProcessUtilities: Launch an app on the desktop from a service
// https://www.developpez.net/forums/d1198920/dotnet/langages/csharp/lancer-appli-bureau-service

// https://github.com/murrayju/CreateProcessAsUser Visible task launched from a Windows service 
// https://github.com/atifaziz/NCrontab Cron engine
// https://github.com/sergeyt/CronDaemon Task scheduler based on NContab
// https://github.com/bradymholt/cron-expression-descriptor

namespace winCronNamespace
{
    public partial class winCron : ServiceBase
    {

        const bool checkFilePathCmd = false; // Set false to disable task file path check

        public static readonly DateTime nullDate = new DateTime(1900, 1, 1);
        public static readonly string crLf = Environment.NewLine;
        string formatHM = "HH:mm";
        string formatDateHM = "dd\\/MM\\/yyyy HH:mm";
        string formatDateHMS = "dd\\/MM\\/yyyy HH:mm:ss";
        string m_path = "";
        string m_logPath = "";

        public winCron()
        {
            InitializeComponent();
        }
        protected override void OnStop()
        {
            LogMessage("Service stopped!");
        }

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        protected override void OnStart(string[] args)
        {

            //ProcessExtensions.StartProcessAsCurrentUser("calc.exe"); // Quick test: Ok!
            //return;

            // Handle exception on a Windows Service:
            // https://stackoverflow.com/questions/2456819/how-can-i-set-up-net-unhandledexception-handling-in-a-windows-service
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += 
                new UnhandledExceptionEventHandler(ExceptionHandler);

            string serviceExePath = GetServiceExePath();
            m_path = FileHelper.GetParentDirectoryPath(serviceExePath);
            m_logPath = m_path + "\\winCron.log";
            string tasksPath = m_path + "\\Tasks.txt";
#pragma warning disable CS0162 // Unreachable code detected
            if (ProjectInstaller.invisibleUserService) {
                m_logPath = m_path + "\\winCronInv.log";
                tasksPath = m_path + "\\TasksInv.txt";
            }
#pragma warning restore CS0162 // Unreachable code detected

            string serviceVersion = "";
            DateTime serviceExeDate = nullDate; // new DateTime(1900, 1, 1);
            FileHelper.GetAssemblyVersion(serviceExePath, ref serviceVersion, ref serviceExeDate);

            string userService = (ProjectInstaller.invisibleUserService ?
                "(invisible user service)" : "(visible local system service)");
            string msg = "Service started! " + userService + " " + 
                serviceVersion + " " + serviceExeDate.ToString(formatDateHM) + crLf;
            string errMsg = "";
            
            string[] txtCrons = FileHelper.ReadFile(tasksPath, out errMsg);
            
            var cronList = new List<string>();
            foreach (string cron in txtCrons) {
                if (cron.StartsWith("//")) continue; // Task comment
                if (String.IsNullOrEmpty(cron.Trim())) continue; // Task blank line
                cronList.Add(cron.Trim());
            }

            var frequentlyDic = new SortedDic<String, clsTaskList>();
            var hourlyDic = new SortedDic<String, clsTaskList>();
            var dailyDic = new SortedDic<String, clsTaskList>();
            var weeklyDic = new SortedDic<String, clsTaskList>();
            var monthlyDic = new SortedDic<String, clsTaskList>();
            bool errFound = false;
            int nbCronsOK = 0;
            foreach (string cronTaskLine in cronList)
            {
                string[] fields = cronTaskLine.Split(':');
                if (fields.Length < 2) {
                    msg += "  -> Syntax error: [" + cronTaskLine + "]" + crLf;
                    continue;
                }
                string cron = "";
                string task = "";
                string strNoLog = "";
                bool noLog = false;
                int ub = fields.GetUpperBound(0);
                if (ub >= 0) cron = fields[0].Trim();
                if (ub >= 1) task = fields[1].Trim();
                if (ub >= 2) strNoLog = fields[2].Trim();
                if (strNoLog == "NoLog") { 
                    noLog = true; strNoLog = " (no log)"; 
                }

                if (checkFilePathCmd && !File.Exists(m_path + "\\" + task)) {
                    msg += "  -> Can't find: [" + m_path + "\\" + task + "]" + crLf;
                    continue;
                }

                // Display periodicity in plain text
                string cronDescr = CronExpressionDescriptor.ExpressionDescriptor.GetDescription(cron);
                msg += "  " + task + ": [" + cron + "]: " + cronDescr + strNoLog + crLf;

                if (!MakePlanning(cron, task, cronDescr, 
                        frequentlyDic, hourlyDic, dailyDic, weeklyDic, monthlyDic, ref errMsg)) {
                    msg += "  -> Syntax error: " + errMsg + crLf;
                    continue;
                }

                if (!StartTasks(cron, task, noLog, ref errMsg)) {
                    msg += "  Crons bug: " + errMsg + crLf;
                    errFound = true;
                    break;
                }
                nbCronsOK++;
            }
            if (nbCronsOK==0) msg += "  No cron found!" + crLf;
            else if (!errFound) msg += "  Crons ok!" + crLf;

            LogMessage(msg);

            LogMessage(PrintPlanning(frequentlyDic, dailyDic, weeklyDic, monthlyDic));
        }

        #region "Planning"

        class clsTask
        {
            public DateTime date;
            public string task;
            public string descr;
            public clsTask(DateTime date, string task, string descr)
            {
                this.date = date;
                this.task = task;
                this.descr = descr;
            }
        }

        class clsTaskList
        {
            public DateTime date;
            public List<clsTask> taskList;
            public clsTaskList(DateTime date, List<clsTask> taskList)
            {
                this.date = date;
                this.taskList = taskList;
            }
        }

        private bool MakePlanning(string cron, string task, string taskDescr,
            SortedDic<String, clsTaskList> frequentlyDic,
            SortedDic<String, clsTaskList> hourlyDic,
            SortedDic<String, clsTaskList> dailyDic,
            SortedDic<String, clsTaskList> weeklyDic,
            SortedDic<String, clsTaskList> monthlyDic, ref string errMsg) 
        {

            errMsg = "";
            NCrontab.CrontabSchedule s = null;
            try
            {
                s = NCrontab.CrontabSchedule.Parse(cron);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }

            var cronL = cron.ToLower();

            var firstDay = DateTime.Today.FirstDayOfMonth();

            var isRecurrent = false;
            string taskDescrL = taskDescr.ToLower();
            if (taskDescrL.Contains("every")) isRecurrent = true;
            if (taskDescrL.Contains("tous")) isRecurrent = true; // French
            if (taskDescrL.Contains("toutes")) isRecurrent = true; // French

            bool mon = cronL.Contains(" mon");
            bool tue = cronL.Contains(" tue");
            bool wed = cronL.Contains(" wed");
            bool thu = cronL.Contains(" thu");
            bool fri = cronL.Contains(" fri");
            bool sat = cronL.Contains(" sat");
            bool sun = cronL.Contains(" sun");
            bool isWeekly = mon || tue || wed || thu || fri || sat || sun;

            bool onDay = taskDescrL.Contains("on day") || 
                         taskDescrL.Contains("le"); // French
            bool ofTheMonth = taskDescrL.Contains("of the month") ||
                              taskDescrL.Contains("du mois"); // French
            bool isMonthly = onDay && ofTheMonth;

            if (isRecurrent && !isWeekly && !isMonthly) {
                var hourlyStartDate = firstDay;
                var hourlyEndDate = hourlyStartDate.AddHours(24);
                var hourlyOccurrences = s.GetNextOccurrences(hourlyStartDate, hourlyEndDate);
                foreach (DateTime date in hourlyOccurrences)
                {
                    
                    string key = taskDescr;
                    if (!hourlyDic.ContainsKey(key))
                    {
                        var lst = new List<clsTask> { new clsTask(date, task, taskDescr) };
                        hourlyDic.Add(key, new clsTaskList(date, lst));
                    }
                    else
                    {
                        var lst = hourlyDic[key];
                        lst.taskList.Add(new clsTask(date, task, taskDescr));
                    }

                    key = date.ToString(formatHM);
                    if (!frequentlyDic.ContainsKey(key))
                    {
                        var lst = new List<clsTask> { new clsTask(date, task, taskDescr) };
                        frequentlyDic.Add(key, new clsTaskList(date, lst));
                    }
                    else
                    {
                        var lst = frequentlyDic[key];
                        lst.taskList.Add(new clsTask(date, task, taskDescr));
                    }

                }
            }

            if (!isRecurrent && !isWeekly && !isMonthly)
            {
                var dailyStartDate = firstDay;
                var dailyEndDate = dailyStartDate.AddDays(1);
                var dailyOccurrences = s.GetNextOccurrences(dailyStartDate, dailyEndDate);
                foreach (DateTime date in dailyOccurrences)
                {
                    string key = taskDescr;
                    if (hourlyDic.ContainsKey(key)) continue;
                    if (!dailyDic.ContainsKey(key))
                    {
                        var lst = new List<clsTask> { new clsTask(date, task, taskDescr) };
                        dailyDic.Add(key, new clsTaskList(date, lst));
                    }
                    else
                    {
                        var lst = dailyDic[key];
                        lst.taskList.Add(new clsTask(date, task, taskDescr));
                    }
                }
            }

            if (!isRecurrent && isWeekly && !isMonthly)
            {
                //var weeklyStartDate = getMondayBefore(firstDay);
                var weeklyStartDate = firstDay.GetMondayBefore();
                var weeklyEndDate = weeklyStartDate.AddDays(7);
                var weeklyOccurrences = s.GetNextOccurrences(weeklyStartDate, weeklyEndDate);
                foreach (DateTime date in weeklyOccurrences)
                {
                    string key = taskDescr;
                    if (hourlyDic.ContainsKey(key)) continue;
                    if (dailyDic.ContainsKey(key)) continue;
                    if (!weeklyDic.ContainsKey(key))
                    {
                        var lst = new List<clsTask> { new clsTask(date, task, taskDescr) };
                        weeklyDic.Add(key, new clsTaskList(date, lst));
                    }
                    else
                    {
                        var lst = weeklyDic[key];
                        lst.taskList.Add(new clsTask(date, task, taskDescr));
                    }
                }
            }

            if (!isRecurrent && !isWeekly && isMonthly)
            {
                var monthlyStartDate = firstDay;
                var monthlyEndDate = monthlyStartDate.AddMonths(1);
                var monthlyOccurrences = s.GetNextOccurrences(monthlyStartDate, monthlyEndDate);
                foreach (DateTime date in monthlyOccurrences)
                {
                    string key = taskDescr;
                    if (hourlyDic.ContainsKey(key)) continue;
                    if (dailyDic.ContainsKey(key)) continue;
                    if (weeklyDic.ContainsKey(key)) continue;
                    if (!monthlyDic.ContainsKey(key))
                    {
                        var lst = new List<clsTask> { new clsTask(date, task, taskDescr) };
                        monthlyDic.Add(key, new clsTaskList(date, lst));
                    }
                    else
                    {
                        var lst = monthlyDic[key];
                        lst.taskList.Add(new clsTask(date, task, taskDescr));
                    }
                }
            }
            return true;
        } 

        private string PrintPlanning(
            SortedDic<String, clsTaskList> frequentlyDic,
            SortedDic<String, clsTaskList> dailyDic,
            SortedDic<String, clsTaskList> weeklyDic,
            SortedDic<String, clsTaskList> monthlyDic)
        {
            var sb = new StringBuilder("Planning:" + crLf);

            var now = DateTime.Now;
            int nbHoursLocalTime = (int)(now - now.ToUniversalTime()).TotalHours;

            if (frequentlyDic.Count > 0) { 
                sb.AppendLine("");
                sb.AppendLine("Frequently:");
                int nbTasks = 0;
                const int nbTasksMax = 10;
                foreach (clsTaskList taskList in frequentlyDic.Sort("date")) {
                    foreach (clsTask task in taskList.taskList) {
                        if (CronScheduling.CronDaemon.useLocalTime)
                            sb.AppendLine(task.date.ToString(formatHM) + " : " + 
                                task.task + " : " + task.descr);
                        else
                            sb.AppendLine(task.date.ToString(formatHM) +
                                " (" + task.date.AddHours(nbHoursLocalTime).ToString(formatHM) + ") : " +
                                task.task + " : " + task.descr);

                        if (++nbTasks >= nbTasksMax) { sb.AppendLine("..."); break; }
                    }
                    if (nbTasks >= nbTasksMax) break;
                }
            }

            if (dailyDic.Count > 0) {
                sb.AppendLine("");
                sb.AppendLine("Daily:");
                foreach (clsTaskList taskList in dailyDic.Sort("date"))
                foreach (clsTask task in taskList.taskList)
                    if (CronScheduling.CronDaemon.useLocalTime)
                        sb.AppendLine(task.date.ToString(formatHM) + " : " + 
                            task.task + " : " + task.descr);
                    else
                        sb.AppendLine(task.date.ToString(formatHM) +
                            " (" + task.date.AddHours(nbHoursLocalTime).ToString(formatHM) + ") : " +
                            task.task + " : " + task.descr);

            }

            if (weeklyDic.Count > 0) {
                sb.AppendLine("");
                sb.AppendLine("Weekly:");
                foreach (clsTaskList taskList in weeklyDic.Sort("date"))
                foreach (clsTask task in taskList.taskList)
                    if (CronScheduling.CronDaemon.useLocalTime)
                        sb.AppendLine(task.date.ToString("ddd ") +
                            task.date.ToString(formatHM) + " : " + 
                            task.task + " : " + task.descr);
                    else
                        sb.AppendLine(task.date.ToString("ddd ") +
                            task.date.ToString(formatHM) +
                            " (" + task.date.AddHours(nbHoursLocalTime).ToString(formatHM) + ") : " + 
                            task.task + " : " + task.descr);
            }

            if (monthlyDic.Count > 0) {
                sb.AppendLine("");
                sb.AppendLine("Monthly:");
                foreach (clsTaskList taskList in monthlyDic.Sort("date"))
                foreach (clsTask task in taskList.taskList)
                    if (CronScheduling.CronDaemon.useLocalTime)
                        sb.AppendLine(task.date.ToString("dd HH:mm") + " : " + 
                            task.task + " : " + task.descr);
                    else
                        sb.AppendLine(task.date.ToString("dd HH:mm") +
                            " (" + task.date.AddHours(nbHoursLocalTime).ToString(formatHM) + ") : " + 
                            task.task + " : " + task.descr);
            }

            return sb.ToString();
        }

        //public DateTime getMondayBefore(DateTime date)
        //{
        //    // Return monday before a date
        //    var previousDate = date;
        //    while (previousDate.DayOfWeek != DayOfWeek.Monday) 
        //        previousDate = previousDate.AddDays(-1);
        //    return previousDate;
        //}
        
        #endregion

        void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception ex = (Exception)args.ExceptionObject;
            string msg = "Error: " + ex.Message + ex.StackTrace;
            LogMessage(msg);
        }

        void LogMessage(string msg)
        {
            string errMsg = "";
            string dateMsg = DateTime.Now.ToString(formatDateHMS) + " " + msg + crLf;
            FileHelper.WriteFile(m_logPath, dateMsg, out errMsg, append: true);
        }

        bool StartTasks(string cron, string task, bool noLog, ref string errMsg)
        {
            errMsg = "";
            try
            {
                var crond = CronScheduling.CronDaemon.Start<string>(
                    value => {

                        try 
                        {
                            string pathTask = m_path + "\\" + task;
                            string invisible = "";
                            
#pragma warning disable CS0162 // Unreachable code detected
                            if (ProjectInstaller.invisibleUserService)
                            {
                                FileHelper.StartProcess(pathTask); // Works in User service, invisible
                            }
                            else { // LocalSystem service

                                // Windows Home: OK
                                // Windows Workstation: OK
                                // Windows Server 2012: Random bug:
                                // StartProcessAsCurrentUser: GetSessionUserToken failed
                                // WTSQueryUserToken requires the SE_TCB_NAME privilege
                                // Use AdjustTokenPrivileges to enable it:
                                // (see bCreateProcess: StartImpersonation: AcquireLoadUserProfilePriveleges)
                                // http://www.vbforums.com/showthread.php?616830-RESOLVED-VB-Net-CreateProcessAsUser-API
                                string errMsg1 = "";
                                if (!ProcessExtensions.StartProcessAsCurrentUser(pathTask, out errMsg1))
                                {
                                    // Solution: invisible run 
                                    invisible = " (" + errMsg1 + ": invisible)";
                                    ProcessUtilities.CreateUIProcessForServiceRunningAsLocalSystem(
                                        pathTask, in_strArguments: "");
                                }

                            }
#pragma warning restore CS0162 // Unreachable code detected
                            if (!noLog) LogMessage("Service Cron: " + value + invisible);

                        }
                        catch (Exception ex)
                        {
                            string errMsg1 = "Service Cron: " + value + crLf;
                            errMsg1 += " Error:" + ex.Message + ex.StackTrace; 
                            LogMessage(errMsg1);
                        }
                    });

                crond.Add(task + ": " + cron, cron);

                return true;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return false;
            }
        }

        private string GetServiceExePath()
        {
            try {
                var service = System.Reflection.Assembly.GetAssembly(typeof(ProjectInstaller));
                return service.Location;
            }
            catch (Exception ex) {
                return "Error GetServiceExePath(): " + ex.Message;
            }
        }
    }
}
