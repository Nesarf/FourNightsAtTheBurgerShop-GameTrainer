> ## WARNING: This repository is for adult content
>
> **This is an 18+ repository.**
>
> The game it targets, *Four Nights at the Burger Shop*, is an adult work, and the
> manual and changelog mention that game's adult content (no images, models or assemblies are included).
>
> **Please do not continue if you are under 18.** If adult content makes you uncomfortable, leave this page.
>
> This repository **contains none of the game's assets** — no textures, no models, no assemblies.
> Only the mod's source, its documentation and the build outputs.

---

[**中文**](README.md) | **English**

---

# 汉堡店的四个夜晚 (Four Nights at the Burger Shop) · Numeric Modifier

A **BepInEx plugin + companion config editor** for
*Four Nights at the Burger Shop ～ハンバーガー食べながら食べられるミニゲーム～*.
The game itself is never modified.

> **The Chinese community also writes this game as 「汉堡店的四个夜晚」.**
> If you searched for 「汉堡店的四个夜晚修改器」, 「Four Nights at the Burger Shop 修改器」,
> 「汉堡店四个夜晚 数值修改器」 or 「burger shop 修改器」 — this is the place you are looking for.
> The original Japanese title is *Four Nights at the Burger Shop ～ハンバーガー食べながら食べられるミニゲーム～*.

> **This repository contains no assets or assemblies from the game.**
> Building requires you to take the reference DLLs **from your own game installation** (see 「Build」 below).
> That is deliberate — game assets are copyrighted and should not be redistributed.

---

## Runtime environment

The complete dependency list. **Every entry says who provides it** —
anything marked "ships with the system" or "ships with the installer" needs nothing from you.

### 1. The trainer (three programs)

| Dependency | Who provides it | Usually already present |
| --- | --- | --- |
| Windows (x64 or x86) | your machine | required |
| **.NET Framework 4.7.2** | the system | built in on Win10 1809+ / Win11; needs installing on Win7 SP1 / 8.1 |
| Windows Forms | part of .NET | comes with .NET once it is installed |
| `System.IO.Compression` | part of .NET | same as above (used by the installer to unpack) |

