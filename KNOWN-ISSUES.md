# 已知问题

记录已确认但**暂不修**的问题。修之前请先读「状态」那一行，别自作主张动它。

---

## 1. 攻击累积的索取欲不会触发第二段及以后的叠层

**状态： 已修（2026-10-07）**

**症状**
角色**攻击**累积起来的「索取欲」不会触发索取/榨取模式的第 2 段及以后的叠层。
索取欲会一直涨（实测到 314%），但段数长期停在 1。

**根因**
叠层判定**只存在于打屁股那条路径**里：

| 路径 | 做什么 |
| --- | --- |
| `DoSpankLogic()`（打屁股） | 加值 → **评估门槛 → `EnterOrStackDemand()`** |
| `Postfix_OsiriGirlMainMixer` / `Postfix_SitGirlMainMixer`（攻击） | **只加值**，从不评估门槛 |

所以攻击只能把进度条填满，**消费进度条的只有打屁股这一下**。这是有意设计
（让玩家能控制节奏），但表现上就是"第 2 段以后叠不上去"。

**证据** —— `BepInEx/l2d_dump/cmd/watch.csv`（`tools/watch.sh` 的产出）

```
t,pose,subState,...,urge,stacks,demandLeft,...
1791378463,Osiri,attacking,...,253.6,1,72.6,...
1791378471,Osiri,kyusei,   ...,275.9,1,65.2,...
1791378474,Osiri,kyusei,   ...,314.1,1,61.6,...
```

该用户 `DemandThresholdBase=100` / `Step=49.78`，第 2 段需 149.8%，
索取欲早已越过很多，`stacks` 始终为 1。

**修法（已实施）**

把叠层判定抽成唯一实现 `TryStackNow(source)`，**所有累积来源共用**：

| 来源 | 累积 | 叠层判定 |
| --- | --- | --- |
| 打屁股 `AccumulateAndMaybeStack(..., applyEscapePenalty: true, ...)` | 是 |  |
| 坐姿任意点击（修 #6 新增） | 是 |  |
| 角色攻击（骑乘位 / 坐姿） | 是 |  **← 本次补上** |

开关：`AttackCanStackDemand`（默认开）。关掉就回到"只有打屁股能叠"的旧行为。

**验证**（`urgeaudit` 现场日志）：

```
攻击命中 +23.45 → 115.82%
叠加·清零 -115.82 → 0%              ← 角色攻击触发的叠加
[索取] 叠加第 6 段（角色攻击），+91.1 秒
[索取] 叠加第 7 段（角色攻击），+113.8 秒
[索取] 叠加第 8 段（角色攻击），+101.3 秒
[索取] 叠加第 9 段（角色攻击），+145.6 秒
[索取] 叠加第 10 段（角色攻击），+82.2 秒
```

---

## 2. 配置并发写的合并路径未干净验证

**状态： 已验证通过（2026-10-07）—— 用 `tools/cfgmerge-check.sh` 一键跑**

**做法**：脚本做了三件事 ——
(1) 先等 8 秒确认**没有别的写入者**（不安静就直接退出，不给你假结果）
(2) 让两个写入者改**不同的键**：游戏侧走 `set` 命令（会触发 `Config.Save` 整份重写），
   外部侧同时直接改文件
(3) 验**两边都还在**；任一被覆盖就 FAIL，并自动还原起始配置

**实测**

```
 安静（8 秒内无写入）
当前姿势：正骑（set 会写 _Sit 那一份）
起始：AttackUrgeChance_Sit=20  SpankSpeedGain_Sit=20
结束：AttackUrgeChance_Sit=27  SpankSpeedGain_Sit=29.0
 游戏侧的改动保住了
 外部侧的改动保住了
结果：PASS —— 两边都没丢，合并路径成立
```

**脚本自己先给我上了一课**：第一版写成 `FAIL`，我差点当成产品缺陷报出去 ——
实际是**测错了键**：`set` 命令按**当前姿势**写那一份（`PIdx` 用 `PoseIdx()`），
而当时姿势是正骑，我却在验 `_Osiri`

> **凡是走 `set` 的测试，必须先问当前姿势、再决定验哪个键** ——
> 三份参数带来的一个新陷阱：**同一个逻辑名，落在哪个物理键上取决于当时的姿势。**
> 脚本现在会先跑 `states` 问出来，并在输出里写明"set 会写 _XXX 那一份"。

**用法**（找一个不调参的安静窗口）：

```bash
bash tools/cfgmerge-check.sh
```

`CfgGuard`（修订号 + 锁 + 逐键合并）的**检测**部分验证过 —— 日志能打出
「检测到外部改动，已按文件合并」。但**"并入 N 项"从未成功出现过**：
实测时外部写入 `DemandAttBonus = 777` + 递增 rev，游戏内 `set` 之后
该值仍被写回 125。

**当时无法干净复现的原因**：测试期间用户正在用面板实时调参，
他自己的写入不断覆盖我的测试写入。

**下次要在「用户不动配置」的安静时段重测。** 复现步骤：
1. `bash tools/cfgsnap.sh save`
2. 记下 `DemandAttBonus` 的当前值
3. 用脚本把文件里的 `DemandAttBonus` 改成 777，并把 `<cfg>.rev` 加 1
4. 游戏内 `set AttackUrgeJitter <另一个值>`
5. 看 cfg 里 `DemandAttBonus` 是否保住 777，以及日志有没有「并入 N 项」

---

## 3. `EffectiveSpeed` 的语义未确认

**状态：待用户手感判断**

绝顶动画倍速走的是 Animancer 的 `EffectiveSpeed`。设 800% 时实测读到 **3.38**，
不是 8。

可能是这个属性的正常语义（按片段长度归一化），也可能被别处限了。
**只能靠手感判断**：倍速拉到 800% 够快就不用管；偏慢则改用 `Speed`（更直接），或两层都设。

---

## 4. `xcheck` 的不变量没覆盖新增状态

**状态： 已扩充（2026-10-07）—— 见第 34 条**

`xcheck` 现有 16 条不变量都是早期的（动摇、上限、UI `sizeDelta`）。
新加的状态之间**有约束关系但没人核对**：

- `_afterglowRemaining > 0` 时不应处于索取模式中
- `_afterglowPending > 0` 时闸门 `_demandEntered` 必须是开的
- `_chainDemand` / `_chainNormal` 不应同时非零
- `_sitDeferred` 为真时 `centerGirlState` 应是 `Osiri`
- `_demandStacks` 不应超过 `DemandMaxStacks`

这正是当初做 `xcheck` 要防的那类问题。

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

###  用日志判断"有没有跑过"时有个陷阱

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
| Sit 榨取模式（Sit 射精不减速） | `[榨取]` | **已跑通 1 次**  |
| 余韵闭环 | `[余韵]` | 已跑通 5 次  |
| 回归累积 + 坐姿挂起 | `[回归]` | **仍为 0** |
| 坐姿 / 口交的射精吸精累积 | — | 需开 `urgeaudit` 才能判定 |

---

## 6. Sit 模式没有便于手动累积榨取欲的部位

**状态： 已修（2026-10-07）**

**症状**
背面骑乘（Osiri）有**打屁股**这个又大又明确的目标，随手点就能累积索取欲；
而坐姿（Sit）**没有一个方便的部位**去手动累积榨取欲。

**根因 —— 两边的累积条件宽严差很多**

| | 累积条件 | 窗口 | 目标 |
| --- | --- | --- | --- |
| **Osiri** `Osiri叩かれる()` | `osiriState == attacking` | **长期稳定**（整个攻击阶段） | 屁股，又大又显眼 |
| **Sit** `頭叩かれるSit()` | `kissing == 1` | **只有 4~5 秒** | 头部，且只在那个窗口内才认 |

原码：

```csharp
public void 頭叩かれるSit()
{
    if (kissing == 0) { return; }                 // 什么都不做
    if (kissing == 2) { SkillCheck...; return; }   // 走技能检定，【不加计数】
    if (kissing == 1) { SitGirlKiss叩く量++; }      // ← 只有这一支才累积
}
```

`kissing` 的状态机：

- `0 → 1` 由 `PrepareKissing()` 触发，**窗口长度 = `KissPrepareTime`**（第 1~3 天 5 秒、
  第 4 天 4.5 秒、第 5 天 4 秒；每次 `KissPrepare解除()` 还会 `*= 0.9` 继续缩短）
- `1 → 2` 由 `StartKISS()` 在 `拘束キスTimer < 0` 时触发 → 变成技能检定，**这时点击不再累积**
- 平时自动进入靠 `Random.value < 0.1f`（10%），不可控

**关键的隐藏入口：游戏有一组开发者快捷键**

`TabemiControl.Update()` 里直接绑了：

| 键 | 作用 |
| --- | --- |
| **Z** | `Show_CenterGirlSit()` —— 直接进入坐姿 |
| **X** | **`PrepareKissing()` —— 直接把 `kissing` 置 1，打开累积窗口** |
| **C** | `StartKISS()` —— 直接进技能检定（另一个 C 分支是 `HPHeal(10)`） |
| **V** | （待查） |

**所以手动累积 Sit 榨取欲的实际步骤是：先按 X，再在 4~5 秒内点头部。**
这条路径游戏本身完全没提示，只能从代码里读出来。

**修法（已实施）**

在 `Postfix_SitHits` 里补了一条：

- 计数**真的增加了**（`kissing == 1` 窗口内）→ 走完整逻辑，含脱出惩罚（原行为不变）
- 计数**没增加** → 只要 `SitClickAlwaysAccumulate` 开着、且 `sitState == "attacking"`，
  就**只累积榨取欲 + 过一遍叠层判定**，**不动游戏的 `SitGirlKiss叩く量`**

**关键**：不动游戏自己的计数，所以**脱出机制与原版完全一致** ——
只是让榨取欲能靠点击攒起来，等于给了坐姿一个和「打屁股」对等的手动入口。

开关：`SitClickAlwaysAccumulate`（默认开）。

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

## 8. Sit 无法触发连榨，因此也无法触发余韵

**状态： 已修（2026-10-07）**

**症状**
坐姿下连榨永远起不来，余韵自然也就永远触发不了。

**现场证据**

`watch.csv` 里 **133 个 Sit 样本的 `Syaseing` 列全部为 0**，而且
**子状态列全部是 `attacking`** —— 从来没有切到过 `kyusei` 或 `syasei`。
余韵列也全程为 0。

对比：同一份数据里 Osiri 有 4 个 `kyusei` 样本、`Syaseing=1` 出现过 5 次。

**这是决定性的**：Sit 不是"连榨起不来"，而是**连榨之前的那一步（进入榨取）都到不了**。

**用户补充（与代码一致）**：「sit 的连榨效果貌似只出现于【束缚之吻】状态下」

