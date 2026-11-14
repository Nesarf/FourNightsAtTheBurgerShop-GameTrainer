# -*- coding: utf-8 -*-
"""
Static consistency check — needs neither the game nor a compile, so it runs on CI as is.

[Why it exists]
Every config entry in this project has to appear in several places at once:

    BindP3 declaration -> panel slider -> set command -> description template -> manual

**Missing any one of them is a silent failure** — no error, the feature just does not work.
This file turns that consistency into rules a machine can check.

[Rules]
  (1) every BindP3 param needs a panel control
  (2) every BindP3 param needs a set command branch
  (3) every param must appear in the manual (compared with the _Fella/_Osiri/_Sit suffix stripped)
  (4) the same target method must not be patched twice (Harmony raises AmbiguousMatch)
  (5) the plugin source must contain no absolute local paths
  (6) pose wording that should not be hard-coded ("索取" must not appear in a shared template)
  (7) the mount count must not drop below the expected figure (guards against "half the patches hooked")
  (16) every panel button must carry an explicit width (no full-row stretching)

Usage:
    python tools/static-check.py [repo root]
Exit code: 0 = all passed; 1 = something failed
"""
import re
import hashlib
import sys
import pathlib

# [Breaks on CI] The GitHub runner's Python defaults to cp1252, and printing Chinese
# raises UnicodeEncodeError outright. Standard output must be set to UTF-8 explicitly.
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

# Comments mention `GUILayout.Button(text)` and Chinese copy while explaining the rules, so the
# panel is also kept as a copy with comments blanked out. Newlines are preserved, so any line
# number reported from it stays correct.
def _strip_comments(text):
    text = re.sub(r'/\*.*?\*/', lambda m: re.sub(r'[^\n]', ' ', m.group(0)), text, flags=re.S)
    return re.sub(r'//[^\n]*', lambda m: re.sub(r'[^\n]', ' ', m.group(0)), text)


_panel_nc = _strip_comments(read(ROOT / 'plugin' / 'BurgerShopModder.cs') or '')


src = read(PLUGIN)
if src is None:
    print(f'cannot find {PLUGIN}')
    sys.exit(2)
cmd = read(CMD) or ''
doc = read(DOC) or read(HINT_DOC) or ''

print('== static consistency check ==\n')

# ── collect the BindP3 params ────────────────────────────────
# BindP3("Name", ...) / BindP3Int / BindP3Bool
p3 = set(re.findall(r'BindP3(?:Int|Bool)?\(\s*"([A-Za-z_][A-Za-z0-9_]*)"', src))
print(f'(1) BindP3 params: {len(p3)}')

# panel control: SetP3(Name3, ...)
panel = set(re.findall(r'SetP3\(\s*([A-Za-z_][A-Za-z0-9_]*?)(?:3)\s*,', src))
# set command branch: case "Name": ... SetP3(Name3
setc = set(re.findall(r'case\s+"([A-Za-z_][A-Za-z0-9_]*)"\s*:', cmd + src))

miss_panel = sorted(p for p in p3 if f'{p}3' not in src.replace('BindP3', '') and p not in panel)
# the accurate test: does the source contain SetP3(<Name>3 ...)
have_panel = set(re.findall(r'SetP3\(\s*([A-Za-z_][A-Za-z0-9_]*)3\s*,', src))
miss_panel = sorted(p for p in p3 if p not in have_panel)
miss_set = sorted(p for p in p3 if p not in setc)

if miss_panel:
    fail("(2) params with no panel control: " + ", ".join(miss_panel[:12]) + (" ... %d in total" % len(miss_panel) if len(miss_panel) > 12 else ""))
else:
    ok(f"(2) all {len(p3)} params have a panel control")

if miss_set:
    # the set branch may live outside GameCommandServer, so this is a warning only
    warn("(3) params with no set branch: " + ", ".join(miss_set[:12]) + (" ... %d in total" % len(miss_set) if len(miss_set) > 12 else ""))
