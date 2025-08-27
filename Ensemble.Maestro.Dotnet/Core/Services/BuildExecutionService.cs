using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Ensemble.Maestro.Dotnet.Core.Services;

/// <summary>
/// Service for executing actual build operations on aggregated code files
/// Supports multiple programming languages and provides detailed error reporting
/// </summary>
public class BuildExecutionService : IBuildExecutionService
{
    private readonly ILogger<BuildExecutionService> _logger;

    public BuildExecutionService(ILogger<BuildExecutionService> logger)
    {
        _logger = logger;
    }

    public async Task<BuildExecutionResult> ExecuteBuildAsync(string buildDirectory, string primaryLanguage, 
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("🔨 Starting build for {Language} in directory: {Directory}", 
            primaryLanguage, buildDirectory);

        try
        {
            return primaryLanguage.ToLower() switch
            {
                "csharp" or "c#" => await ExecuteCSharpBuildAsync(buildDirectory, cancellationToken),
                "typescript" or "javascript" => await ExecuteTypeScriptBuildAsync(buildDirectory, cancellationToken),
                "python" => await ExecutePythonBuildAsync(buildDirectory, cancellationToken),
                "java" => await ExecuteJavaBuildAsync(buildDirectory, cancellationToken),
                _ => await ExecuteGenericBuildAsync(buildDirectory, primaryLanguage, cancellationToken)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Build execution failed for {Language}", primaryLanguage);
            
            return new BuildExecutionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                BuildOutput = ex.ToString(),
                BuildDurationSeconds = (int)(DateTime.UtcNow - startTime).TotalSeconds,
                BuildCompleted = DateTime.UtcNow,
                ErrorDetails = new List<BuildError>
                {
                    new BuildError
                    {
                        ErrorType = "BuildSystemError",
                        ErrorMessage = ex.Message,
                        Severity = 10
                    }
                }
            };
        }
    }

    private async Task<BuildExecutionResult> ExecuteCSharpBuildAsync(string buildDirectory, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        
        // Create a simple .csproj file
        var csprojContent = GenerateCSharpProjectFile();
        var csprojPath = Path.Combine(buildDirectory, "GeneratedProject.csproj");
        await File.WriteAllTextAsync(csprojPath, csprojContent, cancellationToken);

        // Execute dotnet build
        var result = await ExecuteProcessAsync("dotnet", $"build \"{csprojPath}\"", buildDirectory, cancellationToken);
        
        var buildResult = new BuildExecutionResult
        {
            Success = result.ExitCode == 0,
            BuildOutput = result.Output + "\n" + result.Error,
            BuildDurationSeconds = (int)(DateTime.UtcNow - startTime).TotalSeconds,
            BuildCompleted = DateTime.UtcNow
        };

        if (!buildResult.Success)
        {
            buildResult.ErrorMessage = "C# build failed";
            buildResult.ErrorDetails = ParseCSharpBuildErrors(result.Output + "\n" + result.Error);
        }
        else
        {
            // Look for generated artifacts
            var binPath = Path.Combine(buildDirectory, "bin");
            if (Directory.Exists(binPath))
            {
                buildResult.GeneratedArtifacts = Directory.GetFiles(binPath, "*", SearchOption.AllDirectories).ToList();
            }
        }

        return buildResult;
    }

    private async Task<BuildExecutionResult> ExecuteTypeScriptBuildAsync(string buildDirectory, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        
        // Create package.json and tsconfig.json
        var packageJsonContent = GeneratePackageJsonFile();
        var tsconfigContent = GenerateTsConfigFile();
        
        await File.WriteAllTextAsync(Path.Combine(buildDirectory, "package.json"), packageJsonContent, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(buildDirectory, "tsconfig.json"), tsconfigContent, cancellationToken);

        // Execute npm install and tsc
        var npmResult = await ExecuteProcessAsync("npm", "install", buildDirectory, cancellationToken);
        if (npmResult.ExitCode != 0)
        {
            return new BuildExecutionResult
            {
                Success = false,
                ErrorMessage = "npm install failed",
                BuildOutput = npmResult.Output + "\n" + npmResult.Error,
                BuildDurationSeconds = (int)(DateTime.UtcNow - startTime).TotalSeconds,
                BuildCompleted = DateTime.UtcNow,
                ErrorDetails = new List<BuildError>
                {
                    new BuildError { ErrorType = "DependencyError", ErrorMessage = "npm install failed", Severity = 8 }
                }
            };
        }

        var tscResult = await ExecuteProcessAsync("npx", "tsc", buildDirectory, cancellationToken);
        
        var buildResult = new BuildExecutionResult
        {
            Success = tscResult.ExitCode == 0,
            BuildOutput = npmResult.Output + "\n" + tscResult.Output + "\n" + tscResult.Error,
            BuildDurationSeconds = (int)(DateTime.UtcNow - startTime).TotalSeconds,
            BuildCompleted = DateTime.UtcNow
        };

        if (!buildResult.Success)
        {
            buildResult.ErrorMessage = "TypeScript build failed";
            buildResult.ErrorDetails = ParseTypeScriptBuildErrors(tscResult.Output + "\n" + tscResult.Error);
        }

        return buildResult;
    }

