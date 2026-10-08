using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BurgerShopModder
{
    /// <summary>
    /// 【游戏绑定层】
    ///
    /// 这个插件通过 Harmony 按**方法名**给游戏打补丁。而游戏里同一个方法名
    /// 可能属于不同的类 —— 挂错了**不会有任何报错**，Harmony 只是静静地不生效 
    ///
    /// 这个坑在这个项目里踩了【五次】：
    ///
    ///     Osiri叩かれる()            → 在 TabemiControl
    ///     Event_OsiriSyaseiStart()   → 在 Live2D_AnimationControl
    ///     DealDamage_Fella()         → 在 TabemiControl
    ///     Event_SitKissSyaseiOnEnd() → 在 Live2D_Animation_SitOsiri（不在 TabemiControl）
    ///     GetTouchTargetName()       → 在 Live2D_HitAreaCheck（不是 HitAreaCheck）
    ///
    /// 【本层的做法】把"方法属于哪个类"变成**一张可校验的表**：
    /// 启动时逐条确认 —— 方法在不在它该在的类上；
    /// 如果它其实在**另一个**已知的游戏类上，就**大声报出来**。
    ///
    /// 于是"挂错类"从静默失败变成启动日志里的一条 WARN 
    ///
    /// 用法：
    ///     GameBindings.VerifyAll(FindType);   // 启动时调一次
    ///     GameBindings.Dump();                // 排错时看全表
    /// </summary>
    internal static class GameBindings
    {
        // ── 游戏类名（唯一事实来源）──────────────────────────────
        internal const string T_Player      = "PlayerControl";
        internal const string T_Tabemi      = "TabemiControl";
        internal const string T_AnimFella   = "Live2D_AnimationControl";
        internal const string T_AnimSitOsiri= "Live2D_Animation_SitOsiri";
        internal const string T_HitArea     = "Live2D_HitAreaCheck";
        internal const string T_GameManager = "GameManager";

        /// <summary>所有已知的游戏类（用来检测"挂到了别的类上"）。</summary>
        internal static readonly string[] AllTypes =
        {
            T_Player, T_Tabemi, T_AnimFella, T_AnimSitOsiri, T_HitArea, T_GameManager,
        };

        internal sealed class Bind
        {
            internal string Method;      // 游戏里的方法名
            internal string Owner;       // 它【应该】属于哪个类
            internal string Note;        // 易错点说明（可选）
            internal Bind(string m, string o, string n = null) { Method = m; Owner = o; Note = n; }
        }

        /// <summary>
        /// 已打过补丁的目标。
        /// **只收录"曾经挂错或容易挂错"的那些** —— 不追求收录全部，
        /// 因为全收录的维护成本高，而真正会出问题的就是这几个跨类同名的方法。
        /// </summary>
        internal static readonly Bind[] Patches =
        {
            new Bind("Osiri叩かれる",              T_Tabemi,       "背榨点击"),
            new Bind("頭叩かれるSit",              T_Tabemi,       "坐姿点头部 = 解除束缚之吻"),
            new Bind("Osiri解除",                  T_Tabemi),
            new Bind("UpdateAtt",                  T_Tabemi,       "每帧重算攻击力，必须挂后缀"),
            new Bind("PrepareKissing",             T_Tabemi),
            new Bind("KissPrepare解除",            T_Tabemi),
            new Bind("DealDamage_Fella",           T_Tabemi),
            new Bind("DealDamage_SitKiss",         T_Tabemi),
            new Bind("ShowCenterGirlFella",        T_Tabemi,       "Osiri解除() 也走这里"),
            new Bind("ShowCenterGirlOsiri",        T_Tabemi,       "★ 方法体内直接赋值 centerGirlState"),
            new Bind("Show_CenterGirlSit",         T_Tabemi),

            new Bind("Event_SitKissSyaseiOnEnd",   T_AnimSitOsiri, "★ 不在 TabemiControl"),
            new Bind("Play_SitKissSyasei",         T_AnimSitOsiri),
            new Bind("Even_SitKiss吸精OnEnd",      T_AnimSitOsiri, "名字里 Even_ 是游戏的笔误"),
            new Bind("Event_SitKiss吸精1",         T_AnimSitOsiri),
            new Bind("Event_SitKissSyaseiStart",   T_AnimSitOsiri),
            new Bind("Event_SitKissSyasei1",       T_AnimSitOsiri),
            new Bind("Event_SitKissSyasei2",       T_AnimSitOsiri),
            new Bind("Event_OsiriSyaseiStart",     T_AnimSitOsiri, "★ 不在 Live2D_AnimationControl"),
            new Bind("Event_OsiriSyaseiOnEnd",     T_AnimSitOsiri),
            new Bind("Event_Osiri吸精End",         T_AnimSitOsiri, "用硬编码 Random<0.9，不读 KyuseiRate"),
            new Bind("Event_Osiri吸精Damage",      T_AnimSitOsiri, "只扣血，不产生绝顶值增量"),
            new Bind("Event_OsiriGirlMainMixer",   T_AnimSitOsiri, "每攻击循环触发一次"),
            new Bind("Play_OsiriSyasei",           T_AnimSitOsiri),
            new Bind("OsiriSE",                    T_AnimSitOsiri, "动画事件按名字回调，代码里搜不到调用者"),

            new Bind("Event_FellaSyaseiStart",     T_AnimFella),
            new Bind("Event_Fella吸精",            T_AnimFella),
            new Bind("Event_FellaSyaseiOnEnd",     T_AnimFella),

            new Bind("GetTouchTargetName",         T_HitArea,      "★ 类名是 Live2D_HitAreaCheck"),
            new Bind("HitAreaCheckWindows",        T_HitArea,      "鼠标路径：每帧算静态 mousePointing"),
        };

        private static int _ok, _missing, _misplaced;

        /// <summary>
        /// 启动时校验。findType 传 Plugin.FindType（带缓存的那个）。
        /// </summary>
        internal static void VerifyAll(Func<string, Type> findType, Action<string> warn, Action<string> info)
        {
            _ok = _missing = _misplaced = 0;

            // 先把所有已知类型的 Type 拿到手，用于检测"挂到了别的类上"
            var types = new Dictionary<string, Type>();
            foreach (string t in AllTypes)
            {
                Type ty = findType(t);
                if (ty != null) types[t] = ty;
            }
            if (types.Count < AllTypes.Length)
            {
                var miss = AllTypes.Where(t => !types.ContainsKey(t)).ToArray();
                warn("[绑定] 找不到这些游戏类：" + string.Join("、", miss)
                     + "（游戏版本可能不符，相关功能不会生效）");
            }

            foreach (Bind b in Patches)
            {
                Type owner;
                if (!types.TryGetValue(b.Owner, out owner)) { _missing++; continue; }

                bool here = owner.GetMethod(b.Method, Plugin.AllFlags) != null;
                if (here) { _ok++; continue; }

                // 不在这里 —— 看看是不是跑到别的类上去了（这正是"挂错类"）
                var elsewhere = new List<string>();
                foreach (var kv in types)
                {
                    if (kv.Key == b.Owner) continue;
                    if (kv.Value.GetMethod(b.Method, Plugin.AllFlags) != null) elsewhere.Add(kv.Key);
                }

                if (elsewhere.Count > 0)
                {
                    _misplaced++;
                    warn(string.Format("[绑定] ★挂错类：{0} 不在 {1}，而是在 {2}{3}",
                        b.Method, b.Owner, string.Join(" / ", elsewhere),
                        string.IsNullOrEmpty(b.Note) ? "" : "（" + b.Note + "）"));
                }
                else
                {
                    _missing++;
                    warn(string.Format("[绑定] 找不到方法 {0}.{1}{2}",
                        b.Owner, b.Method,
                        string.IsNullOrEmpty(b.Note) ? "" : "（" + b.Note + "）"));
                }
            }

            if (_misplaced > 0)
                warn("[绑定] 有 " + _misplaced + " 个方法挂错了类 —— 这些补丁不会生效，功能静默失效");
            else if (_missing > 0)
                warn("[绑定] 有 " + _missing + " 个方法找不到 —— 相关功能不会生效");
            else
                info("[绑定] " + _ok + " 个补丁目标全部校验通过");
        }

        internal static int OkCount { get { return _ok; } }
        internal static int MissingCount { get { return _missing; } }
        internal static int MisplacedCount { get { return _misplaced; } }

        /// <summary>排错用：把整张表打出来。</summary>
        internal static string Dump(Func<string, Type> findType)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("游戏绑定层 · 共 ").Append(Patches.Length).Append(" 条").Append((char)10);
            sb.Append("校验：通过 ").Append(_ok)
              .Append(" / 找不到 ").Append(_missing)
              .Append(" / 挂错类 ").Append(_misplaced).Append((char)10);
            sb.Append((char)10);
            foreach (Bind b in Patches)
            {
                Type owner = findType(b.Owner);
                bool here = owner != null && owner.GetMethod(b.Method, Plugin.AllFlags) != null;
                sb.Append(here ? "  [OK]   " : "  [FAIL] ")
                  .Append(b.Owner).Append('.').Append(b.Method);
                if (!string.IsNullOrEmpty(b.Note)) sb.Append("   — ").Append(b.Note);
                sb.Append((char)10);
            }
            return sb.ToString();
        }
    }
}
