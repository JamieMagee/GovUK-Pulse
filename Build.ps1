param(
    [Parameter(Mandatory=$false)][bool]   $RestorePackages  = $false,
    [Parameter(Mandatory=$false)][string] $Configuration    = "Release",
    [Parameter(Mandatory=$false)][string] $OutputPath       = ""
)

$solutionPath  = Split-Path $MyInvocation.MyCommand.Definition

if ($OutputPath -eq "") {
    $OutputPath = "$(Convert-Path "$PSScriptRoot")\artifacts"
}

if ($env:CI -ne $null -Or $env:TF_BUILD -ne $null) {
    $RestorePackages = $true
}

function MsBuildProject { param([string]$Project, [string]$Configuration)
    msbuild $Project /p:PackageLocation=$OutputPath  /p:Configuration=$Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "msbuild failed with exit code $LASTEXITCODE"
    }
}

$MsBuildProjects = @(
	(Join-Path $solutionPath "GovUk.SslScanner\GovUk.SslScanner.csproj")
)

$NpmProjects = @(
	(Join-Path $solutionPath "GovUk.Static\")
)

$NugetPackageConfigs = @(
	(Join-Path $solutionPath "GovUk.SslScanner\packages.config")
)

if ($RestorePackages -eq $true) {
    Write-Host "Restoring NuGet packages for $($NugetPackageConfigs.Count) projects..." -ForegroundColor Green
	ForEach ($project in $NugetPackageConfigs) {
        nuget restore $project -PackagesDirectory (Join-Path $solutionPath "packages")
    }
	
	Write-Host "Restoring npm packages for $($NpmProjects.Count) projects..." -ForegroundColor Green
	ForEach ($project in $NpmProjects) {
       npm --prefix $project install $project
    }
}

Write-Host "Building $($MsBuildProjects.Count) msbuild projects..." -ForegroundColor Green
ForEach ($project in $MsBuildProjects) {
    MsBuildProject $project $Configuration
}

Write-Host "Building $($NpmProjects.Count) npm projects..." -ForegroundColor Green
ForEach ($project in $NpmProjects) {
    gulp --cwd $project
}