    private async Task<BuildExecutionResult> ExecutePythonBuildAsync(string buildDirectory, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        
        // For Python, we'll do a syntax check
        var pythonFiles = Directory.GetFiles(buildDirectory, "*.py");
        var allOutput = "";
        var errors = new List<BuildError>();
        var success = true;

        foreach (var file in pythonFiles)
        {
            var result = await ExecuteProcessAsync("python", $"-m py_compile \"{file}\"", buildDirectory, cancellationToken);
            allOutput += result.Output + "\n" + result.Error + "\n";
            
            if (result.ExitCode != 0)
            {
                success = false;
                errors.AddRange(ParsePythonBuildErrors(result.Error, Path.GetFileName(file)));
            }
        }

        return new BuildExecutionResult
        {
            Success = success,
            ErrorMessage = success ? null : "Python syntax check failed",
            BuildOutput = allOutput,
            BuildDurationSeconds = (int)(DateTime.UtcNow - startTime).TotalSeconds,
            BuildCompleted = DateTime.UtcNow,
            ErrorDetails = errors
        };
    }

    private async Task<BuildExecutionResult> ExecuteJavaBuildAsync(string buildDirectory, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        
        var javaFiles = Directory.GetFiles(buildDirectory, "*.java");
        if (!javaFiles.Any())
        {
            return new BuildExecutionResult
            {
                Success = false,
                ErrorMessage = "No Java files found",
                BuildDurationSeconds = 0,
                BuildCompleted = DateTime.UtcNow
            };
        }

        var fileList = string.Join(" ", javaFiles.Select(f => $"\"{f}\""));
        var result = await ExecuteProcessAsync("javac", fileList, buildDirectory, cancellationToken);
        
        var buildResult = new BuildExecutionResult
        {
            Success = result.ExitCode == 0,
            BuildOutput = result.Output + "\n" + result.Error,
            BuildDurationSeconds = (int)(DateTime.UtcNow - startTime).TotalSeconds,
            BuildCompleted = DateTime.UtcNow
        };

        if (!buildResult.Success)
        {
            buildResult.ErrorMessage = "Java compilation failed";
            buildResult.ErrorDetails = ParseJavaBuildErrors(result.Output + "\n" + result.Error);
        }

        return buildResult;
    }

    private async Task<BuildExecutionResult> ExecuteGenericBuildAsync(string buildDirectory, string language, CancellationToken cancellationToken)
    {
        _logger.LogWarning("⚠️ Generic build for unsupported language: {Language}", language);
        
        return new BuildExecutionResult
        {
            Success = true, // Assume success for unsupported languages
            BuildOutput = $"Generic build completed for {language}. No specific build validation performed.",
            BuildDurationSeconds = 1,
            BuildCompleted = DateTime.UtcNow,
            ErrorDetails = new List<BuildError>()
        };
    }

    private async Task<ProcessResult> ExecuteProcessAsync(string fileName, string arguments, string workingDirectory, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🚀 Executing: {FileName} {Arguments}", fileName, arguments);
        
        var processStartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();
        
        process.OutputDataReceived += (sender, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (sender, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };
        
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        await process.WaitForExitAsync(cancellationToken);
        
        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            Output = outputBuilder.ToString(),
            Error = errorBuilder.ToString()
        };
    }

    private List<BuildError> ParseCSharpBuildErrors(string buildOutput)
    {
        var errors = new List<BuildError>();
        var lines = buildOutput.Split('\n');
        
        foreach (var line in lines)
        {
            // Parse C# compiler errors: file.cs(line,col): error CS1234: message
            var match = Regex.Match(line, @"(.+\.cs)\((\d+),\d+\):\s+(error|warning)\s+(CS\d+):\s+(.+)");
            if (match.Success)
            {
                errors.Add(new BuildError
                {
                    ErrorType = "CompileError",
                    ErrorMessage = match.Groups[5].Value.Trim(),
                    FileName = Path.GetFileName(match.Groups[1].Value),
                    LineNumber = int.Parse(match.Groups[2].Value),
                    Severity = match.Groups[3].Value == "error" ? 8 : 4
                });
            }
        }
        
        return errors;
    }

