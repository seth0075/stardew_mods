$ErrorActionPreference = 'Stop'

$excludedDirectoryNames = @('.git', '.vscode', 'bin', 'obj', 'venv')

function Get-ProjectFiles {
    param([string] $Directory)

    Get-ChildItem -LiteralPath $Directory -File -Filter '*.csproj'

    foreach ($childDirectory in Get-ChildItem -LiteralPath $Directory -Directory) {
        if ($childDirectory.Name -in $excludedDirectoryNames -or $childDirectory.Name -like '*Tests') {
            continue
        }

        Get-ProjectFiles -Directory $childDirectory.FullName
    }
}

$projects = @(
    Get-ProjectFiles -Directory $PSScriptRoot |
        Where-Object {
            $manifestPath = Join-Path $_.DirectoryName 'manifest.json'
            if (-not (Test-Path -LiteralPath $manifestPath)) {
                return $false
            }

            $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
            -not [string]::IsNullOrWhiteSpace($manifest.EntryDll)
        } |
        Sort-Object FullName
)

if ($projects.Count -eq 0) {
    throw "No buildable SMAPI mod projects with a manifest were found under '$PSScriptRoot'."
}

$modsPathOutput = & dotnet msbuild $projects[0].FullName '-getProperty:GameModsPath'
if ($LASTEXITCODE -ne 0) {
    throw "Couldn't resolve GameModsPath (dotnet msbuild exited with code $LASTEXITCODE)."
}

$modsPath = ($modsPathOutput | Select-Object -Last 1).Trim()
if ([string]::IsNullOrWhiteSpace($modsPath)) {
    throw 'GameModsPath is empty; set it to the Stardew Valley Mods folder before running this script.'
}

$modsPath = [System.IO.Path]::GetFullPath($modsPath)
if ([System.IO.Path]::GetFileName($modsPath) -ine 'Mods') {
    throw "GameModsPath must point to a folder named 'Mods', but resolved to '$modsPath'."
}
if (-not (Test-Path -LiteralPath $modsPath -PathType Container)) {
    throw "The Stardew Valley Mods folder doesn't exist: '$modsPath'."
}

$stagingModsPath = Join-Path ([System.IO.Path]::GetTempPath()) "stardew-mods-build-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $stagingModsPath | Out-Null

try {
    foreach ($project in $projects) {
        Write-Host "Rebuilding mod project: $($project.FullName)"
        & dotnet build $project.FullName --no-incremental "-p:GameModsPath=$stagingModsPath"
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed for '$($project.FullName)' with exit code $LASTEXITCODE."
        }
    }

    $glassworksPath = Join-Path $PSScriptRoot 'Glassworks'
    if (-not (Test-Path -LiteralPath (Join-Path $glassworksPath 'manifest.json') -PathType Leaf)) {
        throw "The Glassworks content pack manifest wasn't found at '$glassworksPath'."
    }

    Copy-Item -LiteralPath $glassworksPath -Destination $stagingModsPath -Recurse

    $stagedModFolders = @(Get-ChildItem -LiteralPath $stagingModsPath -Directory | Sort-Object Name)
    if ($stagedModFolders.Count -eq 0) {
        throw 'The build completed but did not stage any mod folders.'
    }

    foreach ($folder in $stagedModFolders) {
        if (-not (Test-Path -LiteralPath (Join-Path $folder.FullName 'manifest.json') -PathType Leaf)) {
            throw "The staged mod folder '$($folder.Name)' has no manifest.json."
        }
    }

    $legacyModFolderNames = @(
        'CarnivorousPlants',
        'CarnivorousPlants.Core',
        'InsectValley.Core',
        '[CP] Insect Valley'
    )
    $managedModFolderNames = @($legacyModFolderNames + $stagedModFolders.Name | Select-Object -Unique)

    foreach ($folderName in $managedModFolderNames) {
        $installedPath = Join-Path $modsPath $folderName
        if (Test-Path -LiteralPath $installedPath) {
            if (-not (Test-Path -LiteralPath $installedPath -PathType Container)) {
                throw "Expected a mod directory at '$installedPath', but found a file."
            }

            Write-Host "Removing old mod folder: $installedPath"
            Remove-Item -LiteralPath $installedPath -Recurse -Force
        }
    }

    foreach ($folder in $stagedModFolders) {
        Write-Host "Installing mod folder: $($folder.Name)"
        Copy-Item -LiteralPath $folder.FullName -Destination $modsPath -Recurse
    }

    Write-Host "Rebuilt $($projects.Count) mod project(s) and installed $($stagedModFolders.Count) mod folder(s) in '$modsPath'."
}
finally {
    if (Test-Path -LiteralPath $stagingModsPath) {
        Remove-Item -LiteralPath $stagingModsPath -Recurse -Force
    }
}
