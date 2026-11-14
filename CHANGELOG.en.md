> This changelog is an English translation of the Chinese original, [CHANGELOG.md](CHANGELOG.md); the Chinese version is authoritative.
> Where the two disagree, trust the Chinese text and fix this file instead.
> Everything below follows the original structure exactly: same versions, same order, same entries.

# Changelog

This project follows [Semantic Versioning](https://semver.org/).

The detailed **root cause / fix / lesson** for each item lives in [`ENGINEERING-NOTES.md`](ENGINEERING-NOTES.md);
the `#N` after an entry is its number in that file.

---

## v1.0.95 — repository hygiene

**Temporary artefacts removed, signing records completed, the uninstall script made bilingual.**

After v1.0.94 shipped, the repository itself got a second sweep. Four stale things turned up —
none of them functional, but each one made what sits in the repository disagree with what was
actually released.

### The four

- **`_orphan.txt` removed from the repository.** It is a temporary list that `static-check.py`
  rewrites on every run, yet it sat in the repository root — so every check dirtied the working
  tree. Output now goes to `_tmp/orphan.txt`, and both are in `.gitignore`.
- **`SIGNINGS.ndjson` went from 32 to 138 records.** The copy in the repository had stopped at
  v1.0.9; the signing records for v1.0.91 through v1.0.94 (12 signings across the plugin, the
  editor and the installer) were missing. It is meant to be an export of the signing ledger and
  had simply not been re-exported for a long time.
- **`卸载还原.ps1` is bilingual.** It is a script users actually double-click and it only
  printed Chinese; it now picks Chinese or English from the system UI language, with both sets
  of comments.
- **README said "41 historical entries", now 42.** Entry 42 was added back in v1.0.92 and the
  README had not caught up.

### Installer rebuilt

`卸载还原.ps1` ships inside the installer payload, so the payload and the installer were rebuilt
and re-signed — the v1.0.94 installer still carried the Chinese-only uninstall script.

### Verification

```
static check   15 rules, 0 failed
regression     install 27 / uninstall 27 and fully clean
signing        3 files Valid + DigiCert timestamp
```

**No tuned value was touched**; `Language` is still `zh`.

---

## v1.0.94 — runtime-side English

**Beyond the panel: logs, command replies, the installer, the editor, the static checker and CI are English now too.**

Earlier rounds translated what was drawn on the panel. English users also have to paste logs
into an issue, run the installer, and read CI results — none of which live on the panel.
This version aligns all of that.

### Plugin: 426 log and reply literals go through L.T

Every Chinese literal inside `Log.LogInfo` / `LogWarning` / `LogError`, `Say()` (command-channel
replies) and `warn` / `info` (binding verification) is wrapped in `L.T`. 439 new translations,
the table grows from 602 to 997 entries. In Chinese mode `L.T` returns the literal unchanged,
so not a byte of the Chinese logs changed.

### Two "nested L.T" cases had never actually been translated

The `[绝顶骤降]` and `[骤降溯源]` log lines were written as
`L.T("A" + "B" + L.T("C"), args)` — the outer `L.T` receives the concatenated string while the
table stores the segment keys, so it never matched. Rewritten as
`L.T("A" + "B" + "C", args)`, one key now covers the whole line.

### Two escapes turned paths into control characters

Two command replies were written as `cmd\anim.csv` and `cmd\tremor.csv`. In C#, `\a` is a bell
and `\t` is a tab — those log paths had carried an invisible character all along.
Corrected to `\\a` / `\\t`, so a real backslash is printed now.

### Installer and editor: bilingual, following the in-game language switch

Both executables were Chinese-only — translate the plugin all you like, the very first step
still blocks an English user.

New `shared/Locale.cs`, referenced by both projects (112 entries). The language is decided in
three layers: `--lang` on the command line > `Language` in
`<game folder>/BepInEx/config/nesarf.burgershop.modder.cfg` > the system UI language
(Chinese systems get Chinese, everything else English). There is no cfg before the install,
so the system language is the fallback.

### Three pre-existing bugs in the editor

Surfaced while checking the English layout; unrelated to translation:

- **The four buttons had never been added to the window** — `MakeButton` built them and the
  events were wired, but `Controls.Add` was missing. The window showed nothing but sliders
  and the save button could not be clicked.
- **The status line sat behind the scrolling panel** — the panel started at y=78, the status
  line at 88.
- **English is wider than Chinese, so names were clipped** — in English mode the label column
  goes 122 to 150 px, the slider start moves 30 px right, and hint rows size themselves.

### Tooling and CI

All 15 rule messages and the result line of `tools/static-check.py` are English;
the job / step names and every message in `.github/workflows/build.yml` are English too.

### Documentation

- `使用说明.en.md` brought up to the same structure as the Chinese manual
- `KNOWN-ISSUES.en.md` and `CHANGELOG.en.md` added
- `README.en.md` brought up to the same structure as the Chinese README
- `ENGINEERING-NOTES.md` opens with an English reading note

### Verification

```
static check   15 rules, 0 failed (release tree)
coverage       997 / 998 (the one left is "bilibili", a product name)
regression     install 27 / uninstall 27 and fully clean / reinstall 27
UI             installer and editor each captured in both languages, item by item
signing        3 files Valid + DigiCert timestamp
```

**No tuned value was touched**; `Language` is still `zh`.

---

## v1.0.93 — runtime-built strings translated

**After the second round, three more batches of text that is "only assembled at runtime" turned up.**

These strings cannot be spotted as Chinese by a static scan — the key `L.T` receives is the whole concatenated string, the table has no row for that exact string, and it falls back to Chinese. Most of them appear in the small blue text (the description lines drawn by `Hint`).

### Batch one: four concatenated hints

- **Absorb · current state** — `"当前场景：" + ChainSlotName() + "，上限 " + …`
- **Demand / Drain status lines** — `"当前：" + DemandCmd("status")`
- **The mode status itself** — what `DemandCmd` returns is `ModeWord() + "模式中 {1} 段 / 共剩余 {0:0.#}s"`
- **The material binding hint in the Riding column** — `"当前绑定：" + (… ? "（无，用手拖的矩形）" : 名字)`

### Batch two: parameter values inside a status string

In the long `demand status` receipt, three of the parameter values themselves were Chinese: `[未生效]`, `(未武装)`, `·闸门关`. The templates had been translated long ago, the values had not, so English mode showed half and half.

### Batch three: 11 more in other files

Item (13) used to scan only `BurgerShopModder.cs`. Once the other `.cs` files in the same directory joined the scan, 7 spots in `GameBuild.cs` (the whole sentence after `Game version:`) and 11 in `PresetStore.cs` (the save / apply / delete receipts for presets) surfaced together — they had **never had a translation**.

While there, the `当前{0}欲 {1:0.#}% · 距叠第 {2} 层还差 {3:0.#}%` line under "Absorb · current state" got a translation too.

### How they were found

Added `tools/_runtime_scan.py`: it takes the first argument of `L.T` / `Hint` / `Sub` / `Section` / `SliderF` / `SliderI` / `AboutRow` and lists every one that is "anything other than a plain literal" (a variable, a concatenation, a ternary, `string.Format`), then each was read by hand.

The scope of static check item (13) also widened to every `.cs` file.

### Verification

In-game screenshots column by column: eight columns, scrolled to the top and to the bottom of each, all small blue text in English (including the `Game version:` line and the long `demand status` receipt). Translation coverage **602 / 603** (the one left out is `bilibili`, a product name, not translated).

## v1.0.92 — English UI round two: filling in the half the first scan could not see

**English UI, round two: filling in the half the first scanner could not see.**

The first round (v1.0.91) translated "the drawing calls that wrap `L.T` directly". This round searched from the other side — grab every string literal that contains Chinese, subtract the ones that fall inside a translation helper's argument span, judge what is left one by one. That turned up seven shapes and 96 new translations in total.

### The seven shapes filled in

- **`Sub()` level-two subsection headers** — this helper had no `L.T` inside it, so all 14 call sites showed Chinese
- **The `_snapHint` status line** — the hint at the bottom of the panel was a bare literal printed straight out, about 25 of them
- **The five severity labels of `SevTag()`** — things like `[蓝·提示]`
- **`ChainSlotName()`** — the on-screen labels such as `骑乘位·常规`
- **`PoseDesc()` and 55 config description templates** — the explanations in the config manager (F1) used to be pure Chinese
- **The return messages of `GameBuild.Summary()` / `PresetStore`** — displayed directly on the panel
- **Chinese baked into format arguments** — such as `"   ← 「全部显示」生效中"`

### Two problems fixed along the way

- **The false "the patch may not have attached" warning in English mode** — the test used to be `verdict.Contains("一致")`; the English string does not contain those two characters, so it always judged a mismatch. Changed to `GameBuild.IsMatch`
- **Clipped text in four places on the system column** — `Recording` was squeezed into `Recor ding`, and a few long buttons lost their tails. `Sub` headers moved out of the horizontal row, long buttons each get a row of their own

### Tools

- **`tools/zh-scan.py` (new)** — a full scan: lexically extract every literal, then subtract the argument spans of the translation helpers. The "the text is stored in a variable first" shape is only visible from this direction
- **Static check item (14) relaxed** — from three control types to every display-type control, and it now takes the whole call by matching parentheses (the old non-greedy match truncated on nested calls and reported already-translated buttons as bare Chinese)
- **Static check item (15) (new)** — the normalised form of the BindP3 description template is what the table keys hold; the comparison in item (13) cannot see them

### Checksums

See the "checksums" section of `README.md`, or `SHA256SUMS.txt`.

## v1.0.91 — full English UI

**Full English UI.** Every sentence a user can see in the plugin now has an English version, 465 in total.

### Coverage

- **Panel** 316: column names, buttons, toggles, slider labels, hint lines, window titles, the bottom status line
- **Config descriptions** 112: the explanations written into `.cfg`, which the mod also displays
- **Self-check and logs** 37: the diagnostic report and the `[drop trace]` trace lines. Users do not normally see them, but in English mode an exported diagnostic report is in their own language

### How it was done

The translation was not done by editing call sites one by one; it is a three-layer structure:

1. **The four drawing helpers wrap `T` internally** — `Section` / `Hint` / `SliderF` / `SliderI`. One change covers nearly every call site, and the rest of the work is only adding rows to the table.
2. **Buttons wrap `L.T` directly**, including the irregular ones: a ternary passed as an argument, a concatenated string, the copy button that reads from the `TabNames` array.
3. **`string.Format` replaced by the `L.T(fmt, args)` overload** — translate the template first, then fill in the arguments, so numbers never get translated.

`L.T("中文原文")` looks the table up; a hit gives English, a miss hands the Chinese back unchanged. So "half translated" is a usable normal state rather than an intermediate state that must not be touched.

### Switching

At the very front of the system column, two buttons, `中文` / `English`, Chinese by default, and switching needs no restart.

One limitation: config descriptions are bound at startup, so after switching the panel changes at once, but the description comments already written into `.cfg` only update on the next start.

### Layout problems fixed along the way

English is longer than Chinese, so a few hard-coded widths clipped the text or wrapped it:

- The "save as preset" button in the preset column is fixed at 90px, and `Save as preset` was clipped to `Save as prese`
- `Feedback email` in the about column was split over two lines inside 74px
- `Copied from:` in the Fellatio column was wrapped the same way

Added two helpers, `BtnW` / `LabelW`, that compute width from the actual layout. Hard-coded widths show no problem at all with only one language; add a second language and they all surface.

### Three items added to the static check

- **Item 13**: every Chinese row in the table must have a matching `L.T` call in the source. Change one character of the Chinese and that row can never match again, while the fallback mechanism quietly keeps showing Chinese.
- **Item 14**: no bare Chinese literals in panel controls. Item 13 checks "can every key in the table be matched to the source", which cannot catch "a new line of Chinese was written into the source without wrapping `L.T`" — the copy button was missed exactly that way.
- The key extraction in item 13 changed to parenthesis matching, so it recognises compile-time merged concatenations such as `L.T("A" + "B")`.

### Known

The 37 self-check / log strings stay Chinese in Chinese mode too, which is expected — they follow the language setting, they just do not appear on the panel.

---

## v1.0.9 — install bug fix, source sweep, documentation

Fixed one install bug reported by a user, plus a handful of small problems found while sweeping the source, and a batch of documentation additions.

### Code

- **Fix issue #1: the installer mistook itself for the game** — besides the exact comparison, `GameRunning()` had a loose `ProcessName.IndexOf("Burger")` match. But the installer is called `BurgerShopModder-Installer` and the mod `BurgerShopModder-Trainer`, both containing `Burger`. So the installer matched the moment it opened, and clicking install always popped "the game is running, please close it first". Changed to exclude the current process and compare only the exact game process name. These two exe names were settled in the round that renamed the assets; before that they were `汉堡店修改器-安装程序.exe`, and a Chinese process name contains no Burger — so this was a regression brought by that rename.

- **The version number in the diagnostic export now uses the constant** — the header of the "full self-check" export had always printed a hard-coded `1.0.0`, while the constant `PluginVersion` had long been 1.0.8
- **The system column is now an explicit `case 5`** — it used to land on `default`, so an out-of-range `_tab` would silently enter the system column too; `default` now returns to the player column
- **Removed the unused field `_hitRectDbg`** — declared, never referenced

### Documentation

- **New English usage guide** (`使用说明.en.md`, 347 lines). The 1479 lines were not translated word for word: tables were translated row by row, the long "why it is designed this way" passages were condensed to a sentence or two, and the top states that "the in-game panel is still Chinese; what you get here are the English names for reference". Both guides have a language switch at the top
- **New English README** (`README.en.md`, 190 lines), with a language switch at the top of both READMEs. Not a word-for-word translation; rearranged around what an English reader needs: how to install, what it requires, why it gets flagged, how to verify the files
- **Merged the duplicate `使用说明.md`** — there was one in the repository root and one in `dist/`, identical in content, kept in sync by hand with `tools/sync.sh`. Now only the root copy exists, and both packaging and CI read it
- **"six columns" changed to "eight columns" in the README and Description** — v1.0.4 added the preset and about columns but three places still said six. The wording now distinguishes "the first six columns are by pose, the last two are tools and information", otherwise a reader would think two more poses had been added
- **README "feature overview" gained the main features and the design trade-offs** — it used to describe only the column structure and the three parameter sets and mention none of the core mechanics
- The repository gained the Chinese alias「汉堡店的四个夜晚」and the naming origin from that Big Smoke order

### Issue status

- **Issue 7** (Sit's demand mode causing abnormal ecstasy changes) confirmed fine by the user's own testing, moved out of `KNOWN-ISSUES`
- **Issue S9** (the Manman hit area may leave the screen) likewise confirmed fine. It was recorded only in `KNOWN-ISSUES`, not in the engineering notes, so the body was moved there first and then deleted
- **Issue 5** (features with zero live verification) changed to "awaiting feedback" — the features are usable, but usage is not enough

### Re-release: repository state corrected

This version was released twice. After the first release, sweeping the project showed that **the code tag v1.0.9 pointed at in the repository was still v1.0.8**.

- Five places in the source still said 1.0.8: `[BepInPlugin]`, `PluginVersion`, `Installer.Version`, the `<Version>` of the three `.csproj` files
- The three artifacts in `dist/` were v1.0.8 as well
- The three SHA256 values in the README followed the old files

The files in the online release were correct (the hand-signed, hand-uploaded copy); what was wrong was this side of the repository, and the three numbers in the README that people check against — downloading v1.0.9 and computing them found all three mismatched.

The root cause was `tools/sync.sh`. It used "one level above the script's own directory" as the working copy, and "working copy/../burger-shop-publish" as the publish copy. But it sat in both trees — run from the publish copy, the two variables resolved to the same directory, so every diff compared equal, the script printed a page of "same" and finally "0 files in total", and it looked like a successful sync. It now works out which tree it is in from the directory name, and stops with an error when it cannot compute two distinct trees.

Other things fixed along the way:

- The sync list gained `GameBuild.cs`, `PresetStore.cs`, the two exes in `dist/` and `卸载还原.ps1`; the four items under `installer/` changed from "review the diff by hand" to automatic sync
- `make_payload.ps1` in the publish copy still pointed at the long-deleted `dist/使用说明.md`, so packaging there was bound to break
- `dist/BurgerShopModder.dll` (an old artifact from before the rename) removed from the repository; `installer/payload.zip` is a build intermediate of the installer, removed from the repository and added to `.gitignore`
- `SIGNINGS.ndjson` stopped at v1.0.2; the 27 entries for v1.0.3 through v1.0.9 added
- Two static check items added: (11) the six version numbers in the source must agree, (12) the three artifacts in `dist/` must carry the same version. Item (12) is a coarse check — the version resource always lives in the PE as UTF-16, so not finding it means that artifact was built from another version

---

## v1.0.8 — assembly renamed

**The plugin's assembly name changed. No functional change.**

### The new assembly name

```
2×9+9L+6[ED]+7+2×45→1[C]+LS
```

This is the compact notation for Big Smoke's order in GTA San Andreas:

> I'll have two number 9s, a number 9 large, a number 6 with extra dip,
> a number 7, two number 45s, one with cheese, and a large soda.

Two number 9s, one number 9 large, one number 6 with extra dip, one number 7, two number 45s one of them with cheese, and a large soda.

Once installed into `BepInEx/plugins/` this is the file name. **The downloadable Release asset is still called `BurgerShopModder.dll`** (readable); BepInEx identifies plugins by reflecting over the plugin attributes, and the file name does not affect loading.

### Knock-on changes this name brought

The **square brackets** in it are a wildcard character class in PowerShell's `-Path`, which makes the path silently unfindable. Therefore:

- `make_payload.ps1`, `sync.sh` and CI were changed to build the file name into a variable and use `-LiteralPath`
- The mapping table between README hashes and dist files in the static check changed with it
- The install path in the usage guide and the plugin section heading in the README changed with it

### Asset names use another spelling

The three asset file names on Releases **differ from the assembly name**, on purpose:

| Location | Name | Why |
| --- | --- | --- |
| Releases asset | `18-9L-6_ED-7-45-45_C-LS-*` | asset names may only hold ASCII; single quotes get turned into dots by GitHub |
| Installed into plugins | the string with multiplication signs and brackets | no such limits, so it can express the meaning in full |

`18` is how two number 9s multiply, `_ED` is extra dip, `_C` is cheese — two notations for the same order.

Two rules established by experiment: **single quotes** become dots on upload (`6.ED` looks like an extension), and **square brackets** break `gh`'s glob parsing so the upload does not happen at all.

### New: leftovers from older versions are cleaned up at install time

After the assembly rename, people upgrading from an older version still have the old `BurgerShopModder.dll` in `plugins` — **two plugins with the same GUID present at once, and BepInEx's behaviour is undefined**.

The installer now checks for it and removes it before unpacking. The criterion is whether the file contains our GUID string (string constants live in the `#US` heap as UTF-16), not the file name alone — a user may have copied something else in there by hand. Before deleting it, it is renamed to `.old`, so a wrong call can still be undone.

The cleanup happens **before unpacking**, not after installing: otherwise a failure halfway leaves two plugins present at once.

### Install

Regression: installs 26 files, install / uninstall / reinstall all exit code 0. After installing there is only one file in `plugins`.

---

## v1.0.7 — about-tab repository URL corrected

**No functional change.** Only the repository URL in the panel's "about" column was corrected.

After the previous version the repository was renamed once (`BurgerShop-FourNights-Mod`), the motivation being search ranking, but the direction was wrong — `FourNightsAtTheBurgerShop` in `FourNightsAtTheBurgerShop-GameTrainer` is **the game's own name** (Four Nights At The Burger Shop with the spaces removed), and reordering the words of a game's name for ranking is not appropriate. The name has been changed back.

The Chinese alias「汉堡店的四个夜晚」is written into the README and the repository description — search engines index page content, the name cannot hold Chinese characters, and in the body it matches just as well.

Regression: installs 26 files, install / uninstall / reinstall all exit code 0.

---

## v1.0.6 — game build verification

Added "game build verification". **No functional change**; it only lets the plugin tell the user whether the version is right.

### New: game build verification

This game **has no version number** — the exe's `FileVersion` holds the Unity version (2022.1.24f1), and `app.info` has only the vendor name and the title. So a user cannot say "I am on version 1.2.3", while whether the patch attaches at all depends entirely on whether the method names and signatures in the assembly have changed.

At startup the plugin now reads
`<game directory>/<exe name>_Data/Managed/Assembly-CSharp.dll`, computes its SHA256 and compares it with the copy from development time.

The verdict is written in two places:

- **Log**: search for「程序集核对」
- **Panel**: the first line of "System → forensics / self-check"; on a mismatch an extra button, "see details (assembly fingerprint)", appears

**Report only, never block** — a different version is not necessarily broken, and a hard block turns "might work" into "definitely does not work".

Two implementation details:

- The dll is opened with `FileShare.ReadWrite` — the Mono runtime has it mapped, so default shared read very likely fails to open it, and the check would then always report "not found", which is worse than having no check
- **"Not found" and "mismatch" are reported separately** — failing to check is not the same as not matching

### Documentation

The README gained a "supported game versions" section: it gives the engine version and the assembly fingerprint, explains why a fingerprint rather than a version number, and what a version mismatch looks like.

Regression: installs 26 files, install / uninstall / reinstall all exit code 0.

---

## v1.0.5 — invocation warning fix

Fixed a call warning that had been hanging around for a long time. **No functional change**; what changed is one internal check and its logging.

### Fix: two call warnings

Occasionally seen in the log:

```
[Warning] 调用 HPGaugeChangePercent 失败：Exception has been thrown by the target of an invocation.
[Warning] 调用 EcstasyGaugeChangePercent 失败：Exception has been thrown by the target of an invocation.
```

(the message reads: calling HPGaugeChangePercent failed: Exception has been thrown by the target of an invocation.)

**The root cause is not "the call timing is wrong"**; two things overlap:

1. The first line of `InvokeFloat` is `if (obj == null) return;`, and `obj` is typed `object` — that is a **reference equality** test, which bypasses the `==` overloaded on `UnityEngine.Object`. After a Unity object is destroyed the C# reference **is not null**, so the cached `player` passes that line after a scene change and then blows up inside `Invoke`
2. `TargetInvocationException` wraps the real cause, and the log recorded only the outer message — **that is why it stayed broken so long: not hard to fix, the log swallowed the clue**

**The fix**: add a guard, `obj is UnityEngine.Object uo && uo == null`; dig the inner exception out and log it (type + message + stack + object type + arguments), detailing each method only once so the log is not flooded.

**What could not be verified**: these two warnings are **occasional**. One round after the fix did not show them again, but not reproducing them once does not mean they will never recur. What is confirmed is that the guard takes effect and the normal call path is not broken.

### Documentation

Issues 3 and 16 each gained a note: **it may not trigger deterministically**, and is hard to reproduce reliably; the possibility that **the game's own source quality is poor** should also be considered.

Regression: installs 27 files, install / uninstall / reinstall all exit code 0.

---

## v1.0.4 — config presets, about column, NSFW notice

Added configuration presets, and made "about" a column of its own. The repository is now also explicitly marked NSFW.

### New: configuration presets

The eighth column, "presets". Store all current settings as a named snapshot and switch back to it whenever you like.

- A name field plus "save as preset"; each row of the list has apply / overwrite / delete
- Storage location: `<game directory>/BepInEx/config/nesarf.burgershop.modder.presets/`
- **A generic snapshot, no per-parameter roster** — `ConfigDefinitions` is stored and read as a whole, so parameters added later join presets automatically
- **Always back up before overwriting** — "apply" overwrites every setting, so a timestamped backup is written first and its file name shown in the hint. This comes from an accident in this project: early tuning of the defaults overwrote a user's thirty-odd tuned parameters with the code defaults
- Round trip verified in practice: value 20 → save preset → change to 77 → apply preset → back to 20, and the backup was generated as expected

### New: the "about" column

The seventh column. Repository URL, bilibili profile, feedback email, each with "open" and "copy".

The email keeps copy only: `mailto:` pops an "choose an app" dialog in some environments, or does nothing at all, and a button that does nothing when clicked is more confusing than not having the button.

### Repository explicitly marked NSFW

A warning block at the top of the README, the repository Description starting with "18+ ·", and `nsfw` / `adult` / `18-plus` added to Topics.

### Corrections

- The preset list listed backup files as presets too; changed to "anything containing a dot is not a preset"
- Eight columns did not fit, the eighth was pushed out of the visible area; column width 66 → 62
- `plugin/BurgerShopModder.csproj` hard-coded `<Version>1.0.0</Version>`, so the DLL's file version stayed at 1.0.0.0

Regression: installs 27 files, install / uninstall / reinstall all exit code 0.

---

## v1.0.3 — button placement fix

Small appearance fixes, **no effect on function**.

- **Two buttons, "trigger tremor manually" and "this run's state", were in the wrong place** — they belong to "tremor" but sat under the "absorb" subsection. Moved into the "ecstasy tremor" subsection
- **The "absorb" subsection renamed to "absorb · current state"** — it holds only two read-only rows, the chain drain state and the absorb rate, and the old name was too broad

### Internal

- Static check item (10) added: verify the SHA256 values in the README against the actual files in `dist/`
- Later that item was changed from a hard failure to a **reminder** — the hashes in the README describe "the released version", while `dist/` holds build output, so a difference after a code change is normal
- The signing record `SIGNINGS.ndjson` added to the repository (redacted: local paths removed)

---

## v1.0.2 — emergency installer fix

**Emergency fix: the installer could not install.**

### What was fixed

**One, BackgroundWorker did not declare progress reporting**

```
此 BackgroundWorker 声明它不报告进度。
请修改 WorkerReportsProgress 以声明它报告进度。
```

(the message reads: this BackgroundWorker declares that it does not report progress. Please change WorkerReportsProgress to declare that it does report progress.)

Both the install and the uninstall path had this problem.

**Two, directory entries in the zip were created as files**

After fixing the first one, the install broke again at 7%:

```
安装失败：未能找到路径"…\BepInEx\config\"的一部分
```

(the message reads: install failed: could not find a part of the path "…\BepInEx\config\".)

### Why both were missed

Before release only "it compiles + the resources are embedded" was verified; **the installer was never actually run once**. After this version a full regression was added: install / uninstall / reinstall, all three paths.

---

## v1.0.1 — everything after v1.0.0

Every change since v1.0.0. The focus of this version is not new features but **turning the invisible problems into visible ones**.

### New features

- **Burger-making countdown adjustment** — per-order duration and countdown speed (0 = frozen, take your time)
- **Automatic config migration** — upgrading from an older version no longer silently loses tuned parameters. At startup the config version is checked, and on an old format it backs up, moves values and logs. One migration moved 105 keys in testing

### New ways to check

- **Game binding layer** — turns "which class does the method belong to" into a verifiable table. Getting the class wrong is a silent failure in Harmony; it now becomes a warning in the startup log
- **Static consistency check** (`tools/static-check.py`, 9 items) — checks whether a config entry exists in the panel / the `set` command / the manual; whether patched methods collide by name; whether local paths are present; whether decorative symbols are present
- **CI** (`.github/workflows/build.yml`) — runs the safety valve, artifact integrity and the static check on every push; builds a Release automatically when a tag is pushed
- **Screenshot verification chain** (`tools/tabshot.sh`) — column switching and the screenshot happen in the same frame, the panel rectangle comes from the plugin and the crop is exact

### Fixes

- **The mode name in the middle of the screen always showed "Demand"** — three hard-coded places, and the Fellatio / Riding columns showed it too. Changed to determine the pose when the mode is entered, so changing pose mid-mode does not display the wrong name
- **The Manman hit area could leave the screen entirely** — a rectangle with a negative y was observed. Added two gates: degenerate detection and screen intersection
- **The same setting appeared as two sliders on the panel** — the chain drain cap was drawn once in the tremor section and once per column
- **Panel readability** — the default skin is too transparent, and labels were nearly unreadable when the game scene showed through; an opaque background was added
- The subsection structure of the six columns unified (`Section` / `Sub` / `Hint` / `Rule`)

### Internal cleanup

- `DrawDemandSection` split up from 296 lines, the Riding-only parts extracted into `DrawSitExtras`, the Rear-only parts into `DrawOsiriExtras`, both called from the same place, so the structure is symmetric
- Decorative and judgement symbols all removed (circle check marks, cross marks, warning triangles, circled numbers and so on), and enforced as CI item (9)
- Hard-coded paths in `installer/` made derivable (the script's own location / `tools/gamepath.txt`)
- Documentation tightened: `KNOWN-ISSUES` holds only unresolved items, historical entries live in `ENGINEERING-NOTES`
- Temporary files no longer write to the C drive (an in-repository `_tmp` instead, overridable by an environment variable)

### Known

See [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md) (6 items).
One of them is "features with zero live verification" — a batch of features was written and never ran in real play.

---

## v1.0.0 — first public release

### Features

- **Six-column panel**: player / Fellatio / Rear / Riding / outfit / system
- **Three parameter sets by pose**: 54 parameters × 3 = 162 independent keys (`_Fella` / `_Osiri` / `_Sit`)
- **Demand / Drain modes**: spank or click to build urge → stack layers → longer duration + more attack power + faster animation
- **Afterglow**: a tail of chain drain after the mode ends, "lower base speed, each one lasts longer"
- **Binding kiss**: entered automatically, prevents the window from decaying, three independent speed controls
- **Ecstasy tremor / adaptation**: decaying sine wave + the health bonus must be paid back within the adaptation window
- **Self-built hit area** `HitArea_Manman_By_Plugins`: the model has no such part, the plugin works out the rectangle itself
- **Installer**: single file, precise uninstall from a manifest, silent install on the command line

### Diagnostics

- An in-game **file-based command channel** (never touches the keyboard or mouse driving the game)
- `xcheck` — **23 runtime state invariants**
- Diagnostic commands: `states` / `manman` / `demand status` / `urgeaudit` / `animlog` and others
- An activity recorder, config snapshot and restore, logic cross-checks

### Burger-making countdown adjustment

- `BurgerTimeAll`: per-order make duration (0 = the original 10 seconds)
- `BurgerTimeSpeed`: countdown speed (100 = normal, 50 = half speed, 0 = frozen)
- The panel is in the "player" column, with three shortcuts: original / frozen / relaxed

It hooks the **prefix** of `MenuControl.Timing`, because the game decrements first and tests for zero second.

### Config schema-isation (part one)

The 54 hand-written branches of the `set` command became a **generic path** (through the `_p3ByName` index):

- **A new per-pose parameter is supported by the `set` command automatically**, no need to edit the switch again
- All three types covered (float / int / bool)
- Verified in practice: `set DemandAttBonus` / `set DemandEnabled 42` / `set AfterglowPerSyasei` all went through

### Config migration

Upgrading from an older version **migrates automatically** (the old version had "one global set of keys", this version has "three sets by pose"):

- At startup `ConfigVersion` is checked, and an old config is moved automatically
- **Moved only when all three copies are still at their defaults** — parameters already tuned are never overwritten
- The original config is backed up as `*.bak-before-v1`
- The log lists which keys were moved

Verified: **105 keys** migrated successfully in one pass.

### Fixes in this version (34 items)

- Demand urge accumulated by attacks did not trigger the second and later stacks (#1)
- The merge path of concurrent config writes was not cleanly verified (#2)
- `xcheck` invariants did not cover the new state (#4)
- Sit mode had no convenient place to accumulate drain urge by hand (#6)
- Sit could not trigger chain drain, and therefore could not trigger afterglow (#8)
- Manually entering afterglow did not work (#9)
- Demand mode lasting too long blocked the seated entry indefinitely (#10)
- The chain drain cap split into four sets (a feature change, not a defect) (#11)
- Afterglow could only be triggered by activity inside the demand / drain modes (an invariant) (#12)
- Chain drain was terminated early for no reason (Riding) (#13)
- Seated reaching its mark pushed aside a demand mode in progress (#15)
- The panel changed the "current pose" copy rather than the copy of the column you were looking at (#17)
- Refresh the duration once the stacks are full (a new feature) (#18)
- [experimental] clickable-area highlight (#19)
- Manman area: the Cubism route did not work out, changed to two legs, "material binding + drag by hand" (#20)
- Material binding listed only HitArea*, so no useful material could actually be picked (completed) (#21)
- The manual completed (96 parameters + six new concepts) (#22)
- The remaining 31 parameters were also made three sets by pose (with a rescue of a user's tuning along the way) (#23)
- HitArea_Manman was wired only to the "touch" path, and a mouse click never went through it (#24)
- HitArea_Manman could not stand in for HitArea_Head_Sit (the functions would cross) (#25)
- HitArea_Manman should be symmetric with "spank": raise drain urge + give the seated motion a speed impulse (#26)
- Seated drain urge reaching full jumped to Osiri (three symptoms, one root cause) (#27)
- There appeared to be "two Manman areas" (one self-built, one apparently from the model) (#28)
- Binding kiss needed **its own** speed control (entering the kiss is not the same as triggering chain drain) (#29)
- HitArea_Manman finally has empirical evidence (and the diagnostic command fixed along the way) (#30)
- HitArea_Manman_By_Plugins: renamed + visualisation unified with the other HitAreas (#31)
- The last three parameters brought into the three-set system + item 18 verified (#32)
- xcheck invariants extended to the new state (#4) (#34)
- Seated lock + Fellatio-related items removed from the Riding column + log tags named by pose (#35)
- Seated lock: the exit paths verified as "fully covered" (#36)
- The game got laggy: per-frame reflection and vertex scanning (now cached) (#37)
- Binding kiss bridge audit + three gaps + animations "played directly on the layer" could not be sped up (#38)
- Wording unified across the three columns: description templates swap words by column (#39)
- Sudden-drop tracing log + three more sections in the manual (#40)

---