代码层面确实如此 —— Sit 的榨取只有一个入口，而它会**强制把角色置为接吻态**：

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

所以 **Sit 的连榨 = 束缚之吻专属**，不是"在坐姿里随便就能出"。

**根因（推测，待验证）**
进入 Sit 榨取的那条路被 `kissing` 状态机卡住了：

```
Event_SitKissSyaseiOnEnd()
{
    if (gameover)          Play_SitKiss吸精(true);
    else if (Random < 0.5) Play_SitKiss();          // 回坐姿攻击
    else                   Play_SitKiss吸精(true);   // 50% 进榨取
}
```

也就是说 **Sit 的榨取只能从 `Play_SitKiss吸精` 进**，而它的前置是
**`kissing == 2`（接吻态）** —— 而 `kissing` 要经过

- `0 → 1`：`PrepareKissing()`，靠 10% 随机 或 **按 X**
- `1 → 2`：`StartKISS()`，靠 `拘束キスTimer < 0` 或 **按 C**

才会到。这与第 6 条是同一个根源：**Sit 的手动入口既要按键又只在窄窗口内有效**，
玩家实际玩下来几乎到不了接吻态，于是榨取 → 连榨 → 余韵整条链都摸不到。

**另一个已接管但可能没机会跑的点**
`Even_SitKiss吸精OnEnd` 原版两个分支都是 `Play_SitKiss吸精(true)`（永远重播），
我加了前缀接管以套用 `KyuseiMaxChain`。**但既然榨取都进不去，这段接管就从未被触发过。**

**待查方向**

1. **确认 `kissing` 在实际游玩里的到达率** —— 在 `PrepareKissing` / `StartKISS` 加日志，
   看一局里能进几次
2. **若确实到不了**，考虑让 Sit 的榨取也能从非接吻路径进入
   （例如 `Event_SitSyasei_OnEnd` 现在永远 `Play_Sit()`，可以给它一个小概率进榨取）
3. 或者按第 6 条的方向放宽手动入口，让玩家能主动把状态推到接吻态

---

## 附：证据归档

持续拉取器（`tools/watch.sh`）的产出已从 `BepInEx/l2d_dump/cmd/` 复制到
`watch-archive/`，避免被游戏或 BepInEx 清掉：

```
watch-archive/watch-<时间戳>.csv   紧凑时间线，可用 csv 工具直接分析
watch-archive/watch-<时间戳>.txt   完整状态块，含每轮的原文
```

**下次要复现或对比，先看这里有没有对应时段的档。**

---

## 附：坐姿「攻击行程」的机制（参考知识，非缺陷）

用户观察：「sit 有一个『攻击行程』的视觉，即骑乘的时候上下活动有一个最大长度，
但是在这段距离中间又有一些『点』使得每次 sit 下去前的高度可以不同。」

**代码完全对得上。** 行程是 2D 混合的一个轴：

```csharp
Vector2 parameter = new Vector2(sitSpeed + sitSpeedPlus, sit_HeadZ);
SitMixerA_HeadZ.State.Parameter = parameter;   // 横轴 = 速度，纵轴 = 行程深度
```

`Update_SitMixers()` 每帧把 `SitMixerA/B/KissA/KissB_HeadZ` 四个混合器的
`NormalizedTime` 与主混合器对齐，参数喂同一个 `(speed, sit_HeadZ)`。

**行程怎么变** —— 就是你说的"中间那些点"：

```csharp
headZ判定Timer += headZ判定interval;
if (UnityEngine.Random.value < 0.1f)                     // 10% 概率
{
    float targetDiff = UnityEngine.Random.Range(-1, 2);   // ← 只有 -1 / 0 / +1
    StartCoroutine(SitHeadZCoroutine(targetDiff, 0.5f));  // ← 0.5 秒平滑过渡
}
```

| 项 | 值 |
| --- | --- |
| `sit_HeadZ` 的取值 | **{−1, 0, +1}** —— 三个档位 |
| 换档概率 | 每 `headZ判定interval` 掷一次，**10%** |
| 过渡时长 | **0.5 秒**，用 `Mathf.Lerp` 平滑 |
| 类型 | `private float sit_HeadZ`（`Live2D_Animation_SitOsiri`） |

**所以"每次坐下去前的高度可以不同"是因为档位换过了** —— 三个离散档 + 随机换档 + 平滑过渡。

**对修改器的含义**：我现在动的「坐姿攻击速度」只碰了**横轴**（`sitSpeed`）。
**纵轴 `sit_HeadZ` 是另一个维度，目前完全没碰** —— 若日后要做"行程变长/变短/固定某个档"，
挂点在这里（`Update_SitMixers` 之后改 `sit_HeadZ`，或拦住那个 10% 的换档）。

---

## 9. 手动进入余韵功能无效

**状态： 已修（2026-10-07）**

**症状**
`demand afterglow` 命令 / 面板「手动进入余韵」按钮点了没反应 ——
其实发送成功了（还会弹提示条），但**角色不会真的开始连续吸精**。

**根因 —— 手动命令设的变量和点火逻辑看的变量不是同一个**

| | 变量 |
| --- | --- |
| `demand afterglow` 手动命令 | 设 `_afterglowRemaining = N`，并**把 `_afterglowPending` 清零** |
| `TickAfterglowSettle`（唯一负责"开第一次榨取"的地方） | **第一行就是 `if (_afterglowPending <= 0) return;`** |

于是手动余韵设了个**没人消费的计数器**：

```
手动设 _afterglowRemaining = N
  → 要消费它，得靠榨取收尾事件（Event_Osiri吸精End / Even_SitKiss吸精OnEnd）
    → 榨取要先有一次，而开第一次榨取只有 TickAfterglowSettle 会做
      → TickAfterglowSettle 被 _afterglowPending <= 0 挡住
        → 死循环，计数器就静静躺在那里
```

**这解释了为什么旧版「手动进入余韵」看起来"有效"**（提示条会弹、`status` 里余韵数也对），
**但角色毫无反应** —— 提示条只反映那个计数器，不代表榨取真的开始了。

**修法**
让手动命令走和自动余韵**同一条路径**：

```csharp
_afterglowPending   = UnityEngine.Random.Range(lo, hi + 1);   // 交给点火逻辑
_afterglowRemaining = 0;
_afterglowSettleAt  = -999f;                                   // 立即重新计稳定时间
```

**验证**

```
已手动进入余韵：待发动 17 次（局面稳定后自动开第一次榨取）
[余韵] 最后一段已结束且局面稳定 → 发动余韵 17 次
状态: [Osiri·kyusei]        ← 榨取真的跑起来了
```

---

## 10. 索取模式太久会导致坐姿入口被无限期堵死

**状态： 已修（2026-10-07）**

**用户描述**
「有时候索取的时间太长，到了 sit 可出现的时间段的时候一直进不了 sit，
就可以手动触发余韵强制结束索取模式并清空索取欲，
同时余韵过程中足够低的索取欲增长也可以防止余韵结束后继续接着索取」

**根因（比"手动余韵"更深一层）**

两道闸都会挡坐姿入口，其中第二道是真正的问题：

| 闸 | 条件 | 评价 |
| --- | --- | --- |
| 「余韵清空前不换姿势」 | `_afterglowRemaining > 0` | 余韵跑完自然解除，没问题 |
| **「回归累积」的坐姿挂起** | `_sitDeferred && centerGirlState == "Osiri"` 就一直拦 | **索取模式让角色长期停在 Osiri → 放行条件永远不成立 → 无限期堵死** |

`_sitDeferred` 本来只在"骑乘位结束"时清除，而长索取模式下那一刻永远不会到来。

**修法（三件事）**

1. **手动余韵 = 强制退出索取模式**（逃生阀）
   `demand afterglow` 现在把索取状态**全部清干净**：
   `_demandMode / _demandStacks / _demandUntil / _demandUrge / _chainDemand` 一并归零。

2. **坐姿挂起加超时保护**（修根因）
   新增 `SitDeferTimeout`（默认 45 秒）—— 超过就强制放行，
   不再依赖"骑乘位结束"这个可能永不到来的条件。

3. **余韵期间索取欲低增长 + 结束后清空**
   - `AfterglowUrgeScale`（默认 20%）：余韵期间所有来源的索取欲增长统一乘这个系数
     （打屁股 / 坐姿点击 / 攻击 / 射精吸精都走同一个 `UrgeScaleNow()`）。
     余韵本身会连榨十几次、每次都涨，不压住的话**余韵一结束马上又进索取模式**，
     逃生阀就白用了。
   - `AfterglowClearsUrge`（默认开）：余韵烧完时索取欲归零、`_demandEntered` 落闸，
     之后有一段干净的空档。

**验证**

```
[索取模式中]  索取模式中 1 段 / 共剩余 154.5s   索取欲=2…
[手动余韵后]  [Osiri·kyusei]  不在索取模式   索取欲=1.4%    ← 强制清干净
             已手动进入余韵：待发动 43 次（局面稳定后自动开第一次榨取）
```

---

## 11. 连榨上限拆成四套（功能变更，非缺陷）

**状态： 已实施（2026-10-07）**

**用户要求**
「『玩家』栏里的『连榨上限』需要做 osiri常规、osiri索取、sit常规、sit榨取四个滑块以区分」

**原来**：只有一个 `KyuseiMaxChain`，两种姿势 × 两种模式**共用同一个上限与同一个计数** ——
调一个会连带影响另外三个场景。

**现在**：四套独立的上限与计数。

| 配置键 | 场景 | 面板标签 |
| --- | --- | --- |
| `ChainMaxOsiriNormal` | 骑乘位 · 常规 | 骑乘位·常规 |
| `ChainMaxOsiriDemand` | 骑乘位 · 索取模式 | 骑乘位·索取模式 |
| `ChainMaxSitNormal` | 坐姿 · 常规 | 坐姿·常规 |
| `ChainMaxSitDemand` | 坐姿 · 榨取模式 | 坐姿·榨取模式 |

面板在「玩家」页的「吸精（榨取）」节，并实时显示当前场景：
`当前场景：坐姿·榨取模式，上限 16，已连 3`

**实现**

```csharp
internal static bool PoseIsSit()      // Sit 与 Osiri 在同一个类里，从 centerGirlState 判

private static int ChainNow()         // 姿势 × 模式 → 取对应那个计数
private static void ChainSet(int v)
private static int ChainLimitNow()    // 姿势 × 模式 → 取对应那个上限
internal static string ChainSlotName()  // "坐姿·榨取模式" 这类标签
```

`KyuseiMaxChain` **已废弃**（保留在配置里只为兼容旧档，逻辑上不再被读）。

**用户实测值**（说明四套确实独立生效）：

```
ChainMaxOsiriNormal = 2
ChainMaxOsiriDemand = 12
ChainMaxSitNormal   = 2
ChainMaxSitDemand   = 16
```

