using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BurgerShopModder
{
    /// <summary>
    /// 命令接口：让游戏可以被 pwsh / bash / cmd **直接驱动**。
    ///
    /// 为什么需要：靠"模拟鼠标点击 + 截图"来自动化既脆弱又慢（窗口焦点、坐标、DPI 全是坑）。
    /// 改成文件命令通道后，脚本只要写一行文本，游戏就会执行并把结果写回来 —— 完全可脚本化。
    ///
    /// 协议（极简，避免解析歧义）：
    ///   写命令：  BepInEx\l2d_dump\cmd\in.txt     一行一条
    ///   读结果：  BepInEx\l2d_dump\cmd\out.txt    追加，带时间戳
    ///
    /// 支持的命令：
    ///   ping                          探活
    ///   scene                         打印当前场景名
    ///   scene &lt;name&gt;                 切换到指定场景（例：TabemiMain）
    ///   state                         打印裤袜/换装/部件透明度/强制状态
    ///   tights &lt;0|1|2&gt;               设裤袜并应用
    ///   costume &lt;slot&gt; &lt;value&gt;      设换装槽位（cap/upper/lower/tights/glasses）
    ///   force off|all                 关闭 / 开启"强制显示全部部件"
    ///   force part &lt;名字&gt; on|off      单独强制某部件
    ///   shot &lt;名字&gt;                  截图到 cmd\&lt;名字&gt;.png
    ///   probe masks|partindex|parttable|atlas|vmap|diag   触发对应诊断
    ///   quit                          退出游戏
    /// </summary>
    internal class GameCommandServer : MonoBehaviour
    {
        private static GameCommandServer _instance;
        internal static bool Running { get { return _instance != null; } }

        private string _dir;
        private string _inPath;
        private static string _outPath;
        private float _timer;

        internal static void StartServer()
        {
            if (_instance != null) return;
            GameObject go = new GameObject("BurgerShopModder.CmdServer");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _instance = go.AddComponent<GameCommandServer>();
        }

        private void Awake()
        {
            _dir = Path.Combine(Paths.GameRootPath, "BepInEx", "l2d_dump", "cmd");
            Directory.CreateDirectory(_dir);
            _inPath = Path.Combine(_dir, "in.txt");
            _outPath = Path.Combine(_dir, "out.txt");
            if (!File.Exists(_outPath))
                File.WriteAllText(_outPath, "", new UTF8Encoding(false));
            Say("命令服务已启动。写入 " + _inPath + " 即可驱动游戏。");
        }

        private static void Say(string s)
        {
            try
            {
                File.AppendAllText(_outPath,
                    DateTime.Now.ToString("HH:mm:ss.fff") + "  " + s + "\n", new UTF8Encoding(false));
            }
            catch { }
        }

        private void Update()
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer < 0.1f) return;      // 每 0.1 秒轮询一次，够快也不浪费
            _timer = 0f;

            try
            {
                if (!File.Exists(_inPath)) return;
                string all = File.ReadAllText(_inPath, Encoding.UTF8);
                File.WriteAllText(_inPath, "", new UTF8Encoding(false));   // 立刻清空，避免重复执行

                foreach (string raw in all.Split('\n'))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    try { Execute(line); }
                    catch (Exception e) { Say("ERR  " + line + "  → " + e.GetType().Name + ": " + e.Message); }
                }
            }
            catch { }
        }

        // ---------------------------------------------------------------- 命令分发

        private void Execute(string line)
        {
            string[] a = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (a.Length == 0) return;
            string verb = a[0].ToLowerInvariant();

            switch (verb)
            {
                case "ping":
                    Say("pong  scene=" + SceneName());
                    break;

                case "scene":
                    if (a.Length == 1) { Say("scene " + SceneName()); break; }
                    try
                    {
                        SceneManager.LoadScene(a[1]);
                        Say("scene → " + a[1] + "（已请求加载）");
                    }
                    catch (Exception e) { Say("ERR scene: " + e.Message); }
                    break;

                case "state":
                    Say(StateLine());
                    break;

                case "tights":
                    if (a.Length < 2) { Say("ERR tights 需要参数 0/1/2"); break; }
                    int tv = int.Parse(a[1], CultureInfo.InvariantCulture);
                    {
                        object tab = Plugin.Tabemi();
                        if (tab == null) { Say("ERR 拿不到 TabemiControl（不在店内场景？）"); break; }
                        Plugin.SetFieldPublic(tab, "Tights", tv);
                        Plugin.InvokeMethodPublic(tab, "UpdateCostume");
                        Say("tights = " + tv + "（已应用）");
                    }
                    break;

                case "costume":
                    if (a.Length < 3) { Say("ERR costume 需要 <slot> <value>"); break; }
                    {
                        string slot = SlotField(a[1]);
                        if (slot == null) { Say("ERR 未知槽位 " + a[1]); break; }
                        object tab = Plugin.Tabemi();
                        if (tab == null) { Say("ERR 拿不到 TabemiControl"); break; }
                        Plugin.SetFieldPublic(tab, slot, int.Parse(a[2], CultureInfo.InvariantCulture));
                        Plugin.InvokeMethodPublic(tab, "UpdateCostume");
                        Say("costume " + slot + " = " + a[2]);
                    }
                    break;

                case "force":
                    if (a.Length >= 2 && a[1] == "off") { Plugin.SetForceAll(false); Say("force off"); }
                    else if (a.Length >= 2 && a[1] == "all") { Plugin.SetForceAll(true); Say("force all"); }
                    else if (a.Length >= 4 && a[1] == "part")
                    {
                        bool on = a[3] == "on";
                        Plugin.SetPartForce(a[2], on);
                        Say("force part " + a[2] + " " + a[3]);
                    }
                    else Say("ERR 用法: force off | force all | force part <名字> on|off");
                    break;

                case "shot":
                    {
                        string name = a.Length >= 2 ? a[1] : "shot";
                        string p = Path.Combine(_dir, name + ".png");
                        try
                        {
                            Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                            tex.Apply();
                            byte[] png = tex.EncodeToPNG();
                            UnityEngine.Object.Destroy(tex);
                            File.WriteAllBytes(p, png);
                            Say("shot → " + p + "  (" + Screen.width + "x" + Screen.height + ")");
                        }
                        catch (Exception e) { Say("ERR shot: " + e.Message); }
                    }
                    break;

                case "probe":
                    if (a.Length < 2) { Say("ERR probe <masks|partindex|parttable|atlas|vmap|diag>"); break; }
                    Plugin.RunProbe(a[1]);
                    Say("probe " + a[1] + " 已触发");
                    break;

                case "ecsgain":
                    {
                        // 注入一笔正向量，走的正是游戏自己的 EcstasyChange ——
                        // 因此会经过上升率与「绝顶适应」倍率，可用来直接验证削减效果。
                        if (a.Length < 2) { Say("ecsgain <量>"); break; }
                        float amt = float.Parse(a[1], CultureInfo.InvariantCulture);
                        Say(Plugin.InjectEcstasyGain(amt));
                    }
                    break;

                case "animlog":
                    {
                        float sec = 10f;
                        if (a.Length >= 2) float.TryParse(a[1], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out sec);
                        AnimRecorder.Start(sec);
                        Say("开始逐帧记录动画速率 " + sec + " 秒 → cmd\anim.csv");
                    }
                    break;

                case "redlog":
                    {
                        float sec = 6f;
                        if (a.Length >= 2) float.TryParse(a[1], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out sec);
                        RedRecorder.Start(sec);
                        Say("开始逐帧红光采样 " + sec + " 秒 → cmd\\red.csv（每帧一行）");
                    }
                    break;

                case "tremorlog":
                    {
                        float sec = 4f;
                        if (a.Length >= 2) float.TryParse(a[1], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out sec);
                        TremorRecorder.Start(sec);
                        Say("开始逐帧记录绝顶值 " + sec + " 秒 → cmd\tremor.csv");
                    }
                    break;

                case "xwatch":
                    {
                        if (a.Length >= 2 && a[1] == "off") { Plugin.XWatchStop(); Say("持续监视已停止"); break; }
                        float sec = 60f;
                        if (a.Length >= 2) float.TryParse(a[1], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out sec);
                        Plugin.XWatchStart(sec);
                        Say("持续监视 " + sec + " 秒 → cmd\\xwatch.txt（每秒一次，只记异常）");
                    }
                    break;

                case "xcheck":
                    {
                        string rep = Plugin.CrossCheck();
                        foreach (string ln in rep.Split('\n'))
                            if (ln.Trim().Length > 0) Say(ln);
                        try
                        {
                            File.WriteAllText(Path.Combine(_dir, "xcheck.txt"), rep, new UTF8Encoding(false));
                        }
                        catch { }
                    }
                    break;

                case "defaults":
                    // 【防误用】必须显式写 "defaults yes" 才执行。
                    // 教训：此前调试收尾反复调 defaults，把用户调好的三十来项覆盖成了代码默认值。
                    // 要改配置做实验，请用 tools/cfgsnap.sh save / restore 那一套。
                    if (a.Length >= 2 && a[1].ToLowerInvariant() == "yes")
                        Say(Plugin.RestoreDefaults());
                    else
                        Say("defaults 会覆盖你的全部调参，已要求确认。真的要用请写：defaults yes"
                            + (char)10 + "  更推荐：bash tools/cfgsnap.sh save / restore");
                    break;

                case "urgeaudit":
                    {
                        int n = 60;
                        if (a.Length >= 2) int.TryParse(a[1], out n);
                        Plugin.EnableUrgeAudit(n);
                        Say("索取欲审计已开启，记录接下来 " + n + " 次变动 → BepInEx/LogOutput.log");
                    }
                    break;

                case "spank":
                    {
                        int n = 1;
                        if (a.Length >= 2) int.TryParse(a[1], out n);
                        for (int i = 0; i < Mathf.Clamp(n, 1, 20); i++) Plugin.SimulateSpank();
                        Say("已模拟 " + n + " 次打屁股 → 乘数 ×" + Plugin.SpankSpeedMul().ToString("0.###")
                            + "  目标 ×" + Plugin.SpankSpeedTarget().ToString("0.###"));
                    }
                    break;

                case "situnlock":
                    Plugin.SetSitLockPublic(false);
                    Say("[坐姿锁定] 已解锁 —— 游戏下次尝试切换就会成功（要再锁上：sitlock）");
                    break;
                case "sitlock":
                    Plugin.SetSitLockPublic(true);
                    Say("[坐姿锁定] 已锁上 —— 进入坐姿后不会再被切走");
                    break;
                case "tabshot":
                    {
                        // tabshot <页签> <名字>
                        // 切栏与截图在同一帧完成，并写出面板矩形供精确裁切。
                        if (a.Length < 3) { Say("ERR tabshot <页签> <名字>"); break; }
                        int ti = -1;
                        if (!int.TryParse(a[1], out ti))
                        {
                            string[] nm2 = Plugin.TabNamesPublic();
                            ti = -1;
                            for (int i2 = 0; i2 < nm2.Length; i2++)
                                if (nm2[i2] == a[1] || nm2[i2].Contains(a[1])) { ti = i2; break; }
                        }
                        Say(Plugin.TabShotPublic(ti, a[2]));
                        break;
                    }
                case "tab":
                    {
                        // tab          -> 列出页签
                        // tab 3        -> 切到第 4 栏（0 起）
                        // tab 正骑     -> 按名字切
                        string a2 = a.Length >= 2 ? string.Join(" ", a, 1, a.Length - 1).Trim() : "";
                        if (a2.Length == 0) { Say(Plugin.DumpTabsPublic()); break; }
                        string[] names = Plugin.TabNamesPublic();
                        int idx = -1;
                        // 注意：TryParse 失败会把 idx 置 0，而 0 是合法页签 ——
                        // 必须看返回值，不能只看结果值（踩过：tab 正骑 切到了玩家）
                        if (!int.TryParse(a2, out idx)) idx = -1;
                        if (idx < 0 || idx >= names.Length)
                        {
                            for (int i = 0; i < names.Length; i++)
                                if (names[i] == a2 || names[i].Contains(a2)) { idx = i; break; }
                        }
                        if (idx < 0 || idx >= names.Length) { Say("ERR 没有这个页签：" + a2); break; }
                        Plugin.SetTabPublic(idx);
                        Say("已切到 [" + idx + "] " + names[idx]);
                        break;
                    }
                case "bindings":
                    Say(Plugin.DumpBindingsPublic());
                    break;
                case "manman":
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.Append("[HitArea_Manman] pose=").Append(Plugin.PoseIdxPublic());
                        sb.Append("  enabled=").Append(Plugin.ManmanEnabledPublic());
                        sb.Append("  src=").Append(Plugin.ManmanSourcePublic());
                        try
                        {
                            Type hc0 = Plugin.FindTypePublic("Live2D_HitAreaCheck");
                            var pf0 = hc0 != null ? Plugin.FieldQuietPublic(hc0, "mousePointing") : null;
                            sb.Append("  mousePointing=").Append(pf0 != null ? ((pf0.GetValue(null) as string) ?? "(empty)") : "?");
                        }
                        catch { }
                        string rect;
                        bool inr = Plugin.ManmanRectPublic(out rect);
                        sb.Append("  rect=").Append(rect);
                        sb.Append("  mouse=").Append(Input.mousePosition.x.ToString("0")).Append(",").Append(Input.mousePosition.y.ToString("0"));
                        sb.Append("  inside=").Append(inr ? "YES" : "no");
                        Say(sb.ToString());
                    }
                    break;
                case "demand":
                    Say(Plugin.DemandCmd(a.Length >= 2 ? a[1] : "status"));
                    break;

                case "tremor":
                    Say(Plugin.TremorCmd(a.Length >= 2 ? a[1] : "status")
                        + (a.Length >= 3 && a[1] == "set" ? "" : ""));
                    break;

                case "set":
                    if (a.Length < 3) { Say("ERR set <项> <值>  如 set NoEcstasy 0"); break; }
                    Say(Plugin.SetCfg(a[1], a[2]));
                    break;

                case "cfg":
                    Say(Plugin.DumpCfg());
                    break;

                case "states":
                    {
                        // 直接读状态机字段 —— "状态被锁死"这类问题看字段最准，不用看图。
                        var sb = new StringBuilder("状态机: ");
                        object tab = Plugin.Tabemi();
                        if (tab == null) { Say("ERR 拿不到 TabemiControl（不在店内场景？）"); break; }
                        string[] names = new string[] {
                            "centerGirlState", "osiriState", "sitState", "specialAttState", "tabemiAttState",
                            "LeftGirlState", "RightGirlState", "kissing",
                            "吸精フェラflag", "current叩く量", "Osiri叩く量", "Osiri解除叩く量",
                            "LeftGirlTimer", "RightGirlTimer", "SitGirlStartTimer", "SitGirlStartTimer",
                            "tabemiPower", "TabemiAtt", "fellaAttRate", "ateCount"
                        };
                        foreach (string n in names)
                            sb.Append(n).Append('=').Append(Plugin.GetStateFieldText(tab, n)).Append("  ");
                        Say(sb.ToString());

                        // 点击是否被"桌子"吃掉 —— HitArea_Osiri 的分支靠它决定丢弃还是计数
                        {
                            var sb3 = new StringBuilder("点击归属: ");
                            int found = 0;
                            foreach (UnityEngine.Object o in Resources.FindObjectsOfTypeAll(typeof(MonoBehaviour)))
                            {
                                Component comp = o as Component;
                                if (comp == null) continue;
                                Type ct = comp.GetType();
                                if (Plugin.FieldQuiet(ct, "mouseIsPointingTable") == null) continue;
                                sb3.Append("mouseIsPointingTable=")
                                   .Append(Plugin.GetStateFieldText(comp, "mouseIsPointingTable")).Append("  ");
                                found++;
                                if (found >= 3) break;
                            }
                            if (found == 0) sb3.Append("（没找到该字段）");
                            Say(sb3.ToString());
                        }

                        Say("索取模式: " + Plugin.DemandCmd("status"));
                        // 直接读"当前正在播的动画状态的实际速率"——这是判断绝顶有没有加速的唯一硬证据
                        Say("动画速率: " + Plugin.ReadAnimSpeed());

                        // 动画层状态（Animancer）——是否还停在吸精/绝顶那一层
                        Type at = Plugin.FindType("Live2D_AnimationControl");
                        if (at != null)
                        {
                            UnityEngine.Object[] acs = Resources.FindObjectsOfTypeAll(at);
                            if (acs != null && acs.Length > 0)
                            {
                                object ac = acs[0];
                                var sb2 = new StringBuilder("动画层: ");
                                foreach (string n in new string[] { "KyuseiRate", "fellaKyuseiAttRate", "fellaSpeedUpRate",
                                                                   "fellaSpeed", "isSpeedUpFella", "fellaSpeedPlus" })
                                    sb2.Append(n).Append('=').Append(Plugin.GetStateFieldText(ac, n)).Append("  ");
                                Say(sb2.ToString());

                                // osiriSpeed / isSpeedUpOsiri / sitSpeed 在【另外两个类】上，
                                // 从 Live2D_AnimationControl 读只会得到 "-"。
                                foreach (string tn in new string[] { "Live2D_Animation_SitOsiri", "Live2D_Animation_Sit" })
                                {
                                    Type ot = Plugin.FindType(tn);
                                    if (ot == null) continue;
                                    UnityEngine.Object[] os2 = Resources.FindObjectsOfTypeAll(ot);
                                    if (os2 == null || os2.Length == 0) continue;
                                    var sb3 = new StringBuilder(tn.Replace("Live2D_Animation_", "动画层·") + ": ");
                                    foreach (string n in new string[] { "osiriSpeed", "osiriSpeedPlus", "isSpeedUpOsiri",
                                                                        "sitSpeed", "isSpeedUpSit" })
                                        sb3.Append(n).Append('=').Append(Plugin.GetStateFieldText(os2[0], n)).Append("  ");
                                    Say(sb3.ToString());
                                }
                            }
                        }
                    }
                    break;

                case "ecs":
                    {
                        var sb = new StringBuilder("绝顶一览: ");
                        object pc = null;
                        try
                        {
                            Type pt = Plugin.FindType("PlayerControl");
                            if (pt != null)
                            {
                                UnityEngine.Object[] ps = Resources.FindObjectsOfTypeAll(pt);
                                if (ps != null && ps.Length > 0) pc = ps[0];
                            }
                        }
                        catch { }
                        if (pc == null) { Say("ERR 找不到 PlayerControl"); break; }

                        sb.Append("CurrentEcstasy=").Append(Plugin.GetFloatPublic(pc, "CurrentEcstasy").ToString("0.###"));
                        sb.Append(" maxEcstasy=").Append(Plugin.GetFloatPublicRaw(pc, "maxEcstasy").ToString("0.###"));
                        sb.Append(" ecstasyRate=").Append(Plugin.GetFloatPublicRaw(pc, "ecstasyRate").ToString("0.####"));
                        sb.Append(" EcstasyResist=").Append(Plugin.GetFloatPublic(pc, "EcstasyResist").ToString("0.###"));
                        sb.Append(" Syaseing=").Append(Plugin.GetFloatPublic(pc, "Syaseing").ToString("0"));
                        sb.Append(" HP=").Append(Plugin.GetFloatPublic(pc, "currentHP").ToString("0.#"));
                        sb.Append("/").Append(Plugin.GetFloatPublic(pc, "maxHP").ToString("0.#"));

                        // 本修改器的相关设置
                        // 吸精（榨取）相关：KyuseiRate 决定榨取结束后【再来一轮】的概率。
                        // 设成 100 就是无限连榨 —— 表现为"持续闪红光 + 状态改变不生效"。
                        try
                        {
                            Type at = Plugin.FindType("Live2D_AnimationControl");
                            if (at != null)
                            {
                                UnityEngine.Object[] acs = Resources.FindObjectsOfTypeAll(at);
                                if (acs != null && acs.Length > 0)
                                {
                                    object ac = acs[0];
                                    sb.Append("  | 吸精发生率KyuseiRate=").Append(Plugin.GetFloatPublicRaw(ac, "KyuseiRate").ToString("0"))
                                      .Append("% 吸精攻击率=").Append(Plugin.GetFloatPublicRaw(ac, "fellaKyuseiAttRate").ToString("0"))
                                      .Append("% 加速率=").Append(Plugin.GetFloatPublicRaw(ac, "fellaSpeedUpRate").ToString("0"))
                                      .Append("%");
                                }
                            }
                        }
                        catch { }

                        sb.Append("  | 上升率=").Append(Plugin.GetRateCfg("EcstasyUpRate").ToString("0"))
                          .Append("% 下降率=").Append(Plugin.GetRateCfg("EcstasyDownRate").ToString("0"))
                          .Append("% 抗性=").Append(Plugin.GetRateCfg("EcstasyResist").ToString("0.##"))
                          .Append(" 上限目标=").Append(Plugin.GetRateCfg("TargetEcstasy").ToString("0"))
                          .Append(" 锁上限=").Append(Plugin.GetRateCfg("LockMaxEcstasy").ToString("0"))
                          .Append(" 无绝顶=").Append(Plugin.GetRateCfg("NoEcstasy").ToString("0"));
                        Say(sb.ToString());
                    }
                    break;

                case "parts":
                    {
                        Type camType = Plugin.FindType("Live2D.Cubism.Core.CubismModel");
                        if (camType == null) { Say("ERR 找不到 CubismModel"); break; }
                        UnityEngine.Object[] models = Resources.FindObjectsOfTypeAll(camType);
                        if (models == null || models.Length == 0) { Say("ERR 没有模型实例"); break; }
                        UnityEngine.Object[] parts = (UnityEngine.Object[])camType.GetProperty("Parts", FLAGS)
                            .GetValue(models[0], null);
                        var on = new List<string>();
                        var off = new List<string>();
                        for (int i = 0; i < parts.Length; i++)
                        {
                            Component pc = parts[i] as Component;
                            if (pc == null) continue;
                            string pid = "";
                            try { pid = pc.GetType().GetProperty("Id", FLAGS)?.GetValue(pc, null) as string ?? pc.name; } catch { }
                            object ov = Plugin.FieldQuiet(pc.GetType(), "Opacity")?.GetValue(pc);
                            float op = ov is float f ? f : -1f;
                            (op >= 0.5f ? on : off).Add(i + ":" + pid);
                        }
                        Say("部件透明度 → 开(" + on.Count + "): " + string.Join(" ", on.ToArray()));
                        Say("                关(" + off.Count + "): " + string.Join(" ", off.ToArray()));
                    }
                    break;

                case "timescale":
                    if (a.Length < 2) { Say("timescale = " + Time.timeScale); break; }
                    {
                        float tv2 = float.Parse(a[1], CultureInfo.InvariantCulture);
                        // 【不要归零】Time.timeScale = 0 会让动画停住，而 Syaseing 正是靠
                        // 动画事件（Event_*SyaseiOnEnd）清除的 —— 一旦归零，榨取会卡住不收束。
                        // 想冻结画面请用 0.05 这种极小值。
                        if (tv2 < 0.05f)
                        {
                            tv2 = 0.05f;
                            Say("已把时间刻度钳到 0.05（0 会让动画停住、状态机卡死）");
                        }
                        Time.timeScale = tv2;
                        Say("Time.timeScale = " + Time.timeScale);
                    }
                    break;

                case "rendercheck":
                    Say(RenderCheck(a.Length >= 2 ? a[1] : null));
                    break;

                case "pix":
                    if (a.Length < 5) { Say("ERR pix <x0> <y0> <x1> <y1>"); break; }
                    Say(PixStats(int.Parse(a[1]), int.Parse(a[2]), int.Parse(a[3]), int.Parse(a[4]), "手动区域"));
                    break;

                case "colliders":
                    {
                        var lines = ListColliders();
                        Say("可点击对象（有碰撞体且激活）共 " + lines.Count + " 个:");
                        foreach (string one in lines) Say("   " + one);
                    }
                    break;

                case "clickobj":
                    if (a.Length < 2) { Say("ERR clickobj <GameObject 名字>"); break; }
                    {
                        string hit = ClickObject(a[1]);
                        Say(hit != null ? ("clickobj → " + hit) : ("ERR 没找到可点击对象: " + a[1]));
                    }
                    break;

                case "buttons":
                    {
                        var found = ListButtons();
                        Say("场景内按钮 " + found.Count + " 个:");
                        foreach (string one in found) Say("   " + one);
                    }
                    break;

                case "click":
                    if (a.Length < 2) { Say("ERR click <名字或按钮文字>"); break; }
                    {
                        string hit = ClickButton(a[1]);
                        Say(hit != null ? ("click → " + hit) : ("ERR 没找到匹配按钮: " + a[1]));
                    }
                    break;

                case "skipdialog":
                    Plugin.SetSkipDialog(a.Length >= 2 && a[1] == "on");
                    Say("skipdialog " + (a.Length >= 2 ? a[1] : "off"));
                    break;

                case "quit":
                    Say("quit 收到，退出游戏");
                    Application.Quit();
                    break;

                default:
                    Say("ERR 未知命令: " + verb);
                    break;
            }
        }

        /// <summary>
        /// 列出场景内所有 UnityEngine.UI.Button。
        /// 用途：脚本驱动的"点击"不经过系统鼠标（因而不会抢窗口焦点），
        /// 直接命中游戏自己的按钮回调。
        /// </summary>
        private static List<string> ListButtons()
        {
            var list = new List<string>();
            try
            {
                UnityEngine.Object[] objs = Resources.FindObjectsOfTypeAll(typeof(UnityEngine.UI.Button));
                int n = 0;
                foreach (UnityEngine.Object o in objs)
                {
                    Component b = o as Component;
                    if (b == null) continue;
                    if (!b.gameObject.activeInHierarchy) continue;
                    n++;
                    string label = b.gameObject.name;
                    string t = GetButtonText(b);
                    if (!string.IsNullOrEmpty(t)) label += "  [" + t + "]";
                    list.Add(label);
                }
                if (n == 0) list.Add("（没有活动按钮）");
            }
            catch (Exception e) { list.Add("ERR " + e.Message); }
            return list;
        }

        private static string GetButtonText(Component b)
        {
            try
            {
                // TextMeshPro 优先（本作使用 TMP），退回 UnityEngine.UI.Text
                foreach (Component c in b.GetComponentsInChildren<Component>(true))
                {
                    if (c == null) continue;
                    Type t = c.GetType();
                    PropertyInfo pi = t.GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
                    if (pi != null && pi.PropertyType == typeof(string))
                    {
                        object v = pi.GetValue(c, null);
                        string s = v as string;
                        if (!string.IsNullOrEmpty(s)) return s;
                    }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 直接读回屏幕像素并**只输出数值**（不落图）。
        ///
        /// 目的：判断"某件东西到底有没有渲染出来"，用数字说话，避免每次存图再人工看图。
        /// </summary>
        private static string PixStats(int x0, int y0, int x1, int y1, string label)
        {
            try
            {
                int w = Screen.width, h = Screen.height;
                x0 = Mathf.Clamp(x0, 0, w - 1); x1 = Mathf.Clamp(x1, x0 + 1, w);
                y0 = Mathf.Clamp(y0, 0, h - 1); y1 = Mathf.Clamp(y1, y0 + 1, h);

                Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                Color32[] px = tex.GetPixels32();
                UnityEngine.Object.Destroy(tex);

                // 注意：ReadPixels 得到的是**自下而上**的行序
                long r = 0, g = 0, b = 0, n = 0, dark = 0, bright = 0;
                for (int y = y0; y < y1; y++)
                {
                    int row = (h - 1 - y) * w;
                    for (int x = x0; x < x1; x++)
                    {
                        Color32 c = px[row + x];
                        r += c.r; g += c.g; b += c.b; n++;
                        int lum = (c.r + c.g + c.b) / 3;
                        if (lum < 60) dark++;
                        if (lum > 200) bright++;
                    }
                }
                if (n == 0) return label + ": 空区域";
                return string.Format("{0}  矩形({1},{2})-({3},{4})  均值RGB=({5},{6},{7})  暗<60={8:0.0}%  亮>200={9:0.0}%",
                    label, x0, y0, x1, y1, r / n, g / n, b / n,
                    100.0 * dark / n, 100.0 * bright / n);
            }
            catch (Exception e) { return "ERR pix: " + e.Message; }
        }

        /// <summary>
        /// 丝袜渲染检查：取丝袜 drawable 的屏幕并集，读回该区域像素并输出数值。
        /// 调用前请先用 tights 命令设置状态；数值应当随 无/黑/白 明显变化。
        /// </summary>
        private static string RenderCheck(string which)
        {
            var sb = new StringBuilder();
            try
            {
                Type camType = Plugin.FindType("Live2D.Cubism.Core.CubismModel");
                Type dt = Plugin.FindType("Live2D.Cubism.Core.CubismDrawable");
                if (camType == null || dt == null) return "ERR 找不到 Cubism 类型";

                UnityEngine.Object[] models = Resources.FindObjectsOfTypeAll(camType);
                if (models == null || models.Length == 0) return "ERR 没有 CubismModel（不在店内场景？）";

                UnityEngine.Object[] draws = (UnityEngine.Object[])camType.GetProperty("Drawables", FLAGS)
                    .GetValue(models[0], null);
                if (draws == null) return "ERR 没有 Drawables";

                // 丝袜 6 个 + 对照（正常可见的部件），用于判断"可见的 drawable 长什么样"
                string[] want6 = new string[] { "ArtMesh186", "ArtMesh187", "ArtMesh188", "ArtMesh189", "ArtMesh190", "ArtMesh191",
                                                "ArtMesh11", "ArtMesh176", "ArtMesh204" };
                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;

                foreach (UnityEngine.Object o in draws)
                {
                    Component dc = o as Component;
                    if (dc == null || Array.IndexOf(want6, dc.name) < 0) continue;

                    Renderer rend = dc.GetComponent<Renderer>();
                    if (rend == null) continue;

                    // 部件透明度
                    int ppi = -1;
                    try { object v = dt.GetProperty("ParentPartIndex", FLAGS)?.GetValue(dc, null); if (v is int iv) ppi = iv; } catch { }
                    float partOp = -1;
                    try
                    {
                        UnityEngine.Object[] parts = (UnityEngine.Object[])camType.GetProperty("Parts", FLAGS).GetValue(models[0], null);
                        if (parts != null && ppi >= 0 && ppi < parts.Length)
                        {
                            Component pc = parts[ppi] as Component;
                            object v = Plugin.FieldQuiet(pc.GetType(), "Opacity")?.GetValue(pc);
                            if (v is float f) partOp = f;
                        }
                    }
                    catch { }

                    // CubismRenderer 的生效状态（与镜头/画面无关，才是判断"画没画"的正解）
                    string eff = "";
                    try
                    {
                        Component cr = rend;
                        PropertyInfo cp = cr.GetType().GetProperty("Color", FLAGS);
                        if (cp != null)
                        {
                            object cv = cp.GetValue(cr, null);
                            if (cv is Color col)
                                eff = " color=(" + col.r.ToString("0.##") + "," + col.g.ToString("0.##")
                                    + "," + col.b.ToString("0.##") + "," + col.a.ToString("0.##") + ")";
                        }
                        foreach (string pn in new string[] { "DidBecomeVisible", "DidBecomeInvisible", "NewVertexColors", "NewVertexPositions" })
                        {
                            PropertyInfo pi = cr.GetType().GetProperty(pn, FLAGS);
                            if (pi != null)
                            {
                                object v = pi.GetValue(cr, null);
                                eff += " " + pn + "=" + (v is bool b ? (b ? "1" : "0") : "?");
                            }
                        }
                    }
                    catch { }

                    sb.Append(dc.name).Append(" partOp=").Append(partOp.ToString("0.##"))
                      .Append(" vis=").Append(rend.isVisible ? 1 : 0)
                      .Append(eff).Append("  ");

                    try
                    {
                        Bounds b = rend.bounds;
                        Camera cam = Camera.main;
                        if (cam != null)
                        {
                            Vector3 c1 = cam.WorldToScreenPoint(b.center - b.extents);
                            Vector3 c2 = cam.WorldToScreenPoint(b.center + b.extents);
                            float sx0 = Mathf.Min(c1.x, c2.x), sx1 = Mathf.Max(c1.x, c2.x);
                            float sy0 = Screen.height - Mathf.Max(c1.y, c2.y), sy1 = Screen.height - Mathf.Min(c1.y, c2.y);
                            minX = Mathf.Min(minX, sx0); maxX = Mathf.Max(maxX, sx1);
                            minY = Mathf.Min(minY, sy0); maxY = Mathf.Max(maxY, sy1);
                        }
                    }
                    catch { }
                    sb.AppendLine();
                }

                if (minX < maxX && minY < maxY)
                {
                    sb.AppendLine(PixStats((int)minX, (int)minY, (int)maxX, (int)maxY, "丝袜并集区域"));
                }
                else sb.AppendLine("（未能计算丝袜屏幕区域）");
            }
            catch (Exception e) { sb.Append("ERR rendercheck: ").Append(e.Message); }
            return sb.ToString().Replace("\n", " | ");
        }

        /// <summary>
        /// 列出场景里"可点击"的对象。
        /// 本作不用 UnityEngine.UI.Button，而是碰撞体 + OnMouseUpAsButton / 射线检测，
        /// 所以要用 Collider 来找可点击目标。
        /// </summary>
        private const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.Static
                                          | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly string[] CLICK_CALLBACKS =
            new string[] { "OnMouseUpAsButton", "OnMouseUp", "OnMouseDown", "OnClick", "OnPointerClick" };

        /// <summary>收集场景里所有"带点击回调"的组件（游戏不用 UI Button，用碰撞体 + OnMouse*）。</summary>
        private static List<Component> FindClickables()
        {
            var list = new List<Component>();
            try
            {
                UnityEngine.Object[] objs = Resources.FindObjectsOfTypeAll(typeof(MonoBehaviour));
                foreach (UnityEngine.Object o in objs)
                {
                    Component comp = o as Component;
                    if (comp == null) continue;
                    GameObject go = comp.gameObject;
                    if (go == null || !go.activeInHierarchy) continue;
                    Type t = comp.GetType();
                    foreach (string m in CLICK_CALLBACKS)
                    {
                        if (t.GetMethod(m, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null)
                        {
                            list.Add(comp);
                            break;
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        private static List<string> ListColliders()
        {
            var list = new List<string>();
            foreach (Component comp in FindClickables())
            {
                var sb = new StringBuilder();
                sb.Append(comp.gameObject.name).Append("  (").Append(comp.GetType().Name).Append(")  ");
                var cbs = new List<string>();
                Type t = comp.GetType();
                foreach (string m in CLICK_CALLBACKS)
                    if (t.GetMethod(m, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null)
                        cbs.Add(m);
                sb.Append(string.Join(", ", cbs.ToArray()));
                list.Add(sb.ToString());
            }
            return list;
        }

        /// <summary>按名字找到 GameObject，调用其上的 OnMouseUpAsButton（游戏自己的点击回调）。</summary>
        private static string ClickObject(string want)
        {
            try
            {
                foreach (Component comp in FindClickables())
                {
                    if (comp.gameObject.name.IndexOf(want, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var done = new List<string>();
                    Type t = comp.GetType();
                    foreach (string m in new string[] { "OnMouseDown", "OnMouseUpAsButton", "OnMouseUp" })
                    {
                        MethodInfo mi = t.GetMethod(m, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (mi == null) continue;
                        mi.Invoke(comp, null);
                        done.Add(m);
                    }
                    return comp.gameObject.name + "  调用: " + string.Join(", ", done.ToArray());
                }
            }
            catch (Exception e) { Say("ERR clickobj: " + e.Message); }
            return null;
        }

        /// <summary>按名字或按钮文字匹配并触发 onClick。返回命中的按钮描述，未命中返回 null。</summary>
        private static string ClickButton(string want)
        {
            try
            {
                UnityEngine.Object[] objs = Resources.FindObjectsOfTypeAll(typeof(UnityEngine.UI.Button));
                foreach (UnityEngine.Object o in objs)
                {
                    Component b = o as Component;
                    if (b == null || !b.gameObject.activeInHierarchy) continue;
                    string name = b.gameObject.name;
                    string txt = GetButtonText(b) ?? "";
                    if (name.IndexOf(want, StringComparison.OrdinalIgnoreCase) < 0 &&
                        txt.IndexOf(want, StringComparison.OrdinalIgnoreCase) < 0) continue;

                    UnityEngine.UI.Button btn = b as UnityEngine.UI.Button;
                    if (btn == null) continue;
                    btn.onClick.Invoke();
                    return name + (txt.Length > 0 ? " [" + txt + "]" : "");
                }
            }
            catch (Exception e) { Say("ERR click: " + e.Message); }
            return null;
        }

        private static string SlotField(string s)
        {
            switch (s.ToLowerInvariant())
            {
                case "cap": return "Cap";
                case "upper": return "Upper";
                case "lower": return "Lower";
                case "tights": return "Tights";
                case "glasses": return "Glasses";
                default: return null;
            }
        }

        private static string SceneName()
        {
            try { return SceneManager.GetActiveScene().name; } catch { return "?"; }
        }

        private static string StateLine()
        {
            var sb = new StringBuilder();
            sb.Append("scene=").Append(SceneName());
            try
            {
                object tab = Plugin.Tabemi();
                if (tab != null)
                {
                    sb.Append("  cap=").Append(Plugin.GetFloatPublic(tab, "Cap"))
                      .Append(" upper=").Append(Plugin.GetFloatPublic(tab, "Upper"))
                      .Append(" lower=").Append(Plugin.GetFloatPublic(tab, "Lower"))
                      .Append(" tights=").Append(Plugin.GetFloatPublic(tab, "Tights"))
                      .Append(" glasses=").Append(Plugin.GetFloatPublic(tab, "Glasses"));
                }
                else sb.Append("  （无 TabemiControl）");

                Type mc = Plugin.FindType("Live2D_ModelControl");
                UnityEngine.Object[] mcs = mc != null ? Resources.FindObjectsOfTypeAll(mc) : null;
                if (mcs != null && mcs.Length > 0)
                {
                    sb.Append("  | _OpW=").Append(Plugin.GetFloatPublic(mcs[0], "_Op_CenterGirlSitting_Tights_White"))
                      .Append(" _OpB=").Append(Plugin.GetFloatPublic(mcs[0], "_Op_CenterGirlSitting_Tights_Black"))
                      .Append(" _OpT=").Append(Plugin.GetFloatPublic(mcs[0], "_Op_CenterGirlSitting_Tights"));
                }

                Type cam = Plugin.FindType("Live2D.Cubism.Core.CubismModel");
                UnityEngine.Object[] models = cam != null ? Resources.FindObjectsOfTypeAll(cam) : null;
                if (models != null && models.Length > 0)
                {
                    UnityEngine.Object[] parts = (UnityEngine.Object[])cam.GetProperty("Parts",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(models[0], null);
                    if (parts != null && parts.Length > 40)
                    {
                        sb.Append("  | 部件39=").Append(PartOp(parts[39]))
                          .Append(" 部件40=").Append(PartOp(parts[40]))
                          .Append(" 部件19(Tights)=").Append(PartOp(parts[19]));
                    }
                }
                sb.Append("  | 强制=").Append(Plugin.ForceStateText());
            }
            catch (Exception e) { sb.Append("  ERR ").Append(e.Message); }
            return sb.ToString();
        }

        private static float PartOp(UnityEngine.Object part)
        {
            try
            {
                Component pc = part as Component;
                if (pc == null) return -1;
                FieldInfo fi = Plugin.FieldQuiet(pc.GetType(), "Opacity");
                object v = fi != null ? fi.GetValue(pc) : null;
                return v is float f ? f : -1;
            }
            catch { return -1; }
        }
    }
}

namespace BurgerShopModder
{
    /// <summary>
    /// 逐帧记录绝顶值到 cmd\tremor.csv。
    /// 用途：动摇是 2~3.6Hz 的波，命令通道 0.1 秒轮询采样太粗，看不出波形。
    /// </summary>
    internal class TremorRecorder : MonoBehaviour
    {
        private static TremorRecorder _inst;
        private float _until;
        private System.Text.StringBuilder _sb;
        private string _path;

        internal static void Start(float seconds)
        {
            if (_inst == null)
            {
                GameObject go = new GameObject("BurgerShopModder.TremorRecorder");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _inst = go.AddComponent<TremorRecorder>();
            }
            _inst.Begin(seconds);
        }

        private void Begin(float seconds)
        {
            _path = Path.Combine(Paths.GameRootPath, "BepInEx", "l2d_dump", "cmd", "tremor.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            _sb = new System.Text.StringBuilder("t,ecstasy,rate,maxEcstasy,tremorActive,caught,timeScale,pulseInterval\n");
            _until = Time.time + Mathf.Clamp(seconds, 0.5f, 30f);
        }

        private void Update()
        {
            if (_sb == null) return;
            if (Time.time > _until)
            {
                try { File.WriteAllText(_path, _sb.ToString(), new UTF8Encoding(false)); } catch { }
                _sb = null;
                return;
            }
            try
            {
                object p = Plugin.PlayerRef();
                if (p != null)
                {
                    _sb.Append((Time.time).ToString("0.000")).Append(',')
                       .Append(Plugin.GetFloatPublicRaw(p, "CurrentEcstasy").ToString("0.###")).Append(',')
                       .Append(Plugin.GetFloatPublicRaw(p, "ecstasyRate").ToString("0.####")).Append(',')
                       .Append(Plugin.GetFloatPublicRaw(p, "maxEcstasy").ToString("0.###")).Append(',')
                       .Append(Plugin.TremorActiveFlag() ? "1" : "0").Append(',')
                       .Append(Plugin.TremorCaughtFlag() ? "1" : "0").Append(',')
                       .Append(Time.timeScale.ToString("0.###")).Append(',')
                       .Append(Plugin.AccumInterval().ToString("0.####")).Append(',')
                       .Append(Plugin.AdaptFactorNow().ToString("0.###")).Append(',')
                       .Append(Plugin.GetFloatPublicRaw(p, "maxHP").ToString("0.##")).Append(',')
                       .Append(Plugin.GetFloatPublicRaw(p, "currentHP").ToString("0.##")).Append(',')
                       .Append(Plugin.HpBonusNow().ToString("0.##")).Append(',')
                       .Append(Plugin.AdaptRemaining().ToString("0.##")).Append((char)10);
                }
            }
            catch { }
        }
    }
}

namespace BurgerShopModder
{
    /// <summary>
    /// 逐帧采样屏幕中心的红通道，用来客观判断"持续闪红光"。
    ///
    /// 为什么需要它：静态截图看不出"闪"，而人眼看到的闪烁在数值上就是红通道的振荡。
    /// 采样区域取屏幕中心一小块（便宜），同时记录 Syaseing 状态，方便把"闪"和"榨取"对齐。
    /// </summary>
    internal class RedRecorder : MonoBehaviour
    {
        private static RedRecorder _inst;
        private float _until;
        private System.Text.StringBuilder _sb;
        private string _path;
        private Texture2D _tex;

        internal static void Start(float seconds)
        {
            if (_inst == null)
            {
                GameObject go = new GameObject("BurgerShopModder.RedRecorder");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _inst = go.AddComponent<RedRecorder>();
            }
            _inst.Begin(seconds);
        }

        private void Begin(float seconds)
        {
            _path = Path.Combine(Paths.GameRootPath, "BepInEx", "l2d_dump", "cmd", "red.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            _sb = new System.Text.StringBuilder("t,meanR,meanG,meanB,syaseing,syaseElapsed,timeScale\n");
            _until = Time.unscaledTime + Mathf.Clamp(seconds, 1f, 60f);
            if (_tex == null)
                _tex = new Texture2D(160, 160, TextureFormat.RGB24, false);
        }

        private void Update()
        {
            if (_sb == null) return;
            if (Time.unscaledTime > _until)
            {
                try { File.WriteAllText(_path, _sb.ToString(), new UTF8Encoding(false)); } catch { }
                _sb = null;
                return;
            }
            try
            {
                int w = 160, h = 160;
                int x = Mathf.Max(0, Screen.width / 2 - w / 2);
                int y = Mathf.Max(0, Screen.height / 2 - h / 2);
                _tex.ReadPixels(new Rect(x, y, w, h), 0, 0);
                _tex.Apply();
                Color32[] px = _tex.GetPixels32();
                long r = 0, g = 0, b = 0;
                for (int i = 0; i < px.Length; i++) { r += px[i].r; g += px[i].g; b += px[i].b; }
                int n = px.Length;

                object pl = Plugin.PlayerRef();
                float sy = pl != null ? Plugin.GetFloatPublicRaw(pl, "Syaseing") : -1f;
                _sb.Append(Time.unscaledTime.ToString("0.000")).Append(',')
                   .Append((r / n).ToString()).Append(',')
                   .Append((g / n).ToString()).Append(',')
                   .Append((b / n).ToString()).Append(',')
                   .Append(sy > 0.5f ? "1" : "0").Append(',')
                   .Append(Plugin.SyaseingElapsedNow().ToString("0.###")).Append(',')
                   .Append(Time.timeScale.ToString("0.###")).Append((char)10);
            }
            catch { }
        }
    }
}

namespace BurgerShopModder
{
    /// <summary>
    /// 逐帧记录"动画实际速率"，用来验证绝顶阶段到底有没有加速。
    ///
    /// 为什么要逐帧：绝顶只持续一两秒，命令通道 1.5 秒轮询一次基本抓不到。
    /// 记录 osiriState + 实际速率 + 是否已套倍速，事后一对照就清楚。
    /// </summary>
    internal class AnimRecorder : MonoBehaviour
    {
        private static AnimRecorder _inst;
        private float _until;
        private System.Text.StringBuilder _sb;
        private string _path;

        internal static void Start(float seconds)
        {
            if (_inst == null)
            {
                GameObject go = new GameObject("BurgerShopModder.AnimRecorder");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _inst = go.AddComponent<AnimRecorder>();
            }
            _inst.Begin(seconds);
        }

        private void Begin(float seconds)
        {
            _path = Path.Combine(Paths.GameRootPath, "BepInEx", "l2d_dump", "cmd", "anim.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            _sb = new System.Text.StringBuilder();
            _sb.Append("t,osiriState,animSpeed,applied,osiriSpeed,demand,syaseing").Append((char)10);
            _until = Time.unscaledTime + Mathf.Clamp(seconds, 1f, 120f);
        }

        private void Update()
        {
            if (_sb == null) return;
            if (Time.unscaledTime > _until)
            {
                try { File.WriteAllText(_path, _sb.ToString(), new UTF8Encoding(false)); } catch { }
                _sb = null;
                return;
            }
            try { _sb.Append(Plugin.SampleAnimLine()).Append((char)10); } catch { }
        }
    }
}
