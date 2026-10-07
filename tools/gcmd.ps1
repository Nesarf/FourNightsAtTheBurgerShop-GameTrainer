# ============================================================
#  向游戏发送一条命令，并回读结果（PowerShell / cmd 版）
#
#  用法：
#    .\g.cmd state
#    .\g.cmd tremor fire
#    .\g.cmd tights 1
#
#  设计说明：这个脚本本身【不含中文】，游戏路径从同目录的 gamepath.txt
#  按 UTF-8 读取 —— 这样就不受 cmd / PowerShell 的代码页影响
#  （中文路径直接写在脚本里会乱码，这是最常踩的坑）。
# ============================================================
param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Cmd)

$ErrorActionPreference = 'Stop'

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$pathFile = Join-Path $here 'gamepath.txt'

if (-not (Test-Path $pathFile)) {
    Write-Host "找不到 gamepath.txt（应在本脚本同目录）" -ForegroundColor Red
    exit 1
}

# 必须显式按 UTF-8 读 —— 否则中文路径会乱码
$game = [IO.File]::ReadAllText($pathFile, [Text.Encoding]::UTF8).Trim()
$cmdDir = Join-Path $game 'BepInEx\l2d_dump\cmd'
$inFile = Join-Path $cmdDir 'in.txt'
$outFile = Join-Path $cmdDir 'out.txt'

if (-not (Test-Path $cmdDir)) {
    Write-Host "找不到命令目录：$cmdDir" -ForegroundColor Red
    Write-Host "→ 游戏需要先启动过一次，插件才会建出这个目录。" -ForegroundColor Yellow
    exit 1
}

if (-not $Cmd -or $Cmd.Count -eq 0) {
    Write-Host '用法：g.cmd <命令> [参数...]'
    Write-Host ''
    Write-Host '读状态   state / parts / states / ecs / cfg / xcheck'
    Write-Host '改状态   tights 0|1|2 / costume tights 1 / set <项> <值> / force off|all'
    Write-Host '动摇     tremor status|fire|on|off / tremorlog 5'
    Write-Host '监视     xwatch 900 / xwatch off / redlog 8 / shot 名字'
    Write-Host '场景     scene / skipdialog on|off / click <名字> / colliders'
    Write-Host ''
    Write-Host '—— 最近几条回显 ——'
    if (Test-Path $outFile) { Get-Content $outFile -Encoding UTF8 | Select-Object -Last 6 }
    exit 0
}

# 写命令：同样必须 UTF-8 无 BOM，插件按 UTF-8 解析
$text = ($Cmd -join ' ') + "`n"
[IO.File]::WriteAllText($inFile, $text, (New-Object Text.UTF8Encoding $false))

Start-Sleep -Milliseconds 1500
if (Test-Path $outFile) { Get-Content $outFile -Encoding UTF8 | Select-Object -Last 6 }
