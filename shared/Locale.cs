using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace BurgerShopLocale
{
    /// <summary>
    /// 安装程序与外部修改器共用的文案表。
    ///
    /// 【为什么要单独一份】
    /// 这两个 exe 不是 BepInEx 插件，拿不到游戏内那套 Localization.cs；
    /// 而它们又是英文用户最先碰到的两个窗口 —— 插件的面板翻得再全，
    /// 安装程序一打开全是中文，那第一步就过不去。
    ///
    /// 【语言怎么定】
    /// 1) 命令行 --lang en / --lang zh（显式指定，优先）
    /// 2) 游戏目录里 cfg 的 Language（跟插件面板保持一致）
    /// 3) 系统 UI 语言：中文系统用中文，其余用英文
    /// 装之前还没有 cfg，所以第 3 条是首次安装时的兜底。
    ///
    /// 【缺译文就显示中文】
    /// 与插件那套一致：查不到就原样还回中文。翻漏了一句不会空着，也不会崩。
    ///
    /// 【字典从哪来】
    /// 由 tools/wrap-exe-i18n.py 从两个源文件的 Txt.T("...") 里提取 key，
    /// 这里只负责存译文。key 必须与调用点一字不差 —— 差一个字就安静地回落中文。
    /// </summary>
    internal static class Txt
    {
        private static Dictionary<string, string> _map;
        private static bool _on;

        /// <summary>当前是否在用英文。</summary>
        internal static bool On { get { return _on; } }

        /// <summary>表里有多少条（诊断用）。</summary>
        internal static int Count { get { return _map == null ? 0 : _map.Count; } }

        /// <summary>cfg 文件名：与插件的 GUID 一致。</summary>
        private const string CfgName = "nesarf.burgershop.modder.cfg";

        /// <summary>
        /// 决定用哪种语言，并载入对照表。返回一个短标签，写日志用。
        /// </summary>
        internal static string Init(string gameRoot, string explicitLang)
        {
            string lang = Normalize(explicitLang);
            string from = "命令行";
            if (lang == null)
            {
                lang = FromCfg(gameRoot);
                from = "cfg";
            }
            if (lang == null)
            {
                lang = IsChineseUi() ? "zh" : "en";
                from = "系统语言";
            }
            _on = lang == "en";
            if (_on && _map == null) _map = Build();
            return _on ? "en (" + from + ", " + _map.Count + " entries)" : "zh (" + from + ")";
        }

        /// <summary>取文案。英文模式查表，查不到原样还回中文。</summary>
        internal static string T(string zh)
        {
            if (!_on || zh == null || _map == null) return zh;
            string en;
            if (_map.TryGetValue(zh, out en) && en != null) return en;
            return zh;
        }

        /// <summary>带格式化参数的版本 —— 先翻模板再填参数。</summary>
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
                return zh;
            }
        }

        private static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            s = s.Trim().ToLowerInvariant();
            if (s == "en" || s == "english") return "en";
            if (s == "zh" || s == "cn" || s == "chinese") return "zh";
            return null;
        }

        /// <summary>从 &lt;游戏目录&gt;/BepInEx/config/&lt;GUID&gt;.cfg 里读 Language。</summary>
        private static string FromCfg(string gameRoot)
        {
            if (string.IsNullOrEmpty(gameRoot)) return null;
            try
            {
                string p = Path.Combine(gameRoot, "BepInEx", "config", CfgName);
                if (!File.Exists(p)) return null;
                foreach (string raw in File.ReadAllLines(p, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line[0] == '#' || line[0] == '[') continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    if (line.Substring(0, eq).Trim() != "Language") continue;
                    return Normalize(line.Substring(eq + 1));
                }
            }
            catch { }
            return null;
        }

        private static bool IsChineseUi()
        {
            try
            {
                string n = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                return string.Equals(n, "zh", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>命令行里的 --lang xxx / -lang xxx / --lang=xxx。</summary>
        internal static string LangFromArgs(string[] args)
        {
            if (args == null) return null;
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                if (a == "--lang" || a == "-lang")
                {
                    if (i + 1 < args.Length) return args[i + 1];
                }
                else if (a.StartsWith("--lang=", StringComparison.Ordinal))
                {
                    return a.Substring(7);
                }
            }
            return null;
        }

        private static Dictionary<string, string> Build()
        {
            var m = new Dictionary<string, string>(256, StringComparer.Ordinal);
            // 译文按文件分组，key 与调用点一字不差。

            m["  · 也可以运行「数值修改器.exe」在进游戏前改"] = "  - or run the Trainer exe to change values before launching";
            m["  · 想还原就点「卸载还原」或运行「卸载还原.ps1」"] = "  - to restore, click Uninstall or run the restore script";
            m["  · 游戏里按 F9 打开数值面板"] = "  - press F9 in game to open the trainer panel";
            m["  装完启动游戏"] = "  Launch after install";
            m["# 安装时间 {0}"] = "# Installed at {0}";
            m["# 格式：相对路径 <TAB> sha256（空 = 安装前已存在，卸载不删）"] = "# Format: relative path <TAB> sha256 (empty = already existed, uninstall keeps it)";
            m["# 汉堡店数值修改器 安装清单 —— 卸载时按此表精确还原"] = "# Burger Shop Trainer install manifest - uninstall restores exactly this list";
            m["# 版本 {0}"] = "# Version {0}";
            m["(自然) 安装完成。"] = "(normal) install finished.";
            m["(自然) 已还原。游戏现在是原版状态。"] = "(normal) restored. The game is back to its original state.";
            m["0~1，1 = 完全免疫"] = "0 to 1, 1 = fully immune";
            m["1 = 原速"] = "1 = normal speed";
            m["=== 开始卸载: {0}"] = "=== uninstall started: {0}";
            m["=== 开始安装: {0}"] = "=== install started: {0}";
            m["{0}失败：{1}"] = "{0} failed: {1}";
            m["一天总时长（秒）"] = "Day length (seconds)";
            m["保存"] = "Save";
            m["保存并启动游戏"] = "Save and run";
            m["保留原有文件 {0}"] = "Keeping the existing file {0}";
            m["写入 PlayerPrefs，持久生效"] = "Writes PlayerPrefs, persists";
            m["写入失败：{0}"] = "Write failed: {0}";
            m["写入安装清单"] = "Writing the install manifest";
            m["冻结时钟"] = "Freeze the clock";
            m["出错"] = "Error";
            m["删除 {0}"] = "Deleting {0}";
            m["卸载"] = "uninstall";
            m["卸载失败：{0}"] = "Uninstall failed: {0}";
            m["卸载还原"] = "Uninstall";
            m["原版 10"] = "Vanilla is 10";
            m["原版 180"] = "Vanilla is 180";
            m["反馈 / 建议 / 其他游戏版本的适配需求：nesarfpersonal@163.com"] = "Feedback: nesarfpersonal@163.com";
            m["口交速度加成"] = "Fellatio speed bonus";
            m["另一个写入者正在写（游戏内的修改器？），请稍后重试。"] = "Another writer is writing (the in-game trainer?); try again shortly.";
            m["吃汉（对方）"] = "Eater (opponent)";
            m["启动失败：{0}"] = "Launch failed: {0}";
            m["启动游戏失败：{0}"] = "Could not launch the game: {0}";
            m["安装"] = "Install";
            m["安装 BepInEx 与修改器插件；游戏本体不会被改动。版本 {0}"] = "Installs BepInEx and the trainer plugin. The game itself is not modified. Version {0}";
            m["安装包内缺少载荷资源，文件可能已损坏。"] = "The payload resource is missing from this package; the file may be corrupt.";
            m["安装失败：{0}"] = "Install failed: {0}";
            m["安装完成。"] = "Install finished.";
            m["安装完成，{0} 个文件：{1}"] = "Installed {0} files: {1}";
            m["完成"] = "Done";
            m["完成：删除 {0} 个文件"] = "Done: removed {0} files";
            m["对方无法让你射精"] = "Cannot make you ejaculate";
            m["射精次数"] = "Ejaculation count";
            m["将从游戏目录删除 BepInEx 与修改器，游戏回到原样。\n\n（安装前就存在的文件会保留）\n\n确定卸载？"] = "This removes BepInEx and the trainer from the game folder and restores the original state.\n\n(Files that existed before the install are kept.)\n\nUninstall?";
            m["尚未找到配置文件。先启动一次游戏（让 BepInEx 生成 cfg），再回来改。"] = "No config file yet. Launch the game once so BepInEx creates the cfg, then come back.";
            m["已保存。\n\n游戏里随时按 F9 也能打开修改面板。"] = "Saved.\n\nYou can also press F9 in game to open the trainer panel.";
            m["已卸载还原。"] = "Uninstalled and restored.";
            m["已卸载，删除 {0} 个文件：{1}"] = "Uninstalled, removed {0} files: {1}";
            m["已自动找到游戏目录。"] = "Game folder found automatically.";
            m["影响每天的具材数，重进当天生效"] = "Re-enter the day to apply";
            m["恢复默认"] = "Reset";
            m["打开游戏目录"] = "Open game dir";
            m["找不到游戏"] = "Game not found";
            m["找不到游戏目录，请显式传入。"] = "Game folder not found; pass it explicitly.";
            m["找到游戏：{0}"] = "Found the game: {0}";
            m["拉高伤害与绝顶值"] = "Raises damage and ecstasy";
            m["改完点「保存」，再进游戏生效。热键 F9 也能在游戏里开面板。"] = "Edit, then click Save and enter the game. F9 opens the in-game panel too.";
            m["攻击力倍率"] = "Attack multiplier";
            m["攻击力强化"] = "Attack power boost";
            m["数值修改器 · 外部编辑器"] = "Burger Shop Trainer - Editor";
            m["数值修改器 · 安装程序"] = "Burger Shop Trainer - Installer";
            m["文件被占用且无法登记重启删除：{0}"] = "File is in use and could not be queued for deletion: {0}";
            m["无敌 · HP 锁满"] = "Invincible";
            m["无法保存"] = "Cannot save";
            m["无限待在店里"] = "Stay in the shop forever";
            m["暂时无法保存"] = "Cannot save right now";
            m["有 {0} 个文件没能删除（多为游戏仍在运行占用）：\n{1}"] = "{0} files could not be removed (usually because the game is still running):\n{1}";
            m["未自动定位到游戏目录。"] = "Could not locate the game folder automatically.";
            m["检测到这个目录里已经装过。\n继续会覆盖安装（设置文件也会被重置为默认）。\n\n要继续吗？"] = "This folder already has an install.\nContinuing overwrites it (and resets the config file to defaults).\n\nContinue?";
            m["汉堡店 数值修改器"] = "Burger Shop Trainer";
            m["汉堡店 数值修改器 · 安装程序"] = "Burger Shop Trainer - Installer";
            m["汉堡店修改器 反馈"] = "Burger Shop Trainer feedback";
            m["没找到游戏目录。\n\n请把本编辑器放到游戏根目录（与 {0} 同级）再运行。"] = "Game folder not found.\n\nPut this editor in the game root folder, next to {0}, then run it.";
            m["没有自动找到游戏目录，请点「浏览…」手动选择。"] = "The game folder was not found automatically; choose it with Browse.";
            m["注意：有文件被游戏占用，已登记为下次重启时自动删除。"] = "Note: some files were in use by the game and are queued for deletion at the next restart.";
            m["流程"] = "Flow";
            m["浏览…"] = "Browse...";
            m["清理旧版插件 {0}"] = "Removing the previous plugin {0}";
            m["游戏未运行：可直接保存。rev {0}"] = "The game is not running: you can save directly. rev {0}";
            m["游戏正在运行，请先关闭游戏。"] = "The game is running. Close it first.";
            m["游戏正在运行，请先关闭游戏再安装（会占用 winhttp.dll）。"] = "The game is running. Close it before installing (it locks winhttp.dll).";
            m["游戏正在运行：保存仍然有效（已做修订号合并），但建议改完重启游戏让面板同步。"] = "The game is running: saving still works (rev merge is in place), but restart the game so the panel picks it up.";
            m["游戏版本："] = "Game version:";
            m["游戏目录"] = "Game folder";
            m["游戏速度"] = "Game speed";
            m["游戏里按 F9 打开数值面板；也可运行「数值修改器.exe」预先设置。"] = "Press F9 in game to open the trainer panel, or run the Trainer exe beforehand to set values.";
            m["玩家（你）"] = "Player (you)";
            m["目录不存在。"] = "Folder not found.";
            m["目录不存在：{0}"] = "Folder not found: {0}";
            m["绝顶值归零"] = "Ecstasy to zero";
            m["绝顶抗性"] = "Ecstasy resistance";
            m["被榨取也不掉血"] = "Drained without losing HP";
            m["解出 {0}"] = "Extracting {0}";
            m["解锁全部章节"] = "Unlock every chapter";
            m["该目录里没有 {0}：{1}"] = "That folder does not contain {0}: {1}";
            m["请选择游戏根目录（里面有 {0}）"] = "Choose the game folder (it contains {0})";
            m["还没有配置文件。\n\n先启动一次游戏，让 BepInEx 生成它，再来保存。"] = "No config file yet.\n\nLaunch the game once so BepInEx creates it, then save.";
            m["这个目录里没有 {0}，请选择正确的游戏根目录。"] = "That folder does not contain {0}; choose the correct game root folder.";
            m["这个目录里没有安装记录（找不到 {0}）。"] = "That folder has no install record (cannot find {0}).";
            m["连吃汉堡上限"] = "Max burgers in a row";
            m["选择游戏根目录（里面要有 {0}）"] = "Choose the game root folder (it must contain {0})";
            m["配合上面的开关"] = "Works with the switch above";
            m["配置文件尚未生成 —— 先启动一次游戏。"] = "The config has not been created yet - launch the game once.";
            m["问题描述："] = "Problem:";
            m["骑乘速度加成"] = "Riding speed bonus";
            m["（可附上 BepInEx 目录下的 LogOutput.log 与本工具的 trainer_dpi.log）"] = "(You may attach LogOutput.log from the BepInEx folder and trainer_dpi.log from this tool.)";
            m["，{0} 个被占用、已登记重启后删除"] = ", {0} were in use and are queued for deletion after a restart";
            m["，保留 {0} 个原有文件"] = ", kept {0} pre-existing files";
            m["，失败 {0} 个"] = ", failed: {0}";
            return m;
        }
    }
}
