Windows Service Cron Scheduler
---

Very simple cron scheduler from a Windows Service in C#, based
on [CreateProcessAsUser](https://github.com/murrayju/CreateProcessAsUser), [CreateUIProcessForService](https://www.developpez.net/forums/d1198920/dotnet/langages/csharp/lancer-appli-bureau-service) (article in french, but source code in english), [ncrontab](https://github.com/atifaziz/NCrontab),
[cronDaemon](https://github.com/sergeyt/CronDaemon), and [cron-expression-descriptor](https://github.com/bradymholt/cron-expression-descriptor).

<!-- TOC -->

- [Goals](#goals)
- [User interactive service and invisible service](#user-interactive-service-and-invisible-service)
- [CreateProcessAsUser](#createprocessasuser)
    - [Random bug (on Windows Server 2012): StartProcessAsCurrentUser: GetSessionUserToken failed ("Can't get user token")](#random-bug-on-windows-server-2012-startprocessascurrentuser-getsessionusertoken-failed-cant-get-user-token)
- [CreateUIProcessForService](#createuiprocessforservice)
- [ncrontab](#ncrontab)
    - [Sample code](#sample-code)
- [cronDaemon](#crondaemon)
    - [Sample code](#sample-code)
- [cron-expression-descriptor](#cron-expression-descriptor)
- [Samples](#samples)
- [Bugs](#bugs)
    - [Does not work on a Windows Home version?](#does-not-work-on-a-windows-home-version)
    - [NCrontab.Signed not found](#ncrontabsigned-not-found)
    - [Bug: "The value needs to translate in milliseconds to -1 (signifying an infinite timeout), 0 or a positive integer less than or equal to Int32.MaxValue."](#bug-the-value-needs-to-translate-in-milliseconds-to--1-signifying-an-infinite-timeout-0-or-a-positive-integer-less-than-or-equal-to-int32maxvalue)
    - [Many packages comes with cron-expression-descriptor](#many-packages-comes-with-cron-expression-descriptor)
    - [CreateProcessAsUser does not work with VB .Net from a Windows Service](#createprocessasuser-does-not-work-with-vb-net-from-a-windows-service)
- [How to set local time instead of UTC time?](#how-to-set-local-time-instead-of-utc-time)
- [Documentation](#documentation)

<!-- /TOC -->

# Goals
Why replace the good old Windows Task Scheduler?
- Compute a planning of all scheduled tasks
- Enable/Disable all scheduled tasks at once
- Log all scheduled tasks in one single log file
- No more need to update each task password regularly

# User interactive service and invisible service
In the general case, if serviceProcessInstaller.Account is configured to ServiceAccount.User (instead of ServiceAccount.LocalSystem), all launched tasks are invisible (Environment.UserInteractive = false). In this mode, you must type your account name (i.e. MyDomain\MyName) and password in a requested message box, at the service installation process, and you cannot see and interact with a task. But the task can be launched even if the user is not logged in (and the service can be configured to be launched automatically when the computer starts). Of course, this is the same functionality as the Windows Task Scheduler, depending on whether you choose to check interactive session or not ("Run only when user is logged on" check box), except one thing that I didn't manage to do: using Windows Service Cron Scheduler a running task remains invisible when a user logs in, while using the Task Scheduler, it will be visible in this case. But wait a minute, I found a solution to launch all the tasks, visible or not, using a single service, and without asking for the password of the user account, and all the tasks can be started automatically when the computer starts.

There is a boolean in the source code to switch from one service to another using a special ReleaseInv configuration:
```c#
#if ReleaseInv // Compile with ReleaseInv
    public const bool invisibleUserService = true;
#else // Compile with Release
    public const bool invisibleUserService = false;
#endif
```

If the user does not have a password defined, this error is triggered when the service starts (the user MUST have a password for his Windows account):
```
Could not start the <service name> service on Local Computer.
Error 1069: The service did not start due to a logon failure.
```

# CreateProcessAsUser
This allows a process running in a different session (such as a windows service) to start a process with a graphical user interface that the user must see.

Note that the process must have the appropriate (admin) privileges for this to work correctly. For [WTSQueryUserToken](https://github.com/murrayju/CreateProcessAsUser/blob/0381db2e8fb36f48794c073e87f773f7ca1ae039/ProcessExtensions/ProcessExtensions.cs#L197) you will need the __SE_TCB_NAME__ privilege, which is typically only held by Services running under the LocalSystem account ([Link](https://stackoverflow.com/a/1289126/1872399)).

This functionality is not required (and not compatible) with the invisible user service, it is mandatory only for the visible local system service.

Usage
```c#
using murrayju.ProcessExtensions;
// ...
ProcessExtensions.StartProcessAsCurrentUser("calc.exe");
```

## Random bug (on Windows Server 2012): StartProcessAsCurrentUser: GetSessionUserToken failed ("Can't get user token")
If the user is not logged in, it is normal to get this error (for example if the session is closed). But even when the user is logged in, this bug occurs randomly (in fact a few hours after the start of the service, then systematically). WTSQueryUserToken (sometimes?) requires the __SE_TCB_NAME__ privilege: Use AdjustTokenPrivileges to enable it.
If you get this bug, there are three solutions:
- use invisible service instead (User service instead of LocalSystem service)
- ignore the bug this time (instead of throwing the exception, log the error and continue the next task, if this task is optional: but that would mean that this solution is not as reliable as the task scheduler!)
- if StartProcessAsCurrentUser fails, then use this code instead:

# CreateUIProcessForService
This code works fine either is the session is open (visible task) or close (unvisible task, if the user close his session, contrary to StartProcessAsCurrentUser), and also with automatic service when computer starts (provided that we use CreateUIProcessForService only if StartProcessAsCurrentUser fails, otherwise this function CreateUIProcessForService does not work with the automatic service when the computer starts).

# ncrontab
NCrontab: Crontab for .NET

## Sample code

```c#
var s = NCrontab.CrontabSchedule.Parse("0 12 * */2 Mon");
var start = new DateTime(2000, 1, 1);
var end = start.AddYears(1);
var occurrences = s.GetNextOccurrences(start, end);
Debug.WriteLine(string.Join(Environment.NewLine,
    from t in occurrences
    select $"{t:ddd, dd MMM yyyy HH:mm}"));
```

# cronDaemon
.NET library with single CronDaemon class with generic implementation of cron scheduling based on ncrontab.

## Sample code

```c#
var crond = CronDaemon.Start<string>(
  value => {
    Console.WriteLine(value);
  });

crond.Add("Print hi hourly", Cron.Hourly());
crond.Add("Print hi daily 5 times", Cron.Daily(), 5);
crond.Add("Print hi at 9AM UTC daily.  The cron expression is always evaluated in UTC", "0 9 * * *")
```

# cron-expression-descriptor
A .NET library that converts cron expressions into human readable descriptions.

Samples:

"* * * * *" : "Every minute"

"*/2 * * * *" : "Every 2 minutes"

# Samples

"* * * * * : Go.bat" : Launch Go.bat Every minute

"*/2 * * * * : Go2.bat" : Launch Go2.bat Every 2 minutes

Go.bat :
```
@echo off
echo %username%
echo "Let's go!"
Pause
```

# Bugs

## Does not work on a Windows Home version?
If you get this error on a Windows Home version:
```
An exception occurred during the Install phase.
System.Security.SecurityException: The source was not found, but some or all event logs could not be searched. Inaccessible logs: Security, State.
```
try launching Install.bat using Administrator privileges, then you should put absolute paths instead of relative ones:

Install.bat :
```
REM %windir%\Microsoft.NET\Framework\v4.0.30319\installutil MyService.exe ->
%windir%\Microsoft.NET\Framework\v4.0.30319\InstallUtil C:\MyDirectory\MyService.exe
```

## NCrontab.Signed not found
I don't understand why NCrontab is published on Nuget with two versions: [NCrontab](https://www.nuget.org/packages/ncrontab) and [NCrontab.Signed](https://www.nuget.org/packages/ncrontab.signed). Everything works fine on local development workstation. But when I release the service on a Windows server I have this bug:

```
Could not load file or assembly 'NCrontab.Signed, Version=3.2.20120.0, Culture=neutral, PublicKeyToken=5247b4370afff365' or one of its dependencies. The located assembly's manifest definition does not match the assembly reference. (Exception from HRESULT: 0x80131040)
```

The (bad) solution is to include cronDaemon source files (version 0.5.0.0) in this source code. This is a bad solution because thus we can not get Nuget updates for cronDaemon. I will try later when new versions of NCrontab are released.

Function added in CronDaemon.cs: Add job with an infinite loop:
```c#
/// <summary>
/// Adds specified job to <see cref="CronDaemon{T}"/> queue with given cron expression and maximum number of repetitions.
/// </summary>
/// <param name="job">The job definition.</param>
/// <param name="cronExpression">Specifies cron expression.  This will be evaluated in UTC time standard.</param>
public void Add(T job, string cronExpression)
{
	var crontab = CrontabSchedule.Parse(cronExpression);

	var cancellation = new CancellationTokenSource();

	Func<DateTime, DateTime?> schedule = time =>
	{
		if (cancellation.IsCancellationRequested) return null;
		return crontab.GetNextOccurrence(time);
	};

	Action run = async () =>
	{
		while (true)
		{
			var now = SystemTime.Now;
			var nextOccurrence = schedule(now);
			if (nextOccurrence == null) break;

			var delay = nextOccurrence.Value - now;
			await Task.Delay(delay, cancellation.Token);

			if (cancellation.IsCancellationRequested) break;

			_execute(_fork(job));
		}
	};

	Task.Run(run, cancellation.Token);

	_cancellations.Add(cancellation);
}
```

## Bug: "The value needs to translate in milliseconds to -1 (signifying an infinite timeout), 0 or a positive integer less than or equal to Int32.MaxValue."
Here is a bug fix in CronDaemon.cs: check the TimeSpan delay range:
```c#
    var delay = nextOccurrence.Value - now;
	await Task.Delay(delay, cancellation.Token);
    ->
    var delay = nextOccurrence.Value - now;
    // Bug fix: "The value needs to translate in milliseconds to -1 (signifying an infinite
	//  timeout), 0 or a positive integer less than or equal to Int32.MaxValue."
	if (delay.TotalMilliseconds < -1 || delay.TotalMilliseconds > Int32.MaxValue) break;
	await Task.Delay(delay, cancellation.Token);
```

## Many packages comes with cron-expression-descriptor
When [cron-expression-descriptor](https://www.nuget.org/packages/CronExpressionDescriptor) Nuget package is installed, many dependent packages must be confirmed for installation.

Solution found: Update .Net framework for the project from (for example) 4.5.2 to 4.7.2

Documentation : https://docs.microsoft.com/fr-fr/dotnet/standard/net-standard

## CreateProcessAsUser does not work with VB .Net from a Windows Service
Solution found: use C# instead!

# How to set local time instead of UTC time?
In CronDaemon.cs, use DateTime.Now instead of SystemTime.Now here:
```c#
//var now = SystemTime.Now; // System time (UTC)
var now = DateTime.Now; // Local time
```
Therefore, the times specified in cron are local times instead of UTC (but the disadvantage of local time is that for the winter / summer time change, tasks will not be scheduled between 2 and 3 am, and will be scheduled twice at the summer / winter time change).

There is a boolean in the code: CronDaemon.useLocalTime: set true if the change of winter / summer time is not a problem, otherwise set false to program all dates in UTC, the planning displays both local time and UTC time in this case.

# Documentation

https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessasusera

https://docs.microsoft.com/fr-fr/archive/blogs/winsdk/launching-an-interactive-process-from-windows-service-in-windows-vista-and-later

https://techcommunity.microsoft.com/t5/ask-the-performance-team/app-application-compatibility-session-0-isolation-windows-vista/ba-p/373687

System.ServiceProcess.ServiceInstaller.StartType : Manual
https://docs.microsoft.com/fr-fr/dotnet/api/system.serviceprocess.serviceinstaller.starttype?view=netframework-4.8

https://docs.microsoft.com/fr-fr/dotnet/api/system.serviceprocess.servicestartmode?view=netframework-4.8

System.ServiceProcess.ServiceProcessInstaller.Account : LocalSystem
https://docs.microsoft.com/fr-fr/dotnet/api/system.serviceprocess.serviceaccount?view=netframework-4.8

https://stackoverflow.com/questions/49025941/how-to-create-process-as-user-with-arguments