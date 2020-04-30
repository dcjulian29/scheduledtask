#tool "nuget:?package=GitVersion.CommandLine&version=3.6.5"

var target = Argument("target", "Default");

var configuration = Argument("configuration", "Debug");

if ((target == "Default") && ((TeamCity.IsRunningOnTeamCity) || (AppVeyor.IsRunningOnAppVeyor))) {
    configuration = "Release";
}

var projectName = "ScheduledTask";

var baseDirectory = MakeAbsolute(Directory("."));

var buildDirectory = baseDirectory + "\\.build";
var outputDirectory = buildDirectory + "\\output";

var gitversion = GitVersion(new GitVersionSettings{
    UpdateAssemblyInfo = false,
    OutputType = GitVersionOutput.Json
});

var version = String.Format("{0}.{1}", gitversion.MajorMinorPatch, gitversion.CommitsSinceVersionSource);
var packageId = gitversion.Sha.Substring(0, 8);
var branch = gitversion.BranchName;

if (AppVeyor.IsRunningOnAppVeyor) {
    if (branch != "master") {
        AppVeyor.UpdateBuildVersion($"{version}-{branch}.{packageId}");
    } else {
        AppVeyor.UpdateBuildVersion(version);
    }
}

var dotNetCoreBuildSettings = new DotNetCoreBuildSettings {
    Configuration = configuration,
    OutputDirectory = outputDirectory,
    MSBuildSettings = new DotNetCoreMSBuildSettings {
        TreatAllWarningsAs = MSBuildTreatAllWarningsAs.Error,
        Verbosity = DotNetCoreVerbosity.Normal
    },
    NoDependencies = true,
    NoIncremental = true,
    NoRestore = true
};

var restoreSettings = new DotNetCoreRestoreSettings { NoDependencies = true };

Setup(setupContext =>
{
    if (setupContext.TargetTask.Name == "Package")
    {
        Information("Switching to Release Configuration for packaging...");
        configuration = "Release";

        dotNetCoreBuildSettings.Configuration = "Release";
    }
});

TaskSetup(setupContext =>
{
    if (TeamCity.IsRunningOnTeamCity)
    {
        TeamCity.WriteStartBuildBlock(setupContext.Task.Description ?? setupContext.Task.Name);
        TeamCity.WriteStartProgress(setupContext.Task.Description ?? setupContext.Task.Name);
    }
});

TaskTeardown(teardownContext =>
{
    if (TeamCity.IsRunningOnTeamCity)
    {
        TeamCity.WriteEndBuildBlock(teardownContext.Task.Description ?? teardownContext.Task.Name);
        TeamCity.WriteEndProgress(teardownContext.Task.Description ?? teardownContext.Task.Name);
    }
});

Task("Default")
    .IsDependentOn("Compile");

Task("Clean")
    .Does(() =>
    {
        CleanDirectories(buildDirectory);

        var settings = new DotNetCoreCleanSettings { Configuration = dotNetCoreBuildSettings.Configuration };

        DotNetCoreClean("ScheduledTask/ScheduledTask.csproj", settings);
        DotNetCoreClean("ScheduledTask.Interfaces/ScheduledTask.Interfaces.csproj", settings);
        DotNetCoreClean("TaskHello1/TaskHello1.csproj", settings);
        DotNetCoreClean("TaskHello2/TaskHello2.csproj", settings);
    });

Task("Init")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        CreateDirectory(buildDirectory);
        CreateDirectory(outputDirectory);
    });

Task("Version")
    .IsDependentOn("Init")
    .Does(() =>
    {
        Information("Marking this build as version: " + version);
        Information("                       branch: " + branch);
        Information("                    packageId: " + packageId);

        CreateAssemblyInfo(buildDirectory + @"\CommonAssemblyInfo.cs", new AssemblyInfoSettings {
            Version = gitversion.AssemblySemVer,
            FileVersion = gitversion.AssemblySemFileVer,
            InformationalVersion = gitversion.InformationalVersion,
            Copyright = String.Format("(c) Julian Easterling {0}", DateTime.Now.Year),
            Company = String.Empty,
            Configuration = configuration
        });
    });

Task("Compile")
    .IsDependentOn("Compile.ScheduledTask")
    .IsDependentOn("Compile.TaskHello1")
    .IsDependentOn("Compile.TaskHello2");

Task("Compile.ScheduledTask")
    .IsDependentOn("Compile.Interfaces")
    .Does(() =>
    {
        var settings = dotNetCoreBuildSettings;
        settings.MSBuildSettings.AddFileLogger(
            new MSBuildFileLoggerSettings {
                LogFile = buildDirectory + "/msbuild-ScheduledTask.log" });

        DotNetCoreRestore("ScheduledTask/ScheduledTask.csproj", restoreSettings);
        DotNetCoreBuild("ScheduledTask/ScheduledTask.csproj", settings);
    });

Task("Compile.Interfaces")
    .IsDependentOn("Init")
    .IsDependentOn("Version")
    .Does(() =>
    {
        var settings = dotNetCoreBuildSettings;
        settings.MSBuildSettings.AddFileLogger(
            new MSBuildFileLoggerSettings {
                LogFile = buildDirectory + "/msbuild-Interfaces.log" });

        DotNetCoreRestore("ScheduledTask.Interfaces/ScheduledTask.Interfaces.csproj", restoreSettings);
        DotNetCoreBuild("ScheduledTask.Interfaces/ScheduledTask.Interfaces.csproj", settings);
    });

Task("Compile.TaskHello1")
    .IsDependentOn("Compile.Interfaces")
    .Does(() =>
    {
        var settings = dotNetCoreBuildSettings;
        settings.MSBuildSettings.AddFileLogger(
            new MSBuildFileLoggerSettings {
                LogFile = buildDirectory + "/msbuild-TaskHello1.log" });

        DotNetCoreRestore("TaskHello1/TaskHello1.csproj", restoreSettings);
        DotNetCoreBuild("TaskHello1/TaskHello1.csproj", settings);
    });

Task("Compile.TaskHello2")
    .IsDependentOn("Compile.Interfaces")
    .Does(() =>
    {
        var settings = dotNetCoreBuildSettings;
        settings.MSBuildSettings.AddFileLogger(
            new MSBuildFileLoggerSettings {
                LogFile = buildDirectory + "/msbuild-TaskHello2.log" });

        DotNetCoreRestore("TaskHello2/TaskHello2.csproj", restoreSettings);
        DotNetCoreBuild("TaskHello2/TaskHello2.csproj", settings);
    });

Task("Run")
    .IsDependentOn("Compile")
    .Does(() =>
    {
        var info = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "ScheduledTask.exe",
            Arguments = "",
            WorkingDirectory = outputDirectory
        };

        System.Diagnostics.Process.Start(info);
    });

Task("Package")
    .IsDependentOn("Compile")
    .Does(() =>
    {
        CreateDirectory(buildDirectory + "\\packages");

        var nuGetPackSettings = new NuGetPackSettings {
            NoPackageAnalysis = true,
            Version = version,
            OutputDirectory = buildDirectory + "\\packages"
        };

        var nuspecFiles = GetFiles(baseDirectory + "\\*.nuspec");

        NuGetPack(nuspecFiles, nuGetPackSettings);
    });

RunTarget(target);