---

## 12. 余韵只能由索取/榨取模式中的活动触发（不变量）

**状态： 已确认成立 + 已写成代码注释（2026-10-07）**

**用户要求**
「所有常规状态下的活动都不会触发余韵」

**现状核查：这条已经成立。**

余韵累积的唯一入口 `NoteDemandSyasei()`，第一行就是闸门：

```csharp
private static void NoteDemandSyasei(string pose)
{
    if (!_demandMode) return;      // ← 常规状态下直接返回
    ...
}
```

`_afterglowPending` 的**全部写入点只有四处**：

1. `NoteDemandSyasei`（模式中射精，带闸门）
2. `EnterOrStackDemand` 里"收回上一轮没跑完的余韵"（只在进入/叠加模式时可达）
3. 手动 `demand afterglow` 命令（**故意**绕过闸门，用于测试与逃生阀）
4. `defaults` 复位

**没有任何一条能让常规状态的活动触发余韵。**

**验证**：常规状态下游玩 12 秒，日志里余韵累积次数 = **0**

**顺带修了一个缺口**：`NoteDemandSyasei` 原先只挂了**两个**调用点（坐姿、骑乘位），
**口交的射精漏了**。已补到 `Postfix_FellaSyaseiStart`。

**已把这条规则写成 `NoteDemandSyasei` 的文档注释**，并列出那四个写入点，
提醒"新增第五条之前请三思" —— 防止日后被无意破坏。

---

## 13. 连榨被莫名提前终止（正骑）

**状态： 已修（2026-10-07）**

**症状**：正骑时连榨链会毫无预兆地断掉。

**根因**：连榨档位（常规 / 索取·榨取）原来只按 `_demandMode` 判：

```csharp
if (_demandMode) return sit ? _chainSitDemand : _chainOsiriDemand;
return sit ? _chainSitNormal : _chainOsiriNormal;
```

而**正骑的榨取是从 `Play_SitKiss吸精()` 进的 —— 它把 `kissing` 强制置 2，但不置 `_demandMode`**。

于是正骑的连榨落到**「常规」档**，被 `ChainMaxSitNormal`（实测值 **2**）提前掐断。

同样的坑我在上一轮（#12 的 `ModeActive()`）刚踩过一次：
**凡是判"_demandMode 才算模式中"的地方，对正骑都不成立。**

**修法**：新增 `DrainSlotIsDemand()`，并把 `ChainNow()` / `ChainSet()` /
`ChainLimitNow()` / `ChainSlotName()` 四处取档全部改用它。

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

**教训（值得单列）**：
**`_demandMode` 不能当作"机制正在生效"的判据。** 正骑的榨取独立于它。
凡是要判"现在是不是在跑这套机制"，一律用 `ModeActive()` / `DrainSlotIsDemand()`。

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

## 15. 坐姿到点会把正在进行的索取模式顶掉

**状态： 已修（2026-10-07）；同时按要求【删除】了坐姿的超时强制放行**

**用户原话**
「把对 sit 的强制放行功能删掉，并且 osiri 的索取模式在我设定 sit 会出现的 1h 处，会被 sit 顶掉」

**症状**
索取模式正跑着，到了坐姿的出现时刻（用户设为 1 小时），角色被强行切到坐姿，模式中断。

**根因**
`Prefix_ShowCenterGirlSit` 当时只挡两种情况：

| 已有的闸 | 条件 |
| --- | --- |
| 余韵清空前不换姿势 | `_afterglowRemaining > 0` |
| 回归累积的挂起 | `_sitDeferred` —— **而它只在"进入骑乘位时坐姿正好待进入"那一刻才置位** |

**没有"模式进行中就不许换姿势"这一道** —— 所以坐姿一到点就长驱直入。

**另一个促成因素**：我之前加的 `SitDeferTimeout`（45 秒超时强制放行）
本意是防止坐姿入口被无限期堵死，但它会**主动把模式打断**，与"模式期间不被打断"这条设计冲突。
**用户明确要求删掉，已删。**

**修法**

```csharp
if (_demandMode)
{
    Log.LogInfo("[索取] " + ModeWord() + "模式进行中（" + _demandStacks + " 段 / 剩余 …s）"
                + " → 不许坐姿顶进来。要退出请用「手动进入余韵」");
    return false;
}
```

**不设任何超时。** 要主动退出索取模式 → 用「手动进入余韵」那个逃生阀
（见第 10 条：它会强制清干净模式状态）。

`SitDeferTimeout` 配置键保留但**已停用**（只为兼容旧档），说明里标了原因。

**设计定型**：

| 想做的事 | 用什么 |
| --- | --- |
| 让坐姿进来 | 等模式自然结束，或「手动进入余韵」强制清空 |
| 模式期间换姿势 | **不允许** —— 这是设计，不是缺陷 |

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

## 17. 面板改的是"当前姿势"那份，而不是"你正在看的栏"那份

**状态： 已修（2026-10-07）**

**症状（我自己发现的）**
三份参数（`_Fella` / `_Osiri` / `_Sit`）做出来之后，
**面板里的滑块按"当前实际姿势"读写，而不是按你正在看的那一栏。**

于是：**你人在骑乘位，去调「口交」栏的滑块 —— 改的其实是骑乘位那份。**
这直接破坏了用户明确要求的"每一个栏的相对独立性"。

**根因**

```csharp
private static int PIdx { get { int i = PoseIdx(); ... } }   // ← 只看实际姿势
private static float P3(ConfigEntry<float>[] a) { return a[PIdx].Value; }
private static void SetP3(...) { a[PIdx].Value = v; }
```

`P3` / `SetP3` 同时被【游戏逻辑】和【面板】使用，但两者需要的索引不同：

| 使用者 | 该用哪个索引 |
| --- | --- |
| 游戏逻辑（每秒跑） | **实际姿势** `PoseIdx()` |
| 面板滑块 | **正在看的那一栏** |

**修法：加 `_editPose`**

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
- `OnGUI` 开头 → `_editPose = -1`（每帧复位，保证不影响游戏逻辑那边的读取）

**教训**：一个"按当前上下文取索引"的访问器被两种上下文共用时，
**必须显式区分**，否则面板看起来在工作、实际改错了对象 —— 而且不会有任何报错。

---

## 18. 叠满段数后刷新持续时间（功能新增）

**状态： 已实施（2026-10-07）**

**用户要求**
「叠满对应欲望层数后，虽然无法继续叠层数，但是达成"叠层"的积累条件后，可刷新状态的持续时间」

**原来**：`EnterOrStackDemand` 在段数满时只打一句日志、什么都不做 ——
段满之后玩家再怎么攒，模式都只会静静走向结束，**积累全白费**。

**现在**：段满时不再加层，但**按 `DemandMaxStackRefreshMul`（默认 100%）把一段时长加到总时长上**，
并把欲望清 0（消耗掉这次积累）。0 = 保持原行为。

**验证**

```
已达 2 段上限 → 不加层，但刷新持续时间 +53.8 秒 → 剩余 257.6 秒
已达 2 段上限 → 不加层，但刷新持续时间 +83.3 秒 → 剩余 340.9 秒
已达 2 段上限 → 不加层，但刷新持续时间 +73.4 秒 → 剩余 414.3 秒
已达 2 段上限 → 不加层，但刷新持续时间 +47.8 秒 → 剩余 462 秒
```

段数停在 2，时间持续刷新

---

## 19. 【实验性】可点击区域高亮

**状态： 已实施（2026-10-07）**

**用户要求**
「各个模式的可点击区域都设置实验性的区域高亮设置按钮」

**做法**：不逐个硬编码，而是**遍历模型里所有以 `HitArea` 开头的 drawable**，
逐个算出屏幕包围盒并画框 + 标签 —— **三个模式一起覆盖**，游戏加新区也自动跟上。

- 开关：`HighlightHitAreas`（默认关，实验性）
- 三个栏（口交 / 背榨 / 正骑）各有一个按钮，**共用同一个开关**
- 另外会把**插件自建的小穴区**也画出来（标「小穴区(插件自建)」），因为它不是模型里的 drawable
- 画法与连榨提示条一样，**不受面板显隐影响**

**取 drawable 名字的坑**：不同 Cubism 版本字段名不一致，
所以 `DrawableName()` 依次试 `Name` / `name` / `Id`（属性与字段都试）——
和之前"动画层成员名各版本不一"是同一种处理方式。

---

## 20. Manman 区：Cubism 路线走不通，改为"素材绑定 + 手拖"两条腿

**状态： 已实施（2026-10-07）**

**用户提出的方向**
「这里就涉及到 Live2D 的素材于模型间的绑定功能，然后又可以去用 Cubism 了」

**前提核查（三条，结果决定了做法）**

| 检查 | 结果 |
| --- | --- |
| Cubism Editor | **装着**（`D:\Live2D Cubism 5.1`） |
| 游戏的 `.cmo3` 源工程 | **没有**  —— 只有编译后的 `.moc3` |
| `.moc3` 能否用 Cubism Editor 打开 | **不能** —— Editor 只认 `.cmo3`，这正是"编译"的意义 |

**结论：在模型里加一个真正的 `HitArea_Manman` drawable 这条路走不通。**
Cubism 的 drawable 集合、顶点数据、绘制顺序全都烘焙在 moc3 里，**运行时加不了**；
而没有 `.cmo3` 就改不了 moc3。

**顺带确认**：模型里 **`HitArea*` 素材一共 5 个**（高亮功能实测列出来的）：
`HitArea_Head` / `HitArea_LeftGirl` / `HitArea_RightGirl` / `HitArea_Head_Sit` / `HitArea_Osiri`
—— **没有 Manman / Manko**（代码里那句 `HitArea_Manman` 是残留，assets 里搜不到）。

**所以改成两条腿**

1. **素材绑定（优先）** —— `ManmanBindDrawable` 填一个 drawable 名，
   判定区直接用**那个素材的包围盒**。
   这是真正的"素材与模型绑定"：跟着网格形变走，人物怎么动都贴合。
   面板里把 5 个 `HitArea*` 列成按钮，点一下即绑。

2. **手拖矩形（兜底）** —— 没绑素材时用 `CX/CY/W/H`（模型相对比例）。
   新增**拖动模式** `ManmanDragMode`：开着时判定区中心实时跟随鼠标，
   对准位置后点「绑定到此位置」，中心就定在那里。

**同时修了一个坐标换算 bug**

「把区域中心设为当前鼠标位置」原来写的是**屏幕比例**（`mp.x / Screen.width`），
而判定区早已改成**模型相对** —— 两者不在同一套坐标里，**按钮能点、位置完全不对**。
新增 `ScreenToModelRel()` 做换算，拖动与绑定都走它。

---

## 21. 素材绑定只列了 HitArea*，实际选不到有用的素材（补充完成）

