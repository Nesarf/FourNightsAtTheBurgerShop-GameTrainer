#!/usr/bin/env bash
# ============================================================================
# tabshot.sh — switch tab + screenshot + crop the panel precisely
#
# [Why this exists]
# Verifying the panel layout used to take two independent commands: switch the tab,
# then take a screenshot. Their timing never lined up — the capture showed the frame
# before the switch, or ImGui had not redrawn yet. That produced at least three wrong
# conclusions, and each time the code was suspected first, wrongly.
#
# Now the tab switch and the screenshot happen together at the end of the plugin's
# OnGUI, and the panel's window rectangle is written to a matching .rect file — the
# crop coordinates come from the plugin itself rather than being guessed.
#
# [Usage]
#     bash tools/tabshot.sh <tab> [name]
#     bash tools/tabshot.sh 3            # Riding, name defaults to tab3
#     bash tools/tabshot.sh 正骑 sit     # switch by name
#
# Output (under BepInEx/l2d_dump/cmd/):
#     <name>.png          full screen
#     <name>.rect         panel rectangle
#     <name>_panel.png    panel cropped to that rectangle (made by this script)
# ============================================================================
set -u

GAME="${GAME_DIR:-D:/Four Nights at the Burger Shop ～ハンバーガー食べながら食べられるミニゲーム～}"
CMD="$GAME/BepInEx/l2d_dump/cmd"
TAB="${1:-}"
NAME="${2:-tab$TAB}"

if [ -z "$TAB" ]; then
  echo "usage: bash tools/tabshot.sh <tab> [name]"
  echo "  the tab may be an index 0-5, or a name (玩家/口交/背榨/正骑/换装/系统)"
  exit 2
fi

if [ ! -d "$CMD" ]; then
  echo "command channel directory not found: $CMD"
  echo "(is the game not running? or is GAME_DIR wrong?)"
  exit 1
fi

rm -f "$CMD/$NAME.png" "$CMD/$NAME.rect" "$CMD/${NAME}_panel.png"

echo "requesting tabshot: $TAB -> $NAME"
printf 'tabshot %s %s\n' "$TAB" "$NAME" > "$CMD/in.txt"

# wait for the plugin to write the files at the end of OnGUI
for i in $(seq 1 40); do
  if [ -f "$CMD/$NAME.rect" ] && [ -f "$CMD/$NAME.png" ]; then break; fi
  sleep 0.25
done

if [ ! -f "$CMD/$NAME.rect" ]; then
  echo "timed out: no $NAME.rect"
  echo "last few lines of output:"
  tail -3 "$CMD/out.txt" 2>/dev/null | sed 's/^/  /'
  exit 1
fi

read -r X Y W H REST < "$CMD/$NAME.rect"
echo "panel rectangle (from the plugin): x=$X y=$Y w=$W h=$H  $REST"

PY=""
# No hard-coded interpreter path — a literal one would trip static-check rule (6) and
# the file could not enter the repository. Try the working copy's venv, then PATH.
VENV_PY="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." 2>/dev/null && pwd)/.venv-unity/Scripts/python.exe"
for c in "$VENV_PY" python3 python; do
  if command -v "$c" >/dev/null 2>&1 || [ -x "$c" ]; then PY="$c"; break; fi
done
if [ -z "$PY" ]; then echo "python not found; skipping the crop (the full-screen image is still there)"; exit 0; fi

"$PY" - "$CMD/$NAME.png" "$CMD/${NAME}_panel.png" "$X" "$Y" "$W" "$H" <<'PYEOF'
import sys
from PIL import Image
src, dst, x, y, w, h = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4]), int(sys.argv[5]), int(sys.argv[6])
im = Image.open(src)
# keep an 8 px margin so the border is visible
pad = 8
box = (max(0, x - pad), max(0, y - pad), min(im.width, x + w + pad), min(im.height, y + h + pad))
im.crop(box).save(dst)
print('cropped -> %s  %dx%d' % (dst, box[2] - box[0], box[3] - box[1]))
PYEOF
