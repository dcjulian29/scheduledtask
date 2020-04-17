using System;
using System.Reflection;
using Serilog;
using Topshelf;

namespace ScheduledTask
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .CreateLogger();

            var assembly = Assembly.GetEntryAssembly().GetName();

            Log.Information($"{assembly.Name} {assembly.Version} initialized on {Environment.MachineName}");

            var rc = HostFactory.Run(x =>
            {
                x.Service<ScheduleJobService>(s =>
                {
                    s.ConstructUsing(name => new ScheduleJobService());
                    s.WhenStarted(j => j.Start());
                    s.WhenStopped(j => j.Stop());
                });

                x.UseSerilog(Log.Logger);

                x.RunAsLocalSystem();

                x.SetDescription($"Service Runtime for {assembly.Name} {assembly.Version}");
                x.SetDisplayName($"{assembly.Name} Service");
                x.SetServiceName(assembly.Name);
            });

            Environment.ExitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
        }
    }
}
