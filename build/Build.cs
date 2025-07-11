using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

// Nuke Build Documentation: https://nuke.build/docs/introduction/

[GitHubActions(
    "continuous",
    GitHubActionsImage.WindowsLatest,
    FetchDepth = 0,
    EnableGitHubToken = true,
    ImportSecrets = [nameof(NuGetOrgApiKey)],
    OnPushBranches = ["main"],
    OnWorkflowDispatchOptionalInputs = ["name"],
    InvokedTargets = [nameof(ShowInfo), nameof(Publish)])]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>();
    
    [Parameter("Override build configuration for a Debug build")]
    readonly bool Debug = false;

    [Parameter("Override build configuration for a Release build")]
    readonly bool Release = false;

    [Parameter("NuGet.org Token Secret"), Secret]
    readonly string NuGetOrgApiKey = string.Empty;

    //[Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    Configuration Configuration = GetDefaultConfiguration();

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion] readonly GitVersion GitVersion;

    AbsolutePath SourceDirectory => RootDirectory / "Src";
    AbsolutePath OutputDirectory => RootDirectory / "Output";
    AbsolutePath LocalNuGetSourceDirectory => @"C:\Fls\Local-NuGet";

    string NugetOrgFeed => "https://api.nuget.org/v3/index.json";
    static GitHubActions GitHubActions => GitHubActions.Instance;
    string GithubNugetFeed => GitHubActions != null
        ? $"https://nuget.pkg.github.com/{GitHubActions.RepositoryOwner}/index.json"
        : null;
    
    Target ShowInfo => _ => _
        .Before(Clean)
        .Before(Restore)
        .Executes(() =>
        {
            string LocalOrRemoteText() => IsLocalBuild ? "Local Build" : "Remote (CI/CD) Build";

            Log.Information("Standard GitVersion Formats:");
            Log.Information($"                         SemVer: {GitVersion.SemVer}");
            Log.Information($"                     FullSemVer: {GitVersion.FullSemVer}");
            Log.Information($"                   LegacySemVer: {GitVersion.LegacySemVer}");
            Log.Information($"             LegacySemVerPadded: {GitVersion.LegacySemVerPadded}\n");

            Log.Information("Standard GitVersion Assembly Formats:");
            Log.Information($"                 AssemblySemVer: {GitVersion.AssemblySemVer}");
            Log.Information($"             AssemblySemFileVer: {GitVersion.AssemblySemFileVer}");
            Log.Information($"           InformationalVersion: {GitVersion.InformationalVersion}\n");

            Log.Information("Standard GitVersion NuGet Formats:");
            Log.Information($"                   NuGetVersion: {GitVersion.NuGetVersion}");
            Log.Information($"                 NuGetVersionV2: {GitVersion.NuGetVersionV2}");
            Log.Information($"             NuGetPreReleaseTag: {GitVersion.NuGetPreReleaseTag}");
            Log.Information($"           NuGetPreReleaseTagV2: {GitVersion.NuGetPreReleaseTagV2}\n");

            Log.Information("Other GitVersion Information:");
            Log.Information($"                     BranchName: {GitVersion.BranchName}");
            Log.Information($"                  BuildMetaData: {GitVersion.BuildMetaData}");
            Log.Information($"            BuildMetaDataPadded: {GitVersion.BuildMetaDataPadded}");
            Log.Information($"      CommitsSinceVersionSource: {GitVersion.CommitsSinceVersionSource}");
            Log.Information($"CommitsSinceVersionSourcePadded: {GitVersion.CommitsSinceVersionSourcePadded}");
            Log.Information($"                     CommitDate: {GitVersion.CommitDate}");
            Log.Information($"             UncommittedChanges: {GitVersion.UncommittedChanges}");
            Log.Information($"               VersionSourceSha: {GitVersion.VersionSourceSha}");
            Log.Information($"                            Sha: {GitVersion.Sha}");
            Log.Information($"                       ShortSha: {GitVersion.ShortSha}\n");

            Log.Information("Build Configuration Information:");
            Log.Information($"  Local or Remote (CI/CD) Build: {LocalOrRemoteText()}");
            //Log.Information($"        Configuration by Branch: {GetBranchBasedConfiguration()}");
            Log.Information($"            Final Configuration: {Configuration}\n");

            Log.Information($"Generic Version Information:");
            Log.Information($"                MajorMinorPatch: {GitVersion.MajorMinorPatch}");
            Log.Information($"                  PreReleaseTag: {GitVersion.PreReleaseTag}");
            Log.Information($"                PreReleaseLabel: {GitVersion.PreReleaseLabel}");
            Log.Information($"               PreReleaseNumber: {GitVersion.PreReleaseNumber}");
            Log.Information($"       WeightedPreReleaseNumber: {GitVersion.WeightedPreReleaseNumber}");
            Log.Information($"              FullBuildMetaData: {GitVersion.FullBuildMetaData}");
        });


    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(path => path.DeleteDirectory());
            OutputDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution)
            );
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .SetCopyright(BuildCopyright())

                .SetVersion(GetSafeNuGetVersion())
                //.SetVersionPrefix(GitVersion.MajorMinorPatch)
                //.SetVersionSuffix(GitVersion.PreReleaseTag)

                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .AddProperty("IncludeSourceRevisionInInformationalVersion", Configuration != Configuration.Release));
        });

    Target UnitTest => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(x => x
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild()
            );
        });

    Target Pack => _ => _
        .DependsOn(Clean)
        .DependsOn(UnitTest)
        //.Produces(OutputDirectory / "*.nupkg")
        .Executes(() =>
        {
            DotNetPack(cfg => cfg
                .SetProject(Solution.GetProject("TidyData")?.Path ?? "<Project Not Found>")
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild()
                .SetCopyright(BuildCopyright())

                .SetVersion(GetSafeNuGetVersion())
                //.SetVersionPrefix(GitVersion.MajorMinorPatch)
                //.SetVersionSuffix(GitVersion.PreReleaseTag)

                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .AddProperty("IncludeSourceRevisionInInformationalVersion", Configuration != Configuration.Release)
                .SetOutputDirectory(OutputDirectory)
            );
        });

    Target PublishToLocalNuGet => _ => _
        .Description($"Publishing Tools Package to a local NuGet Feed Folder.")
        .DependsOn(Pack)
        .OnlyWhenDynamic(() => IsLocalBuild)
        .Executes(() =>
        {
            LocalNuGetSourceDirectory.CreateDirectory();
            OutputDirectory.GlobFiles("*.nupkg")
                .ForEach(pkgFile => File.Copy(pkgFile, LocalNuGetSourceDirectory / pkgFile.Name, true));
        });

    // Examples at: https://anktsrkr.github.io/post/manage-your-package-release-using-nuke-in-github/
    // GitHub NuGet Hosting: https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry
    // New: https://blog.raulnq.com/github-packages-publishing-nuget-packages-using-nuke-with-gitversion-and-github-actions
    Target PublishToNuGetOrg => _ => _
        .Description($"Publishing Tools Package to the public Github NuGet Feed.")
        .DependsOn(Pack)
        .OnlyWhenDynamic(() => IsServerBuild && Configuration.Equals(Configuration.Release))
        .Executes(() =>
        {
            OutputDirectory.GlobFiles("*.nupkg")
                .ForEach(pkgFile =>
                {
                    DotNetNuGetPush(cfg => cfg
                        .SetTargetPath(pkgFile)
                        .SetSource(NugetOrgFeed)
                        .SetApiKey(NuGetOrgApiKey)
                        .EnableSkipDuplicate()
                    );
                });
        });

    Target PublishGitHubNuGet => _ => _
        .Description($"Publishing Tools Package to a private Github NuGet Feed.")
        .DependsOn(Pack)
        .OnlyWhenDynamic(() => IsServerBuild && Configuration.Equals(Configuration.Release))
        .Executes(() =>
        {
            OutputDirectory.GlobFiles("*.nupkg")
                .ForEach(pkgFile =>
                {
                    DotNetNuGetPush(cfg => cfg
                        .SetTargetPath(pkgFile)
                        .SetSource(GithubNugetFeed)
                        .SetApiKey(GitHubActions.Token)
                        .EnableSkipDuplicate()
                    );
                });
        });
		
    Target Publish => _ => _
        .Description("Publish NuGet Package to location depending on if this is a local or remote server build.")
        .Triggers(PublishToLocalNuGet)
        .Triggers(PublishToNuGetOrg)
        //.Triggers(PublishGitHubNuGet)
        .Executes(() =>
        {
        });

    protected override void OnBuildInitialized()
    {
        base.OnBuildInitialized();

        Configuration = GetConfigurationOverrideParameters() ??
            GetBranchBasedConfiguration();

        Assert.True(Configuration != null,
            "Unable to determine configuration by branch or local override parameter!");
    }

    string BuildCopyright()
    {
        CultureInfo enUS = new CultureInfo("en-US");
        DateTime date = DateTime.ParseExact(GitVersion.CommitDate, "yyyy-MM-dd", enUS, DateTimeStyles.None);
        string copyright = $"Copyright (c) {date.Year} Marc Behnke"
            .Replace(",", HttpUtility.UrlEncode(","));
        return copyright;
    }

    Configuration GetConfigurationOverrideParameters()
    {
        // If this is NOT a local build (e.g. CI Server), command line overrides are not allowed.
        if (IsLocalBuild)
        {
            Assert.True(!Debug || !Release,
                $"Build parameters for {nameof(Debug)} and {nameof(Release)} configurations cannot both be set!");
            return Debug
                ? Configuration.Debug
                : (Release ? Configuration.Release : null);
        }

        return null;
    }

    string GetSafeNuGetVersion()
    {
        // Sometimes NuGetVersionV2 is blank - GitVersion bug.
        return !string.IsNullOrWhiteSpace(GitVersion.NuGetVersionV2)
            ? GitVersion.NuGetVersionV2
            : GitVersion.FullSemVer?.Split('+')[0] ?? throw new Exception("Cannot determine NuGet version.");
    }

    private Configuration GetBranchBasedConfiguration()
    {
        return GitVersion.BranchName switch
        {
            "develop" => Configuration.Debug,
            string s when s.StartsWith("feature/") => Configuration.Debug,

            "main" => Configuration.Release,
            string s when s.StartsWith("release/") => Configuration.Release,
            string s when s.StartsWith("hotfix/") => Configuration.Release,

            null => throw new Exception("Unable to determine the git branch!"),
            string unrecognizedBranch => throw new Exception(
                $"Unable to determine build configuration from branch name \"{unrecognizedBranch}\""),
        };
    }
	
    static Configuration GetDefaultConfiguration()
    {
        return IsLocalBuild ? Configuration.Debug : Configuration.Release;
    }
}
