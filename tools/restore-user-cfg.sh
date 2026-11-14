#!/usr/bin/env bash
# Restore the values the user had tuned before a debugging session.
#
# [Historical script] The values below are frozen from the cfg dump taken during one
# rescue in October 2026. Do NOT run this blindly today — the user's live config has
# moved on since. Read it as a record of what those values were, or edit the table
# before running it.
#
# Only DemandUrgeMin/Max were test values set by the developer (100/100) and are
# deliberately not restored; sensible values are used instead.
set -u
G='<游戏目录>'
CFG="$G/BepInEx/config/nesarf.burgershop.modder.cfg"
[ -f "$CFG" ] || { echo "config file not found"; exit 1; }

# back up the current state first
cp "$CFG" "$CFG.before-restore" 2>/dev/null && echo "current state backed up -> nesarf.burgershop.modder.cfg.before-restore"

# rewrite the Value of these keys with python
python3 - "$CFG" <<'PYEOF'
import re, sys
path = sys.argv[1]
# values the user had tuned (from the cfg contents dumped earlier)
USER = {
    # chain drain
    "ChainEcstasyGain": "true",
    "ChainGainPerDrain": "8",
    "ChainSoftCap": "60",
    "ChainWobble": "20",
    "ChainWobbleHz": "0.8",
    "KyuseiMaxChain": "6",
    # tremor
    "TremorZeroHold": "true",
    "TremorZeroHoldTime": "8",
    "TremorZeroHoldAmp": "8",
    # demand mode
    "DemandEnabled": "true",
    "DemandUrgeMin": "8",            # 100/100 was the developer's test value, not restored
    "DemandUrgeMax": "22",
    "DemandEscapePenaltyChance": "31.88406",
    "DemandEscapePenaltyMul": "181.3768",
    "DemandBlockFella": "true",
    "DemandFellaDecay": "40",
    "DemandOsiriBoost": "3",
    "DemandMaxStacks": "9",
    "DemandSpeedSplit": "true",
    "DemandAnimSpeed": "300",
    "DemandAttBonus": "250",
    "DemandDurationMin": "76.42029",
    "DemandDurationMax": "180.0725",
    # demand urge / impulses
    "AttackUrgeChance": "8",
    "AttackUrgeGain": "3",
    "SpankSpeedEnabled": "true",
    "SpankSpeedGain": "12",
    "SpankSpeedStack": "60",
    "SpankSpeedRise": "9",
    "SpankSpeedDecay": "0.25",
}
txt = open(path, encoding='utf-8').read()
lines = txt.split('\n')
out, n = [], 0
for l in lines:
    m = re.match(r'^(\s*)([A-Za-z0-9_]+)(\s*=\s*)(.*)$', l)
    if m and m.group(2) in USER:
        out.append(f"{m.group(1)}{m.group(2)}{m.group(3)}{USER[m.group(2)]}")
        n += 1
    else:
        out.append(l)
open(path, 'w', encoding='utf-8', newline='').write('\n'.join(out))
print(f"restored {n} entries")
PYEOF

echo
echo "=== key values after restore ==="
grep -aE "^(DemandAnimSpeed|DemandAttBonus|DemandDurationMin|DemandDurationMax|DemandEscapePenalty|DemandFellaDecay|DemandOsiriBoost|DemandMaxStacks|KyuseiMaxChain|AttackUrge|SpankSpeedGain|ChainSoftCap)" "$CFG"
