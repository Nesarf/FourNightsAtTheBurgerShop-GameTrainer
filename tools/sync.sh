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
#     安装包与 CI 都读根目录的 使用说明.md（dist/ 里不放副本）
#     （dist/ 下不再放 KNOWN-ISSUES / CHANGELOG 的副本 —— 打包不用它们，留着只会各自漂移）（会被打进 payload.zip），
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
#
# 【这里曾经有个会让整个脚本静默失效的写法】
# 原来是 WORK=<自身>/.. 、PUB=$WORK/../burger-shop-publish。
# 从这个公式看，脚本只应该在工作副本里跑。可它两边都放了一份 ——
# 在发布副本里跑时 WORK 会算成发布副本自己，PUB 又算成
# "发布副本/../burger-shop-publish"，还是发布副本自己。
# 于是【两个变量指向同一个目录】，每条 diff 都相等，脚本打印
# "同" 一片、最后 "共 0 个文件"，看起来像同步成功了。
# v1.0.9 就是这样漏掉的：源码停在 v1.0.8，dist 停在 v1.0.8，README 的
# 校验和跟着旧，三方一致，静态检查也没话说。
#
# 所以现在按【目录名】认出自己在哪一棵树，另一棵就是它的兄弟目录。
SELF="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
case "$(basename "$SELF")" in
  burger-shop-publish) PUB="$SELF"; WORK="$(cd "$SELF/../burger-shop" && pwd)" ;;
  burger-shop)         WORK="$SELF"; PUB="$(cd "$SELF/../burger-shop-publish" && pwd)" ;;
  *) echo "无法判断这是哪一棵树（目录名是 $(basename "$SELF")），停止。" >&2; exit 2 ;;
esac
if [ "$WORK" = "$PUB" ]; then
  echo "两棵树算出来是同一个目录（$WORK），停止 —— 这一定是路径推导出了问题。" >&2
  exit 2
fi

# 代码与工具：工作 → 发布
#
# 【这份清单曾经漏过东西】
# v1.0.9 发版时它还没有 GameBuild.cs 与 PresetStore.cs，也没有 dist 里那三个
# 编译产物。结果是：源码在发布副本里停在 v1.0.8，dist 里的二进制也停在 v1.0.8，
# 而 README 的校验和跟着一起旧 —— 三方都旧，静态检查第 (10) 条比的是 README 与
# dist 是否互洽，两边一致于是报"通过"。用户从 release 下载新文件、照 README
# 核对，会看到三条全不符。
#
# 所以：凡是要进发布副本的文件，都必须在这一份清单里。
# 新增一个 .cs 却忘了加进来，就等于给自己留了一个只有发版才暴露的洞。
CODE=(
  plugin/BurgerShopModder.cs
  plugin/BurgerShopModder.csproj
  plugin/GameBindings.cs
  plugin/GameCommandServer.cs
  plugin/CfgGuard.cs
  plugin/ActivityLogger.cs
  plugin/GameBuild.cs
  plugin/PresetStore.cs
  plugin/Localization.cs
  plugin/lang/en.txt
  trainer/BurgerShopTrainer.cs
  trainer/BurgerShopTrainer.csproj
  tools/static-check.py
  tools/sync.sh
  dist/2×9+9L+6[ED]+7+2×45→1[C]+LS.dll
  dist/数值修改器.exe
  dist/数值修改器.exe.config
  dist/汉堡店修改器-安装程序.exe
  dist/汉堡店修改器-安装程序.exe.config
  dist/卸载还原.ps1
)

# installer/：曾经两边都有对方没有的东西（发布副本有清理时加的注释，
# 工作副本有后来的改动），所以以前只能人工看 diff。
# 现在两边已经收敛到"只差版本号"这种机械差异了，就并进自动同步 ——
# 人工看 diff 这件事靠不住：v1.0.9 就是没人看，于是发布副本里的
# make_payload.ps1 还指着早就删掉的 dist/使用说明.md，在那边打包必然断。
INSTALLER=(
  installer/Installer.cs
  installer/Installer.csproj
  installer/make_payload.ps1
  installer/fix_payload.py
)

# 文档：发布 → 工作
DOCS=(
  README.md
  README.en.md
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
  echo "=== installer/（同样 工作 → 发布）==="
  for f in "${INSTALLER[@]}"; do
    if [ ! -f "$WORK/$f" ] || [ ! -f "$PUB/$f" ]; then echo "  缺     $f"; continue; fi
    if diff -q "$WORK/$f" "$PUB/$f" >/dev/null 2>&1; then
      echo "  同      $f"
    else
      echo "  异      $f   (工作 $(wc -c < "$WORK/$f") / 发布 $(wc -c < "$PUB/$f"))"
    fi
  done
  echo
  for f in 使用说明.md 使用说明.en.md; do
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

# 二进制文件（exe/dll/ico）没法用文本规则判断"里面有没有本机路径"：
# grep 碰到 NUL 字节会直接宣布 Binary file matches，结果是随机命中。
# 所以先认出是不是二进制，是的话跳过这道闸门 ——
# 它们的来源是编译产物，路径问题由 static-check 的源码检查负责。
is_binary() {
  local a b
  a=$(head -c 8192 "$1" 2>/dev/null | wc -c)
  b=$(head -c 8192 "$1" 2>/dev/null | tr -d '\000' | wc -c)
  [ "$a" -ne "$b" ]
}

do_code() {
  echo "=== 代码：工作 → 发布 ==="
  local n=0
  local blocked=0
  for f in "${CODE[@]}" "${INSTALLER[@]}"; do
    [ -f "$WORK/$f" ] || { echo "  跳过（工作没有）: $f"; continue; }
    if [ -f "$PUB/$f" ] && diff -q "$WORK/$f" "$PUB/$f" >/dev/null 2>&1; then continue; fi
    # 【闸门】要同步过去的文件里不允许有本机路径
    # 二进制跳过：那里面查不了文本，而且它们本来就是编译产物
    if ! is_binary "$WORK/$f" && has_leak "$WORK/$f"; then
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
  for f in "${DOCS[@]}" 使用说明.md 使用说明.en.md; do
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
