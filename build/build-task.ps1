<#
.SYNOPSIS
    Compile la solution puis déclenche le packaging Playnite (build.ps1 via PostBuild).

.DESCRIPTION
    Modes :
    - Release     : MSBuild Release + pack .pext + vérification manifest
    - DebugPack   : MSBuild Debug + pack .pext + archive .zip
#>
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("Release", "DebugPack")]
    [string]$Mode
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$SolutionPath = Join-Path $RepoRoot "source\SystemChecker.sln"
$SolutionDir = Join-Path $RepoRoot "source\"

switch ($Mode) {
    "Release" {
        $MsBuildConfiguration = "Release"
        $ConfigurationName = "release"
        $OutDir = Join-Path $SolutionDir "bin\Release\"
    }
    "DebugPack" {
        $MsBuildConfiguration = "Debug"
        $ConfigurationName = "debug-release"
        $OutDir = Join-Path $SolutionDir "bin\Debug\"
    }
}

function Get-MsBuildPath {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $path = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
        if ($path) {
            return $path
        }
    }

    throw "MSBuild introuvable. Installez Visual Studio ou Build Tools avec la charge MSBuild."
}

if (-not (Test-Path $SolutionPath)) {
    throw "Solution introuvable : $SolutionPath"
}

$MsBuild = Get-MsBuildPath
Write-Host "MSBuild : $MsBuild"
Write-Host "Mode    : $Mode ($MsBuildConfiguration, ConfigurationName=$ConfigurationName)"
Write-Host ""

& $MsBuild $SolutionPath `
    /restore `
    /t:Rebuild `
    /p:Configuration=$MsBuildConfiguration `
    /p:Platform="Any CPU" `
    /p:ConfigurationName=$ConfigurationName `
    /v:m `
    /nologo

if ($LASTEXITCODE -ne 0) {
    throw "La compilation a échoué (code $LASTEXITCODE)."
}

Write-Host ""
Write-Host "Compilation terminée. Sortie : $OutDir"
