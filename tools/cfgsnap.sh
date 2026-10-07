#!/usr/bin/env bash
# 配置快照 / 还原 —— 防止调试时把用户调好的值弄丢。
#
# 用法：
#   bash tools/cfgsnap.sh save      # 存一份快照（覆盖上一份，另存带时间戳的历史）
#   bash tools/cfgsnap.sh restore   # 从最近一次快照还原
#   bash tools/cfgsnap.sh list      # 看有哪些快照
#   bash tools/cfgsnap.sh diff      # 当前配置与快照的差异
#
# 【纪律】任何会用 set / defaults 改配置的调试，都必须：
#     先 save，改完再 restore。
#   教训：此前我反复用 defaults 收尾，把用户自己调的
#   DemandAnimSpeed=300 / DemandDurationMin=76.4 / DemandEscapePenaltyMul=181.4
#   等三十来项覆盖成了代码默认值，且当时没有备份。
set -u

G='<游戏目录>'
CFG="$G/BepInEx/config/nesarf.burgershop.modder.cfg"
DIR="<repo>/cfg-snapshots"
LATEST="$DIR/latest.cfg"

[ -f "$CFG" ] || { echo "找不到配置文件：$CFG"; exit 1; }
mkdir -p "$DIR"

case "${1:-}" in
  save)
    TS=$(date +%Y%m%d-%H%M%S)
    cp "$CFG" "$DIR/$TS.cfg"
    cp "$CFG" "$LATEST"
    echo "已快照 → $DIR/$TS.cfg（同时更新 latest.cfg）"
    ;;
  restore)
    [ -f "$LATEST" ] || { echo "没有快照可还原"; exit 1; }
    cp "$CFG" "$DIR/last-before-restore.cfg"
    cp "$LATEST" "$CFG"
    echo "已从 latest.cfg 还原（还原前的状态存为 last-before-restore.cfg）"
    ;;
  list)
    echo "快照目录：$DIR"
    ls -la "$DIR"/*.cfg 2>/dev/null | awk '{print "  ", $6, $7, $8, $9}'
    ;;
  diff)
    [ -f "$LATEST" ] || { echo "没有快照可比"; exit 1; }
    diff <(grep -aE '^[A-Za-z0-9_]+ = ' "$LATEST") \
         <(grep -aE '^[A-Za-z0-9_]+ = ' "$CFG") \
      | sed 's/^/  /' || true
    ;;
  *)
    sed -n '2,20p' "$0" | sed 's/^# \{0,1\}//'
    ;;
esac
