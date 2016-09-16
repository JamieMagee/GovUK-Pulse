param(
    [Parameter(Mandatory=$false)][bool]   $RestorePackages  = $false,
    [Parameter(Mandatory=$false)][string] $Configuration    = "Release",
    [Parameter(Mandatory=$false)][string] $OutputPath       = "",
    [Parameter(Mandatory=$false)][bool]   $PublishWebsite   = $true
)

$solutionPath  = Split-Path $MyInvocation.MyCommand.Definition
$framework     = "netcoreapp1.0"
$dotnetVersion = "1.0.0-preview2-003121"

if ($OutputPath -eq "") {
    $OutputPath = "$(Convert-Path "$PSScriptRoot")\artifacts"
}

$env:DOTNET_INSTALL_DIR = "$(Convert-Path "$PSScriptRoot")\.dotnetcli"

if ($env:CI -ne $null -Or $env:TF_BUILD -ne $null) {
    $RestorePackages = $true
    $PatchVersion = $true
}

if (!(Test-Path $env:DOTNET_INSTALL_DIR)) {
    mkdir $env:DOTNET_INSTALL_DIR | Out-Null
    $installScript = Join-Path $env:DOTNET_INSTALL_DIR "install.ps1"
    Invoke-WebRequest "https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.ps1" -OutFile $installScript
    & $installScript -Version "$dotnetVersion" -InstallDir "$env:DOTNET_INSTALL_DIR" -NoPath
}

$env:PATH = "$env:DOTNET_INSTALL_DIR;$env:PATH"
$dotnet   = "$env:DOTNET_INSTALL_DIR\dotnet"

function DotNetRestore { param([string]$Project)
    & $dotnet restore $Project --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed with exit code $LASTEXITCODE"
    }
}

function DotNetBuild { param([string]$Project, [string]$Configuration)
    & $dotnet build $Project --output $OutputPath --framework $framework --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }
}

function MsBuildProject { param([string]$Project, [string]$Configuration)
    msbuild $Project /p:PackageLocation=$OutputPath  /p:Configuration=$Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "msbuild failed with exit code $LASTEXITCODE"
    }
}

function DotNetPublish { param([string]$Project , [string]$Configuration)
    $publishPath = (Join-Path $OutputPath "publish")
    & $dotnet publish $Project --output $publishPath --framework $framework --configuration $Configuration --no-build
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }
}

$coreProjects = @(
    (Join-Path $solutionPath "GovUk\project.json")
)

$MsBuildProjects = @(
	(Join-Path $solutionPath "SslScanner\SslScanner.csproj")
)

$NugetPackageConfigs = @(
	(Join-Path $solutionPath "SslScanner\packages.config")
)

$publishCoreProjects = @(
    (Join-Path $solutionPath "GovUk\project.json")
)

if ($RestorePackages -eq $true) {
    Write-Host "Restoring NuGet packages for $($coreProjects.Count) projects..." -ForegroundColor Green
    ForEach ($project in $coreProjects) {
        DotNetRestore $project
    }
	ForEach ($project in $NugetPackageConfigs) {
        nuget restore $project -PackagesDirectory (Join-Path $solutionPath "packages")
    }
}

Write-Host "Building $($coreProjects.Count) msbuild projects..." -ForegroundColor Green
ForEach ($project in $MsBuildProjects) {
    MsBuildProject $project $Configuration
}

Write-Host "Building $($coreProjects.Count) dotnet core projects..." -ForegroundColor Green
ForEach ($project in $coreProjects) {
    DotNetBuild $project $Configuration
}

if ($PublishWebsite -eq $true) {
    Write-Host "Publishing $($publishCoreProjects.Count) dotnet core projects..." -ForegroundColor Green
    ForEach ($project in $publishCoreProjects) {
        DotNetPublish $project $Configuration
    }
}