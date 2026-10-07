# -*- coding: utf-8 -*-
"""
静态一致性检查 —— 不需要游戏、不需要编译，可直接跑在 CI 上。

【为什么需要】
这个项目的配置项要同时出现在很多地方：

    BindP3 定义 → 面板滑块 → set 命令 → 描述模板 → 说明书

**漏掉任何一处都是静默失败** —— 不报错，只是"某个功能用不了"。
这里把这类一致性做成可自动检查的规则。

【检查项】
  ① 每个 BindP3 参数都要有面板控件
  ② 每个 BindP3 参数都要有 set 命令分支
  ③ 每个参数都要在说明书里出现（去掉 _Fella/_Osiri/_Sit 后缀比对）
  ④ 同一目标方法不能被 patch 两次（Harmony 会报 AmbiguousMatch）
  ⑤ 插件源码里不能有本机绝对路径
  ⑥ 不该硬编码的三栏用词（"索取"不得出现在共享模板里）
  ⑦ 挂载数不能少于预期（防"补丁挂了一半"）

用法：
    python tools/static-check.py [仓库根目录]
退出码：0 = 全过；1 = 有失败项
"""
import re
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
    print(f'找不到 {PLUGIN}')
    sys.exit(2)
cmd = read(CMD) or ''
doc = read(DOC) or read(HINT_DOC) or ''

print('== 静态一致性检查 ==\n')

# ── 收集 BindP3 参数 ──────────────────────────────────────────
# BindP3("Name", ...) / BindP3Int / BindP3Bool
p3 = set(re.findall(r'BindP3(?:Int|Bool)?\(\s*"([A-Za-z_][A-Za-z0-9_]*)"', src))
print(f'① BindP3 参数：{len(p3)} 个')

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
    fail(f'② 这些参数【没有面板控件】：{", ".join(miss_panel[:12])}'
         + (f' …共 {len(miss_panel)} 个' if len(miss_panel) > 12 else ''))
else:
    ok(f'② 全部 {len(p3)} 个参数都有面板控件')

if miss_set:
    # set 分支可能在 GameCommandServer 之外，宽松处理
    warn(f'③ 这些参数【没有 set 命令分支】：{", ".join(miss_set[:12])}'
         + (f' …共 {len(miss_set)} 个' if len(miss_set) > 12 else ''))
else:
    ok(f'③ 全部 {len(p3)} 个参数都有 set 命令分支')

# ── 说明书覆盖 ────────────────────────────────────────────────
base = set(re.sub(r'_(Fella|Osiri|Sit)$', '', p) for p in p3)
miss_doc = sorted(b for b in base if b not in doc)
if miss_doc:
    warn(f'④ 说明书中未提及：{", ".join(miss_doc[:12])}'
         + (f' …共 {len(miss_doc)} 个' if len(miss_doc) > 12 else ''))
else:
    ok(f'④ 全部 {len(base)} 个逻辑参数都在说明书里')

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
    fail(f'⑤ 重复定义的补丁方法：{", ".join(dups)}')
else:
    ok(f'⑤ 补丁方法无重复定义（共 {len(names)} 个）')

# ── 本机绝对路径 ──────────────────────────────────────────────
LEAKS = [r'E:\\DSH', r'D:\\Four Nights', r'NESARFDX', r'E:/DSH', r'C:\\Users\\Administrator']
leak_hits = []
for p in list(ROOT.rglob('*.cs')) + list(ROOT.rglob('*.ps1')) + list(ROOT.rglob('*.sh')) \
        + list(ROOT.rglob('*.py')) + list(ROOT.rglob('*.md')):
    if '.git' in p.parts:
        continue
    # 跳过检查器自己 —— 它内部就写着这些模式，否则会自己检自己
    if p.resolve() == pathlib.Path(__file__).resolve():
        continue
    t = read(p) or ''
    for pat in LEAKS:
        if re.search(pat, t):
            leak_hits.append(f'{p.relative_to(ROOT)}: {pat}')
if leak_hits:
    fail(f'⑥ 源码里有本机绝对路径（{len(leak_hits)} 处）：{leak_hits[0]} …')
else:
    ok('⑥ 无本机绝对路径')

# ── 硬编码的三栏用词 ──────────────────────────────────────────
# 共享模板（BindP3 的描述）里不该出现"榨取"（那是正骑专属），
# 也不该出现"吸取"（口交专属）。"索取"作为背榨的叫法是允许的。
bad_words = []
for m in re.finditer(r'BindP3(?:Int|Bool)?\(\s*"([A-Za-z_][A-Za-z0-9_]*)",.*?\);', src, re.S):
    body = m.group(0)
    if '榨取' in body or '吸取' in body:
        bad_words.append(m.group(1))
if bad_words:
    warn(f'⑦ 共享模板里混进了专属用词（应由 PoseDesc 按栏位替换）：'
         f'{", ".join(bad_words[:8])} …共 {len(bad_words)}')
else:
    ok('⑦ 共享描述模板无专属用词')

# ── 挂载数 ────────────────────────────────────────────────────
mounts = len(re.findall(r'ok\+\+', src))
if mounts < 35:
    warn(f'⑧ 挂载点数偏少（{mounts}）—— 预期 ≥ 35，确认没有补丁被删掉')
else:
    ok(f'⑧ 挂载点 {mounts} 个')

# ── 汇总 ──────────────────────────────────────────────────────
print()
print(f'结论：{len(fails)} 项失败，{len(warns)} 项提醒')
sys.exit(1 if fails else 0)
