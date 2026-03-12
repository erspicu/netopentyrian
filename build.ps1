param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$solutionPath = Join-Path $PSScriptRoot "OpenTyrianDotNet.sln"
$msbuildPath = "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe"

if (-not (Test-Path $msbuildPath)) {
    throw "MSBuild not found: $msbuildPath"
}

& $msbuildPath $solutionPath /t:Build /p:Configuration=$Configuration /v:minimal

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
