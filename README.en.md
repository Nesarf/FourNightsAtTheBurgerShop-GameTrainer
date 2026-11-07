> ## WARNING: ADULT CONTENT
>
> **This is an 18+ repository.**
>
> The game it targets, *Four Nights at the Burger Shop*, is an adult work.
> Documentation and changelogs mention that game's adult content (no images, models or assemblies are included).
>
> **Do not continue if you are under 18.** Leave this page if adult content makes you uncomfortable.
>
> This repository contains **none of the game's assets** — no textures, no models, no assemblies.
> Only the mod's source, documentation and build outputs.

---

[**中文**](README.md) | **English**

---

# 汉堡店的四个夜晚 (Four Nights at the Burger Shop) · Numeric Modifier

A **BepInEx plugin + companion config editor** for
*Four Nights at the Burger Shop ～ハンバーガー食べながら食べられるミニゲーム～*. The game itself is never modified.

> **This repository contains no game assets or assemblies.**
> Building from source requires you to supply the reference DLLs from your own game installation.
> That is deliberate — game assets are copyrighted and must not be redistributed.

> **The game is also called 「汉堡店的四个夜晚」 in Chinese communities.**
> Japanese title: *Four Nights at the Burger Shop ～ハンバーガー食べながら食べられるミニゲーム～*.

---

## Download

