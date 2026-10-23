<#
    .SYNOPSIS
    Generates nuget pacakges and publishes if server values are provided

    .DESCRIPTION
    Packs all supported target frameworks regardless of CoreExLocalTargetFramework.
    Builds before packing, overriding GeneratePackageOnBuild so existing binaries are not reused without a build.
    Uses Release by default; the Visual Studio configuration does not affect this script.
    Preserves existing packages except when replacing the same package filename.
    Remote publishing uploads only packages produced by this invocation.

    .PARAMETER PublishFolder
    Local folder to publish the built nuget pacakges to

    .PARAMETER IncludeSymbols
    Create nuget symbols package

    .PARAMETER IncludeSource
    Include source in unget package

    .PARAMETER ApiKey
    ApiKey for publishing to server
    
    .PARAMETER NugetServer
    Url to the nuget server

    .PARAMETER ProjectsToPublish
    List of projects to Build and publish.

    .PARAMETER DeleteFromlocalCache
    Removes matching newly packed package versions from the local NuGet cache by default.
    Pass -DeleteFromlocalCache:$false to preserve cached copies.

    .PARAMETER NugetCacheFolder
    Path to Nuget Cache folder.

    .INPUTS
    None. You cannot pipe objects to Add-Extension.

    .Example
    C:\PS> .\nuget-publish.ps1 -configuration 'Debug' -IncludeSymbols -IncludeSource
    Build all and publish locally. 
    Used to test locally changes, Create

    .Example
    C:\PS> .\nuget-publish.ps1 -NugetServer 'https://api.nuget.org/v3/index.json' -ApiKey 'key' -IncludeSymbols -IncludeSource -ProjectsToPublish @('CoreEx')
    Build selected pacakges and push to remote server.
#>
param(
    [string]$PublishFolder = "$($env:USERPROFILE)\nuget-publish",
    [string]$configuration = 'Release',
    [switch]$IncludeSymbols,
    [switch]$IncludeSource,
    [string]$NugetServer,
    [string]$ApiKey,
    [switch]$DeleteFromlocalCache = $true,
    [string]$NugetCacheFolder = "$($env:USERPROFILE)\.nuget\packages",
    [String[]]$ProjectsToPublish = @(
        "src\CoreEx",
        "src\CoreEx.AspNetCore",
        "src\CoreEx.AspNetCore.NSwag",
        "src\CoreEx.Azure.Messaging.ServiceBus",
        "src\CoreEx.Caching.FusionCache",
        "src\CoreEx.CodeGen",
        "src\CoreEx.Cosmos",
        "src\CoreEx.Data",
        "src\CoreEx.Data.GraphQL",
        "src\CoreEx.Database",
        "src\CoreEx.Database.Postgres",
        "src\CoreEx.Database.SqlServer",
        "src\CoreEx.DomainDriven",
        "src\CoreEx.EntityFrameworkCore",
        "src\CoreEx.Events",
        "src\CoreEx.RefData",
        "src\CoreEx.Template",
        "src\CoreEx.Validation",
        "src\CoreEx.UnitTesting")
    )

$ShouldPublishRemote = (![string]::IsNullOrEmpty($apiKey) -and ![string]::IsNullOrEmpty($NugetServer))

# Create the shared local feed without deleting existing packages.
if (!(Test-Path -Path $publishFolder)) {
	Write-Host "Publish folder doesn't exist - creating folder." -ForegroundColor Yellow 
	New-Item -ItemType directory -Path $publishFolder -ErrorAction Stop | Out-Null
}

$stagingFolder = Join-Path ([IO.Path]::GetTempPath()) ("CoreEx-nuget-publish-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stagingFolder -ErrorAction Stop | Out-Null
$packageDetailsRegexp = [regex] '^(?<name>.+?)\.(?<version>\d+\.\d+\.\d+(?:\.\d+)?(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?)$'

try {
    foreach ($project in $projectsToPublish) {
        $packArguments = @('pack', $project, '-o', $stagingFolder, '-c', $configuration, '-p:CoreExLocalTargetFramework=', '-p:GeneratePackageOnBuild=false', '--force')
        if ($IncludeSource) { $packArguments += '--include-source' }
        if ($IncludeSymbols) { $packArguments += '--include-symbols' }

        Write-Host "`r`nPacking $project ($configuration, all target frameworks)." -ForegroundColor Yellow
        & dotnet @packArguments
        if ($LASTEXITCODE -ne 0) { throw "Failed to pack $project (exit $LASTEXITCODE)." }
    }

    $packages = @(Get-ChildItem -LiteralPath $stagingFolder -File |
        Where-Object { $_.Extension -in '.nupkg', '.snupkg' })
    if ($packages.Count -eq 0) { throw 'Packing produced no NuGet packages.' }

    foreach ($package in $packages) {
        Copy-Item -LiteralPath $package.FullName -Destination $publishFolder -Force -ErrorAction Stop

        if ($DeleteFromlocalCache -and $package.Extension -eq '.nupkg') {
            $packageName = [System.IO.Path]::GetFileNameWithoutExtension($package.Name)
            $packageDetails = $packageDetailsRegexp.Matches($packageName)
            if($packageDetails.Success)
            {
                $packageCachePath = [IO.Path]::Combine($NugetCacheFolder, $packageDetails[0].Groups['name'].value, $packageDetails[0].Groups['version'].value)
                if((Test-Path $packageCachePath) `
                    -and $packageCachePath -ne $nugetCacheFolder `
                    -and ![string]::IsNullOrWhiteSpace($packageDetails[0].Groups['name'].value) `
                    -and ![string]::IsNullOrWhiteSpace($packageDetails[0].Groups['version'].value))
                {
                    write-host "Deleting local cache for package`r`n`t$packageCachePath"
                    Remove-Item -LiteralPath $packageCachePath -Recurse -ErrorAction Stop
                }
                else
                {
                    write-host "Local cache for package does not exist`r`n`t$packageCachePath"
                }
            }
        }
    }

    # Push only this run's packages; dotnet also pushes matching symbol packages.
    if ($ShouldPublishRemote) {
        foreach ($package in $packages | Where-Object { $_.Extension -eq '.nupkg' }) {
            Write-Host "`r`nPublishing $($package.Name) to $NugetServer." -ForegroundColor Yellow
            & dotnet nuget push $package.FullName --source $NugetServer --api-key $ApiKey --skip-duplicate
            if ($LASTEXITCODE -ne 0) { throw "Failed to push $($package.Name) (exit $LASTEXITCODE)." }
        }
    }
}
finally {
    Remove-Item -LiteralPath $stagingFolder -Recurse -Force -ErrorAction Stop
}