**状态： 已修（2026-10-07）**

**用户追问**：「素材绑定是不是还没做好」

**确实差一块关键的**：面板里只列了 `HitArea*` 那 5 个素材，
而那 5 个**本身就是可点区** —— 绑它们等于没绑，毫无意义。

**真正需要的是绑到「非 HitArea 的美术素材」上**（例えば裆部那块身体/皮肤图），
而当时**根本选不到**。

**补法**

| 改动 | 说明 |
| --- | --- |
| `AllDrawableNames(filter)` | 列出模型里**全部** drawable（可按子串筛选，不区分大小写） |
| `ManmanDrawableFilter` | 筛选框（面板里可直接输入） |
| `ManmanDrawablePage` | 页码，每页 40 个（模型 drawable 很多，必须分页） |
| 快捷按钮 | `HitArea 筛选` / `清空筛选` |
| 当前项标记 | 已绑定的那个显示 `[记录]`，再点一次取消绑定 |

**顺带修了 `set` 的 `ManmanBindDrawable` 分支** ——
原来写的是 `if (!isNum && name == "ManmanBindDrawable") { ...; break; } break;`，
那个 `name ==` 判断在 `switch (name)` 里恒真，属于绕圈子的写法，已改直。

**已验证的部分**：底层机制是通的 —— 因为「可点击区高亮」功能
（用的是同一套 `DrawableName()` + `TryGetDrawableScreenRect()`）
实测能把 5 个 HitArea 的框准确画在角色身上（见截图 `HitArea Osiri` 那一条）。
所以素材绑定缺的只是**列表范围**，不是机制。

**尚未验证**：绑定到某个非 HitArea 素材之后，判定区是否真的贴合那个素材。
需要在「正骑」栏里选一个素材实测。

---

## 22. 说明书补完（96 项参数 + 六个新概念）

**状态： 已完成（2026-10-07）**

**补之前的状态**

| 检查 | 结果 |
| --- | --- |
| 本族参数 | 141 个，**未提及 96 个** |
| 「背榨」/「正骑」 | **各 0 处** |
| 「束缚之吻」 | **0 处** |
| 「Manman」 | **0 处** |
| 「吸取」 | **0 处** |
| 「Highlight」 | **0 处** |

说明书停留在"面板只有『对手』一栏"的时代。

**补了什么**

| 新增/重写的小节 | 内容 |
| --- | --- |
| **面板的六个栏** | 六栏结构 + 三栏各自的说法（吸取 / 索取 / 榨取） |
| **按姿势三份** | 三份机制、`_Fella/_Osiri/_Sit` 后缀、`_editPose` 那个坑、拷贝、哪些已独立哪些还没 |
| **触发链** | 四个步骤、`TryStackNow` 是唯一判定、两种触发模式、四个累积来源、平均值约定、段满刷新 |
| **模式期间的效果** | 时长/段数/攻击力/倍速/分阶段加速/不回口交/无法打断 |
| **攻击力随速度** | 小指数曲线的公式与对照表（k=0/2/4） |
| **绝顶动画倍速** | 为什么必须单独做（含原码）、判据为何用"主混合器是否在播" |
| **连榨** | 五套上限、按生命值折算、软上限与波动、`_demandMode` 那个反例 |
| **回口交与回归累积** | 三岔路口原码 + 门槛降低 + 立刻进入 + 坐姿挂起 |
| **余韵** | 累积/发动/闸门/速度与波动/独立软上限/生存阀 |
| **束缚之吻** | 为什么正骑的连榨必须经过它、四个参数、滑块绑定 |
| **Manman 区** | Cubism 为什么走不通、模型相对判定、素材绑定与手拖两条路 |
| **可点击区域高亮** | 遍历 HitArea* drawable 的做法与用途 |
| **撞击声** | 动画事件驱动节奏、速度决定音色、四档、防重叠、同音不连播 |
| **打屁股冲量** | 五个参数 + 为什么施加点是动画速率而不是 `osiriSpeed` |
| **坐姿专属两项** | `SitClickAlwaysAccumulate` / `SitSyaseiToDrainChance` |
| **攻击力与速度** | `PowerUnlock` / `TabemiPowerMul` / `FellaSpeedPlus` / `SitSpeedPlus` |
| **Manman 区六个参数** | 含"CX/CY/W/H 是模型相对比例"的提醒 |
| **废弃/停用参数** | `KyuseiMaxChain` / `ChainGainCap` / `DemandUrgeCap` / `SitDeferTimeout` / `DemandAfterglowMin·Max` |
| **日志与提示速查** | 11 个关键词的含义 + 取证工具 |

**结果**

```
说明书：1032 行 → 1339 行
本族参数覆盖：141 个里未提及 96 个 → 8 个
```

剩下那 8 个是**斜杠组合提及**（`AfterglowSpeedWobble` / `Hz`、`AttackUrgeChance` / `Gain` / `Jitter` 这种），
**功能上已全覆盖**，只是字面匹配看不见。

**方法说明**：用了两段脚本按行号精确替换/插入，
并且每次都跑一遍"参数覆盖率"检查来确认效果 —— 不是写完就算。

---

## 23. 剩余 31 个参数也做成了按姿势三份（附带一次用户调参救援）

**状态： 已完成（2026-10-07）**

**做了什么**

把最后 31 个参数（索取欲来源 7 / 打屁股冲量 5 / 连榨 5 / 回归累积 4 / 余韵 10）
迁到 `BindP3` / `P3` / `SetP3` 体系。

**现在一共 51 个参数 × 3 = 153 个键。**

**迁移过程中的四个坑（脚本三轮才跑通）**

| # | 坑 | 修法 |
| --- | --- | --- |
| 1 | 描述被 `new ConfigDescription(...)` 包着，当成 `string` 传给 `BindP3` → 类型不匹配 | 只取其中**第一段字符串字面量** |
| 2 | `ChainSoftCap`（默认 `60f`）被"有没有小数点"的启发式误判成整数 | **类型只认 `Config.Bind<T>` 的泛型参数** |
| 3 | 布尔参数的 `Config.Bind` **没写 `<bool>`**，被当成浮点 | 按默认值是 `true`/`false` 兜底判定 |
| 4 | 读取点替换会把面板的 `X.Value = SliderF(...)` 变成 `P3(X3) = ...`（给方法调用赋值） | 再统一跑一遍 `P3(x) = v;` → `SetP3(x, v);`（**77 处**） |

第 4 条和之前 batch 1 遇到的是同一个问题 —— **先做读取替换再做面板替换，顺序上必然产生 `P3(x) = v` 这种中间态**，
所以"读替换 + 写修正"应该是一个固定动作，不能漏。

**顺带救回一次用户调参**

新键生成后值都是**代码默认值**，而用户调过的值留在**旧的单份键**里。
关掉游戏直接改配置文件，把旧值复制到三份：

```
迁移 93 个键，其中【48 个值确实被调过】：
  AttackUrgeChance      8 → 20
  AttackUrgeGain        3 → 4
  SpankSpeedGain       12 → 20
  SpankSpeedStack      60 → 80
  SpankSpeedDecay    0.25 → 0.05
  OsiriReturnTrigger   30 → 15
  …
```

**不做这一步的话，这 48 个值会静默丢回默认** —— 而用户是边玩边调的，
丢了也不会有任何报错。（这正是"后续记得把用户设定的值进行保留"那条要求的实战场景。）

**验证**

```
挂载 35 条，无异常
set AttackUrgeChance 33（人在口交）
  AttackUrgeChance_Fella = 33   ← 只动这份
  AttackUrgeChance_Osiri = 20
  AttackUrgeChance_Sit   = 20
三份键总计 153 个
```

**已知副作用**：迁移时**跨行拼接的描述串被截断**了（只保留了第一段字面量）。
配置里那些参数的说明会短一些，**功能不受影响**。

---

## 24.  HitArea_Manman 只接了「触摸」那条路，鼠标点击根本没经过它

**状态： 已修（2026-10-07）—— 用户提醒"记得确认 manman 和榨取欲的交互成立"**

**这是我做这个功能以来最严重的一个疏漏。**

**游戏里鼠标和触摸是两条完全不同的路**

| 输入 | 取值来源 | 当时挂的方法 |
| --- | --- | --- |
| **触摸** | `Live2D_HitAreaCheck.GetTouchTargetName(pos)` —— 返回值直接派发 |  已挂 |
| **鼠标** | `Live2D_HitAreaCheck.mousePointing` —— **静态字段** |  **没挂** |

鼠标那条是这样的：

```csharp
// 每帧由 Update() 算出静态字段
private void Update() { HitAreaCheckWindows(); }
private void HitAreaCheckWindows() { ... mousePointing = array[i].Drawable.name; ... }

// 点击时读它来派发
public void Mouse0_Down() { hitAreaDown = Live2D_HitAreaCheck.mousePointing; ... }
public void Mouse0_Hold() { hitAreaPointing = Live2D_HitAreaCheck.mousePointing; ... }
public void Mouse0_Up()   { hitAreaUp = Live2D_HitAreaCheck.mousePointing; ... }
```

**所以只挂 `GetTouchTargetName` 的话，鼠标点击根本不会经过它** ——
而玩家（用户）是用鼠标玩的，**Manman 区等于完全没接上**。

**修法：补挂 `HitAreaCheckWindows` 的后缀**

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

**关键设计：只在真实命中为空时才抢** —— 不跟模型里真正的点按区争。

**验证**

```
点按区补丁已挂载（HitAreaCheckWindows 后缀：HitArea_Manman 鼠标路径）
点按区补丁已挂载（GetTouchTargetName 后缀：坐姿小穴区）
挂载总数 36 条
```

**面板里加了实时诊断**（正骑栏 · Manman 区那一块）：

```
屏幕矩形：820~980 × 430~610
鼠标在区内：是    游戏当前命中：HitArea_Head_Sit
```

**「游戏当前命中」显示 `HitArea_Head_Sit` 才说明交互成立。**
显示「(空)」= 没接上；显示别的 = 鼠标其实在另一个真点按区上。

**教训**

**"我改的这个函数的返回值"  !=  "游戏实际用的那个值"。**
游戏里同一个语义常常有【静态字段】和【方法返回值】两套，
**鼠标 / 触摸 / 键盘可能各走一套**。
挂之前必须找到**真正被派发读取**的那一处，而不是看起来最像的那一处。

---

## 25.  HitArea_Manman 不能冒充 HitArea_Head_Sit（功能会串）

**状态： 已修（2026-10-07）—— 用户指出**

**用户原话**：「sit 的 manman 和 head 不能同一功能，head 是为了解除束缚之吻」

**我原先做错了什么**

Manman 区命中时返回 `HitArea_Head_Sit`，也就是**冒充头部**。
但坐姿的头部点击**根本不是"攒榨取欲"**，它是**解除束缚之吻**：

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

