param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string]$MSBuildPath
)

$solutionPath = Join-Path $PSScriptRoot "OpenTyrianDotNet.sln"

function Resolve-MSBuildPath {
    param(
        [string]$RequestedPath
    )

    if ($RequestedPath) {
        if (Test-Path $RequestedPath) {
            return $RequestedPath
        }

        throw "MSBuild not found: $RequestedPath"
    }

    $vswherePath = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswherePath) {
        $resolved = & $vswherePath -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" |
            Select-Object -First 1
        if ($resolved) {
            return $resolved
        }
    }

    $candidatePaths = @(
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe"
    )

    foreach ($candidatePath in $candidatePaths) {
        if (Test-Path $candidatePath) {
            return $candidatePath
        }
    }

    $command = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    throw "MSBuild not found. Install Visual Studio Build Tools or pass -MSBuildPath."
}

$resolvedMSBuildPath = Resolve-MSBuildPath -RequestedPath $MSBuildPath

& $resolvedMSBuildPath $solutionPath /restore /t:Build /p:Configuration=$Configuration /p:RestoreIgnoreFailedSources=true /m:1 /v:minimal

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
