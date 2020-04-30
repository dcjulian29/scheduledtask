using System.Threading.Tasks;
using Quartz;
using ScheduledTask.Interfaces;
using Serilog;

namespace TaskHello1
{
    /// <summary>
    ///   Sample scheduled task
    /// </summary>
    public class TaskHello : IScheduledJob
    {
        /// <summary>
        ///   Gets the job details.
        /// </summary>
        public IJobDetail JobDetail => JobBuilder.Create<TaskHello>()
                .WithIdentity("TaskHello1", "HelloGroup")
                .Build();

        /// <summary>
        ///   Gets the trigger for this job
        /// </summary>
        public ITrigger Trigger => TriggerBuilder.Create()
                .WithIdentity("Hello1Trigger", "HelloGroup")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(15)
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

            Log.Warning("The Task 1 is executing: Previous run: {lastRun}", lastRun);

            return Task.CompletedTask;
        }
    }
}
