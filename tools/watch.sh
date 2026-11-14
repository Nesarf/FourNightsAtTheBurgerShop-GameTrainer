#!/usr/bin/env bash
# Continuously pull game state — run it as a background job and you get a timeline.
#
# Why this exists: verification used to go "let the user play a round, then read the log",
# but the log only fires on specific events, so the in-between states (which pose, how much
# demand urge, which chain segment) had to be reconstructed afterwards. Polling fills that
# timeline in.
#
# Usage:
#   bash tools/watch.sh [rounds] [gap seconds]     # no rounds = run forever; use & for background
#
# Output (both under BepInEx/l2d_dump/cmd/):
#   watch.txt  one line = timestamp + full state block (state machine / demand mode / anim rate / ecstasy / anim layer)
#   watch.csv  compact timeline, good for eyeballing trends
#
# NOTE: the grep patterns below are Chinese log literals on purpose. They only match while
# the plugin's `Language` is `zh`; in English mode the same lines are English and these
# patterns find nothing.
set -u

G='<游戏目录>'
C="$G/BepInEx/l2d_dump/cmd"
TXT="$C/watch.txt"
CSV="$C/watch.csv"

ROUNDS="${1:-0}"          # 0 = forever
GAP="${2:-1.2}"

mkdir -p "$C"
: > "$TXT"
echo "t,pose,subState,syaseing,hp,maxHp,ecstasy,maxEcstasy,urge,stacks,demandLeft,afterglow,chain" > "$CSV"

i=0
while true; do
  i=$((i+1))

  printf 'states\n' > "$C/in.txt"
  sleep "$GAP"

  # The whole response block is needed — taking only the last line drops the
  # state-machine line, and that line holds the pose and per-state values this
  # timeline exists to observe. Only the LAST block is kept: start accumulating
  # from the last occurrence of the state-machine marker, otherwise tail drags in
  # the previous round's tail and the txt file repeats blocks.
  BLOCK=$(tail -30 "$C/out.txt" 2>/dev/null | sed "s/^[0-9:.]*  //" \
          | awk '/^状态机:/{buf=""} {buf=buf $0 "|"} END{printf "%s", buf}' \
          | grep -aoE "(状态机|索取模式|动画速率|绝顶一览|动画层[^|]*):[^|]*" | tr "\n" "|")

  # The ecstasy overview (HP / ecstasy / the rates) lives in a DIFFERENT command —
  # `states` does not include it, so pull it separately.
  printf 'ecs\n' > "$C/in.txt"
  sleep "$GAP"
  ECSLINE=$(tail -3 "$C/out.txt" 2>/dev/null | sed "s/^[0-9:.]*  //" \
            | grep -a '^绝顶一览:' | tail -1)
  BLOCK="${BLOCK}|${ECSLINE}"

  TS=$(date +%H:%M:%S)
  printf '[%s] %s\n' "$TS" "$BLOCK" >> "$TXT"

  POSE=$(printf '%s' "$BLOCK" | grep -oE 'centerGirlState=[A-Za-z]+' | head -1 | sed 's/^centerGirlState=//')
  SUB=$(printf '%s' "$BLOCK"  | grep -oE '(osiriState|sitState|tabemiAttState)=[a-z0-9]+' | head -1 | cut -d= -f2)
  SYA=$(printf '%s' "$BLOCK"  | grep -oE 'Syaseing=[0-9]+' | head -1 | sed 's/^Syaseing=//')
  HPF=$(printf '%s' "$BLOCK"  | grep -oE 'HP=[0-9.]+/[0-9.]+' | head -1 | sed 's/^HP=//')
  ECS=$(printf '%s' "$BLOCK"  | grep -oE 'CurrentEcstasy=[0-9.]+' | head -1 | sed 's/^CurrentEcstasy=//')
  MAXE=$(printf '%s' "$BLOCK" | grep -oE 'maxEcstasy=[0-9.]+' | head -1 | sed 's/^maxEcstasy=//')
  URGE=$(printf '%s' "$BLOCK" | grep -oE '索取欲=[0-9.]+' | head -1 | sed 's/^索取欲=//')
  STK=$(printf '%s' "$BLOCK"  | grep -oE '[0-9]+ 段' | head -1 | cut -d' ' -f1)
  LEFT=$(printf '%s' "$BLOCK" | grep -oE '剩余 [0-9.]+s' | head -1 | grep -oE '[0-9.]+')
  AG=$(printf '%s' "$BLOCK"   | grep -oE '余韵剩=[0-9]+' | head -1 | sed 's/^余韵剩=//')
  CH=$(printf '%s' "$BLOCK"   | grep -oE '触发过[0-9]+次' | head -1 | grep -oE '[0-9]+')

  printf '%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s\n' \
    "$(date +%s)" "${POSE:-}" "${SUB:-}" "${SYA:-}" \
    "${HPF%%/*}" "${HPF##*/}" "${ECS:-}" "${MAXE:-}" \
    "${URGE:-}" "${STK:-0}" "${LEFT:-}" "${AG:-0}" "${CH:-0}" >> "$CSV"

  if [ "$ROUNDS" -gt 0 ] && [ "$i" -ge "$ROUNDS" ]; then
    echo "pulled $i rounds, stopping"
    break
  fi
done
