#!/usr/bin/env bash
# ============================================================================
# 两棵树之间的同步
#
# 【为什么需要这个脚本】
#
# 这个项目有两棵树：
#     <仓库上级>/burger-shop          工作副本（编译在这里）
#     <仓库上级>/burger-shop-publish  发布副本（git 仓库在这里）
#
# 它们的流向【不是单向的】—— 按文件类别分：
#
#     代码 / 工具   plugin/ trainer/ installer/ tools/
#                   工作副本 为权威  →  工作 → 发布
#
#     文档          README / 使用说明 / KNOWN-ISSUES / CHANGELOG / ENGINEERING-NOTES
#                   发布副本 为权威  →  发布 → 工作
#
#     dist/使用说明.md 特殊：它是安装包的输入（会被打进 payload.zip），
#                   但内容归文档管 → 也走 发布 → 工作
#
# 【为什么之前老出问题】
#
# 我一直用【一个方向】处理【两类文件】：
#   - 拿发布副本覆盖工作副本 → 抽出来的方法丢了、拆分过的文档回退了
#   - 拿工作副本覆盖发布副本 → 本机路径又混进说明书
# 每一边都只对了一半。
#
# 【用法】
#     bash tools/sync.sh check    只看差异，不动文件
#     bash tools/sync.sh code     代码：工作 → 发布
#     bash tools/sync.sh docs     文档：发布 → 工作
#     bash tools/sync.sh all      先 docs 再 code
#
# 【规矩】不要手工 cp。要走就走这里。
# ============================================================================
set -u

# 从脚本自身位置推，不写死绝对路径 ——
# 写死的话 static-check 的规则 (6) 会（正确地）报本机路径，也就没法进仓库了。
WORK="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUB="$(cd "$WORK/../burger-shop-publish" && pwd)"

# 代码与工具：工作 → 发布
CODE=(
  plugin/BurgerShopModder.cs
  plugin/BurgerShopModder.csproj
  plugin/GameBindings.cs
  plugin/GameCommandServer.cs
  plugin/CfgGuard.cs
  plugin/ActivityLogger.cs
  trainer/BurgerShopTrainer.cs
  trainer/BurgerShopTrainer.csproj
  tools/static-check.py
  dist/BurgerShopModder.dll
)

# installer/：两边都有对方没有的东西 —— 发布副本里是我清理时加的说明注释，
# 工作副本里是后来的改动。**不能盲目同步**，要人工看 diff。
# 曾把它当"代码"同步过去，结果把清理过的版本又弄脏了（路径 + 装饰符都回来了）。
INSTALLER=(
  installer/Installer.cs
  installer/Installer.csproj
  installer/make_payload.ps1
  installer/fix_payload.py
)

# 文档：发布 → 工作
DOCS=(
  README.md
  CHANGELOG.md
  KNOWN-ISSUES.md
  ENGINEERING-NOTES.md
  LICENSE
)

do_check() {
  echo "=== 代码（应 工作 → 发布）==="
  for f in "${CODE[@]}"; do
    if [ ! -f "$WORK/$f" ]; then echo "  工作缺  $f"; continue; fi
    if [ ! -f "$PUB/$f" ];  then echo "  发布缺  $f"; continue; fi
    if diff -q "$WORK/$f" "$PUB/$f" >/dev/null 2>&1; then
      echo "  同      $f"
    else
      echo "  异      $f   (工作 $(wc -c < "$WORK/$f") / 发布 $(wc -c < "$PUB/$f"))"
    fi
  done
  echo
  echo "=== 文档（应 发布 → 工作）==="
  for f in "${DOCS[@]}"; do
    if [ ! -f "$PUB/$f" ]; then echo "  发布缺  $f"; continue; fi
    if [ ! -f "$WORK/$f" ]; then echo "  工作缺  $f  (需从发布取)"; continue; fi
    if diff -q "$WORK/$f" "$PUB/$f" >/dev/null 2>&1; then
      echo "  同      $f"
    else
      echo "  异      $f   (工作 $(wc -c < "$WORK/$f") / 发布 $(wc -c < "$PUB/$f"))"
    fi
  done
  echo
  echo "=== installer/（需人工看 diff，脚本不自动同步）==="
  for f in "${INSTALLER[@]}"; do
    if [ ! -f "$WORK/$f" ] || [ ! -f "$PUB/$f" ]; then echo "  缺     $f"; continue; fi
    if diff -q "$WORK/$f" "$PUB/$f" >/dev/null 2>&1; then
      echo "  同      $f"
    else
      echo "  异      $f   (工作 $(wc -c < "$WORK/$f") / 发布 $(wc -c < "$PUB/$f"))  << 人工确认"
    fi
  done
  echo
  for f in 使用说明.md dist/使用说明.md dist/KNOWN-ISSUES.md dist/CHANGELOG.md; do
    if [ -f "$PUB/$f" ]; then
      if [ -f "$WORK/$f" ] && diff -q "$WORK/$f" "$PUB/$f" >/dev/null 2>&1; then
        echo "  同      $f"
      else
        echo "  异/缺   $f  (需从发布取)"
      fi
    fi
  done
}

# 本机路径：发布副本里必须一个都没有。
# 踩过好几次：工作副本的 installer / 说明书里带着本机路径，
# 一同步就把清理过的发布副本又弄脏了。
# 注意：这里是【文件里的字面量】，ERE 里两个反斜杠才表示一个反斜杠。
# 写成少了转义层的那个写法的话 \+ 是字面加号，永远匹配不到（踩过一次）。
LEAKPAT='E:\+DSH|D:\+Four Nights|NESARFDX'

has_leak() {
  grep -qE "$LEAKPAT" "$1" 2>/dev/null
}

do_code() {
  echo "=== 代码：工作 → 发布 ==="
  local n=0
  local blocked=0
  for f in "${CODE[@]}"; do
    [ -f "$WORK/$f" ] || { echo "  跳过（工作没有）: $f"; continue; }
    if [ -f "$PUB/$f" ] && diff -q "$WORK/$f" "$PUB/$f" >/dev/null 2>&1; then continue; fi
    # 【闸门】要同步过去的文件里不允许有本机路径
    if has_leak "$WORK/$f"; then
      echo "  拒绝 $f —— 工作副本里有本机路径，先清干净再同步"
      blocked=$((blocked+1))
      continue
    fi
    mkdir -p "$(dirname "$PUB/$f")"
    cp "$WORK/$f" "$PUB/$f" && { echo "  复制 $f"; n=$((n+1)); }
  done
  echo "  共 $n 个文件"$([ $blocked -gt 0 ] && echo "，拒绝 $blocked 个（见上）")
}

do_docs() {
  echo "=== 文档：发布 → 工作 ==="
  local n=0
  for f in "${DOCS[@]}" 使用说明.md dist/使用说明.md dist/KNOWN-ISSUES.md dist/CHANGELOG.md; do
    [ -f "$PUB/$f" ] || continue
    if [ -f "$WORK/$f" ] && diff -q "$WORK/$f" "$PUB/$f" >/dev/null 2>&1; then continue; fi
    mkdir -p "$(dirname "$WORK/$f")"
    cp "$PUB/$f" "$WORK/$f" && { echo "  复制 $f"; n=$((n+1)); }
  done
  echo "  共 $n 个文件"
}

case "${1:-check}" in
  check) do_check ;;
  code)  do_code ;;
  docs)  do_docs ;;
  all)   do_docs; echo; do_code ;;
  *)     echo "用法: bash tools/sync.sh [check|code|docs|all]"; exit 2 ;;
esac
