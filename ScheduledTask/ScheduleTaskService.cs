using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
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
                                "Loaded {0} [v{1}]",
                                type.FullName,
                                versionInfo.FileVersion);

                            var jobDetail = job.JobDetail;

                            _scheduler
                                .ScheduleJob(jobDetail, job.Trigger)
                                .ConfigureAwait(false)
                                .GetAwaiter()
                                .GetResult();

                            Log.Debug("-- Initialize Job: '{0}', class={1}", jobDetail.Key, jobDetail.JobType);

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
                .PauseAll()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            foreach (var group in _scheduler.GetJobGroupNames().Result)
            {
                foreach (var key in _scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupContains(group)).Result)
                {
                    var jobDetail = _scheduler.GetJobDetail(key).Result;

                    Log.Debug("-- Shutdown Job: '{0}', class={1}", jobDetail.Key, jobDetail.JobType);
                    var job = (IScheduledJob)Activator.CreateInstance(jobDetail.JobType);

                    job.Shutdown();
                }
            }

            _scheduler
                .Shutdown()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
    }
}
