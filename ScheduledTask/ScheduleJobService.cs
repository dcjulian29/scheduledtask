using System.Collections.Specialized;
using Quartz;
using Quartz.Impl;

namespace ScheduledTask
{
    /// <summary>
    ///   ScheduleJob Windows Service
    /// </summary>
    public class ScheduleJobService
    {
        private readonly IScheduler _scheduler;

        /// <summary>
        ///   Initializes a new instance of the <see cref="ScheduleJobService" /> class.
        /// </summary>
        public ScheduleJobService()
        {
            var props = new NameValueCollection
            {
                { "quartz.serializer.type", "binary" },
                { "quartz.scheduler.instanceName", "ScheduledJob" },
                { "quartz.jobStore.type", "Quartz.Simpl.RAMJobStore, Quartz" },
                { "quartz.threadPool.threadCount", "3" }
            };

            var factory = new StdSchedulerFactory(props);

            _scheduler = factory.GetScheduler().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        ///   Starts the scheduled job service.
        /// </summary>
        public void Start()
        {
            _scheduler
                .ScheduleJob(Job.JobDetail, Job.Trigger)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            _scheduler
                .Start()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        ///   Stops the scheduled job service.
        /// </summary>
        public void Stop()
        {
            _scheduler
                .Shutdown()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
    }
}
