#!/bin/bash
# ============================================================
#  向游戏发送一条命令，并回读结果
#
#  用法（在任何目录都能跑）：
#    bash /e/DSH/build/burger-shop/tools/gcmd.sh state
#    bash /e/DSH/build/burger-shop/tools/gcmd.sh tremor fire
#    bash /e/DSH/build/burger-shop/tools/gcmd.sh tights 1
#
#  不加参数 = 显示用法 + 最近几条回显
# ============================================================
G='<游戏目录>'
C="$G/BepInEx/l2d_dump/cmd"
IN="$C/in.txt"; OUT="$C/out.txt"

if [ ! -d "$C" ]; then
  echo "找不到命令目录：$C"
  echo "→ 游戏需要先启动过一次，插件才会建出这个目录。"
  exit 1
fi

if [ $# -eq 0 ]; then
  cat <<'USAGE'
用法：bash gcmd.sh <命令> [参数...]       （不加引号也行）

读状态
  state              换装槽位 / _Op_ 字段 / 部件透明度 / 强制状态
  parts              全部 44 个部件的透明度开闭一览
  states             状态机字段（骑乘位/吸精/女孩状态…）+ 点击归属
  ecs                绝顶值 / 上限 / 抗性 / 吸精发生率
  cfg                修改器全部配置项当前值
  xcheck             16 条不变量交叉检查 → cmd/xcheck.txt
  pix 0 0 1920 1080  读回屏幕像素，只输出数值

改状态
  tights 0|1|2       裤袜：无 / 黑 / 白
  costume tights 1   换装槽位（cap/upper/lower/tights/glasses）
  set TremorChance 30   改修改器配置（会落盘，重启保持）
  force off|all      强制显示全部部件

动摇
  tremor status      当前波动状态
  tremor fire        立刻手动触发一次动摇 ← 你想试的那个
  tremor on|off      开关动摇
  tremorlog 5        逐帧记录绝顶值波形 5 秒 → cmd/tremor.csv

监视 / 记录
  xwatch 900         持续交叉检查 15 分钟，只记异常 → cmd/xwatch.txt
  xwatch off         停止
  redlog 8           逐帧采样屏幕红通道 → cmd/red.csv
  shot 名字          截图 → cmd/名字.png
  ecsgain 20         注入 20 点绝顶值（测适应倍率用）

场景 / 其他
  scene              当前场景
  scene TabemiMain   切到店内
  skipdialog on|off  剧情快进
  colliders          列出可点击对象
  click <名字>       在游戏内触发按钮（不碰系统鼠标）
  quit               退出游戏
USAGE
  echo
  echo "—— 最近几条回显 ——"
  tail -6 "$OUT" 2>/dev/null | sed 's/^/  /'
  exit 0
fi

printf '%s\n' "$*" > "$IN"
sleep 1.5
tail -6 "$OUT" 2>/dev/null
