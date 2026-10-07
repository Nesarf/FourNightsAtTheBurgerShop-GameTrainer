#!/bin/bash
# 用命令通道把游戏驱动到"游玩状态 + 冻结画面"，用于稳定的渲染对比
G='<游戏目录>'
C="$G/BepInEx/l2d_dump/cmd"
EXE="$G/Four Nights at the Burger Shop.exe"
say() { printf '%s\n' "$1" > "$C/in.txt"; sleep "${2:-2}"; }

taskkill //IM "Four Nights at the Burger Shop.exe" //F >/dev/null 2>&1
sleep 2
rm -rf "$C"
powershell -NoProfile -Command "Start-Process -FilePath '$EXE' -WorkingDirectory '$G'" >/dev/null 2>&1

# 等命令服务起来
for i in $(seq 1 40); do [ -f "$C/out.txt" ] && break; sleep 2; done
sleep 3
say "skipdialog on" 2
say "click ButtonCustomDay" 8
say "state" 3
# 跳完开场后会到设置界面，点 Start
say "colliders" 3
if grep -q "Start  (Button)" "$C/out.txt"; then
  say "click Start" 6
else
  say "click Start" 6
fi
say "skipdialog off" 2
say "timescale 0" 2
say "state" 2
echo "--- 到达状态 ---"
tail -3 "$C/out.txt"
