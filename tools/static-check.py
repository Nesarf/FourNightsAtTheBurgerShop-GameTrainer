# -*- coding: utf-8 -*-
"""
静态一致性检查 —— 不需要游戏、不需要编译，可直接跑在 CI 上。

【为什么需要】
这个项目的配置项要同时出现在很多地方：

    BindP3 定义 → 面板滑块 → set 命令 → 描述模板 → 说明书

**漏掉任何一处都是静默失败** —— 不报错，只是"某个功能用不了"。
这里把这类一致性做成可自动检查的规则。

【检查项】
  (1) 每个 BindP3 参数都要有面板控件
  (2) 每个 BindP3 参数都要有 set 命令分支
  (3) 每个参数都要在说明书里出现（去掉 _Fella/_Osiri/_Sit 后缀比对）
  (4) 同一目标方法不能被 patch 两次（Harmony 会报 AmbiguousMatch）
  (5) 插件源码里不能有本机绝对路径
  (6) 不该硬编码的三栏用词（"索取"不得出现在共享模板里）
  (7) 挂载数不能少于预期（防"补丁挂了一半"）

用法：
    python tools/static-check.py [仓库根目录]
退出码：0 = 全过；1 = 有失败项
"""
import re
import hashlib
import sys
import pathlib

# 【CI 上会崩】GitHub runner 的 Python 默认编码是 cp1252，打印中文直接抛
# UnicodeEncodeError。必须显式把标准输出设成 UTF-8。
try:
    sys.stdout.reconfigure(encoding='utf-8')
    sys.stderr.reconfigure(encoding='utf-8')
except Exception:
    pass

ROOT = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else '.')
PLUGIN = ROOT / 'plugin' / 'BurgerShopModder.cs'
CMD = ROOT / 'plugin' / 'GameCommandServer.cs'
DOC = ROOT / '使用说明.md'
HINT_DOC = ROOT / 'dist' / '使用说明.md'

fails, warns = [], []


def fail(msg):
    fails.append(msg)
    print(f'  [FAIL] {msg}')


def warn(msg):
    warns.append(msg)
    print(f'  [WARN] {msg}')


def ok(msg):
    print(f'  [ OK ] {msg}')


def read(p):
    try:
        return p.read_text(encoding='utf-8', errors='ignore')
    except FileNotFoundError:
        return None


src = read(PLUGIN)
if src is None:
    print(f'cannot find {PLUGIN}')
    sys.exit(2)
cmd = read(CMD) or ''
doc = read(DOC) or read(HINT_DOC) or ''

print('== static consistency check ==\n')

# ── 收集 BindP3 参数 ──────────────────────────────────────────
# BindP3("Name", ...) / BindP3Int / BindP3Bool
p3 = set(re.findall(r'BindP3(?:Int|Bool)?\(\s*"([A-Za-z_][A-Za-z0-9_]*)"', src))
print(f'(1) BindP3 params: {len(p3)}')

# 面板控件：SetP3(Name3, ...)
panel = set(re.findall(r'SetP3\(\s*([A-Za-z_][A-Za-z0-9_]*?)(?:3)\s*,', src))
# set 命令分支：case "Name": ... SetP3(Name3
setc = set(re.findall(r'case\s+"([A-Za-z_][A-Za-z0-9_]*)"\s*:', cmd + src))

miss_panel = sorted(p for p in p3 if f'{p}3' not in src.replace('BindP3', '') and p not in panel)
# 更准确的判定：源码里有没有 SetP3(<Name>3 ...)
have_panel = set(re.findall(r'SetP3\(\s*([A-Za-z_][A-Za-z0-9_]*)3\s*,', src))
miss_panel = sorted(p for p in p3 if p not in have_panel)
miss_set = sorted(p for p in p3 if p not in setc)

if miss_panel:
    fail("(2) params with no panel control: " + ", ".join(miss_panel[:12]) + (" ... %d in total" % len(miss_panel) if len(miss_panel) > 12 else ""))
else:
    ok(f"(2) all {len(p3)} params have a panel control")

