using System.Threading.Tasks;
using Quartz;
using Serilog;

namespace ScheduledTask
{
    /// <summary>
    ///   This the scheduled job that will execute according to the trigger
    /// </summary>
    public class Job : IJob
    {
        /// <summary>
        ///   Gets the job details.
        /// </summary>
        public static IJobDetail JobDetail => JobBuilder.Create<Job>()
                .WithIdentity("job", "group")
                .Build();

        /// <summary>
        ///   Gets the trigger for this job
        /// </summary>
        public static ITrigger Trigger => TriggerBuilder.Create()
                .WithIdentity("trigger", "group")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(1)
                    .RepeatForever())
                .Build();

        /// <summary>
        ///   Called by the <see cref="T:Quartz.IScheduler" /> when a <see cref="T:Quartz.ITrigger"
        ///   /> fires that is associated with the <see cref="T:Quartz.IJob" />.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <returns>That the task completed</returns>
        public Task Execute(IJobExecutionContext context)
        {
            var lastRun = context.PreviousFireTimeUtc?.LocalDateTime.ToString() ?? string.Empty;

            Log.Warning("The Job is executing: Previous run: {lastRun}", lastRun);

            // Do the work here

            return Task.CompletedTask;
        }
    }
}
