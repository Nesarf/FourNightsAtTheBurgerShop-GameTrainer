#!/usr/bin/env bash
# ============================================================================
# Syncing between the two trees
#
# [Why this script exists]
#
# This project has two trees:
#     <parent>/burger-shop          working copy (compilation happens here)
#     <parent>/burger-shop-publish  release copy (the git repository lives here)
#
# The flow is NOT one-directional — it depends on the kind of file:
#
#     code / tools  plugin/ trainer/ installer/ tools/
#                   the working copy is authoritative  ->  work -> release
#
#     docs          README / 使用说明 / KNOWN-ISSUES / CHANGELOG / ENGINEERING-NOTES
#                   the release copy is authoritative  ->  release -> work
#
#     The installer and CI both read 使用说明.md at the root (no copy under dist/).
#
# [Why this kept going wrong]
#
# One direction was used for both kinds of files:
#   - copying the release copy over the working copy -> extracted methods lost, split docs reverted
#   - copying the working copy over the release copy -> local paths leaked into the manual
# Either way only half of it was right.
#
# [Usage]
#     bash tools/sync.sh check     show the differences only, change nothing
#     bash tools/sync.sh code      code: work -> release
#     bash tools/sync.sh docs      docs: release -> work
#     bash tools/sync.sh all       docs first, then code
#
# [Rule] Do not copy by hand. Go through this script.
# ============================================================================
set -u

# Derive from the script's own location; never hard-code an absolute path —
# a literal local path would (correctly) trip static-check rule (6) and the
# file could not enter the repository.
#
# [There used to be a form here that silently disabled the whole script]
# WORK=<self>/.. and PUB=$WORK/../burger-shop-publish. From that formula the script
# only made sense in the working copy, yet a copy sat in both trees. Run from the
# release copy, WORK resolved to the release copy itself and PUB to
# "release copy/../burger-shop-publish", which is the release copy again.
# So the two variables pointed at the SAME directory, every diff was equal, the script
# printed "same" all the way down and ended with "0 files" — which looked like a
# successful sync. v1.0.9 was lost exactly that way: sources stuck at v1.0.8, dist
# stuck at v1.0.8, README checksums equally stale, three-way consistent, and the
# static check had nothing to complain about.
#
# So it now recognises which tree it is in by DIRECTORY NAME; the other one is its sibling.
SELF="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
case "$(basename "$SELF")" in
  burger-shop-publish) PUB="$SELF"; WORK="$(cd "$SELF/../burger-shop" && pwd)" ;;
  burger-shop)         WORK="$SELF"; PUB="$(cd "$SELF/../burger-shop-publish" && pwd)" ;;
  *) echo "cannot tell which tree this is (directory name is $(basename "$SELF")), stopping." >&2; exit 2 ;;
esac
if [ "$WORK" = "$PUB" ]; then
  echo "the two trees resolve to the same directory ($WORK), stopping — the path derivation must be wrong." >&2
  exit 2
fi

# Code and tools: work -> release
#
# [This list has missed things before]
# At v1.0.9 it had neither GameBuild.cs nor PresetStore.cs, nor the three compiled
# artefacts under dist. The result: sources in the release copy stuck at v1.0.8, the
# binaries under dist stuck at v1.0.8, and the README checksums equally old — all three
# stale together. Static-check rule (10) compares README against dist for mutual
# consistency, so both being old reported "passed". A user downloading the release and
# verifying against the README would see all three mismatch.
#
# Therefore: every file that belongs in the release copy must appear in this list.
# Adding a .cs and forgetting to list it here creates a hole that only a release exposes.
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
  shared/Locale.cs
  tools/static-check.py
  tools/sync.sh
  dist/2×9+9L+6[ED]+7+2×45→1[C]+LS.dll
  dist/数值修改器.exe
  dist/数值修改器.exe.config
  dist/汉堡店修改器-安装程序.exe
  dist/汉堡店修改器-安装程序.exe.config
  dist/卸载还原.ps1
)

# installer/: the two trees once held things the other did not (the release copy had
# comments added during a cleanup, the working copy had later changes), so differences
# could only be eyeballed. They have since converged to mechanical differences ("version
# number only"), so this went into the automatic sync — eyeballing diffs is unreliable:
# nobody looked at v1.0.9, and the release copy's make_payload.ps1 still pointed at a
# dist/使用说明.md that had long been deleted, so packaging there was bound to fail.
INSTALLER=(
  installer/Installer.cs
  installer/Installer.csproj
  installer/make_payload.ps1
  installer/fix_payload.py
)

# Docs: release -> work
DOCS=(
  README.md
  README.en.md
  CHANGELOG.md
  CHANGELOG.en.md
  KNOWN-ISSUES.md
  KNOWN-ISSUES.en.md
  ENGINEERING-NOTES.md
  ENGINEERING-NOTES.en.md
  LICENSE
)

