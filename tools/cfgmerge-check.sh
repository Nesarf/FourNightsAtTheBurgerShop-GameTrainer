#!/usr/bin/env bash
# =============================================================================
# Concurrent config writes · the merge path self-test (KNOWN-ISSUES #2)
#
# [Why it has to be run by hand] The check requires that nothing else is writing
# the config. While the user is playing and tuning, the cfg gets rewritten by the
# game at any moment, and the real result cannot be measured. So this is a script
# you run in one go during a quiet window.
#
# What it tests: two writers (the in-game `set` command, and an external process
# editing the file directly) each change a DIFFERENT key; both changes must survive.
#
# Verdict: if either side's change is overwritten by the other -> FAIL
# =============================================================================
set -u

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
GAME="$(cat "$ROOT/tools/gamepath.txt" 2>/dev/null | tr -d '\r')"
CFG="$GAME/BepInEx/config/nesarf.burgershop.modder.cfg"
CMD="$GAME/BepInEx/l2d_dump/cmd"

[ -f "$CFG" ] || { echo "config not found: $CFG"; exit 1; }

echo "== concurrent config merge self-test =="
echo "  config: $CFG"
echo

# ---- 0. quiet check: has the cfg been touched recently ----
before_mtime=$(stat -c %Y "$CFG" 2>/dev/null || stat -f %m "$CFG")
echo "  waiting 8 s to confirm nothing else is writing (do not touch the trainer now)..."
sleep 8
after_mtime=$(stat -c %Y "$CFG" 2>/dev/null || stat -f %m "$CFG")
if [ "$before_mtime" != "$after_mtime" ]; then
  echo "   the config changed within those 8 s — another writer is active."
  echo "    Close the trainer panel / stop tuning, then run this again."
  exit 2
fi
echo "   quiet (no writes in 8 s)"
echo

# ---- 1. snapshot ----
cp "$CFG" /tmp/cfgmerge.orig
# [Pose aware] The `set` command writes the "current pose" copy (PIdx goes through
# PoseIdx()), so the current pose has to be asked for first — otherwise the wrong
# key is verified. The first run of this test failed exactly there.
POSE_RAW=$(printf 'states
' > "$CMD/in.txt"; sleep 2.5; tail -6 "$CMD/out.txt" | grep -a '状态机' | tail -1)
case "$POSE_RAW" in
  *"centerGirlState=Sit"*)   SUF="_Sit";   PNAME="Riding" ;;
  *"centerGirlState=Osiri"*) SUF="_Osiri"; PNAME="Rear" ;;
  *)                         SUF="_Fella"; PNAME="Fellatio" ;;
esac
echo "  current pose: $PNAME (set writes the $SUF copy)"

A_KEY="AttackUrgeChance$SUF"
B_KEY="SpankSpeedGain$SUF"
A_OLD=$(grep -a "^$A_KEY = " "$CFG" | head -1 | sed 's/.*= //')
B_OLD=$(grep -a "^$B_KEY = " "$CFG" | head -1 | sed 's/.*= //')
echo "  start: $A_KEY=$A_OLD  $B_KEY=$B_OLD"

A_NEW=$(python3 -c "print(round(float('${A_OLD:-20}')+7,4))")
B_NEW=$(python3 -c "print(round(float('${B_OLD:-20}')+9,4))")
echo "  about to write: game side $A_KEY->$A_NEW (via the set command)  external side $B_KEY->$B_NEW (edit the file directly)"
echo

# ---- 2. two writers, almost simultaneously ----
#   game side: through the set command (which triggers Config.Save and rewrites the whole cfg)
printf 'set AttackUrgeChance %s\n' "$A_NEW" > "$CMD/in.txt" &
#   external side: change a different key in the file
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

# ---- 3. verdict ----
A_END=$(grep -a "^$A_KEY = " "$CFG" | head -1 | sed 's/.*= //')
B_END=$(grep -a "^$B_KEY = " "$CFG" | head -1 | sed 's/.*= //')
echo "  end: $A_KEY=$A_END  $B_KEY=$B_END"
echo

fail=0
if [ "$(python3 -c "print(abs(float('${A_END:-0}')-float('${A_NEW:-0}'))<0.01)")" = "True" ]; then
  echo "   the game side's change survived"
else
  echo "   the game side's change was lost (expected $A_NEW, got $A_END)"; fail=1
fi
if [ "$(python3 -c "print(abs(float('${B_END:-0}')-float('${B_NEW:-0}'))<0.01)")" = "True" ]; then
  echo "   the external side's change survived"
else
  echo "   the external side's change was overwritten (expected $B_NEW, got $B_END)"; fail=1
fi

echo
if [ $fail -eq 0 ]; then
  echo "result: PASS — neither side lost anything, the merge path holds"
  cp "$CFG" /tmp/cfgmerge.pass
else
  echo "result: FAIL — something was overwritten. Evidence in /tmp/cfgmerge.orig (the starting state)"
  echo "       current config: $CFG"
fi

# ---- 4. restore ----
grep -a '^' /tmp/cfgmerge.orig > "$CFG"
echo "starting config restored."
exit $fail
