using Quartz;

namespace ScheduledTask.Interfaces
{
    /// <summary>
    ///   This the scheduled job that will execute according to the trigger
    /// </summary>
    public interface IScheduledJob : IJob
    {
        /// <summary>
        ///   Gets the job details.
        /// </summary>
        IJobDetail JobDetail { get; }

        /// <summary>
        ///   Gets the trigger for this job
        /// </summary>
        ITrigger Trigger { get; }
    }
}