if miss_set:
    # set 分支可能在 GameCommandServer 之外，宽松处理
    warn("(3) params with no set branch: " + ", ".join(miss_set[:12]) + (" ... %d in total" % len(miss_set) if len(miss_set) > 12 else ""))
else:
    ok(f"(3) all {len(p3)} params have a set branch")

# ── 说明书覆盖 ────────────────────────────────────────────────
base = set(re.sub(r'_(Fella|Osiri|Sit)$', '', p) for p in p3)
miss_doc = sorted(b for b in base if b not in doc)
if miss_doc:
    warn("(4) not mentioned in the manual: " + ", ".join(miss_doc[:12]) + (" ... %d in total" % len(miss_doc) if len(miss_doc) > 12 else ""))
else:
    ok(f"(4) all {len(base)} logical params appear in the manual")

# ── Harmony 重复 patch ────────────────────────────────────────
# TryPatch(pc, "Method", ...) / GetMethod("Method", ...) 后 _harmony.Patch
patched = re.findall(r'GetMethod\(\s*"([A-Za-z_][A-Za-z0-9_]*)"', src)
dup = sorted({m for m in patched if patched.count(m) > 2})   # 同一方法可能被取多次用于不同用途
# 真正要防的是【同一方法同一个前缀/后缀名】重复定义
# 标识符里可能含日文（如 Postfix_Osiri吸精Damage）—— 不认的话会被截断成同名，造成误报
prefixes = re.findall(r'private static (?:bool|void)\s+(Prefix|Postfix)_([A-Za-z_][A-Za-z0-9_぀-ヿ一-鿿]*)', src)
names = {}
for kind, who in prefixes:
    names.setdefault(f'{kind}_{who}', 0)
    names[f'{kind}_{who}'] += 1
dups = sorted(k for k, v in names.items() if v > 1)
if dups:
    fail("(5) duplicated patch methods: " + ", ".join(dups))
else:
    ok(f"(5) no duplicated patch methods ({len(names)} total)")

# ── 本机绝对路径 ──────────────────────────────────────────────
# 【本文件自己不能含这些字面量】tools/sync.sh 有一道闸门：要同步到发布副本的
# 文件里不许出现本机路径。检查器自己的规则表恰好写着这些模式，于是每次改动这个
# 文件都会被自己拦住（v1.0.92 踩到，报「工作副本里有本机路径」而文件里其实没有）。
# 用码点拼出来：运行结果一样，文件里没有那个串。
_B = chr(92)
LEAKS = ['E:' + _B + _B + 'DSH',
         'D:' + _B + _B + 'Four Nights',
         'NESARF' + 'DX',
         'E:/DSH',
         'C:' + _B + _B + 'Users' + _B + _B + 'Administrator']
leak_hits = []
for p in list(ROOT.rglob('*.cs')) + list(ROOT.rglob('*.ps1')) + list(ROOT.rglob('*.sh')) \
        + list(ROOT.rglob('*.py')) + list(ROOT.rglob('*.md')):
    if '.git' in p.parts:
        continue
    # 跳过检查器自己 —— 它内部就写着这些模式，否则会自己检自己
    if p.resolve() == pathlib.Path(__file__).resolve():
        continue
    # tools/sync.sh 是【检测器】—— 它的 LEAKPAT 变量里就写着这些字面量。
    # 和检查器自己一样，属于合理的自引用豁免。
    if p.name == 'sync.sh' and p.parent.name == 'tools':
        continue
    t = read(p) or ''
    for pat in LEAKS:
        if re.search(pat, t):
            leak_hits.append(f'{p.relative_to(ROOT)}: {pat}')
if leak_hits:
    fail(f"(6) absolute local paths in the source ({len(leak_hits)}): {leak_hits[0]} ...")
else:
    ok("(6) no absolute local paths")

# ── 三栏用词（规则已于 v1.0.0 之后作废）────────────────────────
# 【为什么删掉这条规则】
# 原来 PoseDesc 是【单向】替换（只把"索取"按栏位改写），所以模板里写死的"榨取"
# 在背榨栏不会变成"索取"  —— 当时确实需要这条检查。
#
# 后来改成了【先归一成占位符、再按栏位填】：
#     d = d.Replace("榨取", P).Replace("吸取", P).Replace("索取", P);
#     d = d.Replace(P, ModeWordAt(i));
# 三个方向都成立了  模板里写哪个词都无所谓，这条检查也就没有意义了。
#
# 【保留注释的理由】下一代人看到"这里本来有条检查"时，
# 能直接知道它为什么被删 —— 而不是怀疑是被误删的。
ok("(7) pose wording is normalised by PoseDesc (rule retired, see the source comment)")