**所以那个做法等于「点小穴会去解吻」** —— 两个功能串在一起了。

**正确做法：Manman 独立成一条**

```
静态 mousePointing 被我们改成 "HitArea_Manman"
  → 游戏 Mouse_Down_0 的 switch 里【没有】这一档   ← 它什么都不做
  → 由我们自己的前缀接住：
        · 累积榨取欲 + 叠层判定
        · 放点击特效（复用 clickEffect.頭叩くエフェクト，手感一致）
        · return false 跳过游戏那个 switch
```

**最终分工**

| 部位 | 功能 |
| --- | --- |
| **头部（HitArea_Head_Sit）** | **解除束缚之吻**（游戏原逻辑，未改动） |
| **HitArea_Manman** | **累积榨取欲**（插件独立处理） |

**三条补丁各司其职**

```
HitAreaCheckWindows 后缀   → 鼠标路径：把静态 mousePointing 改成 HitArea_Manman
GetTouchTargetName 后缀    → 触摸路径：同样改成 HitArea_Manman
Mouse_Down_0 前缀          → 接住 HitArea_Manman，自己处理，不让它进游戏 switch
```

**面板诊断的读法变了**：
「游戏当前命中」显示 **`HitArea_Manman`** 才说明鼠标路径接通了（原来显示 `HitArea_Head_Sit` 是错的）。

**教训**

**"冒充一个已有的 HitArea"这种做法要极其小心** ——
你以为你在借用"能点"这个属性，实际上你连它的**全部语义**一起借来了。
这里头部点击的语义是"解吻"，不是"攒欲望"。
**动手前必须把那个 HitArea 的处理函数从头读完**，而不是只看它"能被点到"。

---

## 26. HitArea_Manman 应与「打屁股」对称：涨榨取欲 + 给坐姿动作速度冲量

**状态： 已实施（2026-10-07）—— 用户指明**

**用户原话**：「manman 的功能应该是和 osiri 在修改器里新增的设定类似，
点击可以增长 sit 模式下的榨取欲和 sit 模式动作的速度」

**当时缺的是什么**

只做了"涨榨取欲"，**没有速度冲量** —— 而背榨那边的打屁股是两样都给。

**修的时候又挖出一个更深的 bug**

冲量的施加点原来只找 **`OsiriMixer`**：

```csharp
object mixer = FieldQuiet(sot, "OsiriMixer")?.GetValue(objs[0]);
```

**而坐姿用的是 `SitMixerA` / `SitMixerB`** —— 坐姿根本没有 `OsiriMixer` 在播，
所以就算给了冲量也**作用不到坐姿的动画上**。

改成按姿势依次找：

```csharp
string[] mixerNames = { "OsiriMixer", "SitMixerA", "SitMixerB", "SitKissMixerA", "SitKissMixerB" };
foreach (string mn in mixerNames) { ... if (IsPlaying) { SetValue(EffectiveSpeed, _spankMul); break; } }
```

**注意 `SpankSpeed*` 那组参数已经按姿势三份了**，所以坐姿读到的就是 `_Sit` 那份 ——
用户当前的值：`Gain 20 / Stack 80 / Rise 10 / Decay 0.05`。

**最终功能对照**

| | 背榨（Osiri） | 正骑（Sit） |
| --- | --- | --- |
| 触发 | 打屁股 | 点 **HitArea_Manman** |
| 涨欲望 | 索取欲 | 榨取欲 |
| 速度冲量 | （OsiriMixer） | （**SitMixerA/B**，本次修好） |
| 参数 | `SpankSpeed*_Osiri` | `SpankSpeed*_Sit` |

**与头部的关系**：头部 = 解除束缚之吻，两者互不干扰（见第 25 条）。

**教训**：**"同一个类"不等于"同一套东西"**。
`Live2D_Animation_SitOsiri` 一个类管着 Osiri 和 Sit 两种姿势，
**里面每一种姿势各有一套混合器字段** ——
只找其中一个的话，另一种姿势会静默地什么都不发生。

---

## 27.  坐姿榨取欲一满就跳到 Osiri（三个症状一个根因）

**状态： 已修（2026-10-07）—— 用户报告**

**用户原话**
「sit 的榨取欲满了不但显示的是"索取模式"，还会直接跳到 osiri，并且诸多功能无效」

**根因：是我自己在第 16 条加的那个"夺回骑乘位"兜底**

```csharp
if (_demandMode && centerGirlState == "Sit")
{
    Log.LogInfo("…模式进行中，状态却被切成坐姿 → 夺回骑乘位");
    ShowCenterGirlOsiri();      // ← 强制切到 Osiri
}
```

它的假设是 **"模式 = 一定在 Osiri"**。
于是坐姿自己的榨取欲一攒满、`_demandMode` 一置位，
这个兜底立刻把人物从 **Sit 拽到 Osiri**。

**三个症状全部由它一个引起**

| 症状 | 解释 |
| --- | --- |
| 显示"索取模式" | 被拽到 Osiri 后 `PoseIdx()` 变成 1 → `ModeWord()` 给"索取" |
| 跳到 osiri | 就是这一行干的 |
| 诸多功能无效 | 人物不在 Sit 了，Manman / 坐姿榨取 / 束缚之吻那一整套自然全落空 |

**又是 `_demandMode`** —— 这个坑在这个项目里已经踩了**第四次**：

| 次 | 症状 |
| --- | --- |
| 1 | 正骑的滑块在束缚之吻里不生效 |
| 2 | 正骑连榨被 `ChainMaxSitNormal` 掐断 |
| 3 | 坐姿入口被无限期堵死（第 10 条） |
| **4** | **坐姿模式被强行拽去 Osiri（本条）** |

**修法：记住"模式是在哪个姿势进入的"**

```csharp
private static int _demandModePose = -1;   // 0=口交 1=背榨 2=正骑；-1=无模式

// 进入模式时
_demandModePose = PoseIdx();
// 退出/清零时（4 处）
_demandMode = false; _demandModePose = -1;

// 兜底改成只对"背榨进入的模式"生效
if (_demandMode && _demandModePose == 1) { ...夺回骑乘位... }
```

**为什么这样对**：
兜底要解决的原始问题是「坐姿协程抢在背榨索取模式前面把人物切走」——
那个场景里 `_demandModePose == 1` 。
而坐姿自己的模式（`_demandModePose == 2`）本来就该待在 Sit，**不该被夺回**。

**顺带排查**：把所有仍在只看 `_demandMode` 的地方过了一遍（16 处），
其余都用于"模式特定效果是否生效""余韵 vs 模式"，语义正确，未改动。

**教训（这次要写死）**

> **`_demandMode` 只表示"模式在跑"，完全不表示"在哪个姿势跑"。**
> 任何要按姿势分支的逻辑，都必须用 `PoseIdx()` 或 `_demandModePose`，
> **绝不能用 `_demandMode` 代替**。
>
> 而且反过来说：**修一个 bug 时加的兜底，很可能变成下一个 bug 的根因** ——
> 第 16 条那个兜底当时确实解决了问题，但它把一个错误假设（模式必在 Osiri）固化进了代码。

---

## 28. 看起来有"两个 Manman 区"（一个是自建、一个像是依据模型）

**状态： 已修（2026-10-07）—— 用户指出**

**用户原话**：「貌似有两个 manman 区，一个是插件自建，一个是依据模型，解决一下这个问题」

**先确认事实**

查了 `sharedassets0.assets`：**`HitArea_Manman` 出现 0 次** —— 模型里仍然没有这块。
`ManmanBindDrawable` 也是**空的**（没做素材绑定）。

**所以"两个框"的真实来源是：我把模型里 5 个 HitArea 全都画了，不管当前是哪个姿势。**

```csharp
foreach (string nm in HitAreaDrawableNames())   // ← 全部，无过滤
    DrawOutline(...);
...
if (SitPussyAreaEnabled.Value)                  // ← 再加一个 Manman
    DrawOutline(..., "HitArea_Manman (插件自建)");
```

于是**站在口交姿势时**，`HitArea_Head_Sit` / `HitArea_Osiri` 这些
**当前根本不生效**的框也照样画出来，恰好和 Manman 区叠在一起 ——
看起来就像"有两个 Manman 区，一个来自模型"。

**修法：按姿势过滤**

| 当前姿势 | 只画这些 |
| --- | --- |
| 口交（Fella） | `HitArea_Head` / `HitArea_LeftGirl` / `HitArea_RightGirl` |
| 背榨（Osiri） | `HitArea_Head` / `HitArea_Osiri` |
| 正骑（Sit） | `HitArea_Head_Sit` |

**另外两处防重**

1. **判定优先级**：若模型里**将来**真的有了 `HitArea_Manman`，
   `PussyAreaScreenRect` 会**优先用它**，不再用自建矩形 —— 避免真的出现两块
2. **绘制去重**：画过的名字记进 `drawn`，Manman 区若是同一块就不再画第二遍
   （覆盖"绑定了某个 HitArea* 素材"那种情况）

**面板里加了「当前来源」**

```
当前来源：模型里的 HitArea_Manman   /   素材绑定：xxx   /   插件自建矩形
```

一眼就能看出现在用的是哪一套，不用再猜。

**用户确认**：「出现了，粉色的 manman 区」—— 去重后只剩一个

**教训**

> **"调试用的可视化"必须跟着实际生效范围走。**
> 把不生效的东西也画出来，会让人误判成"有两个重叠的区域"，
> 然后花时间去查一个根本不存在的重复。

---

## 29. 束缚之吻需要【自己的】调速（进入吻  !=  触发连榨）

**状态： 已实施（2026-10-07）—— 用户提出**

**用户原话**
「既然进入了束缚之吻也不一定出发连榨等，那么束缚之吻就需要单独设置调速等滑块」

**为什么需要单独一套**

| 状态 | 用的混合器 | 原来归谁管 |
| --- | --- | --- |
| **束缚之吻中但没进榨取** | `SitKissMixerA/B` | **没人管** ← 空档 |
| 榨取 / 绝顶中 | `SitMixer*` / 吸精片段 | 榨取与绝顶那套倍速 |

`PrepareKissing()` 让 `kissing` 变 1 之后，**可能一直不进榨取** ——
那段时间用的是 `SitKissMixer*`，而 `DemandAnimSpeed` 只在
`ModeActive()` 成立时才施加（`kyusei` 或 `kissing > 0.5`）…
**但那是"榨取/绝顶"的倍速，不该拿来当接吻的调速**，两者语义不同。

**新增四个参数（正骑栏 · 束缚之吻）**

| 参数 | 默认 | 说明 |
| --- | --- | --- |
| `KissSpeedEnabled` | 开 | 总开关 |
| `KissSpeedMul` | 100% | 接吻动画速度 |
| `KissSpeedWobble` | 0% | 速度波动幅度 |
| `KissSpeedHz` | 0.5 | 波动频率 |

