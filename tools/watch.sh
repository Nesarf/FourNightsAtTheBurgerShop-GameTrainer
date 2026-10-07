#!/usr/bin/env bash
# 持续拉取游戏状态 —— 作为后台任务跑，留一条时间线下来。
#
# 动机：这个会话里我反复靠"让你玩一趟 → 我再看日志"来验证，
# 而日志只在特定事件上打点，中间态（进了哪个姿势、索取欲多少、连榨走到第几段）
# 全靠事后拼。持续拉取能把这条时间线补完整。
#
# 用法：
#   bash tools/watch.sh [轮数] [每轮间隔秒]     # 不给轮数 = 一直跑；后台跑请用 &
#
# 产出（都在 BepInEx/l2d_dump/cmd/）：
#   watch.txt  每行 = 时间戳 + 完整状态块（状态机 / 索取模式 / 动画速率 / 绝顶值 / 动画层）
#   watch.csv  紧凑时间线，便于直接看趋势
set -u

G='<游戏目录>'
C="$G/BepInEx/l2d_dump/cmd"
TXT="$C/watch.txt"
CSV="$C/watch.csv"

ROUNDS="${1:-0}"          # 0 = 无限
GAP="${2:-1.2}"

mkdir -p "$C"
: > "$TXT"
echo "t,pose,subState,syaseing,hp,maxHp,ecstasy,maxEcstasy,urge,stacks,demandLeft,afterglow,chain" > "$CSV"

i=0
while true; do
  i=$((i+1))

  printf 'states\n' > "$C/in.txt"
  sleep "$GAP"

  # 整个响应块都要 —— 只取最后一行会丢掉「状态机」那行，
  # 而姿势与各状态机的值正是最需要连续观察的东西。
  # 只取【最后一块】响应：从最后一次出现「状态机:」开始累积，
  # 否则 tail 会连带上一轮的尾巴，txt 里就出现重复块。
  BLOCK=$(tail -30 "$C/out.txt" 2>/dev/null | sed "s/^[0-9:.]*  //" \
          | awk '/^状态机:/{buf=""} {buf=buf $0 "|"} END{printf "%s", buf}' \
          | grep -aoE "(状态机|索取模式|动画速率|绝顶一览|动画层[^|]*):[^|]*" | tr "\n" "|")

  # 绝顶一览（HP / 绝顶值 / 各种速率）在【另一个命令】里，states 不含它 —— 单独拉一次。
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
    echo "已拉取 $i 轮，结束"
    break
  fi
done
