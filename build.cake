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

var solutionFile = String.Format("{0}\\{1}.sln", baseDirectory, projectName);


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

var msbuildSettings = new MSBuildSettings {
    Configuration = configuration,
    ToolVersion = MSBuildToolVersion.VS2019,
    NodeReuse = false,
    WarningsAsError = false
}.WithProperty("OutDir", outputDirectory);

Setup(setupContext =>
{
    if (setupContext.TargetTask.Name == "Package")
    {
        Information("Switching to Release Configuration for packaging...");
        configuration = "Release";

        msbuildSettings.Configuration = "Release";
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
        MSBuild(solutionFile, msbuildSettings.WithTarget("Clean"));
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
    .IsDependentOn("Init")
    .IsDependentOn("Version")
    .Does(() =>
    {
        MSBuild(solutionFile, msbuildSettings.WithTarget("ReBuild"));
    });

Task("Package")
    .IsDependentOn("Compile")
    .Does(() =>
    {
        CreateDirectory(buildDirectory + "\\packages");

        var nuGetPackSettings = new NuGetPackSettings {
            NoPackageAnalysis       = true,
            Version = gitversion.SemVer,
            OutputDirectory = buildDirectory + "\\packages"
        };

        var nuspecFiles = GetFiles(baseDirectory + "\\*.nuspec");

        NuGetPack(nuspecFiles, nuGetPackSettings);
    });

RunTarget(target);
