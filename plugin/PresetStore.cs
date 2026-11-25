using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BurgerShopModder
{
    /// <summary>
    /// 配置预设：把当前全部配置值存成一份命名快照，之后可以随时切回去。
    ///
    /// 【为什么用通用快照，而不是逐参数列名单】
    /// BepInEx 的 ConfigFile 暴露了 Keys（ConfigDefinition → ConfigEntryBase），
    /// 配合 GetSerializedValue / SetSerializedValue 就能整份存整份读。
    /// 逐参数维护名单的话，以后每加一个参数都要记得同步预设 —— 那是必然会被忘的事。
    /// 通用快照意味着：**新增参数自动进预设**，不需要改这里。
    ///
    /// 【红线：切预设不能把用户调好的值弄丢】
    /// 应用预设是【覆盖】操作。所以在覆盖之前，一定先把当前状态存一份时间戳备份。
    /// 这条来自本项目的一次事故：早期调 defaults 把用户三十来项调参覆盖成了代码默认值。
    /// 所以这里的原则是：**任何覆盖之前先留退路**。
    ///
    /// 文件位置：&lt;游戏目录&gt;/BepInEx/config/nesarf.burgershop.modder.presets/
    ///   预设 &lt;名字&gt;.preset     制表符分隔：Section \t Key \t 值
    ///   备份 &lt;名字&gt;.before-&lt;时间戳&gt;.preset
    ///
    /// 【default 是一个虚拟预设，不落盘】
    /// 列表里永远有一项 default，它不对应任何文件：应用它时直接读每个参数的
    /// DefaultValue（也就是插件代码里 Config.Bind 写下的那个值）。这样它永远等于
    /// 当前这一版代码的出厂默认，不会像一份附带文件那样慢慢过期。
    ///
    /// 它不能被覆盖、也不能被删除 —— 一个能被改掉的基准就不是基准了。
    /// 「恢复默认」这件事原来散在两个地方：面板系统栏的「重置全部开关」只动十项，
    /// 修改器的「恢复默认」只认四个硬编码键。default 预设把这件事收成一处：
    /// 全部参数，一个基准。
    /// </summary>
    internal static class PresetStore
    {
        private const string DirName = "nesarf.burgershop.modder.presets";
        private const int MaxPresets = 20;      // 上限，免得列表长到没法看

        /// <summary>虚拟预设名。它不对应磁盘文件，也不能被覆盖或删除。</summary>
        internal const string DefaultName = "default";

        /// <summary>
        /// 随包出厂配置的文件名。安装包把它放在 config 目录下、用这个名字，
        /// 所以 BepInEx 不会把它当成插件配置加载；default 预设拿它当基准。
        /// </summary>
        internal const string ShippedFileName = "nesarf.burgershop.modder.shipped.cfg";

        /// <summary>
        /// 自动保存的名字。每次应用任何预设之前，当前状态都会写进这一项，
        /// 于是「手滑点了 default」还有一步退路。只读：不能覆盖、不能删除。
        ///
        /// 它和 default 不同 —— default 没有文件（基准来自随包配置），
        /// autosave 是一个真实存在的 .preset 文件，只是名字固定为这一个。
        /// </summary>
        internal const string AutoName = "autosave";

        internal static bool IsAuto(string name)
        {
            return string.Equals((name ?? "").Trim(), AutoName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>既不能覆盖也不能删除的名字（虚拟项与自动保存项）。</summary>
        internal static bool IsReadOnly(string name)
        {
            return IsDefault(name) || IsAuto(name);
        }

        internal static bool IsDefault(string name)
        {
            return string.Equals((name ?? "").Trim(), DefaultName, StringComparison.OrdinalIgnoreCase);
        }

        internal static string DirPath()
        {
            try
            {
                string cfg = Plugin.ConfigPathPublic();
                if (string.IsNullOrEmpty(cfg)) return null;
                return Path.Combine(Path.GetDirectoryName(cfg), DirName);
            }
            catch { return null; }
        }

        private static string SafeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var sb = new StringBuilder();
            foreach (char c in name.Trim())
            {
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ' ') sb.Append(c);
            }
            string s = sb.ToString().Trim();
            if (s.Length == 0) return null;
            if (s.Length > 24) s = s.Substring(0, 24);
            return s;
        }

        private static string PathOf(string name)
        {
            string dir = DirPath();
            string safe = SafeName(name);
            if (dir == null || safe == null) return null;
            return Path.Combine(dir, safe + ".preset");
        }

        /// <summary>列出已有的预设名（按文件名排序）。default 永远排第一，autosave 排第二。</summary>
        internal static List<string> List()
        {
            var outList = new List<string>();
            string dir = null;
            try
            {
                dir = DirPath();
                if (dir != null && Directory.Exists(dir))
                {
                    foreach (string f in Directory.GetFiles(dir, "*.preset"))
                    {
                        string n = Path.GetFileNameWithoutExtension(f);
                        // 【含点的都不是预设】
                        // 预设名经过 SafeName 过滤，不可能含点；而历史备份文件是
                        // "<名字>.before-<时间戳>" 和 "<名字>.current-before-apply-<时间戳>"。
                        // 原来只排除 ".before-"，结果 current-before-apply 那批被当成预设列了出来 ——
                        // 列表里会多出一堆带着「应用/覆盖/删除」按钮的备份，很容易误点。
                        if (n.IndexOf('.') >= 0) continue;
                        // 磁盘上不该有 default.preset（它被 Save 挡住），万一有也不列
                        if (IsDefault(n)) continue;
                        // autosave 由下面单独插入，从文件名扫出来的那份不算
                        if (IsAuto(n)) continue;
                        outList.Add(n);
                    }
                    outList.Sort(StringComparer.OrdinalIgnoreCase);
                }
            }
            catch { }
            // 排序之后再插：default 是基准，autosave 是"上一次覆盖之前的样子"，都不是按字母排的。
            // autosave 只在文件真的存在时出现 —— 还没应用过任何预设时，没有"上一次"可言。
            try
            {
                if (!string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, AutoName + ".preset")))
                    outList.Insert(0, AutoName);
            }
            catch { }
            outList.Insert(0, DefaultName);
            return outList;
        }

        /// <summary>把当前配置整份写成一份预设。同名会覆盖（覆盖前先备份旧的）。</summary>
        internal static string Save(string name)
        {
            try
            {
                if (IsDefault(name))
                    return L.T("ERR 「default」是固定在列表里的基准，不能覆盖。要另存一份就换个名字。");
                // 【autosave 可以被 Save 写】它就是「当前状态」的固定槽 —— 顶上的「保存」
                // 写的就是它。命名预设只能靠「另存为」产生，所以「保存」永远不会
                // 悄悄改掉用户存下的那几份。它仍然不能删除，见 Delete。

                var cfg = Plugin.ConfigPublic();
                if (cfg == null) return L.T("ERR 拿不到配置对象");

                string dir = DirPath();
                string path = PathOf(name);
                if (dir == null || path == null) return L.T("ERR 预设名不合法（只允许中英文、数字、空格、- 和 _）");

                Directory.CreateDirectory(dir);

                // 同名预设先备份，别让一次误操作吃掉旧的那份
                string backup = null;
                if (File.Exists(path))
                {
                    backup = Path.Combine(dir, SafeName(name) + ".before-" + Stamp() + ".preset");
                    File.Copy(path, backup, true);
                }

                var lines = new List<string>();
                lines.Add("# 汉堡店修改器 配置预设");
                lines.Add("# 存于 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                lines.Add("# 格式：Section <TAB> Key <TAB> 值");
                // 遍历 ConfigDefinitions 而不是 Keys ——
                // Keys 只是键名的字符串集合，拿不到 Section，也没有对应的条目对象。
                foreach (var def in cfg.ConfigDefinitions)
                    lines.Add(def.Section + "\t" + def.Key + "\t" + cfg[def].GetSerializedValue());

                File.WriteAllLines(path, lines.ToArray(), new UTF8Encoding(false));
                return L.T("已保存预设「{0}」（{1} 项）{2}",
                           SafeName(name), lines.Count - 3,
                           backup != null ? L.T("，原同名预设已备份") : "");
            }
            catch (Exception e) { return L.T("ERR 保存预设失败：{0}", e.Message); }
        }

        /// <summary>
        /// 应用一份预设。
        /// 【先备份当前状态】—— 覆盖之前一定留退路，这是硬规矩。
        /// </summary>
        internal static string Apply(string name)
        {
            if (IsDefault(name)) return ApplyDefault();
            try
            {
                var cfg = Plugin.ConfigPublic();
                if (cfg == null) return L.T("ERR 拿不到配置对象");

                string path = PathOf(name);
                if (path == null || !File.Exists(path)) return L.T("ERR 没有这个预设：{0}", name);

                // (0) 先把预设内容读进内存。
                // 【顺序不能反】应用 autosave 时，下面第 (1) 步会把当前状态写进
                // autosave.preset —— 如果先写再读，读到的就是刚被覆盖的那份，
                // 「撤回」会变成原地踏步（实测：改到 120，点撤回还是 120，该回 150）。
                // 所以先把要应用的那份拿到手，再动磁盘。
                var want = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
                {
                    string l = raw;
                    if (l.Length == 0 || l[0] == '#') continue;
                    string[] p = l.Split('\t');
                    if (p.Length < 3) continue;
                    want[p[0] + "\u0001" + p[1]] = p[2];
                }

                // (1) 覆盖之前先把当前状态存成 autosave
                // 以前这里写的是带时间戳的文件，但那些文件不在列表里显示 —— 用户想回去
                // 得手动去目录里改名。「有备份」和「能一步退回去」是两件事，改成固定名
                // 之后它就是一个能点的预设。
                string dir = DirPath();
                Directory.CreateDirectory(dir);
                string before = Path.Combine(dir, AutoName + ".preset");
                WriteSnapshot(cfg, before, "自动保存：应用预设「" + SafeName(name) + "」之前的当前状态，" + Stamp());

                // (2) 逐项写回。按 Section+Key 找，不按 Key 找 —— 不同 Section 下可能重名。
                int applied = 0, missing = 0;
                foreach (var def in cfg.ConfigDefinitions)
                {
                    string k = def.Section + "\u0001" + def.Key;
                    string v;
                    if (want.TryGetValue(k, out v))
                    {
                        // 值不对就跳过，不抛 —— 旧预设遇上删除过的参数时会走到这里
                        try { cfg[def].SetSerializedValue(v); applied++; }
                        catch { missing++; }
                    }
                    else missing++;
                }

                cfg.Save();
                return L.T("已应用预设「{0}」：{1} 项{2}；应用前的状态已存为 autosave，列表里可以退回来",
                           SafeName(name), applied,
                           missing > 0 ? L.T("（{0} 项预设里没有，保持当前值）", missing) : "");
            }
            catch (Exception e) { return L.T("ERR 应用预设失败：{0}", e.Message); }
        }

        /// <summary>把当前全部配置写成一份快照文件。任何覆盖之前都先调它。</summary>
        private static void WriteSnapshot(BepInEx.Configuration.ConfigFile cfg, string path, string title)
        {
            var cur = new List<string>();
            cur.Add("# " + title);
            cur.Add("# 存于 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cur.Add("# 格式：Section <TAB> Key <TAB> 值");
            foreach (var def in cfg.ConfigDefinitions)
                cur.Add(def.Section + "\t" + def.Key + "\t" + cfg[def].GetSerializedValue());
            File.WriteAllLines(path, cur.ToArray(), new UTF8Encoding(false));
        }

        /// <summary>
        /// 应用虚拟预设 default：回到「刚装好的状态」。
        ///
        /// 【default 到底是什么，这里定下来】
        /// 安装包会带一份出厂配置（installer/ship-default.cfg），装好之后用户拿到的就是它。
        /// 所以 default 必须指向**那一份**，而不是代码里的 Config.Bind 默认值 —— 否则
        /// 装完的状态和点 default 的结果对不上，用户会以为自己装错了。
        ///
        /// 出厂配置里缺的项（插件升级后新增了参数）退回代码默认值，所以它不会因为
        /// 版本推进而漏掉新参数。
        ///
        /// 【没有附带的出厂配置时】直接用每个条目的 DefaultValue，也就是"代码默认"。
        ///
        /// 【一样先备份】default 也是覆盖操作，红线不例外。
        /// </summary>
        internal static string ApplyDefault()
        {
            try
            {
                var cfg = Plugin.ConfigPublic();
                if (cfg == null) return L.T("ERR 拿不到配置对象");

                string dir = DirPath();
                if (dir == null) return L.T("ERR 拿不到预设目录");
                Directory.CreateDirectory(dir);
                string before = Path.Combine(dir, AutoName + ".preset");
                WriteSnapshot(cfg, before, "自动保存：应用 default 之前的当前状态，" + Stamp());

                var shipped = ReadShippedDefaults();
                int applied = 0, failed = 0, skipped = 0, fromShipped = 0;
                foreach (var def in cfg.ConfigDefinitions)
                {
                    // 【内部状态不参与「恢复默认」】ConfigVersion 的 DefaultValue 是 0，
                    // 把它写回去等于告诉插件「这份配置还没迁移过」，下次启动会重跑一遍迁移。
                    // 它不是用户能调的参数，恢复默认不该碰它。
                    if (string.Equals(def.Key, "ConfigVersion", StringComparison.Ordinal)) { skipped++; continue; }
                    try
                    {
                        var entry = cfg[def];
                        string v;
                        if (shipped != null && shipped.TryGetValue(def.Section + "\u0001" + def.Key, out v))
                        {
                            // 走序列化字符串：出厂文件里是文本，类型由 BepInEx 自己转
                            entry.SetSerializedValue(v);
                            fromShipped++;
                        }
                        else
                        {
                            // 出厂文件里没有这一项（新加的参数）—— 用代码默认值补上
                            entry.BoxedValue = entry.DefaultValue;
                        }
                        applied++;
                    }
                    catch { failed++; }
                }

                cfg.Save();
                return L.T("已恢复出厂状态：{0} 项{1}{2}{3}；恢复前的状态已存为 autosave，列表里可以退回来",
                           applied,
                           shipped != null ? L.T("（其中 {0} 项取自随包出厂配置）", fromShipped) : L.T("（没有随包出厂配置，用的是代码默认值）"),
                           failed > 0 ? L.T("（{0} 项写不回去）", failed) : "",
                           skipped > 0 ? L.T("，跳过 {0} 项内部状态", skipped) : "");
            }
            catch (Exception e) { return L.T("ERR 恢复默认失败：{0}", e.Message); }
        }

        /// <summary>
        /// 读随包附带的出厂配置。安装包把它放在 config 目录下、用不同的名字，
        /// 所以 BepInEx 不会把它当成插件的配置来加载。
        ///
        /// 返回 Section\u0001Key → 值；文件不存在返回 null（调用方退回代码默认值）。
        /// </summary>
        private static Dictionary<string, string> ReadShippedDefaults()
        {
            try
            {
                string cfg = Plugin.ConfigPathPublic();
                if (string.IsNullOrEmpty(cfg)) return null;
                string path = Path.Combine(Path.GetDirectoryName(cfg), ShippedFileName);
                if (!File.Exists(path)) return null;

                var map = new Dictionary<string, string>(StringComparer.Ordinal);
                string section = "";
                foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
                {
                    string l = raw.Trim();
                    if (l.Length == 0 || l[0] == '#') continue;
                    if (l[0] == '[' && l[l.Length - 1] == ']') { section = l.Substring(1, l.Length - 2); continue; }
                    int eq = l.IndexOf('=');
                    if (eq <= 0) continue;
                    map[section + "\u0001" + l.Substring(0, eq).Trim()] = l.Substring(eq + 1).Trim();
                }
                return map;
            }
            catch { return null; }
        }

        internal static string Delete(string name)
        {
            try
            {
                if (IsDefault(name))
                    return L.T("ERR 「default」不能删除 —— 它是回到出厂状态的唯一入口。");
                if (IsAuto(name))
                    return L.T("ERR 「autosave」不能删除 —— 它是撤销上一次覆盖的退路。");
                string path = PathOf(name);
                if (path == null || !File.Exists(path)) return L.T("ERR 没有这个预设：{0}", name);
                string dir = DirPath();
                string bak = Path.Combine(dir, SafeName(name) + ".deleted-" + Stamp() + ".preset");
                File.Move(path, bak);
                return L.T("已删除预设「{0}」（文件改名为 {1}，想找回就改回去）",
                           SafeName(name), Path.GetFileName(bak));
            }
            catch (Exception e) { return L.T("ERR 删除预设失败：{0}", e.Message); }
        }

        private static string Stamp()
        {
            return DateTime.Now.ToString("yyyyMMdd-HHmmss");
        }
    }
}