    private List<BuildError> ParseTypeScriptBuildErrors(string buildOutput)
    {
        var errors = new List<BuildError>();
        var lines = buildOutput.Split('\n');
        
        foreach (var line in lines)
        {
            // Parse TypeScript errors: file.ts(line,col): error TS1234: message
            var match = Regex.Match(line, @"(.+\.ts)\((\d+),\d+\):\s+(error|warning)\s+(TS\d+):\s+(.+)");
            if (match.Success)
            {
                errors.Add(new BuildError
                {
                    ErrorType = "CompileError",
                    ErrorMessage = match.Groups[5].Value.Trim(),
                    FileName = Path.GetFileName(match.Groups[1].Value),
                    LineNumber = int.Parse(match.Groups[2].Value),
                    Severity = match.Groups[3].Value == "error" ? 8 : 4
                });
            }
        }
        
        return errors;
    }

    private List<BuildError> ParsePythonBuildErrors(string buildOutput, string fileName)
    {
        var errors = new List<BuildError>();
        var lines = buildOutput.Split('\n');
        
        foreach (var line in lines)
        {
            if (line.Contains("SyntaxError") || line.Contains("IndentationError"))
            {
                errors.Add(new BuildError
                {
                    ErrorType = "SyntaxError",
                    ErrorMessage = line.Trim(),
                    FileName = fileName,
                    Severity = 9
                });
            }
        }
        
        return errors;
    }

    private List<BuildError> ParseJavaBuildErrors(string buildOutput)
    {
        var errors = new List<BuildError>();
        var lines = buildOutput.Split('\n');
        
        foreach (var line in lines)
        {
            // Parse Java errors: File.java:line: error: message
            var match = Regex.Match(line, @"(.+\.java):(\d+):\s+(error|warning):\s+(.+)");
            if (match.Success)
            {
                errors.Add(new BuildError
                {
                    ErrorType = "CompileError",
                    ErrorMessage = match.Groups[4].Value.Trim(),
                    FileName = Path.GetFileName(match.Groups[1].Value),
                    LineNumber = int.Parse(match.Groups[2].Value),
                    Severity = match.Groups[3].Value == "error" ? 8 : 4
                });
            }
        }
        
        return errors;
    }

    public async Task<BuildValidationResult> ValidateBuildPrerequisitesAsync(string primaryLanguage, 
        CancellationToken cancellationToken = default)
    {
        var result = new BuildValidationResult { IsValid = true };
        
        var requiredTools = primaryLanguage.ToLower() switch
        {
            "csharp" or "c#" => new[] { "dotnet" },
            "typescript" or "javascript" => new[] { "node", "npm" },
            "python" => new[] { "python" },
            "java" => new[] { "javac", "java" },
            _ => Array.Empty<string>()
        };

        foreach (var tool in requiredTools)
        {
            try
            {
                var processResult = await ExecuteProcessAsync(tool, "--version", Environment.CurrentDirectory, cancellationToken);
                if (processResult.ExitCode == 0)
                {
                    result.AvailableTools.Add($"{tool}: available");
                }
                else
                {
                    result.MissingPrerequisites.Add(tool);
                    result.IsValid = false;
                }
            }
            catch
            {
                result.MissingPrerequisites.Add(tool);
                result.IsValid = false;
            }
        }

        if (!result.IsValid)
        {
            result.ErrorMessage = $"Missing required tools for {primaryLanguage}: {string.Join(", ", result.MissingPrerequisites)}";
        }

        return result;
    }

    private string GenerateCSharpProjectFile()
    {
        return @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Library</OutputType>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>";
    }

    private string GeneratePackageJsonFile()
    {
        return @"{
  ""name"": ""maestro-generated"",
  ""version"": ""1.0.0"",
  ""scripts"": {
    ""build"": ""tsc""
  },
  ""devDependencies"": {
    ""typescript"": ""^5.0.0""
  }
}";
    }

    private string GenerateTsConfigFile()
    {
        return @"{
  ""compilerOptions"": {
    ""target"": ""ES2020"",
    ""module"": ""commonjs"",
    ""outDir"": ""./dist"",
    ""strict"": true,
    ""esModuleInterop"": true
  }
}";
    }

    private class ProcessResult
    {
        public int ExitCode { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}