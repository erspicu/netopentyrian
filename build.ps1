param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string]$MSBuildPath
)

$solutionPath = Join-Path $PSScriptRoot "OpenTyrianDotNet.sln"
$nativeProjectPath = Join-Path $PSScriptRoot "native\OpenTyrian.NativeMusic\OpenTyrian.NativeMusic.vcxproj"
$managedOutputPath = Join-Path $PSScriptRoot ("src\OpenTyrian.WinForms\bin\{0}\net40" -f $Configuration)
$nativeOutputPath = Join-Path $PSScriptRoot ("native\OpenTyrian.NativeMusic\bin\{0}\Win32" -f $Configuration)
$seedSaveSourcePath = Join-Path $PSScriptRoot "opentyrian-master\visualc\build\x64\Release\tyrian.sav"

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

if (Test-Path $nativeProjectPath) {
    & $resolvedMSBuildPath $nativeProjectPath /t:Build /p:Configuration=$Configuration /p:Platform=Win32 /m:1 /v:minimal

    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

& $resolvedMSBuildPath $solutionPath /restore /t:Build /p:Configuration=$Configuration /p:RestoreIgnoreFailedSources=true /m:1 /v:minimal

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if (Test-Path (Join-Path $nativeOutputPath "OpenTyrian.NativeMusic.dll")) {
    Copy-Item (Join-Path $nativeOutputPath "OpenTyrian.NativeMusic.dll") -Destination (Join-Path $managedOutputPath "OpenTyrian.NativeMusic.dll") -Force

    if (Test-Path (Join-Path $nativeOutputPath "OpenTyrian.NativeMusic.pdb")) {
        Copy-Item (Join-Path $nativeOutputPath "OpenTyrian.NativeMusic.pdb") -Destination (Join-Path $managedOutputPath "OpenTyrian.NativeMusic.pdb") -Force
    }
}

if (Test-Path $seedSaveSourcePath) {
    Copy-Item $seedSaveSourcePath -Destination (Join-Path $managedOutputPath "tyrian.sav") -Force
}


