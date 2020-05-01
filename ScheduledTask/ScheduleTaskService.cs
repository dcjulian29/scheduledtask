using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Quartz;
using Quartz.Impl;
using ScheduledTask.Interfaces;
using Serilog;

namespace ScheduledTask
{
    /// <summary>
    ///   ScheduleJob Windows Service
    /// </summary>
    public class ScheduleTaskService
    {
        private readonly IScheduler _scheduler;

        /// <summary>
        ///   Initializes a new instance of the <see cref="ScheduleTaskService" /> class.
        /// </summary>
        public ScheduleTaskService()
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
            var jobLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            if (Directory.Exists(Path.Combine(jobLocation, "jobs")))
            {
                jobLocation = Path.Combine(jobLocation, "jobs");
            }

            Log.Debug("Will look in '{0}' for jobs...", jobLocation);

            var files = Directory.GetFiles(jobLocation, "*.dll");

            if (files.Length > 0)
            {
                var interfaceType = typeof(IScheduledJob);
                var assemblies = files.Select(AssemblyLoadContext.Default.LoadFromAssemblyPath);

                foreach (var assembly in assemblies.ToList())
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if ((type != interfaceType) && interfaceType.IsAssignableFrom(type))
                        {
                            var job = (IScheduledJob)Activator.CreateInstance(type);
                            var versionInfo = FileVersionInfo.GetVersionInfo(type.Assembly.Location);

                            Log.Debug(
                                "Loaded {0}::{1} [v{2}]",
                                type.AssemblyQualifiedName,
                                type.FullName,
                                versionInfo.FileVersion);

                            _scheduler
                                .ScheduleJob(job.JobDetail, job.Trigger)
                                .ConfigureAwait(false)
                                .GetAwaiter()
                                .GetResult();

                            job.Initialize();
                        }
                    }
                }
            }

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
