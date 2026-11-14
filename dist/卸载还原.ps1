# One-click uninstall / full restore
# 一键卸载 / 完全还原
#
# This script removes BepInEx and the mod and puts the game back into the state it
# was in before the mod was installed. The game itself (exe, _Data, dlc) was never
# modified at any point.
#
# 本脚本删除 BepInEx 与插件，把游戏还原到安装修改器之前的状态。
# 游戏本体（exe、_Data、dlc）自始至终没有被改动过。
#
# Usage / 用法：
#   右键本文件 ->「使用 PowerShell 运行」，或在 PowerShell 里执行：
#   powershell -ExecutionPolicy Bypass -File .\卸载还原.ps1
#   Right-click this file -> "Run with PowerShell", or in a PowerShell window:
#   powershell -ExecutionPolicy Bypass -File .\卸载还原.ps1

$ErrorActionPreference = 'Stop'

# 语言：系统 UI 是中文就中文，其余一律英文。
# Language: Chinese UI gets Chinese, everything else gets English.
$script:English = ([System.Globalization.CultureInfo]::CurrentUICulture.TwoLetterISOLanguageName -ne 'zh')
function T([string]$zh, [string]$en) {
    if ($script:English) { return $en }
    return $zh
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$gameRoot = $scriptDir

# 允许脚本放在别处：往上找游戏根目录
# The script may sit somewhere else: walk up until the game root is found.
while ($gameRoot -and -not (Test-Path (Join-Path $gameRoot 'Four Nights at the Burger Shop.exe'))) {
    $parent = Split-Path -Parent $gameRoot
    if ($parent -eq $gameRoot) { $gameRoot = $null; break }
    $gameRoot = $parent
}

if (-not $gameRoot) {
    Write-Host (T "找不到游戏根目录（需要存在 Four Nights at the Burger Shop.exe）。" `
                  "Game root not found (the folder must contain Four Nights at the Burger Shop.exe).") -ForegroundColor Red
    Write-Host (T "请把本脚本放到游戏根目录再运行。" `
                  "Put this script in the game root folder and run it again.")
    exit 1
}

Write-Host (T "游戏目录：$gameRoot" "Game folder: $gameRoot") -ForegroundColor Cyan
Write-Host ""

# 1) 备份现有 BepInEx 配置，便于日后想恢复设置
#    Back up the current BepInEx config in case you want the settings back later.
$cfg = Join-Path $gameRoot 'BepInEx\config\nesarf.burgershop.modder.cfg'
if (Test-Path $cfg) {
    $backupDir = Join-Path $env:USERPROFILE 'Documents\burger-shop-mod-backup'
    if (-not (Test-Path $backupDir)) { New-Item -ItemType Directory -Path $backupDir -Force | Out-Null }
    Copy-Item $cfg (Join-Path $backupDir 'nesarf.burgershop.modder.cfg') -Force
    Write-Host (T "已备份设置到：$backupDir" "Settings backed up to: $backupDir") -ForegroundColor Yellow
}

# 2) 删除 BepInEx 相关文件
#    Remove the BepInEx files.
$targets = @(
    'BepInEx',
    'winhttp.dll',
    'doorstop_config.ini',
    '.doorstop_version',
    'changelog.txt',
    'output_log.txt'
)

$removed = 0
foreach ($t in $targets) {
    $p = Join-Path $gameRoot $t
    if (Test-Path $p) {
        Remove-Item $p -Recurse -Force
        Write-Host (T "已删除：$t" "Removed: $t") -ForegroundColor Green
        $removed++
    }
}

Write-Host ""

# 3) 顺带提一下修改器可能写入的开关进度（可选，不影响存档本体）
#    Worth knowing: the mod may have written unlock progress (optional, the save itself is untouched).
Write-Host (T "提示：若曾开启「解锁全部章节」，进度写在注册表 HKCU\Software\幻夢劇場シャルロッテ 下。" `
              "Note: if 'Unlock every chapter' was ever enabled, that progress lives in the registry under HKCU\Software\幻夢劇場シャルロッテ.") -ForegroundColor DarkGray
Write-Host (T "     需要连进度一起清掉的话，手动删掉该注册表项即可（不删也不影响还原）。" `
              "      Delete that registry key by hand if you want it gone (leaving it does not affect the restore).") -ForegroundColor DarkGray
Write-Host ""

if ($removed -eq 0) {
    Write-Host (T "没有发现需要删除的内容，游戏已经是干净状态。" `
                  "Nothing to remove; the game is already clean.") -ForegroundColor Yellow
} else {
    Write-Host (T "完成：游戏已还原到安装修改器之前的状态。" `
                  "Done: the game is back to the state it was in before the mod.") -ForegroundColor Cyan
}

Write-Host ""
Read-Host (T "按回车键关闭" "Press Enter to close")