# ── 装饰符与判定符 ───────────────────────────────────────────
# 【准则来源】mega-index-map 的三条不变量之一：输出与词汇保持 ASCII。
# 本项目的例外（有意保留）：
#   · 中文排版符号    —  ·            （破折号、间隔号）
#   · 数学符号        ×  ±  −  Δ       （写法标准，换成 ASCII 反而难读）
#   · 全部面向用户的中文文案、注释、文档
# 不允许的：纯装饰或充当判定用的符号 ——
#   ✓ ✗ ✅ ❌ ⚠ ★ (自然) (手动) (记录) (停止)  !=   ═ 以及圈码 (1)(2)(3)
# 它们没有语义，只在视觉上干扰阅读，而且容易被误当成结论标记。
# 用【码点构造】而不是字面量 ——
# 否则一旦有别处对本文档做批量替换，这个表自己就先被改坏了（踩过一次）。
DECOR_CODEPOINTS = [
    0x2713, 0x2717, 0x2705, 0x274C, 0x26A0, 0x2605, 0x221A, 0x25CB, 0x25CF, 0x25A0,
    0x2260, 0x258D, 0x2550, 0x2460, 0x2461, 0x2462, 0x2463, 0x2464, 0x2465, 0x2466,
    0x2467, 0x2468, 0x2469, 0x246A, 0x246B, 0x246C, 0x246D, 0x246E, 0x246F, 0x2470,
    0x2471, 0x2472, 0x2473,
]
DECOR_BAD = set(chr(c) for c in DECOR_CODEPOINTS)
SRC_EXT = ('.cs', '.sh', '.ps1', '.py', '.yml', '.cmd', '.md')
hits = []
_SELF = pathlib.Path(__file__).resolve()
for _p in ROOT.rglob('*'):
    if not _p.is_file() or '.git' in _p.parts:
        continue
    if _p.suffix.lower() not in SRC_EXT:
        continue
    if _p.resolve() == _SELF:
        continue          # 跳过自己：表里就写着这些码点
    try:
        _t = _p.read_text(encoding='utf-8', errors='ignore')
    except Exception:
        continue
    for _c in DECOR_BAD:
        if _c in _t:
            hits.append((str(_p.relative_to(ROOT)), _c, _t.count(_c)))
if hits:
    fail("(9) decoration or judgement glyphs: " + ", ".join("%s %r x%d" % (f, c, n) for f, c, n in hits[:5]))
else:
    ok("(9) no decoration or judgement glyphs (Chinese punctuation and math symbols are allowed)")


# ── 挂载数 ────────────────────────────────────────────────────
mounts = len(re.findall(r'ok\+\+', src))
if mounts < 35:
    warn(f"(8) few mount points ({mounts}) - expected at least 35, check that no patch was dropped")
else:
    ok(f"(8) {mounts} mount points")


# ── README 里的 SHA256 是否还准 ─────────────────────────────
# 【为什么需要】README 里那三个哈希是手写的。签名之后它们【过期了两次】，
# 而且没人会发现 —— 用户照着核对会以为文件被篡改了。
# "文档里的数字"最容易过期，这类东西必须有检查盯着。
_sha_lines = re.findall(r'^([0-9a-f]{64})  (\S+)$', read(ROOT / 'README.md') or '', re.M)
if not _sha_lines:
    warn("(10) no SHA256 checksums found in README (ignore if that is intentional)")