**.NET Framework 4.7.2 download** (only Win7 / 8.1 need it):
[offline installer](https://dotnet.microsoft.com/download/dotnet-framework/net472)

### 2. The plugin (`2×9+9L+6[ED]+7+2×45→1[C]+LS.dll`)

> This file name is the **assembly name**. It contains multiplication signs and square brackets,
> so it looks like an arithmetic expression.
> The release asset you download is called `18-9L-6_ED-7-45-45_C-LS.dll` (readable); once installed
> into `BepInEx/plugins/` it carries this assembly name.
> **BepInEx identifies plugins by reflecting on their attributes; the file name does not affect loading.**

| Dependency | Who provides it | Usually already present |
| --- | --- | --- |
| BepInEx 5.4.23.5 | **ships with the installer** | nothing to do |
| ├ `BepInEx/core/BepInEx.dll` and others | ships with the installer | nothing to do |
| ├ `0Harmony.dll` (HarmonyX) | ships with the installer | nothing to do |
| ├ `Mono.Cecil.dll` | ships with the installer | nothing to do |
| ├ `MonoMod.*.dll` | ships with the installer | nothing to do |
| ├ `winhttp.dll` (the Doorstop injector) | ships with the installer | nothing to do |
| └ `doorstop_config.ini` | ships with the installer | nothing to do |
| Unity 2022.1's Mono runtime | **ships with the game**: `MonoBleedingEdge` | nothing to do |
| Game assemblies (`Assembly-CSharp.dll` and others) | ships with the game | nothing to do |

> **Only a manual install** (putting the `.dll` into `BepInEx/plugins/` yourself) needs BepInEx to exist beforehand.
> For a manual install, get **5.4.23.5 x64** from
> [BepInEx Releases](https://github.com/BepInEx/BepInEx/releases) (this game's Unity build is 64-bit).

### 3. The game itself

| Dependency | Who provides it | Usually already present |
| --- | --- | --- |
| Windows | your machine | required |
| **graphics driver** | the GPU vendor | you have to keep it reasonably new yourself |
| `d3d11.dll` — Direct3D 11 (main renderer) | the system | built in on Win10 / 11 |
| `dxgi.dll` | the system | built in |
| `d3d9.dll` — Direct3D 9 (fallback) | the system | built in |
| `opengl32.dll` — OpenGL (fallback) | the system | built in |
| `vulkan-1.dll` — Vulkan (optional) | the graphics driver | comes with a proper driver install |
| `d3dcompiler_47.dll` — shader compilation | the system | built in on Win10 / 11; **possibly missing on Win7 / 8** |
| `xinput1_3.dll` — gamepad support | the system | built in on Win10; **possibly missing on Win7 / 8** |
| `dsound.dll` — DirectSound | the system | built in |

**On Win7 / 8, if `d3dcompiler_47.dll` or `xinput1_3.dll` is missing**,
installing the [DirectX End-User Runtime](https://www.microsoft.com/download/details.aspx?id=35) once is enough
(the package is small). Both files are inside it.

> By the way: `UnityPlayer.dll` itself references `winhttp.dll` — and BepInEx's Doorstop injects
> by replacing exactly that DLL. So it is a very natural injection point.

### 4. Things you do not need to install

| Not needed | Why |
| --- | --- |
| BepInEx | the installer ships it (embedded in the exe) |
| .NET SDK / .NET runtime (CoreCLR) | this uses .NET **Framework**, not .NET 5+ |
| **Visual C++ runtime** | `UnityPlayer.dll` does not reference `vcruntime140` / `msvcp140` |
| Mono | the game folder ships `MonoBleedingEdge` |
| administrator rights | installing into your own game folder is enough |
| network access | the BepInEx inside the installer is embedded; it never downloads anything |
| Java / Python / Node and the like | unrelated to this project |

### 5. Troubleshooting order

When the game will not start, **check in this order** — do not suspect the trainer first:

1. **Launch the game without the trainer** — does it reach the game?
2. Cannot get in → a graphics driver / DirectX runtime problem, unrelated to the trainer
3. Gets in but F9 does nothing → only then is it the trainer's turn (see 「FAQ」 below)

---

## Supported game versions

**This game has no version number.** The exe's `FileVersion` holds the Unity version, and `app.info` contains
only the publisher name and the title (`幻夢劇場シャルロッテ | Four Nights at the Burger Shop`).

So what follows are **facts you can check yourself**, not a sentence like "supports v1.0".

### The build this was developed and tested against

| Item | Value |
| --- | --- |
| **Engine** | Unity **2022.1.24f1** (`709dddfb713f`) |
| **Game assembly** | `Four Nights at the Burger Shop_Data/Managed/Assembly-CSharp.dll` |
| **Size** | 293888 bytes |
| **SHA256** | `9f2c64db2dc259d4347aeb00ef644507a22aee9d5a3b66328cb27babda9a0bb4` |

Check it yourself:

```powershell
Get-FileHash "<game folder>\Four Nights at the Burger Shop_Data\Managed\Assembly-CSharp.dll" -Algorithm SHA256
```

**It matches** → this is the same file the trainer was developed against, and every feature was verified against it.

**It does not match** → not necessarily broken, but **unverified**. See below.

### Why an assembly fingerprint instead of a version number

This trainer uses Harmony to attach patches to **named methods** in the game:

```
TabemiControl.Osiri叩かれる / UpdateAtt / Osiri解除 / 頭叩かれるSit
Live2D_Animation_SitOsiri.Event_OsiriSyasei*
MenuControl.Timing / BuildMenu
```

**If a method name or a signature changes, the matching patch cannot be hooked.** A version number is whatever
the publisher felt like calling it; the assembly is what actually decides whether a patch can be attached.

### What happens when the version does not match

It does not crash, and it does not corrupt your saves. The symptom is that **some features do nothing**:

- the plugin reports 「找不到 xxx」 ("cannot find xxx") or 「挂载失败」 ("hook failed") in the log on startup
- the panel shows the number of **hook points mounted (36)** — **a shortage means some patch was not hooked**
- pressing F9 in game still opens the panel, but changes in that tab do not take effect

Log location: `<game folder>/BepInEx/LogOutput.log`.
Search for `找不到` or `挂载` to see which ones did not succeed.

### The plugin verifies this itself

No manual comparison needed. The plugin reads the game assembly once on startup and writes the conclusion in two places:

- **Log**: search for 「程序集核对」 ("assembly check") in `<game folder>/BepInEx/LogOutput.log`;
  you will see 「与开发时一致」 ("matches the development build") or 「与开发时【不一致】」 ("does NOT match")
- **Panel**: the first line of **System → forensics / self-test** (`系统 → 取证 / 自检`) is that result.
  When it does not match, an extra **「看详情（程序集指纹）」** ("show details (assembly fingerprint)") button
  appears, and opening it shows the expected and the actual value

**It reports only, it does not block** — a different build is not necessarily broken, and hard-blocking would turn
"might work" into "definitely will not work".

### If you are on a different build

Two options:

1. **Tell me** (repository issues / bilibili / email all work) — if that assembly can be obtained,
   comparing method names and signatures tells us where it differs
2. **Check yourself** — the panel's **System → forensics / self-test** has a binding check that lists what was not hooked

> This game has DLC and builds from different distribution channels. **Builds from different channels may differ**,
> so if yours came from somewhere else, check the SHA256 above before installing.

---

## About the plugin's name

After installation the file under `BepInEx/plugins/` is not an English word but a string of symbols:

```
2×9+9L+6[ED]+7+2×45→1[C]+LS
```

It is a compact notation of **Big Smoke's order in GTA San Andreas**. The original line:

> I'll have two number 9s, a number 9 large, a number 6 with extra dip,
> a number 7, two number 45s, one with cheese, and a large soda.

Item by item:

| Line | Notation |
| --- | --- |
| two number 9s | `2` times `9` |
| a number 9 large | `+ 9L` |
| a number 6 with extra dip | `+ 6[ED]` (ED = extra dip) |
| a number 7 | `+ 7` |
| two number 45s | `2` times `45` |
| one with cheese | the arrow points at `1[C]` (C = cheese) |
| and a large soda | `+ LS` (LS = large soda) |

Put into plain words: **two number 9s, one number 9 large, one number 6 with extra dip, one number 7,
two number 45s with cheese on one of them, and one large soda.**

Why it is called that — ah, it is just a meme, there is no reason behind it.

**It does not affect use**: BepInEx identifies plugins by reflecting on their attributes, so the file may be called
anything at all. The two executables on the Release still have ASCII names, because release asset names cannot
hold these characters.

---

## What is in here

| Directory | Contents |
| --- | --- |
| `plugin/` | BepInEx plugin source (Harmony patches, in-game command channel, config guard, activity recorder) |
| `trainer/` | source of the companion trainer `数值修改器.exe` (edits the config outside the game) |
| `installer/` | installer source (installs BepInEx and the plugin into the game folder, single-file distribution) |
| `tools/` | debugging scripts (drive the game, snapshot the config, cross-check the logic) |
| `dist/` | the compiled products |
| `使用说明.md` | **the full manual** (features, parameters, troubleshooting) |
| `KNOWN-ISSUES.md` | **what is currently unresolved** (only the unfixed ones) |
| `ENGINEERING-NOTES.md` | **engineering notes**: symptom / root cause / fix / lesson for every pitfall (41 numbered entries + 5 appendices + 5 S-series entries, with a table of contents) |
| `CHANGELOG.md` | per-version changes |

## Just download it (no build)

`dist/` holds the finished products:

- `汉堡店修改器-安装程序.exe` — the installer: double-click, pick your game folder, done
- `数值修改器.exe` — the trainer: edits the config outside the game
- `18-9L-6_ED-7-45-45_C-LS.dll` — the plugin itself (drop it into `BepInEx/plugins/` for a manual install)

> For a manual install it is fine if the file you drop in is called `18-9L-6_ED-7-45-45_C-LS.dll` —
> **BepInEx identifies plugins by reflecting on their attributes; the file name does not affect loading**.
> An installer run gives you the assembly name (that arithmetic-looking string); either one works.

---

## Build

**Prerequisites**: the .NET SDK (`net472` target, Windows), BepInEx 5.4.x.

### 1. Prepare the references

The plugin needs the game's assemblies in order to compile, but **this repository does not include them**. Please:

1. create a `refs/` directory at the repository root
2. copy these from **your own** game installation (usually `<game folder>/<game>_Data/Managed/`):

```
UnityEngine.dll
UnityEngine.CoreModule.dll
UnityEngine.IMGUIModule.dll
UnityEngine.InputLegacyModule.dll
UnityEngine.TextRenderingModule.dll
UnityEngine.ImageConversionModule.dll
UnityEngine.PhysicsModule.dll
UnityEngine.UI.dll
UnityEngine.UIModule.dll
UnityEngine.TextMeshPro.dll        （如有 / if present）
```

3. copy these from the BepInEx distribution:

```
BepInEx.dll
0Harmony.dll
```

### 2. Compile

```bash
dotnet build plugin/BurgerShopModder.csproj -c Release
dotnet build trainer/BurgerShopTrainer.csproj -c Release
```

The output lands in `dist/`.

### 3. The installer (optional)

```bash
powershell -ExecutionPolicy Bypass -File installer/make_payload.ps1
dotnet build installer/Installer.csproj -c Release
```

`make_payload.ps1` packs the products in `dist/` together with BepInEx into `installer/payload.zip`,
which is then embedded into the installer exe.

---

## Feature overview

The panel has eight tabs. **The first six are split by which pose you are interacting with the character in**;
the last two are tools and information:

```
玩家 / 口交 / 背榨 / 正骑 / 换装 / 系统 / 预设 / 关于
```

In English, in the same order: Player / Fellatio / Rear / Riding / Outfit / System / Presets / About.

- **Presets** (`预设`) — store the entire current configuration as a named snapshot and switch back to it at any time
- **About** (`关于`) — repository URL, bilibili, feedback email

The same mechanism goes by a different name in each of the three tabs — **Fellatio 「吸取」 Absorb,
Rear 「索取」 Demand, Riding 「榨取」 Drain** — and **the parameters are three independent sets**
(`_Fella` / `_Osiri` / `_Sit`), so changing one tab does not affect the other two.

### Main features

- **ecstasy tremor** — while the ecstasy value accumulates, a decaying sine wave is applied in tiers
  (顿挫 / 痉挛 / 深陷 / 迟疑 / 潮汐, i.e. stutter / spasm / sink / hesitation / tide in that order);
  press R to steady it
- **ecstasy adaptation** — after a tremor ends the amplitude is adjusted temporarily, and the end restores it exactly,
  with a safety valve so the loop cannot fail to converge
- **afterglow** — an extra state after a chain drain ends; accumulation and trigger conditions are adjustable
- **binding kiss** — an independent speed control exclusive to Riding (entering the kiss and triggering the chain drain
  are two separate things)
- **Manman zone** — a hit area symmetric with spank: it raises the demand urge and gives the seated animation a speed impulse
- **config presets** — store the entire current configuration as a named snapshot and switch back to it at any time
  (an automatic backup is taken before overwriting)
- **game build check** — compares the game assembly fingerprint on startup and tells you plainly when the version does not match

### Some design trade-offs

- **three independent parameter sets** — the same parameter has its own value in each of the three poses,
  so changing one tab does not affect the other two
- **safe with two config writers** — the in-game panel and the external editor can both change it without
  overwriting each other
- **a way back before overwriting** — applying a preset or migrating the config always backs up first,
  and writes out the backup file name

The full documentation is **[`使用说明.md`](使用说明.md)**.

---

## Debug command channel

The plugin provides a **file-based command channel**, so a script can drive the game (without touching keyboard or mouse).
Just write `BepInEx/l2d_dump/cmd/in.txt`:

```bash
bash tools/gcmd.sh states          # 状态机快照
bash tools/gcmd.sh manman          # HitArea_Manman 诊断
bash tools/gcmd.sh xcheck          # 逻辑交叉检查（23 条不变量）
bash tools/gcmd.sh demand status   # 索取/榨取模式状态
```

Read in English, those four comments are: state machine snapshot · HitArea_Manman diagnostics ·
logic cross-check (23 invariants) · demand/drain mode status.

The script needs `tools/gamepath.txt` pointing at your game folder (**that file is not in the repository**):

```
<game folder>
```

---

## Documentation

| Looking for | Where to go |
| --- | --- |
| how to use it, what each parameter is | [`使用说明.en.md`](使用说明.en.md) (Chinese original: [`使用说明.md`](使用说明.md)) |
| **what is still unfixed right now** | [`KNOWN-ISSUES.en.md`](KNOWN-ISSUES.en.md) |
| **why a feature looks the way it does** (reverse-engineering findings, pitfalls) | [`ENGINEERING-NOTES.md`](ENGINEERING-NOTES.md) — Chinese working log, with an English reading note at the top |
| what changed in each version | [`CHANGELOG.en.md`](CHANGELOG.en.md) |

`ENGINEERING-NOTES.md` holds **41** historical entries + **5** appendices + **5** S-series entries,
each written as **symptom / root cause / fix / lesson**. It opens with a **table of contents**,
so you can jump straight to one entry.

`KNOWN-ISSUES.md` is a **derived view** of it — only the unresolved ones, one line of status per entry,
with numbering that matches the engineering notes.

The pitfalls include:

- hooking the wrong class with Harmony **fails silently** (one method name may belong to different classes)
- the game's animations have **two playback paths** (the mixer's `*.State`, and `*.CurrentState` of `Play` on a layer);
  the speed logic has to cover both
- **mouse and touch go through two completely different paths** (a static field vs. a method return value)
- with three parameter sets, which physical key a `set` command lands on **depends on the current pose**

---

## Code signing

**All three executables carry a signature**, issued to **S.M.Y.T.** (a self-signed certificate, not a commercial one).

```
汉堡店修改器-安装程序.exe    O=S.M.Y.T., CN=S.M.Y.T. Code Signing
数值修改器.exe              O=S.M.Y.T., CN=S.M.Y.T. Code Signing
18-9L-6_ED-7-45-45_C-LS.dll       O=S.M.Y.T., CN=S.M.Y.T. Code Signing
```

You can verify it yourself:

```powershell
Get-AuthenticodeSignature "18-9L-6_ED-7-45-45_C-LS-Installer.exe" |
    Select-Object Status, @{n='Signer';e={$_.SignerCertificate.Subject}}
```

### Why self-signed rather than a commercial certificate

**The organization field of a certificate is filled in by the issuing party.** A commercial CA fills it with
a **vetted legal entity**; it wants to see a business licence. S.M.Y.T. is not a company, and should not become
one just to fill in a field.

So self-signing is not "a concession because a certificate is unaffordable" — **it is the only form that can
honestly state who made this**. The root of trust stays with the person who made the thing, rather than with
a company that can revoke unilaterally, rewrite its terms, or cease to exist some day.

### What this means, spelled out

| | commercial CA | self-signed (this project) |
| --- | --- | --- |
| organization field | from a **vetted company** | one's own declaration |
| root of trust | the CA's, unpublished, not auditable | **yours, held by you** |
| being revoked | the CA can revoke unilaterally | nobody can, and nobody needs to |
| verifies on a machine that trusts the certificate | yes | yes |
| **verifies on a stranger's machine** | yes | **no** |
| **clears the SmartScreen warning** | yes | **no** |

**Therefore: this signature does not clear the SmartScreen warning, and it does not clear antivirus false positives.**

On first run you may still see 「Windows 已保护你的电脑」 ("Windows protected your PC") —
click 「更多信息」 ("More info") → 「仍要运行」 ("Run anyway").

(This can appear **even with a commercial certificate**: SmartScreen also accumulates "download reputation".)

### Antivirus software may flag it, and the reason is explainable

| What is in the repository | Why it gets misjudged |
| --- | --- |
| `winhttp.dll` (BepInEx's Doorstop) | it **replaces a system DLL in the game folder** in order to inject code, which is exactly the typical method of a cheat |
| `18-9L-6_ED-7-45-45_C-LS.dll` | modifies values in the game's memory at runtime |
| the installer | writes files into the game folder and edits `doorstop_config.ini` |

**In behaviour it really does look like a cheat, because it is one.** The only difference is that you installed it
voluntarily, and that it changes this one game only.

### How to confirm the file you downloaded was not modified

**Use the signature and the SHA256 together.**

The signature proves "this was signed by S.M.Y.T."; the checksum proves "what you got is the same thing that was published".

```powershell
# 1. 下载 exe 和 SHA256SUMS.txt 到同一个目录
# 2. 算你自己的哈希
Get-FileHash "18-9L-6_ED-7-45-45_C-LS-Installer.exe" -Algorithm SHA256
# 3. 和 SHA256SUMS.txt 里的那行比，一样就没被动过
```

Those three comments count the steps: download the exe and `SHA256SUMS.txt` into one directory · compute your own
hash · compare it with the matching line in `SHA256SUMS.txt`, and if it is the same the file was not touched.

The values for v1.0.94 are:

```
0b4523ec27d6da32bcee36633462183ed5afd4323d3bc0d6e7c90a50f49aa542  18-9L-6_ED-7-45-45_C-LS-Installer.exe
1b860ccca4dc193b527616f5b3a4cd469d0bf213ffb050a0e5dbb031408fc0d9  18-9L-6_ED-7-45-45_C-LS-Trainer.exe
080614bf2ab409e7e658f06e4c203ed197ed71ce950ae5086ae7f09a085d2a49  18-9L-6_ED-7-45-45_C-LS.dll
```

**If the checksum does not match, do not run it** — that means the file was modified on the way.

Every signing is recorded in the repository's [`SIGNINGS.ndjson`](SIGNINGS.ndjson) —
it records the SHA256 both **before** and **after** signing, so it answers two things:
which file went in to be signed, and which hash was published once it came out.
Local paths have been stripped.

### Put plainly

**A checksum can prove "what you got is the same as what I published", but it cannot prove that "what I published
is itself benign".** That judgement is yours to make, and only yours. The repository is fully open; the code is all here.

## FAQ

### The install reports 「此 BackgroundWorker 声明它不报告进度」

**That is a bug in v1.0.0 and v1.0.1; it is fixed in v1.0.2.**

```
此 BackgroundWorker 声明它不报告进度。
请修改 WorkerReportsProgress 以声明它报告进度。
```

In English, that message reads: *This BackgroundWorker states that it does not report progress.
Set WorkerReportsProgress to declare that it reports progress.*

Please get the latest `18-9L-6_ED-7-45-45_C-LS-Installer.exe` from
[Releases](https://github.com/Nesarf/FourNightsAtTheBurgerShop-GameTrainer/releases).

**You do not need to uninstall first**; just download the new one and install again.

(This note stays because some people may still be holding an old version.)

### After installing, pressing F9 in game does nothing

Check in order:

1. **Did it install into the right folder** — the game folder should now show a `BepInEx` folder and `winhttp.dll`.
   If it went somewhere else, clear it with the installer's 「卸载还原」 ("uninstall and restore") and install again.
2. **Is the game being started through some other entry point** — some launchers skip the `winhttp.dll` injection.
   Try double-clicking `Four Nights at the Burger Shop.exe` in the game folder directly.
3. **Look at the log** — `<game folder>/BepInEx/LogOutput.log`.
   A normal load has the line `数值修改器已加载 — F9 开关面板` ("Numeric Modifier loaded — F9 toggles the panel").

### Restoring the vanilla game

Use the installer's 「卸载还原」 ("uninstall and restore") button, or run `卸载还原.ps1` in the game folder.

**It deletes precisely according to the install manifest**: only the files this install put there.
It does not touch a BepInEx you already had, and it does not touch your saves or your configuration.

### Where the config file is

```
<game folder>/BepInEx/config/nesarf.burgershop.modder.cfg
```

Upgrading from an older version **migrates automatically** (values in the old format are carried over to the new
format, with an automatic backup).

## License

The code is released under the MIT licence (see `LICENSE`).

**All rights to the game itself, its assets and its assemblies belong to the original author.**
This project contains only code written for it, and neither includes nor distributes any game content.
