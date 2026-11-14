#!/usr/bin/env bash
# Config snapshot / restore — so a debugging session cannot lose the user's tuned values.
#
# Usage:
#   bash tools/cfgsnap.sh save      # snapshot now (overwrites latest, keeps a timestamped history file)
#   bash tools/cfgsnap.sh restore   # restore from the newest snapshot
#   bash tools/cfgsnap.sh list      # show the snapshots
#   bash tools/cfgsnap.sh diff      # diff the live config against the snapshot
#
# [Discipline] Any debugging session that runs `set` / `defaults` must:
#     save first, restore afterwards.
#   The lesson behind this rule: defaults were used as a cleanup step once, and it
#   overwrote about thirty of the user's own values
#   (DemandAnimSpeed=300 / DemandDurationMin=76.4 / DemandEscapePenaltyMul=181.4 and more)
#   back to the code defaults, with no backup taken beforehand.
set -u

G='<游戏目录>'
CFG="$G/BepInEx/config/nesarf.burgershop.modder.cfg"
DIR="<repo>/cfg-snapshots"
LATEST="$DIR/latest.cfg"

[ -f "$CFG" ] || { echo "config file not found: $CFG"; exit 1; }
mkdir -p "$DIR"

case "${1:-}" in
  save)
    TS=$(date +%Y%m%d-%H%M%S)
    cp "$CFG" "$DIR/$TS.cfg"
    cp "$CFG" "$LATEST"
    echo "snapshot saved -> $DIR/$TS.cfg (latest.cfg updated too)"
    ;;
  restore)
    [ -f "$LATEST" ] || { echo "no snapshot to restore"; exit 1; }
    cp "$CFG" "$DIR/last-before-restore.cfg"
    cp "$LATEST" "$CFG"
    echo "restored from latest.cfg (the pre-restore state is in last-before-restore.cfg)"
    ;;
  list)
    echo "snapshot directory: $DIR"
    ls -la "$DIR"/*.cfg 2>/dev/null | awk '{print "  ", $6, $7, $8, $9}'
    ;;
  diff)
    [ -f "$LATEST" ] || { echo "no snapshot to compare against"; exit 1; }
    diff <(grep -aE '^[A-Za-z0-9_]+ = ' "$LATEST") \
         <(grep -aE '^[A-Za-z0-9_]+ = ' "$CFG") \
      | sed 's/^/  /' || true
    ;;
  *)
    sed -n '2,20p' "$0" | sed 's/^# \{0,1\}//'
    ;;
esac
