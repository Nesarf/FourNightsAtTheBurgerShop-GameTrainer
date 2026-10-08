using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BurgerShopModder
{
    /// <summary>
    /// Four Nights at the Burger Shop — 数值修改器
    /// 用反射操作游戏托管字段，不修改原始 Assembly-CSharp.dll，卸载 BepInEx 即完全还原。
    /// </summary>
    [BepInPlugin(Guid, "Burger Shop 数值修改器", "1.0.0")]
    [BepInProcess("Four Nights at the Burger Shop.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static Plugin _pluginInstance;
        public const string Guid = "nesarf.burgershop.modder";

        internal static ManualLogSource Log;

        // ---- 玩家（被喂食的一方） ----
        internal static ConfigEntry<int> ConfigVersion;
        internal static ConfigEntry<bool> GodMode;
        internal static ConfigEntry<bool> NoEcstasy;
        internal static ConfigEntry<bool> LockMaxHp;
        internal static ConfigEntry<bool> LockMaxEcstasy;
        internal static ConfigEntry<float> TargetHp;
        internal static ConfigEntry<float> TargetEcstasy;
        internal static ConfigEntry<float> CapStepPerSec;
        internal static ConfigEntry<float> HpDownRate;
        internal static ConfigEntry<float> HpUpRate;
        internal static ConfigEntry<float> EcstasyUpRate;
        internal static ConfigEntry<float> EcstasyDownRate;
        internal static ConfigEntry<float> EcstasyResist;
        internal static ConfigEntry<bool> TremorEnabled;
        internal static ConfigEntry<float> TremorChance;
        internal static ConfigEntry<float> TremorDuration;
        internal static ConfigEntry<float> TremorAmplitude;
        internal static ConfigEntry<float> TremorLoss;
        internal static ConfigEntry<float> TremorCatch;
        internal static ConfigEntry<float> TremorCooldown;
        internal static ConfigEntry<bool> TremorResonate;
        internal static ConfigEntry<float> TremorSlow;
        internal static ConfigEntry<int> TremorPattern;
        internal static ConfigEntry<float> TremorWeaken;
        internal static ConfigEntry<float> TremorSafetyGate;
        internal static ConfigEntry<bool> ChainEcstasyGain;
        internal static ConfigEntry<float> ChainGainCap;
        internal static ConfigEntry<float> ChainGainPerHp;
        internal static ConfigEntry<float> ChainGainPerDrain;
        internal static ConfigEntry<float> ChainSoftCap;
        internal static ConfigEntry<float> ChainWobble;
        internal static ConfigEntry<float> ChainWobbleHz;
        internal static ConfigEntry<bool> TremorZeroHold;
        internal static ConfigEntry<float> TremorZeroHoldTime;
        internal static ConfigEntry<float> TremorZeroHoldAmp;
        internal static ConfigEntry<float>[] DemandThresholdBase3;
        internal static ConfigEntry<float>[] DemandThresholdStep3;
        internal static ConfigEntry<float>[] DemandUrgeMin3;
        internal static ConfigEntry<float>[] DemandUrgeMax3;
        internal static ConfigEntry<float>[] DemandAttBonus3;
        internal static ConfigEntry<float>[] DemandAttSpeedBonus3;
        internal static ConfigEntry<float>[] DemandAttSpeedRef3;
        internal static ConfigEntry<float>[] DemandAttSpeedExp3;
        internal static ConfigEntry<float>[] AttackUrgeChance3;
        internal static ConfigEntry<float>[] AttackUrgeGain3;
        internal static ConfigEntry<float>[] AttackUrgeJitter3;
        internal static ConfigEntry<float>[] SyaseiUrgeChance3;
        internal static ConfigEntry<float>[] SyaseiUrgeGain3;
        internal static ConfigEntry<float>[] KyuseiUrgeChance3;
        internal static ConfigEntry<float>[] KyuseiUrgeGain3;
        internal static ConfigEntry<bool>[] SpankSpeedEnabled3;
        internal static ConfigEntry<float>[] SpankSpeedGain3;
        internal static ConfigEntry<float>[] SpankSpeedStack3;
        internal static ConfigEntry<float>[] SpankSpeedRise3;
        internal static ConfigEntry<float>[] SpankSpeedDecay3;
        internal static ConfigEntry<bool>[] ChainEcstasyGain3;
        internal static ConfigEntry<float>[] ChainGainPerHp3;
        internal static ConfigEntry<float>[] ChainSoftCap3;
        internal static ConfigEntry<float>[] ChainWobble3;
        internal static ConfigEntry<float>[] ChainWobbleHz3;
        internal static ConfigEntry<bool>[] OsiriReturnEnabled3;
        internal static ConfigEntry<float>[] OsiriReturnTrigger3;
        internal static ConfigEntry<float>[] OsiriReturnGain3;
        internal static ConfigEntry<float>[] OsiriReturnJitter3;
        internal static ConfigEntry<int>[] AfterglowPerSyasei3;
        internal static ConfigEntry<float>[] AfterglowSettleDelay3;
        internal static ConfigEntry<float>[] AfterglowSpeed3;
        internal static ConfigEntry<float>[] AfterglowSpeedWobble3;
        internal static ConfigEntry<float>[] AfterglowSpeedHz3;
        internal static ConfigEntry<float>[] AfterglowSoftCap3;
        internal static ConfigEntry<float>[] AfterglowEcstasyWobble3;
        internal static ConfigEntry<float>[] AfterglowUrgeScale3;
        internal static ConfigEntry<bool>[] AfterglowClearsUrge3;
        internal static ConfigEntry<bool>[] AfterglowUseDemandGain3;
        internal static ConfigEntry<float>[] DemandAnimSpeed3;
        internal static ConfigEntry<float>[] DemandEscapePenaltyChance3;
        internal static ConfigEntry<float>[] DemandEscapePenaltyMul3;
        internal static ConfigEntry<float>[] DemandDurationMin3;
        internal static ConfigEntry<float>[] DemandDurationMax3;
        internal static ConfigEntry<int>[] DemandTriggerMode3;
        internal static ConfigEntry<float> DemandMaxStackRefreshMul;
        internal static ConfigEntry<int>[] DemandMaxStacks3;
        internal static ConfigEntry<int>[] DemandOsiriBoost3;
        internal static ConfigEntry<bool>[] DemandEnabled3;
        internal static ConfigEntry<bool>[] AttackCanStackDemand3;
        internal static ConfigEntry<bool>[] DemandSpeedSplit3;
        internal static ConfigEntry<bool>[] DemandBlockFella3;
        internal static ConfigEntry<int> ChainMaxFella;
        internal static ConfigEntry<int> ChainMaxOsiriNormal;
        internal static ConfigEntry<int> ChainMaxOsiriDemand;
        internal static ConfigEntry<int> ChainMaxSitNormal;
        internal static ConfigEntry<int> ChainMaxSitDemand;
        internal static ConfigEntry<int> KyuseiMaxChain;   // 已废弃：被上面四个取代
        internal static ConfigEntry<bool> DemandEnabled;
        internal static ConfigEntry<int> DemandTriggerMode;
        internal static ConfigEntry<float> AfterglowUrgeScale;
        internal static ConfigEntry<bool> AfterglowClearsUrge;
        internal static ConfigEntry<bool> OsiriKyuseiTier4;
        internal static ConfigEntry<bool> SitLockEnabled;
        internal static ConfigEntry<bool> CapSmoothEnabled;
        internal static ConfigEntry<float> CapSmoothRate;
        internal static ConfigEntry<float> EcstasyDropWarn;
        internal static ConfigEntry<bool> KissSpeedEnabled;
        internal static ConfigEntry<float> KissSpeedMul;
        internal static ConfigEntry<float> KissSpeedWobble;
        internal static ConfigEntry<float> KissSpeedHz;
        internal static ConfigEntry<bool> SitKissAutoPrepare;
        internal static ConfigEntry<float> SitKissAutoInterval;
        internal static ConfigEntry<bool> SitKissNoDecay;
        internal static ConfigEntry<float> SitKissWindowMul;
        internal static ConfigEntry<bool> HighlightHitAreas;
        internal static ConfigEntry<string> ManmanDrawableFilter;
        internal static ConfigEntry<int> ManmanDrawablePage;
        internal static ConfigEntry<string> ManmanBindDrawable;
        internal static ConfigEntry<bool> ManmanDragMode;
        internal static ConfigEntry<bool> SitPussyAreaShowRect;
        internal static ConfigEntry<float>[] ChainGainPerDrain3;
        internal static ConfigEntry<float>[] DemandFellaDecay3;
        internal static ConfigEntry<float>[] DemandMaxStackRefreshMul3;
        internal static ConfigEntry<bool> SitPussyAreaEnabled;
        internal static ConfigEntry<float> SitPussyAreaCX;
        internal static ConfigEntry<float> SitPussyAreaCY;
        internal static ConfigEntry<float> SitPussyAreaW;
        internal static ConfigEntry<float> SitPussyAreaH;
        internal static ConfigEntry<bool> SitClickAlwaysAccumulate;
        internal static ConfigEntry<float> SitSyaseiToDrainChance;
        internal static ConfigEntry<bool> AttackCanStackDemand;
        internal static ConfigEntry<float> DemandThresholdBase;
        internal static ConfigEntry<float> DemandThresholdStep;
        internal static ConfigEntry<float> DemandUrgeMin;
        internal static ConfigEntry<float> DemandUrgeMax;
        internal static ConfigEntry<float> DemandEscapePenaltyChance;
        internal static ConfigEntry<float> DemandEscapePenaltyMul;
        internal static ConfigEntry<bool> DemandBlockFella;
        internal static ConfigEntry<float> DemandFellaDecay;
        internal static ConfigEntry<int> DemandOsiriBoost;
        internal static ConfigEntry<bool> OsiriReturnEnabled;
        internal static ConfigEntry<float> OsiriReturnTrigger;
        internal static ConfigEntry<float> OsiriReturnGain;
        internal static ConfigEntry<float> OsiriReturnJitter;
        internal static ConfigEntry<float> SyaseiUrgeChance;
        internal static ConfigEntry<float> SyaseiUrgeGain;
        internal static ConfigEntry<float> KyuseiUrgeChance;
        internal static ConfigEntry<float> KyuseiUrgeGain;
        internal static ConfigEntry<float> AttackUrgeJitter;
        internal static ConfigEntry<float> AttackUrgeChance;
        internal static ConfigEntry<float> AttackUrgeGain;
        internal static ConfigEntry<int> DemandMaxStacks;
        internal static ConfigEntry<bool> SpankSpeedEnabled;
        internal static ConfigEntry<float> SpankSpeedGain;
        internal static ConfigEntry<float> SpankSpeedStack;
        internal static ConfigEntry<float> SpankSpeedRise;
        internal static ConfigEntry<float> SpankSpeedDecay;
        internal static ConfigEntry<bool> DemandSpeedSplit;
        internal static ConfigEntry<float> DemandAnimSpeed;
        internal static ConfigEntry<float> DemandAttBonus;
        internal static ConfigEntry<float> DemandAttSpeedBonus;
        internal static ConfigEntry<float> DemandAttSpeedRef;
        internal static ConfigEntry<float> DemandDurationMin;
        internal static ConfigEntry<float> DemandDurationMax;
        internal static ConfigEntry<float> AfterglowSpeedWobble;
        internal static ConfigEntry<float> AfterglowSpeedHz;
        internal static ConfigEntry<float> AfterglowSoftCap;
        internal static ConfigEntry<float> AfterglowEcstasyWobble;
        internal static ConfigEntry<float> AfterglowSpeed;
        internal static ConfigEntry<bool> AfterglowUseDemandGain;
        internal static ConfigEntry<int> AfterglowPerSyasei;
        internal static ConfigEntry<float> AfterglowSettleDelay;
        internal static ConfigEntry<int> DemandAfterglowMin;
        internal static ConfigEntry<int> DemandAfterglowMax;
        internal static ConfigEntry<float> TremorAdaptTime;
        internal static ConfigEntry<float> TremorAdaptFactor;
        internal static ConfigEntry<float> TremorHpMaxBonus;
        internal static ConfigEntry<float> TremorHpGuard;
        internal static ConfigEntry<float> TremorHpRegen;
        internal static ConfigEntry<int> MaxBurgerNum;
        internal static ConfigEntry<int> SyaseiCount;

        // ---- 吃汉（攻击方） ----
        internal static ConfigEntry<bool> PowerUnlock;
        internal static ConfigEntry<float> TabemiPowerMul;
        internal static ConfigEntry<float> FellaSpeedPlus;
        internal static ConfigEntry<float> SitSpeedPlus;

        // ---- 流程 ----
        internal static ConfigEntry<bool> FrozenClock;
        internal static ConfigEntry<int> ClockFullTime;
        internal static ConfigEntry<float> GameSpeed;
        internal static ConfigEntry<float> BurgerTimeAll;
        internal static ConfigEntry<float> BurgerTimeSpeed;
        internal static ConfigEntry<bool> UnlockAllDays;

        private bool _showPanel = true;
        // 344 太窄：滑块一行要放「标签 − 滑条 + 数值」，再横排两三个按钮就挤爆了。
        private Rect _win = new Rect(24f, 90f, 452f, 0f);
        private float _slowTimer;
        private bool _appliedUnlock;

        private void Awake()
        {
              // 【必须在最前面】下面 BindP3 那一批用的是 _pluginInstance.Config，
              // 而这个赋值原本在第 564 行 —— 晚于它们，于是 Awake 里 NRE、
              // 插件没初始化、游戏里连面板都打不开。
              _pluginInstance = this;
            Log = Logger;

            // 【配置版本】用于自动迁移。
            //   0 = 还没迁移过（老配置 / 全新配置都算 0）
            //   1 = 已经把「单份键」的值搬到「按姿势三份键」
            // 注意默认值必须是 0 —— 老用户的 cfg 里没有这个键，
            // BepInEx 会用默认值创建它；若默认写 1 就永远不会触发迁移。
            ConfigVersion = Config.Bind("0-System", "ConfigVersion", 0,
                "配置版本号（自动迁移用）。不要手动改。");

            GodMode = Config.Bind("1-Player", "GodMode", false,
                "每帧把当前 HP 补满（走游戏自己的 HPChange，界面同步）。注意：这不阻止上限下降。");
            NoEcstasy = Config.Bind("1-Player", "NoEcstasy", false,
                "每帧把绝顶值清零，对方无法让你进入射精状态。");
            LockMaxHp = Config.Bind("1-Player", "LockMaxHp", false,
                "锁住 HP 上限。游戏会在被榨取时按 3%~5% 逐次削减 maxHP（吃汉堡只补当前值，开 DLC 才回涨上限）。");
            LockMaxEcstasy = Config.Bind("1-Player", "LockMaxEcstasy", false,
                "锁住绝顶值上限。游戏会在被榨取时按 1%~5% 逐次削减 maxEcstasy。");
            TargetHp = Config.Bind("1-Player", "TargetHp", 100f,
                new ConfigDescription("HP 上限的目标值（绝对值）。每帧朝它逼近，逼近多少由「逼近速度」决定；" +
                    "0 = 不启用。注意：这是「设到多少」，不是「加百分之多少」——绝不会指数膨胀。",
                    new AcceptableValueRange<float>(0f, 10000f)));
            TargetEcstasy = Config.Bind("1-Player", "TargetEcstasy", 100f,
                new ConfigDescription("绝顶值上限的目标值（绝对值）。0 = 不启用。",
                    new AcceptableValueRange<float>(0f, 10000f)));
            CapStepPerSec = Config.Bind("1-Player", "CapStepPerSec", 20f,
                new ConfigDescription("上限每帧逼近目标的速率（点/秒）。越大越快，越小越平滑。",
                    new AcceptableValueRange<float>(1f, 2000f)));

            // ---- 变化率：按「每次实际变化的具体量」按比例缩放 ----
            // 100% = 原版；0% = 这类变化完全不发生（例如 HP 永不下降）。
            HpDownRate = Config.Bind("1-Player", "HpDownRate", 100f,
                new ConfigDescription("HP 下降率%（每次被榨取掉多少血，按此比例生效）。0 = 永不下降。",
                    new AcceptableValueRange<float>(0f, 200f)));
            HpUpRate = Config.Bind("1-Player", "HpUpRate", 100f,
                new ConfigDescription("HP 回复率%（吃汉堡回多少血，按此比例生效）。",
                    new AcceptableValueRange<float>(0f, 200f)));
            EcstasyUpRate = Config.Bind("1-Player", "EcstasyUpRate", 100f,
                new ConfigDescription("绝顶值上升率%。0 = 完全不增长（等价于不被吸精）。",
                    new AcceptableValueRange<float>(0f, 200f)));
            EcstasyDownRate = Config.Bind("1-Player", "EcstasyDownRate", 100f,
                new ConfigDescription("绝顶值下降率%（自然消退与射精后清零按此比例生效）。0 = 不消退。",
                    new AcceptableValueRange<float>(0f, 200f)));
            EcstasyResist = Config.Bind("1-Player", "EcstasyResist", 0f,
                new ConfigDescription("0 = 无抗性，1 = 完全免疫绝顶（原版仅按 R 时临时拉满）。",
                    new AcceptableValueRange<float>(0f, 1f)));

            // ---- 绝顶动摇（Tremor）：绝顶值累积时随机触发的衰减正弦波动 ----
            TremorEnabled = Config.Bind("1-Player", "TremorEnabled", true,
                "启用「绝顶动摇」：绝顶值每次累积时，按概率触发一次衰减正弦波动，最终落点低于原本会达到的值。");
            TremorChance = Config.Bind("1-Player", "TremorChance", 22f,
                new ConfigDescription("每次累积绝顶值时触发动摇的概率 %。", new AcceptableValueRange<float>(0f, 100f)));
            TremorDuration = Config.Bind("1-Player", "TremorDuration", 1.6f,
                new ConfigDescription("动摇持续时间（秒）。", new AcceptableValueRange<float>(0.3f, 6f)));
            TremorAmplitude = Config.Bind("1-Player", "TremorAmplitude", 90f,
                new ConfigDescription("动摇振幅（占本次增益的 %）。越大晃得越狠。", new AcceptableValueRange<float>(0f, 200f)));
            TremorLoss = Config.Bind("1-Player", "TremorLoss", 85f,
                new ConfigDescription("默认落点损失（占本次增益的 %，实际值在此上下随机 ±15）。"
                    + "可以超过 100 —— 那样落点会压到【波动之前的值以下】，"
                    + "也就是这一摇不但抹掉本次收益，还倒扣一截，才像「适应」。",
                    new AcceptableValueRange<float>(0f, 150f)));
            TremorCatch = Config.Bind("1-Player", "TremorCatch", 8f,
                new ConfigDescription("动摇前半段按 R 稳住后的落点损失（%）。越小保住越多。",
                    new AcceptableValueRange<float>(0f, 90f)));
            TremorCooldown = Config.Bind("1-Player", "TremorCooldown", 2.5f,
                new ConfigDescription("动摇冷却（秒）。上一次动摇结束后至少隔这么久才可能再次触发。"
                    + "绝顶值是按角色攻击动作【脉冲式】累积的，攻速一快脉冲就密集；没有冷却会让条子一直在抖。",
                    new AcceptableValueRange<float>(0f, 20f)));
            TremorResonate = Config.Bind("1-Player", "TremorResonate", true,
                "让波动频率与攻击脉冲共振：攻速快则抖动更细更快，攻速慢则起伏更沉。");
            TremorSlow = Config.Bind("1-Player", "TremorSlow", 30f,
                new ConfigDescription("动摇期间角色动作速度的【最低】值（%）。"
                    + "动摇让对手手一软：动作变慢 → 攻击脉冲变稀 → 绝顶值真的降下来，形成闭环。"
                    + "实际速度按所选模板在 100% ~ 该值之间起伏。100 = 不减速。",
                    new AcceptableValueRange<float>(10f, 100f)));
            TremorPattern = Config.Bind("1-Player", "TremorPattern", 0,
                new ConfigDescription("减速模板：0=每次随机；1=顿挫（短促方波）；2=痉挛（高频细抖）；"
                    + "3=深陷（长而深，一次到底再慢慢回）；4=迟疑（两次犹豫）；5=潮汐（平滑起伏）。",
                    new AcceptableValueRange<int>(0, 5)));
            TremorAdaptTime = Config.Bind("1-Player", "TremorAdaptTime", 4f,
                new ConfigDescription("「绝顶适应」持续时间（秒）。动摇结束后这段时间内，后续的绝顶值累积被削减 —— "
                    + "这才是它作为防御手段的实际作用，而不只是动画变慢。",
                    new AcceptableValueRange<float>(0f, 30f)));
            TremorWeaken = Config.Bind("1-Player", "TremorWeaken", 25f,
                new ConfigDescription("动摇【期间】的攻击削弱（%）。手一软：这段时间里打进来的绝顶值只剩这么多。"
                    + "和「绝顶下降率」一起决定净效果 —— 削弱压住累积、消退照常跑，"
                    + "下降率高的时候绝顶值就会【持续下降】。100 = 不削弱。",
                    new AcceptableValueRange<float>(0f, 100f)));
            TremorSafetyGate = Config.Bind("1-Player", "TremorSafetyGate", 90f,
                new ConfigDescription("【安全闸】绝顶值到达上限的这个百分比之后，动摇/适应【不再削弱】累积。"
                    + "必须保留这道闸：绝顶值一旦永远到不了上限，"
                    + "Update_Syasei() 就不会置位 Syaseing，榨取循环永远不会收束（表现为「持续被榨精」）。"
                    + "100 = 闸门关闭（不推荐）。", new AcceptableValueRange<float>(50f, 100f)));
            // ---- 清零后保持波动 ----
            // 思路：与其和"连榨判定"硬碰，不如让绝顶值永远够不到上限 ——
            // Update_Syasei() 的条件是 CurrentEcstasy >= maxEcstasy，
            // 值一直在低位振荡就永远不满足，新的榨取也就不会被触发。
// [已迁移到按姿势三份]             ChainEcstasyGain = Config.Bind("1-Player", "ChainEcstasyGain", true,
            //                 "连榨本身也累积绝顶值。游戏原码 EcstasyChange() 的守卫是 "
            //                     + "if (!Syaseing || !(amount > 0f)) —— Syaseing 为真时【正值增量被整段忽略】，"
            //                     + "所以连榨期间绝顶值永远涨不上去。这里在后缀里把它补上，"
            //                     + "但上限压在安全闸之下，避免连榨一结束就又触发一次。")
            ChainEcstasyGain3 = BindP3Bool("ChainEcstasyGain", true, "连榨本身也累积绝顶值。游戏原码 EcstasyChange() 的守卫是 ");
            ChainGainCap = Config.Bind("1-Player", "ChainGainCap", 90f,
                new ConfigDescription("上面那条的封顶（占上限的 %）。必须低于触发线，"
                    + "否则连榨结束的瞬间会立刻又触发一次榨取。",
                    new AcceptableValueRange<float>(10f, 98f)));
// [已迁移到按姿势三份]             ChainGainPerHp = Config.Bind("1-Player", "ChainGainPerHp", 100f,
            //                 new ConfigDescription("连榨的绝顶值【按这次榨取对生命值的影响】折算的比例（%）。"
            //                     + "算法：伤害占最大生命的百分比 × 这个比例 = 绝顶值增量（占最大值 %）。"
            //                     + "例：骑乘位每次 5 点伤害 / 200 最大生命 = 2.5% → 比例 100% 时每次 +2.5%。"
            //                     + "用的是伤害【常量本身】而非实际掉血量，所以开了无敌 / 锁血也照样计算。"
            //                     + "设为 0 则改用下面的固定值模式。", new AcceptableValueRange<float>(0f, 2000f)))
            ChainGainPerHp3 = BindP3("ChainGainPerHp", 100f, "连榨的绝顶值【按这次榨取对生命值的影响】折算的比例（%）。", 0f, 2000f);
// [已迁移到按姿势三份]             ChainGainPerDrain = Config.Bind("1-Player", "ChainGainPerDrain", 8f,
            //                 new ConfigDescription("每一次连榨给绝顶值加多少（占最大值的 %）。"
            //                     + "注意：榨取动画本身【不产生】绝顶值增量（Event_Osiri吸精Damage 只扣血），"
            //                     + "所以这个量必须由插件自己加。",
            //                     new AcceptableValueRange<float>(0f, 100f)))
            ChainGainPerDrain3 = BindP3("ChainGainPerDrain", 8f, "每一次连榨给绝顶值加多少（占最大值的 %）。", 0f, 100f);;
// [已迁移到按姿势三份]             ChainSoftCap = Config.Bind("1-Player", "ChainSoftCap", 60f,
            //                 new ConfigDescription("连榨期间绝顶值的【软上限】（占最大值的 %）。"
            //                     + "涨到这个数值后不再继续上涨。", new AcceptableValueRange<float>(10f, 98f)))
            ChainSoftCap3 = BindP3("ChainSoftCap", 60f, "连榨期间绝顶值的【软上限】（占最大值的 %）。", 10f, 98f);
// [已迁移到按姿势三份]             ChainWobble = Config.Bind("1-Player", "ChainWobble", 20f,
            //                 new ConfigDescription("到位后的波动幅度（占最大值的 %）。软上限 60 + 幅度 20 "
            //                     + "= 在 40~80% 之间起伏。0 = 不波动、定在软上限。",
            //                     new AcceptableValueRange<float>(0f, 40f)))
            ChainWobble3 = BindP3("ChainWobble", 20f, "到位后的波动幅度（占最大值的 %）。软上限 60 + 幅度 20 ", 0f, 40f);
// [已迁移到按姿势三份]             ChainWobbleHz = Config.Bind("1-Player", "ChainWobbleHz", 0.8f,
            //                 new ConfigDescription("到位后波动的频率（Hz）。", new AcceptableValueRange<float>(0.05f, 8f)))
            ChainWobbleHz3 = BindP3("ChainWobbleHz", 0.8f, "到位后波动的频率（Hz）。", 0.05f, 8f);
            TremorZeroHold = Config.Bind("1-Player", "TremorZeroHold", true,
                "绝顶值被清零后，让它继续在一段低位上振荡（而不是死平在 0）。"
                    + "这样值永远够不到上限，Update_Syasei() 不再触发，连榨循环就出不来。");
            TremorZeroHoldTime = Config.Bind("1-Player", "TremorZeroHoldTime", 8f,
                new ConfigDescription("清零后的持续振荡时长（秒）。", new AcceptableValueRange<float>(1f, 60f)));
            TremorZeroHoldAmp = Config.Bind("1-Player", "TremorZeroHoldAmp", 8f,
                new ConfigDescription("清零后振荡的幅度（占上限的 %）。必须远小于安全闸，"
                    + "否则又会把值顶到上限。", new AcceptableValueRange<float>(1f, 50f)));
            // ---- 连榨上限：按【姿势 × 是否索取/榨取模式】分成四套 ----
            // 原来只有一个 KyuseiMaxChain，两种姿势、两种模式共用同一个上限与同一个计数，
            // 调一个会连带影响另外三个场景。
            DemandThresholdBase3 = BindP3("DemandThresholdBase", 20.0f, "叠第 1 层{模式}状态所需的索取欲（%）", 0.0f, 300.0f);
            DemandThresholdStep3 = BindP3("DemandThresholdStep", 15.0f, "每多叠一层所需的索取欲再增加多少（%）", 0.0f, 300.0f);
            DemandUrgeMin3 = BindP3("DemandUrgeMin", 5.0f, "每次累积索取欲的下限（%）", 0.0f, 30.0f);
            DemandUrgeMax3 = BindP3("DemandUrgeMax", 10.0f, "每次累积索取欲的上限（%）", 0.0f, 30.0f);
            DemandAttBonus3 = BindP3("DemandAttBonus", 50.0f, "{模式}模式期间攻击力提升（%）", 0.0f, 500.0f);
            DemandAttSpeedBonus3 = BindP3("DemandAttSpeedBonus", 60.0f, "攻击力随攻击速度被动波动的幅度（%）", 0.0f, 500.0f);
            DemandAttSpeedRef3 = BindP3("DemandAttSpeedRef", 50.0f, "上面那条的参考速度增幅（%）", 5.0f, 300.0f);
            DemandAttSpeedExp3 = BindP3("DemandAttSpeedExp", 2f, "攻击力随速度曲线的【指数】（0 = 线性，越大越前段平后段陡）", 0f, 8f);
            DemandAnimSpeed3 = BindP3("DemandAnimSpeed", 150.0f, "绝顶/吸精动画的播放倍速（%）", 50.0f, 800.0f);
            DemandEscapePenaltyChance3 = BindP3("DemandEscapePenaltyChance", 35.0f, "脱出惩罚的触发概率（%）", 0.0f, 100.0f);
            DemandEscapePenaltyMul3 = BindP3("DemandEscapePenaltyMul", 135.0f, "脱出所需点击次数的倍数（%）", 100.0f, 500.0f);
            DemandDurationMin3 = BindP3("DemandDurationMin", 12.0f, "{模式}模式最短持续（秒）", 2.0f, 300.0f);
            DemandDurationMax3 = BindP3("DemandDurationMax", 25.0f, "{模式}模式最长持续（秒）", 2.0f, 300.0f);
            DemandTriggerMode3 = BindP3Int("DemandTriggerMode", 0, "叠层判定方式：0=概率模式，1=阈值模式", 0, 1);
// [已迁移到按姿势三份]             DemandMaxStackRefreshMul = Config.Bind("1-Player", "DemandMaxStackRefreshMul", 100f,
            //                 new ConfigDescription("叠满段数上限后，达成叠层条件时【刷新持续时间】给多少（%）。"
            //                     + "0 = 刷新不给时间（保持原行为：什么都不做）。",
            //                     new AcceptableValueRange<float>(0f, 300f)))
            DemandMaxStackRefreshMul3 = BindP3("DemandMaxStackRefreshMul", 100f, "叠满段数上限后，达成叠层条件时【刷新持续时间】给多少（%）。", 0f, 300f);;
            DemandMaxStacks3 = BindP3Int("DemandMaxStacks", 5, "最多叠加段数", 1, 20);
            DemandOsiriBoost3 = BindP3Int("DemandOsiriBoost", 1, "回口交后把骑乘位出现门槛降低多少", 0, 10);
            DemandEnabled3 = BindP3Bool("DemandEnabled", true, "启用{模式}模式");
            AttackCanStackDemand3 = BindP3Bool("AttackCanStackDemand", true, "角色攻击也参与叠层判定");
            DemandSpeedSplit3 = BindP3Bool("DemandSpeedSplit", true, "分阶段加速（避免与 osiri 加速叠加）");
            DemandBlockFella3 = BindP3Bool("DemandBlockFella", true, "{模式}模式期间不回口交");
            ChainMaxFella = Config.Bind("1-Player", "ChainMaxFella", 6,
                new ConfigDescription("【口交】的连榨次数上限。0 = 不限。",
                    new AcceptableValueRange<int>(0, 50)));
            ChainMaxOsiriNormal = Config.Bind("1-Player", "ChainMaxOsiriNormal", 6,
                new ConfigDescription("【骑乘位·常规】的连榨次数上限。0 = 不限（有卡死游戏的风险）。",
                    new AcceptableValueRange<int>(0, 50)));
            ChainMaxOsiriDemand = Config.Bind("1-Player", "ChainMaxOsiriDemand", 6,
                new ConfigDescription("【骑乘位·索取模式】的连榨次数上限。0 = 不限。",
                    new AcceptableValueRange<int>(0, 50)));
            ChainMaxSitNormal = Config.Bind("1-Player", "ChainMaxSitNormal", 6,
                new ConfigDescription("【坐姿·常规】的连榨次数上限。0 = 不限。",
                    new AcceptableValueRange<int>(0, 50)));
            ChainMaxSitDemand = Config.Bind("1-Player", "ChainMaxSitDemand", 6,
                new ConfigDescription("【坐姿·榨取模式】的连榨次数上限。0 = 不限。",
                    new AcceptableValueRange<int>(0, 50)));
            KyuseiMaxChain = Config.Bind("1-Player", "KyuseiMaxChain", 6,
                new ConfigDescription("吸精【连续次数上限】。修正了游戏的整数除法 bug 之后，"
                    + "发生率 100% 就真的意味着 100% 重播，会变成无限连榨（游戏卡死：Syaseing 永不清除）。"
                    + "这里兜一道：连到这个次数就强制收尾。0 = 不限。",
                    new AcceptableValueRange<int>(0, 50)));

            // ---- 背面骑乘「索取模式」+「余韵」----
            // [已废弃，改为按姿势三份 DemandEnabled3] DemandEnabled = Config.Bind("1-Player", "DemandEnabled", true,
            //                 "启用「索取模式」：背面骑乘时打屁股有概率让角色进入索取模式（为榨精而存在的状态）。");
            // [已废弃，改为按姿势三份 DemandTriggerMode3] DemandTriggerMode = Config.Bind("1-Player", "DemandTriggerMode", 0,
            //                 new ConfigDescription("索取欲怎么决定叠层。" + (char)10
            //                     + "  0 = 概率模式：过了门槛后，仍按索取欲数值当概率掷骰（会【攒到五六成还偶尔叠】）" + (char)10
            //                     + "  1 = 阈值模式：攒到该层门槛就【必定】叠层，索取欲变成纯进度条（可预期）",
            //                     new AcceptableValueRange<int>(0, 1)));
// [已迁移到按姿势三份]             AfterglowUrgeScale = Config.Bind("1-Player", "AfterglowUrgeScale", 20f,
            //                 new ConfigDescription("【余韵期间】所有来源累积索取欲的缩放（%）。"
            //                     + "手动余韵可以当作「强制退出索取模式」的逃生阀，"
            //                     + "但余韵本身会连榨十几次、每次都涨索取欲，"
            //                     + "涨太满就会【余韵一结束马上又进索取模式】。"
            //                     + "调低它就能让余韵之后有一段干净的空档。0 = 余韵期间完全不涨。",
            //                     new AcceptableValueRange<float>(0f, 100f)))
            AfterglowUrgeScale3 = BindP3("AfterglowUrgeScale", 20f, "【余韵期间】所有来源累积索取欲的缩放（%）。", 0f, 100f);
// [已迁移到按姿势三份]             AfterglowClearsUrge = Config.Bind("1-Player", "AfterglowClearsUrge", true,
            //                 "余韵清空后把索取欲一并清零（连同【进过模式】的闸门一起落），"
            //                     + "避免紧接着又触发索取模式。")
            AfterglowClearsUrge3 = BindP3Bool("AfterglowClearsUrge", true, "余韵清空后把索取欲一并清零（连同【进过模式】的闸门一起落），");
            // ---- 坐姿：在【小穴】新建一个点按区 ----
            // 模型里本来有 HitArea_Head_Sit / HitArea_Osiri 这些 Live2D 命名的点按区，
            // 但【没有】小穴那一块（代码里那句 HitArea_Manman 是残留，模型里搜不到）。
            // 所以这里自己判一个矩形：命中就返回 HitArea_Head_Sit，
            // 走既有的點頭部逻辑（会累积榨取欲），不需要改任何资源。
            // ---- 正骑：束缚之吻的专门调整 ----
            // 【为什么需要】正骑状态下，连榨与余韵【只有进入束缚之吻才会发生】：
            //   · 榨取的唯一入口 Play_SitKiss吸精() 会把 kissing 强制置 2（束缚之吻）
            //   · 而进入它要 kissing==2，前置是 PrepareKissing()（0→1）→ StartKISS()（1→2）
            //   · 原版 PrepareKissing() 靠 10% 随机或按键 X，窗口只有 KissPrepareTime（4~5 秒），
            //     而且每次 KissPrepare解除() 都会 KissPrepareTime *= 0.9 —— 越玩越短，最后形同不存在
            // 所以这里给束缚之吻单独开一组调整。
            OsiriKyuseiTier4 = Config.Bind("1-Player", "OsiriKyuseiTier4", true,
                "【连榨撞击声固定用第 3、4 档】其余状态下仍按 osiriSpeed 分档。"
                    + "骑乘撞击声由动画事件 OsiriSE() 触发，按 osiriSpeed 分四档："
                    + "<0.25→1、<0.5→2、<=0.75→3、<=1→4（对应 OsiriList1~4 四组音频）。"
                    + "开着的话：连榨（osiriState == kyusei）期间在 3、4 两档之间随机；"
                    + "其他状态（含普通骑乘）保持原版的速度分档。");
            // ---- 正骑：束缚之吻【自己的】调速 ----
            // 【为什么需要】进入束缚之吻  !=  一定触发连榨 ——
            //   · Play_SitKiss吸精() 才是榨取入口（它把 kissing 置 2）
            //   · 但 PrepareKissing() 让 kissing 变 1 之后，可能一直没进榨取
            //   · 那段时间用的是 SitKissMixer*，跟榨取/绝顶走的是不同的混合器
            // 所以束缚之吻要有自己的一套速度，不能借用 DemandAnimSpeed。
            // ---- 绝顶值【单帧骤降】侦测（第 7 条那个 −195.1 的复发警报）----
            // 当初那条异常是靠人工盯 watch.csv 发现的 —— 那不叫检查手段。
            // 这个每帧比对 CurrentEcstasy，掉幅超过阈值就记一条带上下文的日志，
            // 于是"它到底复发没有"变成日志里能直接搜到的事实。
            // ---- 被上限"拉下来"时的平滑（绝顶值 / 生命，以及同类被钳的数值）----
            // 原来凡是"当前值超过上限/软上限"就【一次设过去】，于是单帧会出现
            // Δ-240 这种跳变（实测抓到 38 次，全是这个造成的）。
            // 这里改成按速率逼近 —— 数值该下来，但不该"啪"一下掉下去。
            SitLockEnabled = Config.Bind("1-Player", "SitLockEnabled", true,
                "【坐姿锁定】进入坐姿后不许被切到口交或骑乘位。"
                    + "正骑的榨取链（连榨/余韵）都建立在「人在坐姿」上，中途被切走会断链。"
                    + "关掉则恢复原版的自动切换。");
            CapSmoothEnabled = Config.Bind("1-Player", "CapSmoothEnabled", true,
                "被上限/软上限拉回来时平滑过渡（而不是瞬间跳到目标）。");
            CapSmoothRate = Config.Bind("1-Player", "CapSmoothRate", 60f,
                new ConfigDescription("平滑速率（每秒最多变化占上限的百分比）。"
                    + "60 = 每秒最多挪上限的 60%；调大更接近原来的瞬间跳变。",
                    new AcceptableValueRange<float>(5f, 1000f)));
            EcstasyDropWarn = Config.Bind("1-Player", "EcstasyDropWarn", 50f,
                new ConfigDescription("绝顶值单帧掉幅超过多少就报警（绝对值）。0 = 关。"
                    + "第 7 条那次是 −195.1，所以默认 50 足够灵敏。"
                    + "注意：射精（Syaseing）期间绝顶值会被正常消费掉，那种骤降已排除，不会报警。",
                    new AcceptableValueRange<float>(0f, 1000f)));
            KissSpeedEnabled = Config.Bind("1-Player", "KissSpeedEnabled", true,
                "【正骑·束缚之吻】单独调速。只在【接吻中但没进榨取】时生效"
                    + "（进了榨取就交给榨取/绝顶那套倍速管，两者不叠加）。");
            KissSpeedMul = Config.Bind("1-Player", "KissSpeedMul", 100f,
                new ConfigDescription("束缚之吻的动画速度（%）。100 = 原速。",
                    new AcceptableValueRange<float>(10f, 400f)));
            KissSpeedWobble = Config.Bind("1-Player", "KissSpeedWobble", 0f,
                new ConfigDescription("速度波动幅度（%）。0 = 不波动。",
                    new AcceptableValueRange<float>(0f, 90f)));
            KissSpeedHz = Config.Bind("1-Player", "KissSpeedHz", 0.5f,
                new ConfigDescription("速度波动的频率（Hz）。",
                    new AcceptableValueRange<float>(0.02f, 4f)));
            SitKissAutoPrepare = Config.Bind("1-Player", "SitKissAutoPrepare", true,
                "【正骑】自动进入束缚之吻（不用再按 X）。坐姿常态下每隔一段时间自动触发一次 PrepareKissing。");
            SitKissAutoInterval = Config.Bind("1-Player", "SitKissAutoInterval", 6f,
                new ConfigDescription("自动进入束缚之吻的间隔（秒）。", new AcceptableValueRange<float>(1f, 60f)));
            SitKissNoDecay = Config.Bind("1-Player", "SitKissNoDecay", true,
                "【正骑】阻止束缚之吻的窗口越玩越短。原版每次解除都会 KissPrepareTime *= 0.9，"
                    + "不开这个的话窗口会衰减到几乎没有。");
            SitKissWindowMul = Config.Bind("1-Player", "SitKissWindowMul", 200f,
                new ConfigDescription("【正骑】束缚之吻窗口长度的倍数（%）。原版 4~5 秒，"
                    + "200% 即 8~10 秒，够你点几下头部。", new AcceptableValueRange<float>(50f, 1000f)));
            HighlightHitAreas = Config.Bind("1-Player", "HighlightHitAreas", false,
                "【实验性】把游戏的可点击区域全部高亮出来（按模型里 HitArea_* 那些 drawable 逐个画框）。"
                    + "口交 / 背榨 / 正骑三个模式共用这一个开关，各自栏里都有按钮。");
            ManmanDrawableFilter = Config.Bind("1-Player", "ManmanDrawableFilter", "",
                "素材绑定的名字筛选（子串匹配，不区分大小写）。留空 = 全部列出。"
                    + "提示：先填 HitArea 看那 5 个可点区，清空后就能绑到任意美术素材（身体/皮肤等）。");
            ManmanDrawablePage = Config.Bind("1-Player", "ManmanDrawablePage", 0,
                new ConfigDescription("素材列表的页码（每页 40 个）。模型 drawable 很多，所以分页。",
                    new AcceptableValueRange<int>(0, 200)));
            ManmanBindDrawable = Config.Bind("1-Player", "ManmanBindDrawable", "",
                "【HitArea_Manman_By_Plugins · 素材绑定】填一个模型 drawable 的名字，判定区就直接用【那个素材的包围盒】。"
                    + "这比手拖矩形更好：它跟着模型的网格形变走，人物怎么动都贴合。"
                    + "留空 = 用手拖的矩形（CX/CY/W/H）。"
                    + "注：Cubism 的 drawable 是烘焙进 moc3 的，运行时【加不了】新的；"
                    + "而游戏的 .cmo3 源工程不存在，所以只能用已有的 drawable 来绑。");
            ManmanDragMode = Config.Bind("1-Player", "ManmanDragMode", false,
                "【HitArea_Manman_By_Plugins · 拖动绑定】开着时，判定区中心实时跟随鼠标；"
                    + "把鼠标移到你想要的位置，再点面板上的「绑定到此位置」，中心就定在那里。");
            SitPussyAreaShowRect = Config.Bind("1-Player", "SitPussyAreaShowRect", true,
                "在屏幕上画出 HitArea_Manman_By_Plugins 的框（粉色，与另外几个 HitArea 同一套可视化逻辑）。");
            SitPussyAreaEnabled = Config.Bind("1-Player", "SitPussyAreaEnabled", true,
                "【坐姿】自建一个 HitArea_Manman_By_Plugins。功能与背榨的「打屁股」对称："
                    + "点击涨榨取欲 + 给坐姿动作一个速度冲量（用 SpankSpeed* 那组，已按姿势三份）。"
                    + "【与头部的区别】头部是解除束缚之吻，两者互不干扰。模型里本来没有这块点按区，"
                    + "这个名字是游戏代码里残留的 HitArea_Manman。");
            SitPussyAreaCX = Config.Bind("1-Player", "SitPussyAreaCX", 0.5f,
                new ConfigDescription("小穴点按区中心：屏幕宽度的百分比。", new AcceptableValueRange<float>(0f, 1f)));
            SitPussyAreaCY = Config.Bind("1-Player", "SitPussyAreaCY", 0.62f,
                new ConfigDescription("小穴点按区中心：屏幕高度的百分比（从下往上）。", new AcceptableValueRange<float>(0f, 1f)));
            SitPussyAreaW = Config.Bind("1-Player", "SitPussyAreaW", 0.10f,
                new ConfigDescription("小穴点按区宽度：屏幕宽度的百分比。", new AcceptableValueRange<float>(0.01f, 0.6f)));
            SitPussyAreaH = Config.Bind("1-Player", "SitPussyAreaH", 0.12f,
                new ConfigDescription("小穴点按区高度：屏幕高度的百分比。", new AcceptableValueRange<float>(0.01f, 0.6f)));
            SitClickAlwaysAccumulate = Config.Bind("1-Player", "SitClickAlwaysAccumulate", true,
                "【修 #6】坐姿下点头部即使在【非束缚之吻】状态也累积榨取欲。"
                    + "原版只有 kissing == 1 那个 4~5 秒窗口才认点击，"
                    + "导致坐姿几乎没有可用的手动累积手段（背面骑乘有「打屁股」这个大目标）。"
                    + "游戏自己的 SitGirlKiss叩く量 不受影响，仍只在原窗口增加。");
            SitSyaseiToDrainChance = Config.Bind("1-Player", "SitSyaseiToDrainChance", 40f,
                new ConfigDescription("【修 #8】坐姿【射精收尾】时直接进入榨取的概率（%）。"
                    + "原版 Event_SitSyasei_OnEnd() 永远 Play_Sit()（回坐姿攻击），"
                    + "而唯一的榨取入口要 kissing == 2（束缚之吻）——"
                    + "实际玩下来到不了接吻态，于是榨取→连榨→余韵整条链都摸不到。0 = 保持原版。",
                    new AcceptableValueRange<float>(0f, 100f)));
            // [已废弃，改为按姿势三份 AttackCanStackDemand3] AttackCanStackDemand = Config.Bind("1-Player", "AttackCanStackDemand", true,
            //                 "【修 #1】角色攻击累积的索取欲也参与【叠层判定】。"
            //                     + "关掉则只有打屁股能叠（原行为：攻击只填进度条、从不消费）。");
            // [已废弃，改为按姿势三份 DemandThresholdBase3] DemandThresholdBase = Config.Bind("1-Player", "DemandThresholdBase", 20f,
            //                 new ConfigDescription("【叠第 1 层索取状态所需的索取欲】（%）。"
            //                     + "索取欲低于它时不会叠层 —— 这就是原来【才攒到十几就叠了一层】的解法。",
            //                     new AcceptableValueRange<float>(0f, 300f)));
            // [已废弃，改为按姿势三份 DemandThresholdStep3] DemandThresholdStep = Config.Bind("1-Player", "DemandThresholdStep", 15f,
            //                 new ConfigDescription("每多叠一层，所需的索取欲再增加多少（%）。"
            //                     + "叠第 N 层所需的索取欲 = 首层 + 递增 × (N-1)。"
            //                     + "默认 20 / 15 → 叠各层分别需要 20 / 35 / 50 / 65 / 80 …",
            //                     new AcceptableValueRange<float>(0f, 300f)));
            // [已废弃，改为按姿势三份 DemandUrgeMin3] DemandUrgeMin = Config.Bind("1-Player", "DemandUrgeMin", 5f,
            //                 new ConfigDescription("每次打屁股增加的「索取欲」下限（%）。索取欲本身就是进入索取模式的概率。",
            //                     new AcceptableValueRange<float>(0f, 30f)));
            // [已废弃，改为按姿势三份 DemandUrgeMax3] DemandUrgeMax = Config.Bind("1-Player", "DemandUrgeMax", 10f,
            //                 new ConfigDescription("每次打屁股增加的「索取欲」上限（%）。", new AcceptableValueRange<float>(0f, 30f)));
            // [已废弃，改为按姿势三份 DemandEscapePenaltyChance3] DemandEscapePenaltyChance = Config.Bind("1-Player", "DemandEscapePenaltyChance", 35f,
            //                 new ConfigDescription("每次打屁股时，有这个机会【大幅提高脱出背面骑乘位所需的点击次数】。",
            //                     new AcceptableValueRange<float>(0f, 100f)));
            // [已废弃，改为按姿势三份 DemandEscapePenaltyMul3] DemandEscapePenaltyMul = Config.Bind("1-Player", "DemandEscapePenaltyMul", 135f,
            //                 new ConfigDescription("上面那条的倍数（%）。135 = 脱出要求涨到 1.35 倍。",
            //                     new AcceptableValueRange<float>(100f, 500f)));
            // [已废弃，改为按姿势三份 DemandBlockFella3] DemandBlockFella = Config.Bind("1-Player", "DemandBlockFella", true,
            //                 "索取模式期间【不回到口交状态】。游戏的 Event_OsiriSyaseiOnEnd() 里有 10% 概率"
            //                     + "走 Osiri解除() → ShowCenterGirlFella()，这里在索取模式期间把这一支掐掉。");
// [已迁移到按姿势三份]             DemandFellaDecay = Config.Bind("1-Player", "DemandFellaDecay", 25f,
            //                 new ConfigDescription("每进入一次索取模式，「回到口交」的概率衰减多少（%）。"
            //                     + "10% 的基准概率按 (1-衰减)^次数 递减。0 = 不衰减。",
            //                     new AcceptableValueRange<float>(0f, 90f)))
            DemandFellaDecay3 = BindP3("DemandFellaDecay", 25f, "每进入一次索取模式，「回到口交」的概率衰减多少（%）。", 0f, 90f);;
// [已迁移到按姿势三份]             OsiriReturnEnabled = Config.Bind("1-Player", "OsiriReturnEnabled", true,
            //                 "从骑乘位回到口交后，角色每做一次【造成伤害的活动】（攻击 / 射精 / 吸精）"
            //                     + "都有一定概率累积「再次进入骑乘位的概率」。到 100% 时立刻进入骑乘位。"
            //                     + "若此时游戏正准备进入坐姿，则把坐姿挂起，等这次骑乘位结束再进。")
            OsiriReturnEnabled3 = BindP3Bool("OsiriReturnEnabled", true, "从骑乘位回到口交后，角色每做一次【造成伤害的活动】（攻击 / 射精 / 吸精）");
// [已迁移到按姿势三份]             OsiriReturnTrigger = Config.Bind("1-Player", "OsiriReturnTrigger", 30f,
            //                 new ConfigDescription("每次造成伤害的活动，累积成功的【平均】概率（%）。",
            //                     new AcceptableValueRange<float>(0f, 100f)))
            OsiriReturnTrigger3 = BindP3("OsiriReturnTrigger", 30f, "每次造成伤害的活动，累积成功的【平均】概率（%）。", 0f, 100f);
// [已迁移到按姿势三份]             OsiriReturnGain = Config.Bind("1-Player", "OsiriReturnGain", 12f,
            //                 new ConfigDescription("累积成功时，「再次进入骑乘位的概率」增加多少（平均 %）。",
            //                     new AcceptableValueRange<float>(0f, 100f)))
            OsiriReturnGain3 = BindP3("OsiriReturnGain", 12f, "累积成功时，「再次进入骑乘位的概率」增加多少（平均 %）。", 0f, 100f);
// [已迁移到按姿势三份]             OsiriReturnJitter = Config.Bind("1-Player", "OsiriReturnJitter", 50f,
            //                 new ConfigDescription("上面两项的波动幅度（%）：实际值在平均值的 (1-j)~(1+j) 之间随机，"
            //                     + "期望值仍是滑块设的平均值。", new AcceptableValueRange<float>(0f, 90f)))
            OsiriReturnJitter3 = BindP3("OsiriReturnJitter", 50f, "上面两项的波动幅度（%）：实际值在平均值的 (1-j)~(1+j) 之间随机，", 0f, 90f);
            // [已废弃，改为按姿势三份 DemandOsiriBoost3] DemandOsiriBoost = Config.Bind("1-Player", "DemandOsiriBoost", 1,
            //                 new ConfigDescription("真正回到口交之后，把「骑乘位再次出现」的门槛 OsiriPenaltyToAppear "
            //                     + "降低多少（它原本是 第1~4天 = 10/9/8/7）。越低来得越快，0 = 不干预。",
            //                     new AcceptableValueRange<int>(0, 10)));
// [已迁移到按姿势三份]             SyaseiUrgeChance = Config.Bind("1-Player", "SyaseiUrgeChance", 100f,
            //                 new ConfigDescription("【射精时】累积索取欲的概率（平均 %）。骑乘位 / 坐姿 / 口交的射精都算。",
            //                     new AcceptableValueRange<float>(0f, 100f)))
            SyaseiUrgeChance3 = BindP3("SyaseiUrgeChance", 100f, "【射精时】累积索取欲的概率（平均 %）。骑乘位 / 坐姿 / 口交的射精都算。", 0f, 100f);
// [已迁移到按姿势三份]             SyaseiUrgeGain = Config.Bind("1-Player", "SyaseiUrgeGain", 8f,
            //                 new ConfigDescription("【射精时】命中后索取欲增加多少（平均 %）。",
            //                     new AcceptableValueRange<float>(0f, 100f)))
            SyaseiUrgeGain3 = BindP3("SyaseiUrgeGain", 8f, "【射精时】命中后索取欲增加多少（平均 %）。", 0f, 100f);
// [已迁移到按姿势三份]             KyuseiUrgeChance = Config.Bind("1-Player", "KyuseiUrgeChance", 100f,
            //                 new ConfigDescription("【每次吸精 / 连榨】累积索取欲的概率（平均 %）。余韵走的就是同一条连榨链，所以余韵也照此累积。",
            //                     new AcceptableValueRange<float>(0f, 100f)))
            KyuseiUrgeChance3 = BindP3("KyuseiUrgeChance", 100f, "【每次吸精 / 连榨】累积索取欲的概率（平均 %）。余韵走的就是同一条连榨链，所以余韵也照此累积。", 0f, 100f);
// [已迁移到按姿势三份]             KyuseiUrgeGain = Config.Bind("1-Player", "KyuseiUrgeGain", 5f,
            //                 new ConfigDescription("【每次吸精 / 连榨】命中后索取欲增加多少（平均 %）。连榨一轮有多次，单次给小一点。",
            //                     new AcceptableValueRange<float>(0f, 100f)))
            KyuseiUrgeGain3 = BindP3("KyuseiUrgeGain", 5f, "【每次吸精 / 连榨】命中后索取欲增加多少（平均 %）。连榨一轮有多次，单次给小一点。", 0f, 100f);
// [已迁移到按姿势三份]             AttackUrgeJitter = Config.Bind("1-Player", "AttackUrgeJitter", 50f,
            //                 new ConfigDescription("攻击涨索取欲时的【波动幅度】（%，围绕平均值上下浮动）。"
            //                     + "50 = 实际值在平均值的 50%~150% 之间随机；期望值仍是滑块设的平均值。",
            //                     new AcceptableValueRange<float>(0f, 90f)))
            AttackUrgeJitter3 = BindP3("AttackUrgeJitter", 50f, "攻击涨索取欲时的【波动幅度】（%，围绕平均值上下浮动）。", 0f, 90f);
// [已迁移到按姿势三份]             AttackUrgeChance = Config.Bind("1-Player", "AttackUrgeChance", 8f,
            //                 new ConfigDescription("骑乘位【常态】（attacking）时，角色每攻击一次有这个概率提升索取欲。"
            //                     + "挂点是 OsiriMixer 的循环事件 Event_OsiriGirlMainMixer，每个攻击循环触发一次。",
            //                     new AcceptableValueRange<float>(0f, 100f)))
            AttackUrgeChance3 = BindP3("AttackUrgeChance", 8f, "骑乘位【常态】（attacking）时，角色每攻击一次有这个概率提升索取欲。", 0f, 100f);
// [已迁移到按姿势三份]             AttackUrgeGain = Config.Bind("1-Player", "AttackUrgeGain", 3f,
            //                 new ConfigDescription("上面那条命中时，索取欲增加多少（%）。",
            //                     new AcceptableValueRange<float>(0f, 100f)))
            AttackUrgeGain3 = BindP3("AttackUrgeGain", 3f, "上面那条命中时，索取欲增加多少（%）。", 0f, 100f);
            // [已废弃，改为按姿势三份 DemandMaxStacks3] DemandMaxStacks = Config.Bind("1-Player", "DemandMaxStacks", 5,
            //                 new ConfigDescription("索取模式最多叠几段。已在索取模式中再次触发时，"
            //                     + "不重新计时，而是把一段时长【加在总时长上】。",
            //                     new AcceptableValueRange<int>(1, 20)));
// [已迁移到按姿势三份]             SpankSpeedEnabled = Config.Bind("1-Player", "SpankSpeedEnabled", true,
            //                 "打屁股给攻击速度一个短促冲量：快速冲上去、缓慢消退。多次打按递减倍率叠加。"
            //                     + "施加点是【主混合器动画的播放速率】—— 因为 osiriSpeed 被游戏钳在 [0,1] 且"
            //                     + "Play_OsiriMainMixer 直接置 1，没有加成的余量。")
            SpankSpeedEnabled3 = BindP3Bool("SpankSpeedEnabled", true, "打屁股给攻击速度一个短促冲量：快速冲上去、缓慢消退。多次打按递减倍率叠加。");
// [已迁移到按姿势三份]             SpankSpeedGain = Config.Bind("1-Player", "SpankSpeedGain", 12f,
            //                 new ConfigDescription("第一次打屁股的冲量（%）。12 = 攻击动画速率冲到 1.12。",
            //                     new AcceptableValueRange<float>(0f, 200f)))
            SpankSpeedGain3 = BindP3("SpankSpeedGain", 12f, "第一次打屁股的冲量（%）。12 = 攻击动画速率冲到 1.12。", 0f, 200f);
// [已迁移到按姿势三份]             SpankSpeedStack = Config.Bind("1-Player", "SpankSpeedStack", 60f,
            //                 new ConfigDescription("叠加倍率（%）：每次新增量 = 上一次新增量 × 这个值。"
            //                     + "60 = 打越多每下加得越少（递减叠加）。", new AcceptableValueRange<float>(0f, 100f)))
            SpankSpeedStack3 = BindP3("SpankSpeedStack", 60f, "叠加倍率（%）：每次新增量 = 上一次新增量 × 这个值。", 0f, 100f);
// [已迁移到按姿势三份]             SpankSpeedRise = Config.Bind("1-Player", "SpankSpeedRise", 9f,
            //                 new ConfigDescription("冲上去的快慢（每秒逼近目标的比例）。", new AcceptableValueRange<float>(1f, 60f)))
            SpankSpeedRise3 = BindP3("SpankSpeedRise", 9f, "冲上去的快慢（每秒逼近目标的比例）。", 1f, 60f);
// [已迁移到按姿势三份]             SpankSpeedDecay = Config.Bind("1-Player", "SpankSpeedDecay", 0.25f,
            //                 new ConfigDescription("消退的快慢（每秒衰减比例）。越小消得越慢、影响留得越久。",
            //                     new AcceptableValueRange<float>(0.01f, 5f)))
            SpankSpeedDecay3 = BindP3("SpankSpeedDecay", 0.25f, "消退的快慢（每秒衰减比例）。越小消得越慢、影响留得越久。", 0.01f, 5f);
            // [已废弃，改为按姿势三份 DemandSpeedSplit3] DemandSpeedSplit = Config.Bind("1-Player", "DemandSpeedSplit", true,
            //                 "加速分阶段，避免叠加：攻击阶段用游戏自己的 osiriSpeed 加速累积，"
            //                     + "绝顶/吸精阶段用「绝顶动画倍速」。"
            //                     + "关掉的话两边会同时生效 —— 实测会叠成过快。");
            // [已废弃，改为按姿势三份 DemandAnimSpeed3] DemandAnimSpeed = Config.Bind("1-Player", "DemandAnimSpeed", 150f,
            //                 new ConfigDescription("索取模式期间【绝顶动画】的播放倍速（%）。"
            //                     + "游戏的 osiriSpeed 只喂给 OsiriMixer，而绝顶播的是同层上的另一个 clip —— "
            //                     + "主混合器被替换后 IsPlaying 为假，osiriSpeed 就完全没被应用，绝顶会回到常规速度。"
            //                     + "这里直接设正在播放的状态速度。100 = 不加速。",
            //                     new AcceptableValueRange<float>(50f, 800f)));
            // [已废弃，改为按姿势三份 DemandAttSpeedBonus3] DemandAttSpeedBonus = Config.Bind("1-Player", "DemandAttSpeedBonus", 60f,
            //                 new ConfigDescription("索取期间【攻击力随攻击速度被动波动】的幅度（%）："
            //                     + "速度越快攻击力越高。驱动量是实际的攻击动画倍率（打屁股冲量推起来的那个），"
            //                     + "因为 osiriSpeed 被 Play_OsiriMainMixer 恒定置 1，没有变化量可用。"
            //                     + "0 = 不随速度变化。", new AcceptableValueRange<float>(0f, 500f)));
            // [已废弃，改为按姿势三份 DemandAttSpeedRef3] DemandAttSpeedRef = Config.Bind("1-Player", "DemandAttSpeedRef", 50f,
            //                 new ConfigDescription("上面那条的参考速度增幅（%）：攻击动画倍率涨到这个幅度时"
            //                     + "吃满速度加成。50 = 倍率到 1.5 时吃满。",
            //                     new AcceptableValueRange<float>(5f, 300f)));
            // [已废弃，改为按姿势三份 DemandAttBonus3] DemandAttBonus = Config.Bind("1-Player", "DemandAttBonus", 50f,
            //                 new ConfigDescription("进入索取模式时【攻击力】提升（%）。攻击力就是 TabemiAtt，"
            //                     + "背面骑乘的伤害与绝顶值都按它算。0 = 不提升。",
            //                     new AcceptableValueRange<float>(0f, 500f)));
            // [已废弃，改为按姿势三份 DemandDurationMin3] DemandDurationMin = Config.Bind("1-Player", "DemandDurationMin", 12f,
            //                 new ConfigDescription("索取模式最短持续（秒）。期间速度不因射精而减缓，且只能由角色自行退出。",
            //                     new AcceptableValueRange<float>(2f, 300f)));
            // [已废弃，改为按姿势三份 DemandDurationMax3] DemandDurationMax = Config.Bind("1-Player", "DemandDurationMax", 25f,
            //                 new ConfigDescription("索取模式最长持续（秒）。", new AcceptableValueRange<float>(2f, 300f)));
// [已迁移到按姿势三份]             AfterglowSpeedWobble = Config.Bind("1-Player", "AfterglowSpeedWobble", 25f,
            //                 new ConfigDescription("余韵动画速度的波动幅度（%，围绕 AfterglowSpeed 上下浮动）。"
            //                     + "余韵是【更低但更绵长】的连榨，速度本身也应该起伏，不该是一条直线。",
            //                     new AcceptableValueRange<float>(0f, 90f)))
            AfterglowSpeedWobble3 = BindP3("AfterglowSpeedWobble", 25f, "余韵动画速度的波动幅度（%，围绕 AfterglowSpeed 上下浮动）。", 0f, 90f);
// [已迁移到按姿势三份]             AfterglowSpeedHz = Config.Bind("1-Player", "AfterglowSpeedHz", 0.35f,
            //                 new ConfigDescription("余韵速度波动的频率（Hz）。慢一点更像【绵长的收尾】。",
            //                     new AcceptableValueRange<float>(0.02f, 4f)))
            AfterglowSpeedHz3 = BindP3("AfterglowSpeedHz", 0.35f, "余韵速度波动的频率（Hz）。慢一点更像【绵长的收尾】。", 0.02f, 4f);
// [已迁移到按姿势三份]             AfterglowSoftCap = Config.Bind("1-Player", "AfterglowSoftCap", 50f,
            //                 new ConfigDescription("余韵期间绝顶值的【软上限】（占最大值 %），与连榨的软上限分开。"
            //                     + "余韵是收尾，理应比连榨更克制。", new AcceptableValueRange<float>(5f, 98f)))
            AfterglowSoftCap3 = BindP3("AfterglowSoftCap", 50f, "余韵期间绝顶值的【软上限】（占最大值 %），与连榨的软上限分开。", 5f, 98f);
// [已迁移到按姿势三份]             AfterglowEcstasyWobble = Config.Bind("1-Player", "AfterglowEcstasyWobble", 15f,
            //                 new ConfigDescription("余韵到位后绝顶值的波动幅度（%）：在 软上限 ± 这个幅度 之间起伏。",
            //                     new AcceptableValueRange<float>(0f, 40f)))
            AfterglowEcstasyWobble3 = BindP3("AfterglowEcstasyWobble", 15f, "余韵到位后绝顶值的波动幅度（%）：在 软上限 ± 这个幅度 之间起伏。", 0f, 40f);
// [已迁移到按姿势三份]             AfterglowSpeed = Config.Bind("1-Player", "AfterglowSpeed", 70f,
            //                 new ConfigDescription("余韵期间的动画速度（%）。"
            //                     + "余韵可以理解为【基础速度更低、单次持续时间更长】的连榨 —— "
            //                     + "降低速度本身就同时拉长了每一次的持续时间，所以这一个旋钮管两件事。"
            //                     + "100 = 与常速相同。", new AcceptableValueRange<float>(10f, 400f)))
            AfterglowSpeed3 = BindP3("AfterglowSpeed", 70f, "余韵期间的动画速度（%）。", 10f, 400f);
// [已迁移到按姿势三份]             AfterglowUseDemandGain = Config.Bind("1-Player", "AfterglowUseDemandGain", true,
            //                 "余韵期间是否仍照常按射精累积余韵（一般应为否：余韵是收尾，不该自我延长）。")
            AfterglowUseDemandGain3 = BindP3Bool("AfterglowUseDemandGain", true, "余韵期间是否仍照常按射精累积余韵（一般应为否：余韵是收尾，不该自我延长）。");
// [已迁移到按姿势三份]             AfterglowPerSyasei = Config.Bind("1-Player", "AfterglowPerSyasei", 2,
            //                 new ConfigDescription("索取 / 榨取模式中【每射精一次】累积多少次余韵。"
            //                     + "余韵只在索取/榨取模式中累积 —— 模式外的射精不计。",
            //                     new AcceptableValueRange<int>(0, 20)))
            AfterglowPerSyasei3 = BindP3Int("AfterglowPerSyasei", (int)(2f), "索取 / 榨取模式中【每射精一次】累积多少次余韵。", 0, 20);
// [已迁移到按姿势三份]             AfterglowSettleDelay = Config.Bind("1-Player", "AfterglowSettleDelay", 0.6f,
            //                 new ConfigDescription("最后一段结束到发动余韵之间，等待局面稳定的秒数。"
            //                     + "设它是为了让最后一段的连榨、吸精、动画收尾都跑完再发动。",
            //                     new AcceptableValueRange<float>(0f, 5f)))
            AfterglowSettleDelay3 = BindP3("AfterglowSettleDelay", 0.6f, "最后一段结束到发动余韵之间，等待局面稳定的秒数。", 0f, 5f);

            DemandAfterglowMin = Config.Bind("1-Player", "DemandAfterglowMin", 8,
                new ConfigDescription("索取模式结束后「余韵」的连续吸精次数下限。",
                    new AcceptableValueRange<int>(0, 100)));
            DemandAfterglowMax = Config.Bind("1-Player", "DemandAfterglowMax", 20,
                new ConfigDescription("「余韵」的连续吸精次数上限。", new AcceptableValueRange<int>(0, 100)));
            TremorAdaptFactor = Config.Bind("1-Player", "TremorAdaptFactor", 35f,
                new ConfigDescription("适应期内的累积倍率（%）。35 = 只吃到 35% 的绝顶值。"
                    + "100 = 不削减。", new AcceptableValueRange<float>(0f, 100f)));
            // ---- 「绝顶适应」的生命侧效果（都只在适应期内生效，结束严格还原）----
            TremorHpMaxBonus = Config.Bind("1-Player", "TremorHpMaxBonus", 20f,
                new ConfigDescription("适应期内生命【上限】提升（占当前上限的 %）。结束立即还原。0 = 关闭。",
                    new AcceptableValueRange<float>(0f, 200f)));
            TremorHpGuard = Config.Bind("1-Player", "TremorHpGuard", 35f,
                new ConfigDescription("适应期内受到的伤害倍率（%）。35 = 只吃 35% 的伤害。100 = 不减免。",
                    new AcceptableValueRange<float>(0f, 100f)));
            TremorHpRegen = Config.Bind("1-Player", "TremorHpRegen", 6f,
                new ConfigDescription("适应期内每秒回复（占上限的 %）。只在未满血时生效，不会超过上限。0 = 关闭。",
                    new AcceptableValueRange<float>(0f, 100f)));
            MaxBurgerNum = Config.Bind("1-Player", "MaxBurgerNum", 10,
                new ConfigDescription("连吃汉堡上限，原版 10。", new AcceptableValueRange<int>(0, 200)));
            SyaseiCount = Config.Bind("1-Player", "SyaseiCount", 0,
                new ConfigDescription("射精次数。每天的具材数 = Base具材数 - 2 + 该值，改完回标题重进当天生效。",
                    new AcceptableValueRange<int>(0, 100)));

            PowerUnlock = Config.Bind("2-Girl", "PowerUnlock", false,
                "把 tabemiPower / baseAtt / TabemiAtt 按倍率拉高，每次动作造成的伤害与绝顶值大幅提升。");
            TabemiPowerMul = Config.Bind("2-Girl", "TabemiPowerMul", 10f,
                new ConfigDescription("配合 PowerUnlock 使用。", new AcceptableValueRange<float>(1f, 1000f)));
            FellaSpeedPlus = Config.Bind("2-Girl", "FellaSpeedPlus", 0f,
                new ConfigDescription("口交速度加成，原版 0，越大越快进入高速阶段。",
                    new AcceptableValueRange<float>(0f, 1f)));
            SitSpeedPlus = Config.Bind("2-Girl", "SitSpeedPlus", 0f,
                new ConfigDescription("骑乘速度加成，原版 0。", new AcceptableValueRange<float>(0f, 1f)));

            FrozenClock = Config.Bind("3-Flow", "FrozenClock", false,
                "停止白天倒计时，可以无限待在店里。");
            ClockFullTime = Config.Bind("3-Flow", "ClockFullTime", 180,
                new ConfigDescription("一天的总时长（秒），原版 180。", new AcceptableValueRange<int>(60, 3600)));
            GameSpeed = Config.Bind("3-Flow", "GameSpeed", 1f,
                new ConfigDescription("1 = 原速（时钟也按此速度走）。", new AcceptableValueRange<float>(0.5f, 5f)));
            // ---- 汉堡制作倒计时 ----
            // 游戏里 MenuControl 每帧 timeLeft -= Time.deltaTime，归零就 TimeOut。
            // 每单时长是 timeAll（下单时 timeLeft = timeAll）。
            BurgerTimeAll = Config.Bind("2-Burger", "BurgerTimeAll", 0f,
                new ConfigDescription("每单制作时长（秒）。0 = 用游戏原版（10 秒）。",
                    new AcceptableValueRange<float>(0f, 600f)));
            BurgerTimeSpeed = Config.Bind("2-Burger", "BurgerTimeSpeed", 100f,
                new ConfigDescription("倒计时速度（%）。100 = 原速；50 = 慢一半；0 = 冻结（不倒计时）。",
                    new AcceptableValueRange<float>(0f, 1000f)));
            UnlockAllDays = Config.Bind("3-Flow", "UnlockAllDays", false,
                "把已通关天数写满，标题画面可选全部日期。写入 PlayerPrefs，重开仍生效。");

            // 【必须在所有配置绑定之后】迁移要读「三份副本的当前值」，
            // 绑定没做完就迁移的话，副本还不存在 —— 会漏掉一部分参数。
            MigrateConfigIfNeeded();

            // 【游戏绑定校验】确认每个补丁目标都真的在它该在的类上。
            // 挂错类在 Harmony 里是静默失败 —— 这里把它变成启动日志里的一条 WARN。
            try { GameBindings.VerifyAll(FindType, Log.LogWarning, Log.LogInfo); }
            catch (Exception e) { Log.LogWarning("[绑定] 校验失败：" + e.Message); }

            Log.LogInfo("数值修改器已加载 — F9 开关面板");

            // 变化率 + 部件覆盖：都靠 Harmony 前缀/后缀补丁
            try
            {
                // 命令接口：让游戏可由 pwsh / bash / cmd 直接驱动
                _pluginInstance = this;
                GameCommandServer.StartServer();

                _harmony = new Harmony(HarmonyId);
                PatchRates();
                PatchTremor();
                PatchKyuseiRate();
                PatchDemand();

            // 记下"我读到的是哪一版配置"，写之前用它判断有没有被训练器改过
            try { CfgGuard.NoteRead(Config.ConfigFilePath); } catch { }
                PatchPartOverride();
                PatchDlcCheck();
                PatchActivityEvents();
                PatchDialogSkip();
            }
            catch (Exception e)
            {
                Log.LogError("Harmony 初始化失败：" + e);
            }
        }

        // =================================================================
        // 修正游戏自身的 DLC 检测 bug
        // =================================================================

        /// <summary>
        /// 游戏原版的 DLC 检测有个 bug（`TitleDLCCheck.InitiateDLC()`）：
        ///
        ///     path = Application.dataPath + "/..//Patches/";     // ← 指向 游戏目录\Patches        ///     if (AssetBundle.LoadFromFile(Path.Combine(path, "Four Nights at the Burger Shop DLC")) == null)
        ///     {
        ///         GameManager.DLC = false;
        ///         PlayerPrefs.SetInt("DLC", 0);                  // ← 把 DLC 永久关掉
        ///     }
        ///
        /// 而 DLC 文件实际在**游戏根目录**（`DLCManager.LoadAssetBundle()` 用的就是根目录，所以那边是对的）。
        /// `Patches\` 这个目录根本不存在 → 加载必然返回 null → 一旦这段跑到，就把 DLC 标记写成 0，
        /// 游戏随后回退到基础图集（那张没有衣服），表现为"选了裤袜却是光腿"。
        ///
        /// 这里做的是：在游戏方法跑完之后，**从正确路径**重新检测一次并纠正标记。
        /// 不改游戏文件，不阻止原生逻辑（只是事后纠正它的错误结论）。
        /// </summary>
        private static void Postfix_TitleDLCCheck(object __instance)
        {
            try
            {
                string gameRoot = System.IO.Directory.GetParent(Application.dataPath).FullName;
                string dlcFile = System.IO.Path.Combine(gameRoot, "Four Nights at the Burger Shop dlc");

                bool exists = System.IO.File.Exists(dlcFile);
                if (Log != null)
                    Log.LogInfo("DLC 自检（修正 Patches/ 路径 bug）：文件 " + dlcFile + " → "
                                + (exists ? "存在" : "不存在"));

                if (!exists)
                {
                    // 真没有 DLC，尊重游戏原判，不硬开
                    return;
                }

                if (PlayerPrefs.GetInt("DLC", 0) != 1)
                {
                    PlayerPrefs.SetInt("DLC", 1);
                    PlayerPrefs.Save();
                    if (Log != null) Log.LogInfo("  已把 PlayerPrefs 的 DLC 标记从 0 纠正为 1");
                }

                Type gm = FindType("GameManager");
                FieldInfo f = gm != null ? FieldQuiet(gm, "DLC") : null;
                if (f != null)
                {
                    object old = f.GetValue(null);
                    f.SetValue(null, true);
                    if (Log != null) Log.LogInfo("  GameManager.DLC：" + old + " → true");
                }

                // 顺带把资产包也载上（游戏在店内场景也有兜底加载，这里只是提前）
                Type dm = FindType("DLCManager");
                if (dm != null)
                {
                    FieldInfo bundleField = FieldQuiet(dm, "dlcAssetBundle");
                    if (bundleField != null && bundleField.GetValue(null) == null)
                    {
                        UnityEngine.Object[] dms = Resources.FindObjectsOfTypeAll(dm);
                        if (dms != null && dms.Length > 0)
                        {
                            MethodInfo load = dm.GetMethod("LoadAssetBundle", AllFlags);
                            if (load != null) load.Invoke(dms[0], null);
                            if (Log != null) Log.LogInfo("  已调用 DLCManager.LoadAssetBundle()");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (Log != null) Log.LogWarning("DLC 自检失败：" + e.Message);
            }
        }

        /// <summary>
        /// 给 Live2D_ModelControl.UpdatePartOpacity 挂**后缀**补丁。
        ///
        /// 为什么必须是后缀：游戏在 **LateUpdate** 里调它，用 `_Op_*` 字段覆盖所有部件透明度。
        /// 而 LateUpdate 永远晚于 Update —— 所以在 Update 里写透明度会被立刻盖掉（这一点我踩过）。
        /// 挂在它之后才能确保覆盖生效。
        /// </summary>
        /// <summary>把 DLC 检测修正挂到 TitleDLCCheck 上（它自己那套方法体全是空的，所以挂在 Start 即可）。</summary>
        private static void PatchDlcCheck()
        {
            try
            {
                Type t = FindType("TitleDLCCheck");
                if (t == null) { Log.LogWarning("找不到 TitleDLCCheck，DLC 修正未挂载（不影响其他功能）"); return; }

                MethodInfo post = typeof(Plugin).GetMethod("Postfix_TitleDLCCheck", AllFlags);
                MethodInfo target = t.GetMethod("Start", AllFlags) ?? t.GetMethod("Awake", AllFlags);
                if (target == null) { Log.LogWarning("TitleDLCCheck 没有 Start/Awake"); return; }

                _harmony.Patch(target, null, new HarmonyMethod(post), null);
                Log.LogInfo("DLC 路径修正补丁已挂载（" + t.Name + "." + target.Name + " 后缀）");

                // 更可靠的一条：DLCManager.LoadAssetBundle() 是游戏**实际会执行**的加载路径
                // （TitleDLCCheck 在标题画面，可能根本不跑）。
                // 挂在它后面：包加载成功就把 DLC 标记确保为开启。
                Type dm = FindType("DLCManager");
                if (dm != null)
                {
                    MethodInfo lb = dm.GetMethod("LoadAssetBundle", AllFlags);
                    MethodInfo post2 = typeof(Plugin).GetMethod("Postfix_LoadAssetBundle", AllFlags);
                    if (lb != null && post2 != null)
                    {
                        _harmony.Patch(lb, null, new HarmonyMethod(post2), null);
                        Log.LogInfo("DLC 加载修正补丁已挂载（DLCManager.LoadAssetBundle 后缀）");
                    }
                }
            }
            catch (Exception e) { Log.LogWarning("挂 DLC 修正补丁失败：" + e.Message); }
        }

        /// <summary>
        /// DLCManager.LoadAssetBundle() 跑完之后：若资产包确实加载成功，确保 DLC 标记为开启。
        /// 这样即使 `TitleDLCCheck` 那条带 bug 的路径（找不存在的 Patches\ 目录）把标记写成了 0，
        /// 也会在真正使用的这条路径上被纠正回来。
        /// </summary>
        private static void Postfix_LoadAssetBundle()
        {
            try
            {
                Type dm = FindType("DLCManager");
                FieldInfo bf = dm != null ? FieldQuiet(dm, "dlcAssetBundle") : null;
                object bundle = bf != null ? bf.GetValue(null) : null;
                if (bundle == null)
                {
                    if (Log != null) Log.LogInfo("DLC 加载后检查：资产包为空（无 DLC 或加载失败），保持原判");
                    return;
                }

                int cur = PlayerPrefs.GetInt("DLC", -1);
                if (cur != 1)
                {
                    PlayerPrefs.SetInt("DLC", 1);
                    PlayerPrefs.Save();
                    if (Log != null) Log.LogInfo("DLC 加载后检查：包已加载，DLC 标记 " + cur + " → 1");
                }
                else
                {
                    if (Log != null) Log.LogInfo("DLC 加载后检查：包已加载，DLC 标记已是 1");
                }
            }
            catch (Exception e)
            {
                if (Log != null) Log.LogWarning("DLC 加载后检查失败：" + e.Message);
            }
        }

        private static void PatchPartOverride()
        {
            try
            {
                Type mc = FindType("Live2D_ModelControl");
                if (mc == null) { Log.LogWarning("找不到 Live2D_ModelControl，部件覆盖不可用"); return; }
                MethodInfo target = mc.GetMethod("UpdatePartOpacity", AllFlags);
                if (target == null) { Log.LogWarning("找不到 UpdatePartOpacity"); return; }
                MethodInfo post = typeof(Plugin).GetMethod("Postfix_UpdatePartOpacity", AllFlags);
                _harmony.Patch(target, null, new HarmonyMethod(post), null);
                Log.LogInfo("部件覆盖补丁已挂载（UpdatePartOpacity 后缀）");
            }
            catch (Exception e)
            {
                Log.LogWarning("挂部件覆盖补丁失败：" + e.Message);
            }
        }

        /// <summary>
        /// 游戏写完透明度之后：
        ///   (2) 「部件」页勾选的强制显示：在**游戏本帧算出的值**之上叠加
        ///
        /// 关键点：不勾选的部件**一律写回游戏本帧的值**（`natural`）。
        /// 之前的做法是「只把强制项设为 1、其余不动」——那样一旦取消强制，
        /// 部件就停在上一次被设为 1 的状态，**回不到游戏原本的隐藏状态**（能开不能关）。
        /// 现在无论开还是关，每个部件都显式回到「游戏本帧想让它是什么样」。
        /// </summary>
        private static void Postfix_UpdatePartOpacity(object __instance)
        {
            try
            {
                if (__instance == null) return;
                Type t = __instance.GetType();

                bool overrideOn = _partOverrideEnabled;

                Type camType = FindType("Live2D.Cubism.Core.CubismModel");
                if (camType == null) return;

                string sig = overrideOn
                    ? (_forceAllParts ? "ALL" : string.Join(",", new List<string>(_partForceOn).ToArray()))
                    : "OFF";
                bool logThis = sig != _lastForceSig;
                if (logThis) _lastForceSig = sig;

                int shown = 0, restored = 0;

                // ===== 修正游戏自身的索引 bug =====
                //
                // 游戏原意：
                //     modelA.Parts[IDs.CenterGirlSitting_Tights].Opacity = _Op_CenterGirlSitting_Tights;
                //
                // 但 CheckParts() 里那个 `parent` 层级下**没有**名叫 CenterGirlSitting_Tights 的子物体，
                // 字段保持默认值 0；而 Parts[0] 是 LeftGirl ——
                // 于是游戏每帧把【左边女孩】的透明度写成 0，
                // 而真正的基础裤袜层（Parts[19]，Id = "Tights"）**从未被点亮过**，
                // 表现为"选了裤袜却看不到"。
                //
                // 这里按游戏的本意补上：选了裤袜（非 0）就把基础层点亮，否则熄灭。
                // 注意基础层自己名下没有 drawable —— 它是父部件，靠透明度向下传递生效。
                {
                    float tightsSlot = 0f;
                    object tab0 = Tabemi();
                    if (tab0 != null) tightsSlot = GetFloat(tab0, "Tights");

                    foreach (UnityEngine.Object mo in Resources.FindObjectsOfTypeAll(camType))
                    {
                        Component comp = mo as Component;
                        if (comp == null) continue;
                        UnityEngine.Object[] ps = null;
                        try { ps = (UnityEngine.Object[])camType.GetProperty("Parts", AllFlags).GetValue(comp, null); }
                        catch { }
                        if (ps == null) continue;

                        foreach (UnityEngine.Object po in ps)
                        {
                            Component pc = po as Component;
                            if (pc == null) continue;
                            string id = null;
                            try { id = pc.GetType().GetProperty("Id", AllFlags)?.GetValue(pc, null) as string ?? pc.name; }
                            catch { }
                            if (id != "Tights") continue;      // 只处理基础裤袜层

                            FieldInfo fi = FieldQuiet(pc.GetType(), "Opacity");
                            if (fi == null) continue;
                            float want = (tightsSlot != 0f) ? 1f : 0f;
                            object cur = fi.GetValue(pc);
                            float curf = cur is float cf ? cf : -1f;
                            if (Mathf.Abs(curf - want) > 0.001f)
                            {
                                fi.SetValue(pc, want);
                                if (want > curf) shown++; else restored++;
                            }
                        }
                    }
                }

                foreach (UnityEngine.Object mo in Resources.FindObjectsOfTypeAll(camType))
                {
                    Component comp = mo as Component;
                    if (comp == null) continue;
                    UnityEngine.Object[] parts = null;
                    try { parts = (UnityEngine.Object[])camType.GetProperty("Parts", AllFlags).GetValue(comp, null); }
                    catch { }
                    if (parts == null) continue;

                    foreach (UnityEngine.Object po in parts)
                    {
                        Component pc = po as Component;
                        if (pc == null) continue;
                        try
                        {
                            string nm = pc.GetType().GetProperty("Id", AllFlags)?.GetValue(pc, null) as string ?? pc.name;
                            if (nm.StartsWith("HitArea")) continue;
                            FieldInfo fi = FieldQuiet(pc.GetType(), "Opacity");
                            if (fi == null) continue;

                            // 游戏本帧算出的值（拦截之前读，就是它写的值）
                            object curv = fi.GetValue(pc);
                            float natural = curv is float cf ? cf : 0f;

                            float want = natural;
                            if (overrideOn)
                            {
                                if (_forceAllParts || _partForceOn.Contains(nm)) want = 1f;
                                // 注意：**不要**在这里强行点亮 "Tights" 基础层。
                                // 基础图集里这层是空的（衣服只在 DLC 图集里），
                                // 强行点亮只会让它在基础图集下露出"裸腿"，造成"选了裤袜却是光腿"的假象。
                                // DLC 生效时游戏自己会用 DLC 图集渲染正确的丝袜，无需插件干预。
                            }

                            if (Mathf.Abs(want - natural) > 0.001f)
                            {
                                fi.SetValue(pc, want);
                                if (want > natural) shown++; else restored++;
                            }
                        }
                        catch { }
                    }
                }

                if (logThis && Log != null)
                    Log.LogInfo("部件覆盖 " + sig + "：显示 " + shown + " 个 / 还原 " + restored + " 个");
            }
            catch { }
        }

        private void Update()
        {
            // 热键：只留面板、快照、自检。取证用的导出功能已按需求移除。
            if (Input.GetKeyDown(KeyCode.F9)) _showPanel = !_showPanel;
            if (Input.GetKeyDown(KeyCode.F5)) CaptureSnapshot();
            if (Input.GetKeyDown(KeyCode.F7)) SelfTestAll();
            if (Input.GetKeyDown(KeyCode.F8)) SelfTestCostume();

            FlushPendingOps();
            TickTightsSampling();
            TickTightsTest();

            // 自动连续记录：每 ~0.5 秒落一份 texhit，用来抓"贴图绑定随时间变化"的过程
            // （手动导出只能看到某一刻，而这类问题的关键在于"什么时候变的"）
            if (_autoExport)
            {
                _autoExportTimer++;
                if (_autoExportTimer >= 30)
                {
                    _autoExportTimer = 0;
                    DumpBindings();     // 轻量：只记打包绑定，不做像素分析
                }
            }

            // 一次性打印运行时贴图状态：用于判断"改了资产 m_IsReadable 为何运行时仍是不可读"
            if (!_texProbeDone && Time.frameCount > 120)
            {
                _texProbeDone = true;
                ProbeRuntimeTextures();
            }

            // 导出"游戏部件索引 vs 实际部件"的对照表（排查换装设了值却看不到的原因）。
            // 重试直到成功：CubismModel 只在店内场景存在，标题画面里会失败。
            if (!_partIndexProbeDone && Time.frameCount > 300)
            {
                if (DumpPartIndexMap()) { _partIndexProbeDone = true; DumpPartTable(); DumpMasks(); }
            }

            // F4：诊断导出（排查部件/贴图问题时最常用，给个专用热键）
            if (Input.GetKeyDown(KeyCode.F4)) DumpDiagnostics();

            // Ctrl+R：启停活动记录（功能键大多被系统吞掉，用组合键更可靠）
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
            {
                if (ActivityLogger.Running) ActivityLogger.StopLogging(); else ActivityLogger.StartLogging();
            }

            if (UnlockAllDays.Value && !_appliedUnlock)
            {
                _appliedUnlock = true;
                WriteClearedDays();
            }
            else if (!UnlockAllDays.Value)
            {
                _appliedUnlock = false;
            }

            ApplyPersistent();
            if (_gaugeBaseline.Count == 0) RememberGaugeBaseline();   // 尽早建立条尺寸基准
            TickTremor();
            TickAdaptBuff();
            TickXWatch();
            TickDemand();
            TickZeroHold();
            TickSpankSpeed();
            TickOsiriReturn();
            TickSitKiss();
            TickKissSpeed();
            TickEcstasyDropWatch();
            TickManmanDrag();
            TickAfterglowSettle();
            EnforceGaugeBaseline();

            // 【必须每帧】ApplySlow 里写着 Time.timeScale。
            // 它原来只在 1 秒定时器里跑一次 —— 动摇只有 1.6 秒，那一秒一次的机会多半踩空，
            // 表现就是"动摇期间角色没有变慢"，结束后速度也最长 1 秒才回来。
            // 里面都是普通字段写入，每帧跑没有代价。
            ApplySlow();
            _slowTimer += Time.unscaledDeltaTime;
            if (_slowTimer >= 1f) _slowTimer = 0f;
        }

        // ---------------------------------------------------------------
        // 反射定位（缓存）
        // ---------------------------------------------------------------
        private static readonly Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();
        private static readonly Dictionary<string, FieldInfo> _fieldCache = new Dictionary<string, FieldInfo>();
        internal const BindingFlags AllFlags = BindingFlags.Instance | BindingFlags.Static
                                            | BindingFlags.Public | BindingFlags.NonPublic;

        internal static Type FindType(string name)
        {
            Type t;
            if (_typeCache.TryGetValue(name, out t)) return t;
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { t = a.GetType(name, false); }
                catch { t = null; }
                if (t != null) break;
            }
            _typeCache[name] = t;
            return t;
        }

        internal static FieldInfo Field(Type t, string name)
        {
            if (t == null) return null;
            string key = t.FullName + "." + name;
            FieldInfo fi;
            if (_fieldCache.TryGetValue(key, out fi)) return fi;
            fi = t.GetField(name, AllFlags);
            _fieldCache[key] = fi;
            if (fi == null && Log != null) Log.LogWarning("找不到字段 " + key);
            return fi;
        }

        /// <summary>查字段但**不打警告**。用于「字段没有就退回属性」这类正常回退路径。</summary>
        internal static FieldInfo FieldQuiet(Type t, string name)
        {
            if (t == null) return null;
            string key = t.FullName + "." + name;
            FieldInfo fi;
            if (_fieldCache.TryGetValue(key, out fi)) return fi;
            fi = t.GetField(name, AllFlags);
            _fieldCache[key] = fi;
            return fi;
        }

        private static UnityEngine.Object _hubObj;

        /// <summary>
        /// 找场景里的进度托管者。Chapter1 / Chapter2 都继承 ProgressHub，
        /// 而 Unity 的 FindObjectOfType(基类) 不匹配派生类，所以只当兜底用。
        /// </summary>
        private static object HubInstance()
        {
            if (_hubObj != null) return _hubObj;

            Type hubType = FindType("ProgressHub");
            if (hubType == null) return null;

            UnityEngine.Object[] found =
                UnityEngine.Object.FindObjectsOfType(hubType);
            if (found != null && found.Length > 0) _hubObj = found[0];

            return _hubObj;
        }

        internal static object Player()
        {
            object ani = StaticPart("ani");
            if (ani != null)
            {
                object p = Field(ani.GetType(), "player")?.GetValue(ani);
                if (p != null) return p;
            }

            object hub = HubInstance();
            if (hub != null)
            {
                object p = Field(hub.GetType(), "player")?.GetValue(hub);
                if (p != null) return p;
            }
            return null;
        }

        internal static object Tabemi()    { return StaticPart("tabemi"); }
        private static object Clock()      { return StaticPart("clock"); }
        private static object Anim()       { return StaticPart("ani"); }
        private static object AnimSit()    { return StaticPart("aniSitOsiri"); }

        /// <summary>读 ProgressHub 上的静态引用（场景初始化时赋值）。</summary>
        private static object StaticPart(string field)
        {
            Type hubType = FindType("ProgressHub");
            if (hubType == null) return null;
            FieldInfo fi = Field(hubType, field);
            if (fi == null || !fi.IsStatic) return null;
            return fi.GetValue(null);
        }

        /// <summary>
        /// 每帧应用：这部分一定要每帧跑。
        /// 关键点：不许直接写 currentHP / CurrentEcstasy 字段——那会绕过游戏自己的
        /// OnHPChanged / OnEcstasyChanged 事件，血条与绝顶槽不刷新。
        /// 正确做法是「算出差值再调 HPChange / EcstasyChange」，让游戏自己写值并发事件。
        /// </summary>
        private void ApplyPersistent()
        {
            TrackSyaseing();

            object player = Player();
            if (player != null)
            {
                float maxHp = GetFloat(player, "maxHP");
                float curHp = GetFloat(player, "currentHP");

                if (GodMode.Value && maxHp > 0f && curHp < maxHp)
                {
                    // 用差值走 HPChange，界面才会跟着动
                    InvokeFloat(player, "HPChange", maxHp - curHp);
                }

                if (NoEcstasy.Value)
                {
                    float curEc = GetFloat(player, "CurrentEcstasy");
                    if (curEc > 0.01f) InvokeFloat(player, "EcstasyChange", -curEc);
                }

                if (EcstasyResist.Value > 0f)
                    SetField(player, "ecstasyResist", Mathf.Clamp01(EcstasyResist.Value));

                // 上限对策：锁基线 + 朝目标平滑逼近
                if (LockMaxHp.Value || LockMaxEcstasy.Value) KeepMaxCaps(player);
                PumpCaps(player);

                // 目标上限生效时把当前值填上，否则血条看着是空的
                if (TargetHp.Value > 0f)
                {
                    float mh = GetFloat(player, "maxHP");
                    float ch = GetFloat(player, "currentHP");
                    if (ch < mh - 0.01f) InvokeFloat(player, "HPChange", mh - ch);
                }
                // 【已移除】原来这里有一段"目标上限生效时把绝顶值填到 99%"。
                //
                // 那是错的，而且正是"绝顶值要么一下到顶、要么完全不动"的根源：
                //
                //   1) 「一下到顶」——TargetEcstasy > 0 时每帧把 CurrentEcstasy 顶到 99%，
                //      玩家自己的操作完全被抹掉，看上去就是瞬间满格。
                //   2) 「完全不动」——EcstasyChange 里增量要乘 (1 - EcstasyResist)：
                //          CurrentEcstasy += amount * (1f - EcstasyResist);
                //      一旦「绝顶抗性」拉到 1，乘法归零，什么增量都进不去，值就钉死不动。
                //      再叠加「无绝顶」（每帧归零），更是完全静止。
                //
                // TargetEcstasy 的本意只是**设定上限值**（见它的配置说明），
                // 上限由 PumpCaps() 负责，这里不该去动当前值。
                //
                // 另注：绝顶值确实不能填到满 —— Update_Syasei() 一旦判定
                //   CurrentEcstasy >= maxEcstasy 就会置 Syaseing = true，
                // 而 EcstasyChange 在 Syaseing 为真时整段忽略正值增量，
                // 于是值不再变化、角色动作也会停住。所以正确做法是**完全不填**，
                // 而不是"填到 99%"。
            }

            if (FrozenClock.Value)
            {
                object clock = Clock();
                if (clock != null) SetField(clock, "isClockRunning", false);
            }

            // 注意：这里**不做**任何 Syaseing 干预。
            // 曾经的「卡死看门狗」是错的：射精瞬间 EcstasyReset() 会把值清零，
            // 而 Syaseing 在整个射精动画期间保持 true —— 也就是「Syaseing=true 且值为 0」
            // 是**完全正常**的状态。用「值远低于上限」去判定卡死，会每 3 秒打断一次正在播的
            // 射精动画，表现为反复重播、一直闪白光。Syaseing 归游戏的状态机管，插件不碰。
        }
        /// <summary>把 maxHP / maxEcstasy 压回基线值（首次见到时记住的原始上限）。</summary>
        private static float _baseMaxHp = -1f;
        private static float _baseMaxEcstasy = -1f;

        // 「只在滑块变动时写一次」用的记忆值（-1 = 还没应用过，进场景后会补写一次；
        // 之后不再持续覆盖，游戏自己的递增才不会被按死）
        // 全量自检会把 currentHP / CurrentEcstasy 改成测试值，这里记原值供收尾还原
        private static float _selfTestKeepHp = float.NaN;
        private static float _selfTestKeepEc = float.NaN;

        private static float _lastSyasei = -1f;
        private static int _lastMaxBurger = -1;

        private void KeepMaxCaps(object player)
        {
            float maxHp = GetFloat(player, "maxHP");
            float maxEc = GetFloat(player, "maxEcstasy");

            // 基线：取见过的最大值（避免把已经被削过的值当成基线）
            // 但**绝不能**让基线跟到「目标上限」之上——否则锁与目标互相顶，
            // 表现为「上限一直往上走」。基线最多取到 max(目标, 天花板) 里较小的那个。
            float ceilHp = TargetHp.Value > 0f ? TargetHp.Value : CapCeiling;
            float ceilEc = TargetEcstasy.Value > 0f ? TargetEcstasy.Value : CapCeiling;
            if (ceilHp > CapCeiling) ceilHp = CapCeiling;
            if (ceilEc > CapCeiling) ceilEc = CapCeiling;

            if (_baseMaxHp < 0f || maxHp > _baseMaxHp) _baseMaxHp = Mathf.Min(maxHp, ceilHp);
            if (_baseMaxEcstasy < 0f || maxEc > _baseMaxEcstasy) _baseMaxEcstasy = Mathf.Min(maxEc, ceilEc);

            // 这里**不能**用游戏的百分比接口来补：
            // 它是 maxEc += maxEc * percent/100（乘法），浮点上补不回精确值，
            // 而本方法每帧都跑 → 会累积漂移；又因为基线取最大值，漂移会被当成新基线，
            // 结果就是上限单调上升、无上限。所以这里一律「精确写回基线」。
            if (LockMaxHp.Value && maxHp < _baseMaxHp - 0.01f)
            {
                SetField(player, "maxHP", _baseMaxHp);
                float cur = GetFloat(player, "currentHP");
                if (cur > _baseMaxHp) SetField(player, "currentHP", EaseToward(cur, _baseMaxHp, _baseMaxHp));
                InvokeFloat(player, "HPGaugeChangeValue", 0f, true);   // 只用来触发 UI 刷新
            }
            if (LockMaxEcstasy.Value && maxEc < _baseMaxEcstasy - 0.01f)
            {
                SetField(player, "maxEcstasy", _baseMaxEcstasy);
                float cur = GetFloat(player, "CurrentEcstasy");
                if (cur > _baseMaxEcstasy) SetField(player, "CurrentEcstasy", EaseToward(cur, _baseMaxEcstasy, _baseMaxEcstasy));
                InvokeFloat(player, "EcstasyGaugeChangePercent", 0f, true);
            }
        }

        /// <summary>调用一个带单个 float 参数的方法（走游戏自己的逻辑，含事件通知）。</summary>
        private static void InvokeFloat(object obj, string name, float value)
        {
            InvokeFloat(obj, name, value, false);
        }

        // -----------------------------------------------------------------
        // 上限对策
        //
        // 游戏机制（Assembly-CSharp 里那两处）：
        //   被榨取：HPGaugeChangePercent(-Random(3~5), 跟当前值)     → maxHP 逐次 -3%~-5%
        //           EcstasyGaugeChangePercent(-Random(1~5), 不跟)    → maxEcstasy 逐次 -1%~-5%
        //   吃汉堡：num2 = currentHP + maxHP×回復量%
        //           if (DLC && num2 > maxHP) HPGaugeChangePercent((num2-maxHP)/maxHP*100*0.5, 跟当前值)
        //   ⇒ 只要「当前值溢出上限」，就能把上限顶上去 —— 这就是可主动提升的杠杆。
        //
        // 所以这里的做法是：先把 currentHP 垫到 (目标 + 溢出量)，再按百分比调接口，
        // 让它内部那条 (current - max)/max 算出我们想要的增幅，不做任何硬写字段。
        // -----------------------------------------------------------------
        // -----------------------------------------------------------------
        // 变化率：Harmony 前缀补丁，在方法进入前缩放 amount 参数
        // 装在 PlayerControl 上（private 方法也打得进去）。
        // 缺失的方法不会导致失败 —— 补丁是逐个 try/catch 挂的。
        // -----------------------------------------------------------------
        internal const string HarmonyId = "nesarf.burgershop.modder.rates";

        /// <summary>列出补丁目标的当前状态（是否已被本插件替换过）。用于面板上快速自查。</summary>
        private static string VerifyPatches()
        {
            var sb = new System.Text.StringBuilder();
            Type pc = FindType("PlayerControl");
            if (pc == null) return "找不到 PlayerControl";
            if (_harmony == null) return "Harmony 未初始化";

            string[] names = { "HPChange", "HPGaugeChangeValue", "EcstasyChange", "EcstasyReset", "EcstasyGaugeChangePercent" };
            int targeted = 0;
            foreach (string n in names)
            {
                bool hit = false;
                try
                {
                    MethodInfo[] ms = n == "EcstasyReset"
                        ? new[] { pc.GetMethod(n, AllFlags, null, new[] { typeof(float) }, null) }
                        : pc.GetMethods(AllFlags);

                    foreach (MethodInfo m in ms)
                    {
                        if (m == null || m.Name != n) continue;
                        Patches info = Harmony.GetPatchInfo(m);
                        if (info == null || info.Prefixes == null) continue;
                        foreach (Patch p in info.Prefixes)
                        {
                            if (p.owner == HarmonyId) { hit = true; break; }
                        }
                        if (hit) break;
                    }
                }
                catch { }
                if (hit) targeted++;
                sb.Append(hit ? "(自然) " : "× ").Append(n).Append("  ");
            }
            string s = targeted + "/" + names.Length + " 已挂：" + sb;
            Log.LogInfo("补丁检查 → " + s);
            return s;
        }

        /// <summary>
        /// 按需诊断导出：把每个 Live2D 部件（含 Part 及其透明度）与它的图集矩形写成 TSV。
        /// 专门给「某件衣服看不到」这类问题用 —— 能看出是透明度没开、还是图集里那块本来就没内容。
        /// </summary>
        private void DumpDiagnostics()
        {
            try
            {
                // 每次导出落到**带时间戳的子目录**，避免多次导出互相覆盖
                // （之前固定文件名会盖掉上一次，排查时反而分不清哪份是哪次）
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string dir = System.IO.Path.Combine(Paths.GameRootPath, "BepInEx", "l2d_dump", "diag_" + stamp);
                System.IO.Directory.CreateDirectory(dir);
                _diagDir = dir;

                Type camType = FindType("Live2D.Cubism.Core.CubismModel");
                Type rendType = FindType("Live2D.Cubism.Rendering.CubismRenderer");
                if (camType == null) { Log.LogWarning("诊断：找不到 CubismModel"); return; }

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("kind\tname\topacity\ttexName\tatlasX0\tatlasY0\tatlasX1\tatlasY1\ttexW\ttexH");

                // 逐顶点 UV（包围盒会误导：mesh 的 UV 只占包围盒的一小块）
                var vb = new System.Text.StringBuilder();
                vb.AppendLine("name\ttexName\tvi\tu\tv\tpx\tpy\topacity");

                // drawable → 所属 part（UnmanagedParentIndex 对应 Parts 数组下标）
                var db = new System.Text.StringBuilder();
                db.AppendLine("drawable\tparentIndex\tparentName\ttexName\tverts");

                // 游戏内直接采样贴图：每个 drawable 的顶点落在不透明像素上的比例
                var hitb = new System.Text.StringBuilder();
                _exportBatch = DateTime.Now.ToString("HHmmss");
                hitb.AppendLine("drawable\ttexName\ttexId\tw\th\tfmt\tisReadable\trawBytes\tforced\thit\ttotal\thitPct\tnote\tbatch");
                _pixelCache.Clear();
                _pixelNote.Clear();
                _pixelDims.Clear();
                _cpuFail.Clear();

                foreach (UnityEngine.Object mo in Resources.FindObjectsOfTypeAll(camType))
                {
                    Component comp = mo as Component;
                    if (comp == null) continue;

                    // 1) Part 级：名字 + 当前透明度（衣服看不到多半是这里为 0）
                    UnityEngine.Object[] parts = null;
                    try { parts = (UnityEngine.Object[])camType.GetProperty("Parts", AllFlags).GetValue(comp, null); }
                    catch { }
                    if (parts != null)
                    {
                        for (int pi = 0; pi < parts.Length; pi++)
                        {
                            Component pc = parts[pi] as Component;
                            if (pc == null) continue;
                            string nm = "";
                            float op = -1f;
                            try
                            {
                                object idv = pc.GetType().GetProperty("Id", AllFlags)?.GetValue(pc, null);
                                nm = idv as string ?? pc.name;
                                // Opacity 是**公开字段**（不是属性）——用 GetProperty 会拿到 -1
                                object opv = FieldQuiet(pc.GetType(), "Opacity")?.GetValue(pc);
                                if (opv is float opf) op = opf;
                            }
                            catch { }
                            sb.AppendLine(string.Join("\t", new string[]
                            {
                                "part", nm, op.ToString("F3"), pi.ToString(), "", "", "", "", "", ""
                            }));
                        }
                    }

                    // 2) Drawable 级：名字 + 图集矩形（用来看那块图有没有内容）
                    UnityEngine.Object[] arr = null;
                    try { arr = (UnityEngine.Object[])camType.GetProperty("Drawables", AllFlags).GetValue(comp, null); }
                    catch { }
                    if (arr == null) continue;

                    foreach (UnityEngine.Object dobj in arr)
                    {
                        Component dc = dobj as Component;
                        if (dc == null) continue;
                        Type dt = dc.GetType();
                        string id = "";
                        Vector2[] uvs = null;
                        try
                        {
                            id = dt.GetProperty("Id", AllFlags)?.GetValue(dc, null) as string ?? "";
                            uvs = dt.GetProperty("VertexUvs", AllFlags)?.GetValue(dc, null) as Vector2[];
                        }
                        catch { }
                        if (uvs == null || uvs.Length == 0) continue;

                        float u0 = float.MaxValue, v0 = float.MaxValue, u1 = float.MinValue, v1 = float.MinValue;
                        foreach (Vector2 uv in uvs)
                        {
                            if (uv.x < u0) u0 = uv.x;
                            if (uv.y < v0) v0 = uv.y;
                            if (uv.x > u1) u1 = uv.x;
                            if (uv.y > v1) v1 = uv.y;
                        }

                        string texName = ""; int tw = 0, th = 0;
                        Texture2D texRef = null;
                        try
                        {
                            Component rend = rendType != null ? dc.GetComponent(rendType) : null;
                            if (rend != null)
                            {
                                Texture t = rendType.GetProperty("MainTexture", AllFlags)?.GetValue(rend, null) as Texture;
                                if (t != null) { texName = t.name; tw = t.width; th = t.height; texRef = t as Texture2D; }
                            }
                        }
                        catch { }

                        sb.AppendLine(string.Join("\t", new string[]
                        {
                            "drawable", id, "", texName,
                            tw > 0 ? ((int)(u0*tw)).ToString() : "",
                            th > 0 ? ((int)(v0*th)).ToString() : "",
                            tw > 0 ? ((int)(u1*tw)).ToString() : "",
                            th > 0 ? ((int)(v1*th)).ToString() : "",
                            tw.ToString(), th.ToString()
                        }));

                        // 这个 drawable 属于哪个 part（用 UnmanagedParentIndex 对应 Parts 下标）
                        try
                        {
                            object pidxObj = dt.GetProperty("UnmanagedParentIndex", AllFlags)?.GetValue(dc, null);
                            int pidx = pidxObj is int pi2 ? pi2 : -1;
                            string pname = "";
                            if (parts != null && pidx >= 0 && pidx < parts.Length)
                            {
                                Component ppc = parts[pidx] as Component;
                                if (ppc != null)
                                    pname = ppc.GetType().GetProperty("Id", AllFlags)?.GetValue(ppc, null) as string ?? ppc.name;
                            }
                            db.AppendLine(string.Join("\t", new string[]
                            {
                                id, pidx.ToString(), pname, texName, uvs.Length.ToString()
                            }));
                        }
                        catch { }

                        // 每个顶点的 u/v 与像素坐标（离线按点采样，判断这块图在 mesh 处到底有没有内容）
                        if (tw > 0 && th > 0)
                        {
                            for (int vi = 0; vi < uvs.Length; vi++)
                            {
                                vb.AppendLine(string.Join("\t", new string[]
                                {
                                    id, texName, vi.ToString(),
                                    uvs[vi].x.ToString("F5"), uvs[vi].y.ToString("F5"),
                                    ((int)(uvs[vi].x * tw)).ToString(),
                                    ((int)(uvs[vi].y * th)).ToString(),
                                    ""
                                }));
                            }
                        }

                        // 在游戏内直接采样它真正在用的那张贴图 —— 排除任何离线导出/换算的不确定性。
                        // 用 GetPixels32 拉一次整张（比逐顶点 GetPixel 快几个数量级，后者在 8192² 上几乎必失败）。
                        if (tw > 0 && th > 0)
                        {
                            try
                            {
                                if (texRef == null)
                                {
                                    hitb.AppendLine(id + "\t" + texName + "\t-\t-\t-\t" + GetOpacityOf(dc) + "\ttexRef=null");
                                }
                                else
                                {
                                    Color32[] buf;
                                    if (!_pixelCache.TryGetValue(texName, out buf) || buf == null)
                                    {
                                        // 取像素的优先级：
                                        //   (1) 纯 CPU 路径（GetRawTextureData + 自己解 DXT5）—— 数据最精确
                                        //   (2) GPU 回读 —— CPU 路径不可用时的兜底
                                        // 注意 `GetPixels32()` 对压缩格式（DXT5/fmt 12）会抛 ArgumentException，
                                        // 所以不能拿它当"可读就能用"的判据。
                                        buf = null;
                                        string readNote = "";
                                        // 旁路探针：在**真正取像素这一刻**记录贴图状态。
                                        // 启动早期的探针与这里可能不同（贴图可能随后才被替换/上传）。
                                        int rawBytes = -1;
                                        string rawErr = "";
                                        try { rawBytes = texRef.GetRawTextureData<byte>().Length; }
                                        catch (Exception e) { rawErr = e.GetType().Name; }
                                        _probeInfo = string.Join("\t", new string[] {
                                            texRef.GetInstanceID().ToString(),
                                            texRef.width.ToString(), texRef.height.ToString(),
                                            texRef.format.ToString(), texRef.isReadable.ToString(),
                                            rawBytes.ToString() + (rawErr == "" ? "" : "(" + rawErr + ")"),
                                            (_partForceOn.Contains(id) || _forceAllParts) ? "Y" : "N"
                                        });
                                        readNote = "isReadable=" + texRef.isReadable
                                                 + ",raw=" + rawBytes + (rawErr == "" ? "" : "(" + rawErr + ")");
                                        if (texRef.isReadable)
                                        {
                                            buf = CpuReadDxt5(texRef, out readNote);
                                            if (buf != null) _lastReadDims = new[] { tw, th };
                                        }
                                        else
                                        {
                                            readNote = "notReadable";
                                        }
                                        if (buf == null)
                                        {
                                            // CPU 直接读不到时，先试**从 .resS 读原始 DXT5**
                                            // —— 流式贴图的真正 CPU 路径（数据就在磁盘上，位置由资产的 StreamData 给出）
                                            long[] _ri;
                                            if (ResInfoById.TryGetValue(texRef.GetInstanceID(), out _ri))
                                            {
                                                long off = _ri[0];
                                                long sz = _ri[1];
                                                string resNote;
                                                buf = CpuReadFromResS(tw, th, off, sz, out resNote);
                                                if (buf != null)
                                                {
                                                    _lastReadDims = new[] { tw, th };
                                                    readNote = resNote;
                                                }
                                                else readNote = readNote + "," + resNote;
                                            }
                                        }
                                        if (buf == null)
                                        {
                                            // 最后兜底：GPU 回读（不可读 / 数据未上传时都能用）
                                            buf = GpuReadTexture(texRef);
                                            readNote = readNote + ",gpu-readback" + _lastReadScale;
                                        }
                                        _pixelNote[texName] = readNote;
                                        _pixelCache[texName] = buf;
                                        _pixelDims[texName] = _lastReadDims;
                                    }
                                    int hit = 0, tot = 0;
                                    // 采样必须按**回读后的实际尺寸**换算（降采样时尺寸变小）
                                    int bw = tw, bh = th;
                                    int[] dims;
                                    if (_pixelDims.TryGetValue(texName, out dims) && dims != null && dims.Length == 2)
                                    {
                                        bw = dims[0]; bh = dims[1];
                                    }
                                    foreach (Vector2 uv in uvs)
                                    {
                                        int sx = Mathf.Clamp((int)(uv.x * bw), 0, bw - 1);
                                        int sy = Mathf.Clamp((int)(uv.y * bh), 0, bh - 1);
                                        int idx = sy * bw + sx;
                                        tot++;
                                        if (idx >= 0 && idx < buf.Length && buf[idx].a > 16) hit++;
                                    }
                                    string note;
                                    if (!_pixelNote.TryGetValue(texName, out note)) note = "";
                                    hitb.AppendLine(string.Join("\t", new string[]
                                    {
                                        id, texName, _probeInfo, hit.ToString(), tot.ToString(),
                                        (tot > 0 ? (100f * hit / tot).ToString("F1") : "0"), note, _exportBatch
                                    }));
                                }
                            }
                            catch (Exception ex)
                            {
                                hitb.AppendLine(id + "\t" + texName + "\t-\t-\t-\t" + GetOpacityOf(dc) + "\t" + ex.GetType().Name);
                            }
                        }
                    }
                }

                string f = System.IO.Path.Combine(dir, "diagnose.tsv");
                System.IO.File.WriteAllText(f, sb.ToString(), new System.Text.UTF8Encoding(false));
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "uvex.tsv"),
                    vb.ToString(), new System.Text.UTF8Encoding(false));
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "drawpart.tsv"),
                    db.ToString(), new System.Text.UTF8Encoding(false));
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "texhit.tsv"),
                    hitb.ToString(), new System.Text.UTF8Encoding(false));

                // 操作流水：把"人为操作"和"操作后游戏给的反馈"一起导出
                var opsb = new System.Text.StringBuilder();
                opsb.AppendLine("序号  时间       操作                   详情 / 游戏反馈");
                foreach (string line in _opLog) opsb.AppendLine(line);
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "ops.txt"),
                    opsb.ToString(), new System.Text.UTF8Encoding(false));
                // 附一份本次运行的环境说明，便于日后对照
                try
                {
                    var meta = new System.Text.StringBuilder();
                    meta.AppendLine("时间     " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    meta.AppendLine("场景     " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                    meta.AppendLine("分辨率   " + Screen.width + "x" + Screen.height);
                    meta.AppendLine("总开关   " + (_partOverrideEnabled ? "开" : "关"));
                    meta.AppendLine("强制列表 " + (_partOverrideEnabled
                        ? string.Join(",", new List<string>(_partForceOn).ToArray())
                        : "(关)"));
                    System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "meta.txt"),
                        meta.ToString(), new System.Text.UTF8Encoding(false));
                }
                catch { }

                Log.LogInfo("诊断已导出到 " + dir + "（diagnose.tsv / uvex.tsv / drawpart.tsv / texhit.tsv / meta.txt）");
            }
            catch (Exception e)
            {
                Log.LogError("诊断导出失败：" + e);
            }
        }

        /// <summary>读一个部件的当前透明度（Opacity 是公开字段）。</summary>
        private static string GetOpacityOf(Component part)
        {
            try
            {
                object v = FieldQuiet(part.GetType(), "Opacity")?.GetValue(part);
                if (v is float f) return f.ToString("F2");
            }
            catch { }
            return "?";
        }

        /// <summary>
        /// 把**游戏正在用的**贴图导出成 PNG（走 GPU 回读，绕开 isReadable=false）。
        ///
        /// 这条路的独特价值：它导出的是**运行时**的贴图内容 ——
        /// 包括启动后发生的贴图替换（例如 DLC 换装把 MainTexture 换成另一张）、
        /// 以及任何只在显存里成立的状态。离线从资产包里解出来的那张**不包含这些**，
        /// 这正是此前"两份图对不上"的原因。
        /// </summary>
        private void ExportRuntimeTextures()
        {
            try
            {
                string dir = DiagStampDir("runtime_tex");
                System.IO.Directory.CreateDirectory(dir);

                Type rendType = FindType("Live2D.Cubism.Rendering.CubismRenderer");
                Type camType = FindType("Live2D.Cubism.Core.CubismModel");
                if (camType == null) { Log.LogWarning("导出运行时贴图：找不到 CubismModel"); return; }

                var seen = new HashSet<string>();
                int ok = 0;
                foreach (UnityEngine.Object mo in Resources.FindObjectsOfTypeAll(camType))
                {
                    Component comp = mo as Component;
                    if (comp == null) continue;
                    UnityEngine.Object[] arr = null;
                    try { arr = (UnityEngine.Object[])camType.GetProperty("Drawables", AllFlags).GetValue(comp, null); }
                    catch { }
                    if (arr == null) continue;

                    foreach (UnityEngine.Object dobj in arr)
                    {
                        Component dc = dobj as Component;
                        if (dc == null) continue;
                        Texture2D t = null;
                        try
                        {
                            Component rend = rendType != null ? dc.GetComponent(rendType) : null;
                            if (rend != null)
                                t = rendType.GetProperty("MainTexture", AllFlags)?.GetValue(rend, null) as Texture2D;
                        }
                        catch { }
                        if (t == null) continue;

                        string key = t.name + "_" + t.GetInstanceID();
                        if (seen.Contains(key)) continue;
                        seen.Add(key);

                        string file = System.IO.Path.Combine(dir, t.name + "_" + t.GetInstanceID() + ".png");
                        try
                        {
                            Log.LogInfo("导出运行时贴图 " + t.name + " (" + t.width + "x" + t.height
                                        + ", isReadable=" + t.isReadable + ") …（大图会卡一会儿）");

                            Color32[] buf = t.isReadable ? t.GetPixels32() : GpuReadTexture(t);
                            int W = t.width, H = t.height;

                            // GPU 回读得到的是**自下而上**的行序，PNG 需要自上而下 → 垂直翻转
                            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                            var flip = new Color32[W * H];
                            for (int y = 0; y < H; y++)
                                Array.Copy(buf, y * W, flip, (H - 1 - y) * W, W);
                            tex.SetPixels32(flip);
                            tex.Apply(false);
                            byte[] png = tex.EncodeToPNG();
                            UnityEngine.Object.Destroy(tex);

                            System.IO.File.WriteAllBytes(file, png);
                            ok++;
                            Log.LogInfo("  已写出 " + file + "（" + (png.Length / 1024) + " KB）");
                        }
                        catch (Exception ex)
                        {
                            Log.LogWarning("  导出 " + t.name + " 失败：" + ex.Message);
                        }
                    }
                }
                Log.LogInfo("运行时贴图导出完成，共 " + ok + " 张 → " + dir);
            }
            catch (Exception e)
            {
                Log.LogError("导出运行时贴图失败：" + e);
            }
        }

        // -----------------------------------------------------------------
        // 纯 CPU 路径：原始压缩数据 → 自己解码
        // -----------------------------------------------------------------

        /// <summary>
        /// 用 **CPU 路径**取压缩贴图的像素：
        /// `GetRawTextureData<byte>()` 拿到的是**未经转换的原始数据**（这里 = DXT5 压缩块），
        /// 再自己做 DXT5 解码得到 RGBA。
        ///
        /// 相比 GPU 回读的好处：**不做任何 GPU 采样/滤波/格式转换**，
        /// 拿到的是磁盘里那份精确原始数据 —— 做图集改写补丁时必须用这条。
        ///
        /// 前提：贴图 `isReadable == true`（否则 GetRawTextureData 也会抛）。
        /// 返回 null 表示不可用，调用方应退回 GPU 回读。
        /// </summary>
        private static Color32[] CpuReadDxt5(Texture2D tex, out string note)
        {
            note = "";
            try
            {
                if (!tex.isReadable) { note = "notReadable"; return null; }
                if (tex.format != TextureFormat.DXT5)
                {
                    note = "cpu(" + tex.format + ")";
                    return tex.GetPixels32();
                }

                byte[] raw = tex.GetRawTextureData<byte>().ToArray();
                int W = tex.width, H = tex.height;
                int expect = ((W + 3) / 4) * ((H + 3) / 4) * 16;
                if (raw.Length < expect)
                {
                    // raw 为空通常**不是**永久失败，而是这张贴图此刻还没上传到 CPU 侧
                    // （游戏刚把 MainTexture 换过去）。标成"稍后重试"，下次导出通常就能拿到。
                    note = raw.Length == 0
                        ? "rawEmpty(稍后重试)"
                        : "rawTooSmall(" + raw.Length + "<" + expect + ")";
                    return null;
                }

                var outPix = new Color32[W * H];
                int bw = (W + 3) / 4, bh = (H + 3) / 4;
                var pal = new Color32[4];

                for (int by = 0; by < bh; by++)
                {
                    for (int bx = 0; bx < bw; bx++)
                    {
                        int o = (by * bw + bx) * 16;

                        // 前 8 字节：alpha 块
                        int a0 = raw[o], a1 = raw[o + 1];
                        ulong abits = 0;
                        for (int i = 0; i < 6; i++) abits |= (ulong)raw[o + 2 + i] << (8 * i);
                        byte[] alphas = BuildDxt5AlphaTable(a0, a1);

                        // 后 8 字节：颜色块（RGB565 两端点 + 16 个 2bit 索引）
                        int c0 = raw[o + 8] | (raw[o + 9] << 8);
                        int c1 = raw[o + 10] | (raw[o + 11] << 8);
                        uint cbits = (uint)(raw[o + 12] | (raw[o + 13] << 8)
                                          | (raw[o + 14] << 16) | (raw[o + 15] << 24));

                        pal[0] = Rgb565(c0);
                        pal[1] = Rgb565(c1);
                        if (c0 > c1)
                        {
                            pal[2] = Lerp(pal[0], pal[1], 2, 3, 1);
                            pal[3] = Lerp(pal[0], pal[1], 1, 3, 2);
                        }
                        else
                        {
                            pal[2] = Lerp(pal[0], pal[1], 1, 2, 1);
                            pal[3] = new Color32(0, 0, 0, 255);
                        }

                        for (int py = 0; py < 4; py++)
                        {
                            for (int px = 0; px < 4; px++)
                            {
                                int x = bx * 4 + px;
                                int gy = by * 4 + py;        // 数据里的行（自下而上）
                                if (x >= W || gy >= H) continue;
                                int y = H - 1 - gy;          // 翻成自上而下，与 Unity 的读取约定一致
                                if (y < 0 || y >= H) continue;

                                int bit = py * 4 + px;
                                int ci = (int)((cbits >> (2 * bit)) & 3);
                                int ai = (int)((abits >> (3 * bit)) & 7);

                                Color32 c = pal[ci];
                                c.a = alphas[ai];
                                outPix[y * W + x] = c;
                            }
                        }
                    }
                }

                note = "cpu-dxt5";
                return outPix;
            }
            catch (Exception e)
            {
                note = "cpuFail:" + e.GetType().Name;
                return null;
            }
        }

        /// <summary>
        /// 直接从 `.resS` 流文件读原始 DXT5 数据并解码 —— **完全不依赖 Unity 的贴图 API**。
        ///
        /// 为什么需要这条路：8192² 那张是**流式贴图**，像素数据在 `sharedassets0.assets.resS` 里，
        /// 运行时只驻留 GPU —— 即使资产里 `m_IsReadable=True`，`isReadable` 依然是 False，
        /// `GetRawTextureData()` 会抛异常。改标志位对它无效（已实测）。
        ///
        /// 但数据**就在磁盘上**，而且位置是明确记录的：
        ///   StreamData.path='sharedassets0.assets.resS'  offset=25916640  size=67108864
        /// 所以直接按偏移读出来自己解 DXT5 即可 —— 这才是真正的 CPU 路径。
        /// </summary>
        private static Color32[] CpuReadFromResS(int W, int H, long offset, long size, out string note)
        {
            note = "";
            try
            {
                string resS = System.IO.Path.Combine(Paths.GameRootPath,
                    "Four Nights at the Burger Shop_Data", "sharedassets0.assets.resS");
                if (!System.IO.File.Exists(resS)) { note = "resS不存在"; return null; }

                int expect = ((W + 3) / 4) * ((H + 3) / 4) * 16;
                if (size < expect) { note = "resS尺寸不符(" + size + "<" + expect + ")"; return null; }

                var raw = new byte[expect];
                using (var fs = new System.IO.FileStream(resS, System.IO.FileMode.Open,
                                                          System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
                {
                    fs.Seek(offset, System.IO.SeekOrigin.Begin);
                    int got = 0;
                    while (got < expect)
                    {
                        int n = fs.Read(raw, got, expect - got);
                        if (n <= 0) break;
                        got += n;
                    }
                    if (got < expect) { note = "resS读取不足(" + got + ")"; return null; }
                }

                Color32[] px = DecodeDxt5(raw, W, H);
                note = "cpu-ress";
                return px;
            }
            catch (Exception e)
            {
                note = "resS失败:" + e.GetType().Name;
                return null;
            }
        }

        /// <summary>纯函数版 DXT5 解码（与 CpuReadDxt5 里那段算法一致，独立出来供 resS 路径复用）。</summary>
        private static Color32[] DecodeDxt5(byte[] raw, int W, int H)
        {
            var outPix = new Color32[W * H];
            int bw = (W + 3) / 4, bh = (H + 3) / 4;
            var pal = new Color32[4];
            for (int by = 0; by < bh; by++)
            {
                for (int bx = 0; bx < bw; bx++)
                {
                    int o = (by * bw + bx) * 16;
                    if (o + 16 > raw.Length) break;
                    int a0 = raw[o], a1 = raw[o + 1];
                    ulong abits = 0;
                    for (int i = 0; i < 6; i++) abits |= (ulong)raw[o + 2 + i] << (8 * i);
                    byte[] alphas = BuildDxt5AlphaTable(a0, a1);

                    int c0 = raw[o + 8] | (raw[o + 9] << 8);
                    int c1 = raw[o + 10] | (raw[o + 11] << 8);
                    uint cbits = (uint)(raw[o + 12] | (raw[o + 13] << 8)
                                      | (raw[o + 14] << 16) | (raw[o + 15] << 24));

                    pal[0] = Rgb565(c0);
                    pal[1] = Rgb565(c1);
                    if (c0 > c1)
                    {
                        pal[2] = Lerp(pal[0], pal[1], 2, 3, 1);
                        pal[3] = Lerp(pal[0], pal[1], 1, 3, 2);
                    }
                    else
                    {
                        pal[2] = Lerp(pal[0], pal[1], 1, 2, 1);
                        pal[3] = new Color32(0, 0, 0, 255);
                    }

                    for (int py = 0; py < 4; py++)
                    {
                        for (int px = 0; px < 4; px++)
                        {
                            int x = bx * 4 + px;
                            int gy = by * 4 + py;
                            if (x >= W || gy >= H) continue;
                            int y = H - 1 - gy;      // 行序：数据自下而上
                            if (y < 0 || y >= H) continue;
                            int bit = py * 4 + px;
                            int ci = (int)((cbits >> (2 * bit)) & 3);
                            int ai = (int)((abits >> (3 * bit)) & 7);
                            Color32 c = pal[ci];
                            c.a = alphas[ai];
                            outPix[y * W + x] = c;
                        }
                    }
                }
            }
            return outPix;
        }

        /// <summary>DXT5 的 8 级 alpha 表。</summary>
        private static byte[] BuildDxt5AlphaTable(int a0, int a1)
        {
            var t = new byte[8];
            t[0] = (byte)a0;
            t[1] = (byte)a1;
            if (a0 > a1)
            {
                for (int i = 1; i <= 6; i++)
                    t[i + 1] = (byte)(((7 - i) * a0 + i * a1) / 7);
            }
            else
            {
                for (int i = 1; i <= 4; i++)
                    t[i + 1] = (byte)(((5 - i) * a0 + i * a1) / 5);
                t[6] = 0;
                t[7] = 255;
            }
            return t;
        }

        private static Color32 Rgb565(int c)
        {
            int r = (c >> 11) & 0x1F;
            int g = (c >> 5) & 0x3F;
            int b = c & 0x1F;
            return new Color32(
                (byte)((r << 3) | (r >> 2)),
                (byte)((g << 2) | (g >> 4)),
                (byte)((b << 3) | (b >> 2)),
                255);
        }

        /// <summary>按分量权重插值两个颜色（DXT5 颜色调色板的中间两档）。</summary>
        private static Color32 Lerp(Color32 a, Color32 b, int wa, int da, int wb)
        {
            return new Color32(
                (byte)((a.r * wa + b.r * wb) / da),
                (byte)((a.g * wa + b.g * wb) / da),
                (byte)((a.b * wa + b.b * wb) / da),
                255);
        }

        /// <summary>
        /// 打印运行时 Live2D 图集的真实状态：isReadable / 格式 / 原始数据字节数。
        /// 这是判断"资产里 m_IsReadable 改成 true 了，为什么运行时还说不可读"的唯一可靠依据。
        /// </summary>
        private static void ProbeRuntimeTextures()
        {
            try
            {
                Type rendType = FindType("Live2D.Cubism.Rendering.CubismRenderer");
                Type camType = FindType("Live2D.Cubism.Core.CubismModel");
                if (camType == null || rendType == null) { Log.LogWarning("贴图探针：找不到 Cubism 类型"); return; }

                var seen = new HashSet<int>();
                foreach (UnityEngine.Object mo in Resources.FindObjectsOfTypeAll(camType))
                {
                    Component comp = mo as Component;
                    if (comp == null) continue;
                    UnityEngine.Object[] arr = null;
                    try { arr = (UnityEngine.Object[])camType.GetProperty("Drawables", AllFlags).GetValue(comp, null); }
                    catch { }
                    if (arr == null) continue;

                    foreach (UnityEngine.Object dobj in arr)
                    {
                        Component dc = dobj as Component;
                        if (dc == null) continue;
                        Texture2D t = null;
                        try
                        {
                            Component rend = dc.GetComponent(rendType);
                            if (rend != null)
                                t = rendType.GetProperty("MainTexture", AllFlags)?.GetValue(rend, null) as Texture2D;
                        }
                        catch { }
                        if (t == null || seen.Contains(t.GetInstanceID())) continue;
                        seen.Add(t.GetInstanceID());

                        int rawLen = -1;
                        string err = "";
                        try { rawLen = t.GetRawTextureData<byte>().Length; }
                        catch (Exception e) { err = e.GetType().Name + ":" + e.Message; }

                        Log.LogInfo(string.Format(
                            "贴图探针：{0} {1}x{2} fmt={3} isReadable={4} rawBytes={5} {6}",
                            t.name, t.width, t.height, t.format, t.isReadable, rawLen, err));
                    }
                }
            }
            catch (Exception e) { Log.LogWarning("贴图探针异常：" + e.Message); }
        }

        // -----------------------------------------------------------------
        // 操作流水：记录"谁做了什么"以及"之后游戏是什么状态"
        // -----------------------------------------------------------------
        private static readonly List<string> _opLog = new List<string>();
        private static float _opStartTime;
        private static bool _opStartSet;

        private static int _opSeq;

        /// <summary>
        /// 记一条操作流水。
        /// 把「人为操作」和「操作之后游戏给出的状态」配成一对，
        /// 这样排查时序类问题（比如"换装卡住"）时能看到完整过程，而不是只有一个终态快照。
        /// </summary>
        private static void LogOp(string action, string detail)
        {
            if (!_opStartSet) { _opStartTime = Time.realtimeSinceStartup; _opStartSet = true; }
            _opSeq++;
            float t = Time.realtimeSinceStartup - _opStartTime;
            string scene = "";
            try { scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name; } catch { }
            _opLog.Add(string.Format("{0:000}  [{1,7:0.0}s]  {2,-22} {3}",
                _opSeq, t, action, detail + (scene != "" ? "  @ " + scene : "")));
            if (_opLog.Count > 2000) _opLog.RemoveAt(0);
            if (Log != null) Log.LogInfo("[操作] " + action + " | " + detail);
        }

        /// <summary>快照当前关键游戏状态，供流水里作为"游戏给出的反馈"。</summary>
        private static string GameStateProbe()
        {
            var sb = new System.Text.StringBuilder();
            try
            {
                object tab = Tabemi();
                if (tab != null)
                {
                    sb.Append("帽子=").Append(GetFloat(tab, "Cap").ToString("0"))
                      .Append(" 上衣=").Append(GetFloat(tab, "Upper").ToString("0"))
                      .Append(" 下装=").Append(GetFloat(tab, "Lower").ToString("0"))
                      .Append(" 裤袜=").Append(GetFloat(tab, "Tights").ToString("0"))
                      .Append(" 眼镜=").Append(GetFloat(tab, "Glasses").ToString("0"));
                    sb.Append(" | 模型透明度: Tights=");
                    object mdl = FieldQuiet(tab.GetType(), "model")?.GetValue(tab);
                    if (mdl != null)
                    {
                        sb.Append(GetFloat(mdl, "_Op_CenterGirlSitting_Tights").ToString("0.#"))
                          .Append(" Black=").Append(GetFloat(mdl, "_Op_CenterGirlSitting_Tights_Black").ToString("0.#"))
                          .Append(" White=").Append(GetFloat(mdl, "_Op_CenterGirlSitting_Tights_White").ToString("0.#"));
                    }
                }
                object pl = Player();
                if (pl != null)
                {
                    sb.Append(" | HP=").Append(GetFloat(pl, "currentHP").ToString("0"))
                      .Append("/").Append(GetFloat(pl, "maxHP").ToString("0"))
                      .Append(" 绝顶=").Append(GetFloat(pl, "CurrentEcstasy").ToString("0"))
                      .Append("/").Append(GetFloat(pl, "maxEcstasy").ToString("0"))
                      .Append(" 射精=").Append(GetFloat(pl, "syaseiCount").ToString("0"));
                }
            }
            catch (Exception e) { sb.Append("探针异常:").Append(e.GetType().Name); }
            return sb.ToString();
        }

        // 延迟采样的待办队列：操作发生后**等游戏跑完 LateUpdate** 再读状态，
        // 否则读到的是调用瞬间的旧值（_Op_* 由游戏在 LateUpdate 里写，
        // 跟着 ApplyCostume 同步读会拿到上一帧的值 —— 这正是我先前误判"换装没生效"的原因）。
        private static readonly List<string> _pendingOps = new List<string>();
        private static int _pendingFrames;

        /// <summary>"操作 + 反馈"成对记录：动作立即记，反馈延后 2 帧采。</summary>
        private static void LogOpWithState(string action, string detail)
        {
            LogOp(action, detail);
            _pendingOps.Add(action);
            _pendingFrames = 2;
        }

        /// <summary>每帧调用：把延后到期的操作补上"游戏状态"那一行。</summary>
        private static void FlushPendingOps()
        {
            if (_pendingOps.Count == 0) return;
            if (_pendingFrames > 0) { _pendingFrames--; return; }
            foreach (string a in _pendingOps)
                LogOp("   ↳ 游戏状态", "[" + a + "] " + GameStateProbe());
            _pendingOps.Clear();
        }

        /// <summary>
        /// 轻量导出：只记录每个 drawable 的**贴图绑定**（名字 / 实例ID / 尺寸 / 格式 / 可读性 / 是否被强制），
        /// 不做任何像素采样。用于"连续记录"模式 —— 抓"绑定随时间怎么变"的过程。
        ///
        /// 与 DumpDiagnostics 的区别：那个是完整快照（含像素分析，慢）；这个是高频时间线（快）。
        /// </summary>
        private void DumpBindings()
        {
            try
            {
                string dir = _autoDir != "" ? _autoDir : (_autoDir = DiagStampDir("auto"));
                System.IO.Directory.CreateDirectory(dir);

                Type rendType = FindType("Live2D.Cubism.Rendering.CubismRenderer");
                Type camType = FindType("Live2D.Cubism.Core.CubismModel");
                if (camType == null) return;

                string stamp = DateTime.Now.ToString("HHmmss_fff");
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("time	drawable	texName	texId	w	h	fmt	isReadable	forced");

                foreach (UnityEngine.Object mo in Resources.FindObjectsOfTypeAll(camType))
                {
                    Component comp = mo as Component;
                    if (comp == null) continue;
                    UnityEngine.Object[] arr = null;
                    try { arr = (UnityEngine.Object[])camType.GetProperty("Drawables", AllFlags).GetValue(comp, null); }
                    catch { }
                    if (arr == null) continue;
                    foreach (UnityEngine.Object dobj in arr)
                    {
                        Component dc = dobj as Component;
                        if (dc == null) continue;
                        Texture2D t = null;
                        try
                        {
                            Component rend = rendType != null ? dc.GetComponent(rendType) : null;
                            if (rend != null)
                                t = rendType.GetProperty("MainTexture", AllFlags)?.GetValue(rend, null) as Texture2D;
                        }
                        catch { }
                        if (t == null) continue;
                        sb.AppendLine(string.Join("	", new string[]
                        {
                            stamp, dc.name, t.name, t.GetInstanceID().ToString(),
                            t.width.ToString(), t.height.ToString(), t.format.ToString(),
                            t.isReadable ? "Y" : "N",
                            (_partForceOn.Contains(dc.name) || _forceAllParts) ? "Y" : "N"
                        }));
                    }
                }
                // 累积写同一个文件（每次带时间戳行），便于看时间线
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "bindings.tsv"), sb.ToString(), new System.Text.UTF8Encoding(false));
            }
            catch { }
        }

        /// <summary>
        /// 导出每个 drawable 的**顶点坐标（模型空间）与 UV**。
        ///
        /// 用途：查明"丝袜 mesh"与"身体/腿 mesh"的顶点是否一一对应 ——
        /// 若对应，就能把腿的图集内容按 UV 映射搬进丝袜预留区，从而真正画出一件丝袜。
        /// </summary>
        private void DumpVertexMap()
        {
            try
            {
                string dir = DiagStampDir("vmap");
                System.IO.Directory.CreateDirectory(dir);

                Type camType = FindType("Live2D.Cubism.Core.CubismModel");
                if (camType == null) return;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("drawable	parent	vi	x	y	u	v");

                Type dt = FindType("Live2D.Cubism.Core.CubismDrawable");
                foreach (UnityEngine.Object mo in Resources.FindObjectsOfTypeAll(camType))
                {
                    Component comp = mo as Component;
                    if (comp == null) continue;
                    UnityEngine.Object[] arr = null;
                    try { arr = (UnityEngine.Object[])camType.GetProperty("Drawables", AllFlags).GetValue(comp, null); }
                    catch { }
                    if (arr == null) continue;

                    foreach (UnityEngine.Object dobj in arr)
                    {
                        Component dc = dobj as Component;
                        if (dc == null) continue;
                        try
                        {
                            Vector3[] pos = (Vector3[])dt.GetProperty("VertexPositions", AllFlags).GetValue(dc, null);
                            Vector2[] uv = (Vector2[])dt.GetProperty("VertexUvs", AllFlags).GetValue(dc, null);
                            if (pos == null || uv == null) continue;
                            int n = Mathf.Min(pos.Length, uv.Length);

                            string pname = "";
                            object pidxObj = dt.GetProperty("UnmanagedParentIndex", AllFlags)?.GetValue(dc, null);
                            int pidx = pidxObj is int pi ? pi : -1;
                            UnityEngine.Object[] parts = null;
                            try { parts = (UnityEngine.Object[])camType.GetProperty("Parts", AllFlags).GetValue(comp, null); }
                            catch { }
                            if (parts != null && pidx >= 0 && pidx < parts.Length)
                            {
                                Component ppc = parts[pidx] as Component;
                                if (ppc != null)
                                    pname = ppc.GetType().GetProperty("Id", AllFlags)?.GetValue(ppc, null) as string ?? ppc.name;
                            }

                            for (int i = 0; i < n; i++)
                                sb.AppendLine(string.Join("	", new string[] {
                                    dc.name, pname, i.ToString(),
                                    pos[i].x.ToString("F4"), pos[i].y.ToString("F4"),
                                    uv[i].x.ToString("F6"), uv[i].y.ToString("F6")
                                }));
                        }
                        catch { }
                    }
                }
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "vmap.tsv"),
                    sb.ToString(), new System.Text.UTF8Encoding(false));
                Log.LogInfo("顶点映射已导出：" + System.IO.Path.Combine(dir, "vmap.tsv"));
            }
            catch (Exception e) { Log.LogWarning("导出顶点映射失败：" + e.Message); }
        }

        /// <summary>
        /// 在游戏内测量**真实运行贴图**的内容分布，并给出丝袜 mesh 的 UV 区域占用。
        ///
        /// 为什么要这么做：离线从资产解出来的图集与游戏实际用的那张**对不上**
        /// （离线同一批 UV 全 0%，游戏却渲染得出来）。所以必须以游戏自己的数据为准。
        ///
        /// 不导出大图，只输出一张粗粒度的占用网格 + 丝袜区域的采样结果，避免几百 MB 的写出。
        /// </summary>
        private void ProbeRuntimeAtlas()
        {
            try
            {
                string dir = DiagStampDir("atlasprobe");

                Type rendType = FindType("Live2D.Cubism.Rendering.CubismRenderer");
                Type camType = FindType("Live2D.Cubism.Core.CubismModel");
                Type dt = FindType("Live2D.Cubism.Core.CubismDrawable");
                if (camType == null) return;

                // 收集 drawable → 贴图 / UV
                var texOf = new Dictionary<string, Texture2D>();
                var uvOf = new Dictionary<string, Vector2[]>();
                foreach (UnityEngine.Object mo in Resources.FindObjectsOfTypeAll(camType))
                {
                    Component comp = mo as Component;
                    if (comp == null) continue;
                    UnityEngine.Object[] arr = null;
                    try { arr = (UnityEngine.Object[])camType.GetProperty("Drawables", AllFlags).GetValue(comp, null); }
                    catch { }
                    if (arr == null) continue;
                    foreach (UnityEngine.Object dobj in arr)
                    {
                        Component dc = dobj as Component;
                        if (dc == null) continue;
                        try
                        {
                            Component rend = rendType != null ? dc.GetComponent(rendType) : null;
                            if (rend != null)
                                texOf[dc.name] = rendType.GetProperty("MainTexture", AllFlags)?.GetValue(rend, null) as Texture2D;
                            uvOf[dc.name] = (Vector2[])dt.GetProperty("VertexUvs", AllFlags).GetValue(dc, null);
                        }
                        catch { }
                    }
                }

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("=== 运行时贴图盘点 ===");
                var byTex = new Dictionary<int, List<string>>();
                foreach (var kv in texOf)
                {
                    if (kv.Value == null) continue;
                    int id = kv.Value.GetInstanceID();
                    if (!byTex.ContainsKey(id)) byTex[byTex.Count == 0 ? id : id] = new List<string>();
                    byTex[id].Add(kv.Key);
                }
                foreach (var kv in byTex)
                {
                    var t = texOf[kv.Value[0]];
                    sb.AppendLine(string.Format("  贴图 {0} id={1} {2}x{3} fmt={4} 被 {5} 个 drawable 使用",
                        t.name, kv.Key, t.width, t.height, t.format, kv.Value.Count));

                    // GPU 回读这张贴图，统计内容分布
                    string pnote = "";
                    Color32[] px = ReadTexturePixels(t, out pnote);
                    sb.AppendLine("    取像素路径: " + pnote);
                    if (px == null) { sb.AppendLine("    回读为空"); continue; }

                    int bw = Mathf.Max(1, t.width / 4), bh = Mathf.Max(1, t.height / 4);
                    if (px.Length < bw * bh) { sb.AppendLine("    数据长度异常 " + px.Length); continue; }
                    int opaque = 0;
                    for (int i = 0; i < bw * bh; i++) if (px[i].a > 16) opaque++;
                    sb.AppendLine(string.Format("    内容占比 {0:0.0}%（{1}/{2} 个 4x4 采样格）",
                        100f * opaque / (bw * bh), opaque, bw * bh));

                    // 丝袜 mesh 在这张贴图上的命中
                    sb.AppendLine("    丝袜 mesh 采样：");
                    foreach (string meshName in new string[] { "ArtMesh186","ArtMesh187","ArtMesh188","ArtMesh189","ArtMesh190","ArtMesh191" })
                    {
                        Texture2D mt;
                        if (!texOf.TryGetValue(meshName, out mt) || mt == null || mt.GetInstanceID() != kv.Key) continue;
                        Vector2[] uvs;
                        if (!uvOf.TryGetValue(meshName, out uvs) || uvs == null) continue;
                        int hit = 0;
                        foreach (Vector2 uv in uvs)
                        {
                            int sx = Mathf.Clamp((int)(uv.x * bw), 0, bw - 1);
                            int sy = Mathf.Clamp((int)(uv.y * bh), 0, bh - 1);
                            if (px[sy * bw + sx].a > 16) hit++;
                        }
                        sb.AppendLine(string.Format("      {0}: {1}/{2} = {3:0.0}%", meshName, hit, uvs.Length, 100f * hit / Mathf.Max(1, uvs.Length)));
                    }
                }

                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "runtime_atlas.txt"),
                    sb.ToString(), new System.Text.UTF8Encoding(false));
                Log.LogInfo("运行时图集盘点已写出 runtime_atlas.txt");
                Log.LogInfo(sb.ToString());
            }
            catch (Exception e) { Log.LogWarning("运行时图集盘点失败：" + e.Message); }
        }

        /// <summary>
        /// 诊断：把游戏用的**部件索引**与它实际指向的部件名对照出来。
        ///
        /// 背景：游戏的 UpdatePartOpacity() 走的是
        ///     modelA.Parts[IDs.CenterGirlSitting_Tights_Black].Opacity = ...
        /// 而 `IDs.xxx` 是在 CheckParts() 里用 **子物体顺序** 当索引填进去的
        /// （`CenterGirlSitting_Tights_Black = i`，i 是 parent.GetChild(i) 的 i）。
        /// 但 `modelA.Parts[]` 用的是 **Unity 内部部件顺序**。两者若不一致，
        /// 游戏就会把 opacity 写到**别的部件**上 —— 表现为"换装设了值却看不到变化"，
        /// 而按名字直接设（插件的强制显示）却有效。
        ///
        /// 这个读数就是为了验证这一点。
        /// </summary>
        private bool DumpPartIndexMap()
        {
            try
            {
                string dir = DiagStampDir("partindex");
                var sb = new System.Text.StringBuilder();

                Type camType = FindType("Live2D.Cubism.Core.CubismModel");
                if (camType == null) { ProbeWhy("camType 为空"); return false; }
                if (Resources.FindObjectsOfTypeAll(camType).Length == 0) { ProbeWhy("没有 CubismModel 实例（不在店内场景？）"); return false; }

                // 不靠类型名查 Live2D_IDs（可能带命名空间或被打包剥离），
                // 而是从 Live2D_ModelControl 实例上的 IDs **字段**取到实例，再从实例拿类型。
                Type mcType = FindType("Live2D_ModelControl");
                if (mcType == null) { ProbeWhy("找不到 Live2D_ModelControl 类型"); return false; }
                UnityEngine.Object[] mcs0 = Resources.FindObjectsOfTypeAll(mcType);
                if (mcs0 == null || mcs0.Length == 0) { ProbeWhy("没有 Live2D_ModelControl 实例"); return false; }
                Component mcComp = mcs0[0] as Component;
                if (mcComp == null) { ProbeWhy("mcComp 为空"); return false; }
                FieldInfo idsField = FieldQuiet(mcType, "IDs");
                if (idsField == null) { ProbeWhy("Live2D_ModelControl 上没有 IDs 字段"); return false; }
                object idsInst = idsField.GetValue(mcComp);
                if (idsInst == null) { ProbeWhy("IDs 字段值为 null（组件未初始化？）"); return false; }
                Type idsType = idsInst.GetType();
                Log.LogInfo("部件索引对照：IDs 实例类型 = " + idsType.FullName);

                // 取一个模型实例
                UnityEngine.Object[] models = Resources.FindObjectsOfTypeAll(camType);
                Component model = models != null && models.Length > 0 ? models[0] as Component : null;
                if (model == null) return false;

                UnityEngine.Object[] parts = (UnityEngine.Object[])camType.GetProperty("Parts", AllFlags)
                    .GetValue(model, null);

                sb.AppendLine("=== 部件索引对照（游戏索引 vs Parts 数组实际指向）===");
                sb.AppendLine("模型: " + model.name + "   Parts 数: " + (parts != null ? parts.Length : 0));
                sb.AppendLine();
                sb.AppendLine(string.Format("{0,-34} {1,6}  {2}", "IDs 字段", "值", "Parts[值] 实际指向"));

                string[] names = new string[] {
                    "CenterGirlSitting_Tights", "CenterGirlSitting_Tights_Black", "CenterGirlSitting_Tights_White",
                    "CenterGirlSitting_Cap", "CenterGirlSitting_Shirts", "CenterGirlSitting_Skirt",
                    "CenterGirlSitting_SkirtA", "CenterGirlSitting_SkirtB", "CenterGirlSitting_SkirtC",
                    "CenterGirlSitting_HotPants", "CenterGirlSitting", "BODY"
                };

                foreach (string n in names)
                {
                    FieldInfo fi = idsType.GetField(n, AllFlags);
                    int idx = -1;
                    string actual = "(字段不存在)";
                    if (fi != null)
                    {
                        object v = fi.GetValue(idsInst);
                        idx = v is int iv ? iv : -1;
                        if (parts != null && idx >= 0 && idx < parts.Length)
                        {
                            Component pc = parts[idx] as Component;
                            if (pc != null)
                                actual = pc.GetType().GetProperty("Id", AllFlags)?.GetValue(pc, null) as string ?? pc.name;
                        }
                        else actual = "(越界)";
                    }
                    sb.AppendLine(string.Format("{0,-34} {1,6}  {2}", n, idx, actual));
                }

                // 反向：在 Parts 里按名字找到这些部件的真实下标
                sb.AppendLine();
                sb.AppendLine("=== 按名字反查真实下标（游戏本应使用的值）===");
                foreach (string n in names)
                {
                    int found = -1;
                    if (parts != null)
                    {
                        for (int i = 0; i < parts.Length; i++)
                        {
                            Component pc = parts[i] as Component;
                            if (pc == null) continue;
                            string pid = pc.GetType().GetProperty("Id", AllFlags)?.GetValue(pc, null) as string ?? pc.name;
                            if (pid == n) { found = i; break; }
                        }
                    }
                    sb.AppendLine(string.Format("{0,-34} 真实下标 = {1}", n, found));
                }

                // ============ 核心：子物体顺序 vs Parts 数组顺序 ============
                // 游戏用 CheckParts() 里的 parent.GetChild(i) 的 i 当部件索引。
                // 这里把两边的顺序都列出来，看同一个名字的下标是否一致。
                sb.AppendLine();
                sb.AppendLine("=== 子物体顺序 vs Parts 数组顺序 ===");

                // 找 Live2D_ModelControl 上的 modelParentA，取它的第0个孩子的第1个孩子
                FieldInfo mpaField = FieldQuiet(mcType, "modelParentA");
                UnityEngine.Object mcObj = mcComp;

                if (mpaField != null && mcObj != null)
                {
                    Transform mpa = mpaField.GetValue(mcObj) as Transform;
                    if (mpa != null && mpa.childCount > 0)
                    {
                        Transform parent = mpa.GetChild(0).GetChild(1);
                        sb.AppendLine("parent = modelParentA.GetChild(0).GetChild(1) = " + parent.name
                                      + "  子物体数 = " + parent.childCount);
                        sb.AppendLine();
                        sb.AppendLine(string.Format("{0,4}  {1,-34} {2}", "子序", "子物体名", "同名部件在 Parts 里的下标"));
                        for (int i = 0; i < parent.childCount; i++)
                        {
                            string cn = parent.GetChild(i).name;
                            int real = -1;
                            if (parts != null)
                            {
                                for (int k = 0; k < parts.Length; k++)
                                {
                                    Component pc = parts[k] as Component;
                                    if (pc == null) continue;
                                    string pid = pc.GetType().GetProperty("Id", AllFlags)?.GetValue(pc, null) as string ?? pc.name;
                                    if (pid == cn) { real = k; break; }
                                }
                            }
                            sb.AppendLine(string.Format("{0,4}  {1,-34} {2}", i, cn, real));
                        }
                    }
                    else sb.AppendLine("modelParentA 为空或没有子物体");
                }
                else sb.AppendLine("拿不到 Live2D_ModelControl.modelParentA");

                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "partindex.txt"),
                    sb.ToString(), new System.Text.UTF8Encoding(false));
                Log.LogInfo("部件索引对照已导出：" + dir);
                Log.LogInfo(sb.ToString());
                return true;
            }
            catch (Exception e) { Log.LogWarning("导出部件索引失败：" + e.Message); return false; }
        }

        /// <summary>
        /// 把每个部件的【Id】【GameObject 名】【透明度】【名下的 drawable】全部列出来。
        ///
        /// 为什么需要：先前发现 diagnose.tsv 里的部件名（如 Tights / SkirtA）在 Parts 数组里
        /// 反查不到，说明 `Id` 属性与 GameObject `name` **不是一回事**，而我一直在混用这两个来源。
        /// 这份完整清单可以一次性消除歧义，并回答"到底哪个部件在渲染丝袜"。
        /// </summary>
        private bool DumpPartTable()
        {
            try
            {
                Type camType = FindType("Live2D.Cubism.Core.CubismModel");
                if (camType == null) { ProbeWhy("camType 为空"); return false; }
                UnityEngine.Object[] models = Resources.FindObjectsOfTypeAll(camType);
                if (models == null || models.Length == 0) { ProbeWhy("没有 CubismModel 实例"); return false; }

                var sb = new System.Text.StringBuilder();
                foreach (UnityEngine.Object mo in models)
                {
                    Component model = mo as Component;
                    if (model == null) continue;
                    UnityEngine.Object[] parts = (UnityEngine.Object[])camType.GetProperty("Parts", AllFlags)
                        .GetValue(model, null);
                    if (parts == null) continue;

                    sb.Append("模型: ").Append(model.name).Append("   部件数: ").Append(parts.Length).AppendLine();
                    sb.AppendLine();
                    sb.AppendLine("下标 | Id | GameObject名 | 透明度 | 名下drawable数 | drawable名");

                    for (int i = 0; i < parts.Length; i++)
                    {
                        Component pc = parts[i] as Component;
                        if (pc == null) continue;
                        string pid = "";
                        try { pid = pc.GetType().GetProperty("Id", AllFlags)?.GetValue(pc, null) as string ?? ""; }
                        catch { }
                        float op = 0f;
                        try
                        {
                            object ov = FieldQuiet(pc.GetType(), "Opacity")?.GetValue(pc);
                            if (ov is float of) op = of;
                        }
                        catch { }

                        // 该部件的子 drawable
                        string kids = "";
                        try
                        {
                            Component partComp = pc;
                            Type partT = pc.GetType();
                            MethodInfo gm = partT.GetMethod("GetComponentsInChildren", new Type[] { typeof(Transform) });
                            // 更简单：直接遍历所有 drawable，看谁的 UnmanagedParentIndex == i
                            kids = "";
                        }
                        catch { }

                        sb.Append(i).Append(" | ").Append(pid).Append(" | ").Append(pc.name)
                          .Append(" | ").Append(op.ToString("0.##")).Append(" | ").AppendLine(kids);
                    }

                    // 反查：每个 drawable 属于哪个 part（用 UnmanagedParentIndex）
                    sb.AppendLine();
                    sb.AppendLine("=== drawable -> 部件归属 ===");
                    Type dt = FindType("Live2D.Cubism.Core.CubismDrawable");
                    UnityEngine.Object[] draws = (UnityEngine.Object[])camType.GetProperty("Drawables", AllFlags)
                        .GetValue(model, null);
                    if (dt != null && draws != null)
                    {
                        // 按部件归组
                        int[] cnt = new int[parts.Length];
                        var namesOf = new List<string>[parts.Length];
                        for (int i = 0; i < parts.Length; i++) namesOf[i] = new List<string>();

                        foreach (UnityEngine.Object dobj in draws)
                        {
                            Component dc = dobj as Component;
                            if (dc == null) continue;
                            int pidx = -1;
                            try
                            {
                                object v = dt.GetProperty("UnmanagedParentIndex", AllFlags)?.GetValue(dc, null);
                                if (v is int iv) pidx = iv;
                            }
                            catch { }
                            if (pidx < 0 || pidx >= parts.Length) continue;
                            cnt[pidx]++;
                            if (namesOf[pidx].Count < 8) namesOf[pidx].Add(dc.name);
                        }

                        for (int i = 0; i < parts.Length; i++)
                        {
                            if (cnt[i] == 0) continue;
                            Component pc = parts[i] as Component;
                            string pid = "";
                            try { pid = pc.GetType().GetProperty("Id", AllFlags)?.GetValue(pc, null) as string ?? ""; }
                            catch { }
                            sb.Append("部件 ").Append(i).Append(" [").Append(pid).Append("]  共 ")
                              .Append(cnt[i]).Append(" 个 drawable: ")
                              .Append(string.Join(", ", namesOf[i].ToArray())).AppendLine();
                        }
                    }
                    sb.AppendLine();
                }

                string dir = DiagStampDir("parttable");
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "parttable.txt"),
                    sb.ToString(), new System.Text.UTF8Encoding(false));
                Log.LogInfo("部件完整清单已导出：" + dir);
                return true;
            }
            catch (Exception e) { Log.LogWarning("导出部件清单失败：" + e.Message); return false; }
        }

        /// <summary>
        /// 丝袜状态短时间线：连续采样 `_Op_` 字段与两个丝袜部件的实际透明度。
        ///
        /// 要判定的矛盾：部件表显示 39(White) 与 40(Black) 的 opacity **同时都是 1**，
        /// 而游戏逻辑里它们是互斥的（Black=(Tights==1)?1:0，White=(Tights==2)?1:0）。
        /// 若两者恒为 1，则无论选 无/1/2 画面都不会变 —— 正好对应"选哪个看起来都一样"。
        /// </summary>
        private static bool _tightsSampling;
        private static int _tightsSampleCount;
        private static float _tightsSampleTimer;
        private static string _tightsSampleDir;
        private static System.Text.StringBuilder _tightsSampleSb;

        /// <summary>
        /// 丝袜状态时间线：**跨帧**连续采样（不能用 Sleep，那会冻结主线程导致采样值全一样）。
        ///
        /// 要判定的矛盾：部件表显示 39(White) 与 40(Black) 的 opacity **同时都是 1**，
        /// 而游戏逻辑里它们互斥（Black=(Tights==1)?1:0，White=(Tights==2)?1:0）。
        /// 若两者恒为 1，则无论选 无/1/2 画面都不会变 —— 正对应"选哪个看起来都一样"。
        /// </summary>
        private static void StartTightsSampling()
        {
            _tightsSampling = true;
            _tightsSampleCount = 0;
            _tightsSampleTimer = 0f;
            _tightsSampleDir = DiagStampDir("tightslog");
            _tightsSampleSb = new System.Text.StringBuilder();
            _tightsSampleSb.AppendLine("时间(s)	槽位	_Op_T	_Op_W	_Op_B	部件39.透明度	部件40.透明度	强制");
            Log.LogInfo("丝袜状态采样已开始（10 秒）→ " + _tightsSampleDir);
        }

        /// <summary>每帧调用；每 0.25 秒记一行，共 40 行。</summary>
        private static void TickTightsSampling()
        {
            if (!_tightsSampling) return;
            _tightsSampleTimer += Time.unscaledDeltaTime;
            if (_tightsSampleTimer < 0.25f) return;
            _tightsSampleTimer = 0f;

            try
            {
                Type camType = FindType("Live2D.Cubism.Core.CubismModel");
                Type mcType = FindType("Live2D_ModelControl");
                float opT = -1, opW = -1, opB = -1, p39 = -1, p40 = -1;

                if (mcType != null)
                {
                    UnityEngine.Object[] mcs = Resources.FindObjectsOfTypeAll(mcType);
                    if (mcs != null && mcs.Length > 0)
                    {
                        opT = GetFloat(mcs[0], "_Op_CenterGirlSitting_Tights");
                        opW = GetFloat(mcs[0], "_Op_CenterGirlSitting_Tights_White");
                        opB = GetFloat(mcs[0], "_Op_CenterGirlSitting_Tights_Black");
                    }
                }
                if (camType != null)
                {
                    UnityEngine.Object[] models = Resources.FindObjectsOfTypeAll(camType);
                    Component model = models != null && models.Length > 0 ? models[0] as Component : null;
                    if (model != null)
                    {
                        UnityEngine.Object[] parts = (UnityEngine.Object[])camType.GetProperty("Parts", AllFlags).GetValue(model, null);
                        if (parts != null)
                        {
                            if (parts.Length > 39) { object v = FieldQuiet((parts[39] as Component).GetType(), "Opacity")?.GetValue(parts[39]); if (v is float f) p39 = f; }
                            if (parts.Length > 40) { object v = FieldQuiet((parts[40] as Component).GetType(), "Opacity")?.GetValue(parts[40]); if (v is float f) p40 = f; }
                        }
                    }
                }
                object tab = Tabemi();
                float slot = tab != null ? GetFloat(tab, "Tights") : -1;

                _tightsSampleSb.Append((_tightsSampleCount * 0.25f).ToString("0.00")).Append("	").Append(slot)
                    .Append("	").Append(opT).Append("	").Append(opW).Append("	").Append(opB)
                    .Append("	").Append(p39).Append("	").Append(p40)
                    .Append("	").Append(_partOverrideEnabled ? (_forceAllParts ? "ALL" : string.Join(",", new List<string>(_partForceOn).ToArray())) : "OFF")
                    .AppendLine();
            }
            catch { }

            _tightsSampleCount++;
            if (_tightsSampleCount >= 40)
            {
                _tightsSampling = false;
                try
                {
                    System.IO.File.WriteAllText(System.IO.Path.Combine(_tightsSampleDir, "tightslog.txt"),
                        _tightsSampleSb.ToString(), new System.Text.UTF8Encoding(false));
                    Log.LogInfo("丝袜状态时间线已导出：" + _tightsSampleDir);
                }
                catch (Exception e) { Log.LogWarning("写丝袜时间线失败：" + e.Message); }
            }
        }

        /// <summary>
        /// 遮罩（Mask）诊断：列出每个 drawable 的遮罩关系，重点是丝袜那 6 个。
        ///
        /// 假设：丝袜 drawable 的**部件透明度已被正确设为 1**（时间线已证明），却看不到 ——
        /// 因为它们被某个「遮罩源 drawable」裁剪，而那个遮罩源是暗的。
        /// Cubism 的遮罩做法是"把遮罩源画进裁剪缓冲"；遮罩源不可见 → 缓冲为空 → 被遮罩者被完全裁掉。
        /// 这能同时解释两件事：(1) 部件透明度对了却看不到；(2) 「全部强制显示」后丝袜就出现。
        /// </summary>
        private bool DumpMasks()
        {
            try
            {
                Type camType = FindType("Live2D.Cubism.Core.CubismModel");
                Type dt = FindType("Live2D.Cubism.Core.CubismDrawable");
                if (camType == null || dt == null) { ProbeWhy("找不到 Cubism 类型"); return false; }
                UnityEngine.Object[] models = Resources.FindObjectsOfTypeAll(camType);
                if (models == null || models.Length == 0) { ProbeWhy("没有 CubismModel 实例"); return false; }

                var sb = new System.Text.StringBuilder();
                foreach (UnityEngine.Object mo in models)
                {
                    Component model = mo as Component;
                    if (model == null) continue;
                    UnityEngine.Object[] parts = (UnityEngine.Object[])camType.GetProperty("Parts", AllFlags).GetValue(model, null);
                    UnityEngine.Object[] draws = (UnityEngine.Object[])camType.GetProperty("Drawables", AllFlags).GetValue(model, null);
                    if (draws == null) continue;

                    sb.Append("模型: ").Append(model.name).Append("  drawable数: ").Append(draws.Length).AppendLine();
                    sb.AppendLine();
                    sb.AppendLine("--- 丝袜 drawable 的遮罩状态 ---");

                    string[] want6 = new string[] { "ArtMesh186", "ArtMesh187", "ArtMesh188", "ArtMesh189", "ArtMesh190", "ArtMesh191" };
                    foreach (string want in want6)
                    {
                        foreach (UnityEngine.Object dobj in draws)
                        {
                            Component dc = dobj as Component;
                            if (dc == null || dc.name != want) continue;

                            bool isMasked = false;
                            try { object v = dt.GetProperty("IsMasked", AllFlags)?.GetValue(dc, null); if (v is bool b) isMasked = b; } catch { }
                            bool inverted = false;
                            try { object v = dt.GetProperty("IsInverted", AllFlags)?.GetValue(dc, null); if (v is bool b) inverted = b; } catch { }
                            int ppi = -1;
                            try { object v = dt.GetProperty("ParentPartIndex", AllFlags)?.GetValue(dc, null); if (v is int iv) ppi = iv; } catch { }

                            string parentName = "?";
                            float parentOp = -1f;
                            if (parts != null && ppi >= 0 && ppi < parts.Length)
                            {
                                Component pc = parts[ppi] as Component;
                                if (pc != null)
                                {
                                    try { parentName = pc.GetType().GetProperty("Id", AllFlags)?.GetValue(pc, null) as string ?? pc.name; } catch { }
                                    try { object v = FieldQuiet(pc.GetType(), "Opacity")?.GetValue(pc); if (v is float f) parentOp = f; } catch { }
                                }
                            }

                            string mk = "";
                            try
                            {
                                UnityEngine.Object[] masks = (UnityEngine.Object[])dt.GetProperty("Masks", AllFlags)?.GetValue(dc, null);
                                if (masks != null)
                                {
                                    var ms = new List<string>();
                                    foreach (UnityEngine.Object m in masks)
                                    {
                                        Component mc = m as Component;
                                        if (mc == null) continue;
                                        int mpi = -1;
                                        try { object v = dt.GetProperty("ParentPartIndex", AllFlags)?.GetValue(mc, null); if (v is int iv) mpi = iv; } catch { }
                                        string mpn = "?";
                                        float mpo = -1f;
                                        if (parts != null && mpi >= 0 && mpi < parts.Length)
                                        {
                                            Component mpc = parts[mpi] as Component;
                                            if (mpc != null)
                                            {
                                                try { mpn = mpc.GetType().GetProperty("Id", AllFlags)?.GetValue(mpc, null) as string ?? mpc.name; } catch { }
                                                try { object v = FieldQuiet(mpc.GetType(), "Opacity")?.GetValue(mpc); if (v is float f) mpo = f; } catch { }
                                            }
                                        }
                                        ms.Add(mc.name + " -> 部件" + mpi + "[" + mpn + "] op=" + mpo.ToString("0.##"));
                                    }
                                    mk = string.Join(" ; ", ms.ToArray());
                                }
                            }
                            catch { }

                            sb.Append(want)
                              .Append("  部件=").Append(ppi).Append("[").Append(parentName).Append("] op=").Append(parentOp.ToString("0.##"))
                              .Append("  IsMasked=").Append(isMasked)
                              .Append("  IsInverted=").Append(inverted)
                              .AppendLine();
                            if (mk.Length > 0) sb.Append("        遮罩源: ").Append(mk).AppendLine();
                        }
                    }
                    sb.AppendLine();
                }

                string dir = DiagStampDir("masks");
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "masks.txt"), sb.ToString(), new System.Text.UTF8Encoding(false));
                Log.LogInfo("遮罩诊断已导出：" + dir);
                Log.LogInfo(sb.ToString());
                return true;
            }
            catch (Exception e) { Log.LogWarning("遮罩诊断失败：" + e.Message); return false; }
        }

        // =================================================================
        // 自动裤袜截图测试：依次设 Tights=0/1/2 并各截一张图
        // =================================================================
        private static bool _ttRunning;
        private static int _ttStep;          // 0,1,2 = 三个槽位；3 = 完成
        private static float _ttTimer;
        private static string _ttDir;
        private static System.Text.StringBuilder _ttSb;

        private static readonly int[] _ttValues = new int[] { 0, 1, 2 };

        /// <summary>
        /// 自动依次把裤袜设为 0/1/2，每次等画面稳定后截图 + 记录部件状态。
        ///
        /// 目的：用**屏幕像素**判定"切换裤袜到底有没有改变渲染结果"。
        /// 前面的数据已证明部件透明度被正确设置，但看不到 —— 那就必须看真实渲染出来的像素。
        /// 全自动的好处：排除手动操作时序的干扰，且不经过任何中间推测。
        /// </summary>
        private static void StartTightsTest()
        {
            // **显式关闭一切强制显示** —— 本测试的目的就是验证"不靠强制，裤袜能否正常显示"。
            // 不这样做的话，如果用户此前开着"强制显示全部部件"，结果就无法解释。
            _partOverrideEnabled = false;
            _forceAllParts = false;
            _partForceOn.Clear();
            _lastForceSig = "";

            _ttRunning = true;
            _ttStep = -1;              // -1 = 还没设过值
            _ttTimer = 0f;
            _ttDir = DiagStampDir("tightstest");
            _ttSb = new System.Text.StringBuilder();
            _ttSb.AppendLine("裤袜值\t_Op_W\t_Op_B\t部件39\t部件40\t强制状态\t截图");
            Log.LogInfo("自动裤袜截图测试开始（已强制关闭部件覆盖）→ " + _ttDir);
        }

        /// <summary>
        /// 每帧调用。
        ///
        /// 时序刻意分成两步，避免"设完值立刻测量"导致读到上一帧的状态：
        ///   第 N 次 tick：设 Tights 值
        ///   第 N+1 次 tick（0.8 秒后）：读取部件状态 + 截图
        /// </summary>
        private static void TickTightsTest()
        {
            if (!_ttRunning) return;

            _ttTimer += Time.unscaledDeltaTime;
            if (_ttTimer < 0.8f) return;
            _ttTimer = 0f;

            // 先测量上一步（如果上一步设过值）
            if (_ttStep >= 0 && _ttStep < _ttValues.Length)
            {
                int vv = _ttValues[_ttStep];
                float opW = -1, opB = -1, p39 = -1, p40 = -1;
                try
                {
                    Type mcType = FindType("Live2D_ModelControl");
                    UnityEngine.Object[] mcs = mcType != null ? Resources.FindObjectsOfTypeAll(mcType) : null;
                    if (mcs != null && mcs.Length > 0)
                    {
                        opW = GetFloat(mcs[0], "_Op_CenterGirlSitting_Tights_White");
                        opB = GetFloat(mcs[0], "_Op_CenterGirlSitting_Tights_Black");
                    }
                    Type camType = FindType("Live2D.Cubism.Core.CubismModel");
                    UnityEngine.Object[] models = camType != null ? Resources.FindObjectsOfTypeAll(camType) : null;
                    if (models != null && models.Length > 0)
                    {
                        UnityEngine.Object[] parts = (UnityEngine.Object[])camType.GetProperty("Parts", AllFlags)
                            .GetValue(models[0], null);
                        if (parts != null)
                        {
                            if (parts.Length > 39) { object o = FieldQuiet((parts[39] as Component).GetType(), "Opacity")?.GetValue(parts[39]); if (o is float f) p39 = f; }
                            if (parts.Length > 40) { object o = FieldQuiet((parts[40] as Component).GetType(), "Opacity")?.GetValue(parts[40]); if (o is float f) p40 = f; }
                        }
                    }
                }
                catch { }

                string forceState = (!_partOverrideEnabled) ? "OFF" : (_forceAllParts ? "ALL" : string.Join(",", new List<string>(_partForceOn).ToArray()));

                string shot = "tights_" + vv + ".png";
                try
                {
                    Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                    tex.Apply();
                    byte[] png = tex.EncodeToPNG();
                    UnityEngine.Object.Destroy(tex);
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(_ttDir, shot), png);
                }
                catch (Exception e) { shot = "失败:" + e.Message; }

                _ttSb.Append(vv).Append('\t').Append(opW).Append('\t').Append(opB)
                     .Append('\t').Append(p39).Append('\t').Append(p40)
                     .Append('\t').Append(forceState).Append('\t').Append(shot).AppendLine();
                Log.LogInfo("  测量 Tights=" + vv + " → _Op_W=" + opW + " _Op_B=" + opB
                            + " 部件39=" + p39 + " 部件40=" + p40 + " 强制=" + forceState);

                _ttStep++;
                if (_ttStep >= _ttValues.Length) { FinishTightsTest(); return; }
            }
            else
            {
                _ttStep = 0;
            }

            // 设本步的值
            try
            {
                object tab = Tabemi();
                if (tab != null)
                {
                    SetField(tab, "Tights", _ttValues[_ttStep]);
                    InvokeMethod(tab, "UpdateCostume");
                }
            }
            catch (Exception e) { Log.LogWarning("设 Tights 失败：" + e.Message); }
        }

        private static void FinishTightsTest()
        {
            _ttRunning = false;
            try
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(_ttDir, "tightstest.txt"),
                    _ttSb.ToString(), new System.Text.UTF8Encoding(false));
                Log.LogInfo("自动裤袜截图测试完成 → " + _ttDir);
            }
            catch (Exception e) { Log.LogWarning("写测试结果失败：" + e.Message); }
        }

        /// <summary>打开产物目录（方便看自检报告与快照）。</summary>
        private void OpenDumpDir()
        {
            try
            {
                string dir = System.IO.Path.Combine(Paths.GameRootPath, "BepInEx", "l2d_dump");
                System.IO.Directory.CreateDirectory(dir);
                System.Diagnostics.Process.Start("explorer.exe", "\"" + dir + "\"");
            }
            catch (Exception e) { Log.LogWarning("打开目录失败：" + e.Message); }
        }

        // -----------------------------------------------------------------
        // 部件可见性实验工具
        // -----------------------------------------------------------------
        private static bool _forceAllParts;
        private static string _lastForceSig = "";

        /// <summary>
        /// 部件覆盖总开关。
        ///
        /// 存在的理由：之前有三套强制来源（逐个勾选 _partForceOn、全部显示 _forceAllParts、
        /// 以及早期那个"丝袜基础层补开"），而「全部还原」只清了第一套 ——
        /// 结果丝袜与左角色的帽子之类「开出来就关不了」。
        /// 现在全部经过这一个开关：关掉即一切还原。
        /// </summary>
        private static bool _partOverrideEnabled;

        /// <summary>
        /// 互斥组：同层互斥的部件（开一个必须关另一个）。
        /// 黑白裤袜是最典型的 —— 两个都强制会让画面同时叠两层，
        /// 看起来就是"换装切黑切白没反应"。
        /// </summary>
        private static readonly string[][] ExclusiveGroups = new string[][]
        {
            new string[] { "CenterGirlSitting_Tights_Black", "CenterGirlSitting_Tights_White" },
            new string[] { "CenterGirlSitting_SkirtA", "CenterGirlSitting_SkirtB", "CenterGirlSitting_SkirtC", "HotPants" },
        };

        /// <summary>
        /// 进「部件」页时快照的原始透明度：部件名 → 当时的值。
        /// 「全部还原」靠它把部件**写回原来的隐藏状态** ——
        /// 否则只停止覆盖的话，部件会停在"被设为 1"的状态回不去（能开不能关）。
        /// </summary>
        private static readonly Dictionary<string, float> _partNaturalSnapshot = new Dictionary<string, float>();

        /// <summary>
        /// 记下各部件"未被强制时"的透明度。
        /// 仅在**覆盖开启之前**采信 —— 否则会把"已被强制成 1"的值当成原值，
        /// 反而导致"关不回去"（这就是上一版的坑）。
        /// </summary>
        private static void SnapshotPartOpacities()
        {
            if (_partOverrideEnabled) return;
            _partNaturalSnapshot.Clear();
            Type camType = FindType("Live2D.Cubism.Core.CubismModel");
            if (camType == null) return;
            foreach (UnityEngine.Object mo in Resources.FindObjectsOfTypeAll(camType))
            {
                Component comp = mo as Component;
                if (comp == null) continue;
                UnityEngine.Object[] parts = null;
                try { parts = (UnityEngine.Object[])camType.GetProperty("Parts", AllFlags).GetValue(comp, null); }
                catch { }
                if (parts == null) continue;
                foreach (UnityEngine.Object po in parts)
                {
                    Component pc = po as Component;
                    if (pc == null) continue;
                    try
                    {
                        string nm = pc.GetType().GetProperty("Id", AllFlags)?.GetValue(pc, null) as string ?? pc.name;
                        object opv = FieldQuiet(pc.GetType(), "Opacity")?.GetValue(pc);
                        if (opv is float f && !_partNaturalSnapshot.ContainsKey(nm))
                            _partNaturalSnapshot[nm] = f;
                    }
                    catch { }
                }
            }
        }

        /// <summary>把单个部件写回快照时的透明度（取消勾选时用）。</summary>
        private static void RestoreOnePart(string name)
        {
            float want;
            if (!_partNaturalSnapshot.TryGetValue(name, out want)) return;
            Type camType = FindType("Live2D.Cubism.Core.CubismModel");
            if (camType == null) return;
            foreach (UnityEngine.Object mo in Resources.FindObjectsOfTypeAll(camType))
            {
                Component comp = mo as Component;
                if (comp == null) continue;
                UnityEngine.Object[] parts = null;
                try { parts = (UnityEngine.Object[])camType.GetProperty("Parts", AllFlags).GetValue(comp, null); }
                catch { }
                if (parts == null) continue;
                foreach (UnityEngine.Object po in parts)
                {
                    Component pc = po as Component;
                    if (pc == null) continue;
                    try
                    {
                        string nm = pc.GetType().GetProperty("Id", AllFlags)?.GetValue(pc, null) as string ?? pc.name;
                        if (nm != name) continue;
                        FieldQuiet(pc.GetType(), "Opacity")?.SetValue(pc, want);
                    }
                    catch { }
                }
            }
        }

        /// <summary>把部件写回快照时的透明度（恢复隐藏状态）。</summary>
        private static void RestorePartOpacities()
        {
            Type camType = FindType("Live2D.Cubism.Core.CubismModel");
            if (camType == null) return;
            int n = 0;
            foreach (UnityEngine.Object mo in Resources.FindObjectsOfTypeAll(camType))
            {
                Component comp = mo as Component;
                if (comp == null) continue;
                UnityEngine.Object[] parts = null;
                try { parts = (UnityEngine.Object[])camType.GetProperty("Parts", AllFlags).GetValue(comp, null); }
                catch { }
                if (parts == null) continue;
                foreach (UnityEngine.Object po in parts)
                {
                    Component pc = po as Component;
                    if (pc == null) continue;
                    try
                    {
                        string nm = pc.GetType().GetProperty("Id", AllFlags)?.GetValue(pc, null) as string ?? pc.name;
                        float want;
                        if (!_partNaturalSnapshot.TryGetValue(nm, out want)) continue;
                        FieldInfo fi = FieldQuiet(pc.GetType(), "Opacity");
                        if (fi == null) continue;
                        fi.SetValue(pc, want);
                        n++;
                    }
                    catch { }
                }
            }
            if (Log != null) Log.LogInfo("已按快照还原 " + n + " 个部件的透明度");
        }

        /// <summary>贴图像素缓存（按贴图名）。GetPixels32 一次拉取，避免 8192² 上逐点 GetPixel。</summary>
        private static readonly Dictionary<string, Color32[]> _pixelCache = new Dictionary<string, Color32[]>();
        private static readonly Dictionary<string, string> _pixelNote = new Dictionary<string, string>();
        private static readonly Dictionary<string, int[]> _pixelDims = new Dictionary<string, int[]>();
        private static readonly Dictionary<string, string> _cpuFail = new Dictionary<string, string>();
        private static string _diagDir = "";
        private static bool _texProbeDone;
        private static bool _partIndexProbeDone;
        private static readonly HashSet<string> _probeWhyLogged = new HashSet<string>();

        /// <summary>部件索引探针的失败原因（同一原因只记一次，避免每帧刷屏）。</summary>
        private static void ProbeWhy(string why)
        {
            if (_probeWhyLogged.Contains(why)) return;
            _probeWhyLogged.Add(why);
            Log.LogInfo("部件索引对照：暂不可用 —— " + why);
        }
        private static string _probeInfo = "";
        private static string _exportBatch = "";
        private static string _autoDir = "";

        /// <summary>
        /// 生成一个带时间戳的产出子目录：BepInEx\l2d_dump\&lt;kind&gt;_&lt;yyyyMMdd_HHmmss&gt;\。
        /// 目的：任何一次产出都不会覆盖上一次 —— 排查问题时"哪一份是哪一次"必须可分辨。
        /// </summary>
        private static string DiagStampDir(string kind)
        {
            string dir = System.IO.Path.Combine(Paths.GameRootPath, "BepInEx", "l2d_dump",
                kind + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            System.IO.Directory.CreateDirectory(dir);
            return dir;
        }
        private static bool _autoExport;
        private static int _autoExportTimer;

        /// <summary>
        /// 贴图**实例ID** → [StreamData.offset, StreamData.size]（.resS 内的位置）。
        ///
        /// 为什么不按名字查：游戏里存在**两张同名同尺寸**的贴图 ——
        ///   · 基础图集：_Data\sharedassets0.assets.resS（**没有衣服**）
        ///   · DLC 图集：游戏根目录的 "four nights at the burger shop dlc" 包内（**有全部衣服**）
        /// 运行时绑定的是后者。按名字查表会把 DLC 贴图误判成基础贴图，
        /// 于是"离线采样"读到的是没有衣服的那张 —— 这正是先前一系列错误结论的来源。
        ///
        /// 现在改为按实例注册，且**只注册确实来自 .resS 文件的那几张**。
        /// 认不出来的（例如 DLC 包内的贴图）宁可报"来源未知"，也不静默读错文件。
        /// </summary>
        private static readonly Dictionary<int, long[]> ResInfoById = new Dictionary<int, long[]>();

        /// <summary>
        /// 已知的 .resS 内偏移对照表：按贴图名给出候选项，但**必须先核对运行时尺寸**才认。
        /// 这个表只覆盖 _Data 里的基础图集；DLC 包内的贴图不在此列（它们不在 .resS 里）。
        /// </summary>
        private static readonly Dictionary<string, long[]> ResInfoByName =
            new Dictionary<string, long[]>
        {
            { "texture_00", new long[] { 25916640L, 67108864L } },
            { "texture_01", new long[] { 13178432L,  1048576L } },
        };

        /// <summary>把某张贴图登记为"来自 .resS"（仅当尺寸与候选一致时）。</summary>
        private static void RegisterResTexture(Texture2D t)
        {
            if (t == null) return;
            int id = t.GetInstanceID();
            if (ResInfoById.ContainsKey(id)) return;
            long[] ri;
            if (ResInfoByName.TryGetValue(t.name, out ri))
            {
                long expectBytes = (long)t.width * t.height;      // DXT5 = 1 字节/像素
                if (ri[1] == expectBytes) ResInfoById[id] = ri;
            }
        }
        private static string _lastReadScale = "";
        private static int[] _lastReadDims = null;

        /// <summary>
        /// 统一的"取贴图像素"入口 —— 优先走**已验证的 CPU/.resS 路径**，GPU 回读只作最后兜底。
        ///
        /// 背景：GPU 回读（Graphics.Blit + ReadPixels）在这台机器上返回全零
        /// （实测两张贴图都报"内容占比 0.0%"，而游戏渲染完全正常），所以它不可信。
        /// 而 .resS 直读 + 自写 DXT5 解码已被逐像素验证（与 UnityPy 100% 一致），
        /// 因此引擎内的所有像素测量都应走那条路。
        /// </summary>
        private static Color32[] ReadTexturePixels(Texture2D t, out string note)
        {
            note = "";
            if (t == null) { note = "null"; return null; }

            // (1) 非压缩且可读 → Unity 自己的 CPU 解码
            if (t.isReadable && t.format != TextureFormat.DXT5)
            {
                try { note = "cpu(" + t.format + ")"; return t.GetPixels32(); }
                catch { }
            }
            // (2) .resS 直读（已验证）—— 必须按**实例**匹配，不能按名字
            RegisterResTexture(t);
            long[] ri;
            if (ResInfoById.TryGetValue(t.GetInstanceID(), out ri))
            {
                string n2;
                Color32[] px = CpuReadFromResS(t.width, t.height, ri[0], ri[1], out n2);
                if (px != null) { note = n2; return px; }
                note = n2;
            }
            else
            {
                // 不认识的贴图（很可能是 DLC 包内那张）：**不要**退回按名字读文件，
                // 否则会拿基础图集冒充，得出"内容为空"这种完全错误的结论。
                note = "来源未知(非.resS，可能是DLC包内贴图)";
                return null;
            }
            // (3) 兜底：GPU 回读（已知不可靠，仅当上面都不行时用）
            try
            {
                note = (note == "" ? "" : note + ",") + "gpu-readback(不可靠)";
                return GpuReadTexture(t);
            }
            catch (Exception e) { note = "全部失败:" + e.GetType().Name; return null; }
        }

        /// <summary>
        /// 用 GPU 回读拿一张**不可读**贴图的像素。
        ///
        /// `Texture2D.isReadable == false` 是**资产里烘死的标志位**，运行时改不了；
        /// 但它只挡 CPU 路径（GetPixels/GetPixel 会抛异常）—— GPU 仍能采样它。
        /// 做法：`Graphics.Blit` 拷进 RenderTexture 再 ReadPixels 回来。
        ///
        /// 分块回读：整张 8192² RGBA 是 268 MB，一次性读会瞬间吃掉几百 MB。
        /// 这里按 tile 逐块 blit→read，读完只占总大小的常驻内存。
        ///
        /// scale 用于**降采样快读**：scale=2 输出边长减半、耗时约 1/4，
        /// 足够判断"这块图有没有内容"；要精确比对再用 scale=1（大图会自动降到 2）。
        /// </summary>
        private static Color32[] GpuReadTexture(Texture src, int scale = 1, int tile = 2048)
        {
            if (scale < 1) scale = 1;
            // 不再自动降采样：降采样会丢细节，导致命中率统计偏低（踩过）。
            // 8192² 全分辨率回读会慢一点，但只在用户点导出时发生一次。

            int W = Mathf.Max(1, src.width / scale);
            int H = Mathf.Max(1, src.height / scale);
            _lastReadScale = (scale > 1 ? "(1/" + scale + ")" : "");
            _lastReadDims = new[] { W, H };
            var all = new Color32[W * H];
            RenderTexture rt = null;
            Texture2D tmp = null;
            RenderTexture prev = RenderTexture.active;

            try
            {
                rt = RenderTexture.GetTemporary(tile, tile, 0, RenderTextureFormat.ARGB32);
                tmp = new Texture2D(tile, tile, TextureFormat.RGBA32, false);

                for (int y = 0; y < H; y += tile)
                {
                    for (int x = 0; x < W; x += tile)
                    {
                        // 让源贴图的 (x,y) 落在 RT 的原点上：UV 偏移 + 缩放
                        float sx = (float)x / W, sy = (float)y / H;
                        float sw = Mathf.Min(1f - sx, (float)tile / W);
                        float sh = Mathf.Min(1f - sy, (float)tile / H);

                        Graphics.Blit(src, rt, new Vector2(sx, sy), new Vector2(sw, sh));

                        RenderTexture.active = rt;
                        tmp.ReadPixels(new Rect(0, 0, tile, tile), 0, 0);
                        tmp.Apply(false);
                        Color32[] block = tmp.GetPixels32();

                        int cw = Mathf.Min(tile, W - x), ch = Mathf.Min(tile, H - y);
                        for (int by = 0; by < ch; by++)
                        {
                            int srcRow = by * tile;
                            int dstRow = (y + by) * W + x;
                            Array.Copy(block, srcRow, all, dstRow, cw);
                        }
                    }
                }
            }
            finally
            {
                RenderTexture.active = prev;
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                if (tmp != null) UnityEngine.Object.Destroy(tmp);
            }
            return all;
        }

        /// <summary>列出当前透明度为 0（不可见）的部件，便于定位"某件衣服是不是根本没开"。</summary>
        private static string ListHiddenParts()
        {
            Type camType = FindType("Live2D.Cubism.Core.CubismModel");
            if (camType == null) return "找不到 CubismModel";

            var hidden = new List<string>();
            var shown = new List<string>();
            foreach (UnityEngine.Object mo in Resources.FindObjectsOfTypeAll(camType))
            {
                Component comp = mo as Component;
                if (comp == null) continue;
                UnityEngine.Object[] parts = null;
                try { parts = (UnityEngine.Object[])camType.GetProperty("Parts", AllFlags).GetValue(comp, null); }
                catch { }
                if (parts == null) continue;

                foreach (UnityEngine.Object po in parts)
                {
                    Component pc = po as Component;
                    if (pc == null) continue;
                    string nm = pc.name;
                    float op = 0f;
                    try
                    {
                        object idv = pc.GetType().GetProperty("Id", AllFlags)?.GetValue(pc, null);
                        nm = idv as string ?? pc.name;
                        object opv = FieldQuiet(pc.GetType(), "Opacity")?.GetValue(pc);
                        if (opv is float f) op = f;
                    }
                    catch { }
                    if (op < 0.5f) hidden.Add(nm); else shown.Add(nm);
                }
            }

            string s = "不可见 " + hidden.Count + " 个 / 可见 " + shown.Count + " 个";
            Log.LogInfo("部件可见性：" + s);
            Log.LogInfo("  不可见: " + string.Join(", ", hidden.ToArray()));
            Log.LogInfo("  可见: " + string.Join(", ", shown.ToArray()));
            return s + "（明细见控制台/日志）";
        }


        private static void PatchRates()
        {
            Type pc = FindType("PlayerControl");
            if (pc == null) { Log.LogWarning("找不到 PlayerControl，变化率补丁未挂载"); return; }

            int ok = 0, miss = 0;
            ok += TryPatch(pc, "HPChange", "Prefix_HPChange", ref miss);
            ok += TryPatch(pc, "HPGaugeChangeValue", "Prefix_HPGaugeChangeValue", ref miss);
            ok += TryPatch(pc, "EcstasyChange", "Prefix_EcstasyChange", ref miss);
            // EcstasyReset 有 0 参 / 1 参两个重载，按签名精确取，避免 AmbiguousMatch
            ok += TryPatch(pc, "EcstasyReset", new[] { typeof(float) }, "Prefix_EcstasyReset", ref miss);
            ok += TryPatch(pc, "EcstasyReset", new Type[0], "Prefix_EcstasyResetNoArg", ref miss);
            ok += TryPatch(pc, "EcstasyGaugeChangePercent", "Prefix_EcstasyGaugeChangePercent", ref miss);

            Log.LogInfo("变化率补丁：" + ok + " 个方法已挂载" + (miss > 0 ? "，" + miss + " 个未找到（该功能对此版本不可用）" : ""));
        }

        private static int TryPatch(Type type, string method, string prefixName, ref int miss)
        {
            return TryPatch(type, method, null, prefixName, ref miss);
        }

        private static int TryPatch(Type type, string method, Type[] argTypes, string prefixName, ref int miss)
        {
            try
            {
                MethodBase target = argTypes == null
                    ? type.GetMethod(method, AllFlags)
                    : type.GetMethod(method, AllFlags, null, argTypes, null);
                MethodInfo prefix = typeof(Plugin).GetMethod(prefixName, AllFlags);
                if (target == null || prefix == null) { miss++; return 0; }

                _harmony.Patch(target, new HarmonyMethod(prefix), null, null);
                return 1;
            }
            catch (Exception e)
            {
                Log.LogWarning("挂补丁失败 " + type.Name + "." + method + "：" + e.Message);
                miss++;
                return 0;
            }
        }

        private static Harmony _harmony;

        // =================================================================
        // 绝顶动摇（Tremor）
        //
        // 玩法：绝顶值每次【累积】时按概率触发一次衰减正弦波动，参数每次随机
        //       （振幅 / 频率 / 相位 / 落点都不同），最终停在
        //           settle = v1 - gain * loss%
        //       也就是把这次累积的收益「摇掉」一部分 —— 落点低于原本会达到的值。
        //
        // 趣味性：波动【前半段】按 R（游戏原有的抵抗键）可以「稳住」，
        //         损失从默认 40% 降到 8%，几乎全额保住；连续稳住会累积手感计数。
        //
        // 安全约束（都是本项目踩过坑换来的）：
        //   · 驱动的值绝不触顶（<= maxEcstasy * 0.985），否则 Update_Syasei() 会置位
        //     Syaseing，之后 EcstasyChange 整段忽略正值增量，值就卡死、角色动作也会停
        //   · Syaseing 为真、或游戏做大幅重置（amount 很负）时【立即中止】，交还游戏
        //   · 驱动前临时把抗性置 0 保证增量精确（EcstasyChange 里增量要乘 (1-抗性)），随后立刻还原
        //   · 驱动期间屏蔽 PlayEsctasyDamageEffect —— 它是 StartCoroutine，
        //     每帧调用会每帧起一个协程
        // =================================================================

        private static bool _tremorActive;
        private static bool _tremorCaught;
        private static bool _suppressDamageFx;
        private static bool _suppressDrive;
        private static float _tremorT0, _tremorDur, _tremorA, _tremorF, _tremorPhase, _tremorTau;
        private static float _tremorSettle, _tremorGain, _tremorV0, _tremorV1;
        private static int _tremorCatchStreak;
        private static float _preEcstasy;
        private static bool _suppressRateScale;   // SetMaxExact 精确触发期间，跳过变化率缩放
        private static float _lastAccumTime = -999f;      // 上一次累积的时刻
        private static float _accumInterval = 0.5f;        // 平滑后的累积脉冲间隔（秒）
        private static float _tremorLastEnd = -999f;       // 上一次动摇结束的时刻
        private static float _tremorIntensity;             // 触发时的攻击强度（0~7）
        private static int _tremorPat;                     // 本次使用的减速模板
        private static float _tremorPatPeriod;             // 该模板的周期（"长短不一"）
        private static float _adaptUntil = -999f;          // 绝顶适应期截止时刻
        private static float _zeroHoldUntil = -999f;       // 清零后振荡的截止时刻
        private static float _zeroHoldPhase;               // 它的相位
        private static float _zeroHoldF = 2.2f;            // 它的频率
        private static int _adaptCount;                    // 本次适应期内被削减的累积次数
        private static int _weakenCount;                   // 被削弱过的累积总次数
        private static float _hpBuffBonus;                 // 适应期生命上限的【增量】（结束按这个数减回去）

        /// <summary>前缀：记录本次调用【之前】的绝顶值（后缀里要靠它算增益）。</summary>
        private static void Prefix_EcstasyRecord(object __instance)
        {
            try { _preEcstasy = GetFloat(__instance, "CurrentEcstasy"); }
            catch { _preEcstasy = -1f; }
        }

        /// <summary>后缀：累积时掷骰决定是否触发动摇；已在波动中则只做中止判定。</summary>
        private static void Postfix_EcstasyChange(object __instance, float amount)
        {
            try
            {
                if (_suppressDrive) return;                    // 自己驱动的写入，不参与判定

                // ---- 连榨也累积绝顶值 ----
                // 游戏在 Syaseing 为真时【整段忽略正值增量】，所以连榨期间绝顶值涨不上去。
                // 这里把被忽略的那一份自己补上；封顶压在 ChainGainCap（默认 90% of max）
                // 之下，避免连榨一结束就立刻又满足触发条件、马上再来一轮。
                // 注：榨取动画（Event_Osiri吸精Damage / Event_Fella吸精）只调 HPChange，
                // 不产生绝顶值增量，所以下面这段在骑乘位/口交都【不会触发】。
                // 真正的连榨累积由 AddChainGain() 在每次重播时自己加。这段留着兜住
                // "万一某个分支确实发了正值增量"的情况，不冲突。
                if (P3(ChainEcstasyGain3) && amount > 0f && GetFloat(__instance, "Syaseing") > 0.5f)
                {
                    float maxE = GetFloat(__instance, "maxEcstasy");
                    if (maxE > 0.01f)
                    {
                        float add = amount * (1f - Mathf.Clamp01(GetFloat(__instance, "ecstasyResist")));
                        float soft = maxE * Mathf.Clamp(P3(ChainSoftCap3), 10f, 98f) / 100f;
                        float cur = GetFloat(__instance, "CurrentEcstasy");

                        // 目标值：
                        //   · 还没到软上限 → 照常往上加
                        //   · 到了软上限 → 改在【软上限 ± 波动幅度】里起伏（60% ± 20% = 40~80%）
                        float wob = Mathf.Clamp(P3(ChainWobble3), 0f, 40f) / 100f * maxE;
                        float target;
                        if (cur < soft - 0.01f)
                        {
                            target = Mathf.Min(cur + add, soft);
                        }
                        else
                        {
                            float t = Time.unscaledTime;
                            target = soft + wob * Mathf.Sin(2f * Mathf.PI * Mathf.Clamp(P3(ChainWobbleHz3), 0.05f, 8f) * t);
                            target = Mathf.Clamp(target, Mathf.Max(0f, soft - wob), soft + wob);
                        }

                        // 【平滑】值高于波动带时按速率逼近，而不是一次设过去 ——
                        // 实测抓到的 Δ-240 那类单帧跳变就是这里造成的（38 次全对得上）。
                        target = EaseToward(cur, target, maxE);
                        // 【骤降溯源】平滑之后仍有大掉幅 → 记一笔，好判断是不是这里
                        if (target - cur < -50f)
                            Log.LogInfo(string.Format("[骤降溯源] 连榨软上限：cur={0:0.#} target={1:0.#} soft={2:0.#} wob={3:0.#} maxE={4:0.#}",
                                cur, target, soft, wob, maxE));
                        float d = target - cur;
                        if (Mathf.Abs(d) > 0.002f)
                        {
                            float savedResist = GetFloat(__instance, "ecstasyResist");
                            _suppressDrive = true;                        // 别让这次写入再触发上面那段
                            SetField(__instance, "ecstasyResist", 0f);
                            InvokeFloat(__instance, "EcstasyChange", d);
                            SetField(__instance, "ecstasyResist", savedResist);
                            _suppressDrive = false;
                        }
                    }
                }

                if (_tremorActive)
                {
                    float maxE = GetFloat(__instance, "maxEcstasy");
                    bool syaseing = GetFloat(__instance, "Syaseing") > 0.5f;
                    // 游戏中途重置（绝顶清零走的就是大幅负量）→ 立刻放手
                    if (syaseing || amount <= -maxE * 0.15f) EndTremor(false);
                    return;
                }

                if (!TremorEnabled.Value) return;
                if (!(amount > 0f)) return;                     // 只在【累积】时触发

                // 先更新"攻击脉冲间隔"——绝顶值是随角色攻击动作脉冲式累积的，
                // 攻速一快脉冲就密集。这个间隔会拿去和波动频率共振。
                {
                    float now = Time.time;
                    float dt = now - _lastAccumTime;
                    _lastAccumTime = now;
                    if (dt > 0.03f && dt < 3f) _accumInterval = Mathf.Lerp(_accumInterval, dt, 0.35f);
                }

                float v1 = GetFloat(__instance, "CurrentEcstasy");
                float gain = v1 - _preEcstasy;
                if (_preEcstasy < 0f || gain <= 0.001f) return;

                // 冷却：脉冲密集时没有它会让条子一直在抖
                if (Time.unscaledTime - _tremorLastEnd < TremorCooldown.Value) return;

                if (UnityEngine.Random.value * 100f >= TremorChance.Value) return;

                StartTremor(__instance, _preEcstasy, v1, gain);
            }
            catch (Exception e) { Log.LogWarning("[动摇] 后缀异常: " + e.Message); }
        }

        /// <summary>
        /// 读当前攻击强度：把各个"正在进攻"的角色状态加总。
        /// 取值口径与游戏自己的 UpdateEcstasyResist() 一致（那里用它决定抵抗时长）。
        /// 侧位女孩各 1，坐姿 1，骑乘位 2，接吻 2。
        /// </summary>
        private static float ReadAttackIntensity()
        {
            try
            {
                object tab = Tabemi();
                if (tab == null) return 0f;
                float n = 0f;
                string lg = GetStringField(tab, "LeftGirlState");
                string rg = GetStringField(tab, "RightGirlState");
                string cg = GetStringField(tab, "centerGirlState");
                if (lg == "Attacking") n += 1f;
                if (rg == "Attacking") n += 1f;
                if (cg == "Osiri") n += 2f;
                else if (cg == "Sit") n += 1f;
                if (GetFloat(tab, "kissing") >= 2f) n += 2f;
                return n;
            }
            catch { return 0f; }
        }

        private static string GetStringField(object obj, string name)
        {
            try
            {
                FieldInfo fi = FieldQuiet(obj.GetType(), name);
                object v = fi != null ? fi.GetValue(obj) : null;
                return v != null ? v.ToString() : "";
            }
            catch { return ""; }
        }

        // ---- 减速模板：长短不一的一整套「手一软」形态 ----
        //
        // 每个模板返回 0~1 的「深度」，1 = 减速到 TremorSlow 指定的最低值，0 = 不减速。
        // 周期各不相同 —— 这正是"长短不一"：顿挫/痉挛短而密，深陷/迟疑长而沉。
        private static readonly string[] TremorPatNames =
            new string[] { "顿挫", "痉挛", "深陷", "迟疑", "潮汐" };
        private static readonly float[] TremorPatPeriods =
            new float[] { 0.42f, 0.30f, 2.20f, 1.40f, 0.90f };

        private static float EvalSlowPattern(int id, float t)
        {
            if (t < 0f) t = 0f;
            switch (id)
            {
                case 0: // 顿挫：短促方波 —— 攻速快时像卡壳
                    {
                        float u = (t % TremorPatPeriods[0]) / TremorPatPeriods[0];
                        return u < 0.5f ? 1f : 0.05f;
                    }
                case 1: // 痉挛：高频细抖 —— 幅度小但一直在颤
                    {
                        float u = (t % TremorPatPeriods[1]) / TremorPatPeriods[1];
                        return Mathf.Clamp01(0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * 3f * u));
                    }
                case 2: // 深陷：长而深 —— 一次到底，久留，再慢慢回来
                    {
                        float u = Mathf.Clamp01(t / TremorPatPeriods[2]);
                        if (u < 0.15f) return Mathf.Lerp(0f, 1f, u / 0.15f);
                        if (u < 0.70f) return 1f;
                        return Mathf.Lerp(1f, 0f, (u - 0.70f) / 0.30f);
                    }
                case 3: // 迟疑：两次犹豫 —— 想动又缩回去
                    {
                        float u = (t % TremorPatPeriods[3]) / TremorPatPeriods[3];
                        float d1 = Mathf.Exp(-Mathf.Pow((u - 0.20f) / 0.12f, 2f));
                        float d2 = Mathf.Exp(-Mathf.Pow((u - 0.65f) / 0.12f, 2f));
                        return Mathf.Clamp01(d1 + d2);
                    }
                default: // 潮汐：平滑起伏
                    {
                        float u = (t % TremorPatPeriods[4]) / TremorPatPeriods[4];
                        return Mathf.Clamp01(0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * u));
                    }
            }
        }

        private static void StartTremor(object player, float v0, float v1, float gain)
        {
            _tremorActive = true;
            _tremorCaught = false;
            _tremorT0 = Time.unscaledTime;
            _tremorDur = Mathf.Clamp(TremorDuration.Value, 0.3f, 6f);
            _tremorA = gain * Mathf.Clamp(TremorAmplitude.Value / 100f, 0f, 2f);
            // 频率：与攻击脉冲共振。脉冲越快 → 抖动越细越快；慢了 → 起伏更沉。
            if (TremorResonate.Value && _accumInterval > 0.03f)
            {
                float pulseHz = 1f / _accumInterval;                 // 攻击脉冲频率
                _tremorF = Mathf.Clamp(pulseHz * UnityEngine.Random.Range(0.35f, 0.6f), 1.4f, 5.5f);
            }
            else
            {
                _tremorF = UnityEngine.Random.Range(2.0f, 3.6f);      // 不共振时纯随机
            }
            _tremorPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            _tremorTau = _tremorDur / 2.6f;
            _tremorGain = gain; _tremorV0 = v0; _tremorV1 = v1;
            _weakenCount = 0;

            // 攻击强度：绝顶值是按角色攻击动作脉冲式累积的，而同一个角色的不同状态
            // 有各自独立的攻击力设定（TabemiAtt = baseAtt * fellaAttRate / 100，
            // 侧位女孩各有 LeftGirlState / RightGirlState）。
            // 这里读一次当前状态，让动摇的"形态"跟着来袭的攻击走：
            //   越猛 → 振幅越大、落点损失越多、抖动越沉
            // 挑一个减速模板（0 = 每次随机），"长短不一"
            {
                int pat = TremorPattern.Value;
                _tremorPat = (pat <= 0) ? UnityEngine.Random.Range(0, TremorPatNames.Length)
                                        : Mathf.Clamp(pat - 1, 0, TremorPatNames.Length - 1);
                _tremorPatPeriod = TremorPatPeriods[_tremorPat];
            }

            _tremorIntensity = ReadAttackIntensity();
            _tremorA *= 1f + _tremorIntensity * 0.15f;

            float loss = Mathf.Clamp(TremorLoss.Value, 0f, 150f)
                       + UnityEngine.Random.Range(-15f, 15f)
                       + _tremorIntensity * 6f;
            _tremorSettle = CalcSettle(player, loss);
        }

        private static float CalcSettle(object player, float lossPercent)
        {
            float maxE = GetFloat(player, "maxEcstasy");
            // 允许超过 100%：落点压到波动之前的值以下，才叫"倒扣"
            float loss = Mathf.Clamp(lossPercent, 0f, 150f) / 100f;
            return Mathf.Clamp(_tremorV1 - _tremorGain * loss, 0f, SafeCeiling(maxE));
        }

        /// <summary>留给自己的安全天花板：绝不触顶（否则会置位 Syaseing 把状态玩坏）。</summary>
        private static float SafeCeiling(float maxEcstasy)
        {
            return maxEcstasy > 0f ? maxEcstasy * 0.985f : 0f;
        }

        private static void EndTremor(bool landed)
        {
            _tremorActive = false;
            _tremorLastEnd = Time.unscaledTime;
            if (landed && _tremorCaught) _tremorCatchStreak++;
            else if (landed) _tremorCatchStreak = 0;

            // 动摇结束 → 进入「绝顶适应」期：后续累积被大幅削减。
            // 这才是它作为防御手段的实际作用，而不只是动画变慢。
            if (landed && TremorAdaptTime.Value > 0.01f && TremorAdaptFactor.Value < 100f)
            {
                _adaptUntil = Time.unscaledTime + TremorAdaptTime.Value;
                _adaptCount = 0;

                // 生命上限：按增量加上去（记下加了多少，结束时按这个数减回来）
                if (TremorHpMaxBonus.Value > 0.01f)
                {
                    object pl = Player();
                    if (pl != null)
                    {
                        // 【关键】先把上一轮还没还掉的加成还掉，再按干净的上限重算。
                        // 否则新一轮会在"已经含加成"的上限上再加一次，
                        // 而旧加成的记录又被覆盖 —— 那部分就永远减不回去，一轮轮叠成失控。
                        if (_hpBuffBonus > 0.0001f) RestoreHpBonus(pl, "新一轮开始，先还旧账");

                        float baseMax = GetFloat(pl, "maxHP");
                        _hpBuffBonus = baseMax * TremorHpMaxBonus.Value / 100f;
                        RaiseMaxHp(pl, baseMax + _hpBuffBonus);
                    }
                }
                Log.LogInfo("[动摇] 进入绝顶适应期 " + TremorAdaptTime.Value.ToString("0.#")
                            + "s，累积倍率 " + TremorAdaptFactor.Value.ToString("0") + "%");
            }
        }

        /// <summary>
        /// 绝顶值被清零后，让它在低位继续振荡一段时间。
        ///
        /// 为什么有用：`Update_Syasei()` 的触发条件是
        ///     if (!(CurrentEcstasy >= maxEcstasy) || Syaseing) return;
        /// 值一直在低位晃就永远够不到上限 —— 新的榨取不会被触发，
        /// 连榨循环自然也起不来。这比去改那条硬编码的判定更干净。
        ///
        /// 幅度必须远小于安全闸（默认 8% vs 90%），否则又会把值顶到上限、适得其反。
        /// </summary>
        private static void StartZeroHold()
        {
            if (!TremorZeroHold.Value) return;
            if (TremorZeroHoldTime.Value <= 0.5f) return;
            _zeroHoldUntil = Time.unscaledTime + TremorZeroHoldTime.Value;
            _zeroHoldPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            _zeroHoldF = Mathf.Clamp(2.0f / Mathf.Max(_accumInterval, 0.08f) * 0.45f, 1.4f, 5.5f);
        }

        private void TickZeroHold()
        {
            if (Time.unscaledTime >= _zeroHoldUntil) return;
            if (!TremorZeroHold.Value) { _zeroHoldUntil = -999f; return; }

            // 真波动在跑时不插手（两者都写同一个值会打架）
            if (_tremorActive) return;

            object player = Player();
            if (player == null) return;
            if (GetFloat(player, "Syaseing") > 0.5f) return;      // 榨取中不插手

            float maxE = GetFloat(player, "maxEcstasy");
            if (maxE <= 0.01f) return;

            float t = Time.unscaledTime;
            float amp = maxE * Mathf.Clamp(TremorZeroHoldAmp.Value, 1f, 50f) / 100f;
            // |sin| 让它只在 0~amp 之间起伏，不会把值压成负数
            float target = amp * Mathf.Abs(Mathf.Sin(2f * Mathf.PI * _zeroHoldF * t + _zeroHoldPhase));
            if (target > maxE * 0.5f) target = maxE * 0.5f;       // 绝对不许靠近上限

            DriveEcstasy(player, target);
        }

        /// <summary>每帧推进波动。必须在游戏自己写完绝顶值之后调用。</summary>
        private void TickTremor()
        {
            if (!_tremorActive) return;
            object player = Player();
            if (player == null || !TremorEnabled.Value) { EndTremor(false); return; }
            if (GetFloat(player, "Syaseing") > 0.5f) { EndTremor(false); return; }

            float maxE = GetFloat(player, "maxEcstasy");
            float t = Time.unscaledTime - _tremorT0;   // 用真实时间：减速期间动摇不会被拖长

            if (t >= _tremorDur)
            {
                DriveEcstasy(player, _tremorSettle);
                EndTremor(true);
                return;
            }

            // 稳住：只在【前半段】有效，太晚按不算
            if (!_tremorCaught && t < _tremorDur * 0.55f && Input.GetKeyDown(KeyCode.R))
            {
                _tremorCaught = true;
                _tremorSettle = CalcSettle(player, TremorCatch.Value);
                Log.LogInfo("[动摇] 稳住了！落点损失降至 " + TremorCatch.Value.ToString("0.#") + "%");
            }

            // 中心线【持续下降】：从 v1 平滑滑向落点。
            // 这样波动不是"绕一个固定值抖"，而是带着绝顶值一路走低 ——
            // 摇一次就是真的被摇掉一截，比"一定上升"有意思得多。
            float u = Mathf.Clamp01(t / _tremorDur);
            float ease = u * u * (3f - 2f * u);                       // smoothstep
            float center = Mathf.Lerp(_tremorV1, _tremorSettle, ease);

            // 包络：指数衰减 × 线性收尾，保证 t = 时长时正好归零（落点平滑，不会突然跳一下）
            float env = Mathf.Exp(-t / Mathf.Max(_tremorTau, 0.001f)) * (1f - u);
            float wave = Mathf.Sin(2f * Mathf.PI * _tremorF * t + _tremorPhase);
            float target = Mathf.Clamp(center + _tremorA * env * wave, 0f, SafeCeiling(maxE));
            DriveEcstasy(player, target);
        }

        /// <summary>
        /// 把绝顶值精确送到 target，走游戏自己的 EcstasyChange（事件/界面/夹取都照常），
        /// 但临时把抗性置 0（否则增量会被乘 (1-抗性)），并在驱动期间屏蔽伤害特效。
        /// </summary>
        private static void DriveEcstasy(object player, float target)
        {
            try
            {
                float cur = GetFloat(player, "CurrentEcstasy");
                float d = target - cur;
                if (Mathf.Abs(d) < 0.002f) return;

                float savedResist = GetFloat(player, "ecstasyResist");
                _suppressDrive = true;
                _suppressDamageFx = true;
                SetField(player, "ecstasyResist", 0f);
                InvokeFloat(player, "EcstasyChange", d);
                SetField(player, "ecstasyResist", savedResist);
                _suppressDamageFx = false;
                _suppressDrive = false;
            }
            catch (Exception e)
            {
                _suppressDrive = false;
                _suppressDamageFx = false;
                Log.LogWarning("[动摇] 驱动失败: " + e.Message);
            }
        }

        /// <summary>屏蔽 PlayEsctasyDamageEffect（它是 StartCoroutine，每帧调用会每帧起协程）。</summary>
        // ---- 视觉事件的计数：用来抓"持续闪红光"这类只在画面上体现的反复触发 ----
        private static readonly List<float> _fxSyaseiTimes = new List<float>();
        private static readonly List<float> _fxDamageTimes = new List<float>();
        private static readonly List<float> _fx吸精Times = new List<float>();   // 吸精红光（真正的那一个）

        private static void NoteFx(List<float> list)
        {
            float now = Time.unscaledTime;
            list.Add(now);
            for (int i = list.Count - 1; i >= 0; i--)
                if (now - list[i] > 30f) list.RemoveAt(i);      // 只留最近 30 秒
        }

        private static int FxCountIn(List<float> list, float seconds)
        {
            float now = Time.unscaledTime;
            int n = 0;
            for (int i = 0; i < list.Count; i++) if (now - list[i] <= seconds) n++;
            return n;
        }

        /// <summary>
        /// 吸精红光特效。
        ///
        /// 【重要】这才是"持续冒红光"里的那个红光 —— 由 `Event_Fella吸精()` /
        /// `Event_SitKiss吸精1()` 这些【动画事件】调用，每个动画循环触发一次。
        /// 我最初把计数挂在 `PlaySyaseiEffect` 上（那是 `SyaseiEffectImage`，另一个效果），
        /// 于是检查一直报 0 次，还被我当成"排除"的依据 —— 盯错了对象。
        /// </summary>
        private static void Prefix_Play吸精Effect()
        {
            NoteFx(_fx吸精Times);
        }

        /// <summary>榨取红光特效（Event_*Syasei2 里调用）。</summary>
        private static void Prefix_PlaySyaseiEffect()
        {
            NoteFx(_fxSyaseiTimes);
        }

        private static bool Prefix_PlayEsctasyDamageEffect()
        {
            NoteFx(_fxDamageTimes);
            return !_suppressDamageFx;
        }

        // =================================================================
        // 背面骑乘「索取模式」+「余韵」
        //
        // 玩法：
        //   在背面骑乘（Osiri）的攻击阶段打屁股（点击计数）时——
        //     · 累积「索取欲」，每次随机加 DemanUrgeMin~Max%；
        //       索取欲的数值【本身就是进入索取模式的概率】（每次打都重新掷骰）
        //     · 并有 DemandEscapePenaltyChance% 的机会把「脱出所需点击次数」大幅提高
        //   进入索取模式后：
        //     · 持续 DemandDurationMin~Max 秒（随机）
        //     · 【速度不因射精而减缓】—— 游戏在 Event_FellaSyaseiStart() 里
        //       把 fellaSpeed 减半并停掉加速累积，这里在索取模式期间原样恢复
        //     · 只能由角色自行退出（时间到），玩家无法打断
        //   退出后进入「余韵」：连续 DemandAfterglowMin~Max 次吸精
        //     （复用下面 KyuseiRate 修正的"借用整数除法"设施来强制重播）
        // =================================================================

        private static float _demandUrge;              // 索取欲（0~100），数值即触发概率
        private static int _urgeAuditLeft;             // 索取欲审计剩余条数（命令开启）
        private static bool _demandMode;
        // 【模式是在哪个姿势进入的】0=口交 1=背榨 2=正骑；-1=没有模式。
        // 这是"夺回骑乘位"那个兜底的关键 —— 它原来看 _demandMode 就假定在 Osiri，
        // 于是坐姿自己的榨取欲一满就把人物拽去 Osiri（三个症状一个根因）。
        private static int _demandModePose = -1;
        private static float _demandUntil;
        private static int _afterglowRemaining;        // 余韵剩余吸精次数
        private static int _demandTotal, _afterglowTotal;
        private static int _mergeDebugLeft = 6;
        private static int _chainLogLeft = 8;      // 折算日志的前几次，用来验证
        private static int _fellaReturns;      // 真正回到口交的次数（用于显示）
        private static float _osiriReturnChance;   // 「再次进入骑乘位」的累积概率
        private static bool _osiriReturnArmed;     // 是否处于"从骑乘位回到口交后"的状态
        private static bool _sitDeferred;          // 坐姿是否被挂起，等这次骑乘位结束
        private static bool _afterglowWasRunning;        // 用来捕捉"余韵刚刚烧完"那一刻
        private static int _afterglowPending;      // 模式中射精累积的余韵次数（还没发动）
        private static bool _demandEntered;        // 本周期是否真的进过索取/榨取模式（余韵的闸门）
        private static int _demandSyaseiCount;     // 本次索取/榨取模式中射精了几次
        private static float _afterglowSettleAt = -999f;  // 局面稳定后发动余韵的时刻
        private static float _lastDemandActivity = -999f; // 最后一次进入/叠加索取·榨取的时刻
        private static int _demandStacks;      // 当前索取模式叠了几段（1~DemandMaxStacks）
        // 连榨计数按【姿势 × 是否索取/榨取模式】分四套，与四个上限一一对应
        private static int _chainFella;
        private static int _chainOsiriNormal;
        private static int _chainOsiriDemand;
        private static int _chainSitNormal;
        private static int _chainSitDemand;
        private static float _preFellaSpeed;
        private static bool _preSpeedUp;
        private static bool _preSpeedUpValid;
        private static int _preOsiriHits = -1;

        // ---- 接管骑乘位的吸精收尾 ----
        //
        // 游戏原码：
        //     private void Event_Osiri吸精End()
        //     {
        //         if (GameManager.gameover)                  Play_Osiri吸精(true);
        //         else if (UnityEngine.Random.value < 0.9f)  Play_Osiri吸精(true);   // ← 硬编码 90%
        //         else                                       tabemi.Osiri解除();
        //     }
        //
        // 【关键】它用的是**硬编码 0.9**，压根不读 KyuseiRate ——
        // 所以针对 KyuseiRate 做的那套（改字段借整数除法）对骑乘位完全无效，
        // 连榨次数上限自然也不生效。这里必须自己接管分支。
        private static bool Prefix_Event_Osiri吸精End(object __instance)
        {
            try
            {
                object player = Player();
                bool gameover = false;
                try
                {
                    Type gm = FindType("GameManager");
                    FieldInfo gf = gm != null ? FieldQuiet(gm, "gameover") : null;
                    object gv = gf != null ? gf.GetValue(null) : null;
                    gameover = gv is bool gb && gb;
                }
                catch { }

                bool repeat;
                if (gameover)
                {
                    repeat = true;                       // 原逻辑：gameover 必定连榨
                }
                else if (_afterglowRemaining > 0 && !_demandMode)
                {
                    repeat = true;                       // 余韵：强制连榨（只在非索取模式下）
                    _afterglowRemaining--;
                    Log.LogInfo("[" + ModeWord() + "] 余韵（骑乘位）：强制连榨，剩 " + _afterglowRemaining + " 次");
                }
                else
                {
                    repeat = UnityEngine.Random.value < 0.9f;   // 原版基准

                    // 连榨次数上限 —— 这才是 KyuseiMaxChain 该管的地方
                    if (repeat && ChainLimitNow() > 0 && ChainNow() >= ChainLimitNow())
                    {
                        repeat = false;
                        Log.LogInfo("[吸精] 已达连榨上限 " + ChainLimitNow()
                                    + " 次（" + ChainSlotName() + "），强制收尾");
                    }
                    if (repeat) ChainSet(ChainNow() + 1); else ChainSet(0);
                }

                if (repeat)
                {
                    // 注：绝顶值增量改由【伤害事件】按对生命值的影响折算，这里不再加，避免重复计数。
                    int left = ChainLimitNow() > 0
                        ? Mathf.Max(0, ChainLimitNow() - ChainNow())
                        : -1;
                    ShowChainBanner(_afterglowRemaining > 0
                        ? string.Format("余韵 · 剩余 {0} 次", _afterglowRemaining)
                        : (left >= 0 ? string.Format("连榨 · 剩余 {0} 次", left) : "连榨"));
                    InvokeAt(__instance, "Play_Osiri吸精", true);
                }
                else if (_demandMode && P3(DemandBlockFella3))
                {
                    // 索取模式期间"该结束了"也不脱出 —— 改为回骑乘位攻击继续
                    InvokeAt(__instance, "Play_OsiriMainMixer");
                    Log.LogInfo("[" + ModeWord() + "] 吸精该收尾，但索取模式期间不脱出 → 回骑乘位继续");
                }
                else
                {
                    object tab = Tabemi();
                    if (tab != null) InvokeNoArg(tab, "Osiri解除");
                }
                return false;      // 已接管
            }
            catch (Exception e)
            {
                Log.LogWarning("[" + ModeWord() + "] 接管骑乘位吸精收尾失败: " + e.Message);
                return true;       // 出错就交还原逻辑
            }
        }

        /// <summary>
        /// 索取模式期间不让 Osiri解除() 把角色送回口交 —— 只能由角色自己退出索取模式。
        /// 玩家点满了也一样拦（这正是"索取模式无法被打断"的一部分）。
        /// </summary>
        private static bool Prefix_Osiri解除()
        {
            if (_demandMode && P3(DemandBlockFella3))
            {
                Log.LogInfo("[" + ModeWord() + "] 索取模式期间：拦下一次脱出到口交");
                return false;
            }
            return true;
        }

        /// <summary>后缀：真正脱出成功时，把"骑乘位门槛"降低。</summary>
        private static void Postfix_Osiri解除()
        {
            OnReturnedToFella();
        }

        // =================================================================
        // 打屁股的速度冲量
        //
        // 每打一次屁股：
        //   · 目标乘数快速抬高一截（第一下 SpankSpeedGain%，之后每次新增量 × SpankSpeedStack%）
        //   · 实际乘数【快速逼近】目标（SpankSpeedRise）
        //   · 目标本身【缓慢衰减】回 1（SpankSpeedDecay）
        // 于是表现是：打一下就猛地快一截，然后慢慢消掉；连打会越叠越高但每次加得越来越少。
        //
        // 施加点是主混合器动画的播放速率（EffectiveSpeed），只在攻击阶段（OsiriMixer 在播）生效。
        // =================================================================
        private static float _spankMul = 1f;          // 实际施加的乘数
        private static float _spankTarget = 1f;       // 目标乘数（缓慢衰减）
        private static float _spankAdd = 0f;          // 本次新增量（用于递减叠加）
        private static int _spankCount;

        private static void NoteSpank()
        {
            if (!P3(SpankSpeedEnabled3)) return;
            float gain = Mathf.Max(0f, P3(SpankSpeedGain3)) / 100f;
            if (gain <= 0.0001f) return;

            float stack = Mathf.Clamp(P3(SpankSpeedStack3) / 100f, 0f, 1f);
            // 已经消退干净就重新起算，否则按递减倍率叠加
            float add = (_spankAdd <= 0.0001f) ? gain : _spankAdd * stack;
            _spankAdd = add;
            _spankCount++;
            _spankTarget += add;
            Log.LogInfo("[屁股] 第 " + _spankCount + " 下：冲量 +" + (add * 100f).ToString("0.#")
                        + "% → 目标 " + _spankTarget.ToString("0.###"));
        }

        /// <summary>每帧推进：实际值快速逼近目标，目标缓慢衰减回 1。</summary>
        private static void TickSpankSpeed()
        {
            if (!P3(SpankSpeedEnabled3))
            {
                _spankMul = 1f; _spankTarget = 1f; _spankAdd = 0f; _spankCount = 0;
                return;
            }

            float dt = Time.unscaledDeltaTime;
            _spankTarget = Mathf.Max(1f, _spankTarget - Mathf.Max(0.01f, P3(SpankSpeedDecay3)) * dt * (_spankTarget - 1f + 0.15f));
            if (_spankTarget < 1.001f) { _spankTarget = 1f; _spankAdd = 0f; _spankCount = 0; }

            // 快速逼近（Rise 越大越快）
            float k = 1f - Mathf.Exp(-Mathf.Max(1f, P3(SpankSpeedRise3)) * dt);
            _spankMul += (_spankTarget - _spankMul) * k;
            if (Mathf.Abs(_spankMul - 1f) < 0.0005f) _spankMul = 1f;

            // 施加到主混合器的动画速率上（只影响攻击动画，不碰绝顶/吸精片段）
            if (_spankMul <= 1.0005f) return;
            try
            {
                Type sot = FindType("Live2D_Animation_SitOsiri");
                if (sot == null) return;
                UnityEngine.Object[] objs = Resources.FindObjectsOfTypeAll(sot);
                if (objs == null || objs.Length == 0) return;

                // 【按姿势找对混合器】
                //   Osiri（背榨）→ OsiriMixer
                //   Sit（正骑）  → SitMixerA / SitMixerB（接吻态另有 SitKiss 那一对）
                // 原来只找 OsiriMixer —— 于是 HitArea_Manman 就算给了冲量，
                // 也作用不到坐姿的动画上（坐姿根本没有 OsiriMixer 在播）。
                string[] mixerNames = { "OsiriMixer", "SitMixerA", "SitMixerB", "SitKissMixerA", "SitKissMixerB" };
                foreach (string mn in mixerNames)
                {
                    object mx = FieldQuiet(sot, mn)?.GetValue(objs[0]);
                    if (mx == null) continue;
                    object st = mx.GetType().GetProperty("State", AllFlags)?.GetValue(mx, null);
                    if (st == null) continue;
                    PropertyInfo ip2 = st.GetType().GetProperty("IsPlaying", AllFlags);
                    object v2 = ip2 != null ? ip2.GetValue(st, null) : null;
                    if (!(v2 is bool b2 && b2)) continue;      // 这个混合器没在播，试下一个
                    PropertyInfo sp2 = st.GetType().GetProperty("EffectiveSpeed", AllFlags);
                    if (sp2 != null && sp2.CanWrite) sp2.SetValue(st, _spankMul, null);
                    break;
                }
            }
            catch { }
        }

        /// <summary>
        /// 骑乘位【常态】下，角色每攻击一次有较低概率提升索取欲。
        ///
        /// 挂点说明：OsiriMixer 的循环事件把 Event_OsiriGirlMainMixer 注册在归一化时间 0.01 处，
        /// 所以它【每个攻击循环触发一次】—— 正是"每攻击一次"的口径。
        /// </summary>
        private static void Postfix_OsiriGirlMainMixer()
        {
            NoteDamageActivity();
            try
            {
                if (!P3(DemandEnabled3)) return;
                // 【模式中也要累积】——原以为"模式中不再靠攻击累积"能防跑飞，
                // 但每层门槛（DemandThresholdBase/Step）已经管住了节奏，
                // 再拦这一道只会让玩家只能靠打屁股攒，反而更死。
                // 现在攻击照常累积，攒到下一层门槛后再由打屁股那一掷决定是否叠加。
                if (P3(AttackUrgeChance3) <= 0f || P3(AttackUrgeGain3) <= 0f) return;

                // 【滑块设的是平均值】—— 每次攻击各自抽一个围绕平均值的系数。
                // 系数取 (1 + U(-j, +j))，期望值正好是 1，所以长期平均等于滑块值。
                float j = Mathf.Clamp(P3(AttackUrgeJitter3), 0f, 90f) / 100f;
                float fChance = 1f + UnityEngine.Random.Range(-j, j);
                float fGain = 1f + UnityEngine.Random.Range(-j, j);

                float chance = Mathf.Clamp(P3(AttackUrgeChance3) * fChance, 0f, 100f);
                if (UnityEngine.Random.value * 100f >= chance) return;

                float gain = Mathf.Max(0f, P3(AttackUrgeGain3) * fGain) * UrgeScaleNow();
                float beforeA = _demandUrge;
                _demandUrge = Mathf.Max(0f, _demandUrge + gain);   // 不设上限：叠层时归零，本身就自限
                AuditUrge("攻击命中(概率骰 " + chance.ToString("0.#") + "% 增量 " + gain.ToString("0.##") + ")", beforeA, _demandUrge);
                // 【修 #1】攻击累积完也过一遍叠层判定 —— 原先它只填进度条、从不消费。
                if (P3(AttackCanStackDemand3)) TryStackNow("角色攻击");
            }
            catch { }
        }

        /// <summary>
        /// 一次连榨按【对生命值的影响】折算绝顶值增量。
        ///
        /// 为什么用伤害常量而不是实际掉血量：开了无敌 / 锁血 / 各种保命功能时，
        /// HPChange 其实没有真的扣血，读实际掉血量会得到 0，连榨就永远不涨。
        /// 而伤害常量（Osiri吸精ダメージ = 5、SitKiss吸精ダメージ = 8、吸精ダメージ = 7）
        /// 是"这一下本该打掉多少"，与保命功能无关 —— 正是"对生命值的影响"的本意。
        ///
        /// 换算：增量（占绝顶最大值 %） = 伤害 / 最大生命 × 100 × ChainGainPerHp%
        /// 这样两边单位无关，改最大生命上限也不会失衡。
        /// </summary>
        private static void AddChainGainByHp(object player, float hpDamage)
        {
            if (!P3(ChainEcstasyGain3) || player == null) return;
            if (hpDamage <= 0f) return;
            try
            {
                float ratio = Mathf.Clamp(P3(ChainGainPerHp3), 0f, 2000f);
                if (ratio <= 0.0001f) { AddChainGainFixed(player, 0f); return; }

                float maxHp = GetFloat(player, "maxHp");
                if (maxHp <= 0.01f)
                {
                    // maxHp 不在 Player 上时退而求其次：用 HP 上限的配置值
                    try
                    {
                        Type gm = FindType("GameManager");
                        if (gm != null) maxHp = GetFloat(gm, "maxHp");
                    }
                    catch { }
                    if (maxHp <= 0.01f) maxHp = 200f;
                }

                float pct = hpDamage / maxHp * 100f * ratio / 100f;   // 占绝顶最大值的百分比
                AddChainGainFixed(player, pct);

                if (_chainLogLeft > 0)
                {
                    _chainLogLeft--;
                    Log.LogInfo("[连榨] 按生命值折算：" + hpDamage.ToString("0.##") + " 伤害 / "
                                + maxHp.ToString("0.#") + " 最大生命 = "
                                + (hpDamage / maxHp * 100f).ToString("0.##") + "% → 绝顶值 +"
                                + pct.ToString("0.##") + "%（比例 " + ratio.ToString("0") + "%）");
                }
            }
            catch { }
        }

        /// <summary>
        /// 一次连榨给绝顶值加一截。
        ///
        /// 为什么要插件自己加：榨取动画的事件（Event_Osiri吸精Damage / Event_Fella吸精）
        /// 都【只调 HPChange】，不产生任何绝顶值增量 —— 所以没有"进来的正值"可以缩放。
        ///
        /// 到达软上限（默认 60%）后不再上涨，改为在【软上限 ± 波动幅度】之间起伏
        /// （60% ± 20% = 40~80%）。
        /// </summary>
        private static void AddChainGain(object player) { AddChainGainFixed(player, -1f); }

        private static void AddChainGainFixed(object player, float pctOverride)
        {
            if (!P3(ChainEcstasyGain3) || player == null) return;
            try
            {
                float gain = pctOverride >= 0f
                    ? Mathf.Clamp(pctOverride, 0f, 100f) / 100f
                    : Mathf.Clamp(P3(ChainGainPerDrain3), 0f, 100f) / 100f;
                if (gain <= 0.000001f) return;
                float maxE = GetFloat(player, "maxEcstasy");
                if (maxE <= 0.01f) return;

                // 余韵期间用【它自己的一套】：软上限与波动都与连榨分开。
                // 余韵是收尾，理应比连榨更克制。
                bool inAfterglow = _afterglowRemaining > 0 && !_demandMode;
                float softPct = inAfterglow ? Mathf.Clamp(P3(AfterglowSoftCap3), 5f, 98f)
                                            : Mathf.Clamp(P3(ChainSoftCap3), 10f, 98f);
                float wobPct = inAfterglow ? Mathf.Clamp(P3(AfterglowEcstasyWobble3), 0f, 40f)
                                           : Mathf.Clamp(P3(ChainWobble3), 0f, 40f);
                float hz = inAfterglow ? 0.45f : Mathf.Clamp(P3(ChainWobbleHz3), 0.05f, 8f);

                float soft = maxE * softPct / 100f;
                float wob = wobPct / 100f * maxE;
                float cur = GetFloat(player, "CurrentEcstasy");

                float target;
                if (cur < soft - 0.01f) target = Mathf.Min(cur + gain * maxE, soft);
                else
                {
                    float t = Time.unscaledTime;
                    target = soft + wob * Mathf.Sin(2f * Mathf.PI * hz * t);
                    target = Mathf.Clamp(target, Mathf.Max(0f, soft - wob), soft + wob);
                }

                float d = target - cur;
                if (Mathf.Abs(d) <= 0.002f) return;
                float savedResist = GetFloat(player, "ecstasyResist");
                _suppressDrive = true;
                SetField(player, "ecstasyResist", 0f);
                InvokeFloat(player, "EcstasyChange", d);
                SetField(player, "ecstasyResist", savedResist);
                _suppressDrive = false;
            }
            catch { }
        }

        /// <summary>进入或叠加索取模式。已在模式中则只叠加【总时长】。</summary>
        private static void EnterOrStackDemand(string reason)
        {
            float dlo = Mathf.Min(P3(DemandDurationMin3), P3(DemandDurationMax3));
            float dhi = Mathf.Max(P3(DemandDurationMin3), P3(DemandDurationMax3));
            float seg = UnityEngine.Random.Range(dlo, dhi);
            int maxStacks = Mathf.Max(1, P3(DemandMaxStacks3));

            // 【任何一次进入或叠加都要做的事】：
            //   · 记下时刻 —— 点火判定要"距最后一次叠加足够久"，否则会在叠的过程中漏出去
            //   · 取消即将点火的计时 —— 抵消本帧内已经贴近的发动
            _lastDemandActivity = Time.unscaledTime;
            _afterglowSettleAt = -999f;

            if (!_demandMode)
            {
                _demandMode = true;
                _demandModePose = PoseIdx();     // ← 记下是在哪个姿势进的
                _demandStacks = 1;
                ChainResetAll();       // 四套计数全部归零
                _demandSyaseiCount = 0;
                _afterglowSettleAt = -999f;
                _demandEntered = true;      // 余韵的闸门：本周期进过模式才允许发动
                _attBoostLogged = false;
                _demandTotal++;
                _demandUntil = Time.unscaledTime + seg;
                AuditUrge("进入模式·清零", _demandUrge, 0f);
                _demandUrge = 0f;
                // 同上：收回而非作废。进入新模式时，上一轮没跑完的余韵退回待发动。
                if (_afterglowRemaining > 0)
                {
                    int back0 = _afterglowRemaining;
                    _afterglowPending += back0;
                    _afterglowRemaining = 0;
                    _afterglowTotal = Mathf.Max(0, _afterglowTotal - 1);
                    Log.LogInfo("[" + ModeWord() + "] 进入新模式，把未跑完的余韵收回待发动 " + back0
                                + " 次 → 待发动共 " + _afterglowPending + " 次");
                }
                Log.LogInfo("[" + ModeWord() + "] 进入" + ModeWord() + "模式（" + reason + "），本段时长 " + seg.ToString("0.#")
                            + " 秒，共 1 段");
            }
            else if (_demandStacks < maxStacks)
            {
                _demandStacks++;
                _demandUntil += seg;      // 【只叠总时长】，不重新计时
                AuditUrge("叠加·清零", _demandUrge, 0f);
                _demandUrge = 0f;
                // 叠加期间余韵绝不能点火：既不能已激活，也不能待发动立刻冒出来。
                // 待发动的次数【保留】—— 按设计它要等"最后一段及其连榨结束后"才发动，
                // 所以这里只是把点火推迟，不是丢掉。
                // 【不能作废，只能收回。】
                // 作废会让"余韵的消失"与"余韵已跑完"变得无法区分 ——
                // 累积值凭空少掉、_afterglowTotal 也对不上账。
                // 正确做法是把它退回待发动，等最后一段及其连榨全部结束后重新点火。
                if (_afterglowRemaining > 0)
                {
                    int back = _afterglowRemaining;
                    _afterglowPending += back;
                    _afterglowRemaining = 0;
                    _afterglowTotal = Mathf.Max(0, _afterglowTotal - 1);   // 这次不算"已触发"
                    Log.LogInfo("[" + ModeWord() + "] 叠加期间把进行中的余韵收回待发动 " + back
                                + " 次 → 待发动共 " + _afterglowPending + " 次");
                }
                Log.LogInfo("[" + ModeWord() + "] 叠加第 " + _demandStacks + " 段（" + reason + "），+"
                            + seg.ToString("0.#") + " 秒 → 剩余 "
                            + Mathf.Max(0f, _demandUntil - Time.unscaledTime).ToString("0.#") + "s");
            }
            else
            {
                // 【叠满段数后不再加层，但刷新持续时间】
                // 原先这里什么都不做 —— 于是段满之后，玩家再怎么打屁股、
                // 再怎么把欲望攒够，模式都只会静静走向结束，积累全白费。
                // 现在：条件达成了就给时间，只是不再增段。
                float mul = Mathf.Clamp(P3(DemandMaxStackRefreshMul3), 0f, 300f) / 100f;
                if (mul > 0.001f)
                {
                    _demandUntil += seg * mul;
                    _demandUrge = 0f;      // 消耗掉这次积累
                    Log.LogInfo("[" + ModeWord() + "] 已达 " + maxStacks + " 段上限 → 不加层，但刷新持续时间 +"
                                + (seg * mul).ToString("0.#") + " 秒 → 剩余 "
                                + Mathf.Max(0f, _demandUntil - Time.unscaledTime).ToString("0.#") + " 秒");
                }
                else
                {
                    Log.LogInfo("[" + ModeWord() + "] 已达 " + maxStacks + " 段上限，本次不再叠加");
                }
            }
        }

        /// <summary>前缀：记录打屁股前的计数与速度状态。</summary>
        private static void Prefix_OsiriHits(object __instance)
        {
            try { _preOsiriHits = (int)GetFloat(__instance, "Osiri叩く量"); }
            catch { _preOsiriHits = -1; }
        }

        /// <summary>后缀：计数增加了 = 一次成功打屁股 → 累积索取欲 / 掷骰。</summary>
        private static void Postfix_OsiriHits(object __instance)
        {
            // 计数增加 = 一次成功打屁股 → 交给共用逻辑
            try
            {
                if (_preOsiriHits < 0 || !P3(DemandEnabled3)) return;
                int now;
                try { now = (int)GetFloat(__instance, "Osiri叩く量"); }
                catch { return; }
                if (now <= _preOsiriHits) return;

                DoSpankLogic(__instance);
            }
            catch { }
        }

        /// <summary>
        // =================================================================
        // Sit（坐姿）的「榨取模式」—— 把 Osiri 的索取模式适配过来
        //
        // 结构对照（两边完全对应）：
        //   osiriState (none/timing/waiting/attacking/kyusei/syasei)
        //     ↕  sitState (none/attacking/syasei/kyusei)
        //   Osiri叩く量 / Osiri解除叩く量 = 7
        //     ↕  SitGirlKiss叩く量 / SitGirlKiss解除叩く量 = 10
        //   打屁股 Osiri叩かれる()
        //     ↕  点头部 頭叩かれるSit()（kissing == 1 时计数）
        //   OsiriMixer ↕ SitMixerA / SitMixerB / SitMixerKissA / SitMixerKissB
        //
        // 【按情况去掉的功能】Sit 没有"回口交"这条路径
        //   （Event_SitSyasei_OnEnd 永远 Play_Sit()，Event_SitKissSyaseiOnEnd 只在
        //     Play_SitKiss 与 Play_SitKiss吸精 之间二选一），
        //   所以「回口交概率」「不回口交」那两项不移植。
        //
        // 【必须补的功能】Sit 的吸精收尾是
        //     Even_SitKiss吸精OnEnd() { if (gameover) Play_SitKiss吸精(true);
        //                               else          Play_SitKiss吸精(true); }
        //   —— 两边一模一样，**永远重播**，默认就是无限连榨。
        //   所以连榨上限在这里比在 Osiri 更需要。
        // =================================================================

        private static int _preSitHits = -1;

        private static void Prefix_SitHits(object __instance)
        {
            try { _preSitHits = (int)GetFloat(__instance, "SitGirlKiss叩く量"); }
            catch { _preSitHits = -1; }
        }

        /// <summary>
        /// 【连榨强制第 4 档撞击声】
        ///
        /// 撞击声的机制：Osiri 骑乘动画片段上挂了按名字回调的 Unity 动画事件 "OsiriSE"，
        ///   节奏来自动画事件的排布，音色来自 osiriSpeed 的档位：
        ///     <0.25 → 1、<0.5 → 2、<=0.75 → 3、<=1 → 4   （OsiriList1~4 四组音频）
        /// 这里在吸精/连榨（osiriState == kyusei）时绕过那套分档，直接放第 4 档。
        /// </summary>

        private static bool Prefix_OsiriSE(object __instance)
        {
            if (!OsiriKyuseiTier4.Value) return true;
            try
            {
                // 【连榨用 3 / 4 档，其余默认】
                //
                // 原版 OsiriSE() 是按 osiriSpeed 分四档：
                //     <0.25→1、<0.5→2、<=0.75→3、<=1→4
                // 这里只在【连榨（osiriState == kyusei）】时接管，
                // 在 3、4 两档之间随机；其余状态一律交还原逻辑（走速度分档）。
                object tab = Tabemi();
                if (tab == null) return true;
                if (GetStringField(tab, "osiriState") != "kyusei") return true;   // 其余默认

                FieldInfo hf = FieldQuiet(__instance.GetType(), "hSound");
                object hs = hf != null ? hf.GetValue(__instance) : null;
                if (hs == null) return true;

                MethodInfo mi = hs.GetType().GetMethod("PlayOsiri", AllFlags);
                if (mi != null)
                {
                    int lv = UnityEngine.Random.value < 0.5f ? 3 : 4;
                    mi.Invoke(hs, new object[] { lv });
                    return false;      // 已接管，跳过原本的分档
                }
            }
            catch { }
            return true;
        }

        // =================================================================
        // 正骑：束缚之吻的专门调整
        //
        // 正骑的连榨与余韵【只有进入束缚之吻才会发生】——
        // 榨取入口 Play_SitKiss吸精() 强制 kissing = 2，而进入它要经过
        // PrepareKissing()（0→1，窗口 KissPrepareTime）→ StartKISS()（1→2）。
        // 原版这条路径靠 10% 随机或按键 X，而且窗口每次解除都 *= 0.9 越玩越短。
        // =================================================================

        private static float _kissAutoAt = -999f;

        /// <summary>每帧：坐姿常态下自动进一次束缚之吻。</summary>
        /// <summary>
        /// 【束缚之吻 · 自己的调速】
        ///
        /// 只在"sitState != kyusei"时生效 —— 进了榨取就交给榨取/绝顶那套倍速，
        /// 两边都设会互相打架。
        /// </summary>
        // =================================================================
        // 绝顶值【单帧骤降】侦测 —— 第 7 条的复发警报
        // =================================================================

        private static float _lastEc = -1f;
        private static int _ecDropCount;

        /// <summary>
        /// 【把"被上限拉下来"变成平滑过渡】
        ///
        /// 原来凡是 cur > cap 就 SetField(cur, cap) —— 一次到位，
        /// 单帧就会出现 Δ-240 这种跳变（实测 38 次全是它）。
        /// 数值确实该下来，但不该"啪"一下。
        ///
        /// 返回：应当写入的值（调用方负责写回去）。
        /// </summary>
        private static float EaseToward(float cur, float target, float maxRef)
        {
            if (!CapSmoothEnabled.Value) return target;
            if (maxRef <= 0.001f) return target;
            float step = Mathf.Max(1f, CapSmoothRate.Value) / 100f * maxRef * Time.unscaledDeltaTime;
            if (Mathf.Abs(cur - target) <= step) return target;
            return cur > target ? cur - step : cur + step;
        }

        private static void TickEcstasyDropWatch()
        {
            float warn = EcstasyDropWarn.Value;
            if (warn <= 0.01f) return;
            try
            {
                object pl = Player();
                if (pl == null) return;
                float cur = GetFloat(pl, "CurrentEcstasy");
                if (_lastEc < 0f) { _lastEc = cur; return; }

                // 【排除射精】Syaseing 期间绝顶值被正常消费掉（实测是两次减半 → 0），
                // 那不是异常。要抓的是"没在射精却骤降"。
                if (GetFloat(pl, "Syaseing") > 0.5f) { _lastEc = cur; return; }

                float d = cur - _lastEc;
                if (d < -warn)
                {
                    _ecDropCount++;
                    object tab = Tabemi();
                    Log.LogWarning(string.Format(
                        "[绝顶骤降 #{0}] {1:0.#} → {2:0.#}（Δ{3:0.#}）  姿势={4}  模式={5}(入于{6})  段={7}"
                        + "  sitState={8}  kissing={9:0}  osiriState={10}  Syaseing={11}"
                        + "  余韵剩={12}  连榨={13}",
                        _ecDropCount, _lastEc, cur, d,
                        PoseIdx(), _demandMode, _demandModePose, _demandStacks,
                        tab != null ? GetStringField(tab, "sitState") : "?",
                        tab != null ? GetFloat(tab, "kissing") : 0f,
                        tab != null ? GetStringField(tab, "osiriState") : "?",
                        GetFloat(pl, "Syaseing"),
                        _afterglowRemaining, ChainNow()));
                }
                _lastEc = cur;
            }
            catch { }
        }

        private static void TickKissSpeed()
        {
            if (!KissSpeedEnabled.Value) return;
            try
            {
                if (PoseIdx() != 2) return;                       // 束缚之吻是坐姿专属
                object tab = Tabemi();
                if (tab == null) return;
                if (GetStringField(tab, "sitState") == "kyusei") return;   // 榨取中 → 交给那套
                if (GetFloat(tab, "kissing") <= 0.5f) return;              // 不在吻里

                float mul = Mathf.Clamp(KissSpeedMul.Value, 10f, 400f) / 100f;
                float wob = Mathf.Clamp(KissSpeedWobble.Value, 0f, 90f) / 100f;
                if (wob > 0.0001f)
                {
                    float hz = Mathf.Clamp(KissSpeedHz.Value, 0.02f, 4f);
                    mul *= 1f + wob * Mathf.Sin(Time.unscaledTime * hz * Mathf.PI * 2f);
                }
                mul = Mathf.Max(0.05f, mul);

                Type sot = FindType("Live2D_Animation_SitOsiri");
                if (sot == null) return;
                UnityEngine.Object[] objs = Resources.FindObjectsOfTypeAll(sot);
                if (objs == null || objs.Length == 0) return;

                bool applied = false;
                foreach (string mn in new string[] { "SitKissMixerA", "SitKissMixerB", "SitMixerA", "SitMixerB" })
                {
                    object mx = FieldQuiet(sot, mn)?.GetValue(objs[0]);
                    if (mx == null) continue;
                    object st = mx.GetType().GetProperty("State", AllFlags)?.GetValue(mx, null);
                    if (st == null) continue;
                    PropertyInfo ip = st.GetType().GetProperty("IsPlaying", AllFlags);
                    object v = ip != null ? ip.GetValue(st, null) : null;
                    if (!(v is bool b && b)) continue;
                    PropertyInfo sp = st.GetType().GetProperty("EffectiveSpeed", AllFlags);
                    if (sp != null && sp.CanWrite) { sp.SetValue(st, mul, null); applied = true; }
                    break;
                }

                // 【兜底】绝顶 / 吸精那些是直接在层上 Play 的，混合器里找不到 ——
                // 原来只找混合器，所以那些动作"加不上速"。
                if (!applied)
                {
                    foreach (string ln in new string[] { "_CenterGirlLayer", "_SitGirlfaceLayer" })
                    {
                        object layer = FieldQuiet(sot, ln)?.GetValue(objs[0]);
                        if (layer == null) continue;
                        object st2 = layer.GetType().GetProperty("CurrentState", AllFlags)?.GetValue(layer, null);
                        if (st2 == null) continue;
                        PropertyInfo ip2 = st2.GetType().GetProperty("IsPlaying", AllFlags);
                        object v2 = ip2 != null ? ip2.GetValue(st2, null) : null;
                        if (v2 is bool b2 && !b2) continue;
                        PropertyInfo sp2 = st2.GetType().GetProperty("EffectiveSpeed", AllFlags);
                        if (sp2 != null && sp2.CanWrite) { sp2.SetValue(st2, mul, null); break; }
                    }
                }
            }
            catch { }
        }

        private void TickSitKiss()
        {
            if (!SitKissAutoPrepare.Value) return;
            try
            {
                if (PoseIdx() != 2) return;                       // 只在正骑
                object tab = Tabemi();
                if (tab == null) return;
                if (GetStringField(tab, "centerGirlState") != "Sit") return;
                if (GetFloat(tab, "kissing") > 0.5f) { _kissAutoAt = -999f; return; }   // 已经在吻里或准备中
                if (GetFloat(Player(), "Syaseing") > 0.5f) return;                      // 榨取中不插手

                if (_kissAutoAt < 0f) { _kissAutoAt = Time.unscaledTime + Mathf.Max(1f, SitKissAutoInterval.Value); return; }
                if (Time.unscaledTime < _kissAutoAt) return;

                _kissAutoAt = -999f;
                InvokeNoArg(tab, "PrepareKissing");
                Log.LogInfo("[束缚之吻] 自动进入（原版要按 X 或靠 10% 随机）");
            }
            catch { }
        }

        /// <summary>进入后把窗口拉长（原版 4~5 秒，点不了几下头）。</summary>
        private static void Postfix_PrepareKissing(object __instance)
        {
            try
            {
                float mul = Mathf.Clamp(SitKissWindowMul.Value, 50f, 1000f) / 100f;
                if (mul <= 1.001f) return;
                float t = GetFloat(__instance, "拘束キスTimer");
                if (t > 0f) SetField(__instance, "拘束キスTimer", t * mul);
            }
            catch { }
        }

        /// <summary>阻止窗口衰减：原版 KissPrepare解除() 里有 KissPrepareTime *= 0.9。</summary>
        private static void Prefix_KissPrepareKaijo(object __instance)
        {
            if (!SitKissNoDecay.Value) return;
            try { _kissTimeBefore = GetFloat(__instance, "KissPrepareTime"); } catch { }
        }

        private static void Postfix_KissPrepareKaijo(object __instance)
        {
            if (!SitKissNoDecay.Value) return;
            try
            {
                if (_kissTimeBefore > 0f) SetField(__instance, "KissPrepareTime", _kissTimeBefore);
            }
            catch { }
        }

        private static float _kissTimeBefore;

        // =================================================================
        // 坐姿小穴判定区 —— 改为【按人物模型】判定 + 屏幕可视化
        //
        // 原来用的是屏幕百分比矩形（0~1 的绝对位置），人物一移动/缩放就废了。
        // 现在改成相对【模型屏幕包围盒】：
        //   · 包围盒 = Live2D_HitAreaCheck.modelRootA 下所有 drawable 顶点投影到屏幕的 min/max
        //   · 判定区 = 包围盒 × (CX/CY/W/H 四个比例)
        // 于是它跟着人物走，换分辨率也不跑偏。
        // =================================================================

        /// <summary>列出模型里全部 drawable 的名字（可按子串筛选）。</summary>
        private static List<string> AllDrawableNames(string filter)
        {
            var list = new List<string>();
            try
            {
                Type hcT = FindType("Live2D_HitAreaCheck");
                if (hcT == null) return list;
                UnityEngine.Object[] ins = Resources.FindObjectsOfTypeAll(hcT);
                if (ins == null || ins.Length == 0) return list;
                Component comp = ins[0] as Component;
                FieldInfo rf = FieldQuiet(hcT, "modelRootA");
                Transform root = rf != null ? rf.GetValue(comp) as Transform : null;
                if (root == null) return list;
                Component model = root.GetComponent("CubismModel") as Component;
                if (model == null) return list;

                PropertyInfo dp = model.GetType().GetProperty("Drawables", AllFlags);
                Array drawables = dp != null ? dp.GetValue(model, null) as Array : null;
                if (drawables == null) return list;

                string f = string.IsNullOrEmpty(filter) ? null : filter.ToLowerInvariant();
                for (int i = 0; i < drawables.Length; i++)
                {
                    object d = drawables.GetValue(i);
                    if (d == null) continue;
                    string nm = DrawableName(d);
                    if (string.IsNullOrEmpty(nm)) continue;
                    if (f != null && !nm.ToLowerInvariant().Contains(f)) continue;
                    if (!list.Contains(nm)) list.Add(nm);
                }
                list.Sort();
            }
            catch { }
            return list;
        }

        private static float _hitRectDbg;

        /// <summary>
        /// 【HitArea_Manman · 鼠标这条路】
        ///
        /// 重点： 关键：游戏里【鼠标】和【触摸】走的是两条完全不同的路 ——
        ///
        ///   触摸 → Live2D_HitAreaCheck.GetTouchTargetName(pos)   ← 返回值直接派发
        ///   鼠标 → Live2D_HitAreaCheck.mousePointing（【静态字段】）
        ///          由 Update() → HitAreaCheckWindows() 每帧算出来，
        ///          再由 TabemiControl.Mouse0_Down/Hold/Up 读它来派发。
        ///
        /// 所以只挂 GetTouchTargetName 的话，【鼠标点击根本不会经过它】——
        /// 玩家用鼠标玩，Manman 区就等于没接上。
        /// 这里补上另一条：每帧算完真实命中之后，若鼠标落在 Manman 区里，
        /// 就把静态字段改成 HitArea_Head_Sit。
        ///
        /// 只在【真实命中为空】时才抢 —— 不跟模型里真正的点按区争。
        /// </summary>
        private static bool _manmanWasInside;

        private static void Postfix_HitAreaCheckWindows()
        {
            if (!SitPussyAreaEnabled.Value) return;
            try
            {
                if (PoseIdx() != 2) return;                 // 只在正骑
                Type hcT = FindType("Live2D_HitAreaCheck");
                if (hcT == null) return;

                FieldInfo pf = FieldQuiet(hcT, "mousePointing");
                if (pf == null) return;
                string cur = pf.GetValue(null) as string;
                if (!string.IsNullOrEmpty(cur)) return;     // 已经命中真的点按区，不抢

                float x0, y0, x1, y1;
                if (!PussyAreaScreenRect(out x0, out y0, out x1, out y1)) return;

                Vector3 mp = Input.mousePosition;
                bool inside = (mp.x >= x0 && mp.x <= x1 && mp.y >= y0 && mp.y <= y1);
                if (inside && !_manmanWasInside)
                {
                    Log.LogInfo(string.Format(
                        "[HitArea_Manman_By_Plugins] 鼠标进入判定区（屏幕 {0:0},{1:0}；矩形 {2:0}~{3:0} × {4:0}~{5:0}）",
                        mp.x, mp.y, x0, x1, y0, y1));
                }
                _manmanWasInside = inside;
                if (inside) pf.SetValue(null, ManmanAreaName);   // 游戏 switch 里没有这一档 → 它什么都不做，由我们自己处理
            }
            catch { }
        }

        /// <summary>
        /// 【HitArea_Manman 自己的点击处理】
        ///
        /// 重点： 为什么不能用 HitArea_Head_Sit：
        ///   坐姿的头部点击（頭叩かれるSit）**是解除束缚之吻的** ——
        ///     kissing == 2 → SkillCheck.GetReadyForSkillCheck()   ← 技能检定 / 解吻
        ///     kissing == 1 → SitGirlKiss叩く量++                    ← 累积解除量
        ///   让 Manman 冒充它，等于"点小穴会去解吻"，功能就串了。
        ///
        /// 所以 Manman 走**独立的一条**：
        ///   · 静态 mousePointing 被我们改成 ManmanAreaName
        ///   · 游戏 Mouse_Down_0 的 switch 里【没有】这一档 → 它什么都不做
        ///   · 由这个前缀接住：累积榨取欲 + 放点击特效，然后 return false 跳过 switch
        ///
        /// 这样两件事彻底分开：**头 = 解吻，小穴 = 攒榨取欲**。
        /// </summary>
        private static bool Prefix_Mouse_Down_0(object __instance)
        {
            try
            {
                if (!SitPussyAreaEnabled.Value) return true;
                Type hcT = FindType("Live2D_HitAreaCheck");
                if (hcT == null) return true;
                FieldInfo pf = FieldQuiet(hcT, "mousePointing");
                if (pf == null) return true;
                string mpNow = pf.GetValue(null) as string;
                if (mpNow != ManmanAreaName) return true;

                Log.LogInfo("[HitArea_Manman_By_Plugins] 命中！姿势=" + PoseIdx()
                            + "  榨取欲=" + _demandUrge.ToString("0.#")
                            + "  段=" + _demandStacks);

                // 点击特效（粒子 + 那声"啪"）—— 复用游戏自己的实现，手感一致
                try
                {
                    object tab = Tabemi();
                    if (tab != null)
                    {
                        FieldInfo cf = FieldQuiet(tab.GetType(), "clickEffect");
                        object ce = cf != null ? cf.GetValue(tab) : null;
                        if (ce != null)
                        {
                            MethodInfo m = ce.GetType().GetMethod("頭叩くエフェクト", AllFlags);
                            if (m != null) m.Invoke(ce, null);
                        }
                    }
                }
                catch { }

                // 【速度冲量】与打屁股完全同一套（SpankSpeed* 那组，已按姿势三份，
                // 坐姿读到的就是 _Sit 那份）。施加点在 TickSpankSpeed 里，
                // 现在会按姿势找对混合器（OsiriMixer / SitMixerA / SitMixerB）。
                NoteSpank();

                // 累积榨取欲 + 叠层判定（与"坐姿点击"同一个来源）
                object tb = Tabemi();
                if (tb != null && SitClickAlwaysAccumulate.Value)
                {
                    string st = GetStringField(tb, "sitState");
                    if (st == "attacking")      // 只在坐姿常态累积，榨取/射精中不刷
                        AccumulateAndMaybeStack(ManmanAreaName, false, tb, "SitGirlKiss解除叩く量");
                }

                return false;      // 已接管，跳过游戏那个 switch
            }
            catch { return true; }
        }

        /// <summary>某个 drawable 的屏幕包围盒（按名字找）。</summary>
        private static bool TryGetDrawableScreenRect(string drawableName, out float x0, out float y0, out float x1, out float y1)
        {
            x0 = y0 = x1 = y1 = 0f;
            try
            {
                Type hcT = FindType("Live2D_HitAreaCheck");
                if (hcT == null) return false;
                UnityEngine.Object[] ins = Resources.FindObjectsOfTypeAll(hcT);
                if (ins == null || ins.Length == 0) return false;
                Component comp = ins[0] as Component;
                if (comp == null) return false;

                FieldInfo rf = FieldQuiet(hcT, "modelRootA");
                Transform root = rf != null ? rf.GetValue(comp) as Transform : null;
                if (root == null) return false;
                Camera cam = Camera.main;
                if (cam == null) return false;
                Component model = root.GetComponent("CubismModel") as Component;
                if (model == null) return false;

                PropertyInfo dp = model.GetType().GetProperty("Drawables", AllFlags);
                Array drawables = dp != null ? dp.GetValue(model, null) as Array : null;
                if (drawables == null) return false;

                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                int used = 0;
                for (int i = 0; i < drawables.Length; i++)
                {
                    object d = drawables.GetValue(i);
                    if (d == null) continue;
                    string nm = DrawableName(d);
                    if (nm == null || nm != drawableName) continue;

                    PropertyInfo vp = d.GetType().GetProperty("VertexPositions", AllFlags);
                    Vector3[] vs = vp != null ? vp.GetValue(d, null) as Vector3[] : null;
                    if (vs == null) continue;
                    for (int k = 0; k < vs.Length; k++)
                    {
                        Vector3 sp = cam.WorldToScreenPoint(root.TransformPoint(vs[k]));
                        if (sp.z < 0f) continue;
                        if (sp.x < minX) minX = sp.x;
                        if (sp.x > maxX) maxX = sp.x;
                        if (sp.y < minY) minY = sp.y;
                        if (sp.y > maxY) maxY = sp.y;
                        used++;
                    }
                    break;
                }
                if (used < 3) return false;
                x0 = minX; y0 = minY; x1 = maxX; y1 = maxY;
                return true;
            }
            catch { return false; }
        }

        /// <summary>取 drawable 的名字（不同 Cubism 版本字段名不一致，逐个试）。</summary>
        private static string DrawableName(object d)
        {
            foreach (string n in new string[] { "Name", "name", "Id" })
            {
                try
                {
                    PropertyInfo pi = d.GetType().GetProperty(n, AllFlags);
                    if (pi != null)
                    {
                        object v = pi.GetValue(d, null);
                        if (v != null) return v.ToString();
                    }
                    FieldInfo fi = FieldQuiet(d.GetType(), n);
                    if (fi != null)
                    {
                        object v = fi.GetValue(d);
                        if (v != null) return v.ToString();
                    }
                }
                catch { }
            }
            return null;
        }

        /// <summary>模型里所有以 HitArea 开头的 drawable 名字。</summary>
        // 【性能】drawable 名单与逐个包围盒的缓存 —— 它们在 OnGUI 里每帧被调 2~3 次，
        // 每次都要 FindObjectsOfTypeAll + 遍历全部 drawable。这里给个短 TTL。
        private static List<string> _namesCache;
        private static float _namesAt = -999f;

        private static List<string> HitAreaDrawableNamesCached()
        {
            if (_namesCache != null && Time.unscaledTime - _namesAt < 0.5f) return _namesCache;
            _namesCache = HitAreaDrawableNames();
            _namesAt = Time.unscaledTime;
            return _namesCache;
        }

        private static List<string> HitAreaDrawableNames()
        {
            var list = new List<string>();
            try
            {
                Type hcT = FindType("Live2D_HitAreaCheck");
                if (hcT == null) return list;
                UnityEngine.Object[] ins = Resources.FindObjectsOfTypeAll(hcT);
                if (ins == null || ins.Length == 0) return list;
                Component comp = ins[0] as Component;
                FieldInfo rf = FieldQuiet(hcT, "modelRootA");
                Transform root = rf != null ? rf.GetValue(comp) as Transform : null;
                if (root == null) return list;
                Component model = root.GetComponent("CubismModel") as Component;
                if (model == null) return list;

                PropertyInfo dp = model.GetType().GetProperty("Drawables", AllFlags);
                Array drawables = dp != null ? dp.GetValue(model, null) as Array : null;
                if (drawables == null) return list;

                for (int i = 0; i < drawables.Length; i++)
                {
                    object d = drawables.GetValue(i);
                    if (d == null) continue;
                    string nm = DrawableName(d);
                    if (string.IsNullOrEmpty(nm)) continue;
                    if (nm.StartsWith("HitArea") && !list.Contains(nm)) list.Add(nm);
                }
            }
            catch { }
            return list;
        }

        // 【性能】组件查找与模型包围盒的缓存。
        // 游戏变卡的根源：Resources.FindObjectsOfTypeAll 每次扫全部已加载对象，
        // 再加上遍历 CubismModel 全部 drawable 的顶点做坐标变换 —— 而这些在
        // Postfix_HitAreaCheckWindows（每帧）与 OnGUI 里被反复调用。
        private static Component _hcComp;
        private static float _hcCompAt = -999f;
        private static float _mrX0, _mrY0, _mrX1, _mrY1, _mrAt = -999f;
        private static bool _mrOk;

        private static Component HitAreaCompCached()
        {
            if (_hcComp != null && Time.unscaledTime - _hcCompAt < 1f) return _hcComp;
            try
            {
                Type t = FindType("Live2D_HitAreaCheck");
                if (t != null)
                {
                    UnityEngine.Object[] a = Resources.FindObjectsOfTypeAll(t);
                    _hcComp = (a != null && a.Length > 0) ? a[0] as Component : null;
                    _hcCompAt = Time.unscaledTime;
                }
            }
            catch { }
            return _hcComp;
        }

        private static bool ModelRectCached(out float x0, out float y0, out float x1, out float y1)
        {
            x0 = _mrX0; y0 = _mrY0; x1 = _mrX1; y1 = _mrY1;
            if (_mrOk && Time.unscaledTime - _mrAt < 0.1f) return true;
            bool ok = TryGetModelScreenRect(out x0, out y0, out x1, out y1);
            if (ok) { _mrX0 = x0; _mrY0 = y0; _mrX1 = x1; _mrY1 = y1; _mrOk = true; _mrAt = Time.unscaledTime; }
            else _mrOk = false;
            return ok;
        }

        private static bool TryGetModelScreenRect(out float xMin, out float yMin, out float xMax, out float yMax)
        {
            xMin = yMin = xMax = yMax = 0f;
            try
            {
                Type hcT = FindType("Live2D_HitAreaCheck");
                if (hcT == null) return false;
                Component comp = HitAreaCompCached();
                if (comp == null) return false;

                FieldInfo rf = FieldQuiet(hcT, "modelRootA");
                Transform root = rf != null ? rf.GetValue(comp) as Transform : null;
                if (root == null) return false;

                Camera cam = Camera.main;
                if (cam == null) return false;

                Component model = root.GetComponent("CubismModel") as Component;
                if (model == null) return false;

                PropertyInfo dp = model.GetType().GetProperty("Drawables", AllFlags);
                Array drawables = dp != null ? dp.GetValue(model, null) as Array : null;
                if (drawables == null || drawables.Length == 0) return false;

                float minX = float.MaxValue, minY = float.MaxValue;
                float maxX = float.MinValue, maxY = float.MinValue;
                int used = 0;

                for (int i = 0; i < drawables.Length; i++)
                {
                    object d = drawables.GetValue(i);
                    if (d == null) continue;
                    PropertyInfo vp = d.GetType().GetProperty("VertexPositions", AllFlags);
                    Vector3[] vs = vp != null ? vp.GetValue(d, null) as Vector3[] : null;
                    if (vs == null) continue;
                    for (int k = 0; k < vs.Length; k++)
                    {
                        Vector3 w = root.TransformPoint(vs[k]);
                        Vector3 sp = cam.WorldToScreenPoint(w);
                        if (sp.z < 0f) continue;
                        if (sp.x < minX) minX = sp.x;
                        if (sp.x > maxX) maxX = sp.x;
                        if (sp.y < minY) minY = sp.y;
                        if (sp.y > maxY) maxY = sp.y;
                        used++;
                    }
                }
                if (used < 3 || maxX <= minX || maxY <= minY) return false;
                xMin = minX; yMin = minY; xMax = maxX; yMax = maxY;
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// 把一个屏幕坐标换算成【模型相对比例】(0~1)。
        ///
        /// 【为什么必须换算】判定区已经改成按模型包围盒算，
        /// 而"设为鼠标位置"原来直接写的是屏幕比例 —— 两者不在同一套坐标里，
        /// 结果就是按钮能点、但位置完全不对。
        /// </summary>
        private static bool ScreenToModelRel(Vector3 screenPos, out float rx, out float ry)
        {
            rx = ry = 0.5f;
            float mx0, my0, mx1, my1;
            if (!ModelRectCached(out mx0, out my0, out mx1, out my1)) return false;
            float mw = mx1 - mx0, mh = my1 - my0;
            if (mw < 1f || mh < 1f) return false;
            rx = Mathf.Clamp01((screenPos.x - mx0) / mw);
            ry = Mathf.Clamp01((screenPos.y - my0) / mh);
            return true;
        }

        /// <summary>拖动模式：每帧把判定区中心跟到鼠标上。</summary>
        private void TickManmanDrag()
        {
            if (!ManmanDragMode.Value) return;
            try
            {
                float rx, ry;
                if (!ScreenToModelRel(Input.mousePosition, out rx, out ry)) return;
                SitPussyAreaCX.Value = rx;
                SitPussyAreaCY.Value = ry;
            }
            catch { }
        }

        /// <summary>判定区在屏幕上的实际矩形（左下为原点）。</summary>
        private static bool PussyAreaScreenRect(out float x0, out float y0, out float x1, out float y1)
        {
            x0 = y0 = x1 = y1 = 0f;

            // 【模型里已经有一块 HitArea_Manman 的话，就直接用它】
            //   否则会出现【两个 Manman 区】：模型那块 + 插件自建的矩形，框重叠在一起。
            //   用户已经/打算自己建一块，所以这里优先认模型里的。
            try
            {
                if (TryGetDrawableScreenRect("HitArea_Manman", out x0, out y0, out x1, out y1))
                    return true;
            }
            catch { }

            // 【其次看素材绑定】填了 drawable 名就直接用它的包围盒 ——
            // 这是真正的"素材与模型绑定"：跟着网格形变走，人物怎么动都贴合。
            try
            {
                string bind = ManmanBindDrawable.Value;
                if (!string.IsNullOrEmpty(bind) && TryGetDrawableScreenRect(bind, out x0, out y0, out x1, out y1))
                    return true;
            }
            catch { }

            float mx0, my0, mx1, my1;
            if (!ModelRectCached(out mx0, out my0, out mx1, out my1)) return false;
            float mw = mx1 - mx0, mh = my1 - my0;
            
            // (1) 退化检测
            // 模型不在场时 ModelRectCached 可能给出 0 或负的宽高，推出来的矩形是垃圾值。
            if (mw < 1f || mh < 1f) return false;
            
            float cx = mx0 + SitPussyAreaCX.Value * mw;
            float cy = my0 + SitPussyAreaCY.Value * mh;
            float hw = SitPussyAreaW.Value * mw * 0.5f;
            float hh = SitPussyAreaH.Value * mh * 0.5f;
            x0 = cx - hw; y0 = cy - hh; x1 = cx + hw; y1 = cy + hh;
            
            // (2) 与屏幕求交
            // 实测出现过 y 为负、整块跑到屏幕外：矩形 754~1194 x -114~82。
            // 那种矩形"取得出来"，但一个像素都点不到 ——
            // 而不变量当时只检查"矩形可取"，所以放过了它。
            float sx0 = Mathf.Max(x0, 0f), sy0 = Mathf.Max(y0, 0f);
            float sx1 = Mathf.Min(x1, Screen.width), sy1 = Mathf.Min(y1, Screen.height);
            if (sx1 - sx0 < 4f || sy1 - sy0 < 4f) return false;   // 屏幕内不足 4px，等于不可用
            
            x0 = sx0; y0 = sy0; x1 = sx1; y1 = sy1;
            return true;
        }

        /// <summary>
        /// 【坐姿的小穴点按区】
        ///
        /// 游戏的 Live2D 射线命中的是 drawable 名（HitArea_Head_Sit / HitArea_Osiri …），
        /// 派发处 switch 那些名字。模型里【没有】小穴那一块
        /// （代码里那句 `_ = hitAreaUp == "HitArea_Manman"` 是残留，assets 里搜不到该名字），
        /// 所以这里自己判一个矩形，命中就【冒充 HitArea_Head_Sit】——
        /// 于是走的是既有的點頭部逻辑，会累积榨取欲，不需要改任何资源。
        ///
        /// 只在坐姿生效；其余姿势的点击一律不拦。
        /// </summary>
        private static void Postfix_GetTouchTargetName(Vector3 touchingPosition, ref string __result)
        {
            try
            {
                if (!SitPussyAreaEnabled.Value) return;
                if (string.IsNullOrEmpty(__result) || __result == "HitArea_Head_Sit") return;
                if (PoseIdx() != 2) return;                 // 只在正骑（坐姿）

                // touchingPosition 是屏幕坐标（左下为原点），与 Input.mousePosition 同系
                // 按【模型包围盒】算，而不是屏幕百分比 —— 人物移动/缩放都跟得住
                float x0, y0, x1, y1;
                if (!PussyAreaScreenRect(out x0, out y0, out x1, out y1)) return;

                if (touchingPosition.x >= x0 && touchingPosition.x <= x1
                    && touchingPosition.y >= y0 && touchingPosition.y <= y1)
                    __result = ManmanAreaName;   // 同上：不走游戏那套，独立成一条
            }
            catch { }
        }

        /// <summary>点头部一次 → 走与打屁股相同的榨取模式逻辑（脱出惩罚改指 Sit 的字段）。</summary>
        private static void Postfix_SitHits(object __instance)
        {
            try
            {
                if (_preSitHits < 0 || !P3(DemandEnabled3)) return;
                int now;
                try { now = (int)GetFloat(__instance, "SitGirlKiss叩く量"); }
                catch { return; }
                if (now > _preSitHits)
                {
                    // 计数真的增加了（kissing == 1 那个窗口）→ 走完整逻辑，含脱出惩罚
                    DoSpankLogic(__instance, "SitGirlKiss解除叩く量");
                    return;
                }

                // 【修 #6】计数没增加 —— 也就是不在那个 4~5 秒的束缚之吻窗口里。
                // 原版这时点击什么都不做，导致坐姿几乎没有可用的手动累积手段
                // （背面骑乘有「打屁股」这个大目标，坐姿没有对等物）。
                // 这里补一次"只累积 + 叠层判定"，**不动游戏的 SitGirlKiss叩く量**，
                // 所以脱出机制与原版完全一致，只是让榨取欲能靠点击攒起来。
                if (!SitClickAlwaysAccumulate.Value) return;
                string st = GetStringField(__instance, "sitState");
                if (st != "attacking") return;      // 只在坐姿常态累积，榨取/射精中不刷
                AccumulateAndMaybeStack("坐姿点击", false, __instance, "SitGirlKiss解除叩く量");
            }
            catch { }
        }

        /// <summary>Sit 常态攻击事件 → 低概率涨索取欲（与 Osiri 同一条逻辑）。</summary>
        private static void Postfix_SitGirlMainMixer()
        {
            NoteDamageActivity();
            try
            {
                if (!P3(DemandEnabled3)) return;
                // 【模式中也要累积】——每层门槛已经管住节奏，再拦这一道只会让玩家只能靠打屁股攒。
                if (P3(AttackUrgeChance3) <= 0f || P3(AttackUrgeGain3) <= 0f) return;

                float j = Mathf.Clamp(P3(AttackUrgeJitter3), 0f, 90f) / 100f;
                float chance = Mathf.Clamp(P3(AttackUrgeChance3) * (1f + UnityEngine.Random.Range(-j, j)), 0f, 100f);
                if (UnityEngine.Random.value * 100f >= chance) return;
                float gain = Mathf.Max(0f, P3(AttackUrgeGain3) * (1f + UnityEngine.Random.Range(-j, j))) * UrgeScaleNow();
                _demandUrge = Mathf.Max(0f, _demandUrge + gain);   // 不设上限：叠层时归零，本身就自限
                // 【修 #1】攻击累积完也过一遍叠层判定 —— 原先它只填进度条、从不消费。
                if (P3(AttackCanStackDemand3)) TryStackNow("角色攻击");
            }
            catch { }
        }

        // ---- Sit 射精：同样不减速 ----
        // 游戏在 Event_SitSyaseiStart() 与 Event_SitKissSyaseiStart() 里都调
        //     SetSpeed_Sit(sitSpeed / 2f);
        // 注意 Sit 是【真的减半】（Osiri 只是停加速），所以这两支都必须覆盖。
        private static float _preSitSpeed;
        private static bool _preSitSpeedValid;

        private static void Prefix_SitSyaseiStart(object __instance)
        {
            try
            {
                _preSitSpeed = GetFloat(__instance, "sitSpeed");
                _preSitSpeedValid = true;
            }
            catch { _preSitSpeedValid = false; }
        }

        private static void Postfix_SitSyaseiStart(object __instance)
        {
            NoteUrgeEvent("坐姿射精", SyaseiUrgeChance, SyaseiUrgeGain);
            if (!_preSitSpeedValid) return;
            // NoteDemandSyasei 在下面按 _demandMode 自己判断；这里只保证 Sit 两个 Start 不重复计
            if (!_demandMode) return;
            try
            {
                SetField(__instance, "sitSpeed", _preSitSpeed);
                SetField(__instance, "isSpeedUpSit", true);
                Log.LogInfo("[榨取] Sit 射精：不减速（sitSpeed=" + _preSitSpeed.ToString("0.###") + "）");
                NoteDemandSyasei("坐姿");
            }
            catch { }
        }

        /// <summary>
        /// 【修 #8】坐姿射精收尾：给一条不经过束缚之吻的榨取入口。
        ///
        /// 原版：
        ///     private void Event_SitSyasei_OnEnd()
        ///     {
        ///         player.Syaseing = false;
        ///         Play_Sit();          // ← 永远回坐姿攻击，永不进榨取
        ///     }
        ///
        /// 而 Sit 唯一的榨取入口 Play_SitKiss吸精() 会把角色强制置为 kissing = 2
        /// （束缚之吻），玩家实际玩下来几乎到不了那个状态 ——
        /// 于是榨取 → 连榨 → 余韵整条链都摸不到（KNOWN-ISSUES 第 8 条，133 个
        /// Sit 样本的子状态全是 attacking，无一进过 kyusei）。
        ///
        /// 这里按 SitSyaseiToDrainChance 给它一个进榨取的机会。0 = 保持原版行为。
        /// </summary>
        private static bool Prefix_Event_SitSyasei_OnEnd(object __instance)
        {
            try
            {
                object player = Player();
                if (player != null) SetField(player, "Syaseing", false);

                float ch = Mathf.Clamp(SitSyaseiToDrainChance.Value, 0f, 100f);
                if (ch > 0f && UnityEngine.Random.value * 100f < ch)
                {
                    Log.LogInfo("[榨取] 坐姿射精收尾 → 按 " + ch.ToString("0.#") + "% 进入榨取（不经过束缚之吻）");
                    InvokeAt(ChainSitInstance(), "Play_SitKiss吸精", true);
                    return false;
                }
                InvokeAt(ChainSitInstance(), "Play_Sit");
                return false;
            }
            catch (Exception e)
            {
                Log.LogWarning("[榨取] 坐姿射精收尾接管失败: " + e.Message);
                return true;
            }
        }

        /// <summary>拿到 Live2D_Animation_SitOsiri 的实例（Sit 与 Osiri 在同一个类里）。</summary>
        private static object ChainSitInstance()
        {
            try
            {
                Type t = FindType("Live2D_Animation_SitOsiri");
                UnityEngine.Object[] objs = t != null ? Resources.FindObjectsOfTypeAll(t) : null;
                if (objs != null && objs.Length > 0) return objs[0];
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Sit 吸精收尾：游戏原码两边都重播（永远连榨），这里接管以套用连榨上限。
        /// </summary>
        // =================================================================
        // 束缚之吻：把「射精」那一半也桥接上来
        //
        // 【为什么】原来只接了「吸精」那条（Even_SitKiss吸精OnEnd），
        // 而角色在束缚之吻里【射精】走的是另一条路（Event_SitKissSyaseiOnEnd）——
        // 于是连榨上限管不到它、绝顶动画拿不到倍速、这次伤害也不计入回归累积。
        // 游戏侧原码：
        //     player.Syaseing = false;
        //     if (GameManager.gameover)  Play_SitKiss吸精(true);
        //     else if (Random.value < 0.5f) Play_SitKiss();          // 回接吻循环
        //     else                       Play_SitKiss吸精(true);    // 继续榨
        // =================================================================

        /// <summary>束缚之吻 · 射精收尾 → 接管连榨上限（与吸精那条同款）。</summary>
        private static bool Prefix_SitKissSyaseiOnEnd(object __instance)
        {
            try
            {
                object player = Player();
                if (player != null) SetField(player, "Syaseing", false);   // 原版第一句

                bool gameover = false;
                try
                {
                    Type gm = FindType("GameManager");
                    FieldInfo gf = gm != null ? FieldQuiet(gm, "gameover") : null;
                    object gv = gf != null ? gf.GetValue(null) : null;
                    gameover = gv is bool gb && gb;
                }
                catch { }

                bool repeat;
                if (gameover) repeat = true;
                else if (_afterglowRemaining > 0 && !_demandMode)
                {
                    repeat = true;
                    _afterglowRemaining--;
                    ShowChainBanner(string.Format("余韵 · 剩余 {0} 次", _afterglowRemaining));
                }
                else
                {
                    repeat = true;      // 原版这里是 50% 概率，取"继续"那一支再套上限
                    if (ChainLimitNow() > 0 && ChainNow() >= ChainLimitNow())
                    {
                        repeat = false;
                        Log.LogInfo("[射精] 已达连榨上限 " + ChainLimitNow() + " 次（Sit·束缚之吻），强制收尾");
                    }
                    if (repeat) ChainSet(ChainNow() + 1); else ChainSet(0);
                }

                if (repeat)
                {
                    int left = ChainLimitNow() > 0 ? Mathf.Max(0, ChainLimitNow() - ChainNow()) : -1;
                    ShowChainBanner(_afterglowRemaining > 0
                        ? string.Format("余韵 · 剩余 {0} 次", _afterglowRemaining)
                        : (left >= 0 ? string.Format("连榨 · 剩余 {0} 次", left) : "连榨"));
                    InvokeAt(__instance, "Play_SitKiss吸精", true);
                }
                else
                {
                    InvokeAt(__instance, "Play_SitKiss");   // 不再榨 → 回接吻循环
                }
                return false;
            }
            catch (Exception e)
            {
                Log.LogWarning("[射精] 束缚之吻射精收尾接管失败: " + e.Message);
                return true;
            }
        }

        /// <summary>束缚之吻 · 绝顶动画 → 记住它在播，好让倍速那套作用到它。</summary>
        private static void Postfix_PlaySitKissSyasei()
        {
            _kissSyaseiAt = Time.unscaledTime;
        }

        private static float _kissSyaseiAt = -999f;
        internal static bool KissSyaseiPlaying() { return Time.unscaledTime - _kissSyaseiAt < 0.5f; }

        /// <summary>束缚之吻 · 伤害 → 计较进「回归累积」（与口交那条同款）。</summary>
        private static void Postfix_DealDamage_SitKiss()
        {
            NoteDamageActivity();
        }

        private static bool Prefix_EvenSitKissKyuseiOnEnd(object __instance)
        {
            try
            {
                object player = Player();
                bool gameover = false;
                try
                {
                    Type gm = FindType("GameManager");
                    FieldInfo gf = gm != null ? FieldQuiet(gm, "gameover") : null;
                    object gv = gf != null ? gf.GetValue(null) : null;
                    gameover = gv is bool gb && gb;
                }
                catch { }

                bool repeat;
                if (gameover) repeat = true;
                else if (_afterglowRemaining > 0 && !_demandMode)
                {
                    repeat = true;
                    _afterglowRemaining--;
                    ShowChainBanner(string.Format("余韵 · 剩余 {0} 次", _afterglowRemaining));
                }
                else
                {
                    repeat = true;      // 原版永远重播
                    if (ChainLimitNow() > 0 && ChainNow() >= ChainLimitNow())
                    {
                        repeat = false;
                        Log.LogInfo("[吸精] 已达连榨上限 " + ChainLimitNow() + " 次（Sit·"
                                    + (_demandMode ? "榨取模式" : "常规") + "），强制收尾");
                    }
                    if (repeat) ChainSet(ChainNow() + 1); else ChainSet(0);
                }

                if (repeat)
                {
                    int left = ChainLimitNow() > 0 ? Mathf.Max(0, ChainLimitNow() - ChainNow()) : -1;
                    ShowChainBanner(_afterglowRemaining > 0
                        ? string.Format("余韵 · 剩余 {0} 次", _afterglowRemaining)
                        : (left >= 0 ? string.Format("连榨 · 剩余 {0} 次", left) : "连榨"));
                    InvokeAt(__instance, "Play_SitKiss吸精", true);
                }
                else
                {
                    // 不再重播 → 回到 Sit 攻击（Sit 没有"回口交"这条路）
                    InvokeAt(__instance, "Play_SitKiss");
                }
                return false;
            }
            catch (Exception e)
            {
                Log.LogWarning("[榨取] Sit 吸精收尾接管失败: " + e.Message);
                return true;
            }
        }

        // =================================================================
        // 每次榨取的伤害事件 → 按对生命值的影响折算绝顶值
        //
        // 挂在【伤害事件】而不是收尾事件上，是为了与"这一次榨取打了多少伤害"一一对应。
        // 收尾事件还可能走别的分支（回攻击 / 脱出），那样就没有对应的伤害。
        // =================================================================

        /// <summary>骑乘位榨取：Osiri吸精ダメージ</summary>
        private static void Postfix_Osiri吸精Damage(object __instance)
        {
            NoteUrgeEvent("骑乘位吸精", KyuseiUrgeChance, KyuseiUrgeGain);
            NoteDamageActivity();
            try { AddChainGainByHp(Player(), GetFloat(__instance, "Osiri吸精ダメージ")); }
            catch { }
        }

        /// <summary>Sit 榨取：SitKiss吸精ダメージ + HP 槽百分比（原版是 Random(3,5)，取中值 4）</summary>
        private static void Postfix_SitKiss吸精1(object __instance)
        {
            NoteUrgeEvent("坐姿吸精", KyuseiUrgeChance, KyuseiUrgeGain);
            NoteDamageActivity();
            try
            {
                object player = Player();
                float dmg = GetFloat(__instance, "SitKiss吸精ダメージ");
                float maxHp = player != null ? GetFloat(player, "maxHp") : 0f;
                if (maxHp <= 0.01f) maxHp = 200f;
                // HP 槽那边是"百分比"口径，直接加；伤害那边是点数，换算成百分比
                float pctDamage = 4f + dmg / maxHp * 100f;
                float ratio = Mathf.Clamp(P3(ChainGainPerHp3), 0f, 2000f);
                if (ratio <= 0.0001f) { AddChainGainFixed(player, 0f); return; }
                AddChainGainFixed(player, pctDamage * ratio / 100f);
            }
            catch { }
        }

        /// <summary>
        /// 口交榨取：Event_Fella吸精()（注意方法名不带 Damage）。
        /// 原码是 player.HPChange((0f - 吸精ダメージ) * fellaKyuseiAttRate / 100f)
        /// —— 【按吸精攻击率缩放】，所以这里也必须把那个缩放算进去，否则低攻击率时高估。
        /// </summary>
        private static void Postfix_Fella吸精(object __instance)
        {
            NoteUrgeEvent("口交吸精", KyuseiUrgeChance, KyuseiUrgeGain);
            NoteDamageActivity();
            try
            {
                float dmg = GetFloat(__instance, "吸精ダメージ");
                float rate = GetFloat(__instance, "fellaKyuseiAttRate");
                if (rate <= 0f) rate = 100f;
                float total = dmg * rate / 100f;
                object player = Player();
                float maxHp = player != null ? GetFloat(player, "maxHp") : 0f;
                AddChainGainByHp2(player, total, dmg, maxHp);
            }
            catch { }
        }

        /// <summary>带已知最大生命值的版本（避免重复取）。</summary>
        private static void AddChainGainByHp2(object player, float totalDamage, float rawDamage, float maxHp)
        {
            try
            {
                if (!P3(ChainEcstasyGain3) || player == null) return;
                float ratio = Mathf.Clamp(P3(ChainGainPerHp3), 0f, 2000f);
                if (ratio <= 0.0001f) { AddChainGainFixed(player, 0f); return; }
                if (maxHp <= 0.01f) maxHp = 200f;
                AddChainGainFixed(player, totalDamage / maxHp * 100f * ratio / 100f);
            }
            catch { }
        }

        /// <summary>
        /// 索取欲审计：把每一次变动的【来源】与【增量】打出来。
        /// 与其靠推理猜"这七十多从哪来"，不如让每次变动自己报出来。
        /// 用 `urgeaudit 60` 开启。
        /// </summary>
        /// <summary>
        /// 射精 / 吸精 / 连榨 也累积索取欲。
        ///
        /// 与"常态攻击"分开一套参数：这些事件比一次普通攻击重得多，
        /// 而且连榨一轮有多次，单次给小一点才不会被一次连榨直接顶满。
        /// 余韵发动时走的就是连榨链，所以它同样会累积 —— 不需要单独挂点。
        /// </summary>
        private static void NoteUrgeEvent(string src, ConfigEntry<float> chanceCfg, ConfigEntry<float> gainCfg)
        {
            try
            {
                if (!P3(DemandEnabled3)) return;
                if (chanceCfg.Value <= 0f || gainCfg.Value <= 0f) return;

                // 与攻击那边同一套【滑块=平均值】的约定：期望值保持为 1 的系数
                float j = Mathf.Clamp(P3(AttackUrgeJitter3), 0f, 90f) / 100f;
                float chance = Mathf.Clamp(chanceCfg.Value * (1f + UnityEngine.Random.Range(-j, j)), 0f, 100f);
                if (UnityEngine.Random.value * 100f >= chance) return;

                float gain = Mathf.Max(0f, gainCfg.Value * (1f + UnityEngine.Random.Range(-j, j))) * UrgeScaleNow();
                float beforeE = _demandUrge;
                _demandUrge = Mathf.Max(0f, _demandUrge + gain);   // 不设上限：叠层时归零，本身就自限
                AuditUrge(src + "(命中 " + gain.ToString("0.##") + ")", beforeE, _demandUrge);

                // 【叠层判定】射精 / 吸精·连榨（以及余韵，它走同一条链）也要能叠层。
                // 原先这里只累积、不判定 —— 于是这三个来源攒起来的索取欲从不消费，
                // 和修 #1 之前"攻击只填进度条"是一模一样的毛病。
                // 这条同时也覆盖了【口交·吸取】：口交的射精/吸精走的就是这个函数。
                TryStackNow(src);
            }
            catch { }
        }

        private static void AuditUrge(string src, float before, float after)
        {
            if (_urgeAuditLeft <= 0) return;
            _urgeAuditLeft--;
            float d = after - before;
            Log.LogInfo("[索取欲] " + src + " " + (d >= 0f ? "+" : "") + d.ToString("0.##")
                        + "  → " + after.ToString("0.##") + "%"
                        + (_urgeAuditLeft == 0 ? "（审计结束）" : ""));
        }

        /// 一次打屁股的完整效果。真实点击与测试命令共用这一份逻辑。
        ///   (1) 速度冲量（快速升、缓慢消、递减叠加）
        ///   (2) 累积索取欲（每次随机加 下限~上限）
        ///   (3) 有概率把"脱出所需点击次数"乘上一截（打屁股让脱出变难）
        ///   (4) 以当前索取欲为概率掷骰：进入索取模式，或叠加一段总时长
        /// </summary>
        private static void DoSpankLogic(object tab) { DoSpankLogic(tab, "Osiri解除叩く量"); }

        private static void DoSpankLogic(object tab, string escapeField)
        {
            // 计数真的增加了 → 走完整逻辑（含脱出惩罚）
            AccumulateAndMaybeStack("打屁股", true, tab, escapeField);
        }

        /// <summary>
        /// 余韵期间索取欲增长的缩放。
        ///
        /// 手动余韵可以当"强制退出索取模式"的逃生阀，但余韵本身会连榨十几次、
        /// 每次都涨索取欲 —— 涨太满就会【余韵一结束马上又进索取模式】，
        /// 于是坐姿入口又被堵回去。这里统一给所有累积来源乘一个系数。
        /// </summary>
        private static float UrgeScaleNow()
        {
            if (_afterglowRemaining > 0 && !_demandMode)
                return Mathf.Clamp(P3(AfterglowUrgeScale3), 0f, 100f) / 100f;
            return 1f;
        }

        /// <summary>累积一次索取欲（不设上限：叠层时归零，本身就自限）。</summary>
        private static void AccumulateUrge(string source)
        {
            float lo = Mathf.Min(P3(DemandUrgeMin3), P3(DemandUrgeMax3));
            float hi = Mathf.Max(P3(DemandUrgeMin3), P3(DemandUrgeMax3));
            float roll0 = UnityEngine.Random.Range(lo, hi) * UrgeScaleNow();
            if (roll0 <= 0f && UrgeScaleNow() <= 0f) return;      // 余韵期间设 0 就完全不累积
            float before0 = _demandUrge;
            _demandUrge = Mathf.Max(0f, _demandUrge + roll0);
            AuditUrge(source + "(区间 " + lo.ToString("0.#") + "~" + hi.ToString("0.#")
                      + " 骰到 " + roll0.ToString("0.##") + ")", before0, _demandUrge);
        }

        /// <summary>
        /// 【叠层判定的唯一实现 —— 所有累积来源共用】。
        ///
        /// 修 #1 的关键：原先叠层判定只写在打屁股那条路径里，
        /// 角色攻击虽然一直在累积索取欲，却从不消费它，
        /// 于是「攻击攒起来的索取欲不会触发第 2 段及以后的叠层」。
        /// </summary>
        private static void TryStackNow(string source)
        {
            // 门槛不再往上钳 —— 那两个滑块本身就是"需要多少才叠"的定义。
            // 索取欲不设硬上限，所以【叠到 150、200】这类阈值是真的能达到的。
            float needLayer = P3(DemandThresholdBase3) + P3(DemandThresholdStep3) * Mathf.Max(0, _demandStacks);

            bool pass;
            if (P3(DemandTriggerMode3) == 1)
            {
                pass = _demandUrge >= needLayer;
                if (!pass && _urgeAuditLeft > 0)
                    AuditUrge("距叠第 " + (_demandStacks + 1) + " 层还差 "
                              + Mathf.Max(0f, needLayer - _demandUrge).ToString("0.#")
                              + "%（需 " + needLayer.ToString("0.#") + "%）", _demandUrge, _demandUrge);
            }
            else
            {
                pass = _demandUrge >= needLayer
                    && UnityEngine.Random.value * 100f < Mathf.Min(_demandUrge, 100f);
                if (!pass && _urgeAuditLeft > 0 && _demandUrge < needLayer)
                    AuditUrge("距叠第 " + (_demandStacks + 1) + " 层还差 "
                              + Mathf.Max(0f, needLayer - _demandUrge).ToString("0.#")
                              + "%（需 " + needLayer.ToString("0.#") + "%）·不掷骰", _demandUrge, _demandUrge);
            }
            if (pass) EnterOrStackDemand(source);
        }

        /// <summary>打屁股 / 坐姿计数的完整效果：速度冲量 + 累积 + 脱出惩罚 + 叠层判定。</summary>
        private static void AccumulateAndMaybeStack(string source, bool applyEscapePenalty, object tab, string escapeField)
        {
            try
            {
                NoteSpank();
                AccumulateUrge(source);

                if (applyEscapePenalty && tab != null
                    && UnityEngine.Random.value * 100f < P3(DemandEscapePenaltyChance3))
                {
                    float need = GetFloat(tab, escapeField);
                    float mul = Mathf.Clamp(P3(DemandEscapePenaltyMul3) / 100f, 1f, 5f);
                    try
                    {
                        FieldInfo fi = FieldQuiet(tab.GetType(), escapeField);
                        if (fi != null) fi.SetValue(tab, need * mul);
                    }
                    catch { }
                    Log.LogInfo("[" + ModeWord() + "] 打屁股让脱出更难了：需要 " + (need * mul).ToString("0.#") + " 次");
                }

                TryStackNow(source);
            }
            catch (Exception e) { Log.LogWarning("[" + ModeWord() + "] 累积失败: " + e.Message); }
        }

        // ---- 通用：射精开始 → 索取模式期间撤销"减速" ----
        //
        // 三条分支的减速方式并不一样（这是踩过的坑）：
        //   Fella（口交）    Event_FellaSyaseiStart()  SetSpeed_Fella(fellaSpeed / 2f) + IsSpeedUping_Fella(0)
        //   Sit（坐姿）      SetSpeed_Sit(sitSpeed / 2f)（两处）+ 停加速
        //   Osiri（背面骑乘）Event_OsiriSyaseiStart()  【只 IsSpeedUping_Osiri(0)，没有减半】
        //
        // 而「索取模式」是背面骑乘位的机制 —— 只挂 Fella 那一支等于没挂。
        private static float _preOsiriSpeed;
        private static bool _preOsiriSpeedValid;
        private static bool _attBoostLogged;
        private static bool _preOsiriUp;

        private static void Prefix_OsiriSyaseiStart(object __instance)
        {
            try
            {
                _preOsiriSpeed = GetFloat(__instance, "osiriSpeed");
                _preOsiriUp = GetFloat(__instance, "isSpeedUpOsiri") > 0.5f;
                _preOsiriSpeedValid = true;
            }
            catch { _preOsiriSpeedValid = false; }
        }

        /// <summary>
        /// 索取模式期间提升攻击力。
        ///
        /// 为什么必须是【后缀】：`TabemiAtt` 由 `TabemiControl.Update()` 每帧调用
        /// `UpdateAtt()` 重算（`TabemiAtt = baseAtt * fellaAttRate / 100`），
        /// 写一次会被立刻覆盖 —— 与本项目"要覆盖每帧写入的状态，必须写在它之后"是同一条。
        ///
        /// 攻击力的作用范围（`DealDamage_Osiri`）：
        ///     num3 = TabemiAtt * ... * HpDamageScaler_Osiri      → 生命伤害
        ///     num4 = TabemiAtt * ... * EcstasyDamageScaler_Osiri → 绝顶值
        /// </summary>
        private static void Postfix_UpdateAtt(object __instance)
        {
            if (!ModeActive()) return;  // 余韵期间不继承；正骑的束缚之吻里也算生效
            float mul = 1f + Mathf.Clamp(P3(DemandAttBonus3), 0f, 500f) / 100f;

            // 攻击力随攻击速度被动波动：速度越快攻击力越高。
            // 驱动量 _spankMul 是"实际在变"的那个 —— 打屁股的冲量把它推上去、再慢慢消退，
            // 所以攻击力也会跟着一起起伏（被动波动）。
            // osiriSpeed 不能用：Play_OsiriMainMixer 把它恒定置 1，没有变化量。
            float speedBonus = Mathf.Clamp(P3(DemandAttSpeedBonus3), 0f, 500f) / 100f;
            if (speedBonus > 0.0001f)
            {
                float refv = Mathf.Max(0.05f, Mathf.Clamp(P3(DemandAttSpeedRef3), 5f, 300f) / 100f);
                float over = Mathf.Max(0f, _spankMul - 1f);          // 超出基准速度的部分
                float k = Mathf.Clamp(P3(DemandAttSpeedExp3), 0f, 8f);
                // 【小指数】归一化，保证 f(refv) 仍等于 1 —— "吃满速度加成"的临界点与线性版一致，
                // 只是形状变凸：over 小时涨得慢（前段平），接近 refv 时涨得快（后段陡）。
                //   f = (e^(k·over) − 1) / (e^(k·refv) − 1)；k = 0 退化为线性。
                float denom = k > 0.001f ? (Mathf.Exp(k * refv) - 1f) : 0f;
                float f = denom > 0.0001f ? (Mathf.Exp(k * over) - 1f) / denom : Mathf.Clamp01(over / refv);
                f = Mathf.Clamp01(f);
                mul += speedBonus * f;
            }

            if (mul <= 1.0001f) return;
            try
            {
                float before = GetFloat(__instance, "TabemiAtt");
                SetField(__instance, "TabemiAtt", before * mul);
                if (!_attBoostLogged)
                {
                    _attBoostLogged = true;
                    Log.LogInfo("[" + ModeWord() + "] 攻击力加成生效：" + before.ToString("0.##") + " → "
                                + (before * mul).ToString("0.##") + "（×" + mul.ToString("0.##")
                                + "，其中速度 ×" + _spankMul.ToString("0.###") + "）");
                }
            }
            catch (Exception e) { Log.LogWarning("[" + ModeWord() + "] 攻击力加成失败: " + e.Message); }
        }

        private static void Postfix_OsiriSyaseiStart(object __instance)
        {
            NoteUrgeEvent("骑乘位射精", SyaseiUrgeChance, SyaseiUrgeGain);
            if (!_demandMode) return;
            try
            {
                // 索取模式期间：速度不减，而且【加速累积强制保持开启】——
                // 不是"恢复原值"（原值可能本来就是停的），而是明确置为 true，
                // 让骑乘位一直加速下去。波动相关的参数一概不动。
                SetField(__instance, "osiriSpeed", _preOsiriSpeedValid ? _preOsiriSpeed : GetFloat(__instance, "osiriSpeed"));
                // 分阶段模式下，这里只保留"不减速"；加速累积交给对应阶段的开关去管
                if (!P3(DemandSpeedSplit3)) SetField(__instance, "isSpeedUpOsiri", true);
                NoteDemandSyasei("骑乘位");
                Log.LogInfo("[" + ModeWord() + "] 骑乘位射精：不减速且加速继续（osiriSpeed="
                            + GetFloat(__instance, "osiriSpeed").ToString("0.###") + "）");
            }
            catch { }
        }

        /// <summary>前缀：记录射精开始前的速度状态。</summary>
        private static void Prefix_FellaSyaseiStart(object __instance)
        {
            try
            {
                _preFellaSpeed = GetFloat(__instance, "fellaSpeed");
                _preSpeedUp = GetFloat(__instance, "isSpeedUpFella") > 0.5f;
                _preSpeedUpValid = true;
            }
            catch { _preSpeedUpValid = false; }
        }

        /// <summary>
        /// 后缀：索取模式期间把"射精导致的减速"原样撤销。
        /// 游戏原码 Event_FellaSyaseiStart()：
        ///     SetSpeed_Fella(fellaSpeed / 2f);     ← 速度减半
        ///     IsSpeedUpingFella(0);                ← 停掉加速累积
        /// 这里只撤销这两项，其余（ateCount /= 2、音效）照常执行。
        /// </summary>
        private static void Postfix_FellaSyaseiStart(object __instance)
        {
            NoteUrgeEvent("口交射精", SyaseiUrgeChance, SyaseiUrgeGain);
            // 余韵累积（内部自带 _demandMode 闸门：常规状态下的射精一律不计）
            NoteDemandSyasei("口交");
            if (!_demandMode || !_preSpeedUpValid) return;
            try
            {
                SetField(__instance, "fellaSpeed", _preFellaSpeed);          // 撤销减半
                SetField(__instance, "isSpeedUpFella", _preSpeedUp);         // 恢复加速累积
                Log.LogInfo("[" + ModeWord() + "] 索取模式：本次射精不减速（保持 fellaSpeed=" + _preFellaSpeed.ToString("0.###") + "）");
            }
            catch { }
        }

        /// <summary>每帧推进：索取模式计时 + 余韵。</summary>
        private void TickDemand()
        {
            if (!P3(DemandEnabled3)) { _demandMode = false; _demandModePose = -1; _afterglowRemaining = 0; return; }

            // 索取模式期间持续保证"加速累积"是开着的。
            // 游戏在别处也会调 IsSpeedUping_Osiri(0)（例如射精、状态切换），
            // 只在射精事件里恢复一次不够稳，所以每帧兜一道。
            // 【状态级兜底：模式进行中不许被换成坐姿】
            //
            // 为什么入口那一层拦不住：切状态的是协程
            //     private IEnumerator _CenterGirl(int i)
            //     {
            //         yield return new WaitForSeconds(effect.GlitchEffect(0.1f, 0.01f, 0.1f));
            //         model.CenterGirlParts(i);
            //         case 1: centerGirlState = CenterGirlState.Sit;   // ← 延迟之后才切
            //     }
            // 也就是说：Show_CenterGirlSit 可能在我们拦之前就被调用过、协程已经排队，
            // 0.1 秒后它照样把状态切成 Sit。前缀拦不住已排队的协程，只能在这里夺回来。
            //
            // 这个异常是可以明确判定的（模式进行中 + 状态却是 Sit = 绝不合法），
            // 所以做纠正动作是恰当的，不是"猜着纠正"。
            // 【只对"在背榨进入的模式"生效】
            //   _demandModePose == 0（口交进）或 2（正骑进）时，人物本来就该待在那边，
            //   夺回骑乘位会把坐姿自己的榨取模式直接掐掉 —— 这正是用户报的 bug。
            if (_demandMode && _demandModePose == 1)
            {
                try
                {
                    object tb3 = Tabemi();
                    if (tb3 != null && GetStringField(tb3, "centerGirlState") == "Sit")
                    {
                        Log.LogInfo("[" + ModeWord() + "] " + ModeWord() + "模式进行中，状态却被切成坐姿 → 夺回骑乘位"
                                    + "（多半是 Show_CenterGirlSit 的协程早已排队）");
                        MethodInfo mi = tb3.GetType().GetMethod("ShowCenterGirlOsiri", AllFlags);
                        if (mi != null) mi.Invoke(tb3, null);
                    }
                }
                catch { }
            }

            // 绝顶动画也加速（osiriSpeed 覆盖不到它）。注意只在 syasei 阶段生效。
            ApplyDemandAnimSpeed(false);
            RestoreAnimSpeedIfLeftSyasei();

            // 加速累积的强制保持 —— 【只在攻击阶段做】。
            // 绝顶/吸精阶段由「绝顶动画倍速」负责；两边同时上会叠成过快
            // （用户实测："同时使用 osiri 自己的加速和连榨的加速"）。
            if (_demandMode && (!P3(DemandSpeedSplit3) || OsiriMixerPlaying()))
            {
                try
                {
                    Type sot = FindType("Live2D_Animation_SitOsiri");
                    if (sot != null)
                    {
                        UnityEngine.Object[] objs = Resources.FindObjectsOfTypeAll(sot);
                        if (objs != null)
                            foreach (UnityEngine.Object o in objs)
                            {
                                Component cp = o as Component;
                                if (cp == null) continue;
                                if (GetFloat(cp, "isSpeedUpOsiri") < 0.5f)
                                    SetField(cp, "isSpeedUpOsiri", true);
                            }
                    }
                }
                catch { }
            }

            if (_demandMode && Time.unscaledTime >= _demandUntil)
            {
                // 角色自行退出。余韵【不在这里立刻发动】——
                // 它已经在模式中按射精次数累积好了，等最后一段的连榨等活动全部结束、
                // 局面稳定之后，由 TickAfterglowSettle 点火。
                _demandMode = false; _demandModePose = -1;
                _demandStacks = 0;
                _lastDemandActivity = Time.unscaledTime;   // 退出也算一次活动，点火要再等
                _chainOsiriNormal = 0; _chainSitNormal = 0;   // 常规计数重新起算
                if (_afterglowPending > 0)
                    Log.LogInfo("[余韵] 最后一段结束，待局面稳定后发动（已累积 " + _afterglowPending + " 次）");
                else
                    Log.LogInfo("[余韵] 最后一段结束，但模式中没有射精 → 不发动余韵");
                int lo = Mathf.Min(DemandAfterglowMin.Value, DemandAfterglowMax.Value);
                int hi = Mathf.Max(DemandAfterglowMin.Value, DemandAfterglowMax.Value);
                _afterglowRemaining = UnityEngine.Random.Range(lo, hi + 1);
                _afterglowTotal++;
                Log.LogInfo("[" + ModeWord() + "] 角色自行退出索取模式 → 进入余韵，连续吸精 " + _afterglowRemaining + " 次");
            }

            // 余韵：等它自然消耗完（每次吸精由 KyuseiRate 掷骰处递减）
            if (_afterglowRemaining > 0 && !_demandMode)
            {
                object tab = Tabemi();
                bool inOsiri = false;
                try
                {
                    string cg = tab != null ? GetStringField(tab, "centerGirlState") : "";
                    inOsiri = cg == "Osiri";
                }
                catch { }
                if (tab == null) return;
                if (!inOsiri && GetFloat(Player(), "Syaseing") < 0.5f && _afterglowRemaining > 0)
                {
                    // 已经完全离开骑乘位且不在榨取中 → 余韵作废，避免次数一直挂着
                    if (GetFloat(Player(), "CurrentEcstasy") <= 0.01f && _afterglowRemaining > 0)
                    { /* 留着，等下次触发 */ }
                }
            }
        }

        // ---- 索取模式期间的绝顶动画倍速 ----
        //
        // 为什么需要：`Update_OsiriMixers()` 里 osiriSpeed 只在
        //     if (OsiriMixer.State.IsPlaying) OsiriMixer.State.Parameter = osiriSpeed;
        // 时才被应用。而绝顶走 `_CenterGirlLayer.Play(OsiriSyaseis[num])`，
        // 把主混合器【换掉了】—— IsPlaying 变假，osiriSpeed 无处可去，
        // 绝顶动画就按默认速度播。所以必须直接设"正在播放"的那个状态。
        //
        // Animancer 的成员名在不同版本不一样，这里依次尝试并记录哪个生效，
        // 免得再靠猜。
        private static string _animSpeedMemberUsed;
        private static bool _animSpeedLogged;
        private static bool _animSpeedApplied;   // 是否已把倍速套在动画上（用于离开绝顶时还原）

        /// <summary>
        /// 骑乘位的主混合器（OsiriMixer）是否正在播放。
        ///
        /// 游戏自己的 Update_OsiriMixers() 用的就是这个判据：
        ///     if (OsiriMixer.State.IsPlaying) OsiriMixer.State.Parameter = osiriSpeed;
        /// 主混合器在播 = 攻击动画；不在播 = 同一个层上换成了绝顶 / 吸精的 clip。
        /// 用它来区分，比按 osiriState 判阶段可靠 —— 后者在"绝顶→吸精"的切换瞬间有空档。
        /// </summary>
        /// <summary>Sit 的任一攻击混合器是否在播（对应 OsiriMixerPlaying）。</summary>
        private static bool SitMixerPlaying()
        {
            try
            {
                Type sot = FindType("Live2D_Animation_SitOsiri");
                if (sot == null) return false;
                UnityEngine.Object[] objs = Resources.FindObjectsOfTypeAll(sot);
                if (objs == null || objs.Length == 0) return false;
                foreach (string fn in new string[] { "SitMixerA", "SitMixerB", "SitMixerKissA", "SitMixerKissB" })
                {
                    object mixer = FieldQuiet(sot, fn)?.GetValue(objs[0]);
                    if (mixer == null) continue;
                    object st = mixer.GetType().GetProperty("State", AllFlags)?.GetValue(mixer, null);
                    PropertyInfo ip = st?.GetType().GetProperty("IsPlaying", AllFlags);
                    object v = ip != null ? ip.GetValue(st, null) : null;
                    if (v is bool b && b) return true;
                }
                return false;
            }
            catch { return false; }
        }

        /// <summary>任一姿势的攻击混合器在播 = 当前是攻击动画（不该加速）。</summary>
        private static bool AttackMixerPlaying()
        {
            return OsiriMixerPlaying() || SitMixerPlaying();
        }

        private static bool OsiriMixerPlaying()
        {
            try
            {
                Type sot = FindType("Live2D_Animation_SitOsiri");
                if (sot == null) return false;
                UnityEngine.Object[] objs = Resources.FindObjectsOfTypeAll(sot);
                if (objs == null || objs.Length == 0) return false;
                object mixer = FieldQuiet(sot, "OsiriMixer")?.GetValue(objs[0]);
                if (mixer == null) return false;
                object mxState = mixer.GetType().GetProperty("State", AllFlags)?.GetValue(mixer, null);
                if (mxState == null) return false;
                PropertyInfo ip = mxState.GetType().GetProperty("IsPlaying", AllFlags);
                object v = ip != null ? ip.GetValue(mxState, null) : null;
                return v is bool b && b;
            }
            catch { return false; }
        }

        private static void ApplyDemandAnimSpeed(bool fromSyaseiStart)
        {
            // 余韵 = 基础速度更低、单次持续时间更长的连榨。
            // 所以余韵期间【即使已经不在索取/榨取模式里】也要继续接管动画速度，
            // 只是换成一个更慢的值。
            bool afterglow = _afterglowRemaining > 0 && !_demandMode;
            if (!ModeActive() && !afterglow) return;
            try
            {
                float want;
                if (afterglow)
                {
                    // 余韵的速度本身也波动：围绕基准做正弦起伏。
                    // 这样【更低但更绵长】不是一条直线，而是有呼吸感的收尾。
                    float baseSpd = Mathf.Clamp(P3(AfterglowSpeed3) / 100f, 0.1f, 4f);
                    float w = Mathf.Clamp(P3(AfterglowSpeedWobble3), 0f, 90f) / 100f;
                    float ph = Mathf.Sin(2f * Mathf.PI * Mathf.Clamp(P3(AfterglowSpeedHz3), 0.02f, 4f)
                                         * Time.unscaledTime);
                    want = Mathf.Clamp(baseSpd * (1f + w * ph), 0.05f, 4f);
                }
                else want = Mathf.Clamp(P3(DemandAnimSpeed3) / 100f, 0.5f, 8f);
                if (afterglow && want >= 0.999f && want <= 1.0001f) return;
                if (!afterglow && want <= 1.0001f) return;

                // 【关键】只有绝顶阶段才套倍速。
                // 攻击动画（OsiriMixer）和绝顶动画（OsiriSyaseis）用的是【同一个层】
                // _CenterGirlLayer == aniA.Layers[0]，不加区分地每帧设 CurrentState 的速度，
                // 会让攻击动画也被套上同样的倍速 —— 两者就"重合"了。
                // 每帧的那条路径必须靠状态判定放行；从 Play_OsiriSyasei 后缀进来的那条
                // 天然就是刚播上绝顶动画，不需要判。
                // 【判据换成"主混合器是否在播"】
                //
                // 原来按 osiriState 判 syasei/kyusei，会留一个空档：
                // 从绝顶切到吸精的那一瞬间，osiriState 未必已经是 kyusei，
                // 于是"离开绝顶就还原速度"抢先执行 → 吸精以原速开始（"很慢"）。
                //
                // 游戏自己的 Update_OsiriMixers() 用的判据就是
                //     if (OsiriMixer.State.IsPlaying) OsiriMixer.State.Parameter = osiriSpeed;
                // —— 主混合器在播 = 攻击动画；不在播 = 同层上换成了绝顶/吸精的 clip。
                // 直接用同一个条件，既没有空档，也不会误碰攻击动画。
                // 主混合器在播 = 攻击动画 → 绝不加速；否则就是同层上的绝顶/吸精 clip
                if (!fromSyaseiStart && AttackMixerPlaying()) return;

                Type sot = FindType("Live2D_Animation_SitOsiri");
                if (sot == null) return;
                UnityEngine.Object[] objs = Resources.FindObjectsOfTypeAll(sot);
                if (objs == null) return;

                foreach (UnityEngine.Object o in objs)
                {
                    Component cp = o as Component;
                    if (cp == null) continue;

                    FieldInfo lf = FieldQuiet(sot, "_CenterGirlLayer");
                    object layer = lf != null ? lf.GetValue(cp) : null;
                    if (layer == null) continue;

                    object state = null;
                    try
                    {
                        PropertyInfo csp = layer.GetType().GetProperty("CurrentState", AllFlags);
                        if (csp != null) state = csp.GetValue(layer, null);
                    }
                    catch { }
                    if (state == null) continue;

                    // 依次尝试候选成员
                    string used = null;
                    foreach (string mn in new string[] { "EffectiveSpeed", "Speed" })
                    {
                        PropertyInfo pi = state.GetType().GetProperty(mn, AllFlags);
                        if (pi != null && pi.CanWrite)
                        {
                            pi.SetValue(state, want, null);
                            used = mn;
                            break;
                        }
                        FieldInfo fi = FieldQuiet(state.GetType(), mn);
                        if (fi != null && fi.FieldType == typeof(float))
                        {
                            fi.SetValue(state, want);
                            used = mn + "(字段)";
                            break;
                        }
                    }
                    if (used == null) continue;

                    if (!_animSpeedLogged)
                    {
                        _animSpeedLogged = true;
                        _animSpeedMemberUsed = used;
                        Log.LogInfo("[" + ModeWord() + "] 绝顶动画倍速已生效：通过 " + used + " 设为 ×" + want.ToString("0.##"));
                    }
                    _animSpeedApplied = true;
                    return;
                }
            }
            catch (Exception e)
            {
                if (!_animSpeedLogged) { _animSpeedLogged = true; Log.LogWarning("[" + ModeWord() + "] 动画倍速失败: " + e.Message); }
            }
        }

        /// <summary>逐帧采样一行：时间,阶段,实际速率,是否已套倍速,osiriSpeed,索取模式,榨取中</summary>
        internal static string SampleAnimLine()
        {
            string phase = "?";
            object tb = Tabemi();
            if (tb != null) phase = GetStringField(tb, "osiriState");

            float sp = -1f, osr = -1f;
            try
            {
                Type sot = FindType("Live2D_Animation_SitOsiri");
                UnityEngine.Object[] objs = sot != null ? Resources.FindObjectsOfTypeAll(sot) : null;
                if (objs != null)
                    foreach (UnityEngine.Object o in objs)
                    {
                        Component cp = o as Component;
                        if (cp == null) continue;
                        osr = GetFloat(cp, "osiriSpeed");
                        FieldInfo lf = FieldQuiet(sot, "_CenterGirlLayer");
                        object layer = lf != null ? lf.GetValue(cp) : null;
                        if (layer == null) continue;
                        object st = null;
                        try
                        {
                            PropertyInfo csp = layer.GetType().GetProperty("CurrentState", AllFlags);
                            if (csp != null) st = csp.GetValue(layer, null);
                        }
                        catch { }
                        if (st != null)
                        {
                            PropertyInfo pi = st.GetType().GetProperty("EffectiveSpeed", AllFlags);
                            if (pi != null) { object v = pi.GetValue(st, null); if (v is float f) sp = f; }
                        }
                        break;
                    }
            }
            catch { }

            object pl = Player();
            float sy = pl != null ? GetFloat(pl, "Syaseing") : -1f;

            return string.Format("{0:0.000},{1},{2:0.###},{3},{4:0.###},{5},{6}",
                Time.unscaledTime, phase, sp, _animSpeedApplied ? 1 : 0, osr,
                _demandMode ? 1 : 0, sy > 0.5f ? 1 : 0);
        }

        /// <summary>
        /// 读"当前正在播放的动画状态的实际速率"，并给出所属阶段。
        /// 这是判断绝顶有没有真的加速的唯一硬证据 —— 不靠肉眼估。
        /// </summary>
        internal static string ReadAnimSpeed()
        {
            try
            {
                string phase = "?";
                object tb = Tabemi();
                if (tb != null) phase = GetStringField(tb, "osiriState");

                Type sot = FindType("Live2D_Animation_SitOsiri");
                if (sot == null) return "找不到 Live2D_Animation_SitOsiri";
                UnityEngine.Object[] objs = Resources.FindObjectsOfTypeAll(sot);
                if (objs == null || objs.Length == 0) return "没有实例";

                foreach (UnityEngine.Object o in objs)
                {
                    Component cp = o as Component;
                    if (cp == null) continue;
                    FieldInfo lf = FieldQuiet(sot, "_CenterGirlLayer");
                    object layer = lf != null ? lf.GetValue(cp) : null;
                    if (layer == null) continue;

                    object state = null;
                    try
                    {
                        PropertyInfo csp = layer.GetType().GetProperty("CurrentState", AllFlags);
                        if (csp != null) state = csp.GetValue(layer, null);
                    }
                    catch { }
                    if (state == null) return "osiriState=" + phase + "  当前层没有播放中的状态";

                    string clip = "?";
                    try
                    {
                        PropertyInfo cp2 = state.GetType().GetProperty("Clip", AllFlags);
                        object cv = cp2 != null ? cp2.GetValue(state, null) : null;
                        if (cv != null) clip = cv.ToString();
                    }
                    catch { }

                    float sp = -1f;
                    try
                    {
                        PropertyInfo pi = state.GetType().GetProperty("EffectiveSpeed", AllFlags);
                        if (pi != null) { object v = pi.GetValue(state, null); if (v is float f) sp = f; }
                    }
                    catch { }

                    return string.Format("osiriState={0}  实际速率={1:0.###}  索取倍速已套用={2}  用过的成员={3}  片段={4}",
                        phase, sp, _animSpeedApplied, _animSpeedMemberUsed ?? "(还没套过)", clip);
                }
            }
            catch (Exception e) { return "ERR " + e.Message; }
            return "未取到";
        }

        /// <summary>供命令 / 面板调用。</summary>
        internal static string DemandCmd(string arg)
        {
            switch ((arg ?? "status").ToLowerInvariant())
            {
                case "on": SetP3(DemandEnabled3, true); return ModeWordForBanner() + "模式 = 开";
                case "off": SetP3(DemandEnabled3, false); _demandMode = false; _demandModePose = -1; _demandStacks = 0; _afterglowRemaining = 0; _afterglowPending = 0; _demandEntered = false; return ModeWordForBanner() + "模式 = 关";
                case "fire":
                    {
                        float lo = Mathf.Min(P3(DemandDurationMin3), P3(DemandDurationMax3));
                        float hi = Mathf.Max(P3(DemandDurationMin3), P3(DemandDurationMax3));
                        EnterOrStackDemand("手动");
                        ShowChainBanner(ModeWordForBanner() + "模式", 3f);
                        return "已手动进入" + ModeWordForBanner() + "模式，持续 " + (_demandUntil - Time.unscaledTime).ToString("0.#") + "s";
                    }
                case "afterglow":
                    {
                        int lo = Mathf.Min(DemandAfterglowMin.Value, DemandAfterglowMax.Value);
                        int hi = Mathf.Max(DemandAfterglowMin.Value, DemandAfterglowMax.Value);
                        // 【逃生阀】手动余韵 = 强制退出索取模式。
                        // 索取时间太长会让角色一直停在 Osiri，而「回归累积」的坐姿挂起
                        // 又要等骑乘位结束才放行 —— 坐在长索取里，坐姿入口就永远进不去。
                        // 所以这里把索取模式相关的状态【全部清干净】。
                        _demandMode = false; _demandModePose = -1;
                        _demandStacks = 0;
                        _demandUntil = 0f;
                        _demandUrge = 0f;
                        ChainResetAll();
                        _demandEntered = true;      // 手动命令视为"进过模式"，方便测试

                        // 【修】以前这里设的是 _afterglowRemaining，而负责"开第一次榨取"的
                        // TickAfterglowSettle 第一行就是 if (_afterglowPending <= 0) return;
                        // 于是手动余韵设了个【没人消费的计数器】：
                        // 消费它要靠榨取收尾事件，榨取要点火逻辑去开，点火逻辑又被 pending 挡住
                        // —— 结果就是"手动进入余韵功能无效"。
                        // 正确做法：交给 pending，让点火逻辑去开第一次榨取。
                        _afterglowPending = UnityEngine.Random.Range(lo, hi + 1);
                        _afterglowRemaining = 0;
                        _afterglowSettleAt = -999f;   // 立即重新计稳定时间
                        return "已手动进入余韵：待发动 " + _afterglowPending
                               + " 次（局面稳定后自动开第一次榨取）";
                    }
                case "status":
                    {
                        string st = _demandMode
                            ? string.Format(ModeWord() + "模式中 {1} 段 / 共剩余 {0:0.#}s", Mathf.Max(0f, _demandUntil - Time.unscaledTime), _demandStacks)
                            : "不在" + ModeWord() + "模式";
                        float att = -1f;
                        try
                        {
                            object tb = Tabemi();
                            if (tb != null) att = GetFloat(tb, "TabemiAtt");
                        }
                        catch { }
                        string pose = "";
                        try
                        {
                            object tb2 = Tabemi();
                            if (tb2 != null)
                            {
                                string cg = GetStringField(tb2, "centerGirlState");
                                string st2 = cg == "Sit" ? GetStringField(tb2, "sitState")
                                                         : GetStringField(tb2, "osiriState");
                                pose = "[" + cg + "·" + st2 + "] ";
                            }
                        }
                        catch { }
                        st = pose + st;
                        float zh = Mathf.Max(0f, _zeroHoldUntil - Time.unscaledTime);
                        if (zh > 0f) st += string.Format("（清零振荡中 剩 {0:0.#}s）", zh);
                        return string.Format("{0}  索取欲={1:0.#}%（叠下一层需 {16:0.#}%）  攻击力={2:0.##}(+{3:0}%)  "
                            + "动画倍速={7:0}%[{8}]  回口交基准={9:0.#}%  已回口交={10}次  回归累积={12:0.#}%{13}  余韵剩={4}(待发动{14}{15})  触发过{5}次",
                            st, _demandUrge, att, P3(DemandAttBonus3), _afterglowRemaining, _demandTotal, _afterglowTotal,
                            P3(DemandAnimSpeed3), _animSpeedMemberUsed ?? "未生效",
                            10f * Mathf.Pow(1f - Mathf.Clamp01(P3(DemandFellaDecay3) / 100f), _demandTotal),
                            _fellaReturns, P3(DemandAttSpeedBonus3),
                            _osiriReturnChance, _osiriReturnArmed ? "" : "(未武装)",
                            _afterglowPending, _demandEntered ? "" : "·闸门关",
                            P3(DemandThresholdBase3) + P3(DemandThresholdStep3) * Mathf.Max(0, _demandStacks));
                    }
                default: return "ERR 用法: demand on|off|fire|afterglow|status";
            }
        }

        /// <summary>
        /// 离开绝顶阶段（回到 attacking）时把动画速度还原成 1 ——
        /// 否则倍速会残留在同层的攻击动画上。
        /// </summary>
        private static void RestoreAnimSpeedIfLeftSyasei()
        {
            if (!_animSpeedApplied) return;
            try
            {
                // 同一个判据：主混合器在播 = 已回到攻击动画，此时才需要还原
                if (!AttackMixerPlaying()) return;

                Type sot = FindType("Live2D_Animation_SitOsiri");
                if (sot == null) return;
                UnityEngine.Object[] objs = Resources.FindObjectsOfTypeAll(sot);
                if (objs == null) return;
                foreach (UnityEngine.Object o in objs)
                {
                    Component cp = o as Component;
                    if (cp == null) continue;
                    FieldInfo lf = FieldQuiet(sot, "_CenterGirlLayer");
                    object layer = lf != null ? lf.GetValue(cp) : null;
                    if (layer == null) continue;
                    object state = null;
                    try
                    {
                        PropertyInfo csp = layer.GetType().GetProperty("CurrentState", AllFlags);
                        if (csp != null) state = csp.GetValue(layer, null);
                    }
                    catch { }
                    if (state == null) continue;
                    PropertyInfo pi = state.GetType().GetProperty(_animSpeedMemberUsed ?? "EffectiveSpeed", AllFlags);
                    if (pi != null && pi.CanWrite) { pi.SetValue(state, 1f, null); _animSpeedApplied = false; }
                    return;
                }
            }
            catch { }
        }

        // ---- 接管绝顶收尾的三岔路口 ----
        //
        // 游戏原码：
        //     private void Event_OsiriSyaseiOnEnd()
        //     {
        //         player.Syaseing = false;
        //         float value = UnityEngine.Random.value;
        //         if (GameManager.gameover)  Play_Osiri吸精(true);
        //         else if (value < 0.5f)     Play_OsiriMainMixer();   // 50% 回骑乘位攻击
        //         else if (value < 0.9f)     Play_Osiri吸精(true);     // 40% 吸精
        //         else                       tabemi.Osiri解除();       // 10% 【回口交】
        //     }
        //
        // 这里在索取模式期间接管这段：把"回口交"那一支按需求收窄
        //     pFella = 0.1 * (1 - 衰减)^已进入索取次数     索取模式期间 = 0
        // 左半边（0.5 的回骑乘位）保持不变，右半边在"吸精 / 回口交"之间重新分配。
        private static bool Prefix_Event_OsiriSyaseiOnEnd(object __instance)
        {
            if (!_demandMode || !P3(DemandBlockFella3)) return true;   // 只在索取模式期间接管
            try
            {
                object player = Player();
                if (player != null) SetField(player, "Syaseing", false);

                bool gameover = false;
                try
                {
                    Type gm = FindType("GameManager");
                    FieldInfo gf = gm != null ? FieldQuiet(gm, "gameover") : null;
                    object gv = gf != null ? gf.GetValue(null) : null;
                    gameover = gv is bool gb && gb;
                }
                catch { }

                float pFella = 0f;      // 索取模式期间：不回口交
                float r = UnityEngine.Random.value;

                if (gameover) { InvokeAt(__instance, "Play_Osiri吸精", true); return false; }
                if (r < 0.5f) { InvokeAt(__instance, "Play_OsiriMainMixer"); return false; }
                if (r < 1f - pFella) { InvokeAt(__instance, "Play_Osiri吸精", true); return false; }

                // 走到这里才是"回口交"
                object tab = Tabemi();
                if (tab != null) InvokeNoArg(tab, "Osiri解除");
                return false;
            }
            catch (Exception e) { Log.LogWarning("[" + ModeWord() + "] 接管绝顶收尾失败: " + e.Message); return true; }
        }

        /// <summary>无参反射调用（带日志）。</summary>
        private static void InvokeAt(object obj, string method)
        {
            try
            {
                MethodInfo mi = obj.GetType().GetMethod(method, AllFlags);
                if (mi != null) mi.Invoke(obj, null);
            }
            catch (Exception e) { Log.LogWarning("[" + ModeWord() + "] 调用 " + method + " 失败: " + e.Message); }
        }

        private static void InvokeAt(object obj, string method, bool arg)
        {
            try
            {
                MethodInfo mi = obj.GetType().GetMethod(method, AllFlags);
                if (mi != null) mi.Invoke(obj, new object[] { arg });
            }
            catch (Exception e) { Log.LogWarning("[" + ModeWord() + "] 调用 " + method + " 失败: " + e.Message); }
        }

        private static void InvokeNoArg(object obj, string method)
        {
            try
            {
                MethodInfo mi = obj.GetType().GetMethod(method, AllFlags);
                if (mi != null) mi.Invoke(obj, null);
            }
            catch (Exception e) { Log.LogWarning("[" + ModeWord() + "] 调用 " + method + " 失败: " + e.Message); }
        }

        // =================================================================
        // 从骑乘位回到口交后：累积「再次进入骑乘位的概率」
        //
        // 规则（来自用户）：
        //   · 回到口交后，角色每做一次【造成伤害的活动】——攻击、射精、吸精——
        //     都有一定概率让这个累积值上涨；概率与增量【都可波动】。
        //   · 累积到 100% → 立刻进入骑乘位。
        //   · 若此时游戏正准备进入坐姿（SitGirlStartTimer < 0 时每帧掷 10%），
        //     则把坐姿【挂起】，等这次骑乘位结束后再让它继续。
        //     游戏原码：if (SitGirlStartTimer < 0f && !player.Syaseing)
        //               { SitGirlStartTimer += num2; if (Random.value < 0.1f) Show_CenterGirlSit(); }
        // =================================================================

        /// <summary>角色造成了一次伤害（攻击 / 射精 / 吸精）→ 累积"再次进入骑乘位"的概率。</summary>
        private static void NoteDamageActivity()
        {
            if (!P3(OsiriReturnEnabled3) || !_osiriReturnArmed) return;
            try
            {
                float j = Mathf.Clamp(P3(OsiriReturnJitter3), 0f, 90f) / 100f;
                float trig = Mathf.Clamp(P3(OsiriReturnTrigger3) * (1f + UnityEngine.Random.Range(-j, j)), 0f, 100f);
                if (UnityEngine.Random.value * 100f >= trig) return;

                float gain = Mathf.Max(0f, P3(OsiriReturnGain3) * (1f + UnityEngine.Random.Range(-j, j)));
                _osiriReturnChance = Mathf.Min(100f, _osiriReturnChance + gain);

                if (_osiriReturnChance >= 100f)
                {
                    Log.LogInfo("[回归] 再次进入骑乘位的概率已达 100% → 立刻进入");
                    TryEnterOsiriNow();
                }
            }
            catch { }
        }

        /// <summary>
        /// 索取 / 榨取模式中射精一次 → 累积余韵。
        ///
        /// 【只在模式中累积】：模式外的射精不计（用户明确要求）。
        /// 【累积不立刻发动】：真正的发动推迟到最后一段及其连榨等活动全部结束之后，
        /// 由 TickAfterglowSettle 在局面稳定时点火。
        /// </summary>
        /// <summary>
        /// 【不变量】余韵只在索取 / 榨取模式中、由射精累积。
        ///
        /// 所有常规状态下的活动（普通攻击、常规连榨、常规射精）**一律不触发余韵** ——
        /// 这一条由下面的 `if (!_demandMode) return;` 保证。
        /// 改这里之前请先确认：任何新增的累积来源都必须经过这道闸门。
        ///
        /// 注：`_afterglowPending` 的写入点【只有四处】——
        ///   1. 本函数（模式中射精，带闸门）
        ///   2. EnterOrStackDemand 里"收回上一轮没跑完的余韵"（只在进入/叠加模式时）
        ///   3. 手动 `demand afterglow` 命令
        ///   4. defaults 复位
        /// 新增第五条之前请三思。
        /// </summary>
        private static void NoteDemandSyasei(string pose)
        {
            if (!_demandMode) return;
            if (!P3(AfterglowUseDemandGain3) && _afterglowRemaining > 0) return;
            try
            {
                _demandSyaseiCount++;
                int add = Mathf.Max(0, P3(AfterglowPerSyasei3));
                _afterglowPending += add;
                Log.LogInfo("[余韵] " + pose + " 模式中第 " + _demandSyaseiCount + " 次射精 → 累积余韵 +"
                            + add + "（累计 " + _afterglowPending + "）");
            }
            catch { }
        }

        /// <summary>
        /// 最后一段结束后，等局面稳定再发动余韵。
        /// "稳定" = 不在榨取中（Syaseing 为假）且索取/榨取模式已结束，
        /// 并连续保持 AfterglowSettleDelay 秒 —— 这样最后一段的连榨、吸精、动画收尾都能跑完。
        /// </summary>
        private void TickAfterglowSettle()
        {
            // 余韵刚刚烧完 → 按开关把索取欲一并清零。
            // 否则余韵那十几次连榨攒起来的索取欲会立刻触发下一轮索取模式，
            // 手动余韵作为"逃生阀"的意义就没了。
            if (_afterglowWasRunning && _afterglowRemaining <= 0)
            {
                _afterglowWasRunning = false;
                if (P3(AfterglowClearsUrge3))
                {
                    AuditUrge("余韵结束·清空索取欲", _demandUrge, 0f);
                    _demandUrge = 0f;
                    _demandStacks = 0;
                    _demandEntered = false;      // 连同闸门一起落，杜绝"紧接着又触发"
                    Log.LogInfo("[余韵] 已清空：索取欲归零、闸门落下 → 之后有一段干净的空档");
                }
            }
            if (_afterglowRemaining > 0) _afterglowWasRunning = true;

            // 【闸门】没进过索取/榨取模式就不会发动余韵。
            // 单看 _afterglowPending > 0 不够 —— 那个值会跨周期残留，
            // 于是"这次根本没进过模式"也会自发冒出一段余韵。
            if (!_demandEntered)
            {
                if (_afterglowPending != 0) { _afterglowPending = 0; _afterglowSettleAt = -999f; }
                return;
            }
            if (_afterglowPending <= 0) return;
            if (_demandMode) { _afterglowSettleAt = -999f; return; }   // 还在模式里，不发动

            // 【叠索取/榨取状态时不许点火】——
            // 只看"当前不在模式里"是不够的：段与段之间、以及叠加上一段的那一刹那，
            // _demandMode 都可能恰好为假，余韵就会在叠的过程中冒出来。
            // 所以还要"距最后一次进入/叠加足够久"。
            float gap = Mathf.Max(0f, P3(AfterglowSettleDelay3));
            if (Time.unscaledTime - _lastDemandActivity < gap) { _afterglowSettleAt = -999f; return; }

            bool busy;
            try { busy = GetFloat(Player(), "Syaseing") > 0.5f; }
            catch { busy = false; }

            if (busy) { _afterglowSettleAt = -999f; return; }          // 还在榨取，重新计时

            // 段数还没退干净（比如刚被叠加过）也不点火
            if (_demandStacks > 0 && _demandMode) { _afterglowSettleAt = -999f; return; }

            if (_afterglowSettleAt < 0f)
                _afterglowSettleAt = Time.unscaledTime + Mathf.Max(0f, P3(AfterglowSettleDelay3));

            if (Time.unscaledTime < _afterglowSettleAt) return;

            // 局面稳定 → 发动
            _afterglowRemaining = _afterglowPending;
            _afterglowPending = 0;
            _demandEntered = false;     // 本周期结束，重新等待下一次"进过模式"
            _afterglowTotal++;
            _afterglowSettleAt = -999f;
            Log.LogInfo("[余韵] 最后一段已结束且局面稳定 → 发动余韵 " + _afterglowRemaining + " 次");
            ShowChainBanner(string.Format("余韵 · 剩余 {0} 次", _afterglowRemaining), 4f);

            // 立刻开一次榨取，把余韵的连榨链启动起来（否则没有"收尾事件"去消费它）
            try
            {
                Type sot = FindType("Live2D_Animation_SitOsiri");
                UnityEngine.Object[] objs = sot != null ? Resources.FindObjectsOfTypeAll(sot) : null;
                if (objs != null && objs.Length > 0)
                {
                    object inst = objs[0];
                    string cg = GetStringField(Tabemi(), "centerGirlState");
                    if (cg == "Osiri") InvokeAt(inst, "Play_Osiri吸精", true);
                    else if (cg == "Sit") InvokeAt(inst, "Play_SitKiss吸精", true);
                    else
                    {
                        Type at = FindType("Live2D_AnimationControl");
                        UnityEngine.Object[] objs2 = at != null ? Resources.FindObjectsOfTypeAll(at) : null;
                        if (objs2 != null && objs2.Length > 0) InvokeAt(objs2[0], "Play_Fella吸精", true);
                    }
                }
            }
            catch (Exception e) { Log.LogWarning("[余韵] 启动余韵连榨失败: " + e.Message); }
        }

        /// <summary>回到口交时武装累积。</summary>
        private static void ArmOsiriReturn()
        {
            if (!P3(OsiriReturnEnabled3)) return;
            _osiriReturnArmed = true;
            _osiriReturnChance = 0f;
        }

        /// <summary>
        /// 立刻进入骑乘位。若游戏正准备进入坐姿，则把坐姿挂起，
        /// 等这次骑乘位结束（centerGirlState 离开 Osiri）再放行。
        /// </summary>
        private static void TryEnterOsiriNow()
        {
            try
            {
                object tab = Tabemi();
                if (tab == null) return;

                string cg = GetStringField(tab, "centerGirlState");
                if (cg != "Fella") return;                       // 只在口交状态时切过去
                if (GetFloat(Player(), "Syaseing") > 0.5f) return;   // 榨取中不打断

                // 游戏是否正准备进入坐姿？

                _osiriReturnArmed = false;
                _osiriReturnChance = 0f;

                MethodInfo mi = tab.GetType().GetMethod("ShowCenterGirlOsiri", AllFlags);
                if (mi != null) mi.Invoke(tab, null);
                Log.LogInfo("[回归] 已进入骑乘位");
            }
            catch (Exception e) { Log.LogWarning("[回归] 进入骑乘位失败: " + e.Message); }
        }

        /// <summary>
        /// 余韵清空之前，不进入口交 / 坐姿。
        ///
        /// 理由（用户）：余韵是一次【绵长的收尾】，中途换姿势会把连榨链断掉，
        /// 余韵就永远清不完了。所以余韵一发动，就把其他姿势的入口全部拦下，
        /// 直到 _afterglowRemaining 归零。
        /// </summary>
        /// <summary>
        /// 【坐姿锁定】人在坐姿时，不许被切到口交或骑乘位。
        ///
        /// 游戏里离开坐姿只有两个出口：
        ///     ShowCenterGirlFella()   ← Osiri解除() 也是调它
        ///     ShowCenterGirlOsiri()
        /// 两个都拦掉，坐姿就成了"进得去出不来"的状态。
        ///
        /// 为什么需要：正骑的榨取链（连榨 / 余韵）全建立在「人在坐姿」上，
        /// 中途被切走会把整条链断掉。
        /// </summary>
        // =================================================================
        // 汉堡制作倒计时
        //
        // 游戏侧（MenuControl）：
        //     private void Timing() {
        //         if (timing) {
        //             timeLeft -= Time.deltaTime;
        //             if (timeLeft <= 0f) { ...TimeOut(); return; }
        //         }
        //         countdownText.text = Mathf.CeilToInt(timeLeft).ToString();
        //     }
        //     下单时：timeLeft = timeAll;
        //
        // 【必须挂前缀，不能挂后缀】
        // 游戏是「先递减、再判零」。如果放在后缀补时间，
        // 判零那一步已经跑过、TimeOut 已经触发 —— 冻结和慢速根本拦不住。
        // 前缀里先把时间补上，游戏再减 dt，净值就等于 dt * 速度 
        // 而且判零看到的是补过的值 
        // =================================================================

        private static void Prefix_MenuTiming(object __instance)
        {
            try
            {
                // (1) 每单时长：写进 timeAll，下单重置时就会用到
                float all = BurgerTimeAll.Value;
                if (all > 0.01f) SetField(__instance, "timeAll", all);

                // (2) 倒计时速度
                float sp = Mathf.Clamp(BurgerTimeSpeed.Value, 0f, 1000f) / 100f;
                if (Mathf.Abs(sp - 1f) < 0.001f) return;

                // 不在计时中就别动 —— 结算之后的界面不该被改写
                try
                {
                    FieldInfo tf = FieldQuiet(__instance.GetType(), "timing");
                    if (tf != null && !(bool)tf.GetValue(__instance)) return;
                }
                catch { }

                float tl = GetFloat(__instance, "timeLeft");
                // 把"多减的部分"提前补上：游戏随后减 dt，净值 = dt * sp
                SetField(__instance, "timeLeft", tl + Time.deltaTime * (1f - sp));
            }
            catch { }
        }

        private static bool SitLockActive()
        {
            if (!SitLockEnabled.Value) return false;
            try { return PoseIdx() == 2; } catch { return false; }
        }

        /// <summary>ShowCenterGirlOsiri 的前缀（原版没有，为新加的）。</summary>
        private static bool Prefix_ShowCenterGirlOsiri()
        {
            if (SitLockActive())
            {
                Log.LogInfo("[坐姿锁定] 人在坐姿 → 拦下一次切到骑乘位");
                return false;
            }
            return true;
        }

        // =================================================================
        // 配置自动迁移
        //
        // 【为什么需要】v1.0.0 之前参数是"全局单份"的，之后改成了"按姿势三份"
        // （`X` → `X_Fella` / `X_Osiri` / `X_Sit`）。老用户升级时，
        // 旧键的值会【静默丢掉】—— 参数回到代码默认，而且没有任何提示。
        //
        // 实测踩过两次：31 个参数那次丢了 48 个用户调过的值；
        // 最后 3 个参数那次丢了 DemandFellaDecay=59.78。两次都是手工改文件救回来的。
        //
        // 【做法】旧键已经不再 Bind，所以读不到内存值 —— 直接解析 cfg 原文。
        //   (1) 备份原文件
        //   (2) 逐对找：旧键有值 且 三份副本都还是默认 → 把旧值复制到三份
        //   (3) 记日志告诉用户搬了什么
        //   (4) ConfigVersion 置 1，以后不再跑
        // =================================================================

        private const int CONFIG_VERSION_CURRENT = 1;

        private static int _migratedCount;

        internal static void MigrateConfigIfNeeded()
        {
            try
            {
                if (ConfigVersion.Value >= CONFIG_VERSION_CURRENT) return;

                string path = _pluginInstance.Config.ConfigFilePath;   // Config 是实例属性
                if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                {
                    ConfigVersion.Value = CONFIG_VERSION_CURRENT;
                    return;
                }

                string[] lines = System.IO.File.ReadAllLines(path, System.Text.Encoding.UTF8);
                // 解析成 key → 值
                var vals = new Dictionary<string, string>();
                foreach (string raw in lines)
                {
                    string l = raw.Trim();
                    if (l.Length == 0 || l[0] == '#' || l[0] == '[') continue;
                    int eq = l.IndexOf('=');
                    if (eq <= 0) continue;
                    vals[l.Substring(0, eq).Trim()] = l.Substring(eq + 1).Trim();
                }

                // ── 找出"有旧键、且三份副本都还是默认"的参数 ──
                var moved = new List<string>();
                foreach (string key in new List<string>(vals.Keys))
                {
                    if (key.EndsWith("_Fella") || key.EndsWith("_Osiri") || key.EndsWith("_Sit")) continue;
                    // 旧键必须不在新体系里（新体系里没有不带后缀的键）
                    if (vals.ContainsKey(key + "_Fella") || vals.ContainsKey(key + "_Osiri")
                        || vals.ContainsKey(key + "_Sit")) continue;
                    continue;   // 旧键本身没后缀、也没有对应副本 → 不是我们要搬的
                }

                // 上面那段只是确认命名约定；真正的搬运按"三份副本"来
                // 收集所有 _Fella 键 → 推出 base 名
                var bases = new List<string>();
                foreach (string key in vals.Keys)
                {
                    if (!key.EndsWith("_Fella")) continue;
                    string b = key.Substring(0, key.Length - "_Fella".Length);
                    if (!vals.ContainsKey(b)) continue;             // 没有旧键，跳过
                    bases.Add(b);
                }

                if (bases.Count > 0)
                {
                    Log.LogInfo("[配置] 发现 " + bases.Count + " 个候选（旧键 + 三份副本都在），开始尝试迁移");
                    // (1) 备份
                    string bak = path + ".bak-before-v1";
                    if (!System.IO.File.Exists(bak)) System.IO.File.Copy(path, bak, true);

                    foreach (string b in bases)
                    {
                        string oldVal = vals[b];
                        string newVal = vals[b + "_Fella"];
                        // 只在"三份副本完全一致且等于默认"时才搬 ——
                        // 否则说明用户已经在新体系里调过，不能覆盖他
                        string vO = vals.ContainsKey(b + "_Osiri") ? vals[b + "_Osiri"] : newVal;
                        string vS = vals.ContainsKey(b + "_Sit") ? vals[b + "_Sit"] : newVal;
                        if (vO != newVal || vS != newVal)
                        {
                            Log.LogInfo("[配置] 跳过 " + b + "：三份副本不一致（用户已在新体系里调过）");
                            continue;
                        }

                        foreach (string suf in PoseSuffix)
                        {
                            ConfigEntry<float>[] fArr = FindP3Float(b);
                            if (fArr == null)
                            {
                                // 诊断：找到了旧键却找不到对应的三份组 —— 说明注册有问题
                                Log.LogWarning("[配置] 迁移：找不到三份组 " + b + "（已注册 float 组 "
                                               + _p3Groups.Count + " / int " + _p3IntGroups.Count
                                               + " / bool " + _p3BoolGroups.Count + "）");
                            }
                            if (fArr != null)
                            {
                                float fv;
                                if (float.TryParse(oldVal, System.Globalization.NumberStyles.Float,
                                        System.Globalization.CultureInfo.InvariantCulture, out fv))
                                    fArr[Array.IndexOf(PoseSuffix, suf)].Value = fv;
                                moved.Add(string.Format("{0} = {1}", b + suf, oldVal));
                                continue;
                            }
                            ConfigEntry<int>[] iArr = FindP3Int(b);
                            if (iArr != null)
                            {
                                int iv;
                                if (int.TryParse(oldVal, out iv))
                                    iArr[Array.IndexOf(PoseSuffix, suf)].Value = iv;
                                moved.Add(string.Format("{0} = {1}", b + suf, oldVal));
                                continue;
                            }
                            ConfigEntry<bool>[] bArr = FindP3Bool(b);
                            if (bArr != null)
                            {
                                bool bv = oldVal.Equals("true", StringComparison.OrdinalIgnoreCase);
                                bArr[Array.IndexOf(PoseSuffix, suf)].Value = bv;
                                moved.Add(string.Format("{0} = {1}", b + suf, oldVal));
                            }
                        }
                    }
                }

                // 【关键】只有真的搬到了东西才推进版本号。
                // 第一版写的是无条件置 1 —— 结果测试时它"跑了但没搬成"，
                // 版本号却被推到 1，于是【以后永远不会再试】
                // 那比不迁移更糟：用户以为迁过了，实际参数全回落默认。
                //
                // 现在：没搬成就不推进 → 下次启动还会再试一次（开销可忽略）。
                if (moved.Count > 0) ConfigVersion.Value = CONFIG_VERSION_CURRENT;
                _migratedCount = moved.Count;

                if (moved.Count > 0)
                {
                    Log.LogInfo("======== 配置自动迁移 ========");
                    Log.LogInfo("检测到旧版配置（单份键），已把值复制到「按姿势三份」键。");
                    Log.LogInfo("原文件已备份为：" + System.IO.Path.GetFileName(path) + ".bak-before-v1");
                    Log.LogInfo("共迁移 " + moved.Count + " 个键：");
                    for (int i = 0; i < moved.Count && i < 40; i++) Log.LogInfo("  " + moved[i]);
                    if (moved.Count > 40) Log.LogInfo("  …（其余 " + (moved.Count - 40) + " 个见配置文件）");
                    Log.LogInfo("============================");
                }
                else
                {
                    Log.LogInfo("[配置] 无需迁移（没有发现旧版单份键）");
                }
            }
            catch (Exception e)
            {
                Log.LogWarning("[配置] 自动迁移失败（不影响使用）：" + e.Message);
            }
        }

        /// <summary>按 base 名在已绑定的三份组里查（float）。</summary>
        private static ConfigEntry<float>[] FindP3Float(string baseName)
        {
            object o;
            if (_p3ByName.TryGetValue(baseName, out o)) return o as ConfigEntry<float>[];
            return null;
        }

        private static ConfigEntry<int>[] FindP3Int(string baseName)
        {
            object o;
            if (_p3ByName.TryGetValue(baseName, out o)) return o as ConfigEntry<int>[];
            return null;
        }

        private static ConfigEntry<bool>[] FindP3Bool(string baseName)
        {
            object o;
            if (_p3ByName.TryGetValue(baseName, out o)) return o as ConfigEntry<bool>[];
            return null;
        }

        private static bool Prefix_ShowCenterGirlFella()
        {
            if (_afterglowRemaining > 0)
            {
                Log.LogInfo("[余韵] 余韵未清空（剩 " + _afterglowRemaining + " 次）→ 不进口交");
                return false;
            }
            // 【坐姿锁定】人在坐姿就不许被切到口交（Osiri解除() 也是调这个方法）
            if (SitLockActive())
            {
                Log.LogInfo("[坐姿锁定] 人在坐姿 → 拦下一次切到口交");
                return false;
            }
            return true;
        }

        /// <summary>坐姿挂起期间，拦住游戏的 Show_CenterGirlSit。</summary>
        private static bool Prefix_ShowCenterGirlSit()
        {
            if (_afterglowRemaining > 0)
            {
                Log.LogInfo("[余韵] 余韵未清空（剩 " + _afterglowRemaining + " 次）→ 不进坐姿");
                return false;
            }
            // 【索取/榨取模式进行中 → 坐姿不许顶进来】
            //
            // 原来这里【没有】这一道，只有"回归累积"的挂起（_sitDeferred），
            // 而它只在"进入骑乘位时坐姿正好待进入"那一刻才置位。
            // 于是坐姿一到点（用户设的是 1 小时）就把正在进行的索取模式顶掉了。
            //
            // 现在明确挡住：模式没结束就不换姿势。
            // 要主动退出索取模式 → 用「手动进入余韵」那个逃生阀（它会强制清干净模式）。
            //
            // 注意：这里【不设超时强制放行】—— 那是我之前加的，用户明确要求删掉。
            // 它会破坏"模式期间不被打断"这条设计。
            if (_demandMode)
            {
                Log.LogInfo("[" + ModeWord() + "] " + ModeWord() + "模式进行中（" + _demandStacks + " 段 / 剩余 "
                            + Mathf.Max(0f, _demandUntil - Time.unscaledTime).ToString("0.#")
                            + "s）→ 不许坐姿顶进来。要退出请用「手动进入余韵」");
                return false;
            }

            if (!_sitDeferred) return true;
            try
            {
                object tab = Tabemi();
                string cg = tab != null ? GetStringField(tab, "centerGirlState") : "";
                if (cg == "Osiri") return false;      // 骑乘位还在进行 → 继续挂起
                _sitDeferred = false;                 // 骑乘位结束 → 放行
                Log.LogInfo("[回归] 骑乘位结束 → 放行被挂起的坐姿");
            }
            catch { }
            return true;
        }

        /// <summary>口交造成伤害（攻击 / 射精）→ 也算一次"造成伤害的活动"。</summary>
        private static void Postfix_DealDamage_Fella()
        {
            NoteDamageActivity();
            // 【口交·吸取】攻击也累积吸取欲 —— 齐平另外两栏（骑乘位/坐姿都有"攻击累积"）。
            // 原先口交只有"射精/吸精"两个来源，少了攻击这一档。
            // 注意：这里【不】判 DemandEnabled —— 那是背榨/正骑的开关，
            // 口交的吸取是独立的一套（它没有"进入模式"这个概念，只累积 + 叠层）。
            try
            {
                if (P3(AttackUrgeChance3) <= 0f || P3(AttackUrgeGain3) <= 0f) return;
                float j = Mathf.Clamp(P3(AttackUrgeJitter3), 0f, 90f) / 100f;
                float chance = Mathf.Clamp(P3(AttackUrgeChance3) * (1f + UnityEngine.Random.Range(-j, j)), 0f, 100f);
                if (UnityEngine.Random.value * 100f >= chance) return;
                float gain = Mathf.Max(0f, P3(AttackUrgeGain3) * (1f + UnityEngine.Random.Range(-j, j))) * UrgeScaleNow();
                float beforeF = _demandUrge;
                _demandUrge = Mathf.Max(0f, _demandUrge + gain);
                AuditUrge("口交攻击(命中 " + gain.ToString("0.##") + ")", beforeF, _demandUrge);
                TryStackNow("口交攻击");
            }
            catch { }
        }

        /// <summary>每帧推进：骑乘位结束后清理挂起状态。</summary>
        private void TickOsiriReturn()
        {
            if (!_sitDeferred) return;
            try
            {
                object tab = Tabemi();
                string cg = tab != null ? GetStringField(tab, "centerGirlState") : "";
                if (cg != "Osiri")
                {
                    _sitDeferred = false;
                    Log.LogInfo("[回归] 骑乘位结束 → 放行被挂起的坐姿");
                }
            }
            catch { }
        }

        /// <summary>
        /// 真正回到口交时，把"骑乘位再次出现"的门槛降低，让下一次更容易来。
        /// 门槛是 TabemiControl.OsiriPenaltyToAppear（第 1~4 天原本 10/9/8/7）。
        /// </summary>
        private static void OnReturnedToFella()
        {
            _fellaReturns++;
            ArmOsiriReturn();       // 从骑乘位回到口交 → 开始累积"再次进入骑乘位的概率"
            if (P3(DemandOsiriBoost3) <= 0) return;
            try
            {
                object tab = Tabemi();
                if (tab == null) return;
                int cur = (int)GetFloat(tab, "OsiriPenaltyToAppear");
                if (cur < 0) return;                       // -1 = 本作无 DLC，不干预
                int next = Mathf.Max(1, cur - P3(DemandOsiriBoost3));
                SetField(tab, "OsiriPenaltyToAppear", next);
                Log.LogInfo("[" + ModeWord() + "] 回到口交 → 骑乘位门槛 " + cur + " → " + next + "（下次更快来）");
            }
            catch { }
        }

        /// <summary>绝顶动画刚被播放时，立刻把倍速套上（不用等到下一帧）。</summary>
        private static void Postfix_PlayOsiriSyasei()
        {
            // 刚把绝顶动画播上这一层，此刻 CurrentState 就是它 —— 直接套倍速
            ApplyDemandAnimSpeed(true);
        }

        private static void PatchDemand()
        {
            Type at = FindType("Live2D_AnimationControl");
            Type tc2 = FindType("TabemiControl");
            Type hc = FindType("Live2D_HitAreaCheck");
            if (at == null) { Log.LogWarning("找不到 Live2D_AnimationControl，索取模式补丁未挂载"); return; }
            int ok = 0;

            // 打屁股：Osiri叩かれる 前后对比计数。
            // 【注意】它在 TabemiControl 上，不在 Live2D_AnimationControl 上 ——
            // 挂错类的话 Harmony 找不到方法，只会静默失败（这里加了明确告警）。
            Type tc = FindType("TabemiControl");
            MethodInfo hit = tc != null ? tc.GetMethod("Osiri叩かれる", AllFlags) : null;
            MethodInfo hitPre = typeof(Plugin).GetMethod("Prefix_OsiriHits", AllFlags);
            MethodInfo hitPost = typeof(Plugin).GetMethod("Postfix_OsiriHits", AllFlags);
            if (hit == null)
                Log.LogWarning("索取模式：找不到 TabemiControl.Osiri叩かれる（打屁股无法识别）");
            else if (hitPre == null || hitPost == null)
                Log.LogWarning("索取模式：回调方法未找到");
            else
            {
                _harmony.Patch(hit, new HarmonyMethod(hitPre), new HarmonyMethod(hitPost), null);
                ok++;
                Log.LogInfo("索取模式补丁已挂载（TabemiControl.Osiri叩かれる 前缀+后缀）");
            }

            // 骑乘位射精（索取模式真正要覆盖的那一支）
            // 【注意】它在 Live2D_Animation_SitOsiri 上，不在 Live2D_AnimationControl 上 ——
            // 三处方法分属三个类：
            //   Event_FellaSyaseiStart  → Live2D_AnimationControl
            //   Event_OsiriSyaseiStart  → Live2D_Animation_SitOsiri
            //   Osiri叩かれる / UpdateAtt → TabemiControl
            Type so = FindType("Live2D_Animation_SitOsiri");
            if (so == null) Log.LogWarning("索取模式：找不到 Live2D_Animation_SitOsiri");
            MethodInfo os = so != null ? so.GetMethod("Event_OsiriSyaseiStart", AllFlags) : null;
            MethodInfo osPre = typeof(Plugin).GetMethod("Prefix_OsiriSyaseiStart", AllFlags);
            MethodInfo osPost = typeof(Plugin).GetMethod("Postfix_OsiriSyaseiStart", AllFlags);
            if (os == null)
                Log.LogWarning("索取模式：找不到 Event_OsiriSyaseiStart（骑乘位射精不减速会失效）");
            else if (osPre == null || osPost == null)
                Log.LogWarning("索取模式：Osiri 回调未找到");
            else
            {
                _harmony.Patch(os, new HarmonyMethod(osPre), new HarmonyMethod(osPost), null);
                ok++;
                Log.LogInfo("索取模式补丁已挂载（Event_OsiriSyaseiStart 前缀+后缀：骑乘位射精不减速）");
            }

            // ---------- Sit（榨取模式）----------
            if (tc2 != null)
            {
                MethodInfo sh = tc2.GetMethod("頭叩かれるSit", AllFlags);
                MethodInfo shPre = typeof(Plugin).GetMethod("Prefix_SitHits", AllFlags);
                MethodInfo shPost = typeof(Plugin).GetMethod("Postfix_SitHits", AllFlags);
                if (sh != null && shPre != null && shPost != null)
                {
                    _harmony.Patch(sh, new HarmonyMethod(shPre), new HarmonyMethod(shPost), null);
                    ok++;
                    Log.LogInfo("榨取模式补丁已挂载（TabemiControl.頭叩かれるSit 前缀+后缀）");
                }
                else Log.LogWarning("榨取模式：找不到 頭叩かれるSit");
            }

            if (so != null)
            {
                // Sit 常态攻击 → 涨索取欲（六支攻击事件都挂）
                MethodInfo smPost = typeof(Plugin).GetMethod("Postfix_SitGirlMainMixer", AllFlags);
                foreach (string mn in new string[] { "Event_SitGirlMainMixerA1", "Event_SitGirlMainMixerA2",
                                                     "Event_SitGirlMainMixerA3", "Event_SitGirlMainMixerB1",
                                                     "Event_SitGirlMainMixerB2", "Event_SitGirlMainMixerB3",
                                                     "Event_SitGirlMainMixerKissA1", "Event_SitGirlMainMixerKissB1" })
                {
                    MethodInfo mm = so.GetMethod(mn, AllFlags);
                    if (mm != null && smPost != null) { _harmony.Patch(mm, null, new HarmonyMethod(smPost), null); ok++; }
                }
                Log.LogInfo("榨取模式补丁已挂载（Sit 攻击事件后缀：涨索取欲）");

                // Sit 射精不减速（两个 Start 事件都会 SetSpeed_Sit(sitSpeed/2f)）
                MethodInfo ssPre = typeof(Plugin).GetMethod("Prefix_SitSyaseiStart", AllFlags);
                MethodInfo ssPost = typeof(Plugin).GetMethod("Postfix_SitSyaseiStart", AllFlags);
                foreach (string mn in new string[] { "Event_SitSyaseiStart", "Event_SitKissSyaseiStart" })
                {
                    MethodInfo mm = so.GetMethod(mn, AllFlags);
                    if (mm != null && ssPre != null && ssPost != null)
                    {
                        _harmony.Patch(mm, new HarmonyMethod(ssPre), new HarmonyMethod(ssPost), null);
                        ok++;
                    }
                }
                Log.LogInfo("榨取模式补丁已挂载（Sit 射精不减速）");

                // Sit 吸精收尾接管（原版永远重播）
                MethodInfo se = so.GetMethod("Even_SitKiss吸精OnEnd", AllFlags);
                MethodInfo sePre = typeof(Plugin).GetMethod("Prefix_EvenSitKissKyuseiOnEnd", AllFlags);
                if (se != null && sePre != null)
                {
                    _harmony.Patch(se, new HarmonyMethod(sePre), null, null);
                    ok++;
                    Log.LogInfo("榨取模式补丁已挂载（Even_SitKiss吸精OnEnd 前缀：接管 Sit 连榨上限）");
                }
                else Log.LogWarning("榨取模式：找不到 Even_SitKiss吸精OnEnd");
            }

            // 榨取伤害事件 → 按对生命值的影响折算绝顶值
            MethodInfo od = so != null ? so.GetMethod("Event_Osiri吸精Damage", AllFlags) : null;
            MethodInfo odPost = typeof(Plugin).GetMethod("Postfix_Osiri吸精Damage", AllFlags);
            if (od != null && odPost != null)
            {
                _harmony.Patch(od, null, new HarmonyMethod(odPost), null); ok++;
                Log.LogInfo("连榨绝顶值补丁已挂载（Event_Osiri吸精Damage 后缀：按伤害折算）");
            }
            else Log.LogWarning("连榨绝顶值：找不到 Event_Osiri吸精Damage");

            MethodInfo sd = so != null ? so.GetMethod("Event_SitKiss吸精1", AllFlags) : null;
            MethodInfo sdPost = typeof(Plugin).GetMethod("Postfix_SitKiss吸精1", AllFlags);
            if (sd != null && sdPost != null)
            {
                _harmony.Patch(sd, null, new HarmonyMethod(sdPost), null); ok++;
                Log.LogInfo("连榨绝顶值补丁已挂载（Event_SitKiss吸精1 后缀：按伤害折算）");
            }
            else Log.LogWarning("连榨绝顶值：找不到 Event_SitKiss吸精1");

            MethodInfo fd = at != null ? at.GetMethod("Event_Fella吸精", AllFlags) : null;
            MethodInfo fdPost = typeof(Plugin).GetMethod("Postfix_Fella吸精", AllFlags);
            if (fd != null && fdPost != null)
            {
                _harmony.Patch(fd, null, new HarmonyMethod(fdPost), null); ok++;
                Log.LogInfo("连榨绝顶值补丁已挂载（Event_Fella吸精 后缀：按伤害×吸精攻击率折算）");
            }
            else Log.LogWarning("连榨绝顶值：找不到 Event_Fella吸精");

            // 【修 #8】坐姿射精收尾 → 可进榨取（原版永远回坐姿攻击）
            MethodInfo sse = so != null ? so.GetMethod("Event_SitSyasei_OnEnd", AllFlags) : null;
            MethodInfo ssePre = typeof(Plugin).GetMethod("Prefix_Event_SitSyasei_OnEnd", AllFlags);
            if (sse != null && ssePre != null)
            {
                _harmony.Patch(sse, new HarmonyMethod(ssePre), null, null); ok++;
                Log.LogInfo("榨取模式补丁已挂载（Event_SitSyasei_OnEnd 前缀：坐姿射精可进榨取）");
            }
            else Log.LogWarning("榨取模式：找不到 Event_SitSyasei_OnEnd");

            // 坐姿挂起拦截 + 口交伤害计数
            if (tc2 != null)
            {
                MethodInfo sf = tc2.GetMethod("ShowCenterGirlFella", AllFlags);
                MethodInfo sfPre = typeof(Plugin).GetMethod("Prefix_ShowCenterGirlFella", AllFlags);
                if (sf != null && sfPre != null)
                {
                    _harmony.Patch(sf, new HarmonyMethod(sfPre), null, null); ok++;
                    Log.LogInfo("余韵补丁已挂载（ShowCenterGirlFella 前缀：余韵清空前不进口交）");
                }

                MethodInfo sc = tc2.GetMethod("Show_CenterGirlSit", AllFlags);
                MethodInfo scPre = typeof(Plugin).GetMethod("Prefix_ShowCenterGirlSit", AllFlags);
                if (sc != null && scPre != null)
                {
                    _harmony.Patch(sc, new HarmonyMethod(scPre), null, null); ok++;
                    Log.LogInfo("回归补丁已挂载（Show_CenterGirlSit 前缀：坐姿挂起）");
                }
            }
            MethodInfo df = tc2 != null ? tc2.GetMethod("DealDamage_Fella", AllFlags) : null;
            MethodInfo dfPost = typeof(Plugin).GetMethod("Postfix_DealDamage_Fella", AllFlags);
            if (df != null && dfPost != null)
            {
                _harmony.Patch(df, null, new HarmonyMethod(dfPost), null); ok++;
                Log.LogInfo("回归补丁已挂载（DealDamage_Fella 后缀：口交造成伤害也计数）");
            }
            else Log.LogWarning("回归：找不到 DealDamage_Fella");

            // 连榨强制第 4 档撞击声
            if (so != null)
            {
                MethodInfo ose = so.GetMethod("OsiriSE", AllFlags);
                MethodInfo osePre = typeof(Plugin).GetMethod("Prefix_OsiriSE", AllFlags);
                if (ose != null && osePre != null)
                {
                    _harmony.Patch(ose, new HarmonyMethod(osePre), null, null); ok++;
                    Log.LogInfo("撞击声补丁已挂载（OsiriSE 前缀：连榨用 3/4 档，其余默认）");
                }
                else Log.LogWarning("撞击声：找不到 OsiriSE");
            }

            // 束缚之吻：拉长窗口 + 阻止衰减
            // 束缚之吻：射精那一半
            // 【注意类归属】Event_SitKissSyaseiOnEnd 与 Play_SitKissSyasei 在
            // Live2D_Animation_SitOsiri 上（跟吸精那条同一个类），不在 TabemiControl。
            // 而 DealDamage_SitKiss 在 TabemiControl 上。挂错类只是静默失败。
            if (so != null)
            {
                MethodInfo ke = so.GetMethod("Event_SitKissSyaseiOnEnd", AllFlags);
                MethodInfo kePre = typeof(Plugin).GetMethod("Prefix_SitKissSyaseiOnEnd", AllFlags);
                if (ke != null && kePre != null)
                {
                    _harmony.Patch(ke, new HarmonyMethod(kePre), null, null); ok++;
                    Log.LogInfo("束缚之吻补丁已挂载（Event_SitKissSyaseiOnEnd 前缀：射精也受连榨上限管制）");
                }
                else Log.LogWarning("束缚之吻：找不到 Event_SitKissSyaseiOnEnd");

                MethodInfo ps = so.GetMethod("Play_SitKissSyasei", AllFlags);
                MethodInfo psPost = typeof(Plugin).GetMethod("Postfix_PlaySitKissSyasei", AllFlags);
                if (ps != null && psPost != null)
                {
                    _harmony.Patch(ps, null, new HarmonyMethod(psPost), null); ok++;
                    Log.LogInfo("束缚之吻补丁已挂载（Play_SitKissSyasei 后缀：绝顶动画可调速）");
                }
                else Log.LogWarning("束缚之吻：找不到 Play_SitKissSyasei");
            }
            if (tc2 != null)
            {
                MethodInfo dd = tc2.GetMethod("DealDamage_SitKiss", AllFlags);
                MethodInfo ddPost = typeof(Plugin).GetMethod("Postfix_DealDamage_SitKiss", AllFlags);
                if (dd != null && ddPost != null)
                {
                    _harmony.Patch(dd, null, new HarmonyMethod(ddPost), null); ok++;
                    Log.LogInfo("束缚之吻补丁已挂载（DealDamage_SitKiss 后缀：伤害计入回归累积）");
                }
            }

            // 汉堡制作倒计时
            {
                Type mc = FindType("MenuControl");
                MethodInfo tm = mc != null ? mc.GetMethod("Timing", AllFlags) : null;
                MethodInfo tmPre = typeof(Plugin).GetMethod("Prefix_MenuTiming", AllFlags);
                if (tm != null && tmPre != null)
                {
                    _harmony.Patch(tm, new HarmonyMethod(tmPre), null, null); ok++;
                    Log.LogInfo("倒计时补丁已挂载（MenuControl.Timing 前缀：每单时长 / 速度）");
                }
                else Log.LogWarning("倒计时：找不到 MenuControl.Timing");
            }

            // 坐姿锁定：两个出口
            if (tc2 != null)
            {
                MethodInfo gf = tc2.GetMethod("ShowCenterGirlFella", AllFlags);
                MethodInfo gfPre = typeof(Plugin).GetMethod("Prefix_ShowCenterGirlFella", AllFlags);
                if (gf != null && gfPre != null)
                {
                    _harmony.Patch(gf, new HarmonyMethod(gfPre), null, null); ok++;
                    Log.LogInfo("坐姿锁定补丁已挂载（ShowCenterGirlFella 前缀）");
                }
                MethodInfo go = tc2.GetMethod("ShowCenterGirlOsiri", AllFlags);
                MethodInfo goPre = typeof(Plugin).GetMethod("Prefix_ShowCenterGirlOsiri", AllFlags);
                if (go != null && goPre != null)
                {
                    _harmony.Patch(go, new HarmonyMethod(goPre), null, null); ok++;
                    Log.LogInfo("坐姿锁定补丁已挂载（ShowCenterGirlOsiri 前缀）");
                }
            }

            if (tc2 != null)
            {
                MethodInfo pk = tc2.GetMethod("PrepareKissing", AllFlags);
                MethodInfo pkPost = typeof(Plugin).GetMethod("Postfix_PrepareKissing", AllFlags);
                if (pk != null && pkPost != null)
                {
                    _harmony.Patch(pk, null, new HarmonyMethod(pkPost), null); ok++;
                    Log.LogInfo("束缚之吻补丁已挂载（PrepareKissing 后缀：拉长窗口）");
                }
                MethodInfo kk = tc2.GetMethod("KissPrepare解除", AllFlags);
                MethodInfo kkPre = typeof(Plugin).GetMethod("Prefix_KissPrepareKaijo", AllFlags);
                MethodInfo kkPost = typeof(Plugin).GetMethod("Postfix_KissPrepareKaijo", AllFlags);
                if (kk != null && kkPre != null && kkPost != null)
                {
                    _harmony.Patch(kk, new HarmonyMethod(kkPre), new HarmonyMethod(kkPost), null); ok++;
                    Log.LogInfo("束缚之吻补丁已挂载（KissPrepare解除 前缀+后缀：阻止窗口衰减）");
                }
                else Log.LogWarning("束缚之吻：找不到 KissPrepare解除");
            }

            // 坐姿小穴点按区（拦截 Live2D 命中结果）
            if (hc != null)
            {
                // 【鼠标这条路】每帧算完静态 mousePointing 之后再补一刀
                MethodInfo hcw = hc.GetMethod("HitAreaCheckWindows", AllFlags);
                MethodInfo hcwPost = typeof(Plugin).GetMethod("Postfix_HitAreaCheckWindows", AllFlags);
                if (hcw != null && hcwPost != null)
                {
                    _harmony.Patch(hcw, null, new HarmonyMethod(hcwPost), null); ok++;
                    Log.LogInfo("点按区补丁已挂载（HitAreaCheckWindows 后缀：HitArea_Manman 鼠标路径）");
                }
                else Log.LogWarning("点按区：找不到 HitAreaCheckWindows（鼠标路径接不上）");

                // 【HitArea_Manman 自己的点击处理】
                Type mk = FindType("MenuController") ?? FindType("ClickControl");
                if (mk == null)
                {
                    // 宿主类名不确定，就按方法名全局找
                    foreach (Type t in AppDomain.CurrentDomain.GetAssemblies()
                             .SelectMany(a => { try { return a.GetTypes(); } catch { return new Type[0]; } }))
                    {
                        if (t.GetMethod("Mouse_Down_0", AllFlags) != null) { mk = t; break; }
                    }
                }
                if (mk != null)
                {
                    MethodInfo md = mk.GetMethod("Mouse_Down_0", AllFlags);
                    MethodInfo mdPre = typeof(Plugin).GetMethod("Prefix_Mouse_Down_0", AllFlags);
                    if (md != null && mdPre != null)
                    {
                        _harmony.Patch(md, new HarmonyMethod(mdPre), null, null); ok++;
                        Log.LogInfo("点按区补丁已挂载（Mouse_Down_0 前缀：HitArea_Manman 独立处理）");
                    }
                    else Log.LogWarning("点按区：找不到 Mouse_Down_0 —— HitArea_Manman 不会响应");
                }
                else Log.LogWarning("点按区：找不到 Mouse_Down_0 的宿主类 —— HitArea_Manman 不会响应");

                MethodInfo gt = hc.GetMethod("GetTouchTargetName", AllFlags);
                MethodInfo gtPost = typeof(Plugin).GetMethod("Postfix_GetTouchTargetName", AllFlags);
                if (gt != null && gtPost != null)
                {
                    _harmony.Patch(gt, null, new HarmonyMethod(gtPost), null); ok++;
                    Log.LogInfo("点按区补丁已挂载（GetTouchTargetName 后缀：坐姿小穴区）");
                }
                else Log.LogWarning("点按区：找不到 GetTouchTargetName");
            }

            // 常态攻击事件：低概率涨索取欲
            MethodInfo gm2 = so != null ? so.GetMethod("Event_OsiriGirlMainMixer", AllFlags) : null;
            MethodInfo gm2Post = typeof(Plugin).GetMethod("Postfix_OsiriGirlMainMixer", AllFlags);
            if (gm2 != null && gm2Post != null)
            {
                _harmony.Patch(gm2, null, new HarmonyMethod(gm2Post), null);
                ok++;
                Log.LogInfo("索取模式补丁已挂载（Event_OsiriGirlMainMixer 后缀：每次攻击低概率涨索取欲）");
            }
            else Log.LogWarning("索取模式：找不到 Event_OsiriGirlMainMixer");

            // 绝顶收尾的三岔路口（管"回口交"的概率）
            MethodInfo oe = so != null ? so.GetMethod("Event_OsiriSyaseiOnEnd", AllFlags) : null;
            MethodInfo oePre = typeof(Plugin).GetMethod("Prefix_Event_OsiriSyaseiOnEnd", AllFlags);
            if (oe != null && oePre != null)
            {
                _harmony.Patch(oe, new HarmonyMethod(oePre), null, null);
                ok++;
                Log.LogInfo("索取模式补丁已挂载（Event_OsiriSyaseiOnEnd 前缀：接管回口交概率）");
            }
            else Log.LogWarning("索取模式：找不到 Event_OsiriSyaseiOnEnd");

            // 骑乘位吸精收尾（连榨上限真正该管的地方）
            MethodInfo oke = so != null ? so.GetMethod("Event_Osiri吸精End", AllFlags) : null;
            MethodInfo okePre = typeof(Plugin).GetMethod("Prefix_Event_Osiri吸精End", AllFlags);
            if (oke != null && okePre != null)
            {
                _harmony.Patch(oke, new HarmonyMethod(okePre), null, null);
                ok++;
                Log.LogInfo("索取模式补丁已挂载（Event_Osiri吸精End 前缀：接管连榨上限）");
            }
            else Log.LogWarning("索取模式：找不到 Event_Osiri吸精End（骑乘位的连榨上限会失效）");

            // Osiri解除：索取模式期间拦住（并记录真正的回口交）
            MethodInfo kaijo = tc2 != null ? tc2.GetMethod("Osiri解除", AllFlags) : null;
            MethodInfo kaijoPre = typeof(Plugin).GetMethod("Prefix_Osiri解除", AllFlags);
            MethodInfo kaijoPost = typeof(Plugin).GetMethod("Postfix_Osiri解除", AllFlags);
            if (kaijo != null && kaijoPre != null && kaijoPost != null)
            {
                _harmony.Patch(kaijo, new HarmonyMethod(kaijoPre), new HarmonyMethod(kaijoPost), null);
                ok++;
                Log.LogInfo("索取模式补丁已挂载（TabemiControl.Osiri解除 前缀+后缀：索取期间不脱出）");
            }
            else Log.LogWarning("索取模式：找不到 TabemiControl.Osiri解除");

            // 绝顶动画播放时立即套倍速
            MethodInfo ps2 = so != null ? so.GetMethod("Play_OsiriSyasei", AllFlags) : null;
            MethodInfo ps2Post = typeof(Plugin).GetMethod("Postfix_PlayOsiriSyasei", AllFlags);
            if (ps2 != null && ps2Post != null)
            {
                _harmony.Patch(ps2, null, new HarmonyMethod(ps2Post), null);
                ok++;
                Log.LogInfo("索取模式补丁已挂载（Play_OsiriSyasei 后缀：绝顶动画倍速）");
            }
            else Log.LogWarning("索取模式：找不到 Live2D_Animation_SitOsiri.Play_OsiriSyasei");

            // 攻击力加成：TabemiControl.UpdateAtt 的后缀
            tc2 = FindType("TabemiControl");
            MethodInfo ua = tc2 != null ? tc2.GetMethod("UpdateAtt", AllFlags) : null;
            MethodInfo uaPost = typeof(Plugin).GetMethod("Postfix_UpdateAtt", AllFlags);
            if (ua == null || uaPost == null)
                Log.LogWarning("索取模式：找不到 TabemiControl.UpdateAtt（攻击力加成会失效）");
            else
            {
                _harmony.Patch(ua, null, new HarmonyMethod(uaPost), null);
                ok++;
                Log.LogInfo("索取模式补丁已挂载（TabemiControl.UpdateAtt 后缀：索取模式期间攻击力提升）");
            }

            // 口交分支（脱出骑乘位之后会走到这里）
            MethodInfo sy = at.GetMethod("Event_FellaSyaseiStart", AllFlags);
            MethodInfo syPre = typeof(Plugin).GetMethod("Prefix_FellaSyaseiStart", AllFlags);
            MethodInfo syPost = typeof(Plugin).GetMethod("Postfix_FellaSyaseiStart", AllFlags);
            if (sy != null && syPre != null && syPost != null)
            {
                _harmony.Patch(sy, new HarmonyMethod(syPre), new HarmonyMethod(syPost), null);
                ok++;
                Log.LogInfo("索取模式补丁已挂载（Event_FellaSyaseiStart 前缀+后缀：索取模式期间不减速）");
            }

            if (ok == 0) Log.LogWarning("索取模式：一个挂点都没挂上");
        }

        // =================================================================
        // 修正游戏的「吸精发生率」整数除法 bug
        //
        // 游戏原码：
        //     public int KyuseiRate = 50;
        //     else if (UnityEngine.Random.value < (float)(KyuseiRate / 100))
        //                                          ↑ int / int = 整数除法
        //
        // 于是这个设置项是坏的：
        //     1~99  → KyuseiRate/100 == 0 → Random.value < 0 恒假 → 永不连续（设置形同虚设）
        //     100   → KyuseiRate/100 == 1 → Random.value < 1 恒真 → 【无限连榨】
        //
        // 100 的那一支会把榨取变成永不结束的循环：每轮闪一次红光、Syaseing 永不清除，
        // 而游戏里有 8 处判定在 if (player.Syaseing) 上 —— 状态迁移全被挡掉。
        // 玩家看到的就是"持续被榨精 / 持续闪红光 / 改什么状态都不生效"。
        //
        // 修法：不去重写它的算式，而是【借用】这段整数除法 ——
        // 在前缀里按真实概率自己掷骰，再临时把字段设成能触发目标分支的值（真→100，假→99），
        // 后缀立刻还原。这样设置界面上的百分比终于按字面意思生效。
        // =================================================================
        private static readonly List<int> _kyuseiBackup = new List<int>();
        private static int _kyuseiChain;          // 当前这一串连榨已经连了几次

        /// <summary>前缀：按真实百分比掷骰，借用整数除法走对应分支。</summary>
        private static void Prefix_KyuseiRateRoll(object __instance)
        {
            try
            {
                int raw = (int)GetFloat(__instance, "KyuseiRate");
                _kyuseiBackup.Add(raw);

                // 真实概率：发生率 50 就是 50%
                bool repeat = UnityEngine.Random.value * 100f < Mathf.Clamp(raw, 0, 100);

                // 余韵：强制连续吸精，次数由索取模式结束时抽定。且不受连榨上限约束
                // （8~20 次是设计值，不该被"防卡死"的上限砍掉）。
                if (_afterglowRemaining > 0 && !_demandMode)
                {
                    repeat = true;
                    _afterglowRemaining--;
                    AddChainGain(Player());
                    ShowChainBanner(string.Format("余韵 · 剩余 {0} 次", _afterglowRemaining));
                    Log.LogInfo("[" + ModeWord() + "] 余韵中，本次强制连榨（剩 " + _afterglowRemaining + " 次）");
                }
                else if (repeat && ChainLimitNow() > 0 && ChainNow() >= ChainLimitNow())
                {
                    // 连榨上限：兜住"发生率 100% = 无限连榨"这种会把游戏卡死的极端情况
                    repeat = false;
                    Log.LogInfo("[吸精] 已达连榨上限 " + ChainLimitNow() + " 次（" + ChainSlotName() + "），强制收尾");
                }

                if (_afterglowRemaining > 0 && !_demandMode) { /* 余韵不参与链计数 */ }
                else if (repeat)
                {
                    ChainSet(ChainNow() + 1);
                    ShowChainBanner(ChainLimitNow() > 0
                        ? string.Format("连榨 · 剩余 {0} 次", Mathf.Max(0, ChainLimitNow() - ChainNow()))
                        : "连榨");
                }
                else _kyuseiChain = 0;

                // 借用整数除法：99/100=0（不重播），100/100=1（重播）
                FieldInfo fi = FieldQuiet(__instance.GetType(), "KyuseiRate");
                if (fi != null) fi.SetValue(__instance, repeat ? 100 : 99);
            }
            catch { _kyuseiBackup.Add(-1); }
        }

        /// <summary>后缀：无条件还原（无论原方法走哪条分支、是否抛异常）。</summary>
        private static void Postfix_KyuseiRateRestore(object __instance)
        {
            try
            {
                if (_kyuseiBackup.Count == 0) return;
                int raw = _kyuseiBackup[_kyuseiBackup.Count - 1];
                _kyuseiBackup.RemoveAt(_kyuseiBackup.Count - 1);
                if (raw < 0) return;
                FieldInfo fi = FieldQuiet(__instance.GetType(), "KyuseiRate");
                if (fi != null) fi.SetValue(__instance, raw);
            }
            catch { }
        }

        private static void PatchKyuseiRate()
        {
            Type at = FindType("Live2D_AnimationControl");
            if (at == null) { Log.LogWarning("找不到 Live2D_AnimationControl，吸精发生率修正未挂载"); return; }
            MethodInfo pre = typeof(Plugin).GetMethod("Prefix_KyuseiRateRoll", AllFlags);
            MethodInfo post = typeof(Plugin).GetMethod("Postfix_KyuseiRateRestore", AllFlags);
            if (pre == null || post == null) { Log.LogWarning("吸精发生率修正：回调未找到"); return; }

            int ok = 0;
            // 两个判定点：Fella 的吸精收尾、以及另一处同样的判定
            // 判定点只有这两个 —— grep KyuseiRate 全代码只出现在这两处的表达式里：
            //   (1) Event_Fella吸精End      —— 吸精收尾时的重播判定
            //   (2) Event_FellaSyaseiOnEnd  —— 绝顶收尾时的「吸精フェラflag && KyuseiRate/100」判定
            // （Event_Osiri吸精End 与 Even_SitKiss吸精OnEnd 都不读 KyuseiRate，不要挂；
            //   Play_Fella吸精 是开始入口，也不读，挂了只会造成嵌套改写。）
            foreach (string mn in new string[] { "Event_Fella吸精End", "Event_FellaSyaseiOnEnd" })
            {
                MethodInfo m = at.GetMethod(mn, AllFlags);
                if (m == null) continue;
                _harmony.Patch(m, new HarmonyMethod(pre), new HarmonyMethod(post), null);
                ok++;
                Log.LogInfo("吸精发生率修正已挂载（" + mn + "）");
            }
            if (ok == 0) Log.LogWarning("吸精发生率修正：一个判定点都没挂上");
        }

        private static void PatchTremor()
        {
            Type pc = FindType("PlayerControl");
            if (pc == null) { Log.LogWarning("找不到 PlayerControl，动摇补丁未挂载"); return; }
            try
            {
                MethodInfo target = pc.GetMethod("EcstasyChange", AllFlags);
                MethodInfo pre = typeof(Plugin).GetMethod("Prefix_EcstasyRecord", AllFlags);
                MethodInfo post = typeof(Plugin).GetMethod("Postfix_EcstasyChange", AllFlags);
                if (target == null || pre == null || post == null) { Log.LogWarning("动摇补丁：方法未找到"); return; }
                _harmony.Patch(target, new HarmonyMethod(pre), new HarmonyMethod(post), null);

                MethodInfo fx = pc.GetMethod("PlayEsctasyDamageEffect", AllFlags);
                MethodInfo fxPre = typeof(Plugin).GetMethod("Prefix_PlayEsctasyDamageEffect", AllFlags);
                if (fx != null && fxPre != null)
                    _harmony.Patch(fx, new HarmonyMethod(fxPre), null, null);

                // 榨取红光特效在 EffectsControl 上，单独挂
                Type ec = FindType("EffectsControl");
                if (ec != null)
                {
                    MethodInfo ps = ec.GetMethod("PlaySyaseiEffect", AllFlags);
                    MethodInfo psPre = typeof(Plugin).GetMethod("Prefix_PlaySyaseiEffect", AllFlags);
                    if (ps != null && psPre != null)
                    {
                        _harmony.Patch(ps, new HarmonyMethod(psPre), null, null);
                        Log.LogInfo("红光特效计数已挂载（EffectsControl.PlaySyaseiEffect）");
                    }

                    // 真正的吸精红光
                    MethodInfo ky = ec.GetMethod("Play吸精Effect", AllFlags);
                    MethodInfo kyPre = typeof(Plugin).GetMethod("Prefix_Play吸精Effect", AllFlags);
                    if (ky != null && kyPre != null)
                    {
                        _harmony.Patch(ky, new HarmonyMethod(kyPre), null, null);
                        Log.LogInfo("吸精红光计数已挂载（EffectsControl.Play吸精Effect）");
                    }
                }

                Log.LogInfo("绝顶动摇补丁已挂载（EcstasyChange 前缀+后缀，PlayEsctasyDamageEffect 前缀）");
            }
            catch (Exception e) { Log.LogWarning("挂动摇补丁失败：" + e.Message); }
        }

        internal static object PlayerRef() { return Player(); }

        /// <summary>把一个字段读成可读文本（枚举取名字，数值直接给）。</summary>
        internal static string GetStateFieldText(object obj, string name)
        {
            try
            {
                FieldInfo fi = FieldQuiet(obj.GetType(), name);
                if (fi == null) return "-";
                object v = fi.GetValue(obj);
                if (v == null) return "null";
                if (v is float f) return f.ToString("0.##");
                if (v is int i) return i.ToString();
                if (v is bool b) return b ? "真" : "假";
                return v.ToString();
            }
            catch { return "ERR"; }
        }
        internal static float AccumInterval() { return _accumInterval; }
        /// <summary>
        /// 把所有"玩法调参"恢复到代码里的默认值。
        ///
        /// 动机：我（AI）在调试时会用 set 改这些值（比如把索取欲上下限设成 100 来强制触发、
        /// 把连榨上限设成 0 来排除干扰），**改完常常忘了还原**，于是玩家看到的是调试配置
        /// 而不是默认配置 —— 已经因此误报过两次 bug（"索取欲只显示 0%"、"连榨不显示次数"）。
        /// 有了这个命令，随时一句就能回到干净状态。
        /// </summary>
        internal static string RestoreDefaults()
        {
            var sb = new StringBuilder("已恢复默认：");
            try
            {
                TremorEnabled.Value = true; TremorChance.Value = 22f; TremorDuration.Value = 1.6f;
                TremorAmplitude.Value = 90f; TremorLoss.Value = 85f; TremorCatch.Value = 8f;
                TremorCooldown.Value = 2.5f; TremorSlow.Value = 30f; TremorPattern.Value = 0;
                TremorWeaken.Value = 25f; TremorAdaptTime.Value = 4f; TremorAdaptFactor.Value = 35f;
                TremorHpMaxBonus.Value = 20f; TremorHpGuard.Value = 35f; TremorHpRegen.Value = 6f;
                TremorSafetyGate.Value = 90f;
                TremorZeroHold.Value = true; TremorZeroHoldTime.Value = 8f; TremorZeroHoldAmp.Value = 8f;
                sb.Append(" 动摇/适应");

                SetP3(ChainEcstasyGain3, true); SetP3(ChainGainPerDrain3, 8f); SetP3(ChainGainPerHp3, 100f);
                SetP3(ChainSoftCap3, 60f); SetP3(ChainWobble3, 20f); SetP3(ChainWobbleHz3, 0.8f);
                ChainMaxFella.Value = 6;
                ChainMaxOsiriNormal.Value = 6; ChainMaxOsiriDemand.Value = 6;
                ChainMaxSitNormal.Value = 6; ChainMaxSitDemand.Value = 6;
                sb.Append(" 连榨");

                SetP3(DemandEnabled3, true); SetP3(DemandUrgeMin3, 8f); SetP3(DemandUrgeMax3, 22f);
                SetP3(DemandEscapePenaltyChance3, 35f); SetP3(DemandEscapePenaltyMul3, 135f);
                SetP3(DemandAttBonus3, 50f); SetP3(DemandAttSpeedBonus3, 60f); SetP3(DemandAttSpeedRef3, 50f);
                SetP3(DemandAnimSpeed3, 150f); SetP3(DemandSpeedSplit3, true);
                SetP3(DemandBlockFella3, true); SetP3(DemandFellaDecay3, 25f); SetP3(DemandOsiriBoost3, 1);
                SetP3(DemandDurationMin3, 12f); SetP3(DemandDurationMax3, 25f);
                DemandAfterglowMin.Value = 8; DemandAfterglowMax.Value = 20;
                SetP3(AfterglowPerSyasei3, 2); SetP3(AfterglowSettleDelay3, 0.6f);
                SetP3(AfterglowSpeed3, 70f); SetP3(AfterglowUseDemandGain3, true);
                SetP3(AfterglowSpeedWobble3, 25f); SetP3(AfterglowSpeedHz3, 0.35f);
                SetP3(AfterglowSoftCap3, 50f); SetP3(AfterglowEcstasyWobble3, 15f);
                _afterglowPending = 0; _demandSyaseiCount = 0; _afterglowSettleAt = -999f;
                _demandEntered = false; _afterglowRemaining = 0;
                SetP3(DemandMaxStacks3, 5);
                sb.Append(" 模式参数（三栏共用一份）");

                SetP3(AttackUrgeChance3, 8f); SetP3(AttackUrgeGain3, 3f); SetP3(AttackUrgeJitter3, 50f);
                SetP3(SyaseiUrgeChance3, 100f); SetP3(SyaseiUrgeGain3, 8f);
                SetP3(KyuseiUrgeChance3, 100f); SetP3(KyuseiUrgeGain3, 5f);
                SetP3(SpankSpeedEnabled3, true); SetP3(SpankSpeedGain3, 12f); SetP3(SpankSpeedStack3, 60f);
                SetP3(SpankSpeedRise3, 9f); SetP3(SpankSpeedDecay3, 0.25f);
                sb.Append(" 索取欲/冲量");

                _demandUrge = 0f; _demandStacks = 0; _spankMul = 1f; _spankTarget = 1f;
                _spankAdd = 0f; _spankCount = 0; ChainResetAll();

                string note2 = SaveCfgSafely();
                sb.Append("（已落盘）").Append(note2);
            }
            catch (Exception e) { sb.Append(" ERR ").Append(e.Message); }
            return sb.ToString();
        }

        internal static void SimulateSpank() { DoSpankLogic(Tabemi()); }
        internal static void EnableUrgeAudit(int n) { _urgeAuditLeft = Mathf.Clamp(n, 1, 500); }
        internal static float DemandUrge { get { return _demandUrge; } }
        // 索取模式与常规状态的连榨计数【分开算】
        /// <summary>当前是不是坐姿（Sit 与 Osiri 在同一个类里，只能从状态机字段判）。</summary>
        /// <summary>0 = 口交(Fella)，1 = 背榨(Osiri)，2 = 正骑(Sit)。</summary>
        internal static int PoseIdx()
        {
            try
            {
                object tab = Tabemi();
                if (tab == null) return 1;
                string cg = GetStringField(tab, "centerGirlState");
                if (cg == "Sit") return 2;
                if (cg == "Osiri") return 1;
                return 0;
            }
            catch { return 1; }
        }

        internal static bool PoseIsSit() { return PoseIdx() == 2; }

        /// <summary>
        /// 【插件自建的 Manman 区在游戏里的名字】
        ///
        /// 起这个名字是为了让它和游戏原生的那批【同级】：
        ///     HitArea_Head / HitArea_Head_Sit / HitArea_Osiri
        ///     HitArea_LeftGirl / HitArea_RightGirl
        ///     HitArea_Manman_By_Plugins        ← 本插件提供
        ///
        /// 后缀标明来源 —— 免得日后误以为模型里真有这么一块。
        /// 注意：模型里【将来真有了】HitArea_Manman（用户自建）时，
        /// 判定会优先用模型那块（见 PussyAreaScreenRect），这个名字让二者不会撞车。
        /// </summary>
        internal const string ManmanAreaName = "HitArea_Manman_By_Plugins";

        // 给 GameCommandServer 用的包装（它拿不到 private 成员）
        internal static int PoseIdxPublic() { return PoseIdx(); }
        internal static string DumpBindingsPublic() { return GameBindings.Dump(FindType); }
        internal static bool ManmanEnabledPublic() { return SitPussyAreaEnabled.Value; }
        internal static void SetSitLockPublic(bool on) { SitLockEnabled.Value = on; }
        internal static Type FindTypePublic(string n) { return FindType(n); }
        internal static FieldInfo FieldQuietPublic(Type t, string n) { return FieldQuiet(t, n); }
        internal static string ManmanSourcePublic()
        {
            try
            {
                float q0, q1, q2, q3;
                if (TryGetDrawableScreenRect("HitArea_Manman", out q0, out q1, out q2, out q3))
                    return "model:HitArea_Manman";
                if (!string.IsNullOrEmpty(ManmanBindDrawable.Value)) return "bound:" + ManmanBindDrawable.Value;
            }
            catch { }
            return "plugin-rect";
        }
        internal static bool ManmanRectPublic(out string txt)
        {
            float x0, y0, x1, y1;
            if (PussyAreaScreenRect(out x0, out y0, out x1, out y1))
            {
                txt = string.Format("{0:0}~{1:0}x{2:0}~{3:0}", x0, x1, y0, y1);
                Vector3 mp = Input.mousePosition;
                return mp.x >= x0 && mp.x <= x1 && mp.y >= y0 && mp.y <= y1;
            }
            txt = "(unavailable)";
            return false;
        }

        /// <summary>
        /// 【机制当前是否生效】—— 这是"把滑块绑到束缚之吻上"的关键。
        ///
        /// 原来各处都直接判 `_demandMode`，但正骑的榨取是从
        /// `Play_SitKiss吸精()` 进的（它把 kissing 强制置 2），**并不置 `_demandMode`** ——
        /// 于是正骑栏里那些滑块（攻击力、动画倍速、连榨软上限…）在束缚之吻里全都不生效。
        ///
        /// 现在统一成：
        ///   · 索取/榨取模式中  → 生效
        ///   · 正骑 + 束缚之吻中（kissing != 0）→ 生效
        ///   · 正骑 + 榨取中（sitState == kyusei）→ 生效
        /// </summary>
        /// <summary>
        /// 连榨该用哪一档（常规 / 索取·榨取）。
        ///
        /// 【不能用 _demandMode】—— 正骑的榨取是从 Play_SitKiss吸精() 进的，
        /// 它把 kissing 强制置 2，但【不置 _demandMode】。
        /// 只看 _demandMode 的话，正骑的连榨会落到"常规"档，
        /// 于是被 ChainMaxSitNormal（默认/实测都是个位数）提前掐断 ——
        /// 表现就是"连榨被莫名提前终止"。
        /// </summary>
        private static bool DrainSlotIsDemand()
        {
            if (_demandMode) return true;
            try
            {
                if (PoseIdx() != 2) return false;
                object tab = Tabemi();
                if (tab == null) return false;
                if (GetStringField(tab, "sitState") == "kyusei") return true;   // 榨取中
                if (GetFloat(tab, "kissing") > 0.5f) return true;               // 束缚之吻中
            }
            catch { }
            return false;
        }

        internal static bool ModeActive()
        {
            if (_demandMode) return true;
            try
            {
                if (PoseIdx() != 2) return false;
                object tab = Tabemi();
                if (tab == null) return false;
                if (GetStringField(tab, "sitState") == "kyusei") return true;
                if (GetFloat(tab, "kissing") > 0.5f) return true;
            }
            catch { }
            return false;
        }

        // =================================================================
        // 【按姿势三份的配置】
        //
        // 目的：每个栏（口交 / 背榨 / 正骑）各自一份，改一个【不会】影响另外两个。
        //
        // 用法：
        //   声明   internal static ConfigEntry<float>[] DemandAttBonus3;
        //   绑定   DemandAttBonus3 = BindP3("DemandAttBonus", 50.0, "攻击力提升", 0.0, 500.0);
        //   读取   P3(DemandAttBonus3)                ← 自动取当前姿势那一份
        //   面板   SetP3(DemandAttBonus3, SliderF("攻击力提升", P3(DemandAttBonus3), 0.0, 500.0, "{0:0}%", 5.0)));
        //   拷贝   CopyPoseSettings 会遍历所有 P3 组，把三份整体搬过去
        //
        // 索引约定与 PoseIdx() 一致：0 = 口交(Fella)，1 = 背榨(Osiri)，2 = 正骑(Sit)
        // =================================================================
        private static readonly List<ConfigEntry<float>[]> _p3Groups = new List<ConfigEntry<float>[]>();

        private static string[] PoseSuffix = { "_Fella", "_Osiri", "_Sit" };
        private static string[] PoseLabel = { "口交", "背榨", "正骑" };

        /// <summary>
        /// 把描述里的 {模式} 换成该栏位的说法：
        ///   口交 → 吸取    背榨 → 索取    正骑 → 榨取
        /// 这样一个描述模板就能在三栏里各自读通。
        /// </summary>
        /// <summary>
        /// 【按 base 名索引三份组】
        ///
        /// 原来迁移时用 `g[0].Definition.Key == baseName` 去查 —— 那个属性拿到的
        /// 并不是配置键，于是 41 个组全都查不到，迁移静默地一个键都没搬 
        /// （诊断日志把这点直接打出来了。）
        ///
        /// 改成在绑定的时候就登记，不再依赖反射去猜。
        /// </summary>
        private static readonly Dictionary<string, object> _p3ByName = new Dictionary<string, object>();

        private static string ModeWordAt(int i) { return i == 0 ? "吸取" : (i == 2 ? "榨取" : "索取"); }
        private static string PoseDesc(int i, string desc)
        {
            // 【先做占位符替换，再按栏位换词】
            string d = desc.Replace("{模式}", ModeWordAt(i));

            // 描述模板是三栏共用的，而模板里【混着三个词】——
            // 有的条写"索取"、有的写"榨取"。单向替换不够：只把"索取"改成别的，
            // 模板里本来写着"榨取"的那些，在【背榨】栏就会显示成"榨取" 
            //
            // 所以【先归一成占位符，再按栏位填】—— 三个方向都成立：
            //   口交 → 吸取    背榨 → 索取    正骑 → 榨取
            d = d.Replace("榨取", "").Replace("吸取", "").Replace("索取", "");
            d = d.Replace("", ModeWordAt(i));

            return "【" + PoseLabel[i] + "·" + ModeWordAt(i) + "】" + d;
        }

        private static ConfigEntry<float>[] BindP3(string name, float def, string desc, float min, float max)
        {
            var arr = new ConfigEntry<float>[3];
            for (int i = 0; i < 3; i++)
                arr[i] = _pluginInstance.Config.Bind("1-Player", name + PoseSuffix[i], def,
                    new ConfigDescription(PoseDesc(i, desc),
                        new AcceptableValueRange<float>(min, max)));
            _p3Groups.Add(arr);
            _p3ByName[name] = arr;
            return arr;
        }

        private static ConfigEntry<int>[] BindP3Int(string name, int def, string desc, int min, int max)
        {
            var arr = new ConfigEntry<int>[3];
            for (int i = 0; i < 3; i++)
                arr[i] = _pluginInstance.Config.Bind("1-Player", name + PoseSuffix[i], def,
                    new ConfigDescription(PoseDesc(i, desc),
                        new AcceptableValueRange<int>(min, max)));
            _p3IntGroups.Add(arr);
            _p3ByName[name] = arr;
            return arr;
        }

        private static ConfigEntry<bool>[] BindP3Bool(string name, bool def, string desc)
        {
            var arr = new ConfigEntry<bool>[3];
            for (int i = 0; i < 3; i++)
                arr[i] = _pluginInstance.Config.Bind("1-Player", name + PoseSuffix[i], def,
                    PoseDesc(i, desc));
            _p3BoolGroups.Add(arr);
            _p3ByName[name] = arr;
            return arr;
        }

        private static readonly List<ConfigEntry<int>[]> _p3IntGroups = new List<ConfigEntry<int>[]>();
        private static readonly List<ConfigEntry<bool>[]> _p3BoolGroups = new List<ConfigEntry<bool>[]>();

        private static int PIdx
        {
            get
            {
                // 面板里编辑时用【正在看的那一栏】，其余时候（游戏逻辑）用【实际姿势】。
                // 不这样分的话，你人在骑乘位去调「口交」栏的滑块，改的会是骑乘位那份 ——
                // 那正好破坏了"每栏相互独立"。
                if (_editPose >= 0 && _editPose <= 2) return _editPose;
                int i = PoseIdx();
                return i < 0 ? 1 : (i > 2 ? 2 : i);
            }
        }
        internal static int _editPose = -1;
        private static float P3(ConfigEntry<float>[] a) { return a[PIdx].Value; }
        private static int P3(ConfigEntry<int>[] a) { return a[PIdx].Value; }
        private static bool P3(ConfigEntry<bool>[] a) { return a[PIdx].Value; }
        private static void SetP3(ConfigEntry<float>[] a, float v) { a[PIdx].Value = v; }
        private static void SetP3(ConfigEntry<int>[] a, int v) { a[PIdx].Value = v; }
        private static void SetP3(ConfigEntry<bool>[] a, bool v) { a[PIdx].Value = v; }

        /// <summary>把一个姿势的三份设置【整体拷到另一个姿势】。</summary>
        internal static void CopyPoseSettings2(int from, int to)
        {
            int f = Mathf.Clamp(from, 0, 2), t = Mathf.Clamp(to, 0, 2);
            if (f == t) return;
            foreach (var g in _p3Groups) g[t].Value = g[f].Value;
            foreach (var g in _p3IntGroups) g[t].Value = g[f].Value;
            foreach (var g in _p3BoolGroups) g[t].Value = g[f].Value;
            Log.LogInfo("[面板] 已把「" + PoseLabel[f] + "」的全部设置拷到「" + PoseLabel[t] + "」");
        }


        // ---- 连榨计数：姿势 × 模式，四套独立 ----
        private static int ChainNow()
        {
            int p = PoseIdx();
            if (p == 0) return _chainFella;
            bool sit = p == 2;
            if (DrainSlotIsDemand()) return sit ? _chainSitDemand : _chainOsiriDemand;
            return sit ? _chainSitNormal : _chainOsiriNormal;
        }

        private static void ChainSet(int v)
        {
            int p = PoseIdx();
            if (p == 0) { _chainFella = v; return; }
            bool sit = p == 2;
            if (DrainSlotIsDemand()) { if (sit) _chainSitDemand = v; else _chainOsiriDemand = v; }
            else { if (sit) _chainSitNormal = v; else _chainOsiriNormal = v; }
        }

        private static int ChainResetAll()
        {
            _chainFella = 0;
            _chainOsiriNormal = 0; _chainOsiriDemand = 0;
            _chainSitNormal = 0; _chainSitDemand = 0;
            return 0;
        }

        /// <summary>当前场景该用哪个上限。</summary>
        private static int ChainLimitNow()
        {
            int p = PoseIdx();
            if (p == 0) return ChainMaxFella.Value;      // 口交
            bool sit = p == 2;
            if (DrainSlotIsDemand()) return sit ? ChainMaxSitDemand.Value : ChainMaxOsiriDemand.Value;
            return sit ? ChainMaxSitNormal.Value : ChainMaxOsiriNormal.Value;
        }

        /// <summary>给面板/提示条显示用："骑乘位·常规" 这类标签。</summary>
        internal static string ChainSlotName()
        {
            int p = PoseIdx();
            if (p == 0) return "口交";
            return (p == 2 ? "坐姿·正骑" : "骑乘位·背榨") + "·" + (DrainSlotIsDemand() ? ModeWord() : "常规");
        }

        /// <summary>
        /// 这个机制在三个栏位各有各的叫法：
        ///   口交 → 吸取    背榨(背面骑乘) → 索取    正骑(坐姿) → 榨取
        /// </summary>
        /// <summary>
        /// 模式名，用于屏幕横幅与命令回执。
        ///
        /// 【为什么不能直接用 ModeWord()】
        /// 模式可能跨姿势持续（_demandModePose 记的是【进入时】的姿势），
        /// 而 ModeWord() 用的是【当前】姿势 —— 模式中途换了姿势，
        /// 横幅就会显示成另一个词。
        /// </summary>
        internal static string ModeWordForBanner()
        {
            int p = _demandModePose >= 0 ? _demandModePose : PoseIdx();
            return ModeWordAt(p);
        }

        internal static string ModeWord()
        {
            int p = PoseIdx();
            return p == 0 ? "吸取" : (p == 2 ? "榨取" : "索取");
        }
        internal static float SpankSpeedMul() { return _spankMul; }
        internal static float SpankSpeedTarget() { return _spankTarget; }
        internal static float GameManagerSpeedValue()
        {
            try
            {
                Type gm = FindType("GameManager");
                FieldInfo f = gm != null ? FieldQuiet(gm, "GameSpeed") : null;
                object v = f != null ? f.GetValue(null) : null;
                return v is float ff ? ff : -999f;
            }
            catch { return -999f; }
        }
        internal static bool TremorActiveFlag() { return _tremorActive; }
        internal static bool TremorCaughtFlag() { return _tremorCaught; }

        /// <summary>供命令接口 / 面板调用。</summary>
        // =================================================================
        // 逻辑交叉检查
        //
        // 起因：这个修改器把 maxHP / maxEcstasy / timeScale / 条尺寸 这些状态
        // 交给了【七个各自为政的每帧写者】（KeepMaxCaps、PumpCaps、熔断、SetMaxExact、
        // RaiseMaxHp、动摇的生命加成、适应期还原）。它们之间没有单一归属，也没有先后纪律，
        // 于是每加一个机制就会和其中某个打架 —— 失控、条不动、值卡死，全是同一类问题换皮。
        //
        // 这个工具把"它们本该满足的关系"显式写下来并逐条核对，
        // 让"互相打架"变成一份可读的报告，而不是等到玩法上出现怪象才发现。
        // =================================================================
        // ---- 持续监视：边玩边抓矛盾，只记录非 OK 的行 ----
        internal static bool XWatchOn { get { return _xwatchUntil > Time.unscaledTime; } }
        private static float _xwatchUntil = -999f;
        private static float _xwatchNext;
        private static string _xwatchPath;

        internal static void XWatchStart(float seconds)
        {
            _xwatchUntil = Time.unscaledTime + Mathf.Clamp(seconds, 5f, 600f);
            _xwatchNext = 0f;
            _xwatchPath = System.IO.Path.Combine(
                BepInEx.Paths.GameRootPath, "BepInEx", "l2d_dump", "cmd", "xwatch.txt");
            try
            {
                System.IO.File.WriteAllText(_xwatchPath,
                    "逻辑交叉检查·持续监视  开始 " + DateTime.Now.ToString("HH:mm:ss") + "\n", new UTF8Encoding(false));
            }
            catch { }
        }

        internal static void XWatchStop() { _xwatchUntil = -999f; }

        private void TickXWatch()
        {
            if (!XWatchOn) return;
            if (Time.unscaledTime < _xwatchNext) return;
            _xwatchNext = Time.unscaledTime + 1f;      // 每秒一条，异常才写

            string rep = CrossCheck();
            var bad = new List<string>();
            foreach (string ln in rep.Split('\n'))
            {
                string t = ln.Trim();
                if (t.StartsWith("[FAIL]") || t.StartsWith("[WARN]")) bad.Add(t);
            }
            if (bad.Count == 0) return;
            try
            {
                System.IO.File.AppendAllText(_xwatchPath,
                    DateTime.Now.ToString("HH:mm:ss") + "\n  " + string.Join("\n  ", bad.ToArray()) + "\n",
                    new UTF8Encoding(false));
            }
            catch { }
        }

        internal static string CrossCheck()
        {
            var sb = new StringBuilder();
            int fail = 0, warn = 0;
            Action<string, bool, string> chk = delegate (string name, bool ok, string detail)
            {
                if (!ok) fail++;
                sb.Append(ok ? "  [OK]   " : "  [FAIL] ").Append(name).Append(" — ").Append(detail).Append((char)10);
            };
            Action<string, bool, string> soft = delegate (string name, bool ok, string detail)
            {
                if (!ok) warn++;
                sb.Append(ok ? "  [OK]   " : "  [WARN] ").Append(name).Append(" — ").Append(detail).Append((char)10);
            };

            object player = Player();
            sb.Append("逻辑交叉检查  ").Append(DateTime.Now.ToString("HH:mm:ss")).Append((char)10);
            if (player == null)
            {
                sb.Append("  [SKIP] 不在可检查状态（找不到 PlayerControl —— 需要先进游玩状态）\n");
                return sb.ToString();
            }

            float maxHp = GetFloat(player, "maxHP");
            float curHp = GetFloat(player, "currentHP");
            float maxEc = GetFloat(player, "maxEcstasy");
            float curEc = GetFloat(player, "CurrentEcstasy");
            float syaseing = GetFloat(player, "Syaseing");
            float resist = GetFloat(player, "ecstasyResist");

            // (1) 上限天花板
            chk("(1) 上限天花板", maxHp <= CapCeiling + 0.5f && maxEc <= CapCeiling + 0.5f,
                string.Format("maxHP={0:0.#} maxEcstasy={1:0.#} 天花板={2:0}", maxHp, maxEc, CapCeiling));

            // (2) 动摇生命加成的账目自洽
            {
                bool ok;
                string d;
                if (_hpBuffBonus > 0.0001f)
                {
                    ok = AdaptRemaining() > 0f;
                    d = string.Format("加成 -{0:0.##} 仍挂在上限上，适应期剩余 {1:0.##}s（应 > 0）",
                        _hpBuffBonus, AdaptRemaining());
                }
                else
                {
                    ok = true;
                    d = "无未还清的加成";
                }
                chk("(2) 生命加成账目", ok, d);
            }

            // (3) 安全闸：削弱生效时，绝顶值必须仍能到达上限
            {
                float gate = Mathf.Clamp(TremorSafetyGate.Value, 50f, 100f) / 100f;
                bool ok = gate < 1f;
                string d = string.Format("闸门 {0:0}%（绝顶值到 {0:0}% 后不再削弱）{1}",
                    TremorSafetyGate.Value,
                    ok ? "" : " ← 闸门关闭，绝顶值可能永远到不了顶，榨取不会收束！");
                chk("(3) 绝顶可及性安全闸", ok, d);
            }

            // (4) 绝顶值范围
            chk("(4) 绝顶值范围", curEc >= -0.01f && (maxEc <= 0f || curEc <= maxEc + 0.01f),
                string.Format("CurrentEcstasy={0:0.##} / maxEcstasy={1:0.##}", curEc, maxEc));

            // (5) Syaseing 与值是否自相矛盾（值远低于上限却在榨取中 = 状态卡死）
            // 注意：只看"值为 0 且 Syaseing 为真"会大量误报 ——
            // 榨取动画正常播放期间本来就是这样。真正该看的是【它持续了多久】。
            {
                float el = SyaseingElapsed();
                float limit = 12f;   // 榨取动画正常几秒内结束；超过这个数就是真卡住
                bool stuck = syaseing > 0.5f && el > limit;
                soft("(5) Syaseing 未长时间卡住", !stuck,
                    string.Format("Syaseing={0:0} 已持续={1:0.##}s 值={2:0.##}/{3:0.##}  timeScale={4:0.###} "
                        + "动摇={5} 近10s榨取={6}次  本次会话最长={7:0.##}s{8}",
                        syaseing, el, curEc, maxEc, Time.timeScale,
                        _tremorActive ? "中" : "停", EcsResetCountIn(10f),
                        _syaseMaxDur,
                        stuck ? " ← 超过 " + limit + "s 仍未解除，是真卡住" : ""));
            }

            // (6) 抑制标志必须在帧末复位
            chk("(6) 抑制标志已复位", !_suppressDrive && !_suppressRateScale && !_suppressDamageFx,
                string.Format("drive={0} rate={1} fx={2}（都应为 false）",
                    _suppressDrive, _suppressRateScale, _suppressDamageFx));

            // (7) timeScale 应等于 游戏速度 × 动摇模板输出
            {
                float expect = Mathf.Clamp(GameSpeed.Value, 0.05f, 20f);
                if (_tremorActive)
                {
                    float floorPct = Mathf.Clamp(TremorSlow.Value / 100f, 0.1f, 1f);
                    float depth = EvalSlowPattern(_tremorPat, Time.unscaledTime - _tremorT0);
                    expect *= Mathf.Lerp(1f, floorPct, depth);
                }
                chk("(7) 时间刻度一致", Mathf.Abs(Time.timeScale - expect) < 0.02f,
                    string.Format("实际 {0:0.###}  期望 {1:0.###}", Time.timeScale, expect));
            }

            // (8) 动摇状态自洽
            soft("(8) 动摇状态自洽", !(_tremorActive && TremorActiveFlag() == false),
                string.Format("_tremorActive={0} 模板={1} 剩余={2:0.##}s",
                    _tremorActive, (_tremorPat >= 0 && _tremorPat < TremorPatNames.Length) ? TremorPatNames[_tremorPat] : "?",
                    _tremorActive ? Mathf.Max(0f, _tremorDur - (Time.unscaledTime - _tremorT0)) : 0f));

            // (9) 条的尺寸：当前 vs 基准（查"条不响应"）
            {
                object holder = FindGaugeHolder();
                if (holder == null) soft("(9) 条尺寸", true, "找不到 UI（可能不在店内场景）");
                else
                {
                    var bad = new List<string>();
                    foreach (string f in GaugeFields)
                    {
                        RectTransform rt = FieldQuiet(holder.GetType(), f)?.GetValue(holder) as RectTransform;
                        if (rt == null) continue;
                        Vector2 bv;
                        if (!_gaugeBaseline.TryGetValue(f, out bv)) continue;
                        if (Mathf.Abs(rt.sizeDelta.x - bv.x) >= 0.5f || Mathf.Abs(rt.sizeDelta.y - bv.y) >= 0.5f)
                            bad.Add(f + "(" + rt.sizeDelta.x.ToString("0.#") + " != " + bv.x.ToString("0.#") + ")");
                    }
                    soft("(9) 条尺寸 vs 基准", bad.Count == 0,
                        bad.Count == 0 ? "全部与基准一致" : string.Join(" ", bad.ToArray()));
                }
            }

            // (10) 变化率设置是否会把某一路彻底掐死
            {
                bool dead = RatePct(EcstasyUpRate) <= 0.0001f;
                soft("(10) 绝顶上升率未归零", !dead,
                    "上升率=" + EcstasyUpRate.Value.ToString("0") + "%" + (dead ? " ← 归零，绝顶值永不增长" : ""));
            }

            // (11) 抗性与归零开关的组合
            {
                bool inert = EcstasyResist.Value >= 1f || NoEcstasy.Value;
                soft("(11) 绝顶未被完全冻结", !inert,
                    "抗性=" + EcstasyResist.Value.ToString("0.##")
                    + " 归零=" + (NoEcstasy.Value ? "开" : "关")
                    + (inert ? " ← 组合起来绝顶值完全不动" : ""));
            }

            // (12) 榨取循环：短时间内榨取反复触发 = 卡在循环里（用户报的"卡在这里一直重复"）
            {
                int n10 = EcsResetCountIn(10f), n30 = EcsResetCountIn(30f);
                bool ok = n10 <= 3;
                chk("(12) 榨取未陷入循环", ok,
                    string.Format("近 10 秒 {0} 次、近 30 秒 {1} 次{2}",
                        n10, n30,
                        ok ? "" : " ← 反复重演，通常是 maxEcstasy 被削减到过小，"
                                 + "导致 CurrentEcstasy 很快又 >= maxEcstasy，Syaseing 一清就又触发"));
            }

            // (13) 上限是否被削到过小（榨取螺旋的燃料）
            soft("(13) 绝顶上限未被削到过小", !(maxEc > 0f && maxEc < 10f),
                string.Format("maxEcstasy={0:0.##} 本次会话见过的最小值={1}", maxEc,
                    _maxEcMinSeen == float.MaxValue ? -1f : _maxEcMinSeen));

            // (14) 时间刻度不得归零 —— 动画停住 = 动画事件不触发 = Syaseing 永不清除
            //    （Syaseing 的清除点是 Event_FellaSyaseiOnEnd / Event_SitSyasei_OnEnd /
            //      Event_SitKissSyaseiOnEnd / Event_OsiriSyasei_OnEnd 这几个【动画事件】）
            {
                bool ok = Time.timeScale > 0.05f;
                chk("(14) 时间刻度未归零", ok,
                    string.Format("Time.timeScale={0:0.###}{1}", Time.timeScale,
                        ok ? "" : " ← 动画会停住，动画事件不触发，Syaseing 永不解除（榨取不收束）"));
            }

            // (15) 视觉特效未反复触发（"持续闪红光"）
            {
                int sy = FxCountIn(_fxSyaseiTimes, 3f);
                int ky = FxCountIn(_fx吸精Times, 3f);
                int dm = FxCountIn(_fxDamageTimes, 3f);
                bool ok = sy <= 3 && ky <= 3 && dm <= 8;
                chk("(15) 特效未反复触发", ok,
                    string.Format("近 3 秒：绝顶红光 {0} 次、吸精红光 {1} 次、受伤红光 {2} 次（Syaseing 已持续 {3:0.##}s）{4}",
                        sy, ky, dm, SyaseingElapsed(),
                        ok ? "" : " ← 在反复播特效，说明对应动画在重复触发而收尾事件没跟上"));
            }

            // (16) 当前值不得超过上限
            //
            //    【这条已知会报，所以用 soft 而不是 chk】
            //
            //    成因：锁上限（KeepMaxCaps）每帧把上限钉回基线，而榨取的上限削减
            //    与它互相拉扯，当前值就被留在上限之上了。
            //
            //    上限平滑（EaseToward）又让这个现象持续更久 —— 从"一次拉到位"
            //    变成"按速率逼近"，留在上限之上的时间变长，于是更容易被抓到。
            //
            //    超出量通常在 1 点以内（上限 500 上下），即 0.2% 左右，
            //    血条上看不出来，机制上也不影响（用户已确认不用修）。
            //
            //    降级成提醒的理由：一个永远报红的硬性检查，看久了就会被忽略，
            //    那比没有检查更糟 —— 它会污染"报红就是有事"这个信号。
            // 这里刻意【不用 string.Format】——
            // 之前用 string.Format 且把花括号写成了 {{0}}，结果占位符原样显示出来了。
            // 改成字符串拼接，没有转义层就不会再错。
            soft("(16) 生命当前值未越上限（已知会报，见代码注释）",
                curHp <= maxHp + 0.5f && curHp >= -0.01f,
                "currentHP=" + curHp.ToString("0.##") + " maxHP=" + maxHp.ToString("0.##")
                    + (curHp > maxHp + 0.5f
                        ? " ← 超出 " + (curHp - maxHp).ToString("0.##")
                          + "（占上限 " + ((curHp - maxHp) / Mathf.Max(1f, maxHp) * 100f).ToString("0.0")
                          + "%），锁上限与上限削减在拉扯，已知项"
                        : ""));


            // ================= 新增状态的不变量（#4 扩充） =================

            {
                bool ok = _demandMode || _demandModePose == -1;
                chk("(17) 模式姿势记录自洽", ok,
                    string.Format("_demandMode={0} _demandModePose={1}{2}",
                        _demandMode, _demandModePose,
                        ok ? "" : " ← 模式已关但姿势没复位（_demandMode=false 有四处赋值点，有漏的）"));
            }

            {
                int mx = Mathf.Max(1, P3(DemandMaxStacks3));
                chk("(18) 段数在范围内", _demandStacks >= 0 && _demandStacks <= mx,
                    string.Format("段={0} 上限={1}", _demandStacks, mx));
            }

            chk("(19) 索取欲非负", _demandUrge >= -0.01f,
                string.Format("索取欲={0:0.##}%", _demandUrge));

            {
                int poseNow = PoseIdx();
                bool bad = _demandMode && _demandModePose == 1 && poseNow == 2;
                soft("(20) 背榨模式未被坐姿顶替", !bad,
                    string.Format("模式入于={0} 当前姿势={1} 段={2}{3}",
                        _demandModePose, poseNow, _demandStacks,
                        bad ? " ← 背榨的模式被切到了坐姿（状态级兜底应当夺回）" : ""));
            }

            {
                int lim = ChainLimitNow();
                int now = ChainNow();
                soft("(21) 连榨计数未超上限", lim <= 0 || now <= lim,
                    string.Format("档位={0} 计数={1} 上限={2}", ChainSlotName(), now, lim));
            }

            chk("(22) 余韵计数非负", _afterglowPending >= 0 && _afterglowRemaining >= 0,
                string.Format("待发动={0} 进行中={1} 累计={2}", _afterglowPending, _afterglowRemaining, _afterglowTotal));

            {
                bool shouldHave = SitPussyAreaEnabled.Value && PoseIdx() == 2;
                float qx0, qy0, qx1, qy1;
                bool got = PussyAreaScreenRect(out qx0, out qy0, out qx1, out qy1);
                soft("(23) Manman 区矩形可取", !shouldHave || got,
                    shouldHave
                        ? (got ? string.Format("矩形 {0:0}~{1:0} × {2:0}~{3:0}", qx0, qx1, qy0, qy1)
                               : "坐姿 + 已启用，却拿不到矩形（模型包围盒取不到？）")
                        : "不需要（不在坐姿或未启用）");
            }
            // 【按结果定级】白(无问题) / 蓝 / 绿 / 黄 / 红
            //   有硬性矛盾        → 红
            //   1~2 条提醒        → 黄
            //   3~5 条提醒        → 绿
            //   6 条以上提醒      → 蓝（提醒太多说明该整理检查项了）
            //   全过              → 白
            Sev sev;
            if (fail > 0) sev = Sev.Red;
            else if (warn == 0) sev = Sev.Ok;
            else if (warn <= 2) sev = Sev.Yellow;
            else if (warn <= 5) sev = Sev.Green;
            else sev = Sev.Blue;

            sb.Append("  ").Append(SevTag(sev)).Append((char)10);
            if (_pluginInstance != null) _pluginInstance.SetHint(
                string.Format("自检：硬性 {0} / 提醒 {1}  {2}", fail, warn, SevTag(sev)), sev);

            sb.Append("结论：").Append(fail == 0 ? "无硬性矛盾" : (fail + " 项硬性矛盾"))
              .Append(warn > 0 ? ("，" + warn + " 项提醒") : "，无提醒").Append((char)10);
            return sb.ToString();
        }

        internal static string TremorCmd(string arg)
        {
            switch ((arg ?? "status").ToLowerInvariant())
            {
                case "on": TremorEnabled.Value = true; return "动摇 = 开";
                case "off":
                    {
                        TremorEnabled.Value = false;
                        EndTremor(false);
                        _adaptUntil = -999f;
                        RestoreHpBonus(Player(), "关闭动摇");
                        return "动摇 = 关（生命上限已还账）";
                    }
                case "fire":
                    {
                        object p = Player();
                        if (p == null) return "ERR 找不到玩家（先进入游玩状态）";
                        float v = GetFloat(p, "CurrentEcstasy");
                        float maxE = GetFloat(p, "maxEcstasy");
                        float g = Mathf.Max(1f, maxE * 0.12f);
                        // 手动触发时用一个合成的增益（界面上的模拟用）
                        StartTremor(p, v, v + g, g);
                        return "已手动触发动摇（合成增益 " + g.ToString("0.#") + "）";
                    }
                case "status":
                    if (!_tremorActive)
                        return string.Format("动摇：未在波动{0}  概率={1:0}%  冷却={2:0.#}s  剩余冷却={3:0.#}s  脉冲间隔={4:0.###}s  手感计数={5}",
                            AdaptRemaining() > 0f
                                ? "【绝顶适应中 剩余 " + AdaptRemaining().ToString("0.#") + "s 倍率 " + TremorAdaptFactor.Value.ToString("0") + " 上限+" + TremorHpMaxBonus.Value.ToString("0") + "% 减伤" + (100f - TremorHpGuard.Value).ToString("0")
                                    + "% 回复" + TremorHpRegen.Value.ToString("0") + "%/s】"
                                : "",
                            TremorChance.Value,
                            TremorCooldown.Value,
                            Mathf.Max(0f, TremorCooldown.Value - (Time.unscaledTime - _tremorLastEnd)),
                            _accumInterval,
                            _tremorCatchStreak);
                    {
                        float t = Time.unscaledTime - _tremorT0;
                        float uu = Mathf.Clamp01(t / _tremorDur);
                        float ce = Mathf.Lerp(_tremorV1, _tremorSettle, uu * uu * (3f - 2f * uu));
                        return string.Format("动摇中：已过 {0:0.00}/{1:0.00}s  振幅={2:0.##}  中心线={8:0.##}(降 {9:0.##})  落点={3:0.##}（v1={4:0.##} 增益={5:0.##} 损失={6:0.#}%）{7}",
                            t, _tremorDur, _tremorA, _tremorSettle, _tremorV1, _tremorGain,
                            _tremorGain > 0f ? (_tremorV1 - _tremorSettle) / _tremorGain * 100f : 0f,
                            _tremorCaught ? "  [已稳住]" : "",
                            ce,   // 当前中心线（{8}）
                            _tremorV1 - ce)   // 相对 v1 已下降多少（{9}）
                            + "  削弱=" + TremorWeaken.Value.ToString("0") + "%(已削 "
                            + _weakenCount + " 次)  下降率=" + EcstasyDownRate.Value.ToString("0") + "%" + "  模板=" + (_tremorPat >= 0 && _tremorPat < TremorPatNames.Length ? TremorPatNames[_tremorPat] : "?")
                            + "(" + _tremorPatPeriod.ToString("0.##") + "s)"
                            + "  攻击强度=" + _tremorIntensity.ToString("0")
                            + "  动作速度=" + TremorSlow.Value.ToString("0") + "%";
                    }
                default: return "ERR 用法: tremor on|off|fire|status";
            }
        }

        private static float RatePct(ConfigEntry<float> e)
        {
            return Mathf.Clamp(e.Value / 100f, 0f, 2f);
        }

        // ---- 前缀：缩放入参，然后让原方法照常执行（事件里报的就是缩放后的值）----
        private static void Prefix_HPChange(ref float amount)
        {
            amount *= amount < 0f ? RatePct(HpDownRate) : RatePct(HpUpRate);

            // 「绝顶适应」的受伤减免：适应期内负向量被压小
            if (amount < 0f && !_suppressDrive && AdaptRemaining() > 0f)
                amount *= Mathf.Clamp01(TremorHpGuard.Value / 100f);
        }

        private static void Prefix_HPGaugeChangeValue(ref float value)
        {
            if (_suppressRateScale) return;
            if (value > 0f) value *= RatePct(HpUpRate);
        }

        private static void Prefix_EcstasyChange(object __instance, ref float amount)
        {
            amount *= amount > 0f ? RatePct(EcstasyUpRate) : RatePct(EcstasyDownRate);

            // 正向量要过两道削弱，都放过自己驱动的写入（_suppressDrive），
            // 否则动摇自己的波形会被吃掉：
            //   (1) 动摇【期间】的攻击削弱 —— 手一软，打进来的量只剩 TremorWeaken%
            //   (2) 动摇结束后的「绝顶适应」—— 一段时间内继续吃不满
            //
            // 净效果：削弱压住累积，而游戏的【自然消退】照常按「绝顶下降率」跑，
            // 两者一比就是净下降 —— 下降率高的时候绝顶值会持续走低。
            // 这正是它作为「绝顶抵抗」的实际作用。
            if (amount > 0f && !_suppressDrive)
            {
                // 【安全闸】接近上限时一律不削弱。
                // 否则绝顶值永远到不了顶 → Update_Syasei() 不置位 Syaseing →
                // 榨取循环永远不收束，表现为「持续被榨精」。
                bool gated = false;
                if (__instance != null)
                {
                    float maxE = GetFloat(__instance, "maxEcstasy");
                    float curE = GetFloat(__instance, "CurrentEcstasy");
                    if (maxE > 0.01f && curE >= maxE * Mathf.Clamp(TremorSafetyGate.Value, 50f, 100f) / 100f)
                        gated = true;
                }

                if (!gated)
                {
                    float w = 1f;
                    if (_tremorActive) w *= Mathf.Clamp(TremorWeaken.Value / 100f, 0f, 1f);
                    w *= AdaptFactor();
                    if (w < 1f)
                    {
                        amount *= w;
                        _weakenCount++;
                    }
                }
            }
        }

        /// <summary>
        /// 每帧推进「绝顶适应」的生命侧效果。
        ///
        /// 「不能永久」这条靠【增量式还原】保证：加了多少就减多少，
        /// 而不是写回某个快照 —— 快照可能已经过期，写回去会把数值永久改坏
        /// （本项目踩过一次：写回过期的部件透明度快照，把状态永久弄脏了）。
        /// </summary>
        /// <summary>
        /// 把「绝顶适应」的生命上限加成还回去。
        /// 按【增量】减，不写回快照 —— 快照可能已经过期（游戏会削上限），
        /// 写回去反而会把数值永久改坏。加了多少就减多少，且幂等（还完即清零）。
        /// </summary>
        private static void RestoreHpBonus(object player, string why)
        {
            if (_hpBuffBonus <= 0.0001f) { _hpBuffBonus = 0f; return; }
            if (player == null) { _hpBuffBonus = 0f; return; }
            float back = Mathf.Max(1f, GetFloat(player, "maxHP") - _hpBuffBonus);
            SetMaxExact(player, "maxHP", "currentHP", back);
            Log.LogInfo("[动摇] 生命上限已还原 -" + _hpBuffBonus.ToString("0.##")
                        + " → " + back.ToString("0.##") + "（" + why + "）");
            _hpBuffBonus = 0f;
        }

        private void TickAdaptBuff()
        {
            object player = Player();
            if (player == null) { _hpBuffBonus = 0f; return; }

            bool adapting = AdaptRemaining() > 0f;

            if (!adapting)
            {
                // 适应期结束 → 严格还原
                RestoreHpBonus(player, "适应期结束");
                return;
            }

            // 持续回复：只在未满血时补，且绝不越过上限 —— 所以不会变成永久的额外生命
            float regen = TremorHpRegen.Value;
            if (regen > 0.01f)
            {
                float maxHp = GetFloat(player, "maxHP");
                float curHp = GetFloat(player, "currentHP");
                if (maxHp > 0f && curHp < maxHp - 0.01f)
                {
                    float add = maxHp * regen / 100f * Time.unscaledDeltaTime;
                    InvokeFloat(player, "HPChange", Mathf.Min(add, maxHp - curHp));
                }
            }
        }

        /// <summary>当前是否处在绝顶适应期；是则返回累积倍率（0~1）。</summary>
        private static float AdaptFactor()
        {
            if (Time.unscaledTime >= _adaptUntil) return 1f;
            return Mathf.Clamp01(TremorAdaptFactor.Value / 100f);
        }

        internal static float AdaptFactorNow() { return AdaptFactor(); }
        internal static float HpBonusNow() { return _hpBuffBonus; }
        internal static float SyaseingElapsedNow() { return SyaseingElapsed(); }

        /// <summary>注入一笔绝顶值正向量（测试/验证用），返回前后数值。</summary>
        internal static string InjectEcstasyGain(float amount)
        {
            object p = Player();
            if (p == null) return "ERR 找不到玩家";
            float before = GetFloat(p, "CurrentEcstasy");
            float af = AdaptFactor();
            InvokeFloat(p, "EcstasyChange", amount);
            float after = GetFloat(p, "CurrentEcstasy");
            return string.Format("注入 {0:0.##} → 实际增加 {1:0.###}（前 {2:0.##} 后 {3:0.##}，"
                + "上升率 {4:0}%，适应倍率 {5:0.##}）",
                amount, after - before, before, after, EcstasyUpRate.Value, af);
        }

        internal static float AdaptRemaining()
        {
            return Mathf.Max(0f, _adaptUntil - Time.unscaledTime);
        }

        // 榨取（绝顶归零）事件的时间戳，用来抓"卡在榨取循环里反复重演"
        private static readonly List<float> _ecsResetTimes = new List<float>();
        private static float _maxEcMinSeen = float.MaxValue;

        private static void NoteEcstasyReset()
        {
            float now = Time.unscaledTime;
            _ecsResetTimes.Add(now);
            // 只保留最近 30 秒
            for (int i = _ecsResetTimes.Count - 1; i >= 0; i--)
                if (now - _ecsResetTimes[i] > 30f) _ecsResetTimes.RemoveAt(i);

            object pl = Player();
            if (pl != null)
            {
                float me = GetFloat(pl, "maxEcstasy");
                if (me > 0f && me < _maxEcMinSeen) _maxEcMinSeen = me;
            }
        }

        // ---- Syaseing 持续时长：用来区分"榨取动画正常播放中"与"真的卡住" ----
        private static bool _syasePrev;
        private static float _syaseSince;
        private static readonly List<float> _syaseDurations = new List<float>();
        private static float _syaseMaxDur;

        private static void TrackSyaseing()
        {
            object p = Player();
            if (p == null) return;
            bool now = GetFloat(p, "Syaseing") > 0.5f;
            if (now && !_syasePrev) { _syaseSince = Time.unscaledTime; ChainResetAll(); }
            if (!now && _syasePrev)
            {
                float d = Time.unscaledTime - _syaseSince;
                _syaseDurations.Add(d);
                if (d > _syaseMaxDur) _syaseMaxDur = d;
                for (int i = _syaseDurations.Count - 1; i >= 0; i--)
                    if (_syaseDurations.Count > 40) _syaseDurations.RemoveAt(i);
            }
            _syasePrev = now;
        }

        private static float SyaseingElapsed()
        {
            return _syasePrev ? (Time.unscaledTime - _syaseSince) : 0f;
        }

        private static int EcsResetCountIn(float seconds)
        {
            float now = Time.unscaledTime;
            int n = 0;
            for (int i = 0; i < _ecsResetTimes.Count; i++)
                if (now - _ecsResetTimes[i] <= seconds) n++;
            return n;
        }

        /// <summary>无参重载 EcstasyReset() 也要计入榨取次数（榨取收尾常走这条）。</summary>
        /// <summary>【骤降溯源】谁把绝顶值清掉的？每处都留痕，下次骤降就能直接看出是哪一处。</summary>
        private static void LogEcstasyDropSource(string where)
        {
            try
            {
                object pl = Player();
                if (pl == null) return;
                float cur = GetFloat(pl, "CurrentEcstasy");
                object tab = Tabemi();
                Log.LogInfo(string.Format("[骤降溯源] {0}：清前值={1:0.#}  姿势={2}  模式={3}(入于{4})  段={5}"
                    + "  sitState={6}  kissing={7:0}  osiriState={8}  Syaseing={9}  连榨={10}",
                    where, cur, PoseIdx(), _demandMode, _demandModePose, _demandStacks,
                    tab != null ? GetStringField(tab, "sitState") : "?",
                    tab != null ? GetFloat(tab, "kissing") : 0f,
                    tab != null ? GetStringField(tab, "osiriState") : "?",
                    GetFloat(pl, "Syaseing"), ChainNow()));
            }
            catch { }
        }

        private static void Prefix_EcstasyResetNoArg()
        {
            LogEcstasyDropSource("EcstasyReset() 无参");
            NoteEcstasyReset();
            StartZeroHold();
        }

        private static void Prefix_EcstasyReset(ref float rate)
        {
            LogEcstasyDropSource("EcstasyReset(rate)");
            NoteEcstasyReset();
            StartZeroHold();
            rate = 1f - (1f - Mathf.Clamp01(rate)) * RatePct(EcstasyDownRate);
        }

        private static void Prefix_EcstasyGaugeChangePercent(ref float percent)
        {
            if (_suppressRateScale) return;
            if (percent < 0f) percent *= RatePct(EcstasyDownRate);
            else percent *= RatePct(EcstasyUpRate);
        }

        // 硬上限：任何情况下都不让 maxHP / maxEcstasy 超过这个值。
        // 之前的版本把「加量」做成百分比、且每次滑块变化都乘一次，导致拖动时指数膨胀到十万/千万。
        // 现在改成绝对值目标 + 每帧受控逼近 + 熔断，三条一起兜。
        // 上限天花板。原来是 20000 —— 太高了：一旦加成逻辑出问题（比如连续触发时
        // 在"含加成"的上限上再叠加），数值能涨到离谱而没人拦。
        // 降到 3000：既有足够余量，又能在失控的早期就熔断压回。
        private const float CapCeiling = 3000f;

        /// <summary>
        /// 上限对策：朝「目标绝对值」平滑逼近，每帧最多走 CapStepPerSec 点。
        /// 绝不按百分比累乘，所以拖滑块不会指数膨胀。
        /// </summary>
        private void PumpCaps(object player)
        {
            // 熔断：万一已经被撑到离谱的值（早期版本的高频乘法漂移会造成这个），先压回天花板
            float maxHp0 = GetFloat(player, "maxHP");
            float maxEc0 = GetFloat(player, "maxEcstasy");
            if (maxHp0 > CapCeiling) SetMaxExact(player, "maxHP", "currentHP", CapCeiling);
            if (maxEc0 > CapCeiling) SetMaxExact(player, "maxEcstasy", "CurrentEcstasy", CapCeiling);
            if (_baseMaxHp > CapCeiling) _baseMaxHp = CapCeiling;
            if (_baseMaxEcstasy > CapCeiling) _baseMaxEcstasy = CapCeiling;

            float budget = Mathf.Max(1f, CapStepPerSec.Value) * Time.unscaledDeltaTime;

            if (TargetHp.Value > 0f)
            {
                float cur = GetFloat(player, "maxHP");
                // 目标里加上「绝顶适应」的生命上限增量，否则两者会互相拉扯
                float d = (TargetHp.Value + _hpBuffBonus) - cur;
                if (d > 0.5f) RaiseMaxHp(player, cur + Mathf.Min(d, budget));
                else if (d < -0.5f) SetMaxExact(player, "maxHP", "currentHP", cur - Mathf.Min(-d, budget));
            }

            if (TargetEcstasy.Value > 0f)
            {
                float cur = GetFloat(player, "maxEcstasy");
                float d = TargetEcstasy.Value - cur;
                if (d > 0.5f) RaiseMaxEcstasy(player, cur + Mathf.Min(d, budget));
                else if (d < -0.5f) SetMaxExact(player, "maxEcstasy", "CurrentEcstasy", cur - Mathf.Min(-d, budget));
            }
        }

        /// <summary>
        /// 记录/还原血条与绝顶槽那几个 RectTransform 的尺寸。
        ///
        /// 起因：游戏的 HPGaugeChange / EcstasyGaugeChange（挂在
        /// OnHPChanged_GaugeChangePercent / OnEcstasyChanged_GaugeChangePercent 上）会按百分比
        /// 直接改 sizeDelta —— 包括**外框的高度**和**当前条的宽度**。所以只要上限被改过，
        /// 条就会被永久拉长，而还原上限并不会把尺寸改回去。
        /// </summary>
        private static readonly Dictionary<string, Vector2> _gaugeSizes = new Dictionary<string, Vector2>();
        private static readonly Dictionary<string, Vector2> _gaugeBaseline = new Dictionary<string, Vector2>();
        private static bool _gaugeBaselineTaken;
        private static string _gaugeBaselineScene = "";

        /// <summary>
        /// 记住「正常」尺寸作为基准。只在当前尺寸还接近基准时才更新 ——
        /// 这样一旦被拉伸过，基准不会被污染，之后每次自检都会把它逐步拉回正常。
        /// </summary>
        private static void RememberGaugeBaseline()
        {
            object holder = FindGaugeHolder();
            if (holder == null) return;
            foreach (string f in GaugeFields)
            {
                RectTransform rt = FieldQuiet(holder.GetType(), f)?.GetValue(holder) as RectTransform;
                if (rt == null) continue;
                Vector2 cur = rt.sizeDelta;
                Vector2 basev;
                if (!_gaugeBaseline.TryGetValue(f, out basev))
                {
                    _gaugeBaseline[f] = cur;
                    continue;
                }
                // 只有没被明显拉伸时才采信为新基准
                bool nearBase = Mathf.Abs(cur.x - basev.x) <= Mathf.Abs(basev.x) * 0.05f
                             && Mathf.Abs(cur.y - basev.y) <= Mathf.Abs(basev.y) * 0.05f;
                if (nearBase) _gaugeBaseline[f] = cur;
            }
        }

        private static readonly string[] GaugeFields =
        {
            "HPGaugeFrame", "HPGauge_Current", "HPGauge_Delay",
            "EcstasyGaugeFrame", "EcstasyGauge_Current"
        };

        /// <summary>找到挂这些 gauge 的 UI 组件（从场景里所有 MonoBehaviour 上按字段名找）。</summary>
        private static object _gaugeHolderCache;
        private static int _gaugeHolderScene = -1;

        /// <summary>缓存的 UI 持有者（每帧要用，不能每帧全场景扫描）。场景变了自动失效。</summary>
        private static object GaugeHolder()
        {
            int sc = 0;
            try { sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex; } catch { }
            if (_gaugeHolderCache != null && (object)_gaugeHolderCache is UnityEngine.Object uo && uo != null
                && _gaugeHolderScene == sc)
                return _gaugeHolderCache;
            _gaugeHolderScene = sc;
            _gaugeHolderCache = FindGaugeHolder();
            return _gaugeHolderCache;
        }

        private static object FindGaugeHolder()
        {
            foreach (UnityEngine.Object o in Resources.FindObjectsOfTypeAll(typeof(MonoBehaviour)))
            {
                Component c = o as Component;
                if (c == null) continue;
                Type t = c.GetType();
                if (t.GetField("HPGaugeFrame", AllFlags) != null) return c;
            }
            return null;
        }

        private static void SnapshotGauges()
        {
            _gaugeSizes.Clear();
            RememberGaugeBaseline();

            object holder = FindGaugeHolder();
            if (holder == null) return;
            foreach (string f in GaugeFields)
            {
                RectTransform rt = FieldQuiet(holder.GetType(), f)?.GetValue(holder) as RectTransform;
                if (rt == null) continue;
                // 优先还原到「基准尺寸」（能把历史累积的拉伸一起收回来），否则退回当前尺寸
                Vector2 v;
                _gaugeSizes[f] = _gaugeBaseline.TryGetValue(f, out v) ? v : rt.sizeDelta;
            }
        }

        /// <summary>
        /// 每帧把两个条的尺寸钉回基准。
        ///
        /// 为什么必须每帧做：游戏的条长度是【事件驱动的乘法累积】——
        ///     HPGauge_Current.sizeDelta = new Vector2(x + x * percent / 100f, y);
        /// 每次 GaugeChangePercent 都在【当前宽度】上再乘一次。
        /// 而修改器改上限要经过 RaiseMaxHp 与 SetMaxExact 两条路，同一次变化可能被报两次，
        /// 于是宽度一路 ×(1+p)×(1+p)… 累积 —— 实测正好停在基准的 2 倍（642.8 → 1285.6）。
        ///
        /// 条真正的填充是 fillAmount（由 OnHPChanged 驱动），与尺寸无关，
        /// 所以把尺寸钉回基准不会影响"血条显示多少血"。
        /// </summary>
        private void EnforceGaugeBaseline()
        {
            if (_gaugeBaseline.Count == 0) return;
            object holder = GaugeHolder();
            if (holder == null) return;
            foreach (var kv in _gaugeBaseline)
            {
                try
                {
                    RectTransform rt = FieldQuiet(holder.GetType(), kv.Key)?.GetValue(holder) as RectTransform;
                    if (rt == null) continue;
                    Vector2 cur = rt.sizeDelta;
                    if (Mathf.Abs(cur.x - kv.Value.x) < 0.5f && Mathf.Abs(cur.y - kv.Value.y) < 0.5f) continue;
                    rt.sizeDelta = kv.Value;
                }
                catch { }
            }
        }

        private static void RestoreGauges()
        {
            if (_gaugeSizes.Count == 0) return;
            object holder = FindGaugeHolder();
            if (holder == null) return;
            foreach (var kv in _gaugeSizes)
            {
                try
                {
                    RectTransform rt = FieldQuiet(holder.GetType(), kv.Key)?.GetValue(holder) as RectTransform;
                    if (rt != null) rt.sizeDelta = kv.Value;
                }
                catch { }
            }
            _gaugeSizes.Clear();
        }

        /// <summary>把当前条尺寸写成一行，便于自检报告里核对（当前 vs 基准）。</summary>
        private static string DescribeGauges()
        {
            object holder = FindGaugeHolder();
            if (holder == null) return "（找不到 UI）";
            var parts = new List<string>();
            foreach (string f in GaugeFields)
            {
                RectTransform rt = FieldQuiet(holder.GetType(), f)?.GetValue(holder) as RectTransform;
                if (rt == null) continue;
                Vector2 basev;
                string mark = "";
                if (_gaugeBaseline.TryGetValue(f, out basev))
                {
                    bool same = Mathf.Abs(rt.sizeDelta.x - basev.x) < 0.5f
                             && Mathf.Abs(rt.sizeDelta.y - basev.y) < 0.5f;
                    mark = same ? "" : " != 基准";
                }
                parts.Add(f.Replace("Gauge", "") + ":" + rt.sizeDelta.x.ToString("0.#") + "x"
                          + rt.sizeDelta.y.ToString("0.#") + mark);
            }
            return string.Join("  ", parts.ToArray());
        }

        /// <summary>精确设置上限（下调/熔断用）。不走游戏的百分比接口——那东西是乘法，补不回精确值，
        /// 高频调用会累积漂移。这里写字段，并用一次零增量的 GaugeChange 触发 UI 刷新。
        /// </summary>
        private static void SetMaxExact(object player, string maxField, string curField, float value)
        {
            float oldMax = GetFloat(player, maxField);
            if (oldMax <= 0.001f)
            {
                SetField(player, maxField, value);
                if (GetFloat(player, curField) > value) SetField(player, curField, value);
                return;
            }

            // 【必须传非零百分比】
            // 游戏的 HPGaugeChangeValue / EcstasyGaugeChangePercent 是"把增量报给界面"的接口：
            //     maxHP += value;  OnHPChanged_GaugeChangeValue?.Invoke(value);
            // 界面按这个值刷新条长。传 0 就等于告诉界面"什么都没变"，条会原地不动 ——
            // 这正是"上限调小时条停滞、而调大时（走 RaiseMaxHp 传的是非零百分比）却正常"的原因。
            //
            // 这里改成：先用字段把上限精确写成目标值，再用【相对旧值的百分比】触发一次刷新，
            // 且 HPfollws = false（不要让当前值跟着被乘法放大，当前值由我们自己夹）。
            float percent = (value - oldMax) / oldMax * 100f;
            try
            {
                SetField(player, maxField, value);
                float cur = GetFloat(player, curField);
                if (cur > value) SetField(player, curField, value);

                _suppressRateScale = true;      // 别让变化率补丁把这次精确触发缩放掉
                if (maxField == "maxHP") InvokeFloat(player, "HPGaugeChangePercent", percent, false);
                else InvokeFloat(player, "EcstasyGaugeChangePercent", percent, false);
                _suppressRateScale = false;

                // 上面那次调用按乘法语义会把上限再乘一遍，这里用字段钉回精确值
                SetField(player, maxField, value);
            }
            catch (Exception e)
            {
                _suppressRateScale = false;
                Log.LogWarning("SetMaxExact 失败: " + e.Message);
            }
        }

        /// <summary>
        /// 把 HP 上限改成 target。做法：先让 currentHP 等于该值，再调 HPGaugeChangePercent（跟当前值），
        /// 使游戏内部那条 (current - max)/max 恰好等于我们要的比例 —— 全程走游戏自己的接口，界面同步。
        /// allowDown=true 时也允许下调（改小上限）。
        /// </summary>
        private static void RaiseMaxHp(object player, float target, bool allowDown = false)
        {
            if (target > CapCeiling) target = CapCeiling;
            if (target < 1f) return;

            float maxHp = GetFloat(player, "maxHP");
            float delta = target - maxHp;
            if (delta <= 0.5f && !(allowDown && delta < -0.5f)) return;

            float percent = delta / maxHp * 100f;
            try
            {
                FieldInfo cur = FieldQuiet(player.GetType(), "currentHP");
                if (cur == null) return;
                cur.SetValue(player, target);
                InvokeFloat(player, "HPGaugeChangePercent", percent, true);
            }
            catch (Exception e)
            {
                if (Log != null) Log.LogWarning("改 HP 上限失败：" + e.Message);
            }
        }

        /// <summary>把绝顶值上限改成 target（EcstasyGaugeChangePercent 不跟随当前值，直接按百分比）。</summary>
        private static void RaiseMaxEcstasy(object player, float target, bool allowDown = false)
        {
            if (target > CapCeiling) target = CapCeiling;
            if (target < 1f) return;

            float maxEc = GetFloat(player, "maxEcstasy");
            float delta = target - maxEc;
            if (delta <= 0.5f && !(allowDown && delta < -0.5f)) return;

            float percent = delta / maxEc * 100f;
            InvokeFloat(player, "EcstasyGaugeChangePercent", percent, true);
        }

        private static void InvokeFloat(object obj, string name, float value, bool twoArgs)
        {
            if (obj == null) return;
            try
            {
                if (twoArgs)
                {
                    MethodInfo mi = obj.GetType().GetMethod(name, AllFlags, null,
                        new[] { typeof(float), typeof(bool) }, null);
                    if (mi != null) { mi.Invoke(obj, new object[] { value, true }); return; }
                }
                MethodInfo m = obj.GetType().GetMethod(name, AllFlags, null,
                    new[] { typeof(float) }, null);
                if (m != null) m.Invoke(obj, new object[] { value });
            }
            catch (Exception e)
            {
                if (Log != null) Log.LogWarning("调用 " + name + " 失败：" + e.Message);
            }
        }

        // ---------------------------------------------------------------
        // 每秒应用
        // ---------------------------------------------------------------
        private void ApplySlow()
        {
            object player = Player();
            object tabemi = Tabemi();
            object clock = Clock();

            // 进场景的第一次先记下血条/绝顶槽的「正常尺寸」作为基准 ——
            // 必须在任何自检/上限操作之前，否则基准会被拉长后的值污染。
            // 场景切换后重新采一次（同场景内只记一次）。
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (!_gaugeBaselineTaken || scene != _gaugeBaselineScene)
            {
                if (Player() != null)
                {
                    _gaugeBaselineTaken = true;
                    _gaugeBaselineScene = scene;
                    _gaugeBaseline.Clear();
                    RememberGaugeBaseline();
                }
            }

            if (player != null)
            {
                // 这两个值必须「只在滑块变动时设一次」。
                // 早期版本每秒无条件写入，等于把游戏自己的递增按死（射精次数永远不涨、
                // 汉堡层数因此恒定不变），并且一直在和游戏机制打架。
                if (Mathf.Abs(SyaseiCount.Value - _lastSyasei) > 0.01f)
                {
                    _lastSyasei = SyaseiCount.Value;
                    SetField(player, "syaseiCount", SyaseiCount.Value);
                }
                if (MaxBurgerNum.Value != _lastMaxBurger)
                {
                    _lastMaxBurger = MaxBurgerNum.Value;
                    SetField(player, "maxBurgerNum", MaxBurgerNum.Value);
                }
                if (EcstasyResist.Value > 0f) SetField(player, "ecstasyResist", Mathf.Clamp01(EcstasyResist.Value));
            }

            if (tabemi != null)
            {
                if (PowerUnlock.Value)
                {
                    float mul = Mathf.Max(1f, TabemiPowerMul.Value);
                    SetField(tabemi, "tabemiPower", 100f * mul);
                    SetField(tabemi, "baseAtt", 5f * mul);
                    SetField(tabemi, "TabemiAtt", 5f * mul);
                }
            }

            // 速度加成在这两个动画控制器上，不在 TabemiControl 上
            object ani = Anim();
            if (ani != null) SetField(ani, "fellaSpeedPlus", FellaSpeedPlus.Value);
            object aniSit = AnimSit();
            if (aniSit != null) SetField(aniSit, "sitSpeedPlus", SitSpeedPlus.Value);

            if (clock != null)
            {
                SetField(clock, "fullTime", ClockFullTime.Value);
                if (FrozenClock.Value) SetField(clock, "isClockRunning", false);
            }

            // 动摇期间让角色动作变慢：乘在「游戏速度」上，而不是另写一次覆盖它。
            // 闭环：抖动 → 动作变慢 → 攻击脉冲变稀 → 绝顶值真的降下来。
            float speed = Mathf.Clamp(GameSpeed.Value, 0.05f, 20f);
            if (_tremorActive)
            {
                // 减速不是恒定的：按本次挑中的模板在 100% ~ TremorSlow% 之间起伏
                float floorPct = Mathf.Clamp(TremorSlow.Value / 100f, 0.1f, 1f);
                float depth = EvalSlowPattern(_tremorPat, Time.unscaledTime - _tremorT0);
                speed *= Mathf.Lerp(1f, floorPct, depth);
            }
            Time.timeScale = speed;
        }

        private void WriteClearedDays()
        {
            const int MaxDay = 4;
            if (PlayerPrefs.GetInt("ClearedDayDLC", 0) < MaxDay) PlayerPrefs.SetInt("ClearedDayDLC", MaxDay);
            if (PlayerPrefs.GetInt("ClearedDay", 0) < MaxDay) PlayerPrefs.SetInt("ClearedDay", MaxDay);
            PlayerPrefs.Save();
            Field(FindType("GameManager"), "clearedDay")?.SetValue(null, MaxDay);
            Log.LogInfo("已写入通关进度（普通 / DLC 各 " + MaxDay + " 天）");
        }

        // ---------------------------------------------------------------
        // 面板（分页，避免压住游戏）
        // ---------------------------------------------------------------
        private static readonly string[] TabNames = { "玩家", "口交", "背榨", "正骑", "换装", "系统" };
        private int _tab;

        /// <summary>供命令通道切栏（截图验证布局时用）。</summary>
        /// <summary>供命令通道用：切栏 + 本帧末尾截图。</summary>
        internal static string TabShotPublic(int tab, string name)
        {
            if (_pluginInstance == null) return "ERR 插件实例不存在";
            if (tab < 0 || tab >= TabNames.Length) return "ERR 没有这个页签：" + tab;
            _pluginInstance.RequestTabShot(tab, name);
            return "已请求截图 [" + tab + "] " + TabNames[tab] + " -> " + name + ".png（本帧末尾执行）";
        }

        internal static void SetTabPublic(int i) { if (_pluginInstance != null) _pluginInstance._tab = i; }
        internal static string[] TabNamesPublic() { return TabNames; }
        internal static string DumpTabsPublic()
        {
            var sb = new System.Text.StringBuilder("页签: ");
            for (int i = 0; i < TabNames.Length; i++)
                sb.Append(i).Append('=').Append(TabNames[i]).Append(i == _pluginInstance._tab ? "(当前) " : " ");
            return sb.ToString();
        }
        private bool _collapsed;
        private Vector2 _scroll;
        private string _snapHint = "";

        // ---- tabshot：切栏 + 同帧截图 ----
        private string _pendingShotName;
        private int _pendingShotTab = -1;

        /// <summary>
        /// 切到指定页签，并在本帧 OnGUI 画完之后截图。
        ///
        /// 【必须在 OnGUI 末尾做】—— 那样保证画的是切换后的新页签，
        /// 而且 ImGui 已经重绘完成。分两个命令发的话时机对不上（踩过三次）。
        /// </summary>
        private System.Collections.IEnumerator CaptureAfterFrame(string name, string rectTxt, int tabNo,
                                              int ix, int iy, int iw, int ih)
        {
            // 等到这一帧真正画完再读像素 —— 否则拿不到 ImGui 的面板
            yield return new WaitForEndOfFrame();
            try
            {
                string dir = System.IO.Path.Combine(Paths.GameRootPath, "BepInEx", "l2d_dump", "cmd");
                System.IO.Directory.CreateDirectory(dir);

                Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
                tex.Apply();
                byte[] png = tex.EncodeToPNG();
                UnityEngine.Object.Destroy(tex);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, name + ".png"), png);
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, name + ".rect"), rectTxt);

                _snapHint = string.Format("tabshot [{0}] {1} -> {2}.png  面板 {3},{4} {5}x{6}",
                    tabNo, TabNames[tabNo], name, ix, iy, iw, ih);
                Log.LogInfo("[tabshot] " + _snapHint);
            }
            catch (Exception e) { Log.LogWarning("[tabshot] 失败: " + e.Message); }
        }

        internal void RequestTabShot(int tab, string name)
        {
            if (tab >= 0 && tab < TabNames.Length) _tab = tab;
            _pendingShotName = name;
            _pendingShotTab = tab;
        }

        // ==============================================================
        // 提示 / 告警的分级配色
        //
        //   白   无问题
        //   蓝   提示（最低一级，基本无害）
        //   绿   注意
        //   黄   异常
        //   红   严重（最高一级）
        //
        // 为什么要有等级而不是只有"对/错"：
        // 一条永远报红的检查，看久了就会被忽略 —— 那比没有检查更糟。
        // 有了等级，才能让"红"保持稀缺，也才能表达"这个不用管，但你要知道"。
        // ==============================================================
        internal enum Sev { Ok = 0, Blue = 1, Green = 2, Yellow = 3, Red = 4 }

        private Sev _hintSev = Sev.Ok;
        // 单独一个样式，不复用 Hint() 的那个 —— 复用会把所有 Hint 的颜色一起改掉
        private static GUIStyle _statusStyle;

        /// <summary>
        /// 告警等级色。取用户指定的四色（Ant Design 调色板）：
        ///     蓝 #1890FF   绿 #52C41A   黄 #FAAD14   红 #FF4D4F
        /// 白（无问题）用 Color.white。
        ///
        /// 十六进制留在这里，方便以后核对是不是被谁改动了。
        /// </summary>
        internal static Color SevColor(Sev s)
        {
            switch (s)
            {
                case Sev.Blue: return new Color(24f / 255f, 144f / 255f, 255f / 255f);   // #1890FF 蓝·提示
                case Sev.Green: return new Color(82f / 255f, 196f / 255f, 26f / 255f);   // #52C41A 绿·注意
                case Sev.Yellow: return new Color(250f / 255f, 173f / 255f, 20f / 255f);   // #FAAD14 黄·异常
                case Sev.Red: return new Color(255f / 255f, 77f / 255f, 79f / 255f);   // #FF4D4F 红·严重
                default:         return Color.white;                     // 无问题
            }
        }

        /// <summary>设提示文字并指定等级。等级只影响颜色，不影响行为。</summary>
        internal void SetHint(string text, Sev sev)
        {
            _snapHint = text;
            _hintSev = sev;
        }

        internal static string SevTag(Sev s)
        {
            switch (s)
            {
                case Sev.Blue:   return "[蓝·提示]";
                case Sev.Green:  return "[绿·注意]";
                case Sev.Yellow: return "[黄·异常]";
                case Sev.Red:    return "[红·严重]";
                default:         return "[白·正常]";
            }
        }

        // ---- 连榨剩余次数提示条（屏幕上，与面板显隐无关）----
        private static string _chainBanner = "";
        private static float _chainBannerUntil = -999f;
        private static GUIStyle _chainBannerStyle;

        /// <summary>连榨开始时调用：显示"剩余次数"若干秒。</summary>
        internal static void ShowChainBanner(string text, float seconds = 2.5f)
        {
            _chainBanner = text;
            _chainBannerUntil = Time.unscaledTime + seconds;
        }

        private void DrawChainBanner()
        {
            if (Time.unscaledTime >= _chainBannerUntil || string.IsNullOrEmpty(_chainBanner)) return;
            if (_chainBannerStyle == null)
            {
                _chainBannerStyle = new GUIStyle(GUI.skin.label);
                _chainBannerStyle.fontSize = 34;
                _chainBannerStyle.fontStyle = FontStyle.Bold;
                _chainBannerStyle.alignment = TextAnchor.MiddleCenter;
                _chainBannerStyle.normal.textColor = new Color(1f, 0.85f, 0.35f);
            }
            // 描边：画两遍深色再画亮色，任何背景上都看得清
            var shadow = new GUIStyle(_chainBannerStyle);
            shadow.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
            Rect r = new Rect(0f, Screen.height * 0.14f, Screen.width, 48f);
            Rect r2 = new Rect(r.x + 2f, r.y + 2f, r.width, r.height);
            GUI.Label(r2, _chainBanner, shadow);
            GUI.Label(r, _chainBanner, _chainBannerStyle);
        }

        private static GUIStyle _hitAreaStyle;
        private static Texture2D _hitLine;

        /// <summary>【实验性】把所有可点击区域画出来（含插件自建的小穴区）。</summary>
        private void DrawHitAreaHighlight()
        {
            if (!HighlightHitAreas.Value) return;
            try
            {
                if (Tabemi() == null) return;

                if (_hitAreaStyle == null)
                {
                    _hitAreaStyle = new GUIStyle(GUI.skin.label);
                    _hitAreaStyle.fontSize = 12;
                    _hitAreaStyle.fontStyle = FontStyle.Bold;
                    _hitAreaStyle.normal.textColor = new Color(0.5f, 1f, 0.7f);
                }
                if (_hitLine == null)
                {
                    _hitLine = new Texture2D(1, 1);
                    _hitLine.SetPixel(0, 0, new Color(0.4f, 1f, 0.65f, 0.95f));
                    _hitLine.Apply();
                }

                // 【按姿势过滤】只画当前姿势真正生效的那些 ——
                // 原来把模型里 5 个 HitArea 全画了，于是在口交时坐姿/骑乘位的框也出来，
                // 和 Manman 区叠在一起，看起来像"有两个 Manman 区"。
                int poseNow = PoseIdx();
                string[] onlyThisPose;
                if (poseNow == 2)       onlyThisPose = new string[] { "HitArea_Head_Sit" };
                else if (poseNow == 1)  onlyThisPose = new string[] { "HitArea_Head", "HitArea_Osiri" };
                else                    onlyThisPose = new string[] { "HitArea_Head", "HitArea_LeftGirl", "HitArea_RightGirl" };

                var drawn = new List<string>();
                foreach (string nm in HitAreaDrawableNamesCached())
                {
                    if (Array.IndexOf(onlyThisPose, nm) < 0) continue;   // 不属于当前姿势，跳过
                    float x0, y0, x1, y1;
                    if (TryGetDrawableScreenRect(nm, out x0, out y0, out x1, out y1))
                    {
                        DrawOutline(x0, y0, x1, y1, nm);
                        drawn.Add(nm);
                    }
                }

                // 【去重】Manman 区可能和上面某个 drawable 是【同一块】——
                //   (1) 模型里已有 HitArea_Manman
                //   (2) 或者绑定了某个已被画过的 HitArea* 素材
                // 这两种情况下再画一次就会出现"两个 Manman 区"。
                if (SitPussyAreaEnabled.Value)
                {
                    string bindNow = ManmanBindDrawable.Value;
                    bool sameAsDrawn = (!string.IsNullOrEmpty(bindNow) && drawn.Contains(bindNow))
                                       || drawn.Contains("HitArea_Manman");
                    if (!sameAsDrawn)
                    {
                        float px0, py0, px1, py1;
                        if (PussyAreaScreenRect(out px0, out py0, out px1, out py1))
                            DrawOutline(px0, py0, px1, py1, "HitArea_Manman_By_Plugins", true);   // 粉色
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 画一个判定区的框。
        /// 【颜色】默认绿（游戏原生的 HitArea）；插件自建的 Manman 区传粉色，
        /// 但走的是【同一个函数】—— 可视化逻辑只有这一套。
        /// </summary>
        private void DrawOutline(float x0, float y0, float x1, float y1, string label, bool pink = false)
        {
            float top = Screen.height - y1;
            float w = x1 - x0, h = y1 - y0;
            if (w < 1f || h < 1f) return;

            Texture2D line = pink ? EnsurePinkLine() : _hitLine;
            GUIStyle st = pink ? EnsurePinkStyle() : _hitAreaStyle;

            const float t = 2f;
            GUI.DrawTexture(new Rect(x0, top, w, t), line);
            GUI.DrawTexture(new Rect(x0, top + h - t, w, t), line);
            GUI.DrawTexture(new Rect(x0, top, t, h), line);
            GUI.DrawTexture(new Rect(x0 + w - t, top, t, h), line);
            GUI.Label(new Rect(x0, top - 16f, 240f, 16f), label, st);
        }

        private static Texture2D _pinkLine;
        private static GUIStyle _pinkStyle;

        private static Texture2D EnsurePinkLine()
        {
            if (_pinkLine == null)
            {
                _pinkLine = new Texture2D(1, 1);
                _pinkLine.SetPixel(0, 0, new Color(1f, 0.42f, 0.72f, 0.98f));
                _pinkLine.Apply();
            }
            return _pinkLine;
        }

        private static GUIStyle EnsurePinkStyle()
        {
            if (_pinkStyle == null)
            {
                _pinkStyle = new GUIStyle(GUI.skin.label);
                _pinkStyle.fontSize = 12;
                _pinkStyle.fontStyle = FontStyle.Bold;
                _pinkStyle.normal.textColor = new Color(1f, 0.55f, 0.8f);
            }
            return _pinkStyle;
        }

        private void OnGUI()
        {
            _editPose = -1;      // 每帧从头开始，避免影响游戏逻辑那边的 P3 读取
            DrawChainBanner();          // 先画（不受面板显隐影响）
            DrawHitAreaHighlight();
            if (!_showPanel) return;

            float w = _collapsed ? 200f : 540f;   // 540 而不是 452 —— 原来标签会被挤到贴边
            _win = GUILayout.Window(0x0717, _win, DrawWindow,
                _collapsed ? "修改器（F9）" : "数值修改器 · F9 隐藏",
                GUILayout.Width(w));
            _win.width = w;
        }

        private void DrawWindow(int id)
        {
            object player = Player();
            object tabemi = Tabemi();
            object clock = Clock();

            GUILayout.BeginVertical();

            // 顶部：收起 / 场景状态
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_collapsed ? "展开" : "收起", GUILayout.Width(56f)))
                _collapsed = !_collapsed;
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            GUILayout.Label(player == null ? "标题画面（无数据）" : scene);
            GUILayout.EndHorizontal();

            if (!_collapsed)
            {
                // 实时数值摘要
                if (player != null)
                {
                    GUILayout.Label(string.Format("HP {0:0.#}/{1:0.#}    绝顶 {2:0.#}/{3:0.#}",
                        GetFloat(player, "currentHP"), GetFloat(player, "maxHP"),
                        GetFloat(player, "CurrentEcstasy"), GetFloat(player, "maxEcstasy")));
                }
                if (clock != null)
                {
                    GUILayout.Label(string.Format("进度 {0:0}%   时刻 {1:0.0}h   一天 {2} 秒",
                        GetFloat(clock, "clockProgress") * 100f, GetFloat(clock, "gameHour"),
                        (int)GetFloat(clock, "fullTime")));
                }

                // 分页标签
                GUILayout.Space(4f);
                GUILayout.BeginHorizontal();
                for (int i = 0; i < TabNames.Length; i++)
                {
                    GUIStyle st = GUI.skin.button;
                    if (_tab == i) st = HighlightStyle(st);
                    if (GUILayout.Button(TabNames[i], st, GUILayout.Width(66f))) _tab = i;
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(4f);

                // 内容区（可滚动，固定高度 → 面板不会长到压住游戏）
                _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(300f));
                switch (_tab)
                {
                    case 0: DrawTabPlayer(player); break;
                    case 1: DrawTabFella(tabemi); break;
                    case 2: DrawTabOsiri(tabemi); break;
                    case 3: DrawTabSit(tabemi); break;
                    case 4: DrawTabCostumeAndParts(tabemi); break;
                    default: DrawTabSystemWithCaps(player, clock); break;
                }
                GUILayout.EndScrollView();

                {
                    if (_statusStyle == null)
                    {
                        _statusStyle = new GUIStyle(GUI.skin.label);
                        _statusStyle.wordWrap = true;
                    }
                    _statusStyle.normal.textColor = SevColor(_hintSev);
                    GUILayout.Label(string.IsNullOrEmpty(_snapHint)
                        ? "产物目录 BepInEx" + (char)92 + "l2d_dump" + (char)92
                        : _snapHint, _statusStyle);
                }

            }

            GUILayout.EndVertical();
            GUI.DragWindow();

            // ---- tabshot：在本帧画完之后捕获 ----
            // 放在这里的原因：此时新页签的内容已经画完，
            // 而且 _win 的宽高已经由 ImGui 按实际内容定好，裁切坐标是准的。
            if (_pendingShotName != null)
            {
                string nm = _pendingShotName;
                _pendingShotName = null;

                // 【为什么不能在这里直接 ReadPixels】
                // OnGUI 里读的是 backbuffer，而 ImGui 的绘制这时还没合成进去 ——
                // 实测截出来的图里【完全没有面板】，只有游戏画面。
                // 正解是等这一帧结束，用 WaitForEndOfFrame 协程。
                //
                // 这里只做两件事：把面板矩形算好（此时 _win 已是本帧的实际值），
                // 然后起协程去截。
                int ix = Mathf.RoundToInt(_win.x);
                // 【不要翻转 y】
                // OnGUI 里的坐标是【左上原点】（GUI 空间），和 Input.mousePosition
                // 的左下原点不是一回事。我一开始按左下原点做了翻转，结果裁到了面板下方。
                // 判据：_win 的初始值是 new Rect(24f, 90f, ...)，那个 90 就是"距顶部 90"。
                int iy = Mathf.RoundToInt(_win.y);
                int iw = Mathf.RoundToInt(_win.width);
                int ih = Mathf.RoundToInt(_win.height);
                string rectTxt = string.Format("{0} {1} {2} {3} tab={4}", ix, iy, iw, ih, _tab);
                int shotTab = _pendingShotTab; _pendingShotTab = -1;
                StartCoroutine(CaptureAfterFrame(nm, rectTxt, shotTab, ix, iy, iw, ih));
            }
        }

        // ---- 页 1：玩家 ----
        private void DrawTabPlayer(object player)
        {
            Section("变化率（按每次实际变化的量缩放）");
            Hint("100% = 原版；0% = 该类变化完全不发生");
            HpDownRate.Value = SliderF("HP 下降率", HpDownRate.Value, 0f, 200f, "{0:0}%", 5f);
            HpUpRate.Value = SliderF("HP 回复率", HpUpRate.Value, 0f, 200f, "{0:0}%", 5f);
            EcstasyUpRate.Value = SliderF("绝顶上升率", EcstasyUpRate.Value, 0f, 200f, "{0:0}%", 5f);
            EcstasyDownRate.Value = SliderF("绝顶下降率", EcstasyDownRate.Value, 0f, 200f, "{0:0}%", 5f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("HP 不下降")) HpDownRate.Value = 0f;
            if (GUILayout.Button("绝顶不上升")) EcstasyUpRate.Value = 0f;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("全回 100%"))
            {
                HpDownRate.Value = 100f;
                HpUpRate.Value = 100f;
                EcstasyUpRate.Value = 100f;
                EcstasyDownRate.Value = 100f;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            Section("快捷开关");
            GodMode.Value = GUILayout.Toggle(GodMode.Value, "  无敌（HP 每帧补满）");
            NoEcstasy.Value = GUILayout.Toggle(NoEcstasy.Value, "  绝顶值归零（不被吸精）");
            EcstasyResist.Value = SliderF("绝顶抗性", EcstasyResist.Value, 0f, 1f, "{0:0.00}", 0.05f);
            MaxBurgerNum.Value = SliderI("连吃汉堡上限", MaxBurgerNum.Value, 0, 200);
            SyaseiCount.Value = SliderI("射精次数", SyaseiCount.Value, 0, 100);
            Hint("射精次数影响每天的具材数，改完回标题重进当天生效。");

            DrawTremorSection();

            GUILayout.Space(4f);
            Section("汉堡层数");
            object menu = StaticPart("menu");
            int baseCount = menu != null ? (int)GetFloat(menu, "Base具材数") : -1;
            int rel = player != null ? (int)GetFloat(player, "syaseiCount") : -1;
            GUILayout.Label(string.Format("基础具材 {0}   射精次数 {1}   → 订单约 {2} 层",
                baseCount < 0 ? "--" : baseCount.ToString(),
                rel < 0 ? "--" : rel.ToString(),
                (baseCount < 0 || rel < 0) ? "--" : (baseCount + rel).ToString()));
            Hint("层数 = 基础具材 + 射精次数（作者设计：榨得越多堆越高）");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("层数复位（4 层）") && player != null)
            {
                SyaseiCount.Value = 0;
                _lastSyasei = 0f;
                SetField(player, "syaseiCount", 0);
                if (menu != null) SetField(menu, "Base具材数", 4);
                _snapHint = "已复位为基础 4 层（重进当天生效）";
            }
            if (GUILayout.Button("基础具材回 4") && menu != null)
            {
                SetField(menu, "Base具材数", 4);
                _snapHint = "Base具材数 = 4";
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("HP 回满") && player != null)
                InvokeFloat(player, "HPChange", GetFloat(player, "maxHP") - GetFloat(player, "currentHP"));
            if (GUILayout.Button("绝顶清零") && player != null)
                InvokeFloat(player, "EcstasyChange", -GetFloat(player, "CurrentEcstasy"));
            GUILayout.EndHorizontal();
        }

        // ---- 页 2：上限（对策） ----
        /// <summary>「绝顶动摇 / 绝顶适应」的控制区（挂在「玩家」页底部）。</summary>
        private void DrawTremorSection()
        {
            GUILayout.Space(8f);
            Section("绝顶动摇（累积时随机触发的衰减正弦波动）");
            TremorEnabled.Value = GUILayout.Toggle(TremorEnabled.Value, "  启用动摇");
            if (TremorEnabled.Value)
            {
                TremorChance.Value = SliderF("触发概率", TremorChance.Value, 0f, 100f, "{0:0}%", 1f);
                TremorDuration.Value = SliderF("持续时间", TremorDuration.Value, 0.3f, 6f, "{0:0.0}s", 0.1f);
                TremorAmplitude.Value = SliderF("振幅", TremorAmplitude.Value, 0f, 200f, "{0:0}%", 5f);
                TremorLoss.Value = SliderF("落点损失", TremorLoss.Value, 0f, 150f, "{0:0}%", 5f);
                Sub("可超过 100% —— 那样落点会压到波动之前的值以下（倒扣）");
                TremorCatch.Value = SliderF("按 R 稳住后损失", TremorCatch.Value, 0f, 90f, "{0:0}%", 1f);
                TremorCooldown.Value = SliderF("冷却", TremorCooldown.Value, 0f, 20f, "{0:0.0}s", 0.5f);
                TremorResonate.Value = GUILayout.Toggle(TremorResonate.Value, "  与攻击脉冲共振（攻速快→抖动更细更快）");
                TremorSlow.Value = SliderF("减速最低值", TremorSlow.Value, 10f, 100f, "{0:0}%", 5f);
                TremorPattern.Value = SliderI("减速模板", TremorPattern.Value, 0, 5);
                Sub("1顿挫 2痉挛 3深陷 4迟疑 5潮汐（长短不一）");
            }

            GUILayout.Space(4f);
            Section("绝顶适应（动摇结束后，均为临时，结束严格还原）");
            TremorWeaken.Value = SliderF("动摇期间攻击削弱", TremorWeaken.Value, 0f, 100f, "{0:0}%", 5f);
            TremorAdaptTime.Value = SliderF("适应持续", TremorAdaptTime.Value, 0f, 30f, "{0:0.0}s", 0.5f);
            TremorAdaptFactor.Value = SliderF("适应期累积倍率", TremorAdaptFactor.Value, 0f, 100f, "{0:0}%", 5f);
            TremorHpMaxBonus.Value = SliderF("生命上限提升", TremorHpMaxBonus.Value, 0f, 200f, "{0:0}%", 5f);
            TremorHpGuard.Value = SliderF("受伤倍率", TremorHpGuard.Value, 0f, 100f, "{0:0}%", 5f);
            TremorHpRegen.Value = SliderF("每秒回复", TremorHpRegen.Value, 0f, 100f, "{0:0}%/s", 1f);
            TremorSafetyGate.Value = SliderF("安全闸", TremorSafetyGate.Value, 50f, 100f, "{0:0}%", 1f);
            Sub("安全闸不能关：绝顶值若永远到不了上限，榨取循环不会收束");

            GUILayout.Space(4f);
            Section("吸精 · 连榨");
            Sub("连榨上限（各栏独立：骑乘位/坐姿 × 常规/模式，口交另有一套）");
            ChainMaxOsiriNormal.Value = SliderI("  骑乘位·常规", ChainMaxOsiriNormal.Value, 0, 50);
            ChainMaxOsiriDemand.Value = SliderI("  骑乘位·索取模式", ChainMaxOsiriDemand.Value, 0, 50);
            ChainMaxSitNormal.Value = SliderI("  坐姿·常规", ChainMaxSitNormal.Value, 0, 50);
            ChainMaxSitDemand.Value = SliderI("  坐姿·榨取模式", ChainMaxSitDemand.Value, 0, 50);
            Hint("0 = 不限；当前场景：" + ChainSlotName()
                + "，上限 " + ChainLimitNow() + "，已连 " + ChainNow());
            GUILayout.Label(string.Format("  吸精发生率 {0}%（已修正游戏的整数除法 bug，填多少就是多少）",
                KyuseiRateNow().ToString("0")));

            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("手动触发动摇")) TremorCmd("fire");
            if (GUILayout.Button("本次状态")) LogTremorStatus();
            GUILayout.EndHorizontal();
        }

        private static float KyuseiRateNow()
        {
            try
            {
                Type at = FindType("Live2D_AnimationControl");
                if (at != null)
                {
                    UnityEngine.Object[] acs = Resources.FindObjectsOfTypeAll(at);
                    if (acs != null && acs.Length > 0)
                        return GetFloatPublicRaw(acs[0], "KyuseiRate");
                }
            }
            catch { }
            return -1f;
        }

        private static void LogTremorStatus()
        {
            Log.LogInfo("[动摇] " + TremorCmd("status"));
        }

        private void DrawTabCaps(object player)
        {
            Section("上限");
            Hint("游戏机制：被榨取按 3~5% 削 maxHP、1~5% 削 maxEcstasy；");
            Hint("吃汉堡的回血按 maxHP 的百分比算 → 上限被削就越来越难回血。");

            GUILayout.Space(4f);
            LockMaxHp.Value = GUILayout.Toggle(LockMaxHp.Value, "  锁 HP 上限（不再下降）");
            LockMaxEcstasy.Value = GUILayout.Toggle(LockMaxEcstasy.Value, "  锁绝顶值上限");

            GUILayout.Space(4f);
            Section("目标上限（绝对值，每帧平滑逼近）");
            TargetHp.Value = SliderF("HP 上限目标", TargetHp.Value, 0f, 3000f, "{0:0}", 25f);
            TargetEcstasy.Value = SliderF("绝顶上限目标", TargetEcstasy.Value, 0f, 3000f, "{0:0}", 25f);
            CapStepPerSec.Value = SliderF("逼近速度（点/秒）", CapStepPerSec.Value, 1f, 2000f, "{0:0}", 20f);
            Hint("0 = 不启用。这是「设到多少」，不是「加百分之多少」");
            Hint("拖滑块只是改目标，不会累乘，绝不会膨胀。");

            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("HP 目标 ×1.5") && player != null)
                TargetHp.Value = Mathf.Min(3000f, GetFloat(player, "maxHP") * 1.5f);
            if (GUILayout.Button("HP 目标 ×2") && player != null)
                TargetHp.Value = Mathf.Min(3000f, GetFloat(player, "maxHP") * 2f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("绝顶目标 ×1.5") && player != null)
                TargetEcstasy.Value = Mathf.Min(3000f, GetFloat(player, "maxEcstasy") * 1.5f);
            if (GUILayout.Button("绝顶目标 ×2") && player != null)
                TargetEcstasy.Value = Mathf.Min(3000f, GetFloat(player, "maxEcstasy") * 2f);
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            if (GUILayout.Button("恢复默认上限（100 / 100）"))
            {
                TargetHp.Value = 100f;
                TargetEcstasy.Value = 100f;
            }
            GUILayout.Label(string.Format("硬上限保护：{0:0}（超过会被压回）", CapCeiling));
        }

        // ---- 页 3：对手（吃汉） ----
        /// <summary>「对手」页里的索取模式控制区。</summary>
        // ==============================================================
        // 面板排版 helper
        //
        // 原来标题有三四种写法（"—— X ——" / "——————" / "—— X"），
        // 间距有四个值（4/6/8/10）—— 全靠手写，所以看起来乱。
        //
        // 现在统一成三个 helper，**间距和格式只有一处定义**。
        // 以后改版式只需要改这里。
        // ==============================================================

        private static GUIStyle _secStyle, _subStyle, _hintStyle;

        private static void EnsureStyles()
        {
            if (_secStyle != null) return;
            _secStyle = new GUIStyle(GUI.skin.label);
            _secStyle.fontSize = 12;
            _secStyle.fontStyle = FontStyle.Bold;
            _secStyle.normal.textColor = new Color(0.85f, 0.88f, 0.96f);
            _secStyle.wordWrap = false;

            // 原来 Sub 和 Section 只差 1px 且都加粗 —— 在面板上【看不出层级】。
            // 真正看到面板之后才发现：两个标题长得几乎一样。
            // 现在 Sub 不加粗、颜色更淡，层级才读得出来。
            _subStyle = new GUIStyle(GUI.skin.label);
            _subStyle.fontSize = 11;
            _subStyle.fontStyle = FontStyle.Normal;
            _subStyle.normal.textColor = new Color(0.66f, 0.72f, 0.84f);

            _hintStyle = new GUIStyle(GUI.skin.label);
            _hintStyle.fontSize = 10;
            _hintStyle.normal.textColor = new Color(0.58f, 0.62f, 0.72f);
            _hintStyle.wordWrap = true;
        }

        /// <summary>一级小节。左侧色条 + 上方固定留白。</summary>
        private void Section(string title)
        {
            EnsureStyles();
            GUILayout.Space(10f);
            GUILayout.Label("" + title, _secStyle);
        }

        /// <summary>二级小节。更小、无装饰。</summary>
        private void Sub(string title)
        {
            EnsureStyles();
            GUILayout.Space(6f);
            GUILayout.Label("   " + title, _subStyle);
        }

        /// <summary>说明文字（灰色小字，自动换行）。</summary>
        private void Hint(string text)
        {
            EnsureStyles();
            GUILayout.Label("     " + text, _hintStyle);
        }

        /// <summary>一条细分隔线（原来写成一串破折号）。</summary>
        private void Rule()
        {
            GUILayout.Space(4f);
            var r = GUILayoutUtility.GetRect(1f, 1f);
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1f),
                Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f,
                new Color(1f, 1f, 1f, 0.10f), 0f, 0f);
            GUILayout.Space(4f);
        }

        private void DrawDemandSection(bool sitSide)
        {
            // 这个机制三栏各有叫法：口交=吸取、背榨=索取、正骑=榨取。
            // 本方法被背榨(sitSide=false)与正骑(sitSide=true)调用，所以按参数取名。
            string mw = sitSide ? "榨取" : "索取";
            Section(sitSide ? "正骑 · 榨取模式" : "背榨 · 索取模式（打屁股触发）");
            SetP3(DemandEnabled3, GUILayout.Toggle(P3(DemandEnabled3), sitSide ? "  启用榨取模式" : "  启用索取模式"));
            GUILayout.Space(4f);
            SetP3(SpankSpeedEnabled3, GUILayout.Toggle(P3(SpankSpeedEnabled3), "  打屁股给攻击速度冲量"));
            if (P3(SpankSpeedEnabled3))
            {
                SetP3(SpankSpeedGain3, SliderF("首下冲量", P3(SpankSpeedGain3), 0f, 200f, "{0:0}%", 2f));
                SetP3(SpankSpeedStack3, SliderF("叠加倍率", P3(SpankSpeedStack3), 0f, 100f, "{0:0}%", 5f));
                SetP3(SpankSpeedRise3, SliderF("冲上去的快慢", P3(SpankSpeedRise3), 1f, 60f, "{0:0}", 1f));
                SetP3(SpankSpeedDecay3, SliderF("消退的快慢", P3(SpankSpeedDecay3), 0.01f, 5f, "{0:0.00}", 0.05f));
                GUILayout.Label(string.Format("  当前乘数 ×{0:0.###}（目标 ×{1:0.###}）",
                    SpankSpeedMul(), SpankSpeedTarget()));
            }
            if (P3(DemandEnabled3))
            {
                if (!sitSide) GUILayout.Label("  打屁股 → 累积「索取欲」，索取欲的数值就是进入索取模式的概率");
                SetP3(AttackUrgeChance3, SliderF("涨欲概率·平均", P3(AttackUrgeChance3), 0f, 100f, "{0:0}%", 1f));
                SetP3(AttackUrgeGain3, SliderF("命中时增加", P3(AttackUrgeGain3), 0f, 100f, "{0:0}%", 1f));
                SetP3(AttackUrgeJitter3, SliderF("上面两项的波动幅度", P3(AttackUrgeJitter3), 0f, 90f, "±{0:0}%", 5f));
                GUILayout.Space(4f);
                if (!sitSide) GUILayout.Label("  射精 / 吸精·连榨 也累积索取欲（余韵走同一链条）");
                SetP3(SyaseiUrgeChance3, SliderF("射精时·累积概率", P3(SyaseiUrgeChance3), 0f, 100f, "{0:0}%", 5f));
                SetP3(SyaseiUrgeGain3, SliderF("射精时·增加", P3(SyaseiUrgeGain3), 0f, 100f, "{0:0}%", 1f));
                SetP3(KyuseiUrgeChance3, SliderF("每次吸精·累积概率", P3(KyuseiUrgeChance3), 0f, 100f, "{0:0}%", 5f));
                SetP3(KyuseiUrgeGain3, SliderF("每次吸精·增加", P3(KyuseiUrgeGain3), 0f, 100f, "{0:0}%", 1f));
                GUILayout.Label(string.Format("  实际在 概率 {0:0.#}~{1:0.#}% / 增量 {2:0.#}~{3:0.#}% 之间随机",
                    P3(AttackUrgeChance3) * Mathf.Max(0f, 1f - P3(AttackUrgeJitter3) / 100f),
                    P3(AttackUrgeChance3) * (1f + P3(AttackUrgeJitter3) / 100f),
                    P3(AttackUrgeGain3) * Mathf.Max(0f, 1f - P3(AttackUrgeJitter3) / 100f),
                    P3(AttackUrgeGain3) * (1f + P3(AttackUrgeJitter3) / 100f)));
                SetP3(DemandMaxStacks3, SliderI("最多叠加段数", P3(DemandMaxStacks3), 1, 20));
                SetP3(DemandMaxStackRefreshMul3, SliderF("叠满后刷新持续时间", P3(DemandMaxStackRefreshMul3), 0f, 300f, "{0:0}%", 5f));
                Hint("段数满了之后不再加层，但条件达成会给时间（0 = 原行为）");
                GUILayout.Space(4f);
                SetP3(AttackCanStackDemand3, GUILayout.Toggle(P3(AttackCanStackDemand3),
                    "  角色攻击也参与叠层判定（关掉则只有打屁股能叠）"));
                GUILayout.Space(4f);
                if (sitSide)

                Sub("叠好一层" + mw + "状态所需的" + mw + "欲（两者上限都是 300%）：");
                Hint("" + mw + "欲攒够对应数值才会叠上那一层");
                if (GUILayout.Toggle(P3(DemandTriggerMode3) == 1, "    阈值模式（攒够就叠，可预期）"))
                    SetP3(DemandTriggerMode3, 1);
                if (GUILayout.Toggle(P3(DemandTriggerMode3) == 0, "    概率模式（" + mw + "欲即概率，随机）"))
                    SetP3(DemandTriggerMode3, 0);
                SetP3(DemandThresholdBase3, SliderF("叠第1层所需索取欲", P3(DemandThresholdBase3), 0f, 300f, "{0:0}%", 5f));
                SetP3(DemandThresholdStep3, SliderF("每多一层所需增加", P3(DemandThresholdStep3), 0f, 300f, "{0:0}%", 5f));
                GUILayout.Label(string.Format("    叠各层所需：{0:0} / {1:0} / {2:0} / {3:0} / {4:0} …",
                    P3(DemandThresholdBase3),
                    P3(DemandThresholdBase3) + P3(DemandThresholdStep3),
                    P3(DemandThresholdBase3) + P3(DemandThresholdStep3) * 2f,
                    P3(DemandThresholdBase3) + P3(DemandThresholdStep3) * 3f,
                    P3(DemandThresholdBase3) + P3(DemandThresholdStep3) * 4f));
                GUILayout.Label(string.Format("    当前" + mw + "欲 {0:0.#}%  ·  距叠第 {1} 层还差 {2:0.#}%",
                    DemandUrge,
                    _demandStacks + 1,
                    Mathf.Max(0f, P3(DemandThresholdBase3) + P3(DemandThresholdStep3) * Mathf.Max(0, _demandStacks) - DemandUrge)));
                SetP3(DemandUrgeMin3, SliderF("每次索取欲下限", P3(DemandUrgeMin3), 0f, 100f, "{0:0}%", 1f));
                SetP3(DemandUrgeMax3, SliderF("每次索取欲上限", P3(DemandUrgeMax3), 0f, 100f, "{0:0}%", 1f));
                GUILayout.Space(4f);
                SetP3(DemandEscapePenaltyChance3, SliderF("脱出惩罚概率", P3(DemandEscapePenaltyChance3), 0f, 100f, "{0:0}%", 5f));
                SetP3(DemandEscapePenaltyMul3, SliderF("脱出要求倍数", P3(DemandEscapePenaltyMul3), 100f, 500f, "{0:0}%", 5f));
                GUILayout.Space(4f);
                SetP3(DemandAttBonus3, SliderF("攻击力提升", P3(DemandAttBonus3), 0f, 500f, "{0:0}%", 5f));
                SetP3(DemandAttSpeedBonus3, SliderF("攻击力随速度加成", P3(DemandAttSpeedBonus3), 0f, 500f, "{0:0}%", 5f));
                SetP3(DemandAttSpeedRef3, SliderF("速度参考增幅", P3(DemandAttSpeedRef3), 5f, 300f, "{0:0}%", 5f));
                SetP3(DemandAttSpeedExp3, SliderF("速度曲线指数", P3(DemandAttSpeedExp3), 0f, 8f, "{0:0.0}", 0.5f));
                GUILayout.Label(string.Format("  当前攻击速度倍率 ×{0:0.###} → 速度项 ×{1:0.###}",
                    SpankSpeedMul(),
                    1f + Mathf.Clamp01(Mathf.Max(0f, SpankSpeedMul() - 1f)
                        / Mathf.Max(0.05f, P3(DemandAttSpeedRef3) / 100f)) * P3(DemandAttSpeedBonus3) / 100f));
                SetP3(DemandAnimSpeed3, SliderF("绝顶动画倍速", P3(DemandAnimSpeed3), 50f, 800f, "{0:0}%", 25f));
                SetP3(DemandSpeedSplit3, GUILayout.Toggle(P3(DemandSpeedSplit3), "  分阶段加速（避免与 osiri 加速叠加）"));
                GUILayout.Space(4f);
                // 【正骑里不显示】回口交 / 回归累积是【背榨】的机制 ——
                // 坐姿现在已锁死（SitLockEnabled），这些"回到口交再回骑乘位"的项在正骑里没有意义。
                if (!sitSide)
                {
                    SetP3(DemandBlockFella3, GUILayout.Toggle(P3(DemandBlockFella3), "  " + mw + "模式期间不回口交"));
                    SetP3(DemandFellaDecay3, SliderF("每次索取·回口交衰减", P3(DemandFellaDecay3), 0f, 90f, "{0:0}%", 5f));
                    SetP3(DemandOsiriBoost3, SliderI("回口交后骑乘位门槛降低", P3(DemandOsiriBoost3), 0, 10));
                }

                GUILayout.Space(4f);
                // 【正骑里不显示】回归累积是【背榨 → 口交 → 再回骑乘位】的机制，坐姿里没有意义
                if (!sitSide)
                {
                    SetP3(OsiriReturnEnabled3, GUILayout.Toggle(P3(OsiriReturnEnabled3), "  回口交后累积「再次进骑乘位」概率"));
                    if (P3(OsiriReturnEnabled3))
                    {
                        SetP3(OsiriReturnTrigger3, SliderF("每次造成伤害·累积概率", P3(OsiriReturnTrigger3), 0f, 100f, "{0:0}%", 1f));
                        SetP3(OsiriReturnGain3, SliderF("累积成功时增加", P3(OsiriReturnGain3), 0f, 100f, "{0:0}%", 1f));
                        SetP3(OsiriReturnJitter3, SliderF("上面两项波动幅度", P3(OsiriReturnJitter3), 0f, 90f, "±{0:0}%", 5f));
                        GUILayout.Label(string.Format("  当前累积 {0:0.#}%{1}", _osiriReturnChance,
                            _osiriReturnArmed ? "" : "（未武装：需先从骑乘位回口交）"));
                    }
                }
                Hint("门槛原值：第1~4天 = 10/9/8/7，越低骑乘位来得越快");
                GUILayout.Space(4f);
                SetP3(DemandDurationMin3, SliderF("索取模式最短", P3(DemandDurationMin3), 2f, 300f, "{0:0}s", 1f));
                SetP3(DemandDurationMax3, SliderF("索取模式最长", P3(DemandDurationMax3), 2f, 300f, "{0:0}s", 1f));
                GUILayout.Space(4f);
                GUILayout.Label(sitSide ? "  榨取模式期间速度不因射精减缓；只能由角色自行退出"
                    : "  索取模式期间速度不因射精减缓；只能由角色自行退出");
                // 【顺序】共享内容全部跑完，最后才是正骑专属的那些。
                // 原来这个调用夹在共享内容中间，导致坐姿栏里「束缚之吻 / 坐姿锁定 /
                // Manman 区」插在叠层门槛之前，读起来是跳的。
                if (sitSide) DrawSitExtras();

                Hint("余韵 = 基础速度更低、单次持续时间更长的连榨");
                Hint("只在模式中按射精累积；等最后一段的连榨等活动全部结束才发动");
                GUILayout.Space(4f);
                Section("余韵 · 累积与发动");
                SetP3(AfterglowPerSyasei3, SliderI("模式中每射精累积余韵", P3(AfterglowPerSyasei3), 0, 20));
                SetP3(AfterglowSettleDelay3, SliderF("局面稳定后发动延迟", P3(AfterglowSettleDelay3), 0f, 5f, "{0:0.0}s", 0.1f));
                SetP3(AfterglowUseDemandGain3, GUILayout.Toggle(P3(AfterglowUseDemandGain3),
                    "  余韵期间仍累积余韵（一般应关：收尾不该自我延长）"));
                SetP3(AfterglowUrgeScale3, SliderF("余韵期间索取欲增长", P3(AfterglowUrgeScale3), 0f, 100f, "{0:0}%", 5f));
                Hint("调低它 → 余韵之后有一段干净空档，不会马上又进" + mw + "模式");
                SetP3(AfterglowClearsUrge3, GUILayout.Toggle(P3(AfterglowClearsUrge3),
                    "  余韵清空后把索取欲一并归零"));
                Sub("手动余韵 = 强制退出" + mw + "模式");
                Hint("" + mw + "太久导致换不了姿势时，用它把" + mw + "模式清干净");
                GUILayout.Space(4f);
                Section("余韵 · 调速与波动");
                SetP3(AfterglowSpeed3, SliderF("余韵动画速度", P3(AfterglowSpeed3), 10f, 400f, "{0:0}%", 5f));
                SetP3(AfterglowSpeedWobble3, SliderF("速度波动幅度", P3(AfterglowSpeedWobble3), 0f, 90f, "±{0:0}%", 5f));
                SetP3(AfterglowSpeedHz3, SliderF("速度波动频率", P3(AfterglowSpeedHz3), 0.02f, 4f, "{0:0.00}Hz", 0.05f));
                GUILayout.Label(string.Format("    速度在 {0:0}% ~ {1:0}% 之间起伏（越低=单次越长）",
                    P3(AfterglowSpeed3) * (1f - P3(AfterglowSpeedWobble3) / 100f),
                    P3(AfterglowSpeed3) * (1f + P3(AfterglowSpeedWobble3) / 100f)));
                GUILayout.Space(4f);
                Sub("余韵 · 绝顶值（与连榨分开的一套）");
                SetP3(AfterglowSoftCap3, SliderF("余韵绝顶值软上限", P3(AfterglowSoftCap3), 5f, 98f, "{0:0}%", 1f));
                SetP3(AfterglowEcstasyWobble3, SliderF("到位后波动幅度", P3(AfterglowEcstasyWobble3), 0f, 40f, "±{0:0}%", 1f));
                GUILayout.Label(string.Format("    绝顶值在 {0:0}% ~ {1:0}% 之间起伏",
                    Mathf.Max(0f, P3(AfterglowSoftCap3) - P3(AfterglowEcstasyWobble3)),
                    P3(AfterglowSoftCap3) + P3(AfterglowEcstasyWobble3)));
                GUILayout.Space(4f);
                Hint("（下面两个只在「手动进入余韵」时用，正常流程不用）");
                DemandAfterglowMin.Value = SliderI("手动余韵下限", DemandAfterglowMin.Value, 0, 100);
                DemandAfterglowMax.Value = SliderI("手动余韵上限", DemandAfterglowMax.Value, 0, 100);
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("手动进入" + mw)) DemandCmd("fire");
            if (GUILayout.Button("手动进入余韵")) DemandCmd("afterglow");
            GUILayout.EndHorizontal();
            Hint("当前：" + DemandCmd("status"));
        }
                // 【正骑专属】从 DrawDemandSection 里抽出来的。
                // 原来是一大块 if (sitSide) {...}，占了那个方法的三分之二，
                // 而它和共享逻辑混在一起，读的时候要一直记着"这段只在坐姿下跑"。
                private void DrawSitExtras()
                {
                Sub("正骑 · 榨取模式（坐姿）");
                SitClickAlwaysAccumulate.Value = GUILayout.Toggle(SitClickAlwaysAccumulate.Value,
                    "  坐姿点头部随时可累积榨取欲（不限束缚之吻）");
                SitSyaseiToDrainChance.Value = SliderF("坐姿射精→进榨取的概率",
                    SitSyaseiToDrainChance.Value, 0f, 100f, "{0:0}%", 5f);
                Hint("坐姿挂起超时见下方「余韵」区");
                GUILayout.Space(4f);
                Section("正骑 · 束缚之吻");
                Hint("正骑的连榨/余韵【只有进入束缚之吻才会发生】");
                Hint("本栏下面那些滑块（攻击力/倍速/连榨…）都已绑定到束缚之吻：");
                Hint("进入吻或榨取状态时才会生效，退出即失效");
                SitKissAutoPrepare.Value = GUILayout.Toggle(SitKissAutoPrepare.Value, "  自动进入束缚之吻（不用按 X）");
                GUILayout.Space(4f);
                Hint("坐姿锁定：进去就出不来（自动切换全拦）");
                SitLockEnabled.Value = GUILayout.Toggle(SitLockEnabled.Value,
                    "  启用坐姿锁定");
                if (SitLockEnabled.Value)
                {
                    if (GUILayout.Button("解锁一次（让游戏能切走）"))
                    {
                        SitLockEnabled.Value = false;
                        _snapHint = "已解锁 —— 游戏下次尝试切换就会成功；要再锁上请重新勾选";
                        Log.LogInfo("[坐姿锁定] 手动解锁（逃生口）");
                    }
                }
                if (SitKissAutoPrepare.Value)
                    SitKissAutoInterval.Value = SliderF("自动进入间隔", SitKissAutoInterval.Value, 1f, 60f, "{0:0}s", 1f);
                SitKissNoDecay.Value = GUILayout.Toggle(SitKissNoDecay.Value, "  阻止窗口越玩越短（原版每次 *=0.9）");
                SitKissWindowMul.Value = SliderF("窗口长度倍数", SitKissWindowMul.Value, 50f, 1000f, "{0:0}%", 25f);
                GUILayout.Space(4f);
                Hint("接吻中但还没进榨取时，单独用这一套速度：");
                KissSpeedEnabled.Value = GUILayout.Toggle(KissSpeedEnabled.Value, "  束缚之吻单独调速");
                if (KissSpeedEnabled.Value)
                {
                    KissSpeedMul.Value = SliderF("接吻动画速度", KissSpeedMul.Value, 10f, 400f, "{0:0}%", 5f);
                    KissSpeedWobble.Value = SliderF("速度波动幅度", KissSpeedWobble.Value, 0f, 90f, "±{0:0}%", 5f);
                    if (KissSpeedWobble.Value > 0.01f)
                        KissSpeedHz.Value = SliderF("波动频率", KissSpeedHz.Value, 0.02f, 4f, "{0:0.00}Hz", 0.05f);
                }
                GUILayout.Space(4f);
                Section("正骑 · HitArea_Manman_By_Plugins");
                Hint("点击 = 坐姿版的「打屁股」：涨榨取欲 + 给坐姿动作一个速度冲量");
                Hint("判定按【人物模型包围盒】算 → 跟着人物走，换分辨率也不跑偏");
                SitPussyAreaShowRect.Value = GUILayout.Toggle(SitPussyAreaShowRect.Value,
                    "  跟着总开关一起画（粉色）—— 总开关在下面「实验性高亮」那个");
                GUILayout.Space(4f);
                Sub("素材绑定（比手拖更贴合，跟着网格形变走）");
                Hint("当前绑定：" + (string.IsNullOrEmpty(ManmanBindDrawable.Value)
                    ? "（无，用手拖的矩形）" : ManmanBindDrawable.Value));
                if (GUILayout.Button("清除绑定（回到手拖矩形）"))
                    ManmanBindDrawable.Value = "";
                Hint("点下面任一素材名即可绑定，它的大小位置就是判定区");
                Hint("（绑定到非 HitArea 的美术素材才有意义 —— 那 5 个 HitArea 本身就能点）");
                Hint("筛选（子串）：");
                ManmanDrawableFilter.Value = GUILayout.TextField(ManmanDrawableFilter.Value ?? "", 24);
                {
                    List<string> all = AllDrawableNames(ManmanDrawableFilter.Value);
                    const int PER = 40;
                    int pages = Mathf.Max(1, (all.Count + PER - 1) / PER);
                    int pg = Mathf.Clamp(ManmanDrawablePage.Value, 0, pages - 1);
                    ManmanDrawablePage.Value = pg;

                    GUILayout.Label(string.Format("    共 {0} 个素材，第 {1}/{2} 页", all.Count, pg + 1, pages));
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("上一页") && pg > 0) ManmanDrawablePage.Value = pg - 1;
                    if (GUILayout.Button("下一页") && pg < pages - 1) ManmanDrawablePage.Value = pg + 1;
                    if (GUILayout.Button("HitArea 筛选")) ManmanDrawableFilter.Value = "HitArea";
                    if (GUILayout.Button("清空筛选")) ManmanDrawableFilter.Value = "";
                    GUILayout.EndHorizontal();

                    int from = pg * PER;
                    for (int i = from; i < Mathf.Min(from + PER, all.Count); i++)
                    {
                        string nm = all[i];
                        bool cur = (nm == ManmanBindDrawable.Value);
                        if (GUILayout.Button((cur ? "(记录) " : "    ") + nm))
                        {
                            ManmanBindDrawable.Value = cur ? "" : nm;
                            _snapHint = cur ? "已取消素材绑定" : ("HitArea_Manman_By_Plugins 已绑定到素材：" + nm);
                            Log.LogInfo("[HitArea_Manman_By_Plugins] 绑定切换 → " + (cur ? "(清除)" : nm));
                        }
                    }
                }
                SitPussyAreaEnabled.Value = GUILayout.Toggle(SitPussyAreaEnabled.Value, "  启用 HitArea_Manman_By_Plugins");
                if (SitPussyAreaEnabled.Value)
                {
                    SitPussyAreaCX.Value = SliderF("中心 X（模型宽%）", SitPussyAreaCX.Value, 0f, 1f, "{0:0.000}", 0.005f);
                    SitPussyAreaCY.Value = SliderF("中心 Y（模型高%）", SitPussyAreaCY.Value, 0f, 1f, "{0:0.000}", 0.005f);
                    SitPussyAreaW.Value = SliderF("宽（模型宽%）", SitPussyAreaW.Value, 0.01f, 0.6f, "{0:0.000}", 0.005f);
                    SitPussyAreaH.Value = SliderF("高（模型高%）", SitPussyAreaH.Value, 0.01f, 0.6f, "{0:0.000}", 0.005f);
                    ManmanDragMode.Value = GUILayout.Toggle(ManmanDragMode.Value,
                        "  拖动模式（判定区中心实时跟随鼠标）");
                    if (ManmanDragMode.Value)
                        Hint("开着时框会跟着鼠标跑；对准位置后点下面的「绑定」");
                    if (GUILayout.Button(ManmanDragMode.Value ? "绑定到此位置（并退出拖动）" : "把中心设为当前鼠标位置"))
                    {
                        float rx, ry;
                        if (ScreenToModelRel(Input.mousePosition, out rx, out ry))
                        {
                            SitPussyAreaCX.Value = rx;
                            SitPussyAreaCY.Value = ry;
                            ManmanDragMode.Value = false;
                            _snapHint = string.Format("HitArea_Manman_By_Plugins 中心已绑定到模型相对位置 ({0:0.000}, {1:0.000})", rx, ry);
                            Log.LogInfo(string.Format("[HitArea_Manman_By_Plugins] 中心绑定到模型相对位置 ({0:0.000}, {1:0.000})", rx, ry));
                        }
                        else
                        {
                            _snapHint = "绑定失败：拿不到模型包围盒（人物不在场？）";
                        }
                    }
                    // 实时诊断：鼠标在不在区内、游戏的静态命中值是什么
                    {
                        float dx0, dy0, dx1, dy1;
                        if (PussyAreaScreenRect(out dx0, out dy0, out dx1, out dy1))
                        {
                            Vector3 mp = Input.mousePosition;
                            bool inRect = (mp.x >= dx0 && mp.x <= dx1 && mp.y >= dy0 && mp.y <= dy1);
                            string pointing = "?";
                            try
                            {
                                Type hcT2 = FindType("Live2D_HitAreaCheck");
                                FieldInfo pf2 = hcT2 != null ? FieldQuiet(hcT2, "mousePointing") : null;
                                if (pf2 != null) pointing = (pf2.GetValue(null) as string) ?? "(空)";
                            }
                            catch { }
                            GUILayout.Label(string.Format("    屏幕矩形：{0:0}~{1:0} × {2:0}~{3:0}",
                                dx0, dx1, dy0, dy1));
                            string src = "插件自建矩形";
                            try
                            {
                                float t0, t1, t2, t3;
                                if (TryGetDrawableScreenRect("HitArea_Manman", out t0, out t1, out t2, out t3))
                                    src = "模型里的 HitArea_Manman";
                                else if (!string.IsNullOrEmpty(ManmanBindDrawable.Value))
                                    src = "素材绑定：" + ManmanBindDrawable.Value;
                            }
                            catch { }
                            Hint("当前来源：" + src);
                            Hint("鼠标在区内：" + (inRect ? "是" : "否")
                                + "   游戏当前命中：" + pointing);
                        }
                        else Hint("注意： 拿不到模型包围盒（人物不在场？）");
                    }
                    GUILayout.Label(string.Format("    当前矩形：x {0:0.000}~{1:0.000}  y {2:0.000}~{3:0.000}（屏幕比例）",
                        SitPussyAreaCX.Value - SitPussyAreaW.Value / 2f, SitPussyAreaCX.Value + SitPussyAreaW.Value / 2f,
                        SitPussyAreaCY.Value - SitPussyAreaH.Value / 2f, SitPussyAreaCY.Value + SitPussyAreaH.Value / 2f));
                }
                GUILayout.Space(4f);
            
                }


        // =================================================================
        // 「对手」拆成三栏：口交 / 背榨（背面骑乘·索取模式）/ 正骑（坐姿·榨取模式）
        //
        // 三栏各自有一份功能，且可互相拷贝（见每栏底部的「拷贝自」按钮）。
        // =================================================================

        /// <summary>
        /// 把一个栏（姿势）的设置拷到另一个栏。
        ///
        /// 目前真正能独立的是【连榨上限】那一组（口交 1 个 / 背榨 2 个 / 正骑 2 个）；
        /// 其余参数（索取欲门槛、攻击力、余韵…）还是全局共享的，
        /// 所以拷贝它们目前是空操作 —— 等它们也按姿势各存一套之后再补。
        /// </summary>
        internal static void CopyPoseSettings(string from, string to)
        {
            try
            {
                // 连榨上限那一组（不在 P3 体系里，单独搬）
                int norm = GetPoseLimit(from, false);
                int dem = GetPoseLimit(from, true);
                SetPoseLimit(to, false, norm);
                SetPoseLimit(to, true, dem);

                // 其余全部按姿势三份的参数，整体搬过去
                CopyPoseSettings2(PoseIdxOf(from), PoseIdxOf(to));

                Log.LogInfo("[面板] 拷贝设置：" + from + " → " + to
                            + "（含连榨上限与 " + _p3Groups.Count + " 组按姿势参数）");
            }
            catch (Exception e) { Log.LogWarning("[面板] 拷贝失败: " + e.Message); }
        }

        private static int PoseIdxOf(string pose)
        {
            if (pose == "口交") return 0;
            if (pose == "背榨") return 1;
            return 2;
        }

        private static int GetPoseLimit(string pose, bool demand)
        {
            if (pose == "口交") return ChainMaxFella.Value;
            if (pose == "背榨") return demand ? ChainMaxOsiriDemand.Value : ChainMaxOsiriNormal.Value;
            return demand ? ChainMaxSitDemand.Value : ChainMaxSitNormal.Value;
        }

        private static void SetPoseLimit(string pose, bool demand, int v)
        {
            if (pose == "口交") { ChainMaxFella.Value = v; return; }
            if (pose == "背榨")
            {
                if (demand) ChainMaxOsiriDemand.Value = v; else ChainMaxOsiriNormal.Value = v;
                return;
            }
            if (demand) ChainMaxSitDemand.Value = v; else ChainMaxSitNormal.Value = v;
        }

        private void DrawCopyRow(string self, string[] others)
        {
            GUILayout.Space(4f);
            Hint("本栏的设置与另外两栏【相互独立】，改一个不影响其他");
            Hint("（按姿势各存一份：_Fella / _Osiri / _Sit）");
            GUILayout.BeginHorizontal();
            GUILayout.Label("拷贝自：", GUILayout.Width(56f));
            foreach (string o in others)
            {
                if (GUILayout.Button(o))
                {
                    CopyPoseSettings(o, self);
                    _snapHint = "已把「" + o + "」的设置拷到「" + self + "」";
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawTabFella(object tabemi)
        {
            _editPose = 0;      // 本栏的滑块读写这一份
            Section("口交");
            Hint("这边的机制叫「吸取」（对应另外两栏的索取 / 榨取）");
            PowerUnlock.Value = GUILayout.Toggle(PowerUnlock.Value, "  攻击力强化");
            TabemiPowerMul.Value = SliderF("攻击力倍率", TabemiPowerMul.Value, 1f, 1000f, "×{0:0.#}", 10f);
            FellaSpeedPlus.Value = SliderF("口交速度加成", FellaSpeedPlus.Value, 0f, 1f, "{0:0.00}", 0.05f);
            GUILayout.Space(4f);
            ChainMaxFella.Value = SliderI("口交连榨上限", ChainMaxFella.Value, 0, 50);
            Hint("0 = 不限（有卡死游戏的风险）");
            GUILayout.Space(6f);
            GUILayout.Space(4f);
            HighlightHitAreas.Value = GUILayout.Toggle(HighlightHitAreas.Value,
                "  【实验性】高亮可点击区域（三栏共用）");
            DrawCopyRow("口交", new string[] { "背榨", "正骑" });
            GUILayout.Space(6f);
            if (GUILayout.Button("随机一套换衣") && tabemi != null)
            {
                InvokeMethod(tabemi, "RandomCostume");
                ApplyCostume(tabemi);
            }
        }

        private void DrawTabOsiri(object tabemi)
        {
            _editPose = 1;      // 本栏的滑块读写这一份
            Section("背榨 · 背面骑乘");
            Hint("这边的机制叫「索取」：打屁股攒索取欲 → 索取模式");
            GUILayout.Space(4f);
            OsiriKyuseiTier4.Value = GUILayout.Toggle(OsiriKyuseiTier4.Value,
                "  连榨撞击声固定用第 3、4 档（其余状态按速度分档）");
            DrawDemandSection(false);
            GUILayout.Space(6f);
            SitSpeedPlus.Value = SliderF("骑乘速度加成", SitSpeedPlus.Value, 0f, 1f, "{0:0.00}", 0.05f);
            GUILayout.Space(4f);
            ChainMaxOsiriNormal.Value = SliderI("骑乘位·常规 连榨上限", ChainMaxOsiriNormal.Value, 0, 50);
            ChainMaxOsiriDemand.Value = SliderI("骑乘位·索取 连榨上限", ChainMaxOsiriDemand.Value, 0, 50);
            GUILayout.Space(4f);
            HighlightHitAreas.Value = GUILayout.Toggle(HighlightHitAreas.Value,
                "  【实验性】高亮可点击区域（三栏共用）");
            DrawCopyRow("背榨", new string[] { "口交", "正骑" });
        }

        private void DrawTabSit(object tabemi)
        {
            _editPose = 2;      // 本栏的滑块读写这一份
            Section("正骑 · 坐姿");
            Hint("这边的机制叫「榨取」：点头部攒榨取欲 → 榨取模式");
            DrawDemandSection(true);
            GUILayout.Space(4f);
            ChainMaxSitNormal.Value = SliderI("坐姿·常规 连榨上限", ChainMaxSitNormal.Value, 0, 50);
            ChainMaxSitDemand.Value = SliderI("坐姿·榨取 连榨上限", ChainMaxSitDemand.Value, 0, 50);
            GUILayout.Space(4f);
            HighlightHitAreas.Value = GUILayout.Toggle(HighlightHitAreas.Value,
                "  【实验性】高亮可点击区域（三栏共用）");
            DrawCopyRow("正骑", new string[] { "口交", "背榨" });
        }


        /// <summary>「换衣·部件」合并栏：先换衣，再部件。</summary>
        /// <summary>「系统」栏：先上限，再系统。</summary>
        private void DrawTabSystemWithCaps(object player, object clock)
        {
            DrawTabCaps(player);
            GUILayout.Space(10f);
            Rule();
            GUILayout.Space(6f);
            DrawTabSystem(player, clock);
        }

        private void DrawTabCostumeAndParts(object tabemi)
        {
            Section("换装");
            DrawCostumeUi();
            Section("部件");
            DrawPartsUi();
        }

        private void DrawTabCostume(object tabemi)
        {
            DrawCostumeUi();
        }

        // ---- 页 5：部件（游戏从不启用的素材在这里手动开启） ----
        private static readonly List<string> _partNames = new List<string>();
        private static readonly List<float> _partNatural = new List<float>();
        private static readonly HashSet<string> _partForceOn = new HashSet<string>();
        private float _partRefreshTimer;

        /// <summary>列出模型里所有部件及其「游戏是否让它可见」的原始状态。</summary>
        private void RefreshPartList()
        {
            _partNames.Clear();
            _partNatural.Clear();

            Type camType = FindType("Live2D.Cubism.Core.CubismModel");
            if (camType == null) return;

            foreach (UnityEngine.Object mo in Resources.FindObjectsOfTypeAll(camType))
            {
                Component comp = mo as Component;
                if (comp == null) continue;
                UnityEngine.Object[] parts = null;
                try { parts = (UnityEngine.Object[])camType.GetProperty("Parts", AllFlags).GetValue(comp, null); }
                catch { }
                if (parts == null) continue;

                foreach (UnityEngine.Object po in parts)
                {
                    Component pc = po as Component;
                    if (pc == null) continue;
                    string nm = pc.name;
                    float op = 0f;
                    try
                    {
                        nm = pc.GetType().GetProperty("Id", AllFlags)?.GetValue(pc, null) as string ?? pc.name;
                        object opv = FieldQuiet(pc.GetType(), "Opacity")?.GetValue(pc);
                        if (opv is float f) op = f;
                    }
                    catch { }
                    if (nm.StartsWith("HitArea")) continue;   // 命中区不是画面素材
                    if (_partNames.Contains(nm)) continue;
                    _partNames.Add(nm);
                    _partNatural.Add(op);
                }
            }
            _partNames.Sort();
        }

        private void DrawPartsUi()
        {
            _partRefreshTimer += Time.unscaledDeltaTime;
            if (_partNames.Count == 0 || _partRefreshTimer > 5f)
            {
                _partRefreshTimer = 0f;
                RefreshPartList();
            }

            Hint("游戏从不启用的素材可以在这里手动开启。");
            Hint("(自然) = 自然可见（游戏在管）；(手动) = 已手动强制显示；空 = 隐藏");
            GUILayout.Label(string.Format("强制列表：{0} 项   总开关：{1}{2}",
                _partForceOn.Count,
                _partOverrideEnabled ? "开" : "关",
                _forceAllParts ? "   ← 「全部显示」生效中" : ""));

            if (GUILayout.Button("列出当前显示的部件"))
            {
                RefreshPartList();
                var onList = new List<string>();
                for (int i = 0; i < _partNames.Count && i < _partNatural.Count; i++)
                    if (_partNatural[i] >= 0.5f) onList.Add(_partNames[i]);
                Log.LogInfo("游戏侧为『显示』的部件（" + onList.Count + "/" + _partNames.Count + "）："
                            + string.Join(", ", onList.ToArray()));
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("全部显示"))
            {
                if (!_partOverrideEnabled) SnapshotPartOpacities();   // 开之前先记住原状
                _partOverrideEnabled = true;
                foreach (string n in _partNames) _partForceOn.Add(n);
                LogOpWithState("部件·全部显示", _partNames.Count + " 个部件");
            }
            if (GUILayout.Button("全部还原"))
            {
                // 总开关关掉 —— 逐个勾选、全部显示 两套一起失效；
                // 再把部件写回快照值，确保真的回到隐藏状态（只停覆盖是不够的）
                _partOverrideEnabled = false;
                _partForceOn.Clear();
                _forceAllParts = false;
                _lastForceSig = "";
                _partNaturalSnapshot.Clear();     // 不写回过期快照（见 SetForceAll 的说明）
                LogOpWithState("部件·全部还原", "强制已关闭");
            }
            GUILayout.EndHorizontal();

            if (_partNames.Count == 0) { GUILayout.Label("未取到部件（不在店内场景？）"); return; }

            foreach (string n in _partNames)
            {
                int i = _partNames.IndexOf(n);
                bool natural = i >= 0 && _partNatural[i] >= 0.5f;
                bool forced = _partForceOn.Contains(n);

                GUILayout.BeginHorizontal();
                string mark = natural ? "(自然)" : (forced ? "(手动)" : "·");
                GUILayout.Label(mark + " " + n, GUILayout.Width(190f));
                bool newForced = GUILayout.Toggle(forced, "强制", GUILayout.Width(60f));
                if (newForced != forced)
                {
                    if (newForced)
                    {
                        if (!_partOverrideEnabled) { SnapshotPartOpacities(); _partOverrideEnabled = true; }
                        _partForceOn.Add(n);
                        // 互斥组：黑白裤袜这类同层互斥的部件，开一个就关掉同组的另一个。
                        // 否则两层同时显示，换装切黑切白时看不出变化（用户实际踩过这个坑）。
                        foreach (var grp in ExclusiveGroups)
                        {
                            if (!grp.Contains(n)) continue;
                            foreach (string other in grp)
                            {
                                if (other == n) continue;
                                if (_partForceOn.Remove(other))
                                    Log.LogInfo("互斥：因开启 " + n + "，已取消强制 " + other);
                            }
                        }
                        LogOpWithState("部件·强制显示", n + "（列表 " + _partForceOn.Count + " 项）");
                    }
                    else
                    {
                        _partForceOn.Remove(n);
                        // 这里**不要**写回快照值！
                        // 快照是在"第一次勾选那一刻"拍的，会过期：例如当时换装是白色（White=1），
                        // 之后换成黑色，再取消勾选就会把这个过期的 1 写回去，把白裤袜钉死 ——
                        // 表现为"必须先把黑白都点一遍再解除，才能正常换装"。
                        // 交给 postfix：它每帧用「游戏本帧算出的值」重算，取消勾选自然回到游戏状态。
                        if (_partForceOn.Count == 0 && !_forceAllParts) _partOverrideEnabled = false;
                        LogOpWithState("部件·取消强制", n + "（列表 " + _partForceOn.Count + " 项）");
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        // ---- 页 6：系统 / 工具 ----
        // =================================================================
        // 游戏运行活动记录器 —— 接线部分
        // =================================================================

        private static readonly Dictionary<string, string> _eventKinds = new Dictionary<string, string>();

        /// <summary>记录一条"游戏响应"事件（同时写进操作流水与活动时间线）。</summary>
        internal static void EmitGame(string kind, string detail)
        {
            LogOp("[游戏] " + kind, detail);
            if (ActivityLogger.Running) ActivityLogger.EmitGame(kind, detail);
        }

        /// <summary>
        /// 通用游戏事件后缀：用「声明类型.方法名」反查事件种类。
        ///
        /// 挂在游戏自己的方法上（而不是每帧轮询），事件由**游戏行为**触发，
        /// 与"用户操作"时间线对齐时不会混入周期噪声。
        /// </summary>
        /// <summary>
        /// 关键：**状态去重**。
        ///
        /// UpdateCostume / UpdatePartOpacity 这类方法**每帧都在跑**，
        /// 直接记录会产生每秒几十条噪声（实测 28 秒 1119 条），把"用户操作"彻底淹没。
        /// 所以这里对每个事件种类维护一个"签名"，**只有签名变化时才记录一条**。
        /// 这样时间线里出现的才是真正的状态切换。
        /// </summary>
        private static readonly Dictionary<string, string> _lastEventSig = new Dictionary<string, string>();

        // =================================================================
        // 供命令接口（GameCommandServer）调用的公开包装
        // =================================================================

        /// <summary>按名字读 float（含私有字段）。</summary>
        internal static float GetFloatPublicRaw(object obj, string name)
        {
            try
            {
                if (obj == null) return -999f;
                FieldInfo fi = FieldQuiet(obj.GetType(), name);
                if (fi == null) return -999f;
                object v = fi.GetValue(obj);
                if (v is float f) return f;
                if (v is int i) return i;
                if (v is bool b) return b ? 1f : 0f;
            }
            catch { }
            return -999f;
        }

        /// <summary>通过命令接口改本修改器的配置（字符输入 → 按配置类型解析）。</summary>
        internal static string SetCfg(string name, string val)
        {
            try
            {
                float f = 0f;
                bool isNum = float.TryParse(val, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out f);
                bool b = val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase);
                // ================================================================
                // 【Schema 化 · 第一块】P3 参数走通用路径
                //
                // 原来每个"按姿势三份"的参数都要在下面那个 switch 里手写一行 case ——
                // 一共 54 行。漏写一个的后果是：面板能调、命令行却报「未知配置项」，
                // 而且【没有任何报错】（S1 那三条就是这么发现的）。
                //
                // 现在先查 `_p3ByName` 索引（BindP3 绑定时就登记好了）—— 命中即自动处理。
                // **新增一个 BindP3 参数，set 命令自动支持** 
                //
                // 下面那些手写的 P3 case 保留着（无害），但已不会被执行 ——
                // 删 54 行属纯清理、风险为零收益也为零，留着还能当"这批参数长什么样"的活文档。
                // ================================================================
                {{
                    object g;
                    if (_p3ByName.TryGetValue(name, out g))
                    {{
                        var gf = g as ConfigEntry<float>[];
                        if (gf != null)
                        {{
                            if (!isNum) return "ERR 需要数字";
                            SetP3(gf, f);
                        }}
                        else
                        {{
                            var gi = g as ConfigEntry<int>[];
                            if (gi != null)
                            {{
                                if (!isNum) return "ERR 需要数字";
                                SetP3(gi, (int)f);
                            }}
                            else
                            {{
                                var gb = g as ConfigEntry<bool>[];
                                if (gb == null) return "ERR 这个参数类型不认识: " + name;
                                SetP3(gb, b);
                            }}
                        }}
                          _pluginInstance.Config.Save();   // Config 是实例属性
                        return name + " = " + val + "  （已设置并落盘·按姿势三份）";
                    }}
                }}
                switch (name)
                {
                    case "NoEcstasy": NoEcstasy.Value = b; break;
                    case "LockMaxEcstasy": LockMaxEcstasy.Value = b; break;
                    case "LockMaxHp": LockMaxHp.Value = b; break;
                    case "GodMode": GodMode.Value = b; break;
                    case "UnlockAllDays": UnlockAllDays.Value = b; break;
                    case "BurgerTimeAll": if (!isNum) return "ERR 需要数字"; BurgerTimeAll.Value = f; break;
                    case "BurgerTimeSpeed": if (!isNum) return "ERR 需要数字"; BurgerTimeSpeed.Value = f; break;
                    case "EcstasyUpRate": if (!isNum) return "ERR 需要数字"; EcstasyUpRate.Value = f; break;
                    case "EcstasyDownRate": if (!isNum) return "ERR 需要数字"; EcstasyDownRate.Value = f; break;
                    case "EcstasyResist": if (!isNum) return "ERR 需要数字"; EcstasyResist.Value = f; break;
                    case "TargetEcstasy": if (!isNum) return "ERR 需要数字"; TargetEcstasy.Value = f; break;
                    case "TremorEnabled": TremorEnabled.Value = b; break;
                    case "TremorChance": if (!isNum) return "ERR 需要数字"; TremorChance.Value = f; break;
                    case "TremorDuration": if (!isNum) return "ERR 需要数字"; TremorDuration.Value = f; break;
                    case "TremorAmplitude": if (!isNum) return "ERR 需要数字"; TremorAmplitude.Value = f; break;
                    case "TremorLoss": if (!isNum) return "ERR 需要数字"; TremorLoss.Value = f; break;
                    case "TremorCatch": if (!isNum) return "ERR 需要数字"; TremorCatch.Value = f; break;
                    case "TremorCooldown": if (!isNum) return "ERR 需要数字"; TremorCooldown.Value = f; break;
                    case "TremorSlow": if (!isNum) return "ERR 需要数字"; TremorSlow.Value = f; break;
                    case "TremorPattern": if (!isNum) return "ERR 需要数字"; TremorPattern.Value = (int)f; break;
                    case "TremorWeaken": if (!isNum) return "ERR 需要数字"; TremorWeaken.Value = f; break;
                    case "TremorHpMaxBonus": if (!isNum) return "ERR 需要数字"; TremorHpMaxBonus.Value = f; break;
                    case "TremorHpGuard": if (!isNum) return "ERR 需要数字"; TremorHpGuard.Value = f; break;
                    case "TremorHpRegen": if (!isNum) return "ERR 需要数字"; TremorHpRegen.Value = f; break;
                    case "TremorAdaptTime": if (!isNum) return "ERR 需要数字"; TremorAdaptTime.Value = f; break;
                    case "TremorAdaptFactor": if (!isNum) return "ERR 需要数字"; TremorAdaptFactor.Value = f; break;
                    case "HpUpRate": if (!isNum) return "ERR 需要数字"; HpUpRate.Value = f; break;
                    case "HpDownRate": if (!isNum) return "ERR 需要数字"; HpDownRate.Value = f; break;
                    case "TargetHp": if (!isNum) return "ERR 需要数字"; TargetHp.Value = f; break;
                    case "GameSpeed": if (!isNum) return "ERR 需要数字"; GameSpeed.Value = f; break;
                    case "DemandEnabled": SetP3(DemandEnabled3, b); break;
                    case "DemandUrgeMin": if (!isNum) return "ERR 需要数字"; SetP3(DemandUrgeMin3, f); break;
                    case "DemandThresholdBase": if (!isNum) return "ERR 需要数字"; SetP3(DemandThresholdBase3, f); break;
                    case "DemandMaxStackRefreshMul": if (!isNum) return "ERR 需要数字"; SetP3(DemandMaxStackRefreshMul3, f); break;
                    case "SitClickAlwaysAccumulate": SitClickAlwaysAccumulate.Value = b; break;
                    case "HighlightHitAreas": HighlightHitAreas.Value = b; break;
                    case "ManmanDragMode": ManmanDragMode.Value = b; break;
                    case "ManmanBindDrawable": ManmanBindDrawable.Value = val; break;
                    case "ManmanDrawableFilter": ManmanDrawableFilter.Value = val; break;
                    case "ManmanDrawablePage": if (!isNum) return "ERR 需要数字"; ManmanDrawablePage.Value = (int)f; break;
                    case "SitKissAutoPrepare": SitKissAutoPrepare.Value = b; break;
                    case "KissSpeedEnabled": KissSpeedEnabled.Value = b; break;
                    case "EcstasyDropWarn": if (!isNum) return "ERR 需要数字"; EcstasyDropWarn.Value = f; break;
                    case "CapSmoothEnabled": CapSmoothEnabled.Value = b; break;
                    case "SitLockEnabled": SitLockEnabled.Value = b; break;
                    case "CapSmoothRate": if (!isNum) return "ERR 需要数字"; CapSmoothRate.Value = f; break;
                    // ── 连榨那一组（原来只有面板没有 set 分支）
                    case "ChainEcstasyGain": SetP3(ChainEcstasyGain3, b); break;
                    case "ChainGainPerDrain": if (!isNum) return "ERR 需要数字"; SetP3(ChainGainPerDrain3, f); break;
                    case "ChainGainPerHp": if (!isNum) return "ERR 需要数字"; SetP3(ChainGainPerHp3, f); break;
                    case "ChainSoftCap": if (!isNum) return "ERR 需要数字"; SetP3(ChainSoftCap3, f); break;
                    case "ChainWobble": if (!isNum) return "ERR 需要数字"; SetP3(ChainWobble3, f); break;
                    case "ChainWobbleHz": if (!isNum) return "ERR 需要数字"; SetP3(ChainWobbleHz3, f); break;
                    case "KissSpeedMul": if (!isNum) return "ERR 需要数字"; KissSpeedMul.Value = f; break;
                    case "KissSpeedWobble": if (!isNum) return "ERR 需要数字"; KissSpeedWobble.Value = f; break;
                    case "KissSpeedHz": if (!isNum) return "ERR 需要数字"; KissSpeedHz.Value = f; break;
                    case "OsiriKyuseiTier4": OsiriKyuseiTier4.Value = b; break;
                    case "SitKissAutoInterval": if (!isNum) return "ERR 需要数字"; SitKissAutoInterval.Value = f; break;
                    case "SitKissNoDecay": SitKissNoDecay.Value = b; break;
                    case "SitKissWindowMul": if (!isNum) return "ERR 需要数字"; SitKissWindowMul.Value = f; break;
                    case "AfterglowUrgeScale": if (!isNum) return "ERR 需要数字"; SetP3(AfterglowUrgeScale3, f); break;
                    case "AfterglowClearsUrge": SetP3(AfterglowClearsUrge3, b); break;
                    case "SitSyaseiToDrainChance": if (!isNum) return "ERR 需要数字"; SitSyaseiToDrainChance.Value = f; break;
                    case "AttackCanStackDemand": SetP3(AttackCanStackDemand3, b); break;
                    case "DemandTriggerMode": if (!isNum) return "ERR 需要数字"; SetP3(DemandTriggerMode3, (int)f); break;
                    case "DemandThresholdStep": if (!isNum) return "ERR 需要数字"; SetP3(DemandThresholdStep3, f); break;
                    case "DemandUrgeMax": if (!isNum) return "ERR 需要数字"; SetP3(DemandUrgeMax3, f); break;
                    case "DemandEscapePenaltyChance": if (!isNum) return "ERR 需要数字"; SetP3(DemandEscapePenaltyChance3, f); break;
                    case "DemandEscapePenaltyMul": if (!isNum) return "ERR 需要数字"; SetP3(DemandEscapePenaltyMul3, f); break;
                    case "DemandAttBonus": if (!isNum) return "ERR 需要数字"; SetP3(DemandAttBonus3, f); break;
                    case "DemandAttSpeedBonus": if (!isNum) return "ERR 需要数字"; SetP3(DemandAttSpeedBonus3, f); break;
                    case "DemandAttSpeedRef": if (!isNum) return "ERR 需要数字"; SetP3(DemandAttSpeedRef3, f); break;
                    case "DemandAttSpeedExp": if (!isNum) return "ERR 需要数字"; SetP3(DemandAttSpeedExp3, f); break;
                    case "DemandAnimSpeed": if (!isNum) return "ERR 需要数字"; SetP3(DemandAnimSpeed3, f); break;
                    case "DemandSpeedSplit": SetP3(DemandSpeedSplit3, b); break;
                    case "SpankSpeedEnabled": SetP3(SpankSpeedEnabled3, b); break;
                    case "AttackUrgeChance": if (!isNum) return "ERR 需要数字"; SetP3(AttackUrgeChance3, f); break;
                    case "AttackUrgeJitter": if (!isNum) return "ERR 需要数字"; SetP3(AttackUrgeJitter3, f); break;
                    case "SyaseiUrgeChance": if (!isNum) return "ERR 需要数字"; SetP3(SyaseiUrgeChance3, f); break;
                    case "SyaseiUrgeGain": if (!isNum) return "ERR 需要数字"; SetP3(SyaseiUrgeGain3, f); break;
                    case "KyuseiUrgeChance": if (!isNum) return "ERR 需要数字"; SetP3(KyuseiUrgeChance3, f); break;
                    case "KyuseiUrgeGain": if (!isNum) return "ERR 需要数字"; SetP3(KyuseiUrgeGain3, f); break;
                    case "AttackUrgeGain": if (!isNum) return "ERR 需要数字"; SetP3(AttackUrgeGain3, f); break;
                    case "DemandMaxStacks": if (!isNum) return "ERR 需要数字"; SetP3(DemandMaxStacks3, (int)f); break;
                    case "SpankSpeedGain": if (!isNum) return "ERR 需要数字"; SetP3(SpankSpeedGain3, f); break;
                    case "SpankSpeedStack": if (!isNum) return "ERR 需要数字"; SetP3(SpankSpeedStack3, f); break;
                    case "SpankSpeedRise": if (!isNum) return "ERR 需要数字"; SetP3(SpankSpeedRise3, f); break;
                    case "SpankSpeedDecay": if (!isNum) return "ERR 需要数字"; SetP3(SpankSpeedDecay3, f); break;
                    case "DemandBlockFella": SetP3(DemandBlockFella3, b); break;
                    case "DemandFellaDecay": if (!isNum) return "ERR 需要数字"; SetP3(DemandFellaDecay3, f); break;
                    case "DemandOsiriBoost": if (!isNum) return "ERR 需要数字"; SetP3(DemandOsiriBoost3, (int)f); break;
                    case "OsiriReturnEnabled": SetP3(OsiriReturnEnabled3, b); break;
                    case "OsiriReturnTrigger": if (!isNum) return "ERR 需要数字"; SetP3(OsiriReturnTrigger3, f); break;
                    case "OsiriReturnGain": if (!isNum) return "ERR 需要数字"; SetP3(OsiriReturnGain3, f); break;
                    case "OsiriReturnJitter": if (!isNum) return "ERR 需要数字"; SetP3(OsiriReturnJitter3, f); break;
                    case "DemandDurationMin": if (!isNum) return "ERR 需要数字"; SetP3(DemandDurationMin3, f); break;
                    case "DemandDurationMax": if (!isNum) return "ERR 需要数字"; SetP3(DemandDurationMax3, f); break;
                    case "DemandAfterglowMin": if (!isNum) return "ERR 需要数字"; DemandAfterglowMin.Value = (int)f; break;
                    case "AfterglowPerSyasei": if (!isNum) return "ERR 需要数字"; SetP3(AfterglowPerSyasei3, (int)f); break;
                    case "AfterglowSpeed": if (!isNum) return "ERR 需要数字"; SetP3(AfterglowSpeed3, f); break;
                    case "AfterglowSpeedWobble": if (!isNum) return "ERR 需要数字"; SetP3(AfterglowSpeedWobble3, f); break;
                    case "AfterglowSpeedHz": if (!isNum) return "ERR 需要数字"; SetP3(AfterglowSpeedHz3, f); break;
                    case "AfterglowSoftCap": if (!isNum) return "ERR 需要数字"; SetP3(AfterglowSoftCap3, f); break;
                    case "AfterglowEcstasyWobble": if (!isNum) return "ERR 需要数字"; SetP3(AfterglowEcstasyWobble3, f); break;
                    case "AfterglowUseDemandGain": SetP3(AfterglowUseDemandGain3, b); break;
                    case "AfterglowSettleDelay": if (!isNum) return "ERR 需要数字"; SetP3(AfterglowSettleDelay3, f); break;
                    case "DemandAfterglowMax": if (!isNum) return "ERR 需要数字"; DemandAfterglowMax.Value = (int)f; break;
                    case "manman":
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.Append("[HitArea_Manman_By_Plugins] 姿势=").Append(PoseIdx());
                        sb.Append("  启用=").Append(SitPussyAreaEnabled.Value);
                        try
                        {
                            Type hc0 = FindType("Live2D_HitAreaCheck");
                            FieldInfo pf0 = hc0 != null ? FieldQuiet(hc0, "mousePointing") : null;
                            sb.Append("  游戏当前命中=").Append(pf0 != null ? ((pf0.GetValue(null) as string) ?? "(空)") : "?");
                        }
                        catch { }
                        float rx0, ry0, rx1, ry1;
                        if (PussyAreaScreenRect(out rx0, out ry0, out rx1, out ry1))
                        {
                            sb.Append(string.Format("  矩形={0:0}~{1:0}x{2:0}~{3:0}", rx0, rx1, ry0, ry1));
                            Vector3 mpv = Input.mousePosition;
                            sb.Append("  鼠标=").Append(mpv.x.ToString("0")).Append(",").Append(mpv.y.ToString("0"));
                            sb.Append("  在区内=").Append((mpv.x >= rx0 && mpv.x <= rx1 && mpv.y >= ry0 && mpv.y <= ry1) ? "是" : "否");
                        }
                        else sb.Append("  矩形=拿不到（人物不在场？）");
                        string srcm = "插件自建矩形";
                        try
                        {
                            float q0, q1, q2, q3;
                            if (TryGetDrawableScreenRect("HitArea_Manman", out q0, out q1, out q2, out q3))
                                srcm = "模型里的 HitArea_Manman";
                            else if (!string.IsNullOrEmpty(ManmanBindDrawable.Value))
                                srcm = "素材绑定：" + ManmanBindDrawable.Value;
                        }
                        catch { }
                        sb.Append("  来源=").Append(srcm);
                        return sb.ToString();
                    }
                    default: return "ERR 未知配置项: " + name;
                }
                // 【必须显式落盘】BepInEx 改 ConfigEntry.Value 只改内存，不会自动写 cfg，
                // 重启后会从文件重读旧值 —— 玩家会看到"设置被静默重置"。
                // 落盘走 SaveCfgSafely：取锁 + 核对修订号 + 必要时先合并对方的改动。
                string note = SaveCfgSafely(name, val);

                return name + " = " + val + "  （已设置并落盘）" + note;
            }
            catch (Exception e) { return "ERR " + e.Message; }
        }

        /// <summary>cfg 的绝对路径（给 CfgGuard 用）。</summary>
        internal static string CfgPath()
        {
            try { return _pluginInstance != null ? _pluginInstance.Config.ConfigFilePath : null; }
            catch { return null; }
        }

        /// <summary>
        /// 安全落盘：走 CfgGuard 的"取锁 → 核对修订号 → 必要时先重读采纳对方的值 → 再写本次改动"。
        ///
        /// 为什么不能直接 Config.Save()：BepInEx 的 Save 是【用内存里的全部值重写整个文件】，
        /// 训练器在此期间写进去的东西会被整片抹掉。
        /// </summary>
        private static string SaveCfgSafely(string reapplyKey = null, string reapplyVal = null)
        {
            string cfg = CfgPath();
            if (cfg == null || _pluginInstance == null)
            {
                try { if (_pluginInstance != null) _pluginInstance.Config.Save(); } catch { }
                return "";
            }

            bool changed;
            CfgGuard.Lock lk = CfgGuard.PrepareWrite(cfg, out changed);
            if (lk == null) return "  注意： 另一个写入者正在写（训练器？），本次未落盘，请稍后重试";

            string note = "";
            try
            {
                // 落盘前把 SaveOnConfigSet 关掉：
                // BepInEx 默认"每次设 ConfigEntry.Value 就立刻写文件"，
                // 于是合并过程中每一次赋值都会落一次盘 —— 中间态会被写出去，
                // 而且 Config.Reload() 本身也会因为赋值而把【内存里的旧值】先写回文件，
                // 反手把对方刚写的东西盖掉（这个坑实测踩到了）。
                bool prevAuto = _pluginInstance.Config.SaveOnConfigSet;
                _pluginInstance.Config.SaveOnConfigSet = false;
                try
                {
                    if (changed)
                    {
                        // 对方改过 → 逐个把【文件里的值】读进内存（本次要改的那个键除外），
                        // 这样随后的 Save() 会把对方的值原样写回去，而不是用内存里的旧值覆盖。
                        // 只对本次这一个键重放我们的新值 —— 面板/命令一次只改一个，
                        // 于是对方的值与我们的值都保得住。
                        note = "  （检测到外部改动，已按文件合并）";
                        int merged = 0;
                        foreach (KeyValuePair<ConfigDefinition, ConfigEntryBase> kv in _pluginInstance.Config)
                        {
                            if (reapplyKey != null && kv.Key.Key == reapplyKey) continue;
                            string fv = ReadValueFromFile(cfg, kv.Key.Section, kv.Key.Key);
                            if (fv == null) continue;
                            if (ApplyToEntry(kv.Value, fv)) merged++;
                        }
                        if (merged > 0) note += "（并入 " + merged + " 项）";
                        else if (_mergeDebugLeft > 0)
                        {
                            _mergeDebugLeft--;
                            Log.LogWarning("[合并] 一项都没并进去。抽查：条目数=" + CountCfgEntries()
                                + "  DemandAttBonus 在文件里读到="
                                + (ReadValueFromFile(cfg, "1-Player", "DemandAttBonus") ?? "(null)")
                                + "  文件存在=" + System.IO.File.Exists(cfg));
                        }
                        if (reapplyKey != null) ReapplyOne(reapplyKey, reapplyVal);
                    }
                    _pluginInstance.Config.Save();
                    CfgGuard.Bump(cfg);
                }
                finally { _pluginInstance.Config.SaveOnConfigSet = prevAuto; }
            }
            finally { lk.Dispose(); }
            return note;
        }

        private static int CountCfgEntries()
        {
            try { int n = 0; foreach (var kv in _pluginInstance.Config) n++; return n; }
            catch { return -1; }
        }

        /// <summary>从 cfg 文本里取指定分节下的某个键的当前值。取不到返回 null。</summary>
        private static string ReadValueFromFile(string cfg, string section, string key)
        {
            try
            {
                if (!System.IO.File.Exists(cfg)) return null;
                string cur = null;
                foreach (string raw in System.IO.File.ReadAllLines(cfg, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    if (line.Length == 0) continue;
                    if (line.StartsWith("#")) continue;
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        cur = line.Substring(1, line.Length - 2).Trim();
                        continue;
                    }
                    if (cur != section) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    if (line.Substring(0, eq).Trim() == key)
                        return line.Substring(eq + 1).Trim();
                }
            }
            catch { }
            return null;
        }

        /// <summary>把字符串按条目自身的类型写进内存（不触发落盘，调用方已关掉 SaveOnConfigSet）。</summary>
        private static bool ApplyToEntry(ConfigEntryBase e, string val)
        {
            try
            {
                if (e == null || val == null) return false;
                if (e.SettingType == typeof(bool))
                {
                    bool b = val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase);
                    if ((bool)e.BoxedValue == b) return false;
                    e.BoxedValue = b; return true;
                }
                if (e.SettingType == typeof(int))
                {
                    int i = (int)double.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                    if ((int)e.BoxedValue == i) return false;
                    e.BoxedValue = i; return true;
                }
                if (e.SettingType == typeof(float))
                {
                    float f = float.Parse(val, System.Globalization.CultureInfo.InvariantCulture);
                    if (Math.Abs((float)e.BoxedValue - f) < 1e-6f) return false;
                    e.BoxedValue = f; return true;
                }
                if (e.SettingType == typeof(string))
                {
                    if ((string)e.BoxedValue == val) return false;
                    e.BoxedValue = val; return true;
                }
            }
            catch { }
            return false;
        }

        /// <summary>重载之后，把本次的改动重新按上去。</summary>
        private static void ReapplyOne(string key, string val)
        {
            try
            {
                // 这个 BepInEx 版本没有 ConfigFile.GetEntry，用索引器。
                ConfigDefinition def = new ConfigDefinition("1-Player", key);
                if (!_pluginInstance.Config.ContainsKey(def)) return;
                ApplyToEntry(_pluginInstance.Config[def], val);
            }
            catch { }
        }

        internal static string DumpCfg()
        {
            var sb = new StringBuilder("配置: ");
            string[] names = { "NoEcstasy", "LockMaxEcstasy", "LockMaxHp", "GodMode", "HpUpRate", "HpDownRate",
                               "EcstasyUpRate", "EcstasyDownRate", "EcstasyResist", "TargetHp", "TargetEcstasy", "GameSpeed" };
            foreach (string n in names) sb.Append(n).Append("=").Append(GetRateCfg(n).ToString("0.##")).Append("  ");
            return sb.ToString();
        }

        /// <summary>读本修改器的某个配置项当前值（供命令接口核对设置）。</summary>
        internal static float GetRateCfg(string name)
        {
            try
            {
                switch (name)
                {
                    case "EcstasyUpRate": return EcstasyUpRate.Value;
                    case "EcstasyDownRate": return EcstasyDownRate.Value;
                    case "EcstasyResist": return EcstasyResist.Value;
                    case "TargetEcstasy": return TargetEcstasy.Value;
                    case "LockMaxEcstasy": return LockMaxEcstasy.Value ? 1f : 0f;
                    case "NoEcstasy": return NoEcstasy.Value ? 1f : 0f;
                    case "LockMaxHp": return LockMaxHp.Value ? 1f : 0f;
                    case "GodMode": return GodMode.Value ? 1f : 0f;
                    case "HpUpRate": return HpUpRate.Value;
                    case "HpDownRate": return HpDownRate.Value;
                }
            }
            catch { }
            return -999f;
        }

        internal static void SetFieldPublic(object obj, string name, int value)
        {
            SetField(obj, name, value);
        }

        internal static void InvokeMethodPublic(object obj, string name)
        {
            InvokeMethod(obj, name);
        }

        internal static float GetFloatPublic(object obj, string name)
        {
            return GetFloat(obj, name);
        }

        internal static void SetForceAll(bool on)
        {
            _forceAllParts = on;
            _partOverrideEnabled = on;
            _lastForceSig = "";
            // 注意：**不要**在这里写回 _partNaturalSnapshot！
            // 那个快照可能来自很早以前的一次单独强制，写回去会把部件透明度永久改坏
            // （实测会把 Parts[19] Tights 置 1，并让 40/39 归零）。
            // 恢复的正确做法是"停止覆盖"，让游戏下一帧自己按 _Op_ 字段重算。
            if (!on) { _partForceOn.Clear(); _partNaturalSnapshot.Clear(); }
            Log.LogInfo("[命令] 强制显示全部 = " + on);
        }

        internal static void SetPartForce(string partName, bool on)
        {
            if (on)
            {
                if (!_partOverrideEnabled) { SnapshotPartOpacities(); _partOverrideEnabled = true; }
                _partForceOn.Add(partName);
            }
            else
            {
                _partForceOn.Remove(partName);
                if (_partForceOn.Count == 0 && !_forceAllParts) _partOverrideEnabled = false;
            }
            _lastForceSig = "";
            Log.LogInfo("[命令] 部件 " + partName + " 强制 = " + on);
        }

        // ---------------- 剧情快进（供命令接口使用） ----------------
        private static bool _forceSkipDialog;
        private static FieldInfo _skipField;

        internal static void SetSkipDialog(bool on)
        {
            _forceSkipDialog = on;
            Log.LogInfo("[命令] 快进剧情 = " + on);
        }

        /// <summary>
        /// FullScreenDialogueSystem.Update 的后缀：把 skiping 置 true，让对话自动快速推进。
        /// 原生逻辑用「按住 Ctrl」来设置这个字段，脚本无法注入按键，所以这里直接置位。
        /// </summary>
        /// <summary>
        /// 对话快进。
        ///
        /// 为什么必须"前缀 + 返回 false 接管"：
        /// 原版 Update 的第一件事就是
        ///     if (按住 Ctrl) skiping = true; else skiping = false;
        /// —— **无条件覆盖** skiping。所以无论在前缀还是后缀里置位，都会被这一行抹掉，
        /// 检查语句永远看不到 true。（我已经用前缀试过一次，同样无效。）
        ///
        /// 因此这里在前缀里把原方法整个跳过，自己复刻它的推进逻辑，
        /// 并把 skiping 视作 true（等价于"一直按住 Ctrl"）。
        /// </summary>
        private static bool Postfix_DialogUpdate(object __instance)
        {
            try
            {
                if (!_forceSkipDialog || __instance == null) return true;   // 不开快进 → 走原逻辑

                Type t = __instance.GetType();

                // skiping = true（原版在这个分支下还会把 GameSpeed 设为 2）
                FieldInfo sf = FieldQuiet(t, "skiping") ?? (_skipField = _skipField ?? FieldQuiet(t, "skiping"));
                if (sf != null) sf.SetValue(__instance, true);

                int ts = (int)GetFloat(__instance, "typingState");
                if (ts >= 1)
                {
                    // 复刻原逻辑：停协程 → 补完文字 → 进入下一状态
                    try
                    {
                        MethodInfo stop = typeof(MonoBehaviour).GetMethod("StopAllCoroutines",
                            BindingFlags.Instance | BindingFlags.Public);
                        if (stop != null) stop.Invoke(__instance, null);
                    }
                    catch { }

                    if (ts == 1)
                    {
                        try
                        {
                            bool chinese = GetFloat(__instance, "chinese") > 0.5f;
                            string present = GetString(__instance, "presentDialogue");
                            string next = GetString(__instance, "newDialogue");
                            object textComp = FieldQuiet(t, chinese ? "dialogueTextLegacy" : "dialogueText")?.GetValue(__instance);
                            if (textComp != null)
                            {
                                PropertyInfo tp = textComp.GetType().GetProperty("text",
                                    BindingFlags.Instance | BindingFlags.Public);
                                if (tp != null) tp.SetValue(textComp, present + next, null);
                            }
                            SetString(__instance, "presentDialogue", present + next);

                            MethodInfo cor = t.GetMethod("TypingState_2", AllFlags);
                            MethodInfo startCor = typeof(MonoBehaviour).GetMethod("StartCoroutine",
                                BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(System.Collections.IEnumerator) }, null);
                            if (cor != null && startCor != null)
                            {
                                object ie = cor.Invoke(__instance, null);
                                if (ie != null) startCor.Invoke(__instance, new object[] { ie });
                            }
                        }
                        catch (Exception e) { Log.LogWarning("[快进] 推进失败: " + e.Message); }
                    }
                }

                // 原版还会在这里设 GameSpeed
                try
                {
                    Type gm = FindType("GameManager");
                    FieldInfo gsf = gm != null ? FieldQuiet(gm, "GameSpeed") : null;
                    if (gsf != null) gsf.SetValue(null, 2f);
                }
                catch { }

                return false;   // 跳过原 Update（它的 skiping 覆盖逻辑不再执行）
            }
            catch (Exception e)
            {
                Log.LogWarning("[快进] 异常: " + e.Message);
                return true;
            }
        }

        private static string GetString(object obj, string field)
        {
            try
            {
                FieldInfo fi = FieldQuiet(obj.GetType(), field);
                object v = fi != null ? fi.GetValue(obj) : null;
                return v as string ?? "";
            }
            catch { return ""; }
        }

        private static void SetString(object obj, string field, string val)
        {
            try
            {
                FieldInfo fi = FieldQuiet(obj.GetType(), field);
                if (fi != null) fi.SetValue(obj, val);
            }
            catch { }
        }

        private static void PatchDialogSkip()
        {
            try
            {
                MethodInfo post = typeof(Plugin).GetMethod("Postfix_DialogUpdate", AllFlags);
                if (post == null) { Log.LogWarning("剧情快进挂载失败：找不到回调方法"); return; }
                int ok = 0;
                foreach (string tn in new string[] { "FullScreenDialogueSystem", "WindowedDialogueSystem" })
                {
                    Type t = FindType(tn);
                    if (t == null) { Log.LogWarning("找不到 " + tn); continue; }
                    MethodInfo m = t.GetMethod("Update", AllFlags);
                    if (m == null) { Log.LogWarning(tn + " 没有 Update"); continue; }
                    _harmony.Patch(m, new HarmonyMethod(post), null, null);
                    ok++;
                    Log.LogInfo("剧情快进补丁已挂载（" + tn + ".Update 前缀，返回 false 接管）");
                }
                if (ok == 0) Log.LogWarning("剧情快进：一个都没挂上");
            }
            catch (Exception e) { Log.LogWarning("挂剧情快进补丁失败：" + e.Message); }
        }

        internal static string ForceStateText()
        {
            if (!_partOverrideEnabled) return "OFF";
            if (_forceAllParts) return "ALL";
            return string.Join(",", new List<string>(_partForceOn).ToArray());
        }

        /// <summary>供命令接口触发各类诊断。</summary>
        internal static void RunProbe(string which)
        {
            Plugin inst = _pluginInstance;
            if (inst == null) { Log.LogWarning("[命令] 插件实例不可用"); return; }
            switch (which.ToLowerInvariant())
            {
                case "masks": inst.DumpMasks(); break;
                case "partindex": inst.DumpPartIndexMap(); break;
                case "parttable": inst.DumpPartTable(); break;
                case "atlas": inst.ProbeRuntimeAtlas(); break;
                case "vmap": inst.DumpVertexMap(); break;
                case "diag": inst.DumpDiagnostics(); break;
                case "tightslog": StartTightsSampling(); break;
                case "tightstest": StartTightsTest(); break;
                default: Log.LogWarning("[命令] 未知 probe: " + which); break;
            }
        }

        private static void Postfix_GameEvent_Generic(object __instance, MethodBase __originalMethod)
        {
            try
            {
                if (!ActivityLogger.Running) return;
                string key = null;
                if (__originalMethod != null && __originalMethod.DeclaringType != null)
                    key = __originalMethod.DeclaringType.Name + "." + __originalMethod.Name;
                if (key == null) return;

                string kind;
                if (!_eventKinds.TryGetValue(key, out kind)) kind = __originalMethod.Name;

                // 用当前关键状态做签名；不变就不记
                string sig = EventSignature(kind);
                string prev;
                if (_lastEventSig.TryGetValue(key, out prev) && prev == sig) return;
                _lastEventSig[key] = sig;

                if (__instance != null) { }     // 保留签名，便于将来按实例区分
                ActivityLogger.EmitGame(kind, sig);
            }
            catch { }
        }

        /// <summary>按事件种类取一个能代表"当前状态"的签名，用于去重。</summary>
        private static string EventSignature(string kind)
        {
            try
            {
                switch (kind)
                {
                    case "costume_change":
                    case "costume_random":
                    {
                        object t = Tabemi();
                        if (t == null) return "?";
                        return string.Format("cap={0} upper={1} lower={2} tights={3} glasses={4}",
                            GetFloat(t, "Cap"), GetFloat(t, "Upper"), GetFloat(t, "Lower"),
                            GetFloat(t, "Tights"), GetFloat(t, "Glasses"));
                    }
                    case "part_opacity":
                    case "title_part_opacity":
                    {
                        object t = Tabemi();
                        object mdl = t != null ? FieldQuiet(t.GetType(), "model")?.GetValue(t) : null;
                        if (mdl == null) return "?";
                        return string.Format("T={0:0.#} B={1:0.#} W={2:0.#}",
                            GetFloat(mdl, "_Op_CenterGirlSitting_Tights"),
                            GetFloat(mdl, "_Op_CenterGirlSitting_Tights_Black"),
                            GetFloat(mdl, "_Op_CenterGirlSitting_Tights_White"));
                    }
                    case "ecstasy_change":
                    case "ecstasy_gauge":
                    case "ecstasy_reset":
                    case "hp_change":
                    case "hp_gauge":
                    {
                        object p = Player();
                        if (p == null) return "?";
                        // 数值类方法**每帧都在跑**，而槽位是连续变化的（实测有每 10ms 一条的段落），
                        // 所以这里不按整数取值，而是按**有意义的闸门**做签名：
                        //   HP 每 5 点、绝顶每 10%、上限每 10 点、射精次数每次变化
                        // 这样时间线里留下的才是"打了一发""掉了一截"这种可读事件。
                        return string.Format("hp{0}/max{1} ecs{2}/max{3} syasei{4}",
                            (int)(GetFloat(p, "currentHP") / 5f),
                            (int)(GetFloat(p, "maxHP") / 10f),
                            (int)(GetFloat(p, "CurrentEcstasy") / 10f),
                            (int)(GetFloat(p, "maxEcstasy") / 10f),
                            (int)GetFloat(p, "syaseiCount"));
                    }
                }
            }
            catch { }
            return "";
        }

        /// <summary>给一个游戏方法挂"状态变更"记录补丁。</summary>
        private static bool PatchGameEvent(string typeName, string methodName, string kind)
        {
            try
            {
                Type t = FindType(typeName);
                if (t == null) return false;
                MethodInfo m = t.GetMethod(methodName, AllFlags);
                if (m == null) return false;
                MethodInfo post = typeof(Plugin).GetMethod("Postfix_GameEvent_Generic", AllFlags);
                if (post == null) return false;
                _harmony.Patch(m, null, new HarmonyMethod(post), null);
                _eventKinds[typeName + "." + methodName] = kind;
                return true;
            }
            catch (Exception e)
            {
                Log.LogWarning("挂事件补丁失败 " + typeName + "." + methodName + "：" + e.Message);
                return false;
            }
        }

        /// <summary>挂上游戏状态变更的记录点。</summary>
        private void PatchActivityEvents()
        {
            int ok = 0;
            string[][] targets = new string[][]
            {
                // 换装 / 模型
                new string[] { "TabemiControl", "UpdateCostume",   "costume_change" },
                new string[] { "TabemiControl", "RandomCostume",   "costume_random" },
                new string[] { "Live2D_ModelControl", "UpdatePartOpacity", "part_opacity" },
                // 状态数值
                new string[] { "PlayerControl", "HPChange",                 "hp_change" },
                new string[] { "PlayerControl", "HPGaugeChangeValue",       "hp_gauge" },
                new string[] { "PlayerControl", "EcstasyChange",            "ecstasy_change" },
                new string[] { "PlayerControl", "ECstasyReset",             "ecstasy_reset" },
                new string[] { "PlayerControl", "EcstasyGaugeChangePercent","ecstasy_gauge" },
                // 资源 / 场景
                new string[] { "DLCManager", "LoadAssetBundle", "dlc_load" },
                new string[] { "DLCManager", "SetUpDLCStaffs",  "dlc_swap_texture" },
                new string[] { "TitleControl", "UpdatePartOpacity", "title_part_opacity" },
            };

            var missed = new List<string>();
            foreach (string[] t in targets)
            {
                if (PatchGameEvent(t[0], t[1], t[2])) ok++;
                else missed.Add(t[0] + "." + t[1]);
            }

            Log.LogInfo("活动记录：已挂 " + ok + "/" + targets.Length + " 个游戏事件记录点"
                        + (missed.Count > 0 ? "；未找到：" + string.Join(", ", missed.ToArray()) : ""));
        }

        private void DrawTabSystem(object player, object clock)
        {
            FrozenClock.Value = GUILayout.Toggle(FrozenClock.Value, "  冻结时钟（无限待在店里）");
            ClockFullTime.Value = SliderI("一天总时长（秒）", ClockFullTime.Value, 60, 3600);
            GameSpeed.Value = SliderF("游戏速度", GameSpeed.Value, 0.5f, 5f, "×{0:0.00}", 0.1f);
            UnlockAllDays.Value = GUILayout.Toggle(UnlockAllDays.Value, "  解锁全部章节");

            // ---- 汉堡制作倒计时 ----
            Section("汉堡制作倒计时");
            Hint("下单后限时做出汉堡，超时算失败。这里调的是那个倒计时。");
            BurgerTimeAll.Value = SliderF("每单时长（秒，0 = 原版）", BurgerTimeAll.Value, 0f, 120f, "{0:0.#}", 0.5f);
            if (BurgerTimeAll.Value < 0.01f) Hint("当前用游戏原版：10 秒");
            BurgerTimeSpeed.Value = SliderF("倒计时速度（%）", BurgerTimeSpeed.Value, 0f, 300f, "{0:0}", 5f);
            if (BurgerTimeSpeed.Value < 1f)
                Hint("0 = 冻结：倒计时不走，可以慢慢做。");
            else if (BurgerTimeSpeed.Value < 100f)
                Hint("慢放：时间过得更慢，等于多给你时间。");
            else if (BurgerTimeSpeed.Value > 100f)
                Hint("加速：时间过得更快。");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("原版（10 秒 / 原速）"))
            {
                BurgerTimeAll.Value = 0f; BurgerTimeSpeed.Value = 100f;
                _snapHint = "倒计时已还原为原版";
            }
            if (GUILayout.Button("冻结倒计时"))
            {
                BurgerTimeSpeed.Value = 0f;
                _snapHint = "倒计时已冻结（可慢慢做）";
            }
            if (GUILayout.Button("宽松（30 秒 / 半速）"))
            {
                BurgerTimeAll.Value = 30f; BurgerTimeSpeed.Value = 50f;
                _snapHint = "已设为 30 秒 + 半速";
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            Section("取证 / 自检");
            GUILayout.BeginHorizontal();
            Sub("截图与导出");
            if (GUILayout.Button("截图存档")) { CaptureSnapshot(); _snapHint = "已保存到 snap_<时间戳>\\"; }
            Sub("自检");
            if (GUILayout.Button("全量自检")) { SelfTestAll(); _snapHint = "自检完成，见 selftest.txt"; }
            if (GUILayout.Button("换衣自检")) { SelfTestCostume(); _snapHint = "换衣自检完成"; }
            GUILayout.EndHorizontal();

            // 一排最多 2~3 个，别横着堆太多（面板宽度有限）
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("补丁检查")) _snapHint = VerifyPatches();
            if (GUILayout.Button("诊断导出")) { LogOp("诊断·导出", "用户点了导出"); DumpDiagnostics(); _snapHint = "已导出到 diag_<时间戳>\\"; }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("导出运行时贴图")) { ExportRuntimeTextures(); _snapHint = "已导出 runtime_tex\\*.png"; }
            if (GUILayout.Button("打开产物目录")) OpenDumpDir();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("强制显示全部部件"))
            {
                bool turningOn = !_forceAllParts;
                if (turningOn && !_partOverrideEnabled) SnapshotPartOpacities();
                _forceAllParts = turningOn;
                _partOverrideEnabled = turningOn;   // 与总开关联动
                _lastForceSig = "";
                if (!turningOn)
                {
                    _partForceOn.Clear();
                    RestorePartOpacities();
                }
                _snapHint = turningOn ? "已强制显示全部部件（实验）" : "已停止强制显示（并已还原）";
            }
            if (GUILayout.Button(_autoExport ? "(停止) 停止连续记录" : "(记录) 连续记录(0.5秒一次)"))
            {
                _autoExport = !_autoExport;
                if (_autoExport) _autoDir = "";      // 每次开始记录换一个新时间戳目录
                LogOp("诊断·连续记录", _autoExport ? "开始（每 0.5 秒落一份）" : "停止");
                _snapHint = _autoExport ? "连续记录中：每次落一个 diag_<时间戳> 目录" : "已停止连续记录";
            }
            Sub("记录");
            if (GUILayout.Button(ActivityLogger.Running ? "(停止) 停止活动记录" : "(记录) 开始活动记录"))
            {
                if (ActivityLogger.Running) ActivityLogger.StopLogging(); else ActivityLogger.StartLogging();
                _snapHint = ActivityLogger.Running ? "记录中：activity_<时间戳>\\" : "已停止并写出 summary.txt";
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            Sub("换装测试");
            if (GUILayout.Button("自动裤袜截图测试")) { StartTightsTest(); _snapHint = "自动依次设 0/1/2 并截图，约 3 秒"; }
            if (GUILayout.Button("丝袜状态采样(10秒)")) { StartTightsSampling(); _snapHint = "采样中，10 秒后落盘"; }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            Sub("部件与图集");
            if (GUILayout.Button("部件索引对照")) { _snapHint = DumpPartIndexMap() ? "已导出 partindex 目录" : "未找到模型（需在店内场景）"; }
            if (GUILayout.Button("盘点运行时图集")) { ProbeRuntimeAtlas(); _snapHint = "见 runtime_atlas.txt / 控制台"; }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("导出顶点映射")) { DumpVertexMap(); _snapHint = "已导出 vmap\vmap.tsv"; }
            if (GUILayout.Button("列出隐藏部件")) _snapHint = ListHiddenParts();
            GUILayout.EndHorizontal();
            GUILayout.Label(_forceAllParts
                ? "实验中：所有部件透明度被拉到 1（用于判断某件衣服到底有没有图）"
                : "「强制显示」可判断某件衣服是不是只是透明度没开");

            GUILayout.Space(4f);
            Sub("危险操作");
            if (GUILayout.Button("重置全部开关"))
            {
                GodMode.Value = false;
                NoEcstasy.Value = false;
                LockMaxHp.Value = false;
                LockMaxEcstasy.Value = false;
                PowerUnlock.Value = false;
                FrozenClock.Value = false;
                GameSpeed.Value = 1f;
                EcstasyResist.Value = 0f;
                TargetHp.Value = 100f;
                TargetEcstasy.Value = 100f;
            }
        }

        // -----------------------------------------------------------------
        // 换衣
        // 机制：TabemiControl 有 5 个槽位（Cap / Upper / Lower / Tights / Glasses），
        // 它自己的 Update() 与 CustomUIManager.LateUpdate() 每帧调用 UpdateCostume()，
        // 把槽位写进 model._Op_CenterGirlSitting_* / Glasses*_Opacity，所以改完自动生效。
        // -----------------------------------------------------------------
        private static readonly string[] SlotFields = { "Cap", "Upper", "Lower", "Tights", "Glasses" };
        private static readonly string[] SlotLabels = { "头部", "上半身", "下半身", "裤袜", "眼镜" };
        private static readonly int[] SlotCounts = { 2, 2, 5, 3, 3 };
        private static readonly int[] SlotDefaults = { 0, 0, 0, 0, 0 };

        private static GUIStyle _slotLabelStyle;

        private void DrawCostumeUi()
        {
            object tabemi = Tabemi();
            if (tabemi == null)
            {
                Hint("未进入店内场景");
                return;
            }

            if (_slotLabelStyle == null)
            {
                _slotLabelStyle = new GUIStyle(GUI.skin.label);
                _slotLabelStyle.fixedWidth = 54f;
                _slotLabelStyle.alignment = TextAnchor.MiddleLeft;
            }

            for (int i = 0; i < SlotFields.Length; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(SlotLabels[i], _slotLabelStyle);

                int cur = (int)GetFloat(tabemi, SlotFields[i]);
                for (int v = 0; v < SlotCounts[i]; v++)
                {
                    GUIStyle style = GUI.skin.button;
                    if (cur == v) style = HighlightStyle(style);
                    if (GUILayout.Button(v == 0 ? "无" : v.ToString(), style, GUILayout.Width(34f)))
                    {
                        SetField(tabemi, SlotFields[i], v);
                        ApplyCostume(tabemi);
                        // 换装是最常出问题的一类操作：记录"选了哪项"以及"游戏随后给出的状态"
                        LogOpWithState("换装·" + SlotLabels[i], "选 " + (v == 0 ? "无" : v.ToString()));
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("随机一套"))
            {
                InvokeMethod(tabemi, "RandomCostume");
                ApplyCostume(tabemi);
                LogOpWithState("换装·随机一套", "调 RandomCostume()");
            }
            if (GUILayout.Button("恢复默认"))
            {
                for (int i = 0; i < SlotFields.Length; i++)
                    SetField(tabemi, SlotFields[i], SlotDefaults[i]);
                ApplyCostume(tabemi);
                LogOpWithState("换装·恢复默认", "五槽位回默认值");
            }
            GUILayout.EndHorizontal();

            // 丝袜基础层的可验证读数（游戏自己从不开这一层，插件代为补开）
            int t = (int)GetFloat(tabemi, "Tights");
            float baseOp = 0f;
            object mdl = FieldQuiet(tabemi.GetType(), "model")?.GetValue(tabemi);
            if (mdl != null)
            {
                object v = FieldQuiet(mdl.GetType(), "_Op_CenterGirlSitting_Tights")?.GetValue(mdl);
                if (v is float f) baseOp = f;
            }
            GUILayout.Label(string.Format("丝袜基础层 {0:0.#}（Tights={1}；插件不干预此层，仅显示实测值）",
                baseOp, t));
        }

        private static GUIStyle _hiStyle;

        private static GUIStyle HighlightStyle(GUIStyle basis)
        {
            if (_hiStyle == null)
            {
                _hiStyle = new GUIStyle(basis);
                _hiStyle.normal.textColor = Color.white;
                _hiStyle.normal.background = basis.active.background;
            }
            return _hiStyle;
        }

        /// <summary>
        /// 立刻把槽位推给模型。游戏自己也每帧调 UpdateCostume()，
        /// 这里主动调一次是为了改完立刻见效、不用等下一帧。
        /// </summary>
        private void ApplyCostume(object tabemi)
        {
            InvokeMethod(tabemi, "UpdateCostume");
        }


        private static void InvokeMethod(object obj, string name)
        {
            if (obj == null) return;
            MethodInfo mi = obj.GetType().GetMethod(name, AllFlags);
            if (mi == null) return;
            try { mi.Invoke(obj, null); } catch { }
        }

        /// <summary>
        /// 全量健壮性自检：把每个可改目标依次「写入测试值 → 回读 → 恢复原值」，
        /// 逐个报告 OK/FAIL。用于确认修改器对游戏版本的适配面是否完整。
        /// 结果写到 BepInEx/l2d_dump/selftest.txt。
        /// </summary>
        private void SelfTestAll()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== 全量健壮性自检 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ===");
            sb.AppendLine("插件版本 1.0.0 / 场景 " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            sb.AppendLine();

            int pass = 0, fail = 0, skip = 0;

            object player = Player();
            object tabemi = Tabemi();
            object clock = Clock();
            object ani = Anim();
            object aniSit = AnimSit();

            // ------------------------------------------------------------------
            // 快照：自检会改这些值，先把原值全记下来，收尾时无条件全部还原。
            // 教训：早先版本靠「在每段测试里各写一句还原」——只要有一条路径没走到，
            // 上限就净涨一次；连按自检于是表现为「上限无限上升」。
            // ------------------------------------------------------------------
            float snapHp = 0f, snapMaxHp = 0f, snapEc = 0f, snapMaxEc = 0f;
            int snapBurger = 0, snapSyasei = 0;
            float snapResist = 0f, snapSpeed = 1f, snapFullTime = 180f;
            bool snapClockRun = true;
            bool snapHasPlayer = player != null;
            float[] snapRates = null;

            if (snapHasPlayer)
            {
                snapHp = GetFloat(player, "currentHP");
                snapMaxHp = GetFloat(player, "maxHP");
                snapEc = GetFloat(player, "CurrentEcstasy");
                snapMaxEc = GetFloat(player, "maxEcstasy");
                snapBurger = (int)GetFloat(player, "maxBurgerNum");
                snapSyasei = (int)GetFloat(player, "syaseiCount");
                snapResist = GetFloat(player, "ecstasyResist");
                snapRates = new[] { HpDownRate.Value, HpUpRate.Value, EcstasyUpRate.Value, EcstasyDownRate.Value };
            }
            if (clock != null)
            {
                snapFullTime = GetFloat(clock, "fullTime");
                snapClockRun = GetFloat(clock, "isClockRunning") > 0.5f;
            }
            snapSpeed = GameSpeed.Value;

            // 血条/绝顶槽的 UI 尺寸也要快照 —— 游戏的 GaugeChange 会直接拉伸它们
            SnapshotGauges();

            sb.AppendLine("-- 目标可用性 --");
            sb.AppendLine("  player " + (player != null ? "OK" : "缺失"));
            sb.AppendLine("  tabemi " + (tabemi != null ? "OK" : "缺失"));
            sb.AppendLine("  clock  " + (clock != null ? "OK" : "缺失"));
            sb.AppendLine("  ani    " + (ani != null ? "OK" : "缺失"));
            sb.AppendLine("  aniSit " + (aniSit != null ? "OK" : "缺失"));
            sb.AppendLine();

            sb.AppendLine("-- 数值读写往返 --");

            // 场景里没有的组件跳过（标题画面本来就没有 player 等）
            if (player == null) { skip += 8; sb.AppendLine("  （不在店内场景：player 相关 8 项跳过）"); }
            else
            {
                // 下面会把 currentHP / CurrentEcstasy 改成测试值，先记原值、收尾时还原
                _selfTestKeepHp = GetFloat(player, "currentHP");
                _selfTestKeepEc = GetFloat(player, "CurrentEcstasy");

                Round(sb, ref pass, ref fail, "PlayerControl.currentHP", player, "currentHP",
                      GetFloat(player, "maxHP") * 0.5f);
                Round(sb, ref pass, ref fail, "PlayerControl.CurrentEcstasy", player, "CurrentEcstasy", 42f);
                Round(sb, ref pass, ref fail, "PlayerControl.maxBurgerNum", player, "maxBurgerNum", 77);
                Round(sb, ref pass, ref fail, "PlayerControl.syaseiCount", player, "syaseiCount", 33);
                Round(sb, ref pass, ref fail, "PlayerControl.ecstasyResist", player, "ecstasyResist", 0.66f);
                Round(sb, ref pass, ref fail, "PlayerControl.maxHP", player, "maxHP", 250f);
                Round(sb, ref pass, ref fail, "PlayerControl.maxEcstasy", player, "maxEcstasy", 150f);
                Round(sb, ref pass, ref fail, "PlayerControl.currentEcstasy", player, "currentEcstasy", 12f);
            }

            if (tabemi == null) { skip += 4; sb.AppendLine("  （tabemi 相关 4 项跳过）"); }
            else
            {
                Round(sb, ref pass, ref fail, "TabemiControl.tabemiPower", tabemi, "tabemiPower", 123f);
                Round(sb, ref pass, ref fail, "TabemiControl.baseAtt", tabemi, "baseAtt", 9f);
                Round(sb, ref pass, ref fail, "TabemiControl.TabemiAtt", tabemi, "TabemiAtt", 9f);
                Round(sb, ref pass, ref fail, "TabemiControl.特殊解除叩く量", tabemi, "特殊解除叩く量", 11);
            }

            if (clock == null) { skip += 3; sb.AppendLine("  （clock 相关 3 项跳过）"); }
            else
            {
                Round(sb, ref pass, ref fail, "ClockControl.fullTime", clock, "fullTime", 240);
                Round(sb, ref pass, ref fail, "ClockControl.isClockRunning", clock, "isClockRunning", false);
                Round(sb, ref pass, ref fail, "ClockControl.elapsedTime", clock, "elapsedTime", 5f);
            }

            if (ani == null) { skip += 1; sb.AppendLine("  （ani 相关 1 项跳过）"); }
            else Round(sb, ref pass, ref fail, "Live2D_AnimationControl.fellaSpeedPlus", ani, "fellaSpeedPlus", 0.5f);

            if (aniSit == null) { skip += 1; sb.AppendLine("  （aniSit 相关 1 项跳过）"); }
            else Round(sb, ref pass, ref fail, "Live2D_Animation_SitOsiri.sitSpeedPlus", aniSit, "sitSpeedPlus", 0.5f);

            Round(sb, ref pass, ref fail, "GameManager.GameSpeed", null, "GameSpeed", 2f, true);
            Round(sb, ref pass, ref fail, "GameManager.SphericalMode", null, "SphericalMode", 1, true);

            sb.AppendLine();
            sb.AppendLine("-- 换衣槽位 --");
            if (tabemi == null) { skip += 5; sb.AppendLine("  （5 个槽位跳过）"); }
            else
            {
                string[] slots = { "Cap", "Upper", "Lower", "Tights", "Glasses" };
                int[] values = { 1, 1, 4, 2, 2 };
                for (int i = 0; i < slots.Length; i++)
                    Round(sb, ref pass, ref fail, "TabemiControl." + slots[i], tabemi, slots[i], values[i]);
            }

            sb.AppendLine();
            sb.AppendLine("-- 上限锁定机制 --");
            if (player == null) { sb.AppendLine("  跳过（无 player）"); }
            else
            {
                float maxHp = GetFloat(player, "maxHP");
                float maxEc = GetFloat(player, "maxEcstasy");

                // 先让基线建立
                bool keepHp = LockMaxHp.Value, keepEc = LockMaxEcstasy.Value;
                LockMaxHp.Value = true; LockMaxEcstasy.Value = true;
                KeepMaxCaps(player);
                float baseHp = _baseMaxHp, baseEc = _baseMaxEcstasy;

                // 模拟游戏削减上限（照它自己那两行：按百分比扣），再让锁压回来。
                // 注意游戏改上限是**乘法**（maxEc += maxEc * p/100），浮点上单次补偿未必
                // 精确回到基线，所以这里迭代几次让它收敛，判定容差也放到 0.5。
                SetField(player, "maxHP", maxHp * 0.9f);
                SetField(player, "maxEcstasy", maxEc * 0.9f);
                for (int i = 0; i < 4; i++) KeepMaxCaps(player);

                float gotHp = GetFloat(player, "maxHP");
                float gotEc = GetFloat(player, "maxEcstasy");
                bool okHp = Mathf.Abs(gotHp - baseHp) < 0.5f;
                bool okEc = Mathf.Abs(gotEc - baseEc) < 0.5f;
                if (okHp) pass++; else fail++;
                if (okEc) pass++; else fail++;
                sb.AppendLine(string.Format("  {0}  maxHP 被削 10% 后 → {1:0.##}（基线 {2:0.##}）",
                    okHp ? "OK  " : "FAIL", gotHp, baseHp));
                sb.AppendLine(string.Format("  {0}  maxEcstasy 被削 10% 后 → {1:0.##}（基线 {2:0.##}）",
                    okEc ? "OK  " : "FAIL", gotEc, baseEc));

                LockMaxHp.Value = keepHp; LockMaxEcstasy.Value = keepEc;
                // 上限用精确写回还原：走 GaugeChange 会被百分比与率补丁干扰
                SetField(player, "maxHP", maxHp);
                SetField(player, "maxEcstasy", maxEc);
                _baseMaxHp = -1f; _baseMaxEcstasy = -1f;

                // 2) 抬上限：按游戏自己的接口把上限顶上去，应当真的变高
                sb.AppendLine();
                sb.AppendLine("-- 抬上限（对策）--");
                float targetHp = maxHp + 30f;
                RaiseMaxHp(player, targetHp);
                float upHp = GetFloat(player, "maxHP");
                bool okUpHp = upHp > maxHp + 1f;
                if (okUpHp) pass++; else fail++;
                sb.AppendLine(string.Format("  {0}  HP 上限 {1:0.##} → {2:0.##}（目标 {3:0.##}）",
                    okUpHp ? "OK  " : "FAIL", maxHp, upHp, targetHp));

                float targetEc = maxEc + 30f;
                RaiseMaxEcstasy(player, targetEc);
                float upEc = GetFloat(player, "maxEcstasy");
                bool okUpEc = upEc > maxEc + 1f;
                if (okUpEc) pass++; else fail++;
                sb.AppendLine(string.Format("  {0}  绝顶上限 {1:0.##} → {2:0.##}（目标 {3:0.##}）",
                    okUpEc ? "OK  " : "FAIL", maxEc, upEc, targetEc));

                // 恢复
                SetField(player, "maxHP", maxHp);
                SetField(player, "maxEcstasy", maxEc);
                SetField(player, "currentHP", maxHp);
                SetField(player, "CurrentEcstasy", 0f);
                SetField(player, "currentEcstasy", 0f);
            }

            sb.AppendLine();
            sb.AppendLine("-- 变化率（Harmony 补丁）--");
            if (player == null) { sb.AppendLine("  跳过（无 player）"); }
            else
            {
                float keepDown = HpDownRate.Value, keepUp = HpUpRate.Value;
                float keepEUp = EcstasyUpRate.Value, keepEDown = EcstasyDownRate.Value;

                // 把两个率设成 0%，然后调游戏的方法，看它是否真的不动
                HpDownRate.Value = 0f;
                float hp0 = GetFloat(player, "currentHP");
                InvokeFloat(player, "HPChange", -10f);
                float hp1 = GetFloat(player, "currentHP");
                bool okHp = Mathf.Abs(hp1 - hp0) < 0.01f;
                if (okHp) pass++; else fail++;
                sb.AppendLine(string.Format("  {0}  HP 下降率 0%：HPChange(-10) → {1:0.###}（原 {2:0.###}）",
                    okHp ? "OK  " : "FAIL", hp1, hp0));

                EcstasyUpRate.Value = 0f;
                float ec0 = GetFloat(player, "CurrentEcstasy");
                InvokeFloat(player, "EcstasyChange", 10f);
                float ec1 = GetFloat(player, "CurrentEcstasy");
                bool okEc = Mathf.Abs(ec1 - ec0) < 0.01f;
                if (okEc) pass++; else fail++;
                sb.AppendLine(string.Format("  {0}  绝顶上升率 0%：EcstasyChange(+10) → {1:0.###}（原 {2:0.###}）",
                    okEc ? "OK  " : "FAIL", ec1, ec0));

                // 50% 应当只生效一半
                HpDownRate.Value = 50f;
                float hp2 = GetFloat(player, "currentHP");
                InvokeFloat(player, "HPChange", -10f);
                float hp3 = GetFloat(player, "currentHP");
                float applied = hp2 - hp3;
                bool okHalf = Mathf.Abs(applied - 5f) < 0.6f;
                if (okHalf) pass++; else fail++;
                sb.AppendLine(string.Format("  {0}  HP 下降率 50%：HPChange(-10) 实际生效 {1:0.##}（期望 5）",
                    okHalf ? "OK  " : "FAIL", applied));

                // 还原
                HpDownRate.Value = keepDown; HpUpRate.Value = keepUp;
                EcstasyUpRate.Value = keepEUp; EcstasyDownRate.Value = keepEDown;
                InvokeFloat(player, "HPChange", hp0 - GetFloat(player, "currentHP"));
            }

            sb.AppendLine();
            sb.AppendLine("-- 方法调用 --");            if (tabemi != null)
            {
                MethodInfo rc = tabemi.GetType().GetMethod("RandomCostume", AllFlags);
                sb.AppendLine("  RandomCostume() " + (rc != null ? "存在" : "缺失"));
                if (rc != null) { try { rc.Invoke(tabemi, null); sb.AppendLine("  调用成功"); } catch (Exception e) { sb.AppendLine("  调用失败: " + e.Message); } }
                InvokeMethod(tabemi, "UpdateCostume");
            }
            else sb.AppendLine("  跳过");

            sb.AppendLine();
            sb.AppendLine("-- 收尾还原（无条件，按开头快照）--");
            try
            {
                if (snapHasPlayer)
                {
                    // 率必须先还原 —— 否则 setter 里的 HPChange 入参会被率乘掉
                    SetField(player, "maxHP", snapMaxHp);
                    SetField(player, "maxEcstasy", snapMaxEc);
                    SetField(player, "currentHP", snapHp);
                    SetField(player, "CurrentEcstasy", snapEc);
                    SetField(player, "currentEcstasy", snapEc);
                    SetField(player, "maxBurgerNum", snapBurger);
                    SetField(player, "syaseiCount", snapSyasei);
                    SetField(player, "ecstasyResist", snapResist);
                    // 基线与「只在变动时写一次」的记忆值一起复位，避免污染后续运行
                    _baseMaxHp = snapMaxHp;
                    _baseMaxEcstasy = snapMaxEc;
                    _lastSyasei = snapSyasei;
                    _lastMaxBurger = snapBurger;
                }
                if (snapRates != null)
                {
                    HpDownRate.Value = snapRates[0];
                    HpUpRate.Value = snapRates[1];
                    EcstasyUpRate.Value = snapRates[2];
                    EcstasyDownRate.Value = snapRates[3];
                }
                if (clock != null)
                {
                    SetField(clock, "fullTime", snapFullTime);
                    SetField(clock, "isClockRunning", snapClockRun);
                }
                GameSpeed.Value = snapSpeed;

                // 条被 GaugeChange 拉伸过，还原尺寸（数值还原不会把它变回去）
                RestoreGauges();

                sb.AppendLine("  条尺寸：" + DescribeGauges());

                sb.AppendLine(snapHasPlayer
                    ? string.Format("  已还原：HP {0:0.##}/{1:0.##}  绝顶 {2:0.##}/{3:0.##}  连吃上限 {4}  射精 {5}",
                        GetFloat(player, "currentHP"), GetFloat(player, "maxHP"),
                        GetFloat(player, "CurrentEcstasy"), GetFloat(player, "maxEcstasy"),
                        (int)GetFloat(player, "maxBurgerNum"), (int)GetFloat(player, "syaseiCount"))
                    : "  跳过（无 player）");
            }
            catch (Exception e)
            {
                sb.AppendLine("  × 还原异常：" + e.Message);
                Log.LogError("自检还原失败：" + e);
            }
            _selfTestKeepHp = float.NaN;
            _selfTestKeepEc = float.NaN;

            sb.AppendLine();
            sb.AppendLine(string.Format("结果：{0} 通过 / {1} 失败 / {2} 跳过", pass, fail, skip));

            string outFile = System.IO.Path.Combine(DiagStampDir("selftest"), "selftest.txt");
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outFile));
                System.IO.File.WriteAllText(outFile, sb.ToString(), new System.Text.UTF8Encoding(false));
            }
            catch { }
            Log.LogInfo("全量自检：" + pass + " 通过 / " + fail + " 失败 / " + skip + " 跳过 → " + outFile);
            if (fail > 0) Log.LogWarning("自检有失败项，详见 " + outFile);
        }

        /// <summary>
        /// 快照：把「画面截图 + 当前可见的部件 + 它们对应的图集矩形」一次性落到同一个目录。
        /// 用途：游玩时看到马赛克就按一下，之后就能在离线端把画面上的那块精确反查到图集区域。
        /// 输出：BepInEx/l2d_dump/snap_<时间戳>/screen.png + visible.tsv + all_parts.tsv + meta.txt
        /// </summary>
        private void CaptureSnapshot()
        {
            try
            {
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string dir = System.IO.Path.Combine(Paths.GameRootPath, "BepInEx", "l2d_dump", "snap_" + stamp);
                System.IO.Directory.CreateDirectory(dir);

                // 1) 画面截图
                string shot = System.IO.Path.Combine(dir, "screen.png");
                bool shotOk = false;
                try
                {
                    Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                    tex.Apply();
                    byte[] png = tex.EncodeToPNG();
                    UnityEngine.Object.Destroy(tex);
                    System.IO.File.WriteAllBytes(shot, png);
                    shotOk = true;
                }
                catch (Exception e) { Log.LogWarning("截图失败：" + e.Message); }

                // 2) 部件级数据
                Type camType = FindType("Live2D.Cubism.Core.CubismModel");
                Type rendType = FindType("Live2D.Cubism.Rendering.CubismRenderer");
                var all = new System.Text.StringBuilder();
                var vis = new System.Text.StringBuilder();
                const string Header = "drawable\ttexName\tvisible\tscreenX0\tscreenY0\tscreenX1\tscreenY1" +
                                      "\tatlasX0\tatlasY0\tatlasX1\tatlasY1\tmodelX0\tmodelY0\tmodelX1\tmodelY1";
                all.AppendLine(Header);
                vis.AppendLine(Header);

                int visibleCount = 0, total = 0;
                if (camType != null)
                {
                    UnityEngine.Object[] models = Resources.FindObjectsOfTypeAll(camType);
                    Camera cam = Camera.main;
                    if (cam == null)
                    {
                        Camera[] cams = Camera.allCameras;
                        if (cams != null && cams.Length > 0) cam = cams[0];
                    }

                    foreach (UnityEngine.Object mo in models)
                    {
                        Component comp = mo as Component;
                        if (comp == null) continue;
                        UnityEngine.Object[] arr =
                            (UnityEngine.Object[])camType.GetProperty("Drawables", AllFlags).GetValue(comp, null);
                        if (arr == null) continue;

                        foreach (UnityEngine.Object dobj in arr)
                        {
                            Component dc = dobj as Component;
                            if (dc == null) continue;
                            Type dt = dc.GetType();

                            string id = (string)dt.GetProperty("Id", AllFlags).GetValue(dc, null);
                            Vector2[] uvs = (Vector2[])dt.GetProperty("VertexUvs", AllFlags).GetValue(dc, null);
                            Vector3[] pos = (Vector3[])dt.GetProperty("VertexPositions", AllFlags).GetValue(dc, null);
                            if (uvs == null || uvs.Length == 0) continue;
                            total++;

                            float u0 = float.MaxValue, v0 = float.MaxValue, u1 = float.MinValue, v1 = float.MinValue;
                            foreach (Vector2 uv in uvs)
                            {
                                if (uv.x < u0) u0 = uv.x; if (uv.y < v0) v0 = uv.y;
                                if (uv.x > u1) u1 = uv.x; if (uv.y > v1) v1 = uv.y;
                            }

                            string texName = ""; int tw = 0, th = 0;
                            bool isVisible = false;
                            Renderer r = dc.GetComponent<Renderer>();
                            if (r != null) isVisible = r.isVisible;
                            try
                            {
                                Component rend = rendType != null ? dc.GetComponent(rendType) : null;
                                if (rend != null)
                                {
                                    Texture t = rendType.GetProperty("MainTexture", AllFlags).GetValue(rend, null) as Texture;
                                    if (t != null) { texName = t.name; tw = t.width; th = t.height; }
                                }
                            }
                            catch { }

                            float sx0 = 0, sy0 = 0, sx1 = 0, sy1 = 0;
                            try
                            {
                                if (r != null && cam != null)
                                {
                                    Bounds b = r.bounds;
                                    Vector3 c = cam.WorldToScreenPoint(b.center);
                                    Vector3 e = cam.WorldToScreenPoint(b.center + b.extents);
                                    sx0 = Mathf.Min(c.x, e.x); sx1 = Mathf.Max(c.x, e.x);
                                    sy0 = Screen.height - Mathf.Max(c.y, e.y);
                                    sy1 = Screen.height - Mathf.Min(c.y, e.y);
                                }
                            }
                            catch { }

                            float mx0 = 0, my0 = 0, mx1 = 0, my1 = 0;
                            if (pos != null && pos.Length > 0)
                            {
                                mx0 = my0 = float.MaxValue; mx1 = my1 = float.MinValue;
                                foreach (Vector3 vv in pos)
                                {
                                    if (vv.x < mx0) mx0 = vv.x; if (vv.y < my0) my0 = vv.y;
                                    if (vv.x > mx1) mx1 = vv.x; if (vv.y > my1) my1 = vv.y;
                                }
                            }

                            string line = string.Join("\t", new string[]
                            {
                                id, texName, isVisible ? "1" : "0",
                                ((int)sx0).ToString(), ((int)sy0).ToString(), ((int)sx1).ToString(), ((int)sy1).ToString(),
                                tw > 0 ? ((int)(u0*tw)).ToString() : "", th > 0 ? ((int)(v0*th)).ToString() : "",
                                tw > 0 ? ((int)(u1*tw)).ToString() : "", th > 0 ? ((int)(v1*th)).ToString() : "",
                                mx0.ToString("F2"), my0.ToString("F2"), mx1.ToString("F2"), my1.ToString("F2")
                            });
                            all.AppendLine(line);
                            if (isVisible) { vis.AppendLine(line); visibleCount++; }
                        }
                    }
                }

                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "all_parts.tsv"),
                    all.ToString(), new System.Text.UTF8Encoding(false));
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "visible.tsv"),
                    vis.ToString(), new System.Text.UTF8Encoding(false));

                // 3) 附带当前数值与场景信息
                var meta = new System.Text.StringBuilder();
                meta.AppendLine("时间      " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                meta.AppendLine("场景      " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                meta.AppendLine("分辨率    " + Screen.width + "x" + Screen.height);
                meta.AppendLine("截图      " + (shotOk ? "screen.png" : "失败"));
                meta.AppendLine("部件总数  " + total + "，可见 " + visibleCount);
                object player = Player();
                if (player != null)
                {
                    meta.AppendLine("HP        " + GetFloat(player, "currentHP") + " / " + GetFloat(player, "maxHP"));
                    meta.AppendLine("绝顶值    " + GetFloat(player, "CurrentEcstasy") + " / " + GetFloat(player, "maxEcstasy"));
                }
                object tabemi = Tabemi();
                if (tabemi != null)
                {
                    meta.AppendLine("换衣      Cap=" + GetFloat(tabemi, "Cap") + " Upper=" + GetFloat(tabemi, "Upper") +
                                    " Lower=" + GetFloat(tabemi, "Lower") + " Tights=" + GetFloat(tabemi, "Tights") +
                                    " Glasses=" + GetFloat(tabemi, "Glasses"));
                }
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "meta.txt"),
                    meta.ToString(), new System.Text.UTF8Encoding(false));

                Log.LogInfo("快照已保存：" + dir + "（部件 " + total + "，可见 " + visibleCount +
                            (shotOk ? "，含截图" : "，截图失败") + "）");
            }
            catch (Exception e)
            {
                Log.LogError("快照失败：" + e);
            }
        }

        /// <summary>写一个测试值、读回来比对、恢复原值。</summary>
        private static void Round(System.Text.StringBuilder sb, ref int pass, ref int fail,
                                 string label, object target, string field, object testValue,
                                 bool isStatic = false)
        {
            try
            {
                FieldInfo fi = isStatic ? Field(FindType("GameManager"), field) : Field(target.GetType(), field);
                if (fi == null)
                {
                    sb.AppendLine("  SKIP  " + label + "（字段不存在）");
                    return;
                }

                object original = fi.GetValue(isStatic ? null : target);

                // 写
                if (fi.FieldType == typeof(float)) fi.SetValue(isStatic ? null : target, Convert.ToSingle(testValue));
                else if (fi.FieldType == typeof(int)) fi.SetValue(isStatic ? null : target, Convert.ToInt32(testValue));
                else if (fi.FieldType == typeof(bool)) fi.SetValue(isStatic ? null : target, Convert.ToBoolean(testValue));
                else fi.SetValue(isStatic ? null : target, testValue);

                // 读回
                object readBack = fi.GetValue(isStatic ? null : target);
                bool ok;
                if (fi.FieldType == typeof(float))
                    ok = Mathf.Abs(Convert.ToSingle(readBack) - Convert.ToSingle(testValue)) < 0.001f;
                else
                    ok = Equals(readBack, fi.FieldType == typeof(int) ? (object)Convert.ToInt32(testValue)
                                                                      : (object)Convert.ToBoolean(testValue));

                // 恢复
                fi.SetValue(isStatic ? null : target, original);

                if (ok) { pass++; sb.AppendLine("  OK    " + label + " = " + testValue + " → 读回一致"); }
                else { fail++; sb.AppendLine("  FAIL  " + label + " 期望 " + testValue + " 读回 " + readBack); }
            }
            catch (Exception e)
            {
                fail++;
                sb.AppendLine("  FAIL  " + label + " 异常: " + e.Message);
            }
        }

        /// <summary>
        /// 换衣自检：把五个槽位依次设成非默认值，回读 model 上的 _Op_* 参数，
        /// 确认「槽位 → 模型参数」这条链真的通，然后恢复原值。
        /// </summary>
        private void SelfTestCostume()
        {
            object tabemi = Tabemi();
            if (tabemi == null) { Log.LogWarning("换衣自检：不在店内场景"); return; }

            object model = Field(tabemi.GetType(), "model")?.GetValue(tabemi);
            if (model == null) { Log.LogWarning("换衣自检：拿不到 model"); return; }

            // 每组：槽位名 → 模型参数名（按 UpdateCostume 的实现）
            var pairs = new[]
            {
                new object[] { "Cap",    "_Op_CenterGirlSitting_Cap",   0f, 1f },
                new object[] { "Upper",  "_Op_CenterGirlSitting_Shirts",0f, 1f },
                new object[] { "Lower",  "_Op_CenterGirlSitting_SkirtA",0f, 1f },
                new object[] { "Tights", "_Op_CenterGirlSitting_Tights_Black", 0f, 1f },
                new object[] { "Glasses","GlassesA_Opacity",            0f, 1f },
            };

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== 换衣自检 " + DateTime.Now.ToString("HH:mm:ss") + " ===");
            int pass = 0, fail = 0;

            foreach (object[] p in pairs)
            {
                string slot = (string)p[0];
                string param = (string)p[1];
                float off = (float)p[2], on = (float)p[3];

                int saved = (int)GetFloat(tabemi, slot);

                SetField(tabemi, slot, (int)on);
                InvokeMethod(tabemi, "UpdateCostume");
                float gotOn = GetFloat(model, param);

                SetField(tabemi, slot, (int)off);
                InvokeMethod(tabemi, "UpdateCostume");
                float gotOff = GetFloat(model, param);

                bool ok = Mathf.Abs(gotOn - on) < 0.001f && Mathf.Abs(gotOff - off) < 0.001f;
                if (ok) pass++; else fail++;

                sb.AppendLine(string.Format("  {0,-8} → {1,-34} 置1后={2:0.##}  置0后={3:0.##}  {4}",
                    slot, param, gotOn, gotOff, ok ? "OK" : "FAIL"));

                SetField(tabemi, slot, saved);
            }

            InvokeMethod(tabemi, "UpdateCostume");

            // 丝袜：插件**不应**干预 _Op_CenterGirlSitting_Tights 基础层。
            //
            // 历史：早期版本会"代为补开"这层，但那是错的 ——
            // 基础图集里这层是空的（衣服只在 DLC 图集里），强行点亮只会露出裸腿 mesh，
            // 造成"选了裤袜却是光腿"的假象。所以这里断言的是**保持原值**（0）。
            {
                int tights = (int)GetFloat(tabemi, "Tights");
                float baseOp = GetFloat(model, "_Op_CenterGirlSitting_Tights");
                bool ok = baseOp < 0.5f;
                if (ok) pass++; else fail++;
                sb.AppendLine(string.Format("  {0}  丝袜基础层未被插件干预 _Op_..._Tights = {1:0.#}（Tights={2}，应为 0）",
                    ok ? "OK  " : "FAIL", baseOp, tights));
            }

            sb.AppendLine(string.Format("结果：{0} 通过 / {1} 失败", pass, fail));

            string outFile = System.IO.Path.Combine(DiagStampDir("selftest"), "costume_selftest.txt");
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outFile));
                System.IO.File.WriteAllText(outFile, sb.ToString(), new System.Text.UTF8Encoding(false));
            }
            catch { }
            Log.LogInfo("换衣自检：" + pass + " 通过 / " + fail + " 失败 → " + outFile);
        }

        // 步进器：滑块 + 「−」「+」精确加减。滑块拖不准时用按钮，步长可指定或按量程自动取。
        private static float SliderF(string label, float value, float min, float max, string fmt)
        {
            return SliderF(label, value, min, max, fmt, StepOf(max));
        }

        /// <summary>记录一次修改器面板按钮点击（用户操作的来源之一）。</summary>
        private static void NotePanelClick(string label)
        {
            LogOp("面板·" + label, "点击");
            if (ActivityLogger.Running) ActivityLogger.EmitUser("panel_click", label);
        }

        private static float SliderF(string label, float value, float min, float max, string fmt, float step)
        {
            float before = value;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(124f));
            if (GUILayout.Button("−", GUILayout.Width(24f))) value -= step;
            value = GUILayout.HorizontalSlider(value, min, max);
            if (GUILayout.Button("+", GUILayout.Width(24f))) value += step;
            GUILayout.Label(string.Format(fmt, value), GUILayout.Width(62f));
            GUILayout.EndHorizontal();
            float after = Mathf.Clamp(value, min, max);
            // 只在值真正变化时记流水（拖动过程中会连续变化，但仍比每帧写入少得多）
            if (Mathf.Abs(after - before) > 0.0001f)
                LogOp("调参·" + label, string.Format("{0} → {1}", before.ToString("0.###"), after.ToString("0.###")));
            return after;
        }

        private static int SliderI(string label, int value, int min, int max)
        {
            int before = value;
            int step = StepOfInt(max);
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(124f));
            if (GUILayout.Button("−", GUILayout.Width(24f))) value -= step;
            value = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, min, max));
            if (GUILayout.Button("+", GUILayout.Width(24f))) value += step;
            GUILayout.Label(value.ToString(), GUILayout.Width(62f));
            GUILayout.EndHorizontal();
            int after = Mathf.Clamp(value, min, max);
            if (after != before) LogOp("调参·" + label, before + " → " + after);
            return after;
        }

        /// <summary>按量程取一个「手感合适」的步长：小量程精调，大量程粗调。</summary>
        private static float StepOf(float max)
        {
            if (max <= 10f) return 0.05f;      // 抗性 / 速度加成
            if (max <= 100f) return 1f;
            if (max <= 600f) return 10f;       // 攻击力倍率
            return 25f;                        // 时长 / 上限目标
        }

        private static int StepOfInt(int max)
        {
            if (max <= 200) return 5;
            if (max <= 600) return 25;
            return 100;
        }

        // ---------------------------------------------------------------
        // 字段读写
        // ---------------------------------------------------------------
        private static float GetFloat(object obj, string name)
        {
            if (obj == null) return 0f;
            Type t = obj.GetType();

            // 字段优先，找不到再试属性（ateCount 这类是属性）。
            // 注意：不能在字段不存在或不匹配时报「找不到」——那会误报（真正的值可能来自属性）。
            FieldInfo fi = FieldQuiet(t, name);
            object v = fi != null ? fi.GetValue(obj) : null;
            if (v == null)
            {
                PropertyInfo pi = t.GetProperty(name, AllFlags);
                if (pi != null && pi.CanRead)
                {
                    try { v = pi.GetValue(obj, null); }
                    catch { v = null; }
                }
            }

            if (v is float f) return f;
            if (v is int i) return i;
            if (v is bool b) return b ? 1f : 0f;
            return 0f;
        }

        private static void SetField(object obj, string field, object value)
        {
            if (obj == null) return;
            FieldInfo fi = Field(obj.GetType(), field);
            if (fi == null) return;
            try
            {
                if (fi.FieldType == typeof(float)) fi.SetValue(obj, Convert.ToSingle(value));
                else if (fi.FieldType == typeof(int)) fi.SetValue(obj, Convert.ToInt32(value));
                else if (fi.FieldType == typeof(bool)) fi.SetValue(obj, Convert.ToBoolean(value));
                else fi.SetValue(obj, value);
            }
            catch (Exception e)
            {
                if (Log != null) Log.LogWarning("写字段 " + field + " 失败: " + e.Message);
            }
        }

        // =================================================================
        // 进阶：导出 Live2D 部件清单（用于定位某块画面到底取自图集哪里）
        // =================================================================
    }
}