else:
    ok(f"(3) all {len(p3)} params have a set branch")

# ── manual coverage ──────────────────────────────────────────
base = set(re.sub(r'_(Fella|Osiri|Sit)$', '', p) for p in p3)
miss_doc = sorted(b for b in base if b not in doc)
if miss_doc:
    warn("(4) not mentioned in the manual: " + ", ".join(miss_doc[:12]) + (" ... %d in total" % len(miss_doc) if len(miss_doc) > 12 else ""))
else:
    ok(f"(4) all {len(base)} logical params appear in the manual")

# ── duplicate Harmony patches ────────────────────────────────
# TryPatch(pc, "Method", ...) / GetMethod("Method", ...) followed by _harmony.Patch
patched = re.findall(r'GetMethod\(\s*"([A-Za-z_][A-Za-z0-9_]*)"', src)
dup = sorted({m for m in patched if patched.count(m) > 2})   # one method may be fetched more than once for different uses
# What actually matters is a duplicated PREFIX/POSTFIX NAME for the same method.
# Identifiers may contain Japanese (Postfix_Osiri吸精Damage); without allowing it they get
# truncated to the same name and produce a false positive.
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

# ── absolute local paths ─────────────────────────────────────
# [This file must not contain those literals itself] tools/sync.sh has a gate: a file about
# to be synced to the release copy must not contain a local path. The checker's own rule table
# happens to spell those patterns out, so every edit to this file tripped its own gate
# (hit at v1.0.92: "the working copy contains a local path" while the file contained none).
# Build them from code points: same behaviour, no such string in the file.
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
    # skip the checker itself — it spells these patterns out, otherwise it checks itself
    if p.resolve() == pathlib.Path(__file__).resolve():
        continue
    # tools/sync.sh is a DETECTOR — its LEAKPAT variable spells those literals out.
    # Same as the checker itself: a legitimate self-reference exemption.
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

# ── pose wording (rule retired after v1.0.0) ─────────────────
# [Why the rule was dropped]
# PoseDesc used to substitute in ONE direction (only "索取" was rewritten per tab), so a
# hard-coded "榨取" in a template did not become "索取" in the Rear tab — the check was
# genuinely needed then.
#
# It was later changed to "normalise into a placeholder first, then fill per tab":
#     d = d.Replace("榨取", P).Replace("吸取", P).Replace("索取", P);
#     d = d.Replace(P, ModeWordAt(i));
# All three directions hold now, whichever word a template uses, so the check lost its point.
#
# [Why the comment stays] so the next reader who finds "there used to be a check here"
# knows immediately why it went, instead of suspecting it was deleted by accident.
ok("(7) pose wording is normalised by PoseDesc (rule retired, see the source comment)")

# ── decoration and judgement glyphs ─────────────────────────
# [Where the rule comes from] one of mega-index-map's three invariants: keep output and
# vocabulary ASCII. Deliberate exceptions in this project:
#   - Chinese punctuation   —  ·            (em dash, middle dot)
#   - maths symbols         ×  ±  −  Δ      (standard notation; ASCII would read worse)
#   - all user-facing Chinese copy, comments and documentation
# Not allowed: symbols that are pure decoration or act as a verdict —
#   check marks, crosses, warning signs, stars, (natural)/(manual)/(record)/(stop) markers,
#   "!=", box-drawing rules, and circled numbers (1)(2)(3).
# They carry no meaning, distract the eye, and get mistaken for a conclusion marker.
# Built from CODE POINTS rather than literals — otherwise a bulk replace elsewhere
# corrupts this table itself first (that happened once).
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
        continue          # skip itself: the table spells those code points out
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


# ── mount count ─────────────────────────────────────────────
mounts = len(re.findall(r'ok\+\+', src))
if mounts < 35:
    warn(f"(8) few mount points ({mounts}) - expected at least 35, check that no patch was dropped")
else:
    ok(f"(8) {mounts} mount points")