**互斥处理**

```csharp
if (GetStringField(tab, "sitState") == "kyusei") return;   // 进了榨取 → 交给那套
if (GetFloat(tab, "kissing") <= 0.5f) return;              // 不在吻里 → 不管
```

**两边都设会互相打架**，所以按 `sitState` 严格互斥

---

## 30. HitArea_Manman 终于有了实证（顺带修好诊断命令）

**状态： 已确认可用（2026-10-07）**

**背景**：用户问「那这个粉色的 manman 区有作用么？」——
那时我**没有任何证据**，只看到"补丁挂上了"。

**查日志的结论是"没用过"**：

```
[屁股]（NoteSpank 的日志）   0 次   ← 从没被调用
HitArea_Manman 出现 2 次           ← 但那 2 次是【挂载日志】本身
```

**加了两个东西让它可以被验证**

1. **命中就留痕**：`Prefix_Mouse_Down_0` 里加日志；鼠标**首次进入判定区**也打一条
2. **`manman` 诊断命令**

**命令插错过一次**：我以为命令表在主文件里，插进了「配置项 switch」的 default 前面，
结果执行 `manman` 报 `ERR 未知命令`  ——
**命令表其实在 `GameCommandServer.cs`**（文件式命令通道那个文件）。
主文件里那个 switch 是 `set` 用的配置项表，两者长得很像。

**实测结果**

```
[HitArea_Manman] pose=2  enabled=True  src=plugin-rect
  mousePointing=HitArea_Manman   mouse=970,157  inside=YES
```

`mousePointing` 被成功改写为 `HitArea_Manman` ，鼠标也在区内
**——鼠标那条路确实接通了。**

**教训**

> **"补丁挂上了"  !=  "功能能用了"。**
> 挂载日志只能证明 Harmony 接上了，证明不了它在真实输入下被走到。
> **要证明，就得让它一旦触发就留痕**，然后去日志里找那条痕。

---

## 31. HitArea_Manman_By_Plugins：改名 + 可视化与其他 HitArea 统一

**状态： 已实施（2026-10-07）—— 用户提出**

**用户原话**
「manman 区接通了就可以和其他 HitArea 同级了，命名为 HitArea_Manman_By_Plugins」
「可以用粉色显示，但是要和其他 hitarea 用同一个可视化逻辑」
「最后那个（模型相对）可以去掉么」

**(1) 改名**

```csharp
internal const string ManmanAreaName = "HitArea_Manman_By_Plugins";
```

现在和游戏原生的那批**同级**：

```
HitArea_Head / HitArea_Head_Sit / HitArea_Osiri
HitArea_LeftGirl / HitArea_RightGirl
HitArea_Manman_By_Plugins          ← 本插件提供
```

用**常量**统一，不再散落字符串字面量（运行时写入的 4 处全部改用 `ManmanAreaName`）。

**注意保留的区分**：
`PussyAreaScreenRect` 里检查模型是否**真的有** `HitArea_Manman`（用户自建）时，
查的仍然是**不带后缀**的名字  —— 二者不会撞车。

**(2) 可视化统一**

原来 Manman 区走的是**另一套独立绘制**：
自己的 `_rectFill` / `_rectLine` 粉色纹理、自己的 `_pussyHintStyle`、
自己的 `SitPussyAreaShowRect` 开关、自己的 `DrawPussyAreaRect()`

现在**并进 `DrawOutline()`**，只多一个颜色参数：

```csharp
private void DrawOutline(float x0, float y0, float x1, float y1, string label, bool pink = false)
```

- 绿色 = 游戏原生的 HitArea
- **粉色 = 插件的 Manman 区**
- **同一个函数、同一套逻辑、同一个总开关 `HighlightHitAreas`**

旧的独立那套**整段删除**（54 行：`DrawPussyAreaRect` + `_pussyHintStyle` + `_rectFill` + `_rectLine`），
残留 0。

**(3) 「（模型相对）」标签消失**

那个后缀是旧 `DrawPussyAreaRect` 硬编码在标签里的。
整段删掉后，标签来自 `DrawOutline(..., "HitArea_Manman_By_Plugins", true)` —— **就只有名字**

**实测**

```
HitArea_Manman_By_Plugins     ← 粉色框，标签干净
```

**教训**

> **同一个东西有两套绘制代码 = 迟早会不一致。**
> 之前那套独立的粉色绘制让"Manman 区"看起来像个特例，
> 而它其实就该是"又一个 HitArea，只是颜色不同"。
> **颜色是参数，不是另一条代码路径。**

---

## 32. 最后三个参数补进三份体系 + 第 18 条验证通过

**状态： 已完成（2026-10-07）**

**用户**：「1 我测过了，没什么问题」——即**第 18 条（叠满段数后刷新持续时间）实战验证通过**

**补的三份参数**

| 参数 | 旧值 | 新值（三份） | 说明 |
| --- | --- | --- | --- |
| `ChainGainPerDrain` | 8 | 8 / 8 / 8 | 连榨固定增量模式 |
| `DemandFellaDecay` | **59.78** | **59.78 × 3** | **用户调过，已迁移** |
| `DemandMaxStackRefreshMul` | 100 | 100 / 100 / 100 | 段满后的刷新比例 |

**又一次差点丢调参**：`DemandFellaDecay` 旧值是 `59.78261`，而新三份键生成时是**默认 25**
关掉游戏直接改配置文件把旧值搬过去 —— 这已经是**第二次**遇到同一件事
（第一次是 31 个参数那批，48 个值）。

> **规律：任何"生成新键"的动作，都会让用户调过的值留在旧键里。**
> 所以**每次加三份参数，必须跟一次"旧值 → 三份"的迁移**，这不是可选步骤。

**结果**：说明书未提及项从 12 降到 4（都是斜杠组合提及），三份键 162 个（54 × 3）。

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
这是设计行为，不是异常

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

## 34. xcheck 不变量扩到新状态（#4）

**状态： 已完成（2026-10-07）**

原来检查到 (16)；新增 **(17)~(23)**：

| # | 不变量 | 硬/软 |
| --- | --- | --- |
| (17) | **`_demandMode` 与 `_demandModePose` 自洽** | 硬 |
| (18) | 段数在 `0 ~ DemandMaxStacks` 内 | 硬 |
| (19) | 索取欲非负 | 硬 |
| (20) | 背榨模式未被坐姿顶替 | 软 |
| (21) | 连榨计数不超本档上限 | 软 |
| (22) | 余韵计数非负 | 硬 |
| (23) | Manman 区在坐姿 + 启用时应当拿得到矩形 | 软 |

**(17) 是这组里最有价值的一条**：`_demandMode = false` 有**四处**赋值点，
每处都要同时把 `_demandModePose` 复位。漏掉任何一处，(17) 就会 FAIL ——
这正是当初那个 bug（#27）能藏那么久的原因。

**实测全过**

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

**顺带**：(23) 的输出**再次暴露矩形跑到屏幕下方外**（y 是 `-164~33`）——
这就是待修的 Manman 区位置问题，现在它会一直在 xcheck 里提醒，直到被修好。

---

## 35. 坐姿锁定 + 正骑栏删除口交相关项 + 日志标签按姿势取名

**状态： 已实施（2026-10-07）—— 用户提出**

**用户原话**
「索取是 osiri 的叫法，sit 都要叫榨取」
「而且要注意进入 sit 后必须不能切到 fella 和 osiri」
「所以正骑里那些和口交有关的都可以删掉了」

**(1) 日志标签**

原来有 **27 处**硬编码的 `"[索取] "` 前缀 —— 在坐姿里打日志也写"索取"
全部改成 `"[" + ModeWord() + "] "`，按当时姿势取名

**(2) 坐姿锁定（`SitLockEnabled`，默认开）**

游戏里离开坐姿只有**两个出口**：

| 出口 | 说明 |
| --- | --- |
| `ShowCenterGirlFella()` | **`Osiri解除()` 也是调它** |
| `ShowCenterGirlOsiri()` | |

两个都加前缀拦掉，坐姿就成了「进得去出不来」的状态
理由：正骑的榨取链（连榨 / 余韵）全建立在「人在坐姿」上，中途被切走会断链。

**踩了个坑**：`Prefix_ShowCenterGirlFella` **早就存在**（余韵那道闸），
我又定义了一个同名的 → `CS0111 重复定义`
正确做法是**给已有的那个加一道**，而不是新增：

```csharp
private static bool Prefix_ShowCenterGirlFella()
{
    if (_afterglowRemaining > 0) { ...不进口交...; return false; }   // 原有：余韵闸
    if (SitLockActive())         { ...坐姿锁定...; return false; }   // 新加
    return true;
}
```

**而且删除重复块时把 `SitLockActive()` 的定义一起删掉了**  → `CS0103`
—— **"删一段"时要确认那段里有没有别人依赖的定义**。

**(3) 正骑栏删掉口交相关项**

用 `if (!sitSide)` 包起来（**只影响正骑栏的显示，背榨那份照旧**）：

| 项 | 为什么正骑里没意义 |
| --- | --- |
| `DemandBlockFella`（模式期间不回口交） | 坐姿已锁死，回不去 |
| `DemandFellaDecay`（每次回口交衰减） | 同上 |
| `DemandOsiriBoost`（回口交后骑乘位门槛降低） | 同上 |
| `OsiriReturn*`（回归累积 4 项） | 那是「背榨 → 口交 → 再回骑乘位」的机制 |

**(4) 顺带：软上限的跳变改成平滑**

实测抓到的 38 次「绝顶骤降」（Δ 最大 −267.8）全部来自**我自己的软上限逻辑**：
值高于波动带时被**一次设过去**

新增 `EaseToward(cur, target, maxRef)` —— 按速率逼近（`CapSmoothRate`，默认 60%/秒），
施加到三处：连榨/余韵软上限、`KeepMaxCaps` 的 `currentHP` 与 `CurrentEcstasy`。

**挂载 39 条**（+2）

---

## 36. 坐姿锁定：出口路径已核实「全覆盖」

**状态： 已核实（2026-10-07）—— 用户确认「就是要进入坐姿后不会切到其他模式」**

**这是设计，不是缺陷** —— 不是"出不来是个 bug"，而是**要的就是出不来**。

**核实过程**：把游戏里所有会改 `centerGirlState` 的地方列了出来。

| 行 | 所在方法 | 是不是"离开坐姿"的出口 | 拦到了吗 |
| --- | --- | --- | --- |
| 10413 | **`Start()`** |  这只是入场初始化 | — |
| 10442 | `ShowCenterGirlFella()` | 是 |  已拦 |
| **10992** | `ShowCenterGirlOsiri()` —— **方法体内直接赋值** | 是 |  已拦 |
| 10445 / 10448 | `_CenterGirl()` 协程 | 只被上面两个方法启动 |  间接拦住 |
| 10619 / 10623 | `Show_CenterGirlSit()` | 那是**进入**坐姿 | 不拦（正确） |

