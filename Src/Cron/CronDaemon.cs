using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NCrontab;

namespace CronScheduling
{
	/// <summary>
	/// Cron scheduling daemon.
	/// </summary>
	public sealed class CronDaemon<T> : IDisposable
	{
		private readonly Action<T> _execute;
		private readonly Func<T, T> _fork;
		private readonly List<CancellationTokenSource> _cancellations = new List<CancellationTokenSource>();

		/// <summary>
		/// Initializes new instance of <see cref="CronDaemon{T}"/>.
		/// </summary>
		/// <param name="execute">The job handler.</param>
		/// <param name="fork">The function to fork job instance on every recurrence.</param>
		public CronDaemon(Action<T> execute, Func<T,T> fork = null)
		{
			if (execute == null) throw new ArgumentNullException("execute");

			_execute = execute;
			_fork = fork ?? DefaultFork;
		}

		private static T DefaultFork(T item)
		{
			var cloneable = item as ICloneable;
			if (cloneable != null) return (T) cloneable.Clone();
			return item;
		}

		public void Dispose()
		{
			foreach (var cancellation in _cancellations)
			{
				cancellation.Cancel(false);
			}
			_cancellations.Clear();
		}

		/// <summary>
		/// Adds specified job to <see cref="CronDaemon{T}"/> queue with given cron expression and maximum number of repetitions.
		/// </summary>
		/// <param name="job">The job definition.</param>
		/// <param name="cronExpression">Specifies cron expression.  This will be evaluated in UTC time standard.</param>
		/// <param name="repeatCount">Specifies maximum number of job recurrence.</param>
		public void Add(T job, string cronExpression, int repeatCount)
		{
			if (repeatCount < 0) throw new ArgumentOutOfRangeException("repeatCount");
			if (repeatCount == 0) repeatCount = 1;

			var crontab = CrontabSchedule.Parse(cronExpression);

			var cancellation = new CancellationTokenSource();

			Func<DateTime, DateTime?> schedule = time =>
			{
				if (cancellation.IsCancellationRequested) return null;

				repeatCount--;

				// 0 means once like in java quartz
				if (repeatCount >= 0)
				{
					return crontab.GetNextOccurrence(time);
				}

				return null;
			};

			Action run = async () =>
			{
				while (true)
				{

					//var now = SystemTime.Now; // System time (UTC)
					//var now = DateTime.Now; // Local time
					DateTime now;
#pragma warning disable CS0162 // Unreachable code detected
					if (CronDaemon.useLocalTime) now = DateTime.Now; else now = SystemTime.Now;
#pragma warning restore CS0162 // Unreachable code detected

					var nextOccurrence = schedule(now);
					if (nextOccurrence == null) break;

					var delay = nextOccurrence.Value - now;
					// Bug fix: "The value needs to translate in milliseconds to -1 (signifying an infinite
					//  timeout), 0 or a positive integer less than or equal to Int32.MaxValue."
					if (delay.TotalMilliseconds < -1 || delay.TotalMilliseconds > Int32.MaxValue) break;
					await Task.Delay(delay, cancellation.Token);

					if (cancellation.IsCancellationRequested) break;

					_execute(_fork(job));	
				}
			};

			Task.Run(run, cancellation.Token);

			_cancellations.Add(cancellation);
		}

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
					
					//var now = SystemTime.Now; // System time (UTC)
					//var now = DateTime.Now; // Local time
					DateTime now;
#pragma warning disable CS0162 // Unreachable code detected
					if (CronDaemon.useLocalTime) now = DateTime.Now; else now = SystemTime.Now;
#pragma warning restore CS0162 // Unreachable code detected

					var nextOccurrence = schedule(now);
					if (nextOccurrence == null) break;
					
					var delay = nextOccurrence.Value - now;
					// Bug fix: "The value needs to translate in milliseconds to -1 (signifying an infinite
					//  timeout), 0 or a positive integer less than or equal to Int32.MaxValue."
					if (delay.TotalMilliseconds < -1 || delay.TotalMilliseconds > Int32.MaxValue) break;
					await Task.Delay(delay, cancellation.Token);

					if (cancellation.IsCancellationRequested) break;

					_execute(_fork(job));
				}
			};

			Task.Run(run, cancellation.Token);

			_cancellations.Add(cancellation);
		}
	}

	public static class CronDaemon
	{
		/// <summary>
		/// Use local time instead of UTC time: the disadvantage of local time is that
		///  for the winter -> summer time change, tasks will not be scheduled between
		///  2 and 3 am, and will be scheduled twice at the summer -> winter time change
		/// </summary>
		internal static bool useLocalTime=true;

		public static CronDaemon<T> Start<T>(Action<T> execute, Func<T, T> fork = null)
		{
			if (execute == null) throw new ArgumentNullException("execute");

			return new CronDaemon<T>(execute, fork);
		}
	}
}