# ── are the SHA256 values in README still right ─────────────
# [Why] those three hashes in the README are typed by hand. After signing they went STALE
# twice and nobody would have noticed — a user verifying would conclude the file had been
# tampered with. "Numbers in documentation" are the first thing to rot, so something has to
# watch them.
_sha_lines = re.findall(r'^([0-9a-f]{64})  (\S+)$', read(ROOT / 'README.md') or '', re.M)
if not _sha_lines:
    warn("(10) no SHA256 checksums found in README (ignore if that is intentional)")
else:
    _bad = []
    for _h, _name in _sha_lines:
        # the README uses the release asset name (ASCII); map it to the original name in dist
        _cand = {
            "18-9L-6_ED-7-45-45_C-LS-Installer.exe": '汉堡店修改器-安装程序.exe',
            "18-9L-6_ED-7-45-45_C-LS-Trainer.exe": '数值修改器.exe',
            # the README checksum names the RELEASE ASSET (ASCII); the actual file in dist
            # carries the ASSEMBLY NAME (with the multiplication signs and brackets).
            # They differ on purpose: the asset name has to be uploadable, the assembly name
            # has to say what it means.
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
        # [a warning, not a failure]
        # The README hashes describe the RELEASED version, while dist/ holds the current
        # build output. After a rebuild they necessarily differ — that is normal.
        # Before a release they must match, or a user verifying against the README will think
        # the file was tampered with.
        warn("(10) README checksums do not match dist (normal before a release, but update them before tagging): "
             + ", ".join(_bad))
    else:
        ok("(10) %d SHA256 values in README match dist" % len(_sha_lines))

# === version consistency ===
# [Why] when v1.0.9 shipped, the working copy was bumped to 1.0.9 while the release copy sat
# at 1.0.8 throughout: [BepInPlugin], PluginVersion, Installer.Version and the three csproj
# files were all old, the three binaries in dist were old, the README checksums were old —
# all three sides stale together.
# Rule (10) asks "do README and dist agree", and both being stale made it report a pass.
# So a user downloading v1.0.9 and verifying against the README saw all three mismatch.
#
# This rule asks a different question: within this tree, does everything state the same version.
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

    # were the binaries in dist built from this version: the version resource is always stored
    # in a PE as UTF-16, so not finding it means that artefact came from another version.
    # A coarse check, hence a warning.
    _want = _now.encode('utf-16-le')
    _miss = []
    for _n in ('2×9+9L+6[ED]+7+2×45→1[C]+LS.dll', '数值修改器.exe', '汉堡店修改器-安装程序.exe'):
        _f = ROOT / 'dist' / _n
        if not _f.exists():
            _miss.append(f'{_n} (missing from dist)')
        elif _want not in _f.read_bytes():
            _miss.append(f'{_n} (no {_now} in it)')
    if _miss:
        warn("(12) dist artifacts are not from this version (rebuild before releasing): " + ", ".join(_miss))
    else:
        ok(f"(12) all 3 artifacts in dist carry version {_now}")

# === does the English table still match the source ===
# [Why] the table uses the CHINESE ORIGINAL as its key. Change one character of that sentence
# in the source (adding a single particle is enough) and the row can never match again. A
# missing translation FALLS BACK TO CHINESE — no error, no crash, that one line simply never
# becomes English. Only a check catches this; the eye cannot.
#
# One direction only: the table has it but the source does not -> the table drifted, fail.
# The other direction (source has it, table does not) is normal — not translated yet — and is
# reported as progress only.
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
    # [v1.0.93] rule (13) used to scan the main file only. The seven L.T calls in
    # GameBuild.cs never had translations and the checker could not see them at all, because it
    # looked at that one file. The other .cs files in the directory are scanned too now.
    # Rule (14) still reads the main file only: its messages carry line numbers, and numbers
    # from a concatenated text would mislead.
    _panel = _panel_main
    for _f in ('GameBuild.cs', 'PresetStore.cs', 'ActivityLogger.cs', 'CfgGuard.cs',
               'GameBindings.cs', 'GameCommandServer.cs', 'Localization.cs'):
        _panel += '\n' + (read(ROOT / 'plugin' / _f) or '')
    # An L.T key may be concatenated ("A" + "B"); the compiler merges it into one string.
    # So take the whole argument by bracket matching and merge the literals — grabbing only the
    # first quoted string truncates such an entry, and the complete key in the table then gets
    # reported as an orphan.
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
                        break       # end of the first argument; the args after it are not part of the key
                _j += 1
            _inner = text[_i:_j]
            # the first argument may be a pure literal concatenation of A + B; the compiler merges it
            if re.fullmatch(r'\s*(?:"[^"]*"\s*(?:\+\s*)?)+\s*', _inner):
                _key = ''.join(re.findall(r'"([^"]*)"', _inner))
                # a backslash in the source is a C# escape ("snap_<时间戳>\\" is one backslash),
                # and the table stores the unescaped form. Without undoing that layer, entries
                # containing a backslash can never match.
                _key = _key.replace('\\\\', '\\')
                if _key:
                    keys.add(_key)
        return keys

    _calls = _lt_keys(_panel)

    # some copy is handed to a helper or an array and translated inside; the regex cannot see it
    _calls |= set(re.findall(r'AboutRow\(\s*"([^"]*)"', _panel))
    _slot = re.search(r'SlotLabels\s*=\s*\{([^}]*)\}', _panel)
    if _slot:
        _calls |= set(re.findall(r'"([^"]*)"', _slot.group(1)))

    # most panel copy is not wrapped in L.T directly; it is passed as an argument to these
    # drawing helpers, which translate inside (one change covers every call site).
    _calls |= set(re.findall(r'(?:Section|Hint|Sub|SliderF|SliderI|ProbeWhy)\(\s*"([^"]*)"', _panel))

    # tab names are not passed as literals; they go through L.T(TabNames[i]).
    # So the elements of the TabNames array count as call sites, or all eight names are
    # reported as orphans.
    _tabarr = re.search(r'TabNames\s*=\s*\{([^}]*)\}', _panel)
    if _tabarr:
        _calls |= set(re.findall(r'"([^"]*)"', _tabarr.group(1)))

    # an empty string is a placeholder (some Hint("")), not an entry to translate
    _calls = {x for x in _calls if x}

    # The three mode words are the return values of ModeWordAt() / ModeWord(); the call site
    # reads L.T(ModeWordAt(i)). The regex cannot see the literal inside, but the table must
    # hold them: ModeWordAt returns Chinese, and L.T turns it into Absorb / Demand / Drain
    # before it is displayed.
    _calls |= {'吸取', '索取', '榨取'}

    # [the orphan check moved after rule (15)] because the normalised description templates also
    # count as call sites, and those templates are only computed at rule (15) — checking earlier
    # reports 30-plus orphans for nothing.
    # === BindP3 description templates: the normalised version is the en.txt key ===
    # PoseDesc() first normalises the template shared by the three tabs into one with {模式},
    # then looks that up. So the source's "每次累积索取欲的下限（%）" and the table's
    # "每次累积{模式}欲的下限（%）" are not the same string — rule (13) cannot see them.
    # This rule compares them separately, in both directions.
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

    # [one direction only] source template -> the table must hold a translation.
    # The other direction (in the table, not in the source) belongs to rule (13), which scans
    # every L.T and helper call site and covers more ground. Rule (15) only adds "the
    # normalised template is a key too".
    _p3miss = sorted(t for t in _p3tpl if t not in set(_entries))
    if _p3miss:
        fail("(15) %d BindP3 description templates have no translation: %s"
             % (len(_p3miss), ", ".join(t[:30] for t in _p3miss[:4])))
    else:
        ok("(15) all %d BindP3 description templates are translated" % len(_p3tpl))

    # the normalised templates count as call sites too — PoseDesc builds those keys at runtime
    _calls |= _p3tpl

    _done = set(_entries) & _calls
    _todo = sorted(_calls - _done)
    print('  [INFO] panel strings translated %d/%d' % (len(_done), len(_calls))
          + (', not yet: ' + ', '.join(_todo[:6]) if _todo else ''))
    _orphan = [z for z in _entries if z not in _calls]
    try:
        # the console mangles Chinese through the local code page, so drop the full list into a UTF-8 file
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

# === any bare Chinese left in panel controls ===
# Rule (13) asks "do the table's keys match the source" — it cannot catch
# "a new Chinese sentence was written in the source and never wrapped in L.T": that string is
# not in the table and not in any checked call site, so neither side sees it.
#
# This rule covers it: scan the literal arguments of panel controls, report any Chinese that
# did not go through L.T. That is how the two "copied from" buttons were missed (their copy
# came from TabNames and was fed straight to Button).
#
# [v1.0.92 widened the scope] it used to know Button / Label / Toggle only. What round two of
# the translation actually missed was not some other control but "copy stored in a variable
# first" and "a helper that does not wrap L.T internally" — the first is invisible to any
# check that scans call sites (see ENGINEERING-NOTES #42), the second was the Sub() section
# heading. So the control list is now every GUI / GUILayout function, and bare literals inside
# Sub are scanned too (it wraps L.T internally, so only keys written into en.txt count).
_bare = []
# [take the whole call by bracket matching] it used to match non-greedily with `[^;]*?\)` up to
# the first closing bracket, so a nested call like
# `GUILayout.Button(L.T("打开"), GUILayout.Width(BtnW("打开", 52f)))` was cut short, the `L.T(`
# fell outside the captured span, and an already translated button was reported as bare Chinese.
for _m in re.finditer(r'\bGU(?:ILayout|I)\.(?:Button|Label|Toggle|Box|TextField|TextArea|Toolbar|Window)\(', _panel_main):
    _i, _d, _j, _in = _m.end() - 1, 0, _m.end() - 1, False
    while _j < len(_panel_nc):
        _c = _panel_nc[_j]
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
    _call = _panel_nc[_i:_j]
    if 'L.T(' in _call or 'TabNames' in _call or 'SlotLabels' in _call:
        continue
    for _s in re.findall(r'"([^"]*)"', _call):
        if _s in ('中文', 'English'):
            continue          # the language buttons show their own name in every language
        if re.search(r'[一-鿿]', _s):
            _line = _panel_nc[: _m.start()].count(chr(10)) + 1
            _bare.append('%d：%s' % (_line, _s[:40]))

if _bare:
    fail("(14) %d Chinese literals in panel controls bypass L.T: %s"
         % (len(_bare), ", ".join(_bare[:4])))
else:
    ok("(14) no bare Chinese in panel controls")

# === any panel button without an explicit width ===
# [Why] `GUILayout.Button(text)` without a width expands to fill the row, so inside a 540 px
# panel even a two-character button took the whole line (fixed in v1.0.97). Reading the code
# does not show it — it looks exactly like a normal button — so a check has to watch it.
# `Btn(text)` and `BtnW(text, min)` are the two helpers that produce a width.
_btn_bare = []
for _m in re.finditer(r'\bGUILayout\.Button\(', _panel_nc):
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
    if 'GUILayout.Width(' in _call or 'Btn(' in _call or 'BtnText(' in _call:
        continue
    _line = _panel_main[: _m.start()].count(chr(10)) + 1
    _btn_bare.append('%d: %s' % (_line, ' '.join(_call.split())[:60]))

if _btn_bare:
    fail("(16) %d panel buttons have no explicit width (they stretch across the whole row): %s"
         % (len(_btn_bare), ", ".join(_btn_bare[:4])))
else:
    ok("(16) every panel button carries an explicit width")

# ── summary ─────────────────────────────────────────────────
print()
print(f'result: {len(fails)} failed, {len(warns)} warnings')
sys.exit(1 if fails else 0)