**关键点**：`ShowCenterGirlOsiri()` 是在**方法体内直接写** `centerGirlState = Osiri`，
不是在协程里 —— 但**前缀 `return false` 会跳过整个方法体**，所以那一行也一起被拦住了

**并且**：`Osiri解除()` 内部是调 `ShowCenterGirlFella()`  走的是同一条被拦的路径。

**结论：两个前缀 = 堵住全部出口。** 坐姿是「进得去、出不来」的状态。

**离开坐姿的途径**（如果将来需要）：只能由插件提供（比如加一个命令/按钮主动清 `SitLockEnabled` 再触发切换）。
目前**没有做**这个逃生口 —— 用户明确要的就是"不会切走"。

**唯一的注意点**：`Start()` 里那次赋值为 `Fella`，发生在场景加载时 ——
**如果在那之前人物已经在坐姿，这次赋值不会被拦**
但 `Start()` 只在场景初始化跑一次，那时还没进坐姿，所以实际不成问题。

---

## 37. 游戏变卡：每帧的反射与顶点扫描（已缓存）

**状态： 已优化（2026-10-07）—— 用户报告「游戏变卡了」**

**先排除一个假象**

`rendercheck` 显示 `Time.timeScale = 0.05`  —— 那是之前调试命令留下的**游戏被钳住**，
表现出来就像"卡"。用 `timescale 1` 恢复  **这一条先排掉**，再看真性能。

**真正的性能问题**

加的这些功能里有几处**每帧**在做很贵的事：

| 位置 | 频率 | 代价 |
| --- | --- | --- |
| `PussyAreaScreenRect` ← `Postfix_HitAreaCheckWindows` | **每帧** | `Resources.FindObjectsOfTypeAll` + 遍历 CubismModel **全部 drawable 的全部顶点**做两次坐标变换 |
| `TryGetDrawableScreenRect` ← `DrawHitAreaHighlight` | **OnGUI 每帧 2~3 次** | 同上 |
| `HitAreaDrawableNames()` | OnGUI 每帧 | `FindObjectsOfTypeAll` + 遍历 |
| `TickKissSpeed` / `TickSpankSpeed` / `TickDemand` | 每帧 | `FindObjectsOfTypeAll(Live2D_Animation_SitOsiri)` |

**关键点**：`FindType` **本身有字典缓存** ，但 **`Resources.FindObjectsOfTypeAll` 没有**  ——
而它每次都要**扫一遍所有已加载对象**，是这里最贵的一环。

**修法：短 TTL 缓存**

| 缓存 | TTL | 理由 |
| --- | --- | --- |
| `HitAreaCompCached()` | 1 秒 | 组件实例不会每帧换 |
| `ModelRectCached()` | **0.1 秒** | 包围盒随人物动作变，但 10Hz 足够判定用 |
| `HitAreaDrawableNamesCached()` | 0.5 秒 | 名单几乎不变 |

**效果**：每帧的 `FindObjectsOfTypeAll` 从「几次」降到「大约 10 秒一次」，
顶点扫描从「每帧 2~4 次」降到「每秒 10 次」。

**教训**

> **"每帧调用一次"和"每帧调用一次反射"是两件完全不同的事。**
> `FindType` 有缓存所以看起来没事，但它后面跟着的
> `Resources.FindObjectsOfTypeAll` 才是真正贵的 —— **缓存要加在最贵的那一层**，
> 而不是"给函数加个缓存"就完事。
>
> 另外：**先确认 timeScale 是不是被钳住了**，再谈性能。这个假象差点让我去改一堆没必要改的代码。

---

## 38. 束缚之吻的桥接审计 + 三个缺口 + 「层上直接播」的动画加不上速

**状态： 已补（2026-10-07）—— 用户要求「看一下束缚之吻是不是所有动作都桥接了」**

### 审计结果

游戏侧束缚之吻相关方法 22 个，原来只桥接了 **4 个**（全是「吸精」那一半）：

| 已桥接 | |
| --- | --- |
| `PrepareKissing()` | 后缀：拉长窗口 |
| `KissPrepare解除()` | 前缀+后缀：阻止衰减 |
| `Event_SitKiss吸精1()` | 后缀：连榨绝顶值按伤害折算 |
| `Even_SitKiss吸精OnEnd()` | 前缀：接管连榨上限 |

**缺口（「射精」那一半基本没接）**：

| 方法 | 后果 |
| --- | --- |
| `Event_SitKissSyaseiOnEnd()` | **连榨上限管不到这条路** |
| `Play_SitKissSyasei()` | 绝顶动画拿不到倍速 |
| `DealDamage_SitKiss()` | 这次伤害不计入回归累积 |

**已补三处**（照着同款抄）：

```
束缚之吻补丁已挂载（Event_SitKissSyaseiOnEnd 前缀：射精也受连榨上限管制）
束缚之吻补丁已挂载（Play_SitKissSyasei 后缀：绝顶动画可调速）
束缚之吻补丁已挂载（DealDamage_SitKiss 后缀：伤害计入回归累积）
```

### 又踩「挂错类」

第一次把 `Event_SitKissSyaseiOnEnd` 和 `Play_SitKissSyasei` 挂在 `TabemiControl` 上
→ `找不到 Event_SitKissSyaseiOnEnd`

它们其实在 **`Live2D_Animation_SitOsiri`**（跟吸精那条同一个类）
而 `DealDamage_SitKiss` 确实在 `TabemiControl`

**——「挂完核对日志条数」这条规矩不能省。**
这次是靠日志里那句「找不到…」发现的，不是靠猜。

### 用户后续反馈：「还是有一些动作没能加速」

**根因**：束缚之吻里有一部分动作**不经过混合器**，而是直接在层上 Play：

```csharp
// Play_SitKissSyasei()
_CenterGirlLayer.Play(SitKissSyaseis[num]);
// Play_SitKiss吸精()
_CenterGirlLayer.Play(SitKiss吸精s[num]);
```

而 `TickKissSpeed` 只找 `SitKissMixerA/B` 和 `SitMixerA/B`
→ **这类动画永远命中不了，所以加不上速。**

**修法：混合器都没命中时，退一步改「当前层正在播的那个状态」**

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

**这条规律值得记下来**：
> 这个游戏的动画有**两种播法** —— 经过混合器（`*.State`）和直接在层上 `Play`（`*.CurrentState`）。
> **任何"给正在播的动画调速"的逻辑，两种都要覆盖**，否则总有一部分动作静默地不受控。
> 之前 Osiri 绝顶那次也是栽在同一个地方（`_CenterGirlLayer.Play(OsiriSyaseis[num])`）。

**挂载 42 条。**

---

## 39. 三栏用词统一：描述模板按栏位自动换词

**状态： 已修（2026-10-07）—— 用户指出「正骑页面里还是有很多"索取"描述」，随后要求「口交那边都用吸取」**

**问题**

配置描述是**一个模板三栏共用**的，而模板里有 **15 条硬编码了"索取"**
（"叠第 1 层索取状态所需的索取欲"这种）

`PoseDesc(i, desc)` 原来只替换 `{模式}` 占位符 ——
**没有占位符的硬编码词就原样漏到另外两栏**

第一次修只动了坐姿，理由是"口交描述里没混进这个词，乱替换可能改坏"。
用户随后明确要求口交也用吸取 —— 于是补上，并**核对替换后的实际文本**（这次没有想当然）。

**修法（一处修全部）**

```csharp
private static string PoseDesc(int i, string desc)
{
    string d = desc.Replace("{模式}", ModeWordAt(i));
    if (i == 2) d = d.Replace("索取", "榨取");        // 正骑
    else if (i == 0) d = d.Replace("索取", "吸取");   // 口交
    // 背榨保持"索取" —— 那是它本来的叫法
    return "【" + PoseLabel[i] + "·" + ModeWordAt(i) + "】" + d;
}
```

**验证（按栏位统计，不是抽查印象）**

```
口交   54 条描述，含「索取」0 条
背榨   54 条描述，含「索取」54 条  （本来的叫法）
正骑   54 条描述，含「索取」0 条

【口交·吸取】每次累积吸取欲的下限（%）
【背榨·索取】每次累积索取欲的下限（%）
【正骑·榨取】每次累积榨取欲的下限（%）
```

**面板标签那边本来就对** —— 5 处含"索取"的标签，
要么在 `if (!sitSide)` 里（只在背榨显示），要么是各栏自己的标题  未改动。

**教训**

> **"共用一份模板" + "模板里有硬编码的专属词" = 迟早串味。**
> 占位符（`{模式}`）只能覆盖到写得规范的那部分；
> 剩下的硬编码词，要么在生成时按栏位统一替换，要么把词也做成占位符。
> **不能靠"写模板的人记得用占位符"。**

---

## 40. 骤降溯源日志 + 说明书补三节

**状态： 已实施（2026-10-07）**

### (1) 骤降溯源

第 39 条推断"骤降来自软上限"，但**平滑明明挂着却没拦住** —— 所以那个推断不完整。
与其继续猜，不如**让每个可能的地方都留痕**：

| 留痕点 | 记什么 |
| --- | --- |
| `Prefix_EcstasyReset`（两个重载） | 清前值 + 姿势/模式/段数/sitState/kissing/osiriState/Syaseing/连榨 |
| 连榨软上限写入处 | cur / target / soft / wob / maxE（仅当掉幅 > 50 时记） |

**下次骤降，日志会直接说"是谁干的"**，不用再靠吻合度猜。

### (2) 说明书补三节

| 新节 | 内容 |
| --- | --- |
| **被上限拉回来时的平滑** | `CapSmoothEnabled` / `CapSmoothRate`，以及为什么做它（38 次 Δ-240 那种跳变） |
| **绝顶值骤降侦测** | `EcstasyDropWarn` + 溯源日志的读法 + "射精期间的消费已排除" |
| **坐姿锁定** | `SitLockEnabled`、两个出口、逃生口（`situnlock` / `sitlock` + 面板按钮） |

**说明书未提及项：12 → 8**（剩 8 项全是斜杠组合提及，功能上已覆盖）。

### (3) #4 状态更正

`xcheck` 的不变量**其实早已扩到 (17)~(23)**（第 34 条），但 #4 一直挂着"技术债"没更新  —— 已改。

> **教训**：**"做完了但状态没更新"和"没做"在账面上是一样的。**
> 盘点时会把它当成待办，浪费一次排查。

---

## S6. 面板布局整理与装饰符清理

**状态：分两部分，一部分已完成，一部分待做**

### 已完成

**排版 helper 与格式统一**

