# Clears all Sites.Host proxy disk cache (every sourceHost folder).
# Default: C:\_cache (Windows) or /_cache (Linux). Override with SITES_CACHE_ROOT.
param()

$ErrorActionPreference = 'Stop'

$RepoRoot = $PSScriptRoot

function Get-CacheRoot {
    if ($env:SITES_CACHE_ROOT) {
        return $env:SITES_CACHE_ROOT.Trim()
    }

    $appsettings = Join-Path $RepoRoot 'src\Sites.Host\appsettings.json'
    if (Test-Path $appsettings) {
        try {
            $json = Get-Content -LiteralPath $appsettings -Raw | ConvertFrom-Json
            $configured = $json.Sites.Cache.RootPath
            if ($configured -and $configured.Trim()) {
                return $configured.Trim()
            }
        }
        catch {
            Write-Warning "Could not read Cache:RootPath from appsettings.json. Using default."
        }
    }

    if ($IsWindows -or $env:OS -like '*Windows*') {
        return 'C:\_cache'
    }

    return '/_cache'
}

$cacheRoot = [System.IO.Path]::GetFullPath((Get-CacheRoot))

if (-not (Test-Path -LiteralPath $cacheRoot)) {
    Write-Host "Cache directory does not exist (nothing to clear): $cacheRoot"
    exit 0
}

$entries = @(Get-ChildItem -LiteralPath $cacheRoot -Force -ErrorAction SilentlyContinue)
if ($entries.Count -eq 0) {
    Write-Host "Cache already empty: $cacheRoot"
    exit 0
}

foreach ($entry in $entries) {
    Remove-Item -LiteralPath $entry.FullName -Recurse -Force
}

Write-Host "Cleared proxy disk cache: $cacheRoot"
Write-Host "Removed $($entries.Count) entr(ies)."
