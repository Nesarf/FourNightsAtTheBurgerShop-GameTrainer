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

**状态：✅ 已全部修复（2026-10-07）** —— 由 `tools/static-check.py` 自动发现（已接入 CI）

这三项**不会让功能失效**，但都是"某处没跟上"的不一致。**现已全部修复**，正文留档。

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

---

## S2. 静态检查现在 0 失败 0 提醒

修复 S1 之后：

```
① BindP3 参数：54 个
  [ OK ] ② 全部 54 个参数都有面板控件
  [ OK ] ③ 全部 54 个参数都有 set 命令分支
  [ OK ] ④ 全部 54 个逻辑参数都在说明书里
  [ OK ] ⑤ 补丁方法无重复定义（共 55 个）
  [ OK ] ⑥ 无本机绝对路径
  [ OK ] ⑦ 三栏用词由 PoseDesc 统一归一
  [ OK ] ⑧ 挂载点 35 个
结论：0 项失败，0 项提醒
```

**第 3 条（共享模板里写死"榨取"）是真 bug，修法值得记**：

原来 `PoseDesc` 是**单向**替换 —— 只把"索取"按栏位改写 ✗
所以模板里本来写着"榨取"的那 2 条，在**背榨**栏会显示成"榨取" ✗

改成**先归一成占位符、再按栏位填**：

```csharp
d = d.Replace("榨取", P).Replace("吸取", P).Replace("索取", P);
d = d.Replace(P, ModeWordAt(i));
```

**三个方向都成立** ✓ 模板里写哪个词都无所谓了 ✓

**连带作废了一条检查规则** —— ⑦ 原来是防这个 bug 的，
bug 修掉后它就没意义了。**但我把规则改成了注释说明而不是直接删掉** ✓
理由是：下一代人看到"这里本来有条检查"时，能直接知道**它为什么被删**，
而不是怀疑是被误删的 ✓

**实测验证**（配置里三栏的描述）：

```
【口交·吸取】…【按这次吸取对生命值的影响】折算的比例（%）
【背榨·索取】…【按这次索取对生命值的影响】折算的比例（%）
【正骑·榨取】…【按这次榨取对生命值的影响】折算的比例（%）
```

---

## S3. ✅ 配置自动迁移系统（已完成）

**状态：✅ 已实施并实测通过（2026-10-07）**

原来升级会**静默丢调参** —— 旧版是"全局单份键"，新版是"按姿势三份"，
老用户升级后参数全回落默认，而且**没有任何提示** ✗
实测踩过**两次**：31 个参数那次丢了 48 个调过的值；最后 3 个那次丢了 `DemandFellaDecay=59.78`。
两次都是手工改文件救回来的 ✗

### 做法

启动时检查 `ConfigVersion`（默认 0 = 没迁过）：

```
旧配置（单份键）
   ↓ 解析 cfg 原文（旧键已不 Bind，读不到内存值）
   ↓ 找"旧键存在 + 三份副本都在"
   ↓ 只在【三份副本完全一致】时才搬 —— 用户已调过的绝不覆盖
   ↓ 备份为 *.bak-before-v1
   ↓ 逐键写入三份
   ↓ ConfigVersion 置 1
   ↓ 日志列出搬了什么
```

### 实测

```
测试配置：SpankSpeedGain = 77（三份副本都是默认 12）

结果：
  SpankSpeedGain_Fella = 77
  SpankSpeedGain_Osiri = 77
  SpankSpeedGain_Sit   = 77
  ConfigVersion: 1
  ════════ 配置自动迁移 ════════
  共迁移 105 个键
```

### 过程中修的两个问题（都值得记）

**① 版本号无条件推进 —— 比不迁移更糟** ✗

第一版是 `ConfigVersion.Value = CURRENT;` 无条件执行 ✗
测试时它"跑了但没搬成"，版本号却被推到 1 →
**以后永远不会再试** ✗✗✗ 用户以为迁过了，实际参数全回落默认 ✗

改成 **`if (moved.Count > 0) ConfigVersion.Value = CURRENT;`** ✓
没搬成就不推进 → 下次启动还会再试（开销可忽略）✓

> **"标记为已完成"和"真的完成了"必须分开。**
> 无条件打勾的状态机，比没有状态机更危险。

**② 反射查 key 查不到 —— 靠诊断日志才发现** ✗

原来用 `g[0].Definition.Key == baseName` 查找三份组 ✗
那个属性拿到的**不是配置键** → 41 个组全查不到 → 迁移静默地一个键都没搬 ✗

**是加了诊断日志才看出来的**：

```
迁移：找不到三份组 AfterglowUseDemandGain（已注册 float 组 41 / int 4 / bool 9）
```

改成**在 `BindP3` 绑定时就登记名字索引**（`_p3ByName`）✓
不再依赖反射去猜 —— 而且日志里把"注册了几个"打出来，下次同类问题一眼可见 ✓

> **"静默失败"要靠"把中间状态打出来"才能变成"可见失败"。**
> 这次如果没加那三条诊断日志，我大概率会以为是别的原因。

### 文档同步

`使用说明.md` 里原来写"**不会自动搬，需要手动复制**" ✗ —— 现在**这句话是错的了** ✓
已改成"会自动迁移"并说明备份位置与"已调过的不覆盖" ✓

> **功能变了，文档没跟上，就等于文档在骗人。**

---

## S4. ✅ 游戏绑定层 GameBindings（已完成）

**状态：✅ 已实施并实测通过（2026-10-07）**

### 要解决的问题

插件通过 Harmony 按**方法名**打补丁，而游戏里同一个方法名可能属于不同的类 ✗
挂错了**不会有任何报错** —— Harmony 只是静静不生效 ✗

这个坑在这个项目里踩了**五次**：

