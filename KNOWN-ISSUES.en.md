# Known issues (still open)

> **Only unsolved problems live here.**
> Each one's history (symptom / root cause / fix / lesson) is in
> [`ENGINEERING-NOTES.md`](ENGINEERING-NOTES.md) under the same entry number;
> version-level changes are in [`CHANGELOG.md`](CHANGELOG.md).

**Maintenance rule**: once an issue is fixed, **delete it from here** (the write-up
moves into the engineering notes) and leave only a one-line summary in `CHANGELOG.md`.

> This file was once rolled back to the old 47-entry full list **twice**, because the
> working copy was copied over the release copy. New content goes into the
> **release copy** only; sync with `tools/sync.sh docs`.

---

## At a glance

| # | Issue | Status |
| --- | --- | --- |
| 3 | The semantics of `EffectiveSpeed` are unconfirmed | Waiting on hands-on judgement |
| 5 | Features with zero real-play verification | Waiting on feedback — they work, but there is not enough use yet to tune them |
| 16 | The seated pose still displaces demand mode (fix #15 was not enough) | A state-level fallback is in place; waiting on real-play verification |

The numbers match [`ENGINEERING-NOTES.md`](ENGINEERING-NOTES.md).

> **Entries 3 and 16 both carry a disclaimer**: neither problem may trigger
> deterministically, so they are hard to reproduce reliably; **the game's own
> source quality is also a possibility worth weighing** — the symptom may be
> something the game does in a particular state, with the plugin merely passing
> through. Until there are stable reproduction steps, changing the plugin logic
> for them is not advisable.

---
## 3. The semantics of `EffectiveSpeed` are unconfirmed

**Status: waiting on hands-on judgement**

> **Disclaimer: this issue may not trigger deterministically.**
> In other words it is hard to reproduce reliably and costs more to investigate than
> an ordinary defect — **and one possibility must be kept in mind: the game's own
> source quality is not high**, so the symptom may be something the game produces in
> a particular state, with the plugin merely passing through. Until stable
> reproduction steps exist, changing the plugin logic for it is not advisable.

The climax animation speed goes through Animancer's `EffectiveSpeed`. Setting 800%
was measured as **3.38**, not 8.

That may be the property's normal semantics (normalised by clip length), or something
else may be clamping it. **It can only be settled by feel**: if 800% is fast enough,
leave it; if it feels slow, use `Speed` instead (more direct), or set both layers.

---

## 5. Features with zero real-play verification

**Status: waiting on feedback — they work, but there is not enough use yet to tune them**

> This is not "not built yet", it is "built, but the sample is too small". Which values
> are right and which feel wrong only becomes clear when someone uses them in real
> play. If something feels off while using it, report it.

The following paths **have patches hooked but have never appeared in the log**
(as of the last census):

| Feature | Log tag |
| --- | --- |
| The whole seated "Drain mode" chain | `[榨取]` |
| "Return accumulation" + holding the seated pose | `[回归]` |
| Demand urge accumulated from seated / fellatio ejaculation and absorb | `坐姿吸精` / `坐姿射精` / `口交吸精` / `口交射精` |
| The new afterglow loop (accumulate → fire once settled → hold the pose until cleared) | `[余韵]` |

**`_sitDeferred` is the most suspicious one** — only `Show_CenterGirlSit` has a prefix
interceptor; if the game enters the seated pose by another path, it slips through.

### There is a trap when using the log to decide "has this ever run"

**The demand-urge source log lines (`骑乘位吸精` / `坐姿射精` / `口交吸精` and similar)
are only written while `urgeaudit` is on** (`AuditUrge` starts with
`if (_urgeAuditLeft <= 0) return;`). Looking at the log after the audit budget is used
up will make it look as though "this path never ran".

This has actually happened: `[榨取]` (seated ejaculation does not slow down) appeared
once, which proves `Event_SitSyaseiStart` fired and its postfix ran — yet
`NoteUrgeEvent("坐姿射精", ...)` in the same postfix left no trace at all, because the
audit had already switched off.

**To verify these sources, run `urgeaudit 200` before playing.**

### 2026-10-07 census

| Feature | Log tag | Status |
| --- | --- | --- |
| Seated drain mode (seated ejaculation does not slow down) | `[榨取]` | **ran once** |
| Afterglow loop | `[余韵]` | ran 5 times |
| Return accumulation + holding the seated pose | `[回归]` | **still 0** |
| Seated / fellatio ejaculation and absorb accumulation | — | needs `urgeaudit` to judge |

---

## 16. The seated pose still displaces demand mode (fix #15 was not enough)

**Status: state-level fallback added, waiting on real-play verification**

> **Disclaimer: this issue may not trigger deterministically.**
> In other words it is hard to reproduce reliably and costs more to investigate than
> an ordinary defect — **and one possibility must be kept in mind: the game's own
> source quality is not high**, so the symptom may be something the game produces in
> a particular state, with the plugin merely passing through. Until stable
> reproduction steps exist, changing the plugin logic for it is not advisable.

**Symptom**: after fix #15 (an `if (_demandMode) return false;` at the entry point),
**the seated pose can still take over**.

**Why the entry point cannot stop it — the state change is a delayed coroutine**

```csharp
private IEnumerator _CenterGirl(int i)
{
    yield return new WaitForSeconds(effect.GlitchEffect(0.1f, 0.01f, 0.1f));   // ← waits 0.1 s first
    model.CenterGirlParts(i);
    switch (i)
    {
    case 0: centerGirlState = CenterGirlState.Fella; break;
    case 1: centerGirlState = CenterGirlState.Sit;   break;   // ← only now does it switch
    case 2: centerGirlState = CenterGirlState.Osiri; break;
    }
}
```

`Show_CenterGirlSit()` does `StartCoroutine(_CenterGirl(1))`. **If it was already
called and the coroutine already queued before we intercepted, then 0.1 s later it
switches the state to Sit anyway** — a prefix on the entry point cannot stop a
coroutine that is already queued.

**The fix: a state-level fallback**

In `TickDemand()`, check every frame:

```csharp
if (_demandMode && centerGirlState == "Sit")
{
    Log.LogInfo("…模式进行中，状态却被切成坐姿 → 夺回骑乘位");
    ShowCenterGirlOsiri();      // take it back
}
```

**Why "correcting" is appropriate here**: the project has a rule that says "do not
auto-correct unless the abnormal state can be told apart from a normal transient".
Here **`_demandMode && state == Sit` is a combination that can never be legal** — it
can be judged with certainty rather than guessed at, so acting on it is right.

**Call paths** (all three go through `Show_CenterGirlSit`, so the entry prefix catches
them):

| Line | Source |
| --- | --- |
| 2201 | story script |
| 2245 | developer hotkey **Z** |
| 10714 | seated-appearance timer (the 10% roll after `SitGirlStartTimer` expires) |

**But what actually takes effect is that delayed coroutine**, so both layers are
needed: the entry point and the state check.

**Verification note**: this fallback **has not been verified in a real scenario** —
you have to play until the seated pose appears. If "夺回骑乘位" appears in the log,
the fallback worked.

---

## Counts

| | Entries |
| --- | --- |
| **Open (this document)** | **3** |
| Closed (write-ups in the engineering notes) | see the table of contents of `ENGINEERING-NOTES.md` |

All 3 remaining issues **need real use to move forward**; the code alone cannot settle
them:

- **3** `EffectiveSpeed` semantics unconfirmed — waiting on hands-on judgement
- **5** Features with zero real-play verification — waiting on feedback — they work, but there is not enough use yet to tune them
- **16** The seated pose still displaces demand mode (fix #15 was not enough) — state-level fallback added, waiting on real-play verification

> `7` (seated ecstasy anomaly) and `S9` (Manman hit area off-screen) were confirmed
> fine by the user in real play; `14` (two call warnings) had its root cause found and
> fixed — all three were removed from this document on 2026-10-13.
> The intermittent nature of `14` means "one run without a repro" does not count as
> verification, which the write-up states explicitly.
