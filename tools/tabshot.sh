#!/usr/bin/env bash
# ============================================================================
# tabshot.sh —— 切栏 + 截图 + 精确裁切面板
#
# 【为什么有这个东西】
# 原来验证面板布局靠两个独立命令：先 tab 切栏，再 shot 截图。
# 两者时机对不上 —— 截到的是切换前的画面，或者 ImGui 还没重绘。
# 我为此至少误判过三次，而且每次都先怀疑代码，怀疑错了对象。
#
# 现在切栏与截图由插件在同一帧的 OnGUI 末尾完成，并把面板窗口矩形
# 写成同名 .rect 文件 —— 裁切坐标是插件自己给的，不是猜的。
#
# 【用法】
#     bash tools/tabshot.sh <页签> [名字]
#     bash tools/tabshot.sh 3            # 正骑，名字自动用 tab3
#     bash tools/tabshot.sh 正骑 sit     # 按名字切
#
# 产出（在 BepInEx/l2d_dump/cmd/ 下）：
#     <名字>.png          全屏
#     <名字>.rect         面板矩形
#     <名字>_panel.png    按矩形裁好的面板（本脚本生成）
# ============================================================================
set -u

GAME="${GAME_DIR:-D:/Four Nights at the Burger Shop ～ハンバーガー食べながら食べられるミニゲーム～}"
CMD="$GAME/BepInEx/l2d_dump/cmd"
TAB="${1:-}"
NAME="${2:-tab$TAB}"

if [ -z "$TAB" ]; then
  echo "用法: bash tools/tabshot.sh <页签> [名字]"
  echo "  页签可以是序号 0~5，也可以是名字（玩家/口交/背榨/正骑/换装/系统）"
  exit 2
fi

if [ ! -d "$CMD" ]; then
  echo "找不到命令通道目录：$CMD"
  echo "（游戏没在跑？或者 GAME_DIR 设错了）"
  exit 1
fi

rm -f "$CMD/$NAME.png" "$CMD/$NAME.rect" "$CMD/${NAME}_panel.png"

echo "请求 tabshot：$TAB -> $NAME"
printf 'tabshot %s %s\n' "$TAB" "$NAME" > "$CMD/in.txt"

# 等插件在 OnGUI 末尾把文件写出来
for i in $(seq 1 40); do
  if [ -f "$CMD/$NAME.rect" ] && [ -f "$CMD/$NAME.png" ]; then break; fi
  sleep 0.25
done

if [ ! -f "$CMD/$NAME.rect" ]; then
  echo "超时：没有拿到 $NAME.rect"
  echo "最后几行输出："
  tail -3 "$CMD/out.txt" 2>/dev/null | sed 's/^/  /'
  exit 1
fi

read -r X Y W H REST < "$CMD/$NAME.rect"
echo "面板矩形（插件给的）：x=$X y=$Y w=$W h=$H  $REST"

PY=""
for c in "E:/DSH/build/burger-shop/.venv-unity/Scripts/python.exe" python3 python; do
  if command -v "$c" >/dev/null 2>&1 || [ -x "$c" ]; then PY="$c"; break; fi
done
if [ -z "$PY" ]; then echo "找不到 python，跳过裁切（全屏图仍在）"; exit 0; fi

"$PY" - "$CMD/$NAME.png" "$CMD/${NAME}_panel.png" "$X" "$Y" "$W" "$H" <<'PYEOF'
import sys
from PIL import Image
src, dst, x, y, w, h = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4]), int(sys.argv[5]), int(sys.argv[6])
im = Image.open(src)
# 留 8px 边距，方便看清边框
pad = 8
box = (max(0, x - pad), max(0, y - pad), min(im.width, x + w + pad), min(im.height, y + h + pad))
im.crop(box).save(dst)
print('裁切 -> %s  %dx%d' % (dst, box[2] - box[0], box[3] - box[1]))
PYEOF