新增 `Section` / `Sub` / `Hint` / `Rule` 四个 helper，间距与格式只有一处定义。
原来是三四种标题写法加四个间距值（4/6/8/10），全靠手写，所以看起来乱。

转换：标题 7 处、分隔线 2 处、缩进说明 10 处。
面板宽度从 452 加到 540，标签不再被挤到贴边。

**抽出一个 297 行方法里的坐姿专属块**

`DrawDemandSection` 原来同时承担背榨与正骑两栏，正骑专属内容
（束缚之吻、坐姿锁定、Manman 区，约 150 行）夹在共享逻辑里。

现在抽成 `DrawSitExtras()`，并把调用点**移到共享内容之后** ——
这一步才是真正解决"读起来是跳的"的关键：原来它在叠层门槛之前就被调用。

抽取时踩了一个坑：第一次用简单的花括号计数，而代码里有
`string.Format("...{0}...")` 这种带花括号的字符串，计数就偏了。
第二次改成**逐字符扫描并跳过字符串与注释**的匹配器，一次成功。

**装饰符与判定符清理，并做成 CI 强制规则**

对照 mega-index-map 的三条不变量（输出与词汇保持 ASCII），
清掉了全部装饰符与判定符：圆圈对勾、叉号、警告三角、星号、根号、
实心空心圆、方块、不等号、块元素、双线边框，以及圈码序号。

共 273 处在用户可见字符串里、108 处在文档里、其余散在源码与安装程序。

**允许保留的例外**（写进了检查规则的注释）：
中文排版符号（破折号、间隔号）、数学符号（乘号、正负号、减号、Delta）、
以及全部面向用户的中文文案。理由和 mega 给 README 开例外一样 ——
这是中文面向的项目，全 ASCII 会把正常排版也砍掉。

新增检查规则 (9)，每次推送强制。表用**码点构造**而不是字面量，
因为清理过程中发生过一次"清理脚本把检查器自己的符号表也替换掉了"的事故。

### 待做

**面板布局只整理了一半**。格式统一了、最大的一个块抽出来了，
但 `DrawDemandSection` 仍有 146 行，而且各栏的小节顺序还没有系统梳理过。

**建议**：一次处理一个栏，改完立刻编译并截图对比，
不要一次动多个栏 —— 这次的经验是布局改动很容易在括号层面出错。

### 顺带发现

跑 xcheck 时，(16) 生命当前值未越上限 报了一次硬性矛盾：
`currentHP=692.09 maxHP=691.08`，超出约 1.01。

这条不是本次改动引起的，但不变量自己写着成因：
锁上限每帧把上限钉回基线，而榨取的上限削减与它互相拉扯。

**但本次新加的平滑（EaseToward）会让这个现象持续更久** ——
原来是一次拉到位，现在是按速率逼近，所以当前值留在上限之上的时间变长。

1.01 点在 691 上是 0.15%，不影响功能，但可以让这条不变量更准：
要么把容差从 0.5 放宽，要么给上限也加同样的平滑。**留待决定。**

---

## S7. 汉堡制作倒计时：已实现，行为未实测

**状态：待实测**

功能已加好，补丁确认挂载：

```
倒计时补丁已挂载（MenuControl.Timing 前缀：每单时长 / 速度）
挂载点 36 个（原 35）
BurgerTimeAll = 0    BurgerTimeSpeed = 100
set BurgerTimeAll 25   已设置并落盘
set BurgerTimeSpeed 40 已设置并落盘
```

**但没有真的看过倒计时变慢或冻结。**

原因：要验证它，游戏必须处在「下单之后、做汉堡途中」那个状态。
实施时游戏停在背榨的战斗状态里，进不去那个场合。

**验证方法**（下次进游戏时）：
1. 面板调到「宽松（30 秒 / 半速）」
2. 正常玩到下单，看角上的秒数是不是从 30 开始、而且走得慢
3. 调到「冻结」，确认秒数不再下降、也不会判超时

**注意一处理论风险**：`BurgerTimeAll` 是在每帧的前缀里写 `timeAll` 的，
而游戏的 `BuildMenu` 里是 `if (resetTime) timeLeft = timeAll;`。
如果某次下单 `resetTime` 为假，那一单就不会用新时长。
这一点没法从静态代码确认，要实测。

---

## S8. 屏幕中间的模式名一律显示「索取」（已修）

**状态：已修复并实测**

### 症状

屏幕中间的横幅、以及命令回执，不管当前是哪一栏，一律说「索取模式」。
口交应该显示吸取、正骑应该显示榨取。

### 根因

`DemandCmd()` 里有四处**写死**的「索取模式」：

```
return "索取模式 = 开";
return "索取模式 = 关";
ShowChainBanner("索取模式", 3f);
return "已手动进入索取模式，持续 " ...
```

`ModeWord()` 本身是对的（按姿势返回 吸取/榨取/索取），
但这四处根本没调它。是个纯粹的遗漏。

### 修法

新增 `ModeWordForBanner()`，并且**不用当前姿势，而用 `_demandModePose`**：

```csharp
int p = _demandModePose >= 0 ? _demandModePose : PoseIdx();
return ModeWordAt(p);
```

理由：模式可能**跨姿势持续**（`_demandModePose` 记的是进入时的姿势），
而 `ModeWord()` 用当前姿势 —— 模式中途换了姿势，横幅就会显示成另一个词。

四处全部改成调它。

### 实测

游戏当时正好在口交姿势：

```
[Fella·none] 吸取模式中 1 段 / 共剩余 73.6s
已手动进入吸取模式，持续 136.4s
吸取模式 = 关
```

修之前这三行都会是「索取」。

### 顺带修的

`defaults` 命令的文案里也写死了「索取模式」，而那三栏共用一份，改成「模式参数（三栏共用一份）」。

### 核查过但没问题的

- `DrawDemandSection(bool sitSide)` 只被背榨和正骑调用，口交不走它 ——
  所以里面 `sitSide ? "榨取" : "索取"` 的两分支是对的，不是缺口交
- 连榨上限那两个标签（骑乘位·索取模式 / 坐姿·榨取模式）写死了也没错，
  它们本来就各指一栏

---

## S9. Manman 判定区可能整块跑到屏幕外（已加防护）

**状态：已加两道闸，目标情形待复现**

### 症状

xcheck 第 (23) 条曾报出：

```
矩形 754~1194 x -114~82
```

x 正常，但 **y 是负的** —— 整块判定区在屏幕外，一个像素都点不到。
而这个区存在的唯一意义就是被点。

### 为什么自检没拦住

(23) 只检查「矩形可取」—— 它确实取得出来，只是位置是废的。
**"能取到"和"能用"是两件事**，不变量只覆盖了前者。

### 根因（推测）

判定区由模型包围盒推出：

```csharp
float cy = my0 + SitPussyAreaCY.Value * mh;   // CY 默认 0.62
float hh = SitPussyAreaH.Value * mh * 0.5f;
y0 = cy - hh;  y1 = cy + hh;
```

模型不在场时 `ModelRectCached` 可能给出 0 或负的宽高，
这样推出来的矩形自然是垃圾值。面板里那句
「拿不到模型包围盒（人物不在场？）」就是这个状态的提示。

### 修法

加两道闸：

1. **退化检测**：`mw < 1f || mh < 1f` 直接判不可用
2. **与屏幕求交**：求出屏幕内的部分，不足 4px 判不可用；
   有交集就返回交集（部分在屏幕内仍然可点）

用求交而不是直接判否，是因为判定区部分出界时仍然可点，
只有完全没交集才该判"用不了"。

### 未验证的部分

**目标情形没复现出来。** 部署后游戏停在非坐姿状态，
(23) 报的是「不需要（不在坐姿或未启用）」。

要复现得让角色进入坐姿、且模型包围盒拿不到值。
**下次在坐姿里跑一次 xcheck，看 (23) 是否还报负数。**

---

## S10. 告警分级配色 + (16) 的幅度其实会很大

**状态：分级已实施 / (16) 的定性需要重新判断**

### 分级配色（已实施）

提示与告警按五级走，从小到大：

```
白   无问题
蓝   提示（最低）
绿   注意
黄   异常
红   严重（最高）
```

实现：`Sev` 枚举 + `SevColor()` + `SetHint(text, sev)`，
接到面板底部那行状态文字的绘制上（单独一个样式，不复用 `Hint()` 的 ——
复用会把所有 Hint 的颜色一起改掉）。

`CrossCheck()` 结束时按结果自己定级：

```
有硬性矛盾    → 红
1~2 条提醒    → 黄
3~5 条提醒    → 绿
6 条以上提醒  → 蓝（提醒太多说明该整理检查项了）
全过          → 白
```

文本输出里也带上标签（`[黄·异常]`），这样落盘的 txt 里也看得出等级。

### 顺带修掉两个自己造的 bug

**(1) 格式串被写成了字面量**

`(16)` 的详情原来显示成：

```
currentHP={0:0.##} maxHP={1:0.##}{2}
```

原因：写 `string.Format` 时把花括号转义成了 `{{0}}`，
但那段代码不是 f-string，`{{` 原样进了 C# —— 于是它成了字面量。

**改法：干脆不用 `string.Format`，改成字符串拼接。** 没有转义层就不会再错。

**(2) `_hintStyle` 重名**

分级配色本来想复用一个样式字段，而那个名字已经被 `Hint()` 用了。
更关键的是：**复用会把所有 `Hint()` 的颜色一起改掉**。
改成单独的 `_statusStyle`。

### (16) 的幅度需要重新判断

之前我看到的是超出 **0.91**，据此认为"0.2%，可以忽略"，按用户的意思降级成了提醒。

**但分级做好之后再跑，看到的是：**

```
currentHP=394.09 maxHP=328.41 ← 超出 65.68（占上限 20.0%）
```

**20%，不是 0.2%。** 差了 100 倍。

所以那个"已知会报、不用管"的定性**可能是错的** ——
上限被削到 328 而当前值停在 394，血条会明显超格。

**超出量是变化的**（0.9 ~ 65 都见过），说明它取决于上限削减的速率与平滑的博弈，
不是一个固定的小偏移。

**待用户重新判断**：如果 20% 那种情况常见，这条就不该按"无害"处理，
而应该去修根因（每帧把 `curHp` 钳到 `maxHp`，让这个状态根本不存在）。

### 另一处观察：Manman 区贴到了屏幕下缘

这一跑的矩形是 `754~1194 x 0~86`，**y 从 0 开始**。

屏幕下方 0~54 是游戏的**视角拖动带**（`edgeRatio = 0.05`），
也就是说这个矩形有一半压在拖动带上 —— 点它的下半部分会变成拖视角。

这不一定是缺陷（人物位置不同，矩形就会不同），但值得记：
**判定区贴边时要留意和拖动带的重叠**。