do_check() {
  echo "=== code (should be work -> release) ==="
  for f in "${CODE[@]}"; do
    if [ ! -f "$WORK/$f" ]; then echo "  work missing  $f"; continue; fi
    if [ ! -f "$PUB/$f" ];  then echo "  release missing  $f"; continue; fi
    if diff -q "$WORK/$f" "$PUB/$f" >/dev/null 2>&1; then
      echo "  same    $f"
    else
      echo "  differs $f   (work $(wc -c < "$WORK/$f") / release $(wc -c < "$PUB/$f"))"
    fi
  done
  echo
  echo "=== docs (should be release -> work) ==="
  for f in "${DOCS[@]}"; do
    if [ ! -f "$PUB/$f" ]; then echo "  release missing  $f"; continue; fi
    if [ ! -f "$WORK/$f" ]; then echo "  work missing  $f  (must come from release)"; continue; fi
    if diff -q "$WORK/$f" "$PUB/$f" >/dev/null 2>&1; then
      echo "  same    $f"
    else
      echo "  differs $f   (work $(wc -c < "$WORK/$f") / release $(wc -c < "$PUB/$f"))"
    fi
  done
  echo
  echo "=== installer/ (also work -> release) ==="
  for f in "${INSTALLER[@]}"; do
    if [ ! -f "$WORK/$f" ] || [ ! -f "$PUB/$f" ]; then echo "  missing $f"; continue; fi
    if diff -q "$WORK/$f" "$PUB/$f" >/dev/null 2>&1; then
      echo "  same    $f"
    else
      echo "  differs $f   (work $(wc -c < "$WORK/$f") / release $(wc -c < "$PUB/$f"))"
    fi
  done
  echo
  for f in 使用说明.md 使用说明.en.md; do
    if [ -f "$PUB/$f" ]; then
      if [ -f "$WORK/$f" ] && diff -q "$WORK/$f" "$PUB/$f" >/dev/null 2>&1; then
        echo "  same    $f"
      else
        echo "  differs/missing $f  (must come from release)"
      fi
    fi
  done
}

# Local paths: the release copy must contain none at all.
# This has been hit several times: the working copy's installer / manual carried local
# paths, and one sync re-dirtied a release copy that had already been cleaned.
# Note: these are LITERALS IN THE FILE — in ERE two backslashes mean one backslash.
# Writing the version with one less escaping layer makes \+ a literal plus sign, which
# never matches (that happened once).
LEAKPAT='E:\+DSH|D:\+Four Nights|NESARFDX'

has_leak() {
  grep -qE "$LEAKPAT" "$1" 2>/dev/null
}

# Binary files (exe/dll/ico) cannot be judged by a text rule for "does it contain a
# local path": grep hits a NUL byte, announces "Binary file matches", and the result is
# random. So detect binary first and skip the gate for those — they are build output, and
# path problems are covered by static-check's source-side rule.
is_binary() {
  local a b
  a=$(head -c 8192 "$1" 2>/dev/null | wc -c)
  b=$(head -c 8192 "$1" 2>/dev/null | tr -d '\000' | wc -c)
  [ "$a" -ne "$b" ]
}

do_code() {
  echo "=== code: work -> release ==="
  local n=0
  local blocked=0
  for f in "${CODE[@]}" "${INSTALLER[@]}"; do
    [ -f "$WORK/$f" ] || { echo "  skipped (not in work): $f"; continue; }
    if [ -f "$PUB/$f" ] && diff -q "$WORK/$f" "$PUB/$f" >/dev/null 2>&1; then continue; fi
    # [gate] a file about to be synced must not contain a local path
    # binaries are skipped: text cannot be searched there, and they are build output anyway
    if ! is_binary "$WORK/$f" && has_leak "$WORK/$f"; then
      echo "  refused $f — the working copy contains a local path; clean it before syncing"
      blocked=$((blocked+1))
      continue
    fi
    mkdir -p "$(dirname "$PUB/$f")"
    cp "$WORK/$f" "$PUB/$f" && { echo "  copied $f"; n=$((n+1)); }
  done
  echo "  $n files"$([ $blocked -gt 0 ] && echo ", $blocked refused (see above)")
}

do_docs() {
  echo "=== docs: release -> work ==="
  local n=0
  for f in "${DOCS[@]}" 使用说明.md 使用说明.en.md; do
    [ -f "$PUB/$f" ] || continue
    if [ -f "$WORK/$f" ] && diff -q "$WORK/$f" "$PUB/$f" >/dev/null 2>&1; then continue; fi
    mkdir -p "$(dirname "$WORK/$f")"
    cp "$PUB/$f" "$WORK/$f" && { echo "  copied $f"; n=$((n+1)); }
  done
  echo "  $n files"
}

case "${1:-check}" in
  check) do_check ;;
  code)  do_code ;;
  docs)  do_docs ;;
  all)   do_docs; echo; do_code ;;
  *)     echo "usage: bash tools/sync.sh [check|code|docs|all]"; exit 2 ;;
esac
