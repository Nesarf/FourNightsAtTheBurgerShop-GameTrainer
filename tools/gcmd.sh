#!/bin/bash
# ============================================================
#  Send one command to the game and read the reply back
#
#  Usage (works from any directory):
#    bash /e/DSH/build/burger-shop/tools/gcmd.sh state
#    bash /e/DSH/build/burger-shop/tools/gcmd.sh tremor fire
#    bash /e/DSH/build/burger-shop/tools/gcmd.sh tights 1
#
#  No arguments = print the usage block plus the last few replies
# ============================================================
G='<游戏目录>'
C="$G/BepInEx/l2d_dump/cmd"
IN="$C/in.txt"; OUT="$C/out.txt"

if [ ! -d "$C" ]; then
  echo "command directory not found: $C"
  echo "-> the game has to be launched once before the plugin creates it."
  exit 1
fi

if [ $# -eq 0 ]; then
  cat <<'USAGE'
Usage: bash gcmd.sh <command> [args...]       (quoting is optional)

Read state
  state              costume slots / _Op_ fields / part opacity / force state
  parts              opacity on-off list for all 44 parts
  states             state-machine fields (riding / absorb / girl state ...) plus click ownership
  ecs                ecstasy / cap / resistance / absorb rate
  cfg                current value of every config entry
  xcheck             16 invariant cross-checks -> cmd/xcheck.txt
  pix 0 0 1920 1080  read back screen pixels, numbers only

Change state
  tights 0|1|2       tights: none / black / white
  costume tights 1   costume slot (cap/upper/lower/tights/glasses)
  set TremorChance 30   change a config entry (written to disk, survives restart)
  force off|all      force every part visible

Tremor
  tremor status      current wobble state
  tremor fire        fire one tremor manually right now
  tremor on|off      toggle tremor
  tremorlog 5        log the ecstasy waveform frame by frame for 5 s -> cmd/tremor.csv

Watch / record
  xwatch 900         keep cross-checking for 15 minutes, log anomalies only -> cmd/xwatch.txt
  xwatch off         stop
  redlog 8           sample the screen's red channel frame by frame -> cmd/red.csv
  shot <name>        screenshot -> cmd/<name>.png
  ecsgain 20         inject 20 points of ecstasy (for testing the adaptation multiplier)

Scene / misc
  scene              current scene
  scene TabemiMain   switch to the shop scene
  skipdialog on|off  story fast-forward
  colliders          list clickable objects
  click <name>       trigger a button inside the game (does not touch the system mouse)
  quit               leave the game
USAGE
  echo
  echo "-- last few replies --"
  tail -6 "$OUT" 2>/dev/null | sed 's/^/  /'
  exit 0
fi

printf '%s\n' "$*" > "$IN"
sleep 1.5
tail -6 "$OUT" 2>/dev/null
