param(
  [Parameter(Mandatory=$true)][string]$PluginFile,
  [Parameter(Mandatory=$true)][string]$Version
)

$ErrorActionPreference = "Stop"

if (!(Test-Path $PluginFile)) {
  throw "Plugin file not found: $PluginFile"
}

$t = [IO.File]::ReadAllText($PluginFile)

$pattern = 'public\s+const\s+string\s+PluginVersion\s*=\s*"[^"]*"\s*;'
$replacement = "public const string PluginVersion = ""$Version"";"

$t2 = [Text.RegularExpressions.Regex]::Replace($t, $pattern, $replacement)

if ($t -ne $t2) {
  [IO.File]::WriteAllText($PluginFile, $t2)
  Write-Host "Synced PluginVersion to $Version"
} else {
  Write-Host "PluginVersion already $Version"
}
