param(
  [string]$AssetsRoot = "Assets",
  [string]$EditorBuildSettings = "ProjectSettings/EditorBuildSettings.asset",
  [string]$OutDir = "Reports"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-GuidFromMeta([string]$metaPath) {
  if (-not (Test-Path $metaPath)) { return $null }
  $line = Select-String -Path $metaPath -Pattern '^guid:\s*([a-f0-9]{32})' -SimpleMatch:$false -CaseSensitive:$false | Select-Object -First 1
  if ($line) { $m = [regex]::Match($line.Line, 'guid:\s*([a-f0-9]{32})', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase); if ($m.Success) { return $m.Groups[1].Value.ToLowerInvariant() } }
  return $null
}

function Get-BuildScenes([string]$buildSettingsPath) {
  if (-not (Test-Path $buildSettingsPath)) { return @() }
  $content = Get-Content -Path $buildSettingsPath -Raw
  $matches = [regex]::Matches($content, 'path:\s*(?<path>Assets\/[^\r\n]+\.unity)')
  $scenes = @()
  foreach ($m in $matches) { $scenes += $m.Groups['path'].Value }
  return $scenes | Sort-Object -Unique
}

function Get-ReferencedGuids([string]$filePath) {
  try { $text = Get-Content -Path $filePath -Raw -ErrorAction Stop } catch { return @() }
  $matches = [regex]::Matches($text, 'guid:\s*([a-f0-9]{32})', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
  $guids = @()
  foreach ($m in $matches) { $guids += $m.Groups[1].Value.ToLowerInvariant() }
  return $guids | Sort-Object -Unique
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$timestamp = Get-Date -Format yyyyMMdd_HHmmss

# 1) ASSETS: Reachability from build scenes
Write-Host "[1/3] Analyzing assets reachability..."
$assetExtensions = @(
  '.unity','.prefab','.mat','.asset','.controller','.anim','.fbx','.dae','.obj',
  '.png','.jpg','.jpeg','.tga','.psd','.bmp','.tif','.tiff','.gif','.svg',
  '.mp4','.mov','.avi','.wav','.mp3','.ogg','.aiff','.m4a','.flac',
  '.shader','.cginc','.hlsl','.compute','.renderTexture','.ttf','.otf'
)

$allAssets = Get-ChildItem -Recurse -File -Path $AssetsRoot |
  Where-Object { $assetExtensions -contains $_.Extension.ToLowerInvariant() }

$guidToPath = @{}
$pathToGuid = @{}
foreach ($f in $allAssets) {
  $meta = "$($f.FullName).meta"
  $guid = Get-GuidFromMeta $meta
  if ([string]::IsNullOrEmpty($guid)) { continue }
  $rel = ($f.FullName | Resolve-Path).Path.Replace((Get-Location).Path + '\\','') -replace '\\','/'
  $guidToPath[$guid] = $rel
  $pathToGuid[$rel] = $guid
}

$seedScenes = Get-BuildScenes $EditorBuildSettings
if ($seedScenes.Count -eq 0) {
  $seedScenes = Get-ChildItem -Recurse -File -Path $AssetsRoot -Filter *.unity | ForEach-Object { $_.FullName -replace '\\','/' }
}

$queue = New-Object System.Collections.Generic.Queue[string]
$visitedGuids = New-Object System.Collections.Generic.HashSet[string]

foreach ($scene in $seedScenes) {
  $sceneRel = $scene
  if (-not ($sceneRel.StartsWith('Assets/'))) { $sceneRel = ($scene | Resolve-Path).Path.Replace((Get-Location).Path + '\\','') -replace '\\','/' }
  $sg = Get-GuidFromMeta "$sceneRel.meta"
  if ($sg) { $null = $visitedGuids.Add($sg) }
  $queue.Enqueue($sceneRel)
}

while ($queue.Count -gt 0) {
  $current = $queue.Dequeue()
  $refs = Get-ReferencedGuids $current
  foreach ($g in $refs) {
    if ($visitedGuids.Contains($g)) { continue }
    $null = $visitedGuids.Add($g)
    if ($guidToPath.ContainsKey($g)) { $queue.Enqueue($guidToPath[$g]) }
  }
}

# Always-keep: any Resources folder and StreamingAssets
$resourcesDirs = Get-ChildItem -Recurse -Directory -Path $AssetsRoot | Where-Object { $_.Name -eq 'Resources' } | ForEach-Object { $_.FullName }
$alwaysKeep = @('Assets/StreamingAssets') + $resourcesDirs
foreach ($keep in $alwaysKeep) {
  if (Test-Path $keep) {
    Get-ChildItem -Recurse -File -Path $keep | ForEach-Object {
      $rp = ($_.FullName | Resolve-Path).Path.Replace((Get-Location).Path + '\\','') -replace '\\','/'
      if ($pathToGuid.ContainsKey($rp)) { $null = $visitedGuids.Add($pathToGuid[$rp]) }
    }
  }
}

$allAssetPaths = $pathToGuid.Keys
$unusedAssets = @()
foreach ($p in $allAssetPaths) {
  $g = $pathToGuid[$p]
  if (-not $visitedGuids.Contains($g)) { try { $unusedAssets += Get-Item $p } catch {} }
}
$unusedSorted = @($unusedAssets | Sort-Object Length -Descending)
$totalBytes = 0
if ($unusedSorted.Count -gt 0) { $totalBytes = ($unusedSorted | Measure-Object -Property Length -Sum).Sum }

$assetReport = @()
$assetReport += "Unity Unused Assets Report ($timestamp)"
$assetReport += "Build Scenes:"; foreach ($s in $seedScenes) { $assetReport += "  - $s" }
$assetReport += ""
$assetReport += "Unused asset count: $($unusedSorted.Count)"
$assetReport += ("Total potential bytes: {0:N0}" -f $totalBytes)
$assetReport += ""
$assetReport += "Top 100 by size:"
foreach ($f in ($unusedSorted | Select-Object -First 100)) {
  $rel = ($f.FullName | Resolve-Path).Path.Replace((Get-Location).Path + '\\','') -replace '\\','/'
  $assetReport += ("{0,12:N0}  {1}" -f $f.Length, $rel)
}
$assetReportPath = Join-Path $OutDir "UnusedAssets_$timestamp.txt"
Set-Content -Path $assetReportPath -Value $assetReport -Encoding UTF8

# 2) PACKAGES: Heuristic usage scan
Write-Host "[2/3] Auditing Unity packages usage..."
$manifestPath = "Packages/manifest.json"
$pkgReportPath = Join-Path $OutDir "UnusedPackages_$timestamp.txt"
if (Test-Path $manifestPath) {
  $manifest = Get-Content -Raw -Path $manifestPath | ConvertFrom-Json
  $deps = @($manifest.dependencies.PSObject.Properties | ForEach-Object { $_.Name })
  $pkgPatterns = @{
    'com.unity.ads' = @('UnityEngine.Advertisements','Advertisement');
    'com.unity.analytics' = @('UnityEngine.Analytics','AnalyticsEvent');
    'com.unity.purchasing' = @('UnityEngine.Purchasing','IStoreListener','IAPButton','CodelessIAPStoreListener');
    'com.unity.timeline' = @('PlayableDirector','UnityEngine.Timeline');
    'com.unity.visualscripting' = @('Unity.VisualScripting','Bolt');
    'com.unity.multiplayer.center' = @('Multiplayer');
    'com.unity.collab-proxy' = @('CollabProxy');
  }
  $likelyUnused = @()
  foreach ($pkg in $pkgPatterns.Keys) {
    if ($deps -notcontains $pkg) { continue }
    $patterns = $pkgPatterns[$pkg]
    $hits = Select-String -Path "$AssetsRoot/**" -Pattern $patterns -SimpleMatch -ErrorAction SilentlyContinue
    if (-not $hits) { $likelyUnused += $pkg }
  }
  $pkgLines = @()
  $pkgLines += "Unity Packages Usage Report ($timestamp)"
  $pkgLines += "Declared packages: $($deps.Count)"
  $pkgLines += "Likely unused (no references found):"
  foreach ($p in ($likelyUnused | Sort-Object)) { $pkgLines += "  - $p" }
  Set-Content -Path $pkgReportPath -Value $pkgLines -Encoding UTF8
} else {
  Set-Content -Path $pkgReportPath -Value @('manifest.json not found') -Encoding UTF8
}

# 3) SCRIPTS: Scene/prefab reference + code cross-ref
Write-Host "[3/3] Scanning scripts usage..."
$scripts = Get-ChildItem -Recurse -File -Path $AssetsRoot -Filter *.cs | Where-Object { $_.FullName -notmatch '\\Editor\\' }
$scriptInfos = @()
foreach ($s in $scripts) {
  $guid = Get-GuidFromMeta "$($s.FullName).meta"
  if ([string]::IsNullOrEmpty($guid)) { continue }
  $rel = ($s.FullName | Resolve-Path).Path.Replace((Get-Location).Path + '\\','') -replace '\\','/'
  $scriptInfos += [pscustomobject]@{ Path=$rel; Guid=$guid; Name=[System.IO.Path]::GetFileNameWithoutExtension($s.Name) }
}

$codeFiles = Get-ChildItem -Recurse -File -Path $AssetsRoot -Filter *.cs | ForEach-Object { ($_.FullName | Resolve-Path).Path }
$usedInScenes = @(); $usedByCode = @(); $unusedScripts = @()
foreach ($info in $scriptInfos) {
  $isUsedScene = $false
  $hits = Select-String -Path "$AssetsRoot/**/*.unity","$AssetsRoot/**/*.prefab","$AssetsRoot/**/*.asset" -Pattern $info.Guid -SimpleMatch -ErrorAction SilentlyContinue
  if ($hits) { $isUsedScene = $true }
  if ($isUsedScene) { $usedInScenes += $info; continue }

  $selfFull = ((Resolve-Path ($info.Path -replace '/', '\\')).Path)
  $pattern = "\b$([regex]::Escape($info.Name))\b"
  $codeRef = $false
  foreach ($cf in $codeFiles) {
    if ($cf -eq $selfFull) { continue }
    try { $text = Get-Content -Path $cf -Raw -ErrorAction Stop; if ([regex]::IsMatch($text, $pattern)) { $codeRef = $true; break } } catch {}
  }
  if ($codeRef) { $usedByCode += $info } else { $unusedScripts += $info }
}

$scriptReport = @()
$scriptReport += "Unity Unused Scripts Report ($timestamp)"
$scriptReport += "Used by scenes/prefabs: $($usedInScenes.Count)"
$scriptReport += "Used only by code: $($usedByCode.Count)"
$scriptReport += "Unused candidates: $($unusedScripts.Count)"
$scriptReport += ""
$scriptReport += "Unused scripts:"; foreach ($u in ($unusedScripts | Sort-Object Path)) { $scriptReport += "  - $($u.Path)" }
$scriptReportPath = Join-Path $OutDir "UnusedScripts_$timestamp.txt"
Set-Content -Path $scriptReportPath -Value $scriptReport -Encoding UTF8

Write-Host "Reports written to:"
Write-Host "  - $assetReportPath"
Write-Host "  - $pkgReportPath"
Write-Host "  - $scriptReportPath"

