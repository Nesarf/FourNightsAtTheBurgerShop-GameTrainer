using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace BurgerShopModder
{
    /// <summary>
    /// 界面文案的多语言表。
    ///
    /// 【为什么要有这一层】
    /// 面板里的按钮、开关、标签、提示行一共 100 多处，全是硬编码中文；
    /// 加上 102 条配置描述（它们会写进 .cfg，修改器也会显示）。英文读者要能用，
    /// 这些文案得先有一条出路 —— 否则只能整段重写面板代码，那不可能分批做。
    ///
    /// 【缺英文就显示中文】
    /// T("开") 查表：查到用英文，查不到把中文原样还回去。
    /// 这样翻译可以【分批推进】—— 翻三成、翻七成，任何时候编译出来都能跑，
    /// 不会出现左半面板是英文、右半面板空白的中间态。
    /// 中文那边改了字，对照表失配，那一句回落成中文，不报错。
    ///
    /// 【为什么拿中文原文当 key】
    /// 不用另编一套 key 名，改动只是外面套一层 L.T(...)：
    ///     GUILayout.Button("开")  →  GUILayout.Button(L.T("开"))
    /// 对照表就是「中文 TAB 英文」两列，不懂代码的人也能填、能校对。
    /// 代价是同一句中文在不同语境需要不同译法时区分不了 —— 真遇到了再加
    /// 「中文|后缀」那种写法，现在不做。
    ///
    /// 【译文从哪来】
    /// 1) 嵌入资源 lang/en.txt —— 随 DLL 分发，装上就能用
    /// 2) &lt;BepInEx&gt;/config/nesarf.burgershop.modder/zh-en.txt —— 有就叠加在上面
    ///    校对时改一行重启游戏就能看到，不用重编 DLL
    /// </summary>
    internal static class L
    {
        /// <summary>嵌入资源名。csproj 里用 LogicalName 钉死，免得受 RootNamespace 影响。</summary>
        internal const string ResourceName = "lang/en.txt";

        /// <summary>外置对照表的文件名（放在插件自己的 config 子目录下）。</summary>
        internal const string OverrideFileName = "zh-en.txt";

        private static Dictionary<string, string> _map;
        private static bool _on;

        /// <summary>当前是否在用英文。只有配置选了 en 且成功读过表，这里才是 true。</summary>
        internal static bool On { get { return _on; } }

        /// <summary>已经载入的条目数（诊断用）。</summary>
        internal static int Count { get { return _map == null ? 0 : _map.Count; } }

        /// <summary>
        /// 载入对照表。english = false 时直接把开关关掉，连文件都不读。
        /// 返回值写进日志用，说明这份表到底从哪来的。
        /// </summary>
        internal static string Init(bool english, string overridePath)
        {
            _on = english;
            if (!english)
            {
                // 切回中文时不卸表：下次再切回英文就不用重读磁盘。
                return "中文模式，未载入英文对照表";
            }
            if (_map != null)
            {
                return "英文模式，沿用已载入的 " + _map.Count + " 条";
            }

            _map = new Dictionary<string, string>(1024, StringComparer.Ordinal);

            int fromDll = LoadText(ReadEmbedded());
            int fromFile = 0;
            bool fileExists = !string.IsNullOrEmpty(overridePath) && File.Exists(overridePath);
            if (fileExists)
            {
                try
                {
                    fromFile = LoadText(File.ReadAllText(overridePath, Encoding.UTF8));
                }
                catch (Exception e)
                {
                    return "英文模式，内嵌 " + fromDll + " 条；外置表读取失败：" + e.GetType().Name;
                }
            }

            // 一条都没有的话，说明嵌入资源没打进去 —— 这时候别冒充英文模式，
            // 否则面板上全是中文、用户却以为自己选了英文。
            if (_map.Count == 0)
            {
                _on = false;
                return "英文模式但对照表是空的（嵌入资源 " + ResourceName +
                       " 没找到？），已回退中文";
            }

            return "英文模式，共 " + _map.Count + " 条（内嵌 " + fromDll +
                   (fileExists ? "，外置覆盖 " + fromFile : "") + "）";
        }

        /// <summary>
        /// 取文案。英文模式下查表，查不到就把中文原样还回去。
        /// null 也原样还回去 —— 调用点不用先判空。
        /// </summary>
        internal static string T(string zh)
        {
            if (!_on || zh == null) return zh;
            if (_map == null) return zh;
            string en;
            if (_map.TryGetValue(zh, out en) && !string.IsNullOrEmpty(en)) return en;
            return zh;
        }

        /// <summary>带格式化参数的版本 —— 先翻模板再填参数，参数里的数字不会被翻。</summary>
        internal static string T(string zh, params object[] args)
        {
            string fmt = T(zh);
            if (args == null || args.Length == 0) return fmt;
            try
            {
                return string.Format(fmt, args);
            }
            catch (FormatException)
            {
                // 译文里的花括号和模板对不上时不要炸整个面板，退回原文
                return zh;
            }
        }

        /// <summary>只查表、不做回落 —— 自检用，判断某个中文串到底有没有译文。</summary>
        internal static bool Has(string zh)
        {
            if (_map == null || zh == null) return false;
            return _map.ContainsKey(zh);
        }

        private static string ReadEmbedded()
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream s = asm.GetManifestResourceStream(ResourceName))
                {
                    if (s == null) return null;
                    using (var r = new StreamReader(s, Encoding.UTF8))
                    {
                        return r.ReadToEnd();
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>把「中文 TAB 英文」逐行吃进表里。返回吃进了几条。</summary>
        private static int LoadText(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int n = 0;
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd('\r');
                if (line.Length == 0) continue;
                if (line[0] == '#') continue;

                int tab = line.IndexOf('\t');
                if (tab <= 0) continue;          // 没有分隔符或者中文是空的，跳过

                string zh = line.Substring(0, tab);
                string en = line.Substring(tab + 1);
                if (zh.Length == 0 || en.Length == 0) continue;

                _map[zh] = en;
                n++;
            }
            return n;
        }
    }
}
