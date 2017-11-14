var NUGET_PATH = "./tools/nuget.exe";

var TARGET = Argument("t", "def");
var CONFIG = Argument("c", "Release");
var PROJECT_NAME = "Mek.PasswordUtils";
var PROJECT_SLN = $"./src/{PROJECT_NAME}/{PROJECT_NAME}.sln";
var PROJECT_DIR = $"./src/{PROJECT_NAME}/{PROJECT_NAME}";
var PROJECT_FILE = $"{PROJECT_DIR}/{PROJECT_NAME}.csproj";
var PROJECT_OUTDIR = $"{PROJECT_DIR}/bin/{CONFIG}";
var PROJECT_VERSION = ParseAssemblyInfo($"{PROJECT_DIR}/Properties/AssemblyInfo.cs").AssemblyVersion;
var PROJECT_NUGET_PKG = $"{PROJECT_OUTDIR}/{PROJECT_NAME}.{PROJECT_VERSION}.nupkg";
var PROJECT_NUGET_SYMBOL_PKG = $"{PROJECT_OUTDIR}/{PROJECT_NAME}.{PROJECT_VERSION}.symbols.nupkg";

var LOCAL_NUGET_URL = "http://localhost:81/nuget/def";
var LOCAL_NUGET_APIKEY = "c53b03df0d2e455c96fa4113dcf1e4b6";

string GetEnv(string name, string def) {
	return EnvironmentVariable(name) ?? Argument(name, def);	
}

// create assembly info
// bump major helper
// bump minor helper

Task("spec")
	.Does(() => {
		var exitCode = StartProcess(NUGET_PATH, new ProcessSettings {
			Arguments = "spec",
			WorkingDirectory = PROJECT_DIR
		});

		if(exitCode != 0)
			throw new Exception($"NUGET returned error exit code: '{exitCode}'");
	});

Task("clean")
	.Does(() => {
		MSBuild(PROJECT_SLN, c => c.SetConfiguration(CONFIG).WithTarget("clean"));
		DeleteFiles($"{PROJECT_OUTDIR}/*.nupkg");
	});

Task("restore")
	.Does(() => {
		NuGetRestore(PROJECT_SLN);
	});

Task("build")
	.IsDependentOn("restore")
	.Does(() => {
		MSBuild(PROJECT_SLN, c => c.SetConfiguration(CONFIG));
	});

Task("pack")
	.IsDependentOn("build")
	.Does(() => {
		// pack nuget
		NuGetPack(PROJECT_FILE, new NuGetPackSettings {
			BasePath = PROJECT_DIR,
			OutputDirectory = PROJECT_OUTDIR,
			Properties = new Dictionary<string, string> {
				{ "Configuration", CONFIG }
			}	
		});

		// pack nuget symbols
		NuGetPack(PROJECT_FILE, new NuGetPackSettings {
			BasePath = PROJECT_DIR,
			OutputDirectory = PROJECT_OUTDIR,
			Properties = new Dictionary<string, string> {
				{ "Configuration", CONFIG }
			},
			Symbols = true
		});
	});

Task("pub")
	.IsDependentOn("pack")
	.Does(() => {
		var NUGET_SOURCE = GetEnv("NUGET_SOURCE", LOCAL_NUGET_URL);
		var NUGET_APIKEY = GetEnv("NUGET_APIKEY", LOCAL_NUGET_APIKEY);

		var settings = new NuGetPushSettings {
			Source = NUGET_SOURCE,
			ApiKey = NUGET_APIKEY
		};

		NuGetPush(PROJECT_NUGET_PKG, settings);
		NuGetPush(PROJECT_NUGET_SYMBOL_PKG, settings);
	});

Task("def")
	.IsDependentOn("build")
	.Does(() => {});

RunTarget(TARGET);