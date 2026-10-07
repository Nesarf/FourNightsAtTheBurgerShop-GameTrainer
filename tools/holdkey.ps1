# 按住某个键 N 秒（系统级注入，Unity 的 Input.GetKey 能读到）
# 用法: holdkey.ps1 -Vk 0x11 -Seconds 6      (0x11 = Left Ctrl)
param([int]$Vk = 0x11, [double]$Seconds = 5)
Add-Type @"
using System;using System.Runtime.InteropServices;
public class K3 {
 [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
}
"@
$p = Get-Process -Name "Four Nights at the Burger Shop" -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
if ($p) { [K3]::SetForegroundWindow($p.MainWindowHandle) | Out-Null; Start-Sleep -Milliseconds 300 }
[K3]::keybd_event([byte]$Vk, 0, 0, [UIntPtr]::Zero)          # down
Start-Sleep -Milliseconds ([int]($Seconds*1000))
[K3]::keybd_event([byte]$Vk, 0, 2, [UIntPtr]::Zero)          # up
Write-Output "held vk=$Vk for ${Seconds}s"