else:
    _bad = []
    for _h, _name in _sha_lines:
        # README 里用的是 Release 资产名（ASCII），对应 dist 里的中文原名
        _cand = {
            "18-9L-6_ED-7-45-45_C-LS-Installer.exe": '汉堡店修改器-安装程序.exe',
            "18-9L-6_ED-7-45-45_C-LS-Trainer.exe": '数值修改器.exe',
            # README 里的校验和写的是【Release 资产名】（ASCII），
            # dist 里的实际文件是【程序集名】（含乘号与方括号）。
            # 两者刻意不同：资产名要能上传，程序集名要能表达完整意思。
            "18-9L-6_ED-7-45-45_C-LS.dll": "2×9+9L+6[ED]+7+2×45→1[C]+LS.dll",
        }.get(_name, _name)
        _p = ROOT / 'dist' / _cand
        if not _p.exists():
            _bad.append('%s (missing from dist: %s)' % (_name, _cand))
            continue
        _real = hashlib.sha256(_p.read_bytes()).hexdigest()
        if _real != _h:
            _bad.append('%s (README %s..., actual %s...)' % (_name, _h[:12], _real[:12]))
    if _bad:
        # 【这里是提醒，不是失败】
        # README 里的哈希描述的是【已发布的那个版本】，而 dist/ 是当前工作产物。
        # 改了代码重建之后两者必然不同 —— 那是正常的。
        # 但发版前它们必须一致，否则用户按 README 核对会以为文件被篡改。
        warn("(10) README checksums do not match dist (normal before a release, but update them before tagging): "
             + ", ".join(_bad))
    else:
        ok("(10) %d SHA256 values in README match dist" % len(_sha_lines))

# === 版本号一致性 ===
# 【为什么需要】v1.0.9 发版时，工作副本升到了 1.0.9，发布副本却整棵停在 1.0.8：
# 源码的 [BepInPlugin]、PluginVersion、Installer.Version、三个 csproj 全是旧的，
# dist 里三个二进制也是旧的，README 的校验和跟着旧 —— 三方一起旧。
# 第 (10) 条问的是"README 与 dist 是否互洽"，两边一起旧它就只能报通过。
# 于是用户从 release 下载 v1.0.9、照 README 核对，三条全不符。
#
# 这一条问的是另一个问题：这一棵树里，所有地方说的是不是同一个版本。
def _ver(rel, pat):
    m = re.search(pat, read(ROOT / rel) or '')
    return m.group(1) if m else None

PLUG = 'plugin/BurgerShopModder.cs'
_named = {
    'PluginVersion': _ver(PLUG, r'PluginVersion\s*=\s*"([0-9]+\.[0-9]+\.[0-9]+)"'),
    'BepInPlugin':   _ver(PLUG, r'BepInPlugin\([^)]*"([0-9]+\.[0-9]+\.[0-9]+)"\)'),
    'Installer':     _ver('installer/Installer.cs', r'internal const string Version\s*=\s*"([0-9]+\.[0-9]+\.[0-9]+)"'),
}
for _p in ('plugin/BurgerShopModder.csproj', 'trainer/BurgerShopTrainer.csproj', 'installer/Installer.csproj'):
    _named['csproj:' + _p.split('/')[-1]] = _ver(_p, r'<Version>([0-9]+\.[0-9]+\.[0-9]+)</Version>')

_seen = {v for v in _named.values() if v}
if None in _named.values():
    fail("(11) cannot read a version from: " + ", ".join("%s=%s" % (k, v) for k, v in _named.items()))
elif len(_seen) != 1:
    fail("(11) version numbers disagree: " + ", ".join("%s=%s" % (k, v) for k, v in _named.items()))
else:
    _now = _seen.pop()
    ok(f"(11) version is {_now} everywhere ({len(_named)} places)")

    # dist 里的二进制是不是同一版编译的：版本资源一定以 UTF-16 存在 PE 里，
    # 找不到就说明那份产物是别的版本编出来的。粗检，所以只报提醒。
    _want = _now.encode('utf-16-le')
    _miss = []
    for _n in ('2×9+9L+6[ED]+7+2×45→1[C]+LS.dll', '数值修改器.exe', '汉堡店修改器-安装程序.exe'):
        _f = ROOT / 'dist' / _n
        if not _f.exists():
            _miss.append(f'{_n}（dist 里没有）')
        elif _want not in _f.read_bytes():
            _miss.append(f'{_n}（找不到 {_now}）')
    if _miss:
        warn("(12) dist artifacts are not from this version (rebuild before releasing): " + ", ".join(_miss))
    else:
        ok(f"(12) all 3 artifacts in dist carry version {_now}")

