#!/usr/bin/env bash
# =============================================================================
# 配置并发写的合并路径 · 自检（KNOWN-ISSUES #2）
#
# 【为什么需要手动跑】这个检查要求"没有别的东西在写配置"。
# 用户边玩边调参时，cfg 随时会被游戏落盘覆盖，测不出真实结果。
# 所以做成一个可以【在安静窗口一键跑完】的脚本。
#
# 测什么：两个写入者（游戏内 set 命令 + 外部进程直接改文件）
#         各自改【不同的键】，验证两边的改动都留下来、谁的都没丢。
#
# 判定：任一方的改动被对方覆盖 → FAIL
# =============================================================================
set -u

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
GAME="$(cat "$ROOT/tools/gamepath.txt" 2>/dev/null | tr -d '\r')"
CFG="$GAME/BepInEx/config/nesarf.burgershop.modder.cfg"
CMD="$GAME/BepInEx/l2d_dump/cmd"

[ -f "$CFG" ] || { echo "找不到配置：$CFG"; exit 1; }

echo "== 配置并发合并自检 =="
echo "  配置：$CFG"
echo

# ---- 0. 安静检查：cfg 最近有没有被动过 ----
before_mtime=$(stat -c %Y "$CFG" 2>/dev/null || stat -f %m "$CFG")
echo "  等 8 秒，确认没有别的东西在写（你现在别动修改器）..."
sleep 8
after_mtime=$(stat -c %Y "$CFG" 2>/dev/null || stat -f %m "$CFG")
if [ "$before_mtime" != "$after_mtime" ]; then
  echo "  ⚠ 配置这 8 秒内被改过 —— 有别的写入者在活动。"
  echo "    请【关掉修改器面板 / 停止调参】后重跑。"
  exit 2
fi
echo "   安静（8 秒内无写入）"
echo

# ---- 1. 快照 ----
cp "$CFG" /tmp/cfgmerge.orig
# 【姿势感知】set 命令写的是"当前姿势"那一份（PIdx 用 PoseIdx()），
# 所以必须先问出当前姿势，否则会验错键 —— 第一次跑就栽在这上面。
POSE_RAW=$(printf 'states
' > "$CMD/in.txt"; sleep 2.5; tail -6 "$CMD/out.txt" | grep -a '状态机' | tail -1)
case "$POSE_RAW" in
  *"centerGirlState=Sit"*)   SUF="_Sit";   PNAME="正骑" ;;
  *"centerGirlState=Osiri"*) SUF="_Osiri"; PNAME="背榨" ;;
  *)                         SUF="_Fella"; PNAME="口交" ;;
esac
echo "  当前姿势：$PNAME（set 会写 $SUF 那一份）"

A_KEY="AttackUrgeChance$SUF"
B_KEY="SpankSpeedGain$SUF"
A_OLD=$(grep -a "^$A_KEY = " "$CFG" | head -1 | sed 's/.*= //')
B_OLD=$(grep -a "^$B_KEY = " "$CFG" | head -1 | sed 's/.*= //')
echo "  起始：$A_KEY=$A_OLD  $B_KEY=$B_OLD"

A_NEW=$(python3 -c "print(round(float('${A_OLD:-20}')+7,4))")
B_NEW=$(python3 -c "print(round(float('${B_OLD:-20}')+9,4))")
echo "  将要写入：游戏侧 $A_KEY→$A_NEW（经 set 命令）  外部侧 $B_KEY→$B_NEW（直接改文件）"
echo

# ---- 2. 两个写入者，几乎同时 ----
#   游戏侧：走 set 命令（会触发 Config.Save，重写整个 cfg）
printf 'set AttackUrgeChance %s\n' "$A_NEW" > "$CMD/in.txt" &
#   外部侧：改文件里的另一个键
sleep 0.4
python3 - "$CFG" "$B_KEY" "$B_NEW" <<'PY' &
import re, sys
p, k, v = sys.argv[1], sys.argv[2], sys.argv[3]
s = open(p, encoding='utf-8', errors='ignore').read()
s = re.sub(r'^' + re.escape(k) + r' = .*$', k + ' = ' + v, s, flags=re.M)
open(p, 'w', encoding='utf-8', newline='').write(s)
PY
wait
sleep 3

# ---- 3. 判定 ----
A_END=$(grep -a "^$A_KEY = " "$CFG" | head -1 | sed 's/.*= //')
B_END=$(grep -a "^$B_KEY = " "$CFG" | head -1 | sed 's/.*= //')
echo "  结束：$A_KEY=$A_END  $B_KEY=$B_END"
echo

fail=0
if [ "$(python3 -c "print(abs(float('${A_END:-0}')-float('${A_NEW:-0}'))<0.01)")" = "True" ]; then
  echo "   游戏侧的改动保住了"
else
  echo "   游戏侧的改动丢了（期望 $A_NEW，实际 $A_END）"; fail=1
fi
if [ "$(python3 -c "print(abs(float('${B_END:-0}')-float('${B_NEW:-0}'))<0.01)")" = "True" ]; then
  echo "   外部侧的改动保住了"
else
  echo "   外部侧的改动被覆盖（期望 $B_NEW，实际 $B_END）"; fail=1
fi

echo
if [ $fail -eq 0 ]; then
  echo "结果：PASS —— 两边都没丢，合并路径成立"
  cp "$CFG" /tmp/cfgmerge.pass
else
  echo "结果：FAIL —— 存在覆盖。证据留在 /tmp/cfgmerge.orig（起始）"
  echo "       当前配置：$CFG"
fi

# ---- 4. 还原 ----
grep -a '^' /tmp/cfgmerge.orig > "$CFG"
echo "已还原起始配置。"
exit $fail
