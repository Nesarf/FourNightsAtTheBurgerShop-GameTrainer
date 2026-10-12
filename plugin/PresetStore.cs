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
    /// </summary>
    internal static class PresetStore
    {
        private const string DirName = "nesarf.burgershop.modder.presets";
        private const int MaxPresets = 20;      // 上限，免得列表长到没法看

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

        /// <summary>列出已有的预设名（按文件名排序）。</summary>
        internal static List<string> List()
        {
            var outList = new List<string>();
            try
            {
                string dir = DirPath();
                if (dir == null || !Directory.Exists(dir)) return outList;
                foreach (string f in Directory.GetFiles(dir, "*.preset"))
                {
                    string n = Path.GetFileNameWithoutExtension(f);
                    // 【含点的都不是预设】
                    // 预设名经过 SafeName 过滤，不可能含点；而备份文件是
                    // "<名字>.before-<时间戳>" 和 "<名字>.current-before-apply-<时间戳>"。
                    // 原来只排除 ".before-"，结果 current-before-apply 那批被当成预设列了出来 ——
                    // 列表里会多出一堆带着「应用/覆盖/删除」按钮的备份，很容易误点。
                    if (n.IndexOf('.') >= 0) continue;
                    outList.Add(n);
                }
                outList.Sort(StringComparer.OrdinalIgnoreCase);
            }
            catch { }
            return outList;
        }

        /// <summary>把当前配置整份写成一份预设。同名会覆盖（覆盖前先备份旧的）。</summary>
        internal static string Save(string name)
        {
            try
            {
                var cfg = Plugin.ConfigPublic();
                if (cfg == null) return "ERR 拿不到配置对象";

                string dir = DirPath();
                string path = PathOf(name);
                if (dir == null || path == null) return "ERR 预设名不合法（只允许中英文、数字、空格、- 和 _）";

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
                return "已保存预设「" + SafeName(name) + "」（" + (lines.Count - 3) + " 项）"
                     + (backup != null ? "，原同名预设已备份" : "");
            }
            catch (Exception e) { return "ERR 保存预设失败：" + e.Message; }
        }

        /// <summary>
        /// 应用一份预设。
        /// 【先备份当前状态】—— 覆盖之前一定留退路，这是硬规矩。
        /// </summary>
        internal static string Apply(string name)
        {
            try
            {
                var cfg = Plugin.ConfigPublic();
                if (cfg == null) return "ERR 拿不到配置对象";

                string path = PathOf(name);
                if (path == null || !File.Exists(path)) return "ERR 没有这个预设：" + name;

                // (1) 覆盖之前先把当前状态存下来
                string dir = DirPath();
                string before = Path.Combine(dir, SafeName(name) + ".current-before-apply-" + Stamp() + ".preset");
                {
                    var cur = new List<string>();
                    cur.Add("# 应用预设「" + SafeName(name) + "」之前的当前状态");
                    cur.Add("# 存于 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    foreach (var def in cfg.ConfigDefinitions)
                        cur.Add(def.Section + "\t" + def.Key + "\t" + cfg[def].GetSerializedValue());
                    File.WriteAllLines(before, cur.ToArray(), new UTF8Encoding(false));
                }

                // (2) 逐项写回。按 Section+Key 找，不按 Key 找 —— 不同 Section 下可能重名。
                var want = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
                {
                    string l = raw;
                    if (l.Length == 0 || l[0] == '#') continue;
                    string[] p = l.Split('\t');
                    if (p.Length < 3) continue;
                    want[p[0] + "\u0001" + p[1]] = p[2];
                }

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
                return "已应用预设「" + SafeName(name) + "」：" + applied + " 项"
                     + (missing > 0 ? "（" + missing + " 项预设里没有，保持当前值）" : "")
                     + "；应用前的状态已备份为 " + Path.GetFileName(before);
            }
            catch (Exception e) { return "ERR 应用预设失败：" + e.Message; }
        }

        internal static string Delete(string name)
        {
            try
            {
                string path = PathOf(name);
                if (path == null || !File.Exists(path)) return "ERR 没有这个预设：" + name;
                string dir = DirPath();
                string bak = Path.Combine(dir, SafeName(name) + ".deleted-" + Stamp() + ".preset");
                File.Move(path, bak);
                return "已删除预设「" + SafeName(name) + "」（文件改名为 " + Path.GetFileName(bak) + "，想找回就改回去）";
            }
            catch (Exception e) { return "ERR 删除预设失败：" + e.Message; }
        }

        private static string Stamp()
        {
            return DateTime.Now.ToString("yyyyMMdd-HHmmss");
        }
    }
}
