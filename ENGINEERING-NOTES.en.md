# Engineering notes: the traps we hit and what the reverse-engineering found

> **This is the English edition of the engineering notes.** The Chinese original,
> [ENGINEERING-NOTES.md](ENGINEERING-NOTES.md), is authoritative; this file follows it
> entry for entry, in the same order, with the same `#N` numbers.
>
> This file is a **historical record**, not a to-do list. It records every real problem
> hit during development: **symptom / root cause / fix / lesson**.
>
> **Looking for "what is still unfixed"? → see [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md).**
> **Looking for "why does this feature look like that"? → search this file for a keyword.**

The value of these notes is not "what got fixed" but **why it behaved that way**.
Many of the entries below stand for a reusable rule, for example:

- hooking the **wrong class with Harmony fails silently** (one method name may belong to different classes)
- the game's animations have **two playback paths** (the mixer's `*.State`, and `*.CurrentState` of `Play` on a layer)
- **mouse and touch go through two completely different paths** (a static field vs. a method return value)
- with three copies of the parameters, **which physical key a `set` command lands on depends on the pose at that moment**
- **"_demandMode is true" does not mean "we are currently in Osiri"** — this trap was hit four times

---


## Table of contents

**44** numbered entries in total, plus **5** appendices and **5** S-series entries. The order is **chronological** (in the order they were written), not by importance — it is kept because later entries frequently refer back to earlier conclusions.

`1-40` numbered entries