Get `18-9L-6_ED-7-45-45_C-LS-Installer.exe` from the [Releases](https://github.com/Nesarf/FourNightsAtTheBurgerShop-GameTrainer/releases) page.

Double-click it, pick your game folder, launch the game, press **F9**.

| File | What it is |
| --- | --- |
| `18-9L-6_ED-7-45-45_C-LS-Installer.exe` | Recommended. Installs BepInEx + the plugin in one step |
| `18-9L-6_ED-7-45-45_C-LS-Trainer.exe` | Edits the config outside the game |
| `18-9L-6_ED-7-45-45_C-LS.dll` | The plugin itself, for manual installs |
| `Manual-zh-CN.md` | Full manual (Chinese) |
| `SHA256SUMS.txt` | Checksums |

---

## Requirements

**You do not need to install BepInEx** — the installer bundles it.

| Requirement | Notes |
| --- | --- |
| **Windows** | The game is Windows-only (Unity 2022.1, Mono) |
| **.NET Framework 4.7.2** | Built in on Windows 10 1809+ and Windows 11; install it on Windows 7 SP1 / 8.1 |
| **A working GPU driver** | Needed by the game itself; a bad driver means the game won't start |

**Windows 7 / 8**: you may be missing `d3dcompiler_47.dll` or `xinput1_3.dll`.
Install the [DirectX End-User Runtime](https://www.microsoft.com/download/details.aspx?id=35) once.

**Troubleshooting order**: launch the game **without** the mod first.
If it won't start, that's a driver or DirectX problem, not the mod's.

The full dependency list (every item, with who provides it) is in the Chinese README's 「运行环境」 section.

---

## Supported game version

**The game has no version number.** Its `FileVersion` field holds the Unity version, and `app.info`
only contains the publisher name and title. So there is no "supports v1.2.3" to write.

Instead, here are facts you can check yourself:

| Item | Value |
| --- | --- |
| **Engine** | Unity **2022.1.24f1** |
| **Game assembly** | `Four Nights at the Burger Shop_Data/Managed/Assembly-CSharp.dll` |
| **Size** | 293888 bytes |
| **SHA256** | `9f2c64db2dc259d4347aeb00ef644507a22aee9d5a3b66328cb27babda9a0bb4` |

```powershell
Get-FileHash "<game folder>\Four Nights at the Burger Shop_Data\Managed\Assembly-CSharp.dll" -Algorithm SHA256
```

The plugin checks this for you: on startup it compares the assembly fingerprint and reports the result
in `BepInEx/LogOutput.log` and in the panel under **系统 → 取证 / 自检**.
It only **reports** a mismatch — it does not block loading, because a different build does not necessarily break anything.

---

## About the plugin's name

After installation the plugin's file under `BepInEx/plugins/` is not an English word but a string of symbols:

```
2×9+9L+6[ED]+7+2×45→1[C]+LS
```

It is a compact notation of **Big Smoke's order** in GTA San Andreas:

> I'll have two number 9s, a number 9 large, a number 6 with extra dip,
> a number 7, two number 45s, one with cheese, and a large soda.

| Line | Notation |
| --- | --- |
| two number 9s | `2` times `9` |
| a number 9 large | `+ 9L` |
| a number 6 with extra dip | `+ 6[ED]` |
| a number 7 | `+ 7` |
| two number 45s | `2` times `45` |
| one with cheese | arrow to `1[C]` |
| and a large soda | `+ LS` |

It is a joke, nothing more. **It does not affect anything** — BepInEx identifies plugins by reflecting on
their `[BepInPlugin]` attribute, so the file name is arbitrary.

The release assets use a different spelling (`18-9L-6_ED-7-45-45_C-LS-*`) because asset names must be
ASCII, and GitHub rewrites single quotes to dots.

---

## Features

- **Numeric panel in eight tabs** — the first six are split by pose (`玩家` / `口交` / `背榨` / `正骑` / `换装` / `系统`), the last two are utilities (`预设` / `关于`)
- **Three independent parameter sets** — the same parameter exists once per pose (`_Fella` / `_Osiri` / `_Sit`); changing one does not affect the others
- **Ecstasy tremor** — a decaying sine wave applied to the ecstasy value, in five profiles; press R to steady it
- **Ecstasy adaptation** — temporary amplitude adjustment after a tremor, strictly restored afterwards, with a safety gate so the drain loop always converges
- **Afterglow** — an extra state after a drain chain ends
- **Binding kiss** — an independent speed control for the sitting pose
- **Manman zone** — a hit area symmetric with the spanking one: raises the drain urge and gives the sitting animation a speed impulse
- **Config presets** — save the entire configuration as a named snapshot and switch back at any time; a timestamped backup is written before every apply
- **Game build check** — compares the game assembly fingerprint on startup

---

## Code signing

All three executables are signed with an **S.M.Y.T.** self-signed certificate, with a DigiCert timestamp.

```powershell
Get-AuthenticodeSignature "18-9L-6_ED-7-45-45_C-LS-Installer.exe" |
    Select-Object Status, @{n='Signer';e={$_.SignerCertificate.Subject}}
```

**This does not remove SmartScreen warnings, and it does not stop antivirus false positives.**
A self-signed certificate cannot be verified on a stranger's machine, and the root cause of the AV
detections is the `winhttp.dll` injection itself — which is what a game cheat looks like.

Use the signature and the SHA256 together: the signature says who signed it, the checksum says whether
what you downloaded is what was published. `SHA256SUMS.txt` is attached to every release.

---

## FAQ

**The installer fails with a `BackgroundWorker` error** — that was a bug in v1.0.0 and v1.0.1; fixed in v1.0.2.
Download the latest release.

**F9 does nothing** — check `BepInEx/LogOutput.log` for "找不到" or "挂载" lines. The panel's
**系统 → 取证 / 自检** tab shows how many patch points were mounted (36 when everything is fine).

**Antivirus flags it** — see the Code signing section. The behaviour genuinely resembles a cheat.

**How do I restore the game to vanilla?** Run the uninstaller, or use `卸载还原.ps1` from the game folder.

**Where is the config?** `BepInEx/config/nesarf.burgershop.modder.cfg`. Older configs are migrated automatically.

---

## Documentation

All documentation is in Chinese. Machine translation works reasonably well on it.

| File | Contents |
| --- | --- |
| [`使用说明.md`](使用说明.md) | Full manual — every parameter explained |
| [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md) | What is still open |
| [`ENGINEERING-NOTES.md`](ENGINEERING-NOTES.md) | Engineering log: symptom / root cause / fix / lesson |
| [`CHANGELOG.md`](CHANGELOG.md) | Per-version changes |

---

## License

See [LICENSE](LICENSE).

This project contains no game assets and is not affiliated with the game's author.
