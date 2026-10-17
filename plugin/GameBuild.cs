using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using BepInEx.Logging;
using UnityEngine;

namespace BurgerShopModder
{
    /// <summary>
    /// 游戏构建核对：读游戏自己的 Assembly-CSharp.dll，算 SHA256，和开发时对着的那一份比。
    ///
    /// 【为什么要做这件事】
    /// 这个游戏**没有版本号** —— exe 的 FileVersion 填的是 Unity 的版本，
    /// app.info 里只有厂商名和标题。所以用户没法说"我是 1.2.3 版"，
    /// 而补丁能不能挂上完全取决于程序集里的方法名与签名有没有变。
    ///
    /// 因此这里算一次指纹，在日志和面板上都给出**确定的**结论：
    /// 是同一份、不是同一份、或者查不到（后者不等于不匹配）。
    ///
    /// 【为什么不做成硬性拦截】
    /// 版本不同不一定会坏 —— 可能只是某个方法还在、只是别处变了。
    /// 硬拦会把"或许能用"变成"一定不能用"。所以这里只**报告**，不阻止加载。
    /// </summary>
    internal static class GameBuild
    {
        /// <summary>开发与测试时对着的 Assembly-CSharp.dll 的 SHA256（小写）。</summary>
        internal const string ExpectedSha256 =
            "9f2c64db2dc259d4347aeb00ef644507a22aee9d5a3b66328cb27babda9a0bb4";

        internal const long ExpectedSize = 293888;

        private static bool _done;
        private static string _state;     // same / different / unknown
        private static string _actual;    // 实际算出来的哈希（成功时）
        private static string _detail;    // 查不到时的原因

        /// <summary>一句话结论，给面板用。没查过就先查。</summary>
        internal static string Summary()
        {
            EnsureChecked();
            switch (_state)
            {
                case "same":
                    return "游戏程序集与开发时一致";
                case "different":
                    return "游戏程序集与开发时【不一致】—— 补丁可能挂不上，见下方详情";
                default:
                    return "查不到游戏程序集（" + (_detail ?? "未知原因") + "）";
            }
        }

        /// <summary>多行详情，给面板的取证区用。</summary>
        internal static string Detail()
        {
            EnsureChecked();
            var sb = new StringBuilder();
            sb.AppendLine("  期望 SHA256 : " + ExpectedSha256);
            sb.AppendLine("  期望大小   : " + ExpectedSize + " 字节");
            if (!string.IsNullOrEmpty(_actual))
            {
                sb.AppendLine("  实际 SHA256 : " + _actual);
                sb.AppendLine("  实际大小   : " + _actualSize + " 字节");
            }
            if (!string.IsNullOrEmpty(_detail)) sb.AppendLine("  说明       : " + _detail);
            return sb.ToString();
        }

        private static long _actualSize;

        private static void EnsureChecked()
        {
            if (_done) return;
            _done = true;
            try
            {
                string path = FindAssembly();
                if (path == null)
                {
                    _state = "unknown";
                    _detail = "没找到 Assembly-CSharp.dll";
                    return;
                }

                var fi = new FileInfo(path);
                _actualSize = fi.Length;

                // 【为什么用 FileShare.ReadWrite】
                // 这个文件正被 Unity 的 Mono 运行时映射着。
                // 用默认的 FileShare.Read 可能直接打不开；放开共享读才拿得到内容。
                byte[] bytes;
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read,
                                               FileShare.ReadWrite | FileShare.Delete))
                {
                    bytes = new byte[fs.Length];
                    int off = 0;
                    while (off < bytes.Length)
                    {
                        int n = fs.Read(bytes, off, bytes.Length - off);
                        if (n <= 0) break;
                        off += n;
                    }
                    if (off != bytes.Length) Array.Resize(ref bytes, off);
                }

                using (var sha = SHA256.Create())
                {
                    var hash = sha.ComputeHash(bytes);
                    var sb = new StringBuilder(hash.Length * 2);
                    foreach (byte b in hash) sb.Append(b.ToString("x2"));
                    _actual = sb.ToString();
                }

                _state = string.Equals(_actual, ExpectedSha256, StringComparison.OrdinalIgnoreCase)
                       ? "same" : "different";
            }
            catch (Exception e)
            {
                _state = "unknown";
                _detail = e.GetType().Name + ": " + e.Message;
            }
        }

        private static string FindAssembly()
        {
            try
            {
                // Application.dataPath 就是 <游戏目录>/<exe名>_Data
                string data = Application.dataPath;
                if (!string.IsNullOrEmpty(data))
                {
                    string p = Path.Combine(Path.Combine(data, "Managed"), "Assembly-CSharp.dll");
                    if (File.Exists(p)) return p;
                }
                // 退路：从 BepInEx 的游戏根目录找
                string root = BepInEx.Paths.GameRootPath;
                if (!string.IsNullOrEmpty(root))
                {
                    foreach (string d in Directory.GetDirectories(root, "*_Data"))
                    {
                        string p = Path.Combine(Path.Combine(d, "Managed"), "Assembly-CSharp.dll");
                        if (File.Exists(p)) return p;
                    }
                }
            }
            catch { }
            return null;
        }

        /// <summary>启动时调一次，把结论写进日志。只报一次，不重复刷。</summary>
        internal static void ReportOnce(ManualLogSource log)
        {
            try
            {
                EnsureChecked();
                if (_state == "same")
                {
                    if (log != null) log.LogInfo("游戏程序集核对：与开发时一致");
                }
                else if (_state == "different")
                {
                    if (log != null)
                        log.LogWarning("游戏程序集核对：与开发时【不一致】。"
                            + "补丁可能挂不上，面板「系统 → 取证 / 自检」里有详情。"
                            + " 期望 " + ExpectedSha256.Substring(0, 16) + "…"
                            + "，实际 " + (_actual != null ? _actual.Substring(0, 16) : "?") + "…");
                }
                else
                {
                    if (log != null)
                        log.LogWarning("游戏程序集核对：查不到（" + (_detail ?? "未知") + "）—— 这不等于不匹配，只是没能核对。");
                }
            }
            catch { }
        }
    }
}
