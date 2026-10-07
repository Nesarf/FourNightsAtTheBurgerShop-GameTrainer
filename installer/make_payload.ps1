# 组装安装程序的载荷（BepInEx 核心 + 插件 + 编辑器 + 卸载脚本）
# 产出：installer\payload.zip（内嵌进安装程序 exe 的资源）

$ErrorActionPreference = 'Stop'

$root      = Split-Path -Parent $MyInvocation.MyCommand.Path
$build     = Split-Path -Parent $root
$dist      = Join-Path $build 'dist'
$game      = '<游戏目录>'
$bepSrc    = Join-Path $env:TEMP 'BepInEx_clean'

# 1) 从官方压缩包重新解出干净的 BepInEx（不用游戏目录里那份，避免带入运行缓存）
$zip = Join-Path $env:TEMP 'BepInEx_win_x64.zip'
if (-not (Test-Path $zip)) {
    Write-Host '下载 BepInEx 5.4.23.5 ...'
    Invoke-WebRequest -Uri 'https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_win_x64_5.4.23.5.zip' -OutFile $zip
}

if (Test-Path $bepSrc) { Remove-Item $bepSrc -Recurse -Force }
New-Item -ItemType Directory -Path $bepSrc -Force | Out-Null
Expand-Archive -Path $zip -DestinationPath $bepSrc -Force
Remove-Item (Join-Path $bepSrc 'changelog.txt') -Force -ErrorAction SilentlyContinue

# 2) 插件与配置
$plugDir = Join-Path $bepSrc 'BepInEx\plugins'
$cfgDir  = Join-Path $bepSrc 'BepInEx\config'
New-Item -ItemType Directory -Path $plugDir -Force | Out-Null
New-Item -ItemType Directory -Path $cfgDir  -Force | Out-Null

Copy-Item (Join-Path $dist 'BurgerShopModder.dll') $plugDir -Force

# 默认配置：带完整注释，装好即可被编辑器读取
$gameCfg = Join-Path $game 'BepInEx\config\nesarf.burgershop.modder.cfg'
if (Test-Path $gameCfg) {
    Copy-Item $gameCfg $cfgDir -Force
    Write-Host '已并入默认配置文件'
} else {
    Write-Host '警告：游戏目录里没有生成的 cfg，BepInEx 首次运行会自己生成' -ForegroundColor Yellow
}

# 3) 打包
$staging = Join-Path $root '_staging'
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Path $staging -Force | Out-Null

# BepInEx 整棵（core/plugins/config）
Copy-Item (Join-Path $bepSrc 'BepInEx') $staging -Recurse -Force
# doorstop 三件套
Copy-Item (Join-Path $bepSrc 'winhttp.dll')          $staging -Force
Copy-Item (Join-Path $bepSrc 'doorstop_config.ini')  $staging -Force
Copy-Item (Join-Path $bepSrc '.doorstop_version')    $staging -Force -ErrorAction SilentlyContinue
# 编辑器 + 卸载脚本
Copy-Item (Join-Path $dist '数值修改器.exe')         $staging -Force
Copy-Item (Join-Path $dist '数值修改器.exe.config')  $staging -Force -ErrorAction SilentlyContinue
Copy-Item (Join-Path $dist '卸载还原.ps1')           $staging -Force
Copy-Item (Join-Path $dist '使用说明.md')            $staging -Force

$payload = Join-Path $root 'payload.zip'
if (Test-Path $payload) { Remove-Item $payload -Force }
Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $payload -CompressionLevel Optimal

Remove-Item $staging -Recurse -Force

$mb = [math]::Round((Get-Item $payload).Length / 1MB, 2)
Write-Host "载荷已生成：$payload  ($mb MB)" -ForegroundColor Green

# 列出内容概览
Add-Type -AssemblyName System.IO.Compression.FileSystem
$z = [System.IO.Compression.ZipFile]::OpenRead($payload)
$n = $z.Entries.Count
$top = $z.Entries | ForEach-Object { ($_.FullName -split '/')[0] } | Sort-Object -Unique
$z.Dispose()
Write-Host "共 $n 个条目，顶层：$($top -join ', ')"
