using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Serilog;
using Topshelf;

namespace ScheduledTask
{
    internal static class Program
    {
        private static ScheduleTaskService _service;

        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .CreateLogger();

            var assembly = Assembly.GetEntryAssembly().GetName();

            Log.Information($"{assembly.Name} {assembly.Version} initialized on {Environment.MachineName}");

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Log.Debug("Running on Non-Windows. Running as console style application.");

                _service = new ScheduleTaskService();
                _service.Start();

                AssemblyLoadContext.Default.Unloading += SigTermEventHandler;
                Console.CancelKeyPress += CancelHandler;

                while (true)
                {
                    Console.Read();
                }
            }
            else
            {
                Log.Debug("Running on Windows... Using TopSelf to handle service.");

                HostFactory.Run(x =>
                {
                    x.Service<ScheduleTaskService>(s =>
                    {
                        s.ConstructUsing(name => new ScheduleTaskService());
                        s.WhenStarted(j => j.Start());
                        s.WhenStopped(j => j.Stop());
                    });

                    x.UseSerilog(Log.Logger);

                    x.RunAsLocalSystem();

                    x.SetDescription($"Service Runtime for {assembly.Name} {assembly.Version}");
                    x.SetDisplayName($"{assembly.Name} Service");
                    x.SetServiceName(assembly.Name);
                });
            }
        }

        private static void CancelHandler(object sender, ConsoleCancelEventArgs e)
        {
            Log.Information("Shutting down...");
            _service.Stop();
        }

        private static void SigTermEventHandler(AssemblyLoadContext context)
        {
            Log.Information("Caught SigTerm... Shutting down...");
            _service.Stop();
        }
    }
}
