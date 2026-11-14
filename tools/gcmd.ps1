# ============================================================
#  Send one command to the game and read the reply back (PowerShell / cmd edition)
#
#  Usage:
#    .\g.cmd state
#    .\g.cmd tremor fire
#    .\g.cmd tights 1
#
#  Design note: this script deliberately contains no non-ASCII text. The game path
#  is read from gamepath.txt next to it, as UTF-8 — so the code page of cmd or
#  PowerShell cannot mangle it (a Chinese path written inline into the script is the
#  most common way this breaks).
# ============================================================
param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Cmd)

$ErrorActionPreference = 'Stop'

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$pathFile = Join-Path $here 'gamepath.txt'

if (-not (Test-Path $pathFile)) {
    Write-Host "gamepath.txt not found (it belongs next to this script)" -ForegroundColor Red
    exit 1
}

# Read as UTF-8 explicitly — otherwise the path comes out garbled
$game = [IO.File]::ReadAllText($pathFile, [Text.Encoding]::UTF8).Trim()
$cmdDir = Join-Path $game 'BepInEx\l2d_dump\cmd'
$inFile = Join-Path $cmdDir 'in.txt'
$outFile = Join-Path $cmdDir 'out.txt'

if (-not (Test-Path $cmdDir)) {
    Write-Host "command directory not found: $cmdDir" -ForegroundColor Red
    Write-Host "-> the game has to be launched once before the plugin creates it." -ForegroundColor Yellow
    exit 1
}

if (-not $Cmd -or $Cmd.Count -eq 0) {
    Write-Host 'Usage: g.cmd <command> [args...]'
    Write-Host ''
    Write-Host 'read     state / parts / states / ecs / cfg / xcheck'
    Write-Host 'change   tights 0|1|2 / costume tights 1 / set <item> <value> / force off|all'
    Write-Host 'tremor   tremor status|fire|on|off / tremorlog 5'
    Write-Host 'watch    xwatch 900 / xwatch off / redlog 8 / shot <name>'
    Write-Host 'scene    scene / skipdialog on|off / click <name> / colliders'
    Write-Host ''
    Write-Host '-- last few replies --'
    if (Test-Path $outFile) { Get-Content $outFile -Encoding UTF8 | Select-Object -Last 6 }
    exit 0
}

# Writing the command: UTF-8 without BOM as well, the plugin parses it as UTF-8
$text = ($Cmd -join ' ') + "`n"
[IO.File]::WriteAllText($inFile, $text, (New-Object Text.UTF8Encoding $false))

Start-Sleep -Milliseconds 1500
if (Test-Path $outFile) { Get-Content $outFile -Encoding UTF8 | Select-Object -Last 6 }
