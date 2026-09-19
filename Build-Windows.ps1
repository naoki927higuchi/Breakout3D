param(
    [string]$UnityEditor,
    [ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version = '1.0.0'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Use a fresh staging directory without deleting previous builds.
$editorVersion = (Select-String -LiteralPath (Join-Path $PSScriptRoot 'ProjectSettings\ProjectVersion.txt') -Pattern '^m_EditorVersion: (.+)$').Matches[0].Groups[1].Value
if (-not $UnityEditor) {
    $UnityEditor = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"
}
if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) {
    throw "Unity $editorVersion not found. Pass -UnityEditor with the path to Unity.exe."
}
$runId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 6)
$runRoot = Join-Path $PSScriptRoot "Builds\Runs\$runId"
$packageName = "NeonBreak-$Version-Windows-x64"
$playerRoot = Join-Path $runRoot 'Player'
$packageRoot = Join-Path $runRoot $packageName
$releaseRoot = Join-Path $PSScriptRoot 'Builds\Releases'
New-Item -ItemType Directory -Force -Path $playerRoot, $packageRoot, $releaseRoot | Out-Null

function Invoke-CheckedProcess {
    param([string]$File, [string[]]$Arguments, [int]$TimeoutSeconds, [string]$Log)
    foreach ($argument in $Arguments) {
        if ($argument.Contains('"')) { throw 'Arguments must not contain double quotes.' }
    }
    $quoted = ($Arguments | ForEach-Object { '"' + $_ + '"' }) -join ' '
    $process = Start-Process -FilePath $File -ArgumentList $quoted -PassThru -WindowStyle Hidden
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        $process.Kill()
        throw "Process timed out. See $Log"
    }
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "Process failed ($($process.ExitCode)). See $Log" }
}

$buildLog = Join-Path $runRoot 'build.log'
$playerExe = Join-Path $playerRoot 'NeonBreak.exe'
Invoke-CheckedProcess -File $UnityEditor -TimeoutSeconds 1200 -Log $buildLog -Arguments @(
    '-batchmode', '-quit', '-nographics', '-projectPath', $PSScriptRoot,
    '-executeMethod', 'BuildGame.Build', '-breakoutOutput', $playerExe,
    '-breakoutVersion', $Version, '-logFile', $buildLog
)
foreach ($required in @('NeonBreak.exe', 'UnityPlayer.dll', 'NeonBreak_Data', 'MonoBleedingEdge')) {
    if (-not (Test-Path -LiteralPath (Join-Path $playerRoot $required))) {
        throw "Required player component is missing: $required"
    }
}
# Exclude Unity's explicitly non-shipping diagnostic folders.
Get-ChildItem -LiteralPath $playerRoot -Force |
    Where-Object { $_.Name -notmatch '(_BackUpThisFolder_ButDontShipItWithYourGame|_BurstDebugInformation_DoNotShip)$' } |
    Copy-Item -Destination $packageRoot -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Distribution\README-ja.txt') -Destination (Join-Path $packageRoot 'README-ja.txt')
[ordered]@{
    product = 'NEON BREAK / 3D'
    version = $Version
    platform = 'Windows x64'
    unity = $editorVersion
    builtAtUtc = [DateTime]::UtcNow.ToString('o')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $packageRoot 'build-info.json') -Encoding UTF8

# Test the extracted ZIP, including a path with spaces, before publishing locally.
$candidateZip = Join-Path $runRoot "$packageName.zip"
Compress-Archive -LiteralPath $packageRoot -DestinationPath $candidateZip -CompressionLevel Optimal
$verifyRoot = Join-Path $runRoot 'Extracted verification'
Expand-Archive -LiteralPath $candidateZip -DestinationPath $verifyRoot
$testLog = Join-Path $runRoot 'selftest.log'
Invoke-CheckedProcess -File (Join-Path $verifyRoot "$packageName\NeonBreak.exe") -TimeoutSeconds 120 -Log $testLog -Arguments @(
    '-batchmode', '-nographics', '-selftest', '-logFile', $testLog
)
if (-not (Select-String -LiteralPath $testLog -Pattern '^SELF TESTS PASSED \(' -Quiet)) {
    throw "Self-test completion marker missing. See $testLog"
}
$releaseZip = Join-Path $releaseRoot "$packageName-$runId.zip"
Move-Item -LiteralPath $candidateZip -Destination $releaseZip
$hash = (Get-FileHash -LiteralPath $releaseZip -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([IO.Path]::GetFileName($releaseZip))" | Set-Content -LiteralPath "$releaseZip.sha256" -Encoding ASCII
Write-Host "Verified distribution ZIP: $releaseZip"
Write-Host "SHA256: $hash"
Write-Host "Build and test logs: $runRoot"
