# 一键卸载 / 完全还原
#
# 本脚本删除 BepInEx 与插件，把游戏还原到安装修改器之前的状态。
# 游戏本体（exe、_Data、dlc）自始至终没有被改动过。
#
# 用法：右键本文件 →「使用 PowerShell 运行」，或在 PowerShell 里执行：
#   powershell -ExecutionPolicy Bypass -File .\卸载还原.ps1

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$gameRoot = $scriptDir

# 允许脚本放在别处：往上找游戏根目录
while ($gameRoot -and -not (Test-Path (Join-Path $gameRoot 'Four Nights at the Burger Shop.exe'))) {
    $parent = Split-Path -Parent $gameRoot
    if ($parent -eq $gameRoot) { $gameRoot = $null; break }
    $gameRoot = $parent
}

if (-not $gameRoot) {
    Write-Host "找不到游戏根目录（需要存在 Four Nights at the Burger Shop.exe）。" -ForegroundColor Red
    Write-Host "请把本脚本放到游戏根目录再运行。"
    exit 1
}

Write-Host "游戏目录：$gameRoot" -ForegroundColor Cyan
Write-Host ""

# 1) 备份现有 BepInEx 配置，便于日后想恢复设置
$cfg = Join-Path $gameRoot 'BepInEx\config\nesarf.burgershop.modder.cfg'
if (Test-Path $cfg) {
    $backupDir = Join-Path $env:USERPROFILE 'Documents\burger-shop-mod-backup'
    if (-not (Test-Path $backupDir)) { New-Item -ItemType Directory -Path $backupDir -Force | Out-Null }
    Copy-Item $cfg (Join-Path $backupDir 'nesarf.burgershop.modder.cfg') -Force
    Write-Host "已备份设置到：$backupDir" -ForegroundColor Yellow
}

# 2) 删除 BepInEx 相关文件
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
        Write-Host "已删除：$t" -ForegroundColor Green
        $removed++
    }
}

Write-Host ""

# 3) 顺带清掉修改器可能写入的开关进度（可选，不影响存档本体）
Write-Host "提示：若曾开启「解锁全部章节」，进度写在注册表 HKCU\Software\幻夢劇場シャルロッテ 下。" -ForegroundColor DarkGray
Write-Host "     需要连进度一起清掉的话，手动删掉该注册表项即可（不删也不影响还原）。" -ForegroundColor DarkGray
Write-Host ""

if ($removed -eq 0) {
    Write-Host "没有发现需要删除的内容，游戏已经是干净状态。" -ForegroundColor Yellow
} else {
    Write-Host "完成：游戏已还原到安装修改器之前的状态。" -ForegroundColor Cyan
}

Write-Host ""
Read-Host "按回车键关闭"