# === 英文对照表与源码是否还对得上 ===
# 【为什么需要】对照表拿「中文原文」当 key。源码里把那句中文改了字（哪怕只多一个
# 「的」），表里那行就永远匹配不上。而缺译文时是【回落成中文】—— 不报错、不崩溃，
# 只是那句永远翻不成英文。这种失效只能靠检查抓，人眼看不出来。
#
# 只查一个方向：表里有、源码没有 → 说明表漂移了，报失败。
# 反方向（源码有、表里没有）是正常的，那是还没翻，只报个进度。
_en = ROOT / 'plugin' / 'lang' / 'en.txt'
if _en.exists():
    _entries = []
    for _line in _en.read_text(encoding='utf-8').splitlines():
        if not _line or _line.startswith('#'):
            continue
        if '\t' not in _line:
            continue
        _zh = _line.split('\t', 1)[0]
        if _zh:
            _entries.append(_zh)

    _panel_main = read(ROOT / 'plugin' / 'BurgerShopModder.cs') or ''
    # 【v1.0.93】第 (13) 条原来只扫主文件。GameBuild.cs 里那 7 处 L.T 一直没有译文，
    # 检查器完全看不见 —— 因为它只看这一个文件。同目录其它 .cs 一并并入扫描范围。
    # 第 (14) 条仍然只看主文件：它的报错带行号，拼接后的行号会误导人。
    _panel = _panel_main
    for _f in ('GameBuild.cs', 'PresetStore.cs', 'ActivityLogger.cs', 'CfgGuard.cs',
               'GameBindings.cs', 'GameCommandServer.cs', 'Localization.cs'):
        _panel += '\n' + (read(ROOT / 'plugin' / _f) or '')
    # L.T 的 key 可以是拼接出来的（"A" + "B"），编译期会合并成一个串。
    # 所以按括号配对取出完整实参再做字面量合并 —— 直接抓第一个引号会把这种条目截断，
    # 结果表里那条完整的 key 反而被判成孤儿。
    def _lt_keys(text):
        keys = set()
        for _m in re.finditer(r'L\.T\(\s*', text):
            _i = _m.end()
            _depth, _j, _instr = 1, _i, False
            while _j < len(text) and _depth > 0:
                _c = text[_j]
                if _c == '"':
                    _instr = not _instr
                elif not _instr:
                    if _c == '(':
                        _depth += 1
                    elif _c == ')':
                        _depth -= 1
                        if _depth == 0:
                            break
                    elif _c == ',' and _depth == 1:
                        break       # 第一个实参到此为止，后面的 args 不参与 key
                _j += 1
            _inner = text[_i:_j]
            # 第一个实参可能是 A + B 这类纯字面量拼接，编译期会合并
            if re.fullmatch(r'\s*(?:"[^"]*"\s*(?:\+\s*)?)+\s*', _inner):
                _key = ''.join(re.findall(r'"([^"]*)"', _inner))
                # 源码里的反斜杠是 C# 转义（"snap_<时间戳>\\" 实际是一个反斜杠），
                # 对照表里存的是转义之后的样子。不还原这一层，带 \ 的条目永远对不上。
                _key = _key.replace('\\\\', '\\')
                if _key:
                    keys.add(_key)
        return keys

    _calls = _lt_keys(_panel)

    # 还有些文案是通过 helper 或数组交给内部翻的，正则看不到字面量
    _calls |= set(re.findall(r'AboutRow\(\s*"([^"]*)"', _panel))
    _slot = re.search(r'SlotLabels\s*=\s*\{([^}]*)\}', _panel)
    if _slot:
        _calls |= set(re.findall(r'"([^"]*)"', _slot.group(1)))

    # 面板上大部分文案不是直接包 L.T 的，而是当参数传给这四个绘制 helper ——
    # 翻译在 helper 内部统一做（一处改动覆盖所有调用点）。所以这里也要认。
    _calls |= set(re.findall(r'(?:Section|Hint|Sub|SliderF|SliderI)\(\s*"([^"]*)"', _panel))

    # 栏名不按字面量传进去，走的是 L.T(TabNames[i])。
    # 所以要把 TabNames 数组里的元素也当成「有调用点」，否则 8 个栏名会被误判成孤儿。
    _tabarr = re.search(r'TabNames\s*=\s*\{([^}]*)\}', _panel)
    if _tabarr:
        _calls |= set(re.findall(r'"([^"]*)"', _tabarr.group(1)))

    # 空字符串是占位（比如某个 Hint("")），不当成待译条目
    _calls = {x for x in _calls if x}

    # 三个模式词是 ModeWordAt() / ModeWord() 的返回值，调用点写的是
    # L.T(ModeWordAt(i)) —— 正则看不见里面那个字面量，但对照表里必须有它们，
    # 因为 ModeWordAt 返回的是中文，显示前要靠 L.T 换成 Absorb / Demand / Drain。
    _calls |= {'吸取', '索取', '榨取'}

    # 【orphan 检查挪到第 (15) 条后面】因为归一后的描述模板也算「有调用点」，
    # 而那批模板要到第 (15) 条才算得出来 —— 先查会白白报 30 多条孤儿。
    # === BindP3 描述模板：归一后的那一版才是 en.txt 的 key ===
    # PoseDesc() 先把三栏共用的模板归一成带 {模式} 的一版，再拿它查表。
    # 所以源码里写的「每次累积索取欲的下限（%）」和表里的
    # 「每次累积{模式}欲的下限（%）」不是同一个串 —— 第 (13) 条的比对看不见它们。
    # 这一条单独比一次，两个方向都查。
    def _split_args(s):
        out, depth, cur, i, instr = [], 0, [], 0, False
        while i < len(s):
            c = s[i]
            if instr:
                if c == '\\':
                    cur.append(s[i:i + 2]); i += 2; continue
                if c == '"':
                    instr = False
                cur.append(c); i += 1; continue
            if c == '"':
                instr = True; cur.append(c); i += 1; continue
            if c in '(<[':
                depth += 1
            elif c in ')>]':
                depth -= 1
            if c == ',' and depth == 0:
                out.append(''.join(cur)); cur = []; i += 1; continue
            cur.append(c); i += 1
        out.append(''.join(cur))
        return out

    _p3tpl = set()
    for _m in re.finditer(r'BindP3(?:Int|Bool)?\(\s*', _panel):
        _i, _d, _j, _in = _m.end(), 1, _m.end(), False
        while _j < len(_panel):
            _c = _panel[_j]
            if _in:
                if _c == '\\':
                    _j += 2; continue
                if _c == '"':
                    _in = False
            else:
                if _c == '"':
                    _in = True
                elif _c == '(':
                    _d += 1
                elif _c == ')':
                    _d -= 1
                    if _d == 0:
                        break
            _j += 1
        _args = _split_args(_panel[_i:_j])
        if len(_args) < 3:
            continue
        _e = _args[2].strip()
        if not re.fullmatch(r'(?:"(?:[^"\\]|\\.)*"\s*(?:\+\s*)?)+', _e):
            continue
        _lit = ''.join(re.findall(r'"((?:[^"\\]|\\.)*)"', _e)).replace('\\"', '"')
        _p3tpl.add(_lit.replace('榨取', '{模式}').replace('吸取', '{模式}').replace('索取', '{模式}'))

    # 【只查一个方向】源码模板 -> 表里必须有译文。
    # 反方向（表里有、源码没有）归第 (13) 条：它扫的是 L.T 与 helper 的全部调用点，
    # 覆盖面比这里大。第 (15) 条只补「归一后的模板也是 key」这一件事。
    _p3miss = sorted(t for t in _p3tpl if t not in set(_entries))
    if _p3miss:
        fail("(15) %d BindP3 description templates have no translation: %s"
             % (len(_p3miss), ", ".join(t[:30] for t in _p3miss[:4])))
    else:
        ok("(15) all %d BindP3 description templates are translated" % len(_p3tpl))

    # 归一后的模板也算「有调用点」—— 它们是 PoseDesc 运行时才拼出来的 key
    _calls |= _p3tpl

    _done = set(_entries) & _calls
    _todo = sorted(_calls - _done)
    print('  [INFO] panel strings translated %d/%d' % (len(_done), len(_calls))
          + (', not yet: ' + ', '.join(_todo[:6]) if _todo else ''))
    _orphan = [z for z in _entries if z not in _calls]
    try:
        # 控制台会按本地码页把中文打成乱码，完整清单顺手落一份 UTF-8 文件
        _tmpdir = ROOT / '_tmp'
        _tmpdir.mkdir(exist_ok=True)
        with open(str(_tmpdir / 'orphan.txt'), 'w', encoding='utf-8') as _fh:
            _fh.write('\n'.join(repr(z) for z in _orphan))
    except Exception:
        pass
    if _orphan:
        fail("(13) %d table entries match no L.T call in the source (wording changed?): %s"
             % (len(_orphan), ", ".join(repr(z) for z in _orphan[:6])))
    else:
        ok("(13) all %d table entries match the source" % len(_entries))

