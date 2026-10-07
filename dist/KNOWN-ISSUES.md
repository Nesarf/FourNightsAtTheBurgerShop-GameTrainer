# 已知问题（当前未解决）

> **这里只放"现在还没解决"的问题。**
> 已经修好的（含根因与教训）全部在 [`ENGINEERING-NOTES.md`](ENGINEERING-NOTES.md)，
> 版本级变更看 [`CHANGELOG.md`](CHANGELOG.md)。

## 3. `EffectiveSpeed` 的语义未确认

**状态：待用户手感判断**

绝顶动画倍速走的是 Animancer 的 `EffectiveSpeed`。设 800% 时实测读到 **3.38**，
不是 8。

可能是这个属性的正常语义（按片段长度归一化），也可能被别处限了。
**只能靠手感判断**：倍速拉到 800% 够快就不用管；偏慢则改用 `Speed`（更直接），或两层都设。

---

## 5. 零实战验证的功能

**状态：待玩**

以下路径**挂了补丁但日志里一次都没出现过**（截至最后一次盘点）：

| 功能 | 日志标签 |
| --- | --- |
| Sit「榨取模式」整条链路 | `[榨取]` |
| 「回归累积」+ 坐姿挂起 | `[回归]` |
| 坐姿 / 口交的射精吸精累积索取欲 | `坐姿吸精` / `坐姿射精` / `口交吸精` / `口交射精` |
| 余韵的新闭环（累积 → 稳定后发动 → 清空前不换姿势） | `[余韵]` |

**`_sitDeferred` 那条最可疑** —— 我只在 `Show_CenterGirlSit` 上加了前缀拦截，
游戏若从别的路径进坐姿就漏了。

### ⚠ 用日志判断"有没有跑过"时有个陷阱

**索取欲的来源日志（`骑乘位吸精` / `坐姿射精` / `口交吸精` 之类）只在
`urgeaudit` 开着的时候才打**（`AuditUrge` 开头就是 `if (_urgeAuditLeft <= 0) return;`）。
审计条数用完后再看日志，会误判成"这条路径没跑过"。

实测踩到过：`[榨取]`（Sit 射精不减速）已出现 1 次，说明 `Event_SitSyaseiStart`
确实触发了、它的后缀也跑了，而同一个后缀里的 `NoteUrgeEvent("坐姿射精", ...)`
却因为审计关了而没留下任何痕迹。

**要验证这些来源，先 `urgeaudit 200` 再玩。**

### 2026-10-07 复盘点

| 功能 | 日志标签 | 状态 |
| --- | --- | --- |
| Sit 榨取模式（Sit 射精不减速） | `[榨取]` | **已跑通 1 次** ✓ |
| 余韵闭环 | `[余韵]` | 已跑通 5 次 ✓ |
| 回归累积 + 坐姿挂起 | `[回归]` | **仍为 0** |
| 坐姿 / 口交的射精吸精累积 | — | 需开 `urgeaudit` 才能判定 |

---

## 7. Sit 的索取模式会导致绝顶值变化异常

**状态：用户要求先记录、暂不改**

**症状**
坐姿下开着索取模式时，绝顶值会出现异常跳变。

**现场证据** —— `watch.csv`（109 个 Sit 样本），抓到的突降：

```
t=...947  attacking  绝顶=190.781/199.878  段=10  Syaseing=0
t=...951  attacking  绝顶=  8.922/199.878  段=10  Syaseing=0   ← 一步掉了 181.9
t=...954  attacking  绝顶= 10.960/199.878
t=...958  attacking  绝顶=  6.541/199.878
t=...962  attacking  绝顶= 15.543/199.878   ← 之后在低位缓慢爬升
```

几个值得注意的点：

- **跳变时 `Syaseing=0`** —— 不是榨取造成的
- 跳变前是 190.781，**没到上限 199.878**，也不是"到顶触发"
- 跳变后的形态（6~25 之间起伏、缓慢爬升）**很像「清零振荡」在跑** ——
  而清零振荡的入口正是 `Prefix_EcstasyReset`。所以大概率是
  **某次 `EcstasyReset` 被触发 → 清零振荡接管 → 值被压在低位**
- `段=10` —— 索取模式已叠到上限（`DemandMaxStacks`）并停在那里
- `HP` 在 199.5 ↔ 287.3 之间来回 —— 那是动摇的 HP 加成的施加/还原，正常