| # | Title |
| --- | --- |
| 1 | Demand urge accumulated by attacks does not trigger the second and later stacks |
| 2 | The merge path for concurrent config writes was never cleanly verified |
| 3 | The semantics of `EffectiveSpeed` are unconfirmed |
| 4 | `xcheck` invariants do not cover the newly added state |
| 5 | Features with zero real-play verification |
| 6 | Sit mode has no convenient body part for accumulating drain urge by hand |
| 7 | Demand mode in Sit makes the ecstasy value jump abnormally |
| 8 | Sit cannot trigger a chain drain, and therefore cannot trigger the afterglow |
| 9 | The manual "start afterglow" function does nothing |
| 10 | A very long demand mode blocks the seated entry point indefinitely |
| 11 | The chain-drain limit split into four (a feature change, not a defect) |
| 12 | The afterglow can only be triggered by activity inside demand / drain mode (an invariant) |
| 13 | The chain drain gets cut short for no reason (Riding) |
| 14 | Two call warnings (fixed) |
| 15 | The seated pose, once its timer fires, displaces an active demand mode |
| 16 | The seated pose still displaces demand mode (fix #15 was not enough) |
| 17 | The panel edits the copy for the "current pose", not the copy for "the tab you are looking at" |
| 18 | Refresh the duration once all stacks are full (a new feature) |
| 19 | [Experimental] clickable-area highlighting |
| 20 | The Manman zone: the Cubism route is a dead end, switched to "asset binding + hand-dragged rectangle" |
| 21 | The asset binding only listed HitArea*, so no useful asset could actually be picked (completed) |
| 22 | The manual finished (96 parameters + six new concepts) |
| 23 | The remaining 31 parameters also got three pose-specific copies (plus one user-tuning rescue) |
| 24 | HitArea_Manman was only wired to the "touch" path; mouse clicks never went through it |
| 25 | HitArea_Manman cannot impersonate HitArea_Head_Sit (the features would cross) |
| 26 | HitArea_Manman should be symmetric with "spank": raises drain urge and gives the seated action a speed impulse |
| 27 | The seated drain urge reaching full jumps straight to Osiri (three symptoms, one root cause) |
| 28 | There appear to be "two Manman zones" (one built by us, one that looks model-driven) |
| 29 | The binding kiss needs **its own** speed control (entering the kiss != triggering a chain drain) |
| 30 | HitArea_Manman finally got empirical proof (and the diagnostic command was fixed along the way) |
| 31 | HitArea_Manman_By_Plugins: renamed, and its visualisation unified with the other HitAreas |
| 32 | The last three parameters added to the three-copy scheme + entry 18 verified |
| 33 | Entry 7 (seated ecstasy anomaly) changed to "probably fixed" + a recurrence alarm added |
| 34 | `xcheck` invariants extended to the new state (#4) |
| 35 | Seated-pose lock + fellatio items removed from the Riding tab + log tags named after the pose |
| 36 | Seated-pose lock: the exit paths were verified to be "fully covered" |
| 37 | The game got slow: per-frame reflection and vertex scanning (now cached) |
| 38 | The binding-kiss bridging audit + three gaps + "playing directly on the layer" cannot be sped up |
| 39 | Three-tab wording unified: description templates swap the word according to the tab |
| 40 | Drop-tracing logs + three new sections in the manual |
| 41 | UI English: the fallback mechanism and two silent failures |
| 42 | The blind spot of the second English round: the checker sees call sites, not variables |
| 43 | Three blind spots of the translation: concatenation, init order, the regex |
| 44 | Layout: buttons that stretch across the row, and how to watch a shape you cannot see |

Appendices (mechanism descriptions and rules, not defects)

- Appendix: evidence archive
- Appendix: the seated "attack travel" mechanism (reference knowledge, not a defect)
- Appendix: view dragging (a game mechanic, not a defect)
- Appendix: the two-tree sync rules (it took many rounds to work out)
- Appendix: the settings on the GitHub side of the repository

S series (a separate numbering that runs alongside the numbered entries, started later)

| # | Title |
| --- | --- |
| S6 | Panel layout clean-up and decoration-glyph removal |
| S7 | Burger-making countdown: implemented, behaviour never measured |
| S8 | The mode name in the middle of the screen always showed "索取" (fixed) |
| S9 | The Manman hit area can end up entirely off-screen (resolved) |
| S10 | Severity colour coding + the amplitude in (16) is actually large |

---

## 1. Demand urge accumulated by attacks does not trigger the second and later stacks

**Status: fixed (2026-10-07)**

**Symptom**
Demand urge accumulated by the character's **attacks** does not trigger the 2nd and later stacks of demand / drain mode.
The urge keeps rising (measured up to 314%), but the stack count sits at 1 for a long time.

**Root cause**
The stacking check **only exists on the spank path**:

| Path | What it does |
| --- | --- |
| `DoSpankLogic()` (spank) | adds the value → **evaluates the threshold → `EnterOrStackDemand()`** |
| `Postfix_OsiriGirlMainMixer` / `Postfix_SitGirlMainMixer` (attacks) | **only adds the value**, never evaluates the threshold |

So an attack can fill the progress bar, and **only the spank consumes it**. That is deliberate
(the player is meant to control the pace), but what it looks like is "nothing stacks past the 2nd segment".

**Evidence** — `BepInEx/l2d_dump/cmd/watch.csv` (produced by `tools/watch.sh`)

```
t,pose,subState,...,urge,stacks,demandLeft,...
1791378463,Osiri,attacking,...,253.6,1,72.6,...
1791378471,Osiri,kyusei,   ...,275.9,1,65.2,...
1791378474,Osiri,kyusei,   ...,314.1,1,61.6,...
```

That user had `DemandThresholdBase=100` / `Step=49.78`, so the 2nd segment needs 149.8%;
the urge had long passed that and `stacks` stayed at 1.

**Fix (implemented)**

The stacking check was pulled out into a single implementation, `TryStackNow(source)`, **shared by every accumulation source**:

| Source | Accumulates | Stacking check |
| --- | --- | --- |
| Spank `AccumulateAndMaybeStack(..., applyEscapePenalty: true, ...)` | yes | |
| Any click in the seated pose (added in fix #6) | yes | |
| Character attacks (Riding / seated) | yes | **← added this time** |

Switch: `AttackCanStackDemand` (on by default). Turn it off and you are back to the old behaviour where only spanking stacks.

**Verification** (live log from `urgeaudit`):

```
攻击命中 +23.45 → 115.82%
叠加·清零 -115.82 → 0%              ← a stack triggered by a character attack
[索取] 叠加第 6 段（角色攻击），+91.1 秒
[索取] 叠加第 7 段（角色攻击），+113.8 秒
[索取] 叠加第 8 段（角色攻击），+101.3 秒
[索取] 叠加第 9 段（角色攻击），+145.6 秒
[索取] 叠加第 10 段（角色攻击），+82.2 秒
```

---

## 2. The merge path for concurrent config writes was never cleanly verified

**Status: verified (2026-10-07) — one command runs it, `tools/cfgmerge-check.sh`**

**What the script does** — three things:
(1) first wait 8 seconds to confirm **no other writer** is active (if it is not quiet, exit right away rather than give you a false result)
(2) have the two writers change **different keys**: the game side goes through the `set` command (which triggers a full `Config.Save` rewrite),
   while the outside side edits the file directly at the same time
(3) check that **both changes are still there**; if either was overwritten it is a FAIL, and the starting config is restored automatically

**Measured**

```
安静（8 秒内无写入）
当前姿势：正骑（set 会写 _Sit 那一份）
起始：AttackUrgeChance_Sit=20  SpankSpeedGain_Sit=20
结束：AttackUrgeChance_Sit=27  SpankSpeedGain_Sit=29.0
游戏侧的改动保住了
外部侧的改动保住了
结果：PASS —— 两边都没丢，合并路径成立
```

**The script taught me a lesson itself**: the first version printed `FAIL`, and I nearly reported it
as a product defect — what actually happened is that **the test checked the wrong key**: the `set`
command writes the copy for the **current pose** (`PIdx` uses `PoseIdx()`), the pose at that moment
was Riding, and I was verifying `_Osiri`.

> **Any test that goes through `set` must first ask for the current pose, then decide which key
> to verify** — a new trap introduced by the three copies: **one logical name lands on different
> physical keys depending on the pose at that moment.**
> The script now runs `states` first and states in its output which `_XXX` copy `set` will write.

**Usage** (pick a quiet window where you are not tuning anything):

```bash
bash tools/cfgmerge-check.sh
```

The **detection** half of `CfgGuard` (revision number + lock + per-key merge) is verified — the log
does print "检测到外部改动，已按文件合并". But **"并入 N 项" has never appeared**:
in that measurement an outside write of `DemandAttBonus = 777` with an incremented rev was
still overwritten back to 125 after an in-game `set`.

**Why it could not be reproduced cleanly at the time**: during the test the user was live-tuning
through the panel, and their own writes kept overwriting mine.

**Next time, re-test in a quiet period where the user is not touching the config.** Steps to reproduce:
1. `bash tools/cfgsnap.sh save`
2. Note the current value of `DemandAttBonus`
3. Use a script to change `DemandAttBonus` in the file to 777 and bump `<cfg>.rev` by 1
4. In game, `set AttackUrgeJitter <some other value>`
5. See whether `DemandAttBonus` in the cfg kept 777, and whether the log shows "并入 N 项"

---

## 3. The semantics of `EffectiveSpeed` are unconfirmed

**Status: waiting on hands-on judgement**

> **Disclaimer: this issue may not trigger deterministically.**
> In other words it is hard to reproduce reliably and costs more to investigate than an ordinary
> defect — **and one possibility must be kept in mind: the game's own source quality is not high**,
> so the symptom may be something the game produces in a particular state, with the plugin merely
> passing through. Until stable reproduction steps exist, changing the plugin logic for it is not
> advisable.

The climax animation speed goes through Animancer's `EffectiveSpeed`. Setting 800% was measured as **3.38**, not 8.

That may be the property's normal semantics (normalised by clip length), or something else may be
clamping it. **It can only be settled by feel**: if 800% is fast enough, leave it; if it feels slow,
use `Speed` instead (more direct), or set both layers.

---

## 4. `xcheck` invariants do not cover the newly added state

**Status: extended (2026-10-07) — see entry 34**

The 16 invariants `xcheck` has are all early ones (tremor, caps, UI `sizeDelta`).
The newly added state has **constraints between fields but nobody checks them**:

- while `_afterglowRemaining > 0` it should not be inside demand mode
- while `_afterglowPending > 0` the gate `_demandEntered` must be open
- `_chainDemand` / `_chainNormal` should not both be non-zero
- while `_sitDeferred` is true, `centerGirlState` should be `Osiri`
- `_demandStacks` should not exceed `DemandMaxStacks`

This is exactly the class of problem `xcheck` was built to catch.

---

## 5. Features with zero real-play verification

**Status: waiting to be played**

The following paths **have patches hooked but have never appeared in the log** (as of the last census):

| Feature | Log tag |
| --- | --- |
| The whole seated "Drain mode" chain | `[榨取]` |
| "Return accumulation" + holding the seated pose | `[回归]` |
| Demand urge from seated / fellatio ejaculation and absorb | `坐姿吸精` / `坐姿射精` / `口交吸精` / `口交射精` |
| The new afterglow loop (accumulate → fire once settled → hold the pose until cleared) | `[余韵]` |

**`_sitDeferred` is the most suspicious one** — only `Show_CenterGirlSit` has a prefix interceptor;
if the game enters the seated pose by another path, it slips through.

### Careful: there is a trap in using the log to decide "has this ever run"

**The demand-urge source log lines (`骑乘位吸精` / `坐姿射精` / `口交吸精` and similar) are only
written while `urgeaudit` is on** (`AuditUrge` starts with `if (_urgeAuditLeft <= 0) return;`).
Looking at the log after the audit budget is used up will make it look as though "this path never ran".

This has actually happened: `[榨取]` (seated ejaculation does not slow down) appeared once, which
proves `Event_SitSyaseiStart` fired and its postfix ran — yet `NoteUrgeEvent("坐姿射精", ...)` in the
same postfix left no trace at all, because the audit had already switched off.

**To verify these sources, run `urgeaudit 200` before playing.**

### 2026-10-07 census

| Feature | Log tag | Status |
| --- | --- | --- |
| Seated drain mode (seated ejaculation does not slow down) | `[榨取]` | **ran once** |
| Afterglow loop | `[余韵]` | ran 5 times |
| Return accumulation + holding the seated pose | `[回归]` | **still 0** |
| Seated / fellatio ejaculation and absorb accumulation | — | needs `urgeaudit` to judge |

---

## 6. Sit mode has no convenient body part for accumulating drain urge by hand

**Status: fixed (2026-10-07)**

**Symptom**
Rear riding (Osiri) has the **spank** — a big, unambiguous target that accumulates demand urge with
a casual click. The seated pose (Sit) has **no convenient body part** for accumulating drain urge by
hand.

**Root cause — the accumulation conditions differ a lot in strictness**

| | Accumulation condition | Window | Target |
| --- | --- | --- | --- |
| **Osiri** `Osiri叩かれる()` | `osiriState == attacking` | **stable for a long time** (the whole attack stage) | the butt, big and obvious |
| **Sit** `頭叩かれるSit()` | `kissing == 1` | **only 4 to 5 seconds** | the head, and only recognised inside that window |

Original code:

```csharp
public void 頭叩かれるSit()
{
    if (kissing == 0) { return; }                 // 什么都不做
    if (kissing == 2) { SkillCheck...; return; }   // 走技能检定，【不加计数】
    if (kissing == 1) { SitGirlKiss叩く量++; }      // ← 只有这一支才累积
}
```

The `kissing` state machine:

- `0 → 1` is triggered by `PrepareKissing()`, **window length = `KissPrepareTime`** (days 1 to 3: 5 s,
  day 4: 4.5 s, day 5: 4 s; every `KissPrepare解除()` also does `*= 0.9` to shorten it further)
- `1 → 2` is triggered by `StartKISS()` when `拘束キスTimer < 0` → it becomes a skill check, and
  **clicks no longer accumulate at that point**
- the automatic entry normally relies on `Random.value < 0.1f` (10%), which cannot be controlled

**The key hidden entry point: the game has a set of developer hotkeys**

They are bound directly inside `TabemiControl.Update()`:

| Key | Effect |
| --- | --- |
| **Z** | `Show_CenterGirlSit()` — enter the seated pose directly |
| **X** | **`PrepareKissing()` — set `kissing` to 1 directly, opening the accumulation window** |
| **C** | `StartKISS()` — go straight into the skill check (the other C branch is `HPHeal(10)`) |
| **V** | (to be checked) |

**So the actual way to accumulate seated drain urge by hand is: press X first, then click the head
within 4 to 5 seconds.** The game gives no hint of this path at all; it can only be read out of the code.

**Fix (implemented)**

One branch was added in `Postfix_SitHits`:

- the counter **really did increase** (inside the `kissing == 1` window) → run the full logic,
  including the escape penalty (original behaviour unchanged)
- the counter **did not increase** → as long as `SitClickAlwaysAccumulate` is on and
  `sitState == "attacking"`, **only accumulate drain urge and run the stacking check**,
  **without touching the game's own `SitGirlKiss叩く量`**

**The key point**: the game's own counter is left alone, so **the escape mechanic stays exactly as in
vanilla** — the change only lets drain urge be built up by clicking, which gives the seated pose a
manual entry point equivalent to "spank".

Switch: `SitClickAlwaysAccumulate` (on by default).

---

## 7. Demand mode in Sit makes the ecstasy value jump abnormally

**Status: the user asked to record it first, not to change anything yet**

**Symptom**
With demand mode active in the seated pose, the ecstasy value jumps abnormally.

**Live evidence** — `watch.csv` (109 seated samples), the sudden drops that were caught:

```
t=...947  attacking  绝顶=190.781/199.878  段=10  Syaseing=0
t=...951  attacking  绝顶=  8.922/199.878  段=10  Syaseing=0   ← 181.9 lost in one step
t=...954  attacking  绝顶= 10.960/199.878
t=...958  attacking  绝顶=  6.541/199.878
t=...962  attacking  绝顶= 15.543/199.878   ← then it slowly climbs back from the low range
```

Several things worth noting:

- **`Syaseing=0` at the moment of the jump** — the drain did not cause it
- before the jump the value was 190.781, **below the cap of 199.878**, so it is not "hitting the cap"
- the shape after the jump (oscillating between 6 and 25, climbing slowly) **looks very much like
  the reset oscillation running** — and the entry point of that oscillation is `Prefix_EcstasyReset`.
  So most likely **some `EcstasyReset` was triggered → the reset oscillation took over → the value
  was held down**
- `段=10` — demand mode had stacked up to its cap (`DemandMaxStacks`) and stopped there
- `HP` bounces between 199.5 and 287.3 — that is the tremor's HP bonus being applied and reverted, normal

**Full statistics of the retained evidence** (`watch-archive/watch-20261007-211819.csv`, 167 samples)

```
姿势分布          {'Osiri': 34, 'Sit': 133}
子状态分布        {'Osiri/attacking': 30, 'Osiri/kyusei': 4, 'Sit/attacking': 133}
Syaseing 分布     {'0': 162, '1': 5}
绝顶值最大单步跌幅 −195.1   ← 上限是 199.878，等于整条上限被抹掉
索取欲范围        0.0 ~ 1487.2
段数分布          {0:45, 1:23, 2:50, 5:1, 10:48}
```

That −195.1 drop is **very nearly the whole ecstasy bar being zeroed**, which deserves more attention
than "it lost a chunk" — it looks more like **a complete `EcstasyReset(≈1.0)`** than like wear.

**Directions to investigate**

1. **Who called `EcstasyReset`** — add logging inside `Prefix_EcstasyReset` / `Prefix_EcstasyResetNoArg`
   (printing the source and the call stack) to confirm whether the game called it itself or the demand
   mode logic triggered it
2. **Should the reset oscillation take over during demand mode** — right now it only blocks `Syaseing`,
   not `_demandMode`; during demand mode the ecstasy value is supposed to be governed by the chain-drain
   soft cap, and the two may be fighting each other
3. **`段=10` not moving for a long time** — same origin as entry 1 (only spanking stacks), but here it
   is already full

---

## 8. Sit cannot trigger a chain drain, and therefore cannot trigger the afterglow

**Status: fixed (2026-10-07)**

**Symptom**
In the seated pose the chain drain never gets going, so naturally the afterglow can never trigger either.

**Live evidence**

In `watch.csv`, **the `Syaseing` column is 0 for all 133 seated samples**, and **the sub-state column
is always `attacking`** — it never once switched to `kyusei` or `syasei`. The afterglow column is 0
throughout as well.

For comparison: in the same dataset Osiri has 4 `kyusei` samples and `Syaseing=1` appears 5 times.

**This is decisive**: it is not that "the chain drain will not start" in Sit — it is that **the step
before it (entering drain) is never even reached**.

**User addition (consistent with the code)**: "the chain-drain effect in sit seems to appear only in
the [binding kiss] state"

At the code level that is exactly right — seated drain has only one entry point, and it **forces the
character into the kissing state**:

```csharp
public void Play_SitKiss吸精(bool startFrom0)
{
    tabemi.sitState = SitState.kyusei;   // 进榨取
    tabemi.kissing  = 2;                 // ← 同时强制进【束缚之吻】
    model.MouthParts(4);
    _CenterGirlLayer.Play(SitKiss吸精[num]);
    ...
}
```

So **seated chain drain is exclusive to the binding kiss**; it is not something you can get "any time
while seated".

**Root cause (inferred, to be verified)**
The path into seated drain is held back by the `kissing` state machine:

```
Event_SitKissSyaseiOnEnd()
{
    if (gameover)          Play_SitKiss吸精(true);
    else if (Random < 0.5) Play_SitKiss();          // 回坐姿攻击
    else                   Play_SitKiss吸精(true);   // 50% 进榨取
}
```

In other words **seated drain can only be entered through `Play_SitKiss吸精`**, whose prerequisite is
**`kissing == 2` (the kissing state)** — and `kissing` only gets there through

- `0 → 1`: `PrepareKissing()`, either the 10% roll or **pressing X**
- `1 → 2`: `StartKISS()`, either `拘束キスTimer < 0` or **pressing C**

This is the same root as entry 6: **the seated manual entry point both requires a key press and only
works inside a narrow window**, so in real play the character almost never reaches the kissing state,
and the whole chain of drain → chain drain → afterglow stays out of reach.

**Another point that is hooked but may never get a chance to run**
Both branches of the original `Even_SitKiss吸精OnEnd` are `Play_SitKiss吸精(true)` (it replays forever);
a prefix was added to take it over so `KyuseiMaxChain` applies. **But since drain cannot be entered at
all, that takeover has never been triggered.**

**Directions to investigate**

1. **Confirm the reach rate of `kissing` in real play** — add logging to `PrepareKissing` / `StartKISS`
   to see how many times it is entered per session
2. **If it really is unreachable**, consider letting seated drain also be entered from a non-kissing path
   (for example `Event_SitSyasei_OnEnd` currently always calls `Play_SitKiss()`; it could get a small
   chance to enter drain)
3. Or follow the direction of entry 6 and loosen the manual entry point, so the player can push the
   state into the kissing state on purpose

---

## Appendix: evidence archive

The output of the continuous poller (`tools/watch.sh`) has been copied out of
`BepInEx/l2d_dump/cmd/` into `watch-archive/`, so the game or BepInEx cannot wipe it:

```
watch-archive/watch-<时间戳>.csv   compact timeline, directly analysable with any csv tool
watch-archive/watch-<时间戳>.txt   full state blocks, including the raw text of every round
```

**To reproduce or compare something later, check here first for an archive covering that window.**

---

## Appendix: the mechanism behind the seated "attack travel" (reference knowledge, not a defect)

User observation: "sit has an 'attack travel' visual, i.e. while riding it moves up and down over a maximum length,
but there are some 'points' in the middle of that distance that make the height before each sit go down different."

**The code matches this exactly.** The travel is one axis of a 2D mix:

```csharp
Vector2 parameter = new Vector2(sitSpeed + sitSpeedPlus, sit_HeadZ);
SitMixerA_HeadZ.State.Parameter = parameter;   // 横轴 = 速度，纵轴 = 行程深度
```

Every frame, `Update_SitMixers()` aligns the `NormalizedTime` of the four mixers
`SitMixerA/B/KissA/KissB_HeadZ` with the main mixer and feeds them the same `(speed, sit_HeadZ)`.

**How the travel changes** — that is exactly the "points in the middle" you described:

```csharp
headZ判定Timer += headZ判定interval;
if (UnityEngine.Random.value < 0.1f)                     // 10% 概率
{
    float targetDiff = UnityEngine.Random.Range(-1, 2);   // ← 只有 -1 / 0 / +1
    StartCoroutine(SitHeadZCoroutine(targetDiff, 0.5f));  // ← 0.5 秒平滑过渡
}
```

| Item | Value |
| --- | --- |
| Values of `sit_HeadZ` | **{−1, 0, +1}** — three detents |
| Chance of changing detent | one roll per `headZ判定interval`, **10%** |
| Transition duration | **0.5 s**, smoothed with `Mathf.Lerp` |
| Type | `private float sit_HeadZ` (`Live2D_Animation_SitOsiri`) |

**So "the height before each sit go down can differ" is because the detent changed** — three discrete detents,
a random switch, and a smooth transition.

**What this means for the mod**: what I touch today as "seated attack speed" only touches the
**horizontal axis** (`sitSpeed`). **The vertical axis `sit_HeadZ` is a separate dimension and is currently
untouched** — if we ever want "longer travel / shorter travel / pin it to one detent", the hook point is here
(change `sit_HeadZ` after `Update_SitMixers`, or intercept that 10% detent change).

---

## 9. The manual "enter afterglow" feature did nothing

**Status: fixed (2026-10-07)**

**Symptom**
The `demand afterglow` command / the panel's "enter afterglow manually" button did nothing when clicked —
the send actually succeeded (the banner even popped up), but **the character never started chain absorbing for real**.

**Root cause — the manual command sets a different variable from the one the ignition logic reads**

| | Variable |
| --- | --- |
| The `demand afterglow` manual command | sets `_afterglowRemaining = N` **and zeroes `_afterglowPending`** |
| `TickAfterglowSettle` (the only place that "starts the first drain") | **its very first line is `if (_afterglowPending <= 0) return;`** |

So the manual afterglow set a **counter nobody consumed**:

```
manual sets _afterglowRemaining = N
  → consuming it requires the drain wrap-up event (Event_Osiri吸精End / Even_SitKiss吸精OnEnd)
    → that requires a drain to exist first, and only TickAfterglowSettle starts the first drain
      → TickAfterglowSettle is blocked by _afterglowPending <= 0
        → dead loop; the counter just sits there
```

**This explains why the old "enter afterglow manually" looked "effective"** (the banner popped, and the afterglow
count in `status` was right) **while the character did not react at all** — the banner only reflects that counter,
it does not mean a drain ever started.

**Fix**
Make the manual command take **the same path** as the automatic afterglow:

```csharp
_afterglowPending   = UnityEngine.Random.Range(lo, hi + 1);   // 交给点火逻辑
_afterglowRemaining = 0;
_afterglowSettleAt  = -999f;                                   // 立即重新计稳定时间
```

**Verification**

```
已手动进入余韵：待发动 17 次（局面稳定后自动开第一次榨取）
[余韵] 最后一段已结束且局面稳定 → 发动余韵 17 次
状态: [Osiri·kyusei]        ← 榨取真的跑起来了
```

---

## 10. Demand mode running too long blocks the seated entry indefinitely

**Status: fixed (2026-10-07)**

**User description**
"Sometimes demand lasts so long that when the window where sit can appear arrives I can never get into sit,
so I can manually trigger the afterglow to force demand mode to end and clear demand urge,
and the low demand-urge growth during the afterglow also prevents demand from continuing right after the afterglow ends"

**Root cause (one layer deeper than "manual afterglow")**

Two gates block the seated entry, and the second one is the real problem:

| Gate | Condition | Assessment |
| --- | --- | --- |
| "No pose change until the afterglow is cleared" | `_afterglowRemaining > 0` | Released naturally when the afterglow finishes; fine |
| **The "return accumulation" seated hold** | `_sitDeferred && centerGirlState == "Osiri"` blocks the whole time | **Demand mode keeps the character in Osiri for a long time, so the release condition never becomes true, and it is blocked indefinitely** |

`_sitDeferred` was only ever cleared on "riding ends", and under a long demand that moment never arrives.

**Fix (three things)**

1. **Manual afterglow = force-exit demand mode** (escape valve)
   `demand afterglow` now **clears every demand field**:
   `_demandMode / _demandStacks / _demandUntil / _demandUrge / _chainDemand` are all zeroed together.

2. **A timeout guard on the seated hold** (fixes the root cause)
   New `SitDeferTimeout` (default 45 s) — past that, release unconditionally,
   no longer depending on "riding ends", which may never come.

3. **Low demand-urge growth during the afterglow + clearing when it ends**
   - `AfterglowUrgeScale` (default 20%): during the afterglow, demand-urge growth from every source is
     multiplied by this factor (spanks / seated taps / attacks / ejaculation-and-absorb all go through the
     same `UrgeScaleNow()`). The afterglow itself chain-drains a dozen times and grows on every round; without
     damping it, **demand mode starts again the moment the afterglow ends**, and the escape valve was wasted.
   - `AfterglowClearsUrge` (default on): when the afterglow burns out, demand urge goes to zero and
     `_demandEntered` drops its gate, leaving a clean gap afterwards.

**Verification**

```
[索取模式中]  索取模式中 1 段 / 共剩余 154.5s   索取欲=2…
[手动余韵后]  [Osiri·kyusei]  不在索取模式   索取欲=1.4%    ← 强制清干净
              已手动进入余韵：待发动 43 次（局面稳定后自动开第一次榨取）
```

---

## 11. The chain limit split into four sets (feature change, not a defect)

**Status: implemented (2026-10-07)**

**User request**
"The 'chain limit' in the Player tab needs four sliders to separate osiri normal, osiri demand, sit normal and sit drain"

**Before**: there was only one `KyuseiMaxChain`, and two poses times two modes **shared one limit and one count** —
tuning one affected the other three scenarios.

**Now**: four independent limits and counts.

| Config key | Scenario | Panel label |
| --- | --- | --- |
| `ChainMaxOsiriNormal` | Riding · normal | 骑乘位·常规 |
| `ChainMaxOsiriDemand` | Riding · demand mode | 骑乘位·索取模式 |
| `ChainMaxSitNormal` | Seated · normal | 坐姿·常规 |
| `ChainMaxSitDemand` | Seated · drain mode | 坐姿·榨取模式 |

The panel puts them under the "absorb (drain)" section of the Player tab, and shows the current scenario live:
`当前场景：坐姿·榨取模式，上限 16，已连 3`

**Implementation**

```csharp
internal static bool PoseIsSit()      // Sit 与 Osiri 在同一个类里，从 centerGirlState 判

private static int ChainNow()         // 姿势 × 模式 → 取对应那个计数
private static void ChainSet(int v)
private static int ChainLimitNow()    // 姿势 × 模式 → 取对应那个上限
internal static string ChainSlotName()  // "坐姿·榨取模式" 这类标签
```

`KyuseiMaxChain` **is deprecated** (kept in the config only for compatibility with old files; the logic no longer reads it).

**The user's measured values** (showing the four sets really are independent):

```
ChainMaxOsiriNormal = 2
ChainMaxOsiriDemand = 12
ChainMaxSitNormal   = 2
ChainMaxSitDemand   = 16
```

---

## 12. The afterglow can only be triggered by activity inside demand / drain mode (invariant)

**Status: confirmed to hold + written into the code comments (2026-10-07)**

**User request**
"Nothing in the normal state should ever trigger the afterglow"

**Current-state check: this already holds.**

The only entry point for afterglow accumulation, `NoteDemandSyasei()`, starts with the gate:

```csharp
private static void NoteDemandSyasei(string pose)
{
    if (!_demandMode) return;      // ← 常规状态下直接返回
    ...
}
```

**`_afterglowPending` has exactly four write sites**:

1. `NoteDemandSyasei` (ejaculation inside the mode, gated)
2. `EnterOrStackDemand`'s "take back the previous round's unfinished afterglow" (reachable only when entering or stacking the mode)
3. the manual `demand afterglow` command (**deliberately** bypasses the gate, for testing and as an escape valve)
4. `defaults` reset

**None of them lets activity in the normal state trigger the afterglow.**

**Verification**: twelve seconds of play in the normal state, afterglow accumulation count in the log = **0**

**A gap fixed along the way**: `NoteDemandSyasei` originally had only **two** call sites hooked (seated, riding),
**and the fellatio ejaculation was missing**. Added to `Postfix_FellaSyaseiStart`.

**This rule is now written into the doc comment of `NoteDemandSyasei`**, together with the four write sites,
with a note saying "think twice before adding a fifth" — so it cannot be broken by accident later.

---

## 13. The chain drain cutting out early for no reason (Riding)

**Status: fixed (2026-10-07)**

**Symptom**: while riding, the chain drain stops without warning.

**Root cause**: the chain detent (normal / demand-drain) used to be decided by `_demandMode` alone:

```csharp
if (_demandMode) return sit ? _chainSitDemand : _chainOsiriDemand;
return sit ? _chainSitNormal : _chainOsiriNormal;
```

But **Riding's drain enters through `Play_SitKiss吸精()` — which forces `kissing` to 2 without setting `_demandMode`**.

So a Riding chain fell into the **"normal" detent** and was cut short by `ChainMaxSitNormal` (measured value **2**).

I had stepped on the same trap one round earlier (#12's `ModeActive()`):
**anywhere that decides "in the mode" from `_demandMode` alone does not hold for Riding.**

**Fix**: new `DrainSlotIsDemand()`, and all four detent lookups
(`ChainNow()` / `ChainSet()` / `ChainLimitNow()` / `ChainSlotName()`) now use it.

```csharp
private static bool DrainSlotIsDemand()
{
    if (_demandMode) return true;
    if (PoseIdx() != 2) return false;
    if (sitState == "kyusei") return true;   // 正骑·榨取中
    if (kissing > 0.5f) return true;         // 正骑·束缚之吻中
    return false;
}
```

**Lesson (worth its own line)**:
**`_demandMode` cannot be used as the test for "this mechanic is currently running".** Riding's drain is
independent of it. Whenever the question is "is this mechanic running right now", use `ModeActive()` /
`DrainSlotIsDemand()`.

---

## 14. Two invocation warnings (fixed: stale Unity reference + swallowed inner exception)

**Status: fixed (2026-10-13)**

### Symptom

Occasionally in the log:

```
[Warning] 调用 HPGaugeChangePercent 失败：Exception has been thrown by the target of an invocation.
[Warning] 调用 EcstasyGaugeChangePercent 失败：Exception has been thrown by the target of an invocation.
```

Thrown when the plugin reflectively calls a game method, **with no observable consequence**, so it stayed on
the list as low priority.

### Root cause (located 2026-10-13)

The first line of `InvokeFloat` is `if (obj == null) return;`, and the type of `obj` is **`object`** — which
uses **reference equality** and bypasses the `==` overload of `UnityEngine.Object`.

Once a Unity object is destroyed, the C# reference **is not null**. So a `player` cached before a scene change
passes that check and then blows up inside `Invoke`.

And `TargetInvocationException` wraps the real cause, while the log only recorded `e.Message` (the outer line
"Exception has been thrown by the target of an invocation.") — **which says nothing**. That is why this issue
sat for so long: not hard to fix, **invisible**.

The original guess — "called at the wrong moment (UI slot not initialised / not in the shop scene)" — pointed in
the right direction, but the actual mechanism is a **stale reference**, not timing.

### Fix

1. Add a pre-check: `obj is UnityEngine.Object uo && uo == null` — judged with Unity's overload, so destroyed
   objects are caught right here
2. Dig the **inner exception** out of `TargetInvocationException` and log it (type + message + stack), together
   with the target object's type and the arguments — the next failure of any kind will state its own cause
3. Log each method in detail only once, so a method called every frame cannot flood the log

**Corroboration**: the codebase already used this pattern elsewhere (the `_gaugeHolderCache` spot), which means
the trap was known — `InvokeFloat` had simply missed it.

### What could not be verified

These two warnings are **intermittent**. One run after the fix produced no occurrence —
**but that does not prove it is fixed**; one run without a repro is not the same as never again.

What can be stated: the pre-check is in effect and the normal call path is intact
(`set TargetHp` / `set TargetEcstasy` still take effect in practice).

Settling it properly needs another stretch of observation during play with frequent scene changes.

## 15. The seated appearance displaces a demand mode that is already running

**Status: fixed (2026-10-07); the seated timeout force-release was also **removed** on request**

**The user's words**
"Delete the force-release function for sit, and osiri's demand mode is displaced by sit at the 1h point where I set sit to appear"

**Symptom**
Demand mode is running; when the seated appearance time arrives (set by the user to 1 hour), the character is
forced into the seated pose and the mode is interrupted.

**Root cause**
`Prefix_ShowCenterGirlSit` only blocked two cases at the time:

| Existing gate | Condition |
| --- | --- |
| No pose change until the afterglow is cleared | `_afterglowRemaining > 0` |
| The return-accumulation hold | `_sitDeferred` — **and it is only set at the instant "entering riding while the seated pose happens to be pending"** |

**There was no "no pose change while the mode is active" gate** — so the seated appearance walked straight in.

**Another contributing factor**: the `SitDeferTimeout` I had added earlier (45 s timeout, force release) was meant
to keep the seated entry from being blocked indefinitely, but it **actively interrupts the mode**, which
contradicts the design goal "the mode is not interrupted". **The user asked for it to be removed; it is removed.**

**Fix**

```csharp
if (_demandMode)
{
    Log.LogInfo("[索取] " + ModeWord() + "模式进行中（" + _demandStacks + " 段 / 剩余 …s）"
                + " → 不许坐姿顶进来。要退出请用「手动进入余韵」");
    return false;
}
```

**No timeout of any kind.** To leave demand mode voluntarily, use the "enter afterglow manually" escape valve
(see entry 10: it force-clears the mode state).

The `SitDeferTimeout` config key is kept but **disabled** (only for compatibility with old files), with the reason
noted in its description.

**Settled design**:

| What you want | How to get it |
| --- | --- |
| Let the seated pose in | wait for the mode to end naturally, or force-clear it with "enter afterglow manually" |
| Change pose while the mode is active | **not allowed** — this is the design, not a defect |

---

## 16. The seated pose still displaces demand mode (fix #15 was not enough)

**Status: state-level fallback added, waiting on real-play verification**

> **Disclaimer: this issue may not trigger deterministically.**
> In other words it is hard to reproduce reliably and costs more to investigate than an ordinary defect —
> **and one possibility must be kept in mind: the game's own source quality is not high**, so the symptom may be
> something the game produces in a particular state, with the plugin merely passing through.
> Until stable reproduction steps exist, changing the plugin logic for it is not advisable.

**Symptom**: after fix #15 (an `if (_demandMode) return false;` at the entry point), **the seated pose can still
take over**.

**Why the entry point cannot stop it — the state change is a delayed coroutine**

```csharp
private IEnumerator _CenterGirl(int i)
{
    yield return new WaitForSeconds(effect.GlitchEffect(0.1f, 0.01f, 0.1f));   // ← 先等 0.1 秒
    model.CenterGirlParts(i);
    switch (i)
    {
    case 0: centerGirlState = CenterGirlState.Fella; break;
    case 1: centerGirlState = CenterGirlState.Sit;   break;   // ← 之后才真正切
    case 2: centerGirlState = CenterGirlState.Osiri; break;
    }
}
```

`Show_CenterGirlSit()` does `StartCoroutine(_CenterGirl(1))`. **If it was already called and the coroutine already
queued before we intercepted, then 0.1 s later it switches the state to Sit anyway** — a prefix on the entry point
cannot stop a coroutine that is already queued.

**The fix: a state-level fallback**

In `TickDemand()`, check every frame:

```csharp
if (_demandMode && centerGirlState == "Sit")
{
    Log.LogInfo("…模式进行中，状态却被切成坐姿 → 夺回骑乘位");
    ShowCenterGirlOsiri();      // 夺回来
}
```

**Why "correcting" is appropriate here**: the project has a rule that says "do not auto-correct unless the abnormal
state can be told apart from a normal transient". Here **`_demandMode && state == Sit` is a combination that can
never be legal** — it can be judged with certainty rather than guessed at, so acting on it is right.

**Call paths** (all three go through `Show_CenterGirlSit`, so the entry prefix catches them):

| Line | Source |
| --- | --- |
| 2201 | story script |
| 2245 | developer hotkey **Z** |
| 10714 | seated-appearance timer (the 10% roll after `SitGirlStartTimer` expires) |

**But what actually takes effect is that delayed coroutine**, so both layers are needed: the entry point and the
state check.

**Verification note**: this fallback **has not been verified in a real scenario** — you have to play until the
seated pose appears. If "夺回骑乘位" appears in the log, the fallback worked.

---

## 17. The panel was editing the "current pose" copy, not the tab you were looking at

**Status: fixed (2026-10-07)**

**Symptom (found by myself)**
After the three copies (`_Fella` / `_Osiri` / `_Sit`) existed, **the panel's sliders read and wrote by "the actual
current pose", not by the tab you were looking at.**

So: **you are in the riding pose, you move a slider on the Fellatio tab — and you are actually changing the riding
copy.** That directly broke the per-tab independence the user had explicitly asked for.

**Root cause**

```csharp
private static int PIdx { get { int i = PoseIdx(); ... } }   // ← 只看实际姿势
private static float P3(ConfigEntry<float>[] a) { return a[PIdx].Value; }
private static void SetP3(...) { a[PIdx].Value = v; }
```

`P3` / `SetP3` are used by both the **game logic** and the **panel**, and the two need different indices:

| Caller | Which index it needs |
| --- | --- |
| Game logic (runs every second) | **the actual pose** `PoseIdx()` |
| Panel sliders | **the tab being looked at** |

**Fix: add `_editPose`**

```csharp
internal static int _editPose = -1;      // -1 = 不在编辑（走实际姿势）

private static int PIdx
{
    get
    {
        if (_editPose >= 0 && _editPose <= 2) return _editPose;   // 面板编辑中
        int i = PoseIdx();                                        // 游戏逻辑
        return i < 0 ? 1 : (i > 2 ? 2 : i);
    }
}
```

- `DrawTabFella` → `_editPose = 0`
- `DrawTabOsiri` → `_editPose = 1`
- `DrawTabSit`   → `_editPose = 2`
- start of `OnGUI` → `_editPose = -1` (reset every frame, so the game logic's reads are unaffected)

**Lesson**: when an accessor that resolves an index from "the current context" is shared by two contexts,
**the two must be told apart explicitly** — otherwise the panel looks like it works while writing to the wrong
object, and nothing ever raises an error.

---

## 18. Refresh the duration once all segments are filled (feature addition)

**Status: implemented (2026-10-07)**

**User request**
"After the urge tier is full, stacking tiers is no longer possible, but when the accumulation condition for
'stacking' is met, the duration of the state can be refreshed"

**Before**: when the segment count was full, `EnterOrStackDemand` logged one line and did nothing — after that,
whatever the player accumulated, the mode just quietly ran down, **and the accumulation was wasted**.

**Now**: when full, no new segment is added, but **one segment's duration, scaled by `DemandMaxStackRefreshMul`
(default 100%), is added to the total**, and the urge is zeroed (spending that accumulation). 0 = the old behaviour.

**Verification**

```
已达 2 段上限 → 不加层，但刷新持续时间 +53.8 秒 → 剩余 257.6 秒
已达 2 段上限 → 不加层，但刷新持续时间 +83.3 秒 → 剩余 340.9 秒
已达 2 段上限 → 不加层，但刷新持续时间 +73.4 秒 → 剩余 414.3 秒
已达 2 段上限 → 不加层，但刷新持续时间 +47.8 秒 → 剩余 462 秒
```

The segment count stays at 2 while the time keeps refreshing.

---

## 19. [experimental] clickable-area highlighting

**Status: implemented (2026-10-07)**

**User request**
"Give each mode a button for an experimental area highlight of its clickable areas"

**Approach**: no per-area hardcoding — instead **walk every drawable in the model whose name starts with
`HitArea`**, compute its screen bounding box, and draw a frame plus a label. **All three modes are covered at
once**, and a new area added by the game is picked up automatically.

- Switch: `HighlightHitAreas` (default off, experimental)
- The three tabs (Fellatio / Rear / Riding) each have a button that **share the same switch**
- It also draws the **pussy area the plugin builds itself** (labelled "小穴区(插件自建)"), because that one is not
  a drawable in the model
- Drawn the same way as the chain-drain banner, **unaffected by the panel being shown or hidden**

**A trap when reading drawable names**: field names differ across Cubism versions, so `DrawableName()` tries
`Name` / `name` / `Id` in turn (both properties and fields) — the same handling as the earlier "animation layer
member names differ per version" problem.

---

## 20. The Manman zone: the Cubism route is a dead end, so two legs instead — asset binding plus hand dragging

**Status: implemented (2026-10-07)**

**The direction the user proposed**
"This involves Live2D's asset-to-model binding, and then Cubism becomes usable again"

**Precondition checks (three, and the results decided the approach)**

| Check | Result |
| --- | --- |
| Cubism Editor | **installed** (`D:\Live2D Cubism 5.1`) |
| The game's `.cmo3` source project | **absent** — only the compiled `.moc3` exists |
| Can Cubism Editor open a `.moc3` | **no** — the editor only reads `.cmo3`, which is the whole point of "compiled" |

**Conclusion: adding a real `HitArea_Manman` drawable to the model is not possible.** The drawable set, vertex
data and draw order are all baked into the moc3 and **cannot be added at runtime**; and without a `.cmo3` the moc3
cannot be modified.

**Confirmed along the way**: the model has **five `HitArea*` assets in total** (listed by the highlight feature in
practice): `HitArea_Head` / `HitArea_LeftGirl` / `HitArea_RightGirl` / `HitArea_Head_Sit` / `HitArea_Osiri` —
**there is no Manman / Manko** (the `HitArea_Manman` string in the code is a leftover; it cannot be found in the
assets).

**So: two legs instead**

1. **Asset binding (preferred)** — put a drawable name into `ManmanBindDrawable` and the hit zone uses **that
   asset's bounding box**. This is real asset-to-model binding: it follows the mesh deformation, so it fits
   however the character moves. The panel lists the five `HitArea*` assets as buttons; one click binds.

2. **Hand-dragged rectangle (fallback)** — with nothing bound, use `CX/CY/W/H` (model-relative proportions).
   New **drag mode** `ManmanDragMode`: while it is on, the hit-zone centre follows the mouse live; once it is
   in place, click "bind to this position" and the centre is fixed there.

**A coordinate-conversion bug fixed along the way**

"Set the area centre to the current mouse position" used to write **screen proportions** (`mp.x / Screen.width`),
while the hit zone had long since become **model-relative** — the two were not in the same coordinate space, so
**the button worked and the position was completely wrong**. New `ScreenToModelRel()` does the conversion, and
both dragging and binding go through it.

---

## 21. Drawable binding listed only HitArea*, so nothing useful could be selected (completed)

**Status: fixed (2026-10-07)**

**The user asked**: "is the drawable binding still unfinished?"

**A key piece was indeed missing**: the panel listed only the five `HitArea*` drawables,
and those five **are themselves the clickable areas** — binding to them is the same as not
binding at all, which makes no sense.

**What was actually needed was binding to a non-HitArea art asset** (for example the body/skin
texture over the crotch area), and at that time **it could not be selected at all**.

**What was added**

| Change | Description |
| --- | --- |
| `AllDrawableNames(filter)` | lists **every** drawable in the model (substring filter, case-insensitive) |
| `ManmanDrawableFilter` | filter box (type directly in the panel) |
| `ManmanDrawablePage` | page number, 40 per page (a model has many drawables, so paging is required) |
| Shortcut buttons | `HitArea 筛选` / `清空筛选` |
| Current-item marker | the bound one shows `[记录]`; clicking it again unbinds |

**Also fixed the `ManmanBindDrawable` branch of `set`** — it used to read
`if (!isNum && name == "ManmanBindDrawable") { ...; break; } break;`. That `name ==` check is
always true inside `switch (name)`, which is a roundabout way of writing it; it is straight now.

**Verified part**: the underlying mechanism works — the "clickable-area highlighting" feature
(which uses the same `DrawableName()` + `TryGetDrawableScreenRect()`) was measured drawing the
five HitArea boxes exactly on the character (see the `HitArea Osiri` screenshot entry).
So what the drawable binding lacked was **the range of the list**, not the mechanism.

**Not yet verified**: whether, after binding to a non-HitArea asset, the judgement area really
tracks that asset. That needs an actual test with an asset selected in the Riding tab.

---

## 22. Manual completed (96 parameters + six new concepts)

**Status: done (2026-10-07)**

**The state before**

| Check | Result |
| --- | --- |
| This family's parameters | 141, **96 not mentioned** |
| "Rear" / "Riding" | **0 occurrences each** |
| "binding kiss" | **0 occurrences** |
| "Manman" | **0 occurrences** |
| "Absorb" | **0 occurrences** |
| "Highlight" | **0 occurrences** |

The manual was stuck in the era when "the panel only had an 'Opponent' tab".

**What was added**

| Section added / rewritten | Contents |
| --- | --- |
| **The panel's six tabs** | the six-tab structure plus the wording of each of the three pose tabs (Absorb / Demand / Drain) |
| **Three copies, one per pose** | the three-copy mechanism, the `_Fella/_Osiri/_Sit` suffixes, the `_editPose` trap, copying, which are already independent and which are not |
| **Trigger chain** | the four steps, `TryStackNow` as the sole judgement, the two trigger modes, the four accumulation sources, the mean-value convention, refresh when full |
| **Effects during the mode** | duration / segments / attack power / animation rate / staged speed-up / no fellatio return / cannot be interrupted |
| **Attack power scales with speed** | the mild-exponential curve formula and comparison table (k=0/2/4) |
| **Climax animation rate** | why it has to be handled separately (including the original code), why the criterion is "is the main mixer playing" |
| **Chain drain** | the five caps, scaling by HP, soft cap and wobble, the `_demandMode` counter-example |
| **Return to fellatio and return accumulation** | the three-way branch in the original code, threshold reduction, entering immediately, holding the seated request |
| **Afterglow** | accumulation / firing / gate / speed and wobble / independent soft cap / survival valve |
| **Binding kiss** | why Riding's chain drain must go through it, the four parameters, slider binding |
| **Manman zone** | why Cubism does not work, model-relative judgement, drawable binding and manual dragging as the two routes |
| **Clickable-area highlighting** | how to walk the HitArea* drawables, and what it is for |
| **Impact sounds** | animation events drive the rhythm, speed picks the timbre, four slots, overlap prevention, no repeat of the same sound |
| **Spank impulse** | the five parameters, and why the point of application is animation rate rather than `osiriSpeed` |
| **Two settings specific to seated** | `SitClickAlwaysAccumulate` / `SitSyaseiToDrainChance` |
| **Attack power and speed** | `PowerUnlock` / `TabemiPowerMul` / `FellaSpeedPlus` / `SitSpeedPlus` |
| **The six Manman zone settings** | including the reminder that "CX/CY/W/H are model-relative ratios" |
| **Deprecated / disabled settings** | `KyuseiMaxChain` / `ChainGainCap` / `DemandUrgeCap` / `SitDeferTimeout` / `DemandAfterglowMin·Max` |
| **Log and message quick reference** | what each of 11 keywords means, plus the forensics tools |

**Result**

```
manual: 1032 lines -> 1339 lines
this family's coverage: 96 of 141 unmentioned -> 8
```

Those last 8 are **mentioned as slash combinations** (`AfterglowSpeedWobble` / `Hz`,
`AttackUrgeChance` / `Gain` / `Jitter` and the like). **Functionally they are fully covered**;
only literal matching cannot see them.

**Method note**: two script passes replaced and inserted by exact line number, and the
"parameter coverage" check was run after each one to confirm the effect — writing it was not
taken as done.

---

## 23. The last 31 parameters also became three copies per pose (plus one user-tuning rescue)

**Status: done (2026-10-07)**

**What was done**

The last 31 parameters (7 demand-urge sources / 5 spank impulse / 5 chain drain /
4 return accumulation / 10 afterglow) were migrated onto the `BindP3` / `P3` / `SetP3` system.

**That makes 51 parameters across 3 poses, 153 keys.**

**Four traps in the migration (it took three script passes to get through)**

| # | Trap | Fix |
| --- | --- | --- |
| 1 | the description is wrapped in `new ConfigDescription(...)` and was passed to `BindP3` as a `string` → type mismatch | take only its **first string literal** |
| 2 | `ChainSoftCap` (default `60f`) was misjudged as an integer by a "has a decimal point" heuristic | **type comes only from the generic argument of `Config.Bind<T>`** |
| 3 | the boolean parameters' `Config.Bind` **had no `<bool>`**, so they were treated as floats | fall back on whether the default value is `true`/`false` |
| 4 | read-site replacement turned the panel's `X.Value = SliderF(...)` into `P3(X3) = ...` (assigning to a method call) | run one more sweep `P3(x) = v;` → `SetP3(x, v);` (**77 sites**) |

Trap 4 is the same problem batch 1 hit — **doing read replacement before panel replacement
necessarily produces the intermediate form `P3(x) = v`**, so "read replacement + write fixup"
should be one fixed action and never skipped.

**Note: clarification (to avoid misreading)**

This migration was **done by hand** — the game was closed, the config file edited directly, and
the old key values copied into the three copies. **There is no automatic migration system in the
code.**

So the sentence in `使用说明.md` that says "old single-key values **are not moved over
automatically**" is **correct**. The two are not contradictory, but they are easy to misread,
hence this note.

**Which is also the todo item**: the proper approach is a versioned migration system
(`ConfigVersion` → detect an old version → migrate → back up automatically → write a log telling
the user what was moved). **Not implemented yet.**

---

**And one user-tuning rescue on the side**

New keys are created with **the code's default values**, while the values the user tuned live in
**the old single keys**. With the game closed, the config file was edited directly and the old
values copied into the three copies:

```
migrated 93 keys, of which 【48 values had actually been tuned】:
  AttackUrgeChance      8 → 20
  AttackUrgeGain        3 → 4
  SpankSpeedGain       12 → 20
  SpankSpeedStack      60 → 80
  SpankSpeedDecay    0.25 → 0.05
  OsiriReturnTrigger   30 → 15
  …
```

**Without this step those 48 values would have fallen back to defaults silently** — and the user
tunes while playing, so nothing would have reported an error. (This is exactly the practical
scene behind "remember to keep the values the user set".)

**Verification**

```
35 hooks mounted, no exceptions
set AttackUrgeChance 33 (character in fellatio)
  AttackUrgeChance_Fella = 33   <- only this copy moves
  AttackUrgeChance_Osiri = 20
  AttackUrgeChance_Sit   = 20
153 keys across the three copies
```

**Known side effect**: during the migration **multi-line concatenated description strings were
truncated** (only the first literal was kept). Those parameters' descriptions read shorter in the
config file; **functionality is unaffected**.

---

## 24.  HitArea_Manman only hooked the touch path; mouse clicks never went through it

**Status: fixed (2026-10-07) — the user reminded me to "confirm the Manman and drain-urge interaction actually holds"**

**This was the worst oversight I have made on this feature.**

**In the game, mouse and touch are two completely different paths**

| Input | Source | What was hooked at the time |
| --- | --- | --- |
| **Touch** | `Live2D_HitAreaCheck.GetTouchTargetName(pos)` — the return value is dispatched directly | hooked |
| **Mouse** | `Live2D_HitAreaCheck.mousePointing` — a **static field** | **not hooked** |

The mouse path looks like this:

```csharp
// 每帧由 Update() 算出静态字段
private void Update() { HitAreaCheckWindows(); }
private void HitAreaCheckWindows() { ... mousePointing = array[i].Drawable.name; ... }

// 点击时读它来派发
public void Mouse0_Down() { hitAreaDown = Live2D_HitAreaCheck.mousePointing; ... }
public void Mouse0_Hold() { hitAreaPointing = Live2D_HitAreaCheck.mousePointing; ... }
public void Mouse0_Up()   { hitAreaUp = Live2D_HitAreaCheck.mousePointing; ... }
```

**So hooking only `GetTouchTargetName` means a mouse click never passes through it** — and the
player (the user) plays with the mouse, so **the Manman zone was not wired up at all**.

**The fix: hook a postfix on `HitAreaCheckWindows`**

```csharp
private static void Postfix_HitAreaCheckWindows()
{
    if (!SitPussyAreaEnabled.Value) return;
    if (PoseIdx() != 2) return;

    FieldInfo pf = FieldQuiet(hcT, "mousePointing");
    if (!string.IsNullOrEmpty(pf.GetValue(null) as string)) return;   // 已命中真的点按区，不抢

    if (鼠标落在 Manman 矩形内) pf.SetValue(null, "HitArea_Head_Sit");
}
```

**Key design: only take over when the real hit is empty** — do not fight the model's genuine hit
areas.

**Verification**

```
点按区补丁已挂载（HitAreaCheckWindows 后缀：HitArea_Manman 鼠标路径）
点按区补丁已挂载（GetTouchTargetName 后缀：坐姿小穴区）
挂载总数 36 条
```

**A live diagnostic was added to the panel** (Riding tab, Manman zone block):

```
屏幕矩形：820~980 × 430~610
鼠标在区内：是游戏当前命中：HitArea_Head_Sit
```

**"游戏当前命中" showing `HitArea_Head_Sit` is what proves the interaction holds.**
Showing "(空)" means it is not wired up; showing anything else means the mouse is actually over a
different real hit area.

**Lesson**

**"the return value of the function I patched" != "the value the game actually uses".**
The same meaning in this game often has two forms — **a static field and a method return value** —
and **mouse / touch / keyboard may each take a different one**. Before hooking, the place that is
**actually read at dispatch time** has to be found, not the place that looks most like it.

---

## 25.  HitArea_Manman must not impersonate HitArea_Head_Sit (the features get crossed)

**Status: fixed (2026-10-07) — the user pointed it out**

**The user's words**: "sit's manman and head cannot be the same function, head is for releasing
the binding kiss"

**What I had done wrong**

The Manman zone returned `HitArea_Head_Sit` on a hit, that is, **it impersonated the head**.
But the seated pose's head click **is not "accumulate drain urge" at all**; it is
**releasing the binding kiss**:

```csharp
public void 頭叩かれるSit()
{
    if (kissing == 0) return;
    if (kissing == 2)                                  // 【束缚之吻中】
    {
        if (player.Syaseing) return;
        SkillCheck.GetReadyForSkillCheck();            // ← 技能检定 = 解除束缚之吻
    }
    if (kissing == 1)                                  // 接吻准备窗口
    {
        SitGirlKiss叩く量++;                            // ← 累积解除量
        clickEffect.頭叩くエフェクト();
    }
}
```

**So that approach meant "clicking the pussy goes and releases the kiss"** — the two features were
crossed.

**The right approach: Manman as its own branch**

```
static mousePointing is rewritten to "HitArea_Manman" by us
  -> the game's Mouse_Down_0 switch has NO such case   <- it does nothing
  -> our own prefix catches it:
        · accumulate drain urge + stack judgement
        · play the click effect (reusing clickEffect.頭叩くエフェクト so the feel matches)
        · return false to skip the game's switch
```

**The final division of labour**

| Part | Function |
| --- | --- |
| **Head (HitArea_Head_Sit)** | **releases the binding kiss** (the game's logic, untouched) |
| **HitArea_Manman** | **accumulates drain urge** (handled by the plugin itself) |

**Three patches, each with its own job**

```
HitAreaCheckWindows 后缀   → 鼠标路径：把静态 mousePointing 改成 HitArea_Manman
GetTouchTargetName 后缀    → 触摸路径：同样改成 HitArea_Manman
Mouse_Down_0 前缀          → 接住 HitArea_Manman，自己处理，不让它进游戏 switch
```

**How to read the panel diagnostic changed**: "游戏当前命中" showing **`HitArea_Manman`** is what
proves the mouse path is connected (it used to show `HitArea_Head_Sit`, which was wrong).

**Lesson**

**"Impersonating an existing HitArea" needs extreme care** — you think you are borrowing the
"can be clicked" property, but you borrow **its whole semantics** along with it. Here the head
click's semantics are "release the kiss", not "accumulate urge". **The handler of that HitArea has
to be read from beginning to end before acting**, not just checked for "being clickable".

---

## 26. HitArea_Manman should mirror "spanking": raise drain urge + give the seated animation a speed impulse

**Status: implemented (2026-10-07) — the user specified it**

**The user's words**: "manman's function should be similar to what osiri gained in the trainer —
clicking raises the drain urge in sit mode and the speed of the sit-mode animation"

**What was missing at the time**

Only "raise drain urge" was done; there was **no speed impulse** — whereas spanking on the Rear
side gives both.

**Fixing it dug out a deeper bug**

The impulse's point of application only ever looked for **`OsiriMixer`**:

```csharp
object mixer = FieldQuiet(sot, "OsiriMixer")?.GetValue(objs[0]);
```

**But the seated pose uses `SitMixerA` / `SitMixerB`** — there is no `OsiriMixer` playing in the
seated pose at all, so even with an impulse it would **not reach the seated animation**.

Changed to search pose by pose:

```csharp
string[] mixerNames = { "OsiriMixer", "SitMixerA", "SitMixerB", "SitKissMixerA", "SitKissMixerB" };
foreach (string mn in mixerNames) { ... if (IsPlaying) { SetValue(EffectiveSpeed, _spankMul); break; } }
```

**Note that the `SpankSpeed*` group is already three copies per pose**, so the seated pose reads
the `_Sit` copy — the user's current values are `Gain 20 / Stack 80 / Rise 10 / Decay 0.05`.

**Final feature comparison**

| | Rear (Osiri) | Riding (Sit) |
| --- | --- | --- |
| Trigger | spank | click **HitArea_Manman** |
| Urge raised | demand urge | drain urge |
| Speed impulse | (OsiriMixer) | (**SitMixerA/B**, fixed this time) |
| Parameters | `SpankSpeed*_Osiri` | `SpankSpeed*_Sit` |

**Relation to the head**: the head releases the binding kiss; the two do not interfere (see
entry 25).

**Lesson**: **"the same class" does not mean "the same set of things"**.
`Live2D_Animation_SitOsiri` is one class managing both the Osiri and Sit poses, and
**inside it each pose has its own mixer field** — looking for only one of them means the other
pose silently does nothing.

---

## 27.  The seated drain urge filling up jumps to Osiri (three symptoms, one root cause)

**Status: fixed (2026-10-07) — the user reported it**

**The user's words**
"sit's drain urge filling up not only shows 'demand mode', it also jumps straight to osiri, and a
lot of features stop working"

**Root cause: the "take back the riding position" fallback I added myself in entry 16**

```csharp
if (_demandMode && centerGirlState == "Sit")
{
    Log.LogInfo("…模式进行中，状态却被切成坐姿 → 夺回骑乘位");
    ShowCenterGirlOsiri();      // ← 强制切到 Osiri
}
```

Its assumption was **"mode = definitely in Osiri"**. So as soon as the seated pose's own drain urge
fills up and `_demandMode` is set, this fallback immediately drags the character from **Sit to
Osiri**.

**All three symptoms come from that one line**

| Symptom | Explanation |
| --- | --- |
| shows "demand mode" | after being dragged to Osiri, `PoseIdx()` becomes 1 → `ModeWord()` gives "Demand" |
| jumps to osiri | this line does exactly that |
| many features stop working | the character is no longer in Sit, so Manman / seated drain / binding kiss all fall through naturally |

**`_demandMode` again** — this trap has now been hit **four times** in this project:

| # | Symptom |
| --- | --- |
| 1 | Riding's sliders had no effect inside the binding kiss |
| 2 | Riding's chain drain was cut off by `ChainMaxSitNormal` |
| 3 | the seated entry point was blocked indefinitely (entry 10) |
| **4** | **the seated mode was forcibly dragged to Osiri (this entry)** |

**The fix: remember which pose the mode was entered in**

```csharp
private static int _demandModePose = -1;   // 0=口交 1=背榨 2=正骑；-1=无模式

// 进入模式时
_demandModePose = PoseIdx();
// 退出/清零时（4 处）
_demandMode = false; _demandModePose = -1;

// 兜底改成只对"背榨进入的模式"生效
if (_demandMode && _demandModePose == 1) { ...夺回骑乘位... }
```

**Why this is right**: the fallback exists to solve "the seated coroutine steals the character away
before the Rear demand mode starts". In that scene `_demandModePose == 1`. The seated pose's own
mode (`_demandModePose == 2`) is supposed to stay in Sit and **must not be taken back**.

**A sweep on the side**: every remaining place that still looked only at `_demandMode` was walked
(16 of them); the rest are used for "whether a mode-specific effect applies" and "afterglow versus
mode", which are semantically correct, and were left alone.

**Lesson (to be nailed down this time)**

> **`_demandMode` means only "a mode is running"; it says nothing about which pose it runs in.**
> Any logic that branches by pose must use `PoseIdx()` or `_demandModePose`, and
> **must never substitute `_demandMode` for them**.
>
> And the other way round: **a fallback added to fix one bug may well become the root cause of the
> next one** — the fallback in entry 16 really did solve the problem at the time, but it froze a
> wrong assumption (a mode is always in Osiri) into the code.

---

## 28. It looked like there were "two Manman zones" (one built by the plugin, one apparently from the model)

**Status: fixed (2026-10-07) — the user pointed it out**

**The user's words**: "there seem to be two manman zones, one built by the plugin and one based on
the model, sort this out"

**Establishing the facts first**

`sharedassets0.assets` was checked: **`HitArea_Manman` occurs 0 times** — the model still has no
such area. `ManmanBindDrawable` was **also empty** (no drawable bound).

**So the real source of "two boxes" was: I drew all five HitAreas in the model regardless of the
current pose.**

```csharp
foreach (string nm in HitAreaDrawableNames())   // ← 全部，无过滤
    DrawOutline(...);
...
if (SitPussyAreaEnabled.Value)                  // ← 再加一个 Manman
    DrawOutline(..., "HitArea_Manman (插件自建)");
```

So **while standing in the fellatio pose**, boxes such as `HitArea_Head_Sit` / `HitArea_Osiri`
that **do not apply at all right now** were drawn as well, and they happened to overlap the Manman
zone — looking exactly like "two Manman zones, one of them from the model".

**The fix: filter by pose**

| Current pose | Draw only these |
| --- | --- |
| Fellatio | `HitArea_Head` / `HitArea_LeftGirl` / `HitArea_RightGirl` |
| Rear (Osiri) | `HitArea_Head` / `HitArea_Osiri` |
| Riding (Sit) | `HitArea_Head_Sit` |

**Two more anti-duplication measures**

1. **Judgement priority**: if the model **later** really gains a `HitArea_Manman`,
   `PussyAreaScreenRect` **uses it in preference**, and stops using the built rectangle — so two
   boxes never actually appear
2. **Draw deduplication**: drawn names go into `drawn`, and if the Manman zone is the same box it
   is not drawn a second time (this covers "a HitArea* asset has been bound")

**"Current source" was added to the panel**

```
当前来源：模型里的 HitArea_Manman   /   素材绑定：xxx   /   插件自建矩形
```

Which one is in use is visible at a glance now, no more guessing.

**User confirmation**: "it appeared, the pink manman zone" — after deduplication only one is left

**Lesson**

> **"Debug visualisation must follow the actual effective range."**
> Drawing things that do not apply makes people misjudge it as "two overlapping regions", and then
> spend time hunting a duplication that does not exist.

---

## 29. The binding kiss needs its **own** speed control (entering the kiss != triggering chain drain)

**Status: implemented (2026-10-07) — the user proposed it**

**The user's words**
"since entering the binding kiss does not necessarily trigger chain drain and so on, the binding
kiss needs its own speed sliders"

**Why it needs a separate set**

| State | Mixer used | Who managed it before |
| --- | --- | --- |
| **In the binding kiss but not yet in drain** | `SitKissMixerA/B` | **nobody** <- the gap |
| Drain / climax | `SitMixer*` / absorb clips | the drain and climax speed-up |

After `PrepareKissing()` sets `kissing` to 1, **the game may never enter drain** — during that
stretch `SitKissMixer*` is in use, while `DemandAnimSpeed` is only applied when `ModeActive()`
holds (`kyusei` or `kissing > 0.5`)… **but that is the drain/climax speed-up and should not be used
as the kissing speed**, since the two have different meanings.

**Four new parameters (Riding tab · binding kiss)**

| Parameter | Default | Description |
| --- | --- | --- |
| `KissSpeedEnabled` | on | master switch |
| `KissSpeedMul` | 100% | kissing animation speed |
| `KissSpeedWobble` | 0% | speed wobble amplitude |
| `KissSpeedHz` | 0.5 | wobble frequency |

**Mutual exclusion**

```csharp
if (GetStringField(tab, "sitState") == "kyusei") return;   // 进了榨取 → 交给那套
if (GetFloat(tab, "kissing") <= 0.5f) return;              // 不在吻里 → 不管
```

**Setting both would fight each other**, so they are strictly mutually exclusive on `sitState`.

---

## 30. HitArea_Manman finally has evidence (plus a fixed diagnostic command)

**Status: confirmed working (2026-10-07)**

**Background**: the user asked "does this pink manman zone actually do anything?" — and at that
point I had **no evidence at all**, only "the patch is mounted".

**The log said "never used"**:

```
[屁股]（NoteSpank 的日志）   0 次   ← 从没被调用
HitArea_Manman 出现 2 次           ← 但那 2 次是【挂载日志】本身
```

**Two things were added so it could be verified**

1. **Leave a trace on a hit**: a log line in `Prefix_Mouse_Down_0`; the mouse's **first entry into
   the judgement area** logs a line too
2. **A `manman` diagnostic command**

**The command was inserted in the wrong place once**: I assumed the command table was in the main
file and put it before the `default` of the "config item switch", so running `manman` reported
`ERR 未知命令` — **the command table is actually in `GameCommandServer.cs`** (the file-based command
channel file). The switch in the main file is the config item table used by `set`, and the two look
very much alike.

**Measured result**

```
[HitArea_Manman] pose=2  enabled=True  src=plugin-rect
  mousePointing=HitArea_Manman   mouse=970,157  inside=YES
```

`mousePointing` was successfully rewritten to `HitArea_Manman`, and the mouse was inside the area —
**the mouse path really is connected.**

**Lesson**

> **"the patch is mounted" != "the feature works".**
> A mount log only proves Harmony connected it; it does not prove it is reached under real input.
> **To prove that, make it leave a trace the moment it triggers**, then go find that trace in the
> log.

---

## 31. HitArea_Manman_By_Plugins: renamed, and its visualisation unified with the other HitAreas

**Status: implemented (2026-10-07) — requested by the user**

**The user's own words**

"Once the manman zone is wired up it should be the same level as the other HitAreas; call it HitArea_Manman_By_Plugins."
"It can be shown in pink, but it has to use the same visualisation logic as the other hitareas."
"Can that last one (model-relative) go away?"

**(1) Rename**

```csharp
internal const string ManmanAreaName = "HitArea_Manman_By_Plugins";
```

It now sits at the **same level** as the game's native set:

```
HitArea_Head / HitArea_Head_Sit / HitArea_Osiri
HitArea_LeftGirl / HitArea_RightGirl
HitArea_Manman_By_Plugins          ← provided by this plugin
```

A **constant** unifies it; the string literal no longer appears scattered around
(all 4 runtime write sites now use `ManmanAreaName`).

**A distinction deliberately kept**:
when `PussyAreaScreenRect` checks whether the model **actually has** a `HitArea_Manman`
(one the user built by hand), it still looks for the name **without the suffix** — so the two
cannot collide.

**(2) Visualisation unified**

The Manman zone used to go through **its own separate drawing path**: its own `_rectFill` /
`_rectLine` pink textures, its own `_pussyHintStyle`, its own `SitPussyAreaShowRect` switch,
its own `DrawPussyAreaRect()`.

Now it is **folded into `DrawOutline()`**, with one extra colour parameter:

```csharp
private void DrawOutline(float x0, float y0, float x1, float y1, string label, bool pink = false)
```

- green = the game's native HitArea
- **pink = the plugin's Manman zone**
- **same function, same logic, same master switch `HighlightHitAreas`**

The old separate path was **deleted wholesale** (54 lines: `DrawPussyAreaRect` + `_pussyHintStyle`
+ `_rectFill` + `_rectLine`); nothing is left.

**(3) The "(model-relative)" label is gone**

That suffix was hard-coded inside the old `DrawPussyAreaRect`.
With that deleted, the label comes from `DrawOutline(..., "HitArea_Manman_By_Plugins", true)` —
**just the name**

**Measured**

```
HitArea_Manman_By_Plugins     ← pink box, clean label
```

**Lesson**

> **Two drawing paths for the same thing means they will drift apart.**
> The separate pink path made the "Manman zone" look like a special case,
> when it really should be "one more HitArea, only a different colour".
> **Colour is a parameter, not another code path.**

---

## 32. The last three parameters folded into the three-copy scheme + entry 18 verified

**Status: done (2026-10-07)**

**User**: "1 I tested it, no problems" — that is, **entry 18 (refresh the duration after the stack limit is reached) passed real-play verification**

**The three parameters added**

| Parameter | Old value | New value (three copies) | Note |
| --- | --- | --- | --- |
| `ChainGainPerDrain` | 8 | 8 / 8 / 8 | fixed-increment mode for chain drains |
| `DemandFellaDecay` | **59.78** | **59.78 × 3** | **the user had tuned it; migrated** |
| `DemandMaxStackRefreshMul` | 100 | 100 / 100 / 100 | refresh ratio once the stack limit is reached |

**Nearly lost tuned values again**: the old value of `DemandFellaDecay` was `59.78261`, while
the freshly generated three keys defaulted to **25**. The game was closed and the old value was
carried over by editing the config file directly — this is the **second** time the same thing
has happened (the first was the batch of 31 parameters, 48 values).

> **Rule: any action that generates new keys leaves the user's tuned values behind in the old keys.**
> So **every time three-copy parameters are added, a "old value → three copies" migration must
> follow**; it is not an optional step.

**Result**: parameters missing from the manual went from 12 down to 4 (all of them mentioned
only as part of a slash-separated combination); the three-copy keys number 162 (54 × 3).

---

## 33. Entry 7 (seated ecstasy anomaly) changed to "probably fixed" + a relapse alarm added

> **Follow-up (confirmed by the user in real play)**: this one is fine now and has been removed
> from `KNOWN-ISSUES.md`. The "probably" label can go — the relapse alarm stays; there is no
> need to pull it.

**Status: probably fixed (2026-10-07) — user judgement + automatic detection added**

**User**: "7 may not exist at all, it may have been fixed"

**Why that judgement holds up** — the symptom recorded under #7 was "in seated mode the ecstasy
value drops −195.1 in a single step", and the fixes made since then **all land on possible causes**:

| What was fixed | Why it could cure #7 |
| --- | --- |
| `DrainSlotIsDemand()` (#13) | the wrong chain-drain slot means the wrong soft cap is applied |
| `_demandModePose` (#27) | being dragged to Osiri mid-mode scrambled the state machine |
| `ModeActive()` (#12) | Riding sliders not taking effect inside the binding kiss meant the set value was not the actual value |
| Seated mixer fix (#26) | the speed impulse had never reached the seated pose at all |
| `ChainSoftCap` / `AfterglowSoftCap` | these literally decide "where the ecstasy value should be held down" |

**But "it might be fixed" is not a conclusion** — so automatic detection was added
(`EcstasyDropWarn`, default 50):

every frame `PlayerControl.CurrentEcstasy` is compared; when the drop exceeds the threshold it
writes a warning **with full context**:

```
[绝顶骤降 #1] 262.8 → 131.4（Δ-131.4）  姿势=2  模式=False(入于-1)  段=0
  sitState=attacking  kissing=0  osiriState=none  Syaseing=1  余韵剩=0  连榨=0
```

**The first run caught two — and both were false positives**

```
[绝顶骤降 #1] 262.8 → 131.4（Δ-131.4）  Syaseing=1
[绝顶骤降 #2] 131.4 → 0（Δ-131.4）      Syaseing=1
```

When `Syaseing=1` (the player is ejaculating) the ecstasy value is **consumed normally**, and it
happened to **halve twice** — that is design behaviour, not an anomaly.

**Now excluded**:

```csharp
if (GetFloat(pl, "Syaseing") > 0.5f) { _lastEc = cur; return; }   // consumption during ejaculation is not an anomaly
```

**So the criterion got sharper**: what is being caught is **"a sudden drop while not ejaculating"**.

**How to use it to decide whether #7 is still there**

Play a stretch of seated drain (chain drains and afterglow included), then search the log:

```bash
grep "绝顶骤降" BepInEx/LogOutput.log
```

- **Found (and not ejaculating at the time)** → #7 is still there, and the log carries full context, so it can be located directly
- **Not found** → it can be considered fixed

**Lesson**

> **"It may already be fixed" is a reasonable hypothesis, but it needs a check that can falsify it**,
> otherwise it only reappears on the next relapse as "huh, why is this broken again".
> This time the check was built the right way: **it catches by itself instead of waiting for a human to watch.**

---

## 34. xcheck invariants extended to the new state (#4)

**Status: done (2026-10-07)**

It used to check up to (16); **(17) to (23)** were added:

| # | Invariant | Hard / soft |
| --- | --- | --- |
| (17) | **`_demandMode` and `_demandModePose` agree with each other** | hard |
| (18) | the stack count stays within `0 ~ DemandMaxStacks` | hard |
| (19) | demand urge is non-negative | hard |
| (20) | Rear mode has not been displaced by the seated pose | soft |
| (21) | the chain-drain counter does not exceed this slot's limit | soft |
| (22) | the afterglow counter is non-negative | hard |
| (23) | the Manman zone should have a rectangle while seated and enabled | soft |

**(17) is the most valuable one in the group**: `_demandMode = false` has **four** assignment
sites, and each one must reset `_demandModePose` at the same time. Miss any one of them and (17)
reports FAIL — which is exactly why the original bug (#27) could hide for so long.

**Measured, all passing**

```
[通过] (17) _demandMode=False _demandModePose=-1
[通过] (18) 段=0 上限=5
[通过] (19) 索取欲=33.66%
[通过] (20) 模式入于=-1 当前姿势=2
[通过] (21) 档位=坐姿·正骑·榨取 计数=0 上限=16
[通过] (22) 待发动=0 进行中=0 累计=0
[通过] (23) 矩形 754~1194 × -164~33
结论：无硬性矛盾，无提醒
```

**Side effect**: the output of (23) **exposes once more that the rectangle falls below the
bottom of the screen** (y is `-164~33`) — that is the pending Manman-zone position problem, and
now it will keep reminding through xcheck until it is fixed.

---

## 35. Seated lock + fellatio items removed from the Riding tab + log tags named after the pose

**Status: implemented (2026-10-07) — requested by the user**

**The user's own words**

"Demand is the osiri name; sit must always be called Drain."
"And note that after entering sit it must not be possible to switch to fella and osiri."
"So everything about fellatio in the Riding tab can be deleted."

**(1) Log tags**

There were **27** hard-coded `"[索取] "` prefixes — logging while seated also said "Demand".
All changed to `"[" + ModeWord() + "] "`, naming the tag after the current pose.

**(2) Seated lock (`SitLockEnabled`, on by default)**

There are only **two exits** out of the seated pose in the game:

| Exit | Note |
| --- | --- |
| `ShowCenterGirlFella()` | **`Osiri解除()` calls it as well** |
| `ShowCenterGirlOsiri()` | |

A prefix on both blocks them, which makes the seated pose a state you can enter and not leave.
The reason: the Riding drain chain (chain drain / afterglow) is built entirely on "the character
is seated", so being switched away mid-chain breaks it.

**A trap hit here**: `Prefix_ShowCenterGirlFella` **already existed** (the afterglow gate), and a
second definition with the same name was added → `CS0111 duplicate definition`.
The right move is to **add a check to the existing one**, not to add a new one:

```csharp
private static bool Prefix_ShowCenterGirlFella()
{
    if (_afterglowRemaining > 0) { ...不进口交...; return false; }   // existing: afterglow gate
    if (SitLockActive())         { ...坐姿锁定...; return false; }   // added
    return true;
}
```

**And deleting the duplicate block took the definition of `SitLockActive()` with it** → `CS0103`
— **when deleting a block, check whether anything else depends on a definition inside it**.

**(3) Fellatio-related items removed from the Riding tab**

Wrapped in `if (!sitSide)` (**this only affects what the Riding tab shows; the Rear copy is untouched**):

| Item | Why it means nothing in Riding |
| --- | --- |
| `DemandBlockFella` (no return to fellatio during the mode) | the seated pose is locked; there is no way back |
| `DemandFellaDecay` (decay per fellatio return) | same |
| `DemandOsiriBoost` (riding threshold drops after a fellatio return) | same |
| `OsiriReturn*` (the 4 return-accumulation parameters) | that is the "Rear → fellatio → back to Riding" mechanic |

**(4) While at it: the soft-cap jump was made smooth**

The 38 "ecstasy sudden drops" caught in measurement (largest Δ −267.8) all came from **my own
soft-cap logic**: when the value rises above the wobble band it was **set in one step**.

Added `EaseToward(cur, target, maxRef)` — approaches at a rate (`CapSmoothRate`, default
60%/second) — applied in three places: the chain-drain / afterglow soft caps, and
`KeepMaxCaps`'s `currentHP` and `CurrentEcstasy`.

**39 hooks** (+2)

---

## 36. Seated lock: the exit paths are verified to be fully covered

**Status: verified (2026-10-07) — the user confirmed "what I want is that after entering the seated pose it never switches to another mode"**

**This is design, not a defect** — not "being unable to leave is a bug" but **being unable to
leave is exactly what was asked for**.

**How it was verified**: every place in the game that writes `centerGirlState` was listed.

| Line | Method | Is it an exit from the seated pose | Blocked |
| --- | --- | --- | --- |
| 10413 | **`Start()`** | this is only the initial scene set-up | — |
| 10442 | `ShowCenterGirlFella()` | yes | blocked |
| **10992** | `ShowCenterGirlOsiri()` — **assigns directly inside the method body** | yes | blocked |
| 10445 / 10448 | the `_CenterGirl()` coroutine | only started by the two methods above | indirectly blocked |
| 10619 / 10623 | `Show_CenterGirlSit()` | that is **entering** the seated pose | not blocked (correct) |

**The key point**: `ShowCenterGirlOsiri()` writes `centerGirlState = Osiri` **directly in the
method body**, not inside a coroutine — but **a prefix returning `false` skips the whole method
body**, so that line is blocked along with it.

**Also**: `Osiri解除()` calls `ShowCenterGirlFella()` internally, which walks the same blocked path.

**Conclusion: the two prefixes plug every exit.** The seated pose is a state you can enter and
not leave.

**How to leave it** (if it is ever needed): only the plugin could provide it (say a command or
button that clears `SitLockEnabled` and then triggers the switch).
At present **that escape hatch is not built** — what the user explicitly asked for is "it never
switches away".

**The one thing to watch**: the assignment inside `Start()` sets `Fella` and happens when the
scene loads — **if the character were already seated before that, this assignment would not be
blocked**. But `Start()` runs once during scene set-up, before the seated pose exists, so in
practice it is not a problem.

---

## 37. The game felt laggy: per-frame reflection and vertex scanning (now cached)

**Status: optimised (2026-10-07) — the user reported "the game has become laggy"**

**First rule out a false appearance**

`rendercheck` showed `Time.timeScale = 0.05` — a leftover from an earlier debugging command that
had **clamped the game**, which looks exactly like lag. `timescale 1` restored it, **so that was
ruled out first** before looking at real performance.

**The real performance problem**

Several of the added features were doing expensive work **every frame**:

| Site | Frequency | Cost |
| --- | --- | --- |
| `PussyAreaScreenRect` ← `Postfix_HitAreaCheckWindows` | **every frame** | `Resources.FindObjectsOfTypeAll` + walking **every vertex of every drawable** of the CubismModel through two coordinate transforms |
| `TryGetDrawableScreenRect` ← `DrawHitAreaHighlight` | **2 to 3 times per frame in OnGUI** | same as above |
| `HitAreaDrawableNames()` | every frame in OnGUI | `FindObjectsOfTypeAll` + walking |
| `TickKissSpeed` / `TickSpankSpeed` / `TickDemand` | every frame | `FindObjectsOfTypeAll(Live2D_Animation_SitOsiri)` |

**The key point**: `FindType` **has a dictionary cache of its own**, but
**`Resources.FindObjectsOfTypeAll` does not** — and it has to **scan every loaded object** each
time, which is the most expensive link in the chain.

**Fix: short TTL caches**

| Cache | TTL | Reason |
| --- | --- | --- |
| `HitAreaCompCached()` | 1 second | component instances do not change every frame |
| `ModelRectCached()` | **0.1 second** | the bounding box moves with the character's motion, but 10 Hz is enough for judging |
| `HitAreaDrawableNamesCached()` | 0.5 second | the list barely changes |

**Effect**: per-frame `FindObjectsOfTypeAll` went from "several times" to "about once every 10
seconds", and vertex scanning from "2 to 4 times per frame" to "10 times per second".

**Lesson**

> **"Called once per frame" and "calling reflection once per frame" are two very different things.**
> `FindType` has a cache so it looks harmless, but the `Resources.FindObjectsOfTypeAll` that follows
> it is what actually costs — **the cache belongs on the most expensive layer**,
> not "add a cache to the function" and call it done.
>
> Also: **first confirm whether timeScale has been clamped**, then talk about performance. That
> false appearance nearly had me changing a pile of code that needed no changes.

---

## 38. Binding kiss bridging audit + three gaps + animations "played directly on the layer" cannot be sped up

**Status: filled in (2026-10-07) — the user asked "check whether every binding-kiss action is bridged"**

### Audit result

The game has 22 binding-kiss related methods; only **4** had been bridged (all of them on the
"absorb" half):

| Bridged | |
| --- | --- |
| `PrepareKissing()` | postfix: lengthen the window |
| `KissPrepare解除()` | prefix + postfix: stop the decay |
| `Event_SitKiss吸精1()` | postfix: chain-drain ecstasy scaled by damage |
| `Even_SitKiss吸精OnEnd()` | prefix: take over the chain limit |

**The gaps (the "ejaculation" half was essentially unbridged)**:

| Method | Consequence |
| --- | --- |
| `Event_SitKissSyaseiOnEnd()` | **the chain limit could not reach this path** |
| `Play_SitKissSyasei()` | the climax animation got no speed multiplier |
| `DealDamage_SitKiss()` | this damage was not counted toward return accumulation |

**All three added** (copied from the same pattern):

```
束缚之吻补丁已挂载（Event_SitKissSyaseiOnEnd 前缀：射精也受连榨上限管制）
束缚之吻补丁已挂载（Play_SitKissSyasei 后缀：绝顶动画可调速）
束缚之吻补丁已挂载（DealDamage_SitKiss 后缀：伤害计入回归累积）
```

### The "wrong class" trap again

The first attempt hooked `Event_SitKissSyaseiOnEnd` and `Play_SitKissSyasei` on `TabemiControl`
→ `找不到 Event_SitKissSyaseiOnEnd`

They are actually in **`Live2D_Animation_SitOsiri`** (the same class as the absorb one).
And `DealDamage_SitKiss` really is in `TabemiControl`.

**— the rule "count the log lines after hooking" cannot be skipped.**
This time it was found from that "not found" line in the log, not by guessing.

### Follow-up feedback from the user: "some actions still are not sped up"

**Root cause**: part of the binding-kiss animation **does not go through a mixer**; it is played
directly on the layer:

```csharp
// Play_SitKissSyasei()
_CenterGirlLayer.Play(SitKissSyaseis[num]);
// Play_SitKiss吸精()
_CenterGirlLayer.Play(SitKiss吸精s[num]);
```

But `TickKissSpeed` only looked for `SitKissMixerA/B` and `SitMixerA/B`
→ **this kind of animation can never be hit, so it cannot be sped up.**

**Fix: when no mixer is hit, fall back to "the state the current layer is playing"**

```csharp
if (!applied)
{
    foreach (string ln in new string[] { "_CenterGirlLayer", "_SitGirlfaceLayer" })
    {
        var layer = FieldQuiet(sot, ln)?.GetValue(objs[0]);
        var st = layer.GetType().GetProperty("CurrentState", AllFlags)?.GetValue(layer, null);
        ...
        sp.SetValue(st, mul, null);   // EffectiveSpeed
    }
}
```

**This rule is worth writing down**:
> The game's animations have **two playback paths** — through a mixer (`*.State`) and played
> directly on a layer (`*.CurrentState`).
> **Any logic that "sets the speed of the animation currently playing" must cover both**,
> otherwise some actions are always silently out of control.
> The Osiri climax hit exactly the same wall earlier (`_CenterGirlLayer.Play(OsiriSyaseis[num])`).

**42 hooks.**

---

## 39. Three tabs' wording unified: description templates swap the word per tab

**Status: fixed (2026-10-07) — the user pointed out "the Riding page still has a lot of 'Demand' descriptions", then asked "use Absorb on the fellatio side"**

**The problem**

Config descriptions are **one template shared by three tabs**, and **15 of the templates had
"索取" hard-coded** ("the urge needed for the first layer of Demand state" and the like).

`PoseDesc(i, desc)` only replaced the `{模式}` placeholder — **hard-coded words with no
placeholder leaked unchanged into the other two tabs**.

The first fix only touched the seated tab, on the reasoning that "the word never got mixed into
the fellatio descriptions and a careless replace could break something". The user then explicitly
asked for Absorb on the fellatio side as well — so that was added, and **the actual text after
replacement was checked** (no assumptions this time).

**Fix (one place fixes everything)**

```csharp
private static string PoseDesc(int i, string desc)
{
    string d = desc.Replace("{模式}", ModeWordAt(i));
    if (i == 2) d = d.Replace("索取", "榨取");        // Riding
    else if (i == 0) d = d.Replace("索取", "吸取");   // Fellatio
    // Rear keeps "索取" — that is its own name
    return "【" + PoseLabel[i] + "·" + ModeWordAt(i) + "】" + d;
}
```

**Verification (counted per tab, not sampled by impression)**

```
口交   54 条描述，含「索取」0 条
背榨   54 条描述，含「索取」54 条（本来的叫法）
正骑   54 条描述，含「索取」0 条

【口交·吸取】每次累积吸取欲的下限（%）
【背榨·索取】每次累积索取欲的下限（%）
【正骑·榨取】每次累积榨取欲的下限（%）
```

**The panel labels were already right** — the 5 labels containing "索取" are either inside
`if (!sitSide)` (shown only in Rear) or are each tab's own untouched title.

**Lesson**

> **"One shared template" + "hard-coded tab-specific words in the template" = the wording will
> leak eventually.**
> A placeholder (`{模式}`) only covers the part that was written properly;
> the remaining hard-coded words either have to be replaced per tab when the description is
> generated, or turned into placeholders as well.
> **You cannot rely on "whoever writes the template remembering to use the placeholder".**

---

## 40. The sudden-drop trace log, plus three new manual sections

**Status: implemented (2026-10-07)**

### (1) Tracing the sudden drop

Entry 39 inferred that "the drop comes from the soft cap", but **the smoothing was mounted and still did not catch it** — so that inference is incomplete. Rather than keep guessing, **leave a trace at every place that could possibly be responsible**:

| Trace point | What it records |
| --- | --- |
| `Prefix_EcstasyReset` (both overloads) | value before the reset + pose / mode / segment count / sitState / kissing / osiriState / Syaseing / chain |
| Where the chain soft cap is written | cur / target / soft / wob / maxE (only when the drop exceeds 50) |

**On the next sudden drop, the log will say who did it**, instead of relying on how well the numbers match.

### (2) Three sections added to the manual

| New section | Content |
| --- | --- |
| **Smoothing when a cap pulls the value back** | `CapSmoothEnabled` / `CapSmoothRate`, and why it exists (that 38-times-in-a-row Δ-240 jump) |
| **Ecstasy sudden-drop detection** | `EcstasyDropWarn` + how to read the trace log + "consumption during ejaculation is excluded" |
| **Seated-pose lock** | `SitLockEnabled`, the two exits, and the escape hatch (`situnlock` / `sitlock` + the panel button) |

**Items the manual did not mention: 12 to 8** (the remaining 8 are all mentioned as slash combinations, so they are covered functionally).

### (3) Correcting the status of #4

`xcheck`'s invariants **had actually been extended to (17) to (23)** long ago (entry 34), but #4 kept carrying a "technical debt" note that was never updated — now fixed.

> **Lesson**: **"done but the status was not updated" looks exactly like "not done" on the books.**
> A census treats it as a to-do and wastes one investigation.

---

## 41. English UI: the fallback mechanism, and the two silent failures it brings

**Status: framework done, translation advancing tab by tab** (the framework is in `plugin/Localization.cs`)

### Why build the mechanism before translating

The panel has roughly 200 strings a user can see (53 buttons, 30 toggles, 10 labels, 8 tab names, 58 calls producing hint lines), plus 102 config descriptions. Translating all of it in one go and then shipping it leaves no way to verify the intermediate state, and one wrong edit can take down the whole panel.

So one rule was settled first: **when English is missing, show Chinese**.

`L.T("中文原文")` looks the string up in the table; found means English, missing means the Chinese string is returned unchanged. "Half translated" therefore becomes a shippable, usable state instead of an untouchable intermediate one.

### Why the Chinese original is the key

No separate set of key names; the change is just wrapping a layer around the outside:

```
GUILayout.Button("开")  →  GUILayout.Button(L.T("开"))
```

The table is two columns, "Chinese TAB English", so someone who does not read code can fill it in and proofread it. The cost is that the same Chinese string cannot have two translations in different contexts — when that day comes, add a `Chinese|suffix` form.

### Silent failure one: hard-coded width

The "Save as preset" button in the presets tab had a hard-coded `GUILayout.Width(90f)`. Four Chinese characters fit easily; `Save as preset` got clipped to `Save as prese`.

**This kind of problem is invisible when there is only one language** — the width was sized for the widest Chinese label, and English that is longer simply overflows, without an error, just a partial display. The fix is to compute the width from the actual laid-out translation (`BtnW()`).

### Silent failure two: the table and the source drift apart

The cost of using Chinese as the key: if the Chinese string in the source changes by a character (even one extra 「的」), that row in the table can never match again. And the fallback mechanism makes it **quietly display Chinese** — no error, no crash, just that sentence never becoming English.

The static check therefore gained rule 13: every Chinese row in the table must have a corresponding `L.T` call in the source. The reverse direction (source has it, table does not) is normal — that is simply untranslated, and only reported as progress.

### Where the language control sits is part of the design too

The first version put it after the "Caps" block in the system tab. One screenshot showed it needed scrolling to find — and someone who switched to English and wants to switch back is exactly in the situation of not finding it. Moved to the very top of the system tab, visible the moment the tab opens.

Hiding the way back is the same as locking someone in English.

### Sync direction: the third time falling in the same place

The first time, code was edited in the release copy, and running `sync.sh code` (work to release) overwrote the whole thing with the older work copy. Only `Localization.cs` and `en.txt` survived, because they were new files.

**Code's authority is the work copy** — that is written twice in the appendix, and it was still stepped on in practice. The test is simple: `sync.sh code` goes "work to release", so editing code in the release copy is waiting to be overwritten.

---

## 42. The blind spot of the second English round: the checker sees call sites, not variables

After the first round the panel was basically English, and the user pointed at "still something untranslated" twice more. The cause was not missed translations but that **the checker and the translator used the same angle**: both looked for `L.T(` call sites.

So every shape where "the string is not inside that call" went invisible:

| Shape | Example | Why it is invisible |
| --- | --- | --- |
| T not applied inside a helper | `Sub(string title)` | the call site looks fine, the translation is lost inside the helper |
| stored in a variable, drawn later | `_snapHint = "已导出…"` | no GUI call at the assignment, only a variable at the draw site |
| format arguments | `L.T("{0} 项{1}", n, " ← 生效中")` | the argument is not a key, it enters the template verbatim on lookup |
| the normalised key | `desc.Replace("榨取", "{模式}")` | the table's key is the normalised form, and that string does not exist in the source |

The direction of the fix is **to change the angle**, not to write finer rules: `tools/zh-scan.py` no longer asks "which calls have been translated" but "of all Chinese literals, which ones did not fall inside a translation region". The former requires guessing the shape correctly first; the latter only needs one complete list of literals.

One more thing worth stating: rule (14) used to grab call segments with `GUILayout\.(Button|Label|Toggle)\([^;]*?\)`. The non-greedy match against a nested call like `Button(L.T("打开"), GUILayout.Width(BtnW("打开", 52f)))` stopped at the inner closing parenthesis, so `L.T(` fell outside the cut — an already-translated button was reported as bare Chinese. **A false report from the checker costs as much as a missed one**: once it cries wolf, the next person starts distrusting the checker and then ignores the real one too.

Lesson: a checker's blind spot coincides with the implementer's, because they share one mental model. What actually finds the blind spot is **counting from a different direction** (the total minus what is covered), not writing the rule more finely.

## 43. Three blind spots of the translation: concatenation, init order, the regex

**Symptom**: after v1.0.94, `static-check` reported "all 997 table entries match", yet the
English-mode log still contained Chinese, and so did command replies.

### One: `L.T` nested inside its own argument

```csharp
Log.LogWarning(L.T("A" + "B" + L.T("C"), args));   // the outer call sees the concatenated string
```

The outer `L.T` looks up "A + B + C" while the table stores the segment keys — it can never match.
`[绝顶骤降]` and `[骤降溯源]` failed silently that way, even though v1.0.93 had written their
translations into the table.

**Fix**: `L.T("A" + "B" + "C", args)` — one key for the whole line.
**Lesson**: an `L.T` inside `L.T(...)` means someone mixed per-segment translation with
per-sentence translation.

### Two: the log fires before `L.Init`

`GameBuild.ReportOnce(Log)` used to sit at the top of `Awake`, before the language table loads.
Wrapping it in `L.T` would not have helped — `_on` is still false there, so `L.T` returns Chinese.

**Fix**: move `ReportOnce` after `L.Init`.
**Lesson**: translating a log line is not only "wrap it in `L.T`" — it also asks whether the
table has been loaded by the time that line runs.

### Three: the regex only knew the capitalised `Log.`

The bulk wrapping in v1.0.94 used `Log\.Log(Info|Warning|Error)`.
`GameBuild.cs` writes `log.LogInfo(...)` — `log` is the `ILogger` parameter, lowercase.
Those three assembly-check lines were never matched, and neither the checker nor the table
could see them.

**Fix**: cover `log.Log*`, and widen rule (13) to accept `ProbeWhy(...)`.
**Lesson**: a bulk replace is only as wide as its regex — a narrow one does not fail, it
silently misses.

One more of the same family: `Say(...)` arguments were wrapped, but strings built first and
`Say`-ed later (`StateLine()`) were not, so command replies came out half English and half
Chinese (`scene=TabemiTitle （无 TabemiControl） 强制=OFF`).

### Two escape bugs along the way

Writing `cmd\anim.csv` and `cmd\tremor.csv` as literals made C# read `\a` and `\t` as bell and
tab — those log paths had carried an invisible character all along. `\\a` / `\\t` is the real
backslash.

---

## 44. Layout: buttons that stretch across the row, and how to watch a shape you cannot see

**Symptom**: every button in the panel filled the whole row — a two-character "Delete" was 540 px wide.

**Cause**: `GUILayout.Button(text)` without a width expands to fill. Invisible with one language
and few buttons; with eight tabs the panel looked wide and clumsy.

**Fix**: two helpers, `Btn(zh)` and `BtnText(text)`, giving a width from
`GUILayout.Button`'s `CalcSize` (never below 88 px). 46 buttons plus 8 dynamic-label ones converted.

**Why fixing it was not enough**: the shape **does not show up in the code** — it looks exactly
like a normal button, with no warning and no error, so the next person writes one and the problem
is back. Hence rule (16): take each whole `GUILayout.Button(...)` call and fail it if the span
contains no `GUILayout.Width(`, `Btn(` or `BtnText(`.

It caught 9 on its first run — 8 real (the label was a variable or a ternary), 1 an example
inside a comment. **Comments get scanned too**, so the rule blanks comment spans out first —
otherwise `// GUILayout.Button(text)` becomes a false positive.

### Two more found along the way: tab widths and wrapping

- Tab labels were hard-coded at 62 px, so English `Fellatio` / `Presets` were clipped to
  `Fellati…`. With `TabW()` (4 px padding instead of 14) all eight just fit inside 540 —
  **ten more pixels and the last one falls off the panel**
- `Hint()` had `wordWrap` on all along, but **a Label without a width constraint does not wrap**;
  long English sentences were simply cut at the right edge. `GUILayout.ExpandWidth(true)` made
  them actually wrap

---

## Appendix: view dragging (a game mechanic, not a defect)

**User report**: "moving the mouse up and down into a certain region moves the view"

In the game, `GetMoveVector()` looks only at the **mouse Y coordinate**:

```csharp
private float edgeRatio = 0.05f;

private float GetMoveVector()
{
    float num = Input.mousePosition.y;
    if (num >= screenHeight * (1f - edgeRatio))    // top 5%
        return -1f * (num - screenHeight * (1f - edgeRatio)) / (screenHeight * edgeRatio);
    if (num <= screenHeight * edgeRatio)           // bottom 5%
        return  1f * ((screenHeight * edgeRatio - num) / (screenHeight * edgeRatio));
    ...
}
```

At a height of 1080:

```
top drag band:    y 1026 ~ 1080
bottom drag band: y    0 ~   54
```

**What this means for hit areas**: if the rectangle of `HitArea_Manman_By_Plugins` falls into either band, clicking it drags the view instead of clicking.

**The hit area measured at the time**: `742~1208 x 187~394` — far from both bands, **no conflict**.

**But the CY slider can drag it in.** This is not a defect, it is a mechanic constraint — if someone later reports "the Manman zone works only sometimes", first check whether its y has entered 0~54 or 1026~1080.

**Lesson**: when writing an overlay's hit area, confirm the game itself is not using the same screen region. That region is not visible from the code structure; it has to be deduced from how the game reads the mouse.

---

## Appendix: the two-tree sync rules (took several falls to sort out)

**The project has two trees**:

```
<parent>build/burger-shop           work copy (compilation happens here)
<parent>build/burger-shop-publish   release copy (the git repository is here)
```

**The flow is not one-directional; it depends on the file category**:

| Category | Authority lives in | Direction |
| --- | --- | --- |
| code / tools (plugin, trainer, tools) | work copy | work to release |
| docs (README, manual, KNOWN-ISSUES, CHANGELOG, ENGINEERING-NOTES) | release copy | release to work |
| doc copies inside dist | release copy | release to work (that is what the installer consumes) |
| installer/ | work copy | work to release |

**installer/ used to need a manual diff**: the release copy carried explanatory comments added during a cleanup, the work copy carried later changes — each side had something the other did not. It was synced over once as "code", and the cleaned version got dirty again: local paths and decoration glyphs both came back. Later the two converged to mechanical differences like the version number, so it was folded into automatic sync.

The price of folding it in is "nobody looks at that diff any more", and once diff-reading is handed to a human, it eventually does not happen: in the v1.0.9 round the release copy's `make_payload.ps1` still pointed at a `dist/使用说明.md` that had been deleted long before, so packaging on that side was bound to break. Automatic sync at least makes that kind of drift surface before the commit.

**Because "one direction" was applied to "two kinds of files", it kept going wrong**:

- release copy over work copy → extracted methods lost, split documents rolled back
- work copy over release copy → local paths mixed into the manual again

**The rule**: do not `cp` by hand. Go through `tools/sync.sh`:

```
bash tools/sync.sh check    show differences only
bash tools/sync.sh code     work to release
bash tools/sync.sh docs     release to work
bash tools/sync.sh all      docs first, then code
```

The script has a gate: before syncing code it checks **whether that file contains a local path**, and refuses if it does. The release copy may contain none.

**The gate itself has been bitten once**: the first version was written as `LEAKPAT='E:\+DSH|...'`, and in ERE `\+` means "a backslash repeated one or more times" — but one level of escaping was lost when writing the file, so it became `\+` = a literal plus sign, which never matches. The first run therefore caught nothing and the offending content went through. **After adding a gate, always test once that it actually blocks.**

### The sync script once compared itself with itself (why v1.0.9 was re-released)

The script originally derived its paths like this:

```
WORK=<directory the script sits in>/..
PUB=$WORK/../burger-shop-publish
```

By that formula the script should only run in the work copy. But a copy sits in each tree — run from the release copy, `WORK` resolves to the release copy itself and `PUB` resolves to "release copy/../burger-shop-publish", which is the release copy again. **Both variables point at the same directory**, so every diff is equal, the script prints a wall of "same" and finally "0 files in total", looking like a successful sync.

The cost was that the whole tree stayed on the previous version: five version numbers in the source, three artifacts in `dist/`, three checksums in the README, all stale together. And rule (10) asks whether "the README and dist agree with each other" — both stale together, it could only report pass. A user downloaded v1.0.9 from the release, checked against the README, and all three lines disagreed.

Now it recognises which tree it is in from the directory names, and exits with an error if it cannot compute two different trees. Two rules were added as well: (11) the six version numbers in the source must agree, (12) the three artifacts in `dist/` must carry the same version.

**The symptom of "two variables that should differ compute to the same value" is everything passing neatly.** When everything passes, suspect the tool rather than feel relieved.

---

## Appendix: the repository settings on the GitHub side

The repository used to hold a `GITHUB发布文案.md` as a draft — Description, Topics, Release body and Social preview were written there first and then pasted into GitHub. It was deleted: once pasted, the truth lives on the GitHub side, and this copy could only drift — and its own note literally said "being out of date does not mean the release content is out of date", so nobody would maintain it.

By the time it was deleted, all five parts were wrong in some way: the Description still said "six tabs" (it is eight), Topics was missing five and had one extra, the Release copy was stuck at v1.0.0, "Website can be left empty" while it had long been filled in, and the file name in the Social preview line was blank. Keeping it was more dangerous than not having it — filling the form from it would fill it wrong.

To change something, change it on the GitHub side. The lines below are the current values; update them here as well when you change them:

- **Description**: `汉堡店的四个夜晚 / Four Nights at the Burger Shop 数值修改器。18+ · BepInEx 插件，八栏面板（三种姿势各一份参数 + 预设 + 关于），自带安装程序与完整说明书。不含任何游戏素材。`
- **Topics**: `bepinex-plugin` `csharp` `game-mod` `harmony` `live2d` `modding` `trainer` `unity-mod` `four-nights-at-the-burger-shop` `18-plus` `adult` `nsfw` `burger-shop` `four-nights`
- **Website**: `https://space.bilibili.com/11247581`
- **Social preview**: `social-preview.png` in the repository root (1280×640), uploaded from Settings

---

## S6. Panel layout tidy-up and decoration glyph removal

**Status: two parts, one done, one outstanding**

### Done

**Layout helpers and a single format**

Four helpers were added — `Section` / `Sub` / `Hint` / `Rule` — so spacing and formatting have exactly one definition. There used to be three or four heading styles plus four spacing values (4/6/8/10), all written by hand, which is why it looked messy.

Converted: 7 headings, 2 separators, 10 indented notes. The panel width went from 452 to 540 so labels are no longer squeezed against the edge.

**Pulling the seated-only block out of a 297-line method**

`DrawDemandSection` covered both the Rear and the Riding tabs, with Riding-only content (binding kiss, seated lock, Manman zone, roughly 150 lines) wedged into shared logic.

It is now `DrawSitExtras()`, and the call site was **moved after the shared content** — that step is what actually fixed "reading it feels like a jump": it used to be called before the stacking threshold.

One trap during the extraction: the first attempt counted braces naively, but the code contains strings like `string.Format("...{0}...")`, so the count drifted. The second attempt used a **character-by-character scan that skips strings and comments**, and it worked first time.

**Decoration and judgement glyphs removed, and made a CI rule**

Following mega-index-map's three invariants (output and vocabulary stay ASCII), every decoration and judgement glyph was removed: circled ticks, crosses, warning triangles, stars, radical signs, filled and hollow circles, squares, inequality signs, block elements, double-line borders, and circled numerals.

273 of them were in user-visible strings, 108 in documents, the rest scattered through the source and the installer.

**The exceptions kept** (written into the rule's comment): Chinese typographic marks (em dash, middle dot), mathematical signs (multiplication, plus-minus, minus, Delta), and all user-facing Chinese copy. The reasoning matches mega's exception for the README — this is a Chinese-facing project, and all-ASCII would cut out normal typesetting as well.

Rule (9) was added and is enforced on every push. The table is built from **code points** rather than literals, because during the cleanup a script once replaced the checker's own glyph table.

### Outstanding

**The panel layout is only half tidied.** The format is unified and the largest block is out, but `DrawDemandSection` is still 146 lines, and the section order within each tab has not been systematically reviewed.

**Recommendation**: do one tab at a time, compile and compare screenshots immediately after each change, and do not touch several tabs at once — the experience here is that layout changes go wrong easily at the parenthesis level.

### Found along the way

Running xcheck, (16) "current HP does not exceed the cap" reported one hard contradiction: `currentHP=692.09 maxHP=691.08`, about 1.01 over.

This was not caused by the change in hand, but the invariant states its own cause: the cap lock pins the cap back to the baseline every frame, and the drain's cap reduction pulls against it.

**But the smoothing added here (EaseToward) makes the phenomenon last longer** — it used to snap into place, now it approaches at a rate, so the current value stays above the cap for longer.

1.01 points on 691 is 0.15%, which does not affect function, but the invariant could be made more accurate: either relax the tolerance from 0.5, or apply the same smoothing to the cap. **Left to decide.**

---

## S7. Burger countdown: implemented, behaviour not measured

**Status: awaiting a real test**

The feature is in and the patch is confirmed mounted:

```
倒计时补丁已挂载（MenuControl.Timing 前缀：每单时长 / 速度）
挂载点 36 个（原 35）
BurgerTimeAll = 0    BurgerTimeSpeed = 100
set BurgerTimeAll 25   已设置并落盘
set BurgerTimeSpeed 40 已设置并落盘
```

**But the countdown has never actually been seen slowing down or freezing.**

Why: verifying it requires the game to be in the state "after an order was placed, while the burger is being made". At implementation time the game was sitting in the Rear combat state and could not enter that situation.

**How to verify** (next time in the game):
1. Set the panel to "loose (30 seconds / half speed)"
2. Play normally until an order is placed, and see whether the seconds in the corner start at 30 and tick slowly
3. Switch to "frozen" and confirm the seconds stop decreasing and no timeout is judged

**One theoretical risk to watch**: `BurgerTimeAll` writes `timeAll` in a per-frame prefix, while the game's `BuildMenu` does `if (resetTime) timeLeft = timeAll;`. If `resetTime` is false for some order, that order will not use the new duration. This cannot be settled from static code; it needs a real test.

---

## S8. The mode name in the middle of the screen always said "Demand" (fixed)

**Status: fixed and verified in play**

### Symptom

The banner in the middle of the screen, and the command replies, all said "索取模式" regardless of which tab was active. Fellatio should show Absorb, Riding should show Drain.

### Root cause

`DemandCmd()` had four **hard-coded** "索取模式" strings:

```
return "索取模式 = 开";
return "索取模式 = 关";
ShowChainBanner("索取模式", 3f);
return "已手动进入索取模式，持续 " ...
```

`ModeWord()` itself was correct (it returns 吸取/榨取/索取 by pose), but these four never called it. A plain omission.

### Fix

Added `ModeWordForBanner()`, and it uses **`_demandModePose` rather than the current pose**:

```csharp
int p = _demandModePose >= 0 ? _demandModePose : PoseIdx();
return ModeWordAt(p);
```

The reason: a mode can **persist across poses** (`_demandModePose` records the pose at entry), while `ModeWord()` uses the current pose — if the pose changes mid-mode, the banner would show a different word. All four call sites were switched to it.

### Measured

The game happened to be in the Fellatio pose:

```
[Fella·none] 吸取模式中 1 段 / 共剩余 73.6s
已手动进入吸取模式，持续 136.4s
吸取模式 = 关
```

Before the fix all three lines said 「索取」.

### Fixed along the way

The `defaults` command's text also hard-coded 「索取模式」, and that text is shared by the three tabs; changed to 「模式参数（三栏共用一份）」.

### Checked and found fine

- `DrawDemandSection(bool sitSide)` is called only by Rear and Riding; Fellatio does not go through it — so its `sitSide ? "榨取" : "索取"` two branches are correct, not a missing case
- The two chain-limit labels (riding · Demand mode / seated · Drain mode) are hard-coded too and that is fine, since each refers to one tab

---

## S9. The Manman hit area could move entirely off-screen (resolved)

**Status: resolved — the user confirmed it is fine now (both gates kept as a fallback)**

### Symptom

xcheck rule (23) once reported:

```
矩形 754~1194 x -114~82
```

x was normal but **y was negative** — the whole hit area was off-screen, not one pixel of it clickable. And the only reason that zone exists is to be clicked.

### Why the self-test did not catch it

(23) only checks that "the rectangle can be obtained" — it could indeed be obtained, it was just useless in position. **"Can be obtained" and "can be used" are two different things**, and the invariant covered only the former.

### Root cause (inferred)

The hit area is derived from the model's bounding box:

```csharp
float cy = my0 + SitPussyAreaCY.Value * mh;   // CY defaults to 0.62
float hh = SitPussyAreaH.Value * mh * 0.5f;
y0 = cy - hh;  y1 = cy + hh;
```

When the model is absent, `ModelRectCached` may return 0 or a negative width/height, so the derived rectangle is naturally garbage. The panel line "拿不到模型包围盒（人物不在场？）" is the hint for exactly this state.

### Fix

Two gates:

1. **degenerate detection**: `mw < 1f || mh < 1f` is immediately unusable
2. **intersect with the screen**: compute the on-screen part; less than 4px is unusable; if there is an intersection, return it (partly on-screen is still clickable)

Intersecting rather than rejecting outright because a partly off-screen hit area is still clickable; only a complete absence of intersection should be judged "unusable".

### What was not verified

**The target situation was not reproduced.** After deployment the game stayed out of the seated pose, and (23) reported "not needed (not seated or not enabled)".

Reproducing it needs the character in the seated pose with the model bounding box unavailable. **Next time, run xcheck while seated and see whether (23) still reports a negative number.**

---

---

## S10. Severity colouring, and (16)'s magnitude is actually large

**Status: the grading is implemented / (16)'s classification needs rethinking**

### Grading (implemented)

Hints and warnings run through five levels, smallest to largest:

```
white   no problem
blue    hint (lowest)
green   note
yellow  anomaly
red     severe (highest)
```

Implementation: a `Sev` enum + `SevColor()` + `SetHint(text, sev)`, wired into the status line at the bottom of the panel (a separate style, not reusing `Hint()`'s — reusing it would recolor every hint at once).

`CrossCheck()` grades itself by result when it finishes:

```
hard contradiction   → red
1 to 2 notes         → yellow
3 to 5 notes         → green
6 or more notes      → blue (too many notes means the checks need tidying)
all pass             → white
```

The text output carries the tag too (`[黄·异常]`), so the txt written to disk shows the level as well.

### Two self-inflicted bugs fixed along the way

**(1) A format string became a literal**

(16)'s detail used to display as:

```
currentHP={0:0.##} maxHP={1:0.##}{2}
```

Cause: while writing `string.Format` the braces were escaped as `{{0}}`, but that code was not an f-string, so `{{` went into C# as-is — and it became a literal.

**The fix: drop `string.Format` and use plain concatenation.** Without an escaping layer it cannot go wrong again.

**(2) `_hintStyle` name collision**

The grading wanted to reuse a style field, and that name was already taken by `Hint()`. More importantly: **reusing it would recolor every `Hint()`**. Changed to a separate `_statusStyle`.

### (16)'s magnitude needs a fresh judgement

What was seen earlier was **0.91** over, and on that basis it was called "0.2%, ignorable" and downgraded to a note at the user's request.

**But after the grading was in place, another run showed:**

```
currentHP=394.09 maxHP=328.41 ← 65.68 over (20.0% of the cap)
```

**20%, not 0.2%.** A factor of 100.

So that "known to report, ignore it" classification **may be wrong** — with the cap cut to 328 while the current value sits at 394, the health bar visibly overflows.

**The overrun varies** (0.9 to 65 have all been seen), which means it depends on the race between the cap reduction rate and the smoothing, not a fixed small offset.

**Needs the user's fresh judgement**: if the 20% case is common, this should not be treated as "harmless" but fixed at the root (clamp `curHp` to `maxHp` every frame so the state cannot exist at all).

### Another observation: the Manman zone hugged the bottom edge of the screen

The rectangle in this run was `754~1194 x 0~86`, **y starting at 0**.

The bottom 0~54 of the screen is the game's **view-drag band** (`edgeRatio = 0.05`), so half of that rectangle lay on the drag band — clicking its lower half drags the view instead.

This is not necessarily a defect (a different character position gives a different rectangle), but it is worth recording: **when a hit area hugs an edge, watch for overlap with the drag band**.