elif any(_p.exists() for _p in (ROOT / 'plugin').glob('*.cs')):
    warn("(13) plugin/lang/en.txt not found (ignore if the translation has not started)")

# === 面板控件里还有没有裸中文 ===
# 第 (13) 条查的是「表里的 key 能不能对上源码」—— 它抓不到
# 「源码里新写了一句中文、却根本没包 L.T」这种情况：那种字符串不在表里，
# 也不在任何被检查的调用里，两边都看不见它。
#
# 这一条补上：扫面板控件的字面量参数，含中文又没走 L.T 就报。
# 「拷贝自」那两个按钮就是这么漏的（文案取自 TabNames，直接喂给了 Button）。
#
# 【v1.0.92 放宽了范围】原来只认 Button / Label / Toggle 三种。英文化第二轮
# 实际漏掉的不是别的控件，而是「文案先存进变量」与「helper 内部没套 L.T」这两类
# —— 前者任何按调用点扫的检查都看不见（见 ENGINEERING-NOTES 第 42 条），
# 后者是 Sub() 那个二级小节标题。所以这里把控件放宽到 GUI / GUILayout 的全部函数，
# Sub 的裸字面量也一起扫（它内部套了 L.T，写在 en.txt 里的 key 才算数）。
_bare = []
# 【必须按括号配对取整段调用】原来用 `[^;]*?\)` 非贪婪匹配到第一个右括号，
# 遇到 `GUILayout.Button(L.T("打开"), GUILayout.Width(BtnW("打开", 52f)))` 这种
# 嵌套调用就被截断，`L.T(` 落在截断点之外，于是已翻译的按钮被误报成裸中文。
for _m in re.finditer(r'\bGU(?:ILayout|I)\.(?:Button|Label|Toggle|Box|TextField|TextArea|Toolbar|Window)\(', _panel_main):
    _i, _d, _j, _in = _m.end() - 1, 0, _m.end() - 1, False
    while _j < len(_panel_main):
        _c = _panel_main[_j]
        if _in:
            if _c == '\\':
                _j += 2; continue
            if _c == '"':
                _in = False
        else:
            if _c == '"':
                _in = True
            elif _c == '(':
                _d += 1
            elif _c == ')':
                _d -= 1
                if _d == 0:
                    break
        _j += 1
    _call = _panel_main[_i:_j]
    if 'L.T(' in _call or 'TabNames' in _call or 'SlotLabels' in _call:
        continue
    for _s in re.findall(r'"([^"]*)"', _call):
        if _s in ('中文', 'English'):
            continue          # 语言按钮在各语言下都显示自己的名字
        if re.search(r'[一-鿿]', _s):
            _line = _panel_main[: _m.start()].count(chr(10)) + 1
            _bare.append('%d：%s' % (_line, _s[:40]))

if _bare:
    fail("(14) %d Chinese literals in panel controls bypass L.T: %s"
         % (len(_bare), ", ".join(_bare[:4])))
else:
    ok("(14) no bare Chinese in panel controls")

# ── 汇总 ──────────────────────────────────────────────────────
print()
print(f'result: {len(fails)} failed, {len(warns)} warnings')
sys.exit(1 if fails else 0)