```
Osiri叩かれる()            → TabemiControl
Event_OsiriSyaseiStart()   → Live2D_Animation_SitOsiri（不是 Live2D_AnimationControl）
DealDamage_Fella()         → TabemiControl
Event_SitKissSyaseiOnEnd() → Live2D_Animation_SitOsiri（不是 TabemiControl）
GetTouchTargetName()       → Live2D_HitAreaCheck（不是 HitAreaCheck）
```

### 做法

新增 `plugin/GameBindings.cs`：把"方法属于哪个类"变成**一张可校验的表**。

启动时 `VerifyAll()` 逐条确认：

1. 方法**在不在**它该在的类上
2. 如果不在 —— **看看它是不是跑到别的已知类上去了** ← **这就是"挂错类"的自动检测** ✓
3. 把结果汇总成一条日志

```
[绑定] 30 个补丁目标全部校验通过
```

出问题时会是：

```
[绑定] ★挂错类：Event_SitKissSyaseiOnEnd 不在 TabemiControl，而是在 Live2D_Animation_SitOsiri
[绑定] 有 1 个方法挂错了类 —— 这些补丁不会生效，功能静默失效
```

### 为什么不追求"全部集中"

**只收录 30 条**（那些曾经挂错或容易挂错的跨类同名方法），不做全量搬迁 ✗

理由：全量搬迁要改动几乎每一处反射调用，属于高风险大重构 ✗
**而这 30 条恰好就是真正出过问题的那一类** ✓ —— 用 20% 的改动覆盖 80% 的风险 ✓

新增的 `bindings` 命令可以随时打出全表：

```
[OK]   TabemiControl.ShowCenterGirlOsiri   — ★ 方法体内直接赋值 centerGirlState
[OK]   Live2D_Animation_SitOsiri.Event_SitKissSyaseiOnEnd   — ★ 不在 TabemiControl
[OK]   Live2D_HitAreaCheck.GetTouchTargetName   — ★ 类名是 Live2D_HitAreaCheck
```

**排错时不用再去翻反编译代码找"这个方法到底属于谁"** ✓

### 未验证的部分（如实说明）

**"挂错类"的报警路径本身没有实测过** ✗ —— 要验证它得故意把一个方法挂到错误的类上 ✗
编号逻辑是直读的（`GetMethod` 返回 null → 去别的类找 → 找到就报），
但**没有跑过真实的误配场景** ✓

### 教训

> **"挂错类静默失败"这类问题，正解不是"记住正确的类名"**，
> **而是把它变成机器能检查的东西** ✓
> —— 人记不住 30 个映射，但启动时校验一次是零成本的。

---

## S5. ✅ 配置 Schema 化 · 第一块：`set` 命令的通用路径

**状态：✅ 已实施并实测通过（2026-10-07）**

### 问题（评审 ① 的核心）

评审说"每增加一个配置项都要碰 6~8 个地方"。这个数字在 `set` 命令上是**具体的**：

**141 个手写 `case` 分支** ✗ —— 其中 54 个是"按姿势三份"参数的 ✗

漏写一个的后果是：**面板能调、命令行却报「未知配置项」，而且没有任何报错** ✓
（S1 那三条就是这么发现的 ✓）

### 做法

不重写整个配置系统 ✗ —— 只做**收益最直接的一块**：

`_p3ByName` 这个索引在 `BindP3` 绑定时就已经登记好了 ✓
在 `SetCfg` 的 `switch` **之前**加一段通用处理：

```csharp
object g;
if (_p3ByName.TryGetValue(name, out g))
{
    // 按实际类型分派：float / int / bool
    ...
    _pluginInstance.Config.Save();
    return name + " = " + val + "  （已设置并落盘·按姿势三份）";
}
switch (name) { /* 141 个手写 case …… */ }
```

**于是新增一个 `BindP3` 参数，`set` 命令自动支持** ✓ 不用再碰那个 switch ✓

### 实测

```
set DemandAttBonus         → （已设置并落盘·按姿势三份）   ← float
set ChainSoftCap           → （已设置并落盘·按姿势三份）
set SpankSpeedGain         → （已设置并落盘·按姿势三份）
set DemandEnabled 42       → （已设置并落盘·按姿势三份）   ← bool 也走通
set AfterglowPerSyasei     → （已设置并落盘·按姿势三份）   ← int 也走通
set GodMode 1              → （已设置并落盘）              ← 非 P3 仍走老路径 ✓
set TremorAmplitude 8      → （已设置并落盘）
```

**返回消息里那句「·按姿势三份」就是判据** —— 一眼能看出走的是哪条路径 ✓

### 为什么保留那 54 个死 case

它们在 switch 之前就被拦截了 ✓ 已是死代码 ✓

**不删的理由**：删 54 行属**纯清理、风险为零、收益也为零** ✗
留着还能当"这批参数长什么样"的活文档 ✓

> **重构时"顺手删掉没用的东西"看起来像在做正确的事，
> 但在一个有 40 条历史坑的项目里，稳妥比整洁重要。**

### 下一步（Schema 化还没做完）

| 还能自动化 | 现在是什么状态 |
| --- | --- |
| `set` 命令 | ✅ **本次完成** |
| 面板控件 | 手写（54 个 `SetP3(..., SliderF(...))`） |
| 描述模板 | 手写（`BindP3` 的第 3 个参数） |
| 拷贝列表 | 已自动化（`CopyPoseSettings2` 遍历 `_p3Groups`）✓ |
| 静态检查 | 已自动化 ✓（能查出"漏了面板/set/文档"）✓ |

**面板和描述仍是手写** ✗ —— 但那两块**静态检查能兜住** ✓
真要自动化的话，收益不如"把静态检查再扩几条" ✗
