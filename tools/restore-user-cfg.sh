#!/usr/bin/env bash
# 还原用户此前调好的配置值。
#
# 依据：我在调试过程中 dump 过的 cfg 内容（见会话记录）。
# 只有 DemandUrgeMin/Max 那两项是我自己设为 100 的测试值，不该还原，用合理值。
set -u
G='<游戏目录>'
CFG="$G/BepInEx/config/nesarf.burgershop.modder.cfg"
[ -f "$CFG" ] || { echo "找不到配置文件"; exit 1; }

# 先备份当前状态
cp "$CFG" "$CFG.before-restore" 2>/dev/null && echo "已备份当前 → nesarf.burgershop.modder.cfg.before-restore"

# 用 python 精确改这些键的 Value
python3 - "$CFG" <<'PYEOF'
import re, sys
path = sys.argv[1]
# 用户自己调过的值（来自我 earlier dump 到的 cfg 内容）
USER = {
    # 连榨
    "ChainEcstasyGain": "true",
    "ChainGainPerDrain": "8",
    "ChainSoftCap": "60",
    "ChainWobble": "20",
    "ChainWobbleHz": "0.8",
    "KyuseiMaxChain": "6",
    # 动摇
    "TremorZeroHold": "true",
    "TremorZeroHoldTime": "8",
    "TremorZeroHoldAmp": "8",
    # 索取模式
    "DemandEnabled": "true",
    "DemandUrgeMin": "8",            # 100/100 是我的测试值，不还原
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
    # 索取欲 / 冲量
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
print(f"已还原 {n} 项")
PYEOF

echo
echo "=== 还原后的关键值 ==="
grep -aE "^(DemandAnimSpeed|DemandAttBonus|DemandDurationMin|DemandDurationMax|DemandEscapePenalty|DemandFellaDecay|DemandOsiriBoost|DemandMaxStacks|KyuseiMaxChain|AttackUrge|SpankSpeedGain|ChainSoftCap)" "$CFG"
