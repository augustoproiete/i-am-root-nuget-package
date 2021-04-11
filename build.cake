#tool "nuget:?package=NuGet.CommandLine&version=5.8.1"

#addin "nuget:?package=Cake.MinVer&version=1.0.1"
#addin "nuget:?package=Cake.Args&version=1.0.0"

var target       = ArgumentOrDefault<string>("target") ?? "pack";
var buildVersion = MinVer(s => s.WithTagPrefix("v").WithDefaultPreReleasePhase("preview"));

Task("clean")
    .Does(() =>
{
    CleanDirectory("./build/artifacts");
});

Task("pack")
    .IsDependentOn("clean")
    .Does(() =>
{
    var releaseNotes = $"https://github.com/augustoproiete/i-am-root-nuget-package/releases/tag/v{buildVersion.Version}";

    var nuspecFile = MakeAbsolute(new FilePath("./src/IAmRoot.nuspec"));

    NuGetPack(nuspecFile, new NuGetPackSettings
    {
        BasePath = nuspecFile.GetDirectory(),
        Version = buildVersion.Version,
        OutputDirectory = "./build/artifacts",
        ReleaseNotes = new[] { releaseNotes },
    });
});

Task("publish")
    .IsDependentOn("pack")
    .Does(context =>
{
    var url =  context.EnvironmentVariable("NUGET_URL");
    if (string.IsNullOrWhiteSpace(url))
    {
        context.Information("No NuGet URL specified. Skipping publishing of NuGet packages");
        return;
    }

    var apiKey =  context.EnvironmentVariable("NUGET_API_KEY");
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        context.Information("No NuGet API key specified. Skipping publishing of NuGet packages");
        return;
    }

    var nugetPushSettings = new DotNetCoreNuGetPushSettings
    {
        Source = url,
        ApiKey = apiKey,
    };

    foreach (var nugetPackageFile in GetFiles("./build/artifacts/*.nupkg"))
    {
        DotNetCoreNuGetPush(nugetPackageFile.FullPath, nugetPushSettings);
    }
});

RunTarget(target);