**留存证据的完整统计**（`watch-archive/watch-20261007-211819.csv`，167 样本）

```
姿势分布          {'Osiri': 34, 'Sit': 133}
子状态分布        {'Osiri/attacking': 30, 'Osiri/kyusei': 4, 'Sit/attacking': 133}
Syaseing 分布     {'0': 162, '1': 5}
绝顶值最大单步跌幅 −195.1   ← 上限是 199.878，等于整条上限被抹掉
索取欲范围        0.0 ~ 1487.2
段数分布          {0:45, 1:23, 2:50, 5:1, 10:48}
```

那个 −195.1 的跌幅**几乎等于把绝顶值整条清零**，比单纯"掉一截"更值得注意 ——
它更像**一次完整的 `EcstasyReset(≈1.0)`**，而不是损耗。

**待查方向**

1. **谁调了 `EcstasyReset`** —— 在 `Prefix_EcstasyReset` / `Prefix_EcstasyResetNoArg`
   里加日志（打出来源与调用栈），确认是游戏自己调的、还是索取模式逻辑引发的
2. **清零振荡是否该在索取模式中接管** —— 现在它只挡了 `Syaseing`，没挡 `_demandMode`；
   索取模式中绝顶值本该由连榨的软上限管，两者可能打架
3. **`段=10` 长时间不动** —— 与第 1 条同源（只有打屁股能叠），但这里已经满了

---

## 14. 两条既有的调用警告（低优先级）

**状态：待观察**

日志里偶发：

```
[Warning] 调用 HPGaugeChangePercent 失败：Exception has been thrown by the target of an invocation.
[Warning] 调用 EcstasyGaugeChangePercent 失败：Exception has been thrown by the target of an invocation.
```

这是插件反射调用游戏方法时抛的（内层异常被 `TargetInvocationException` 包住，
所以看不到真实原因）。推测是在**不合适的时机**调用（例如 UI 槽位还没初始化、
或不在店内场景）。**目前没观察到实际后果**，先记着。

---

## 16. 坐姿仍然顶掉索取模式（第 15 条修完还不行）

**状态：已加状态级兜底，待实战验证**

**症状**：第 15 条修完（入口处 `if (_demandMode) return false;`）之后，**坐姿依然能顶上来**。

**为什么入口拦不住 —— 切状态的是延迟协程**

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

`Show_CenterGirlSit()` 里是 `StartCoroutine(_CenterGirl(1))`。
**如果它在我拦下之前就已经被调用过、协程已经排队，那 0.1 秒后它照样把状态切成 Sit** ——
拦入口（前缀）挡不住已经排进队列的协程。

**修法：状态级兜底**

在 `TickDemand()` 里每帧检查：

```csharp
if (_demandMode && centerGirlState == "Sit")
{
    Log.LogInfo("…模式进行中，状态却被切成坐姿 → 夺回骑乘位");
    ShowCenterGirlOsiri();      // 夺回来
}
```

**为什么这里做"纠正"是恰当的**：
项目有条规矩是"不要自动纠正，除非异常能与正常瞬态区分开"。
这里 **`_demandMode && state == Sit` 是绝不合法的组合** —— 可以明确判定，
不是"猜着纠正"，所以做纠正动作是对的。

**调用路径汇总**（这三处都走 `Show_CenterGirlSit`，入口前缀能拦）：

| 行 | 来源 |
| --- | --- |
| 2201 | 剧情脚本 |
| 2245 | 开发者快捷键 **Z** |
| 10714 | 坐姿出现计时（`SitGirlStartTimer` 到点后的 10% 掷骰） |

**但真正生效的是那个延迟协程**，所以入口 + 状态两层都要有。

**验证说明**：本条的兜底**尚未在真实场景验证** —— 需要玩到坐姿出现时刻。
日志里若出现「夺回骑乘位」即说明生效。

---

## 33. 第 7 条（Sit 绝顶值异常）改为「疑似已修」+ 加了复发警报

**状态：🟡 疑似已修（2026-10-07）—— 用户判断 + 已加自动侦测**

**用户**：「7 不一定存在，可能被修好了」

**支持这个判断的理由** —— #7 记录的症状是「Sit 模式下绝顶值单步掉 −195.1」，
而之后这几条修复**正好都命中它的可能成因**：

| 修的东西 | 为什么可能治到 #7 |
| --- | --- |
| `DrainSlotIsDemand()`（#13） | 连榨档位选错 → 会走错的软上限 |
| `_demandModePose`（#27） | 模式中途被拽去 Osiri → 状态机错乱 |
| `ModeActive()`（#12） | 正骑滑块在束缚之吻里不生效 → 设定的值与实际不符 |
| Sit 混合器修正（#26） | 速度冲量之前根本没作用到坐姿 |
| `ChainSoftCap` / `AfterglowSoftCap` | 直接就是"绝顶值该压在哪"的老师 |

**但"可能修好了"不算结论** —— 所以加了自动侦测（`EcstasyDropWarn`，默认 50）：

每帧比对 `PlayerControl.CurrentEcstasy`，掉幅超阈值就记一条**带完整上下文**的警告：

```
[绝顶骤降 #1] 262.8 → 131.4（Δ-131.4）  姿势=2  模式=False(入于-1)  段=0
  sitState=attacking  kissing=0  osiriState=none  Syaseing=1  余韵剩=0  连榨=0
```

**第一次运行就抓到了两次 —— 但那是误报**

```
[绝顶骤降 #1] 262.8 → 131.4（Δ-131.4）  Syaseing=1
[绝顶骤降 #2] 131.4 → 0（Δ-131.4）      Syaseing=1
```

`Syaseing=1`（玩家正在射精）时绝顶值被**正常消费**掉，而且正好**两次减半** ——
这是设计行为，不是异常 ✗

**已排除**：

```csharp
if (GetFloat(pl, "Syaseing") > 0.5f) { _lastEc = cur; return; }   // 射精期间的消费不算异常
```

**于是判据变精确了**：要抓的是**"没在射精却骤降"**。

**怎么用它判 #7 到底还在不在**

玩一段 Sit 榨取（含连榨与余韵），然后搜日志：

```bash
grep "绝顶骤降" BepInEx/LogOutput.log
```

- **搜到（且当时不在射精）** → #7 还在，日志里带着完整上下文，可以直接定位
- **搜不到** → 可以认定已修

**教训**

> **"可能已经修好了"是个合理假设，但必须配一个能被证伪的检查手段**，
> 否则它只会在下一次复发时以"咦怎么又出问题了"的形式重新出现。
> 这次的检查手段做对了：**它会自己去抓，而不是等人去盯。**

---

---

## 统计

| | 条数 |
| --- | --- |
| 未解决 | **6** |
| 已解决 | 34 |

已解决的那些见 `ENGINEERING-NOTES.md`。

---

## S1. 静态检查报出的三项小缺口

**状态：待处理** —— 由 `tools/static-check.py` 自动发现（已接入 CI）

这三项**不会让功能失效**，但都是"某处没跟上"的不一致，
所以单独列出来而不是塞进工程笔记。

### ① 6 个参数没有 `set` 命令分支

```
ChainEcstasyGain / ChainGainPerDrain / ChainGainPerHp
ChainSoftCap / ChainWobble / ChainWobbleHz
```

面板里能调 ✓，但命令行 `set ChainSoftCap 70` 会报「未知配置项」✗
补法：在 `set` 的 switch 里各加一行 `case "X": SetP3(X3, f); break;`。

### ② 说明书的 4 处组合提及

```
AfterglowSpeedHz  → 说明书里写作 "AfterglowSpeedWobble / Hz"
AttackUrgeGain    → "AttackUrgeChance / Gain / Jitter"
KyuseiUrgeGain    → "KyuseiUrgeChance / Gain"
SyaseiUrgeGain    → "SyaseiUrgeChance / Gain"
```

**功能上已覆盖** ✓ 只是字面匹配不到。可改写成完整名，或让检查器认这种写法。

### ③ 2 个共享模板里混进了"榨取"

```
ChainGainPerHp / AfterglowPerSyasei
```

`PoseDesc` 会把"索取"按栏位替换成"吸取"/"榨取" ✓，
但**反向不成立** —— 模板里写死的"榨取"在**背榨**栏不会变成"索取" ✗

补法：把这 2 处描述里的"榨取"改成"索取"（或改成 `{模式}` 占位符）。
