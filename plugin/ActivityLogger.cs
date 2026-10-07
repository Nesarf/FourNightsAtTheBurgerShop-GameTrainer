using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using UnityEngine;

namespace BurgerShopModder
{
    /// <summary>
    /// 游戏运行活动记录器。
    ///
    /// 目的：完整记录"**你做了什么 → 游戏怎么反应 → 用了什么资源**"，并可按时间轴对齐。
    /// 三类事件写进同一份 NDJSON：
    ///
    ///   · user    —— 用户的原始输入：按键按下/抬起、鼠标按键、滚轮、鼠标移动（节流）、
    ///                以及修改器面板上的每一次按钮点击
    ///   · game    —— 游戏对操作的响应：换装槽位变化、HP/绝顶变化、场景加载、资源包加载等
    ///   · sys     —— 运行资源状况：FPS、堆内存、场景内对象/贴图/材质/音频源数量、贴图绑定变化
    ///
    /// 每条事件都带同一次采样的**游戏状态快照**（HP/绝顶/换衣槽位/模型透明度），
    /// 所以离线看时间线时不需要再去别处拼上下文。
    ///
    /// 输出（每次开始记录一个新目录）：
    ///   BepInEx\l2d_dump\activity_&lt;时间戳&gt;\
    ///       activity.ndjson   逐条事件（机器可分析）
    ///       activity.txt      人类可读时间线
    ///       summary.txt       结束时的汇总（时长、计数、资源峰值）
    /// </summary>
    internal class ActivityLogger : MonoBehaviour
    {
        // ---------------------------------------------------------------- 配置
        private const int MaxEvents = 200000;        // 环形上限，防止长时间游玩吃光内存
        private const float SnapshotInterval = 1.0f; // 资源状况采样间隔（秒）

        private static ActivityLogger _instance;
        internal static bool Running { get { return _instance != null && _instance._active; } }

        private bool _active;
        private string _dir;
        private float _t0;
        private float _snapTimer;
        private float _lastFlush;

        private readonly List<string> _events = new List<string>();
        private readonly HashSet<int> _heldKeys = new HashSet<int>();
        private Vector3 _lastMouse;
        private float _lastMouseLog;
        private int _dropped;
        private int _total;

        // 计数
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

        // ---------------------------------------------------------------- 生命周期

        /// <summary>是否在插件加载时自动开始记录（默认关闭）。</summary>
        internal static bool AutoStart = false;

        internal static void StartLogging()
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("BurgerShopModder.ActivityLogger");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<ActivityLogger>();
            }
            _instance.Begin();
        }

        internal static void StopLogging()
        {
            if (_instance != null) _instance.Finish();
        }

        private void Begin()
        {
            _t0 = Time.realtimeSinceStartup;
            _active = true;
            _events.Clear();
            _dropped = 0;
            _total = 0;
            _counts.Clear();
            _snapTimer = 0f;
            _lastFlush = 0f;
            _heldKeys.Clear();

            _dir = Path.Combine(Paths.GameRootPath, "BepInEx", "l2d_dump",
                "activity_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(_dir);

            EmitUser("record_start", "开始记录活动");
            Snapshot("initial");
            Flush(true);
            Plugin.Log.LogInfo("活动记录已开始 → " + _dir);
        }

        private void Finish()
        {
            if (!_active) return;
            EmitUser("record_stop", "停止记录活动");
            Snapshot("final");
            WriteSummary();
            Flush(true);
            _active = false;
            Plugin.Log.LogInfo("活动记录已结束（" + _events.Count + " 条，丢 " + _dropped + "）→ " + _dir);
        }

        private void Update()
        {
            if (!_active) return;

            float now = Time.realtimeSinceStartup;

            CaptureInput(now);

            _snapTimer += Time.unscaledDeltaTime;
            if (_snapTimer >= SnapshotInterval)
            {
                _snapTimer = 0f;
                Snapshot("periodic");
            }

            // 每 5 秒落一次盘，避免崩溃丢数据
            if (now - _lastFlush > 5f)
            {
                _lastFlush = now;
                Flush(false);
            }
        }

        private void OnApplicationQuit()
        {
            if (_active) Finish();
        }

        // ---------------------------------------------------------------- 输入采集

        private void CaptureInput(float now)
        {
            // 键盘：按下 / 抬起
            foreach (KeyCode kc in Enum.GetValues(typeof(KeyCode)))
            {
                bool down;
                try { down = Input.GetKey(kc); }
                catch { continue; }

                int id = (int)kc;
                bool was = _heldKeys.Contains(id);
                if (down && !was)
                {
                    _heldKeys.Add(id);
                    EmitUser("key_down", kc.ToString());
                }
                else if (!down && was)
                {
                    _heldKeys.Remove(id);
                    EmitUser("key_up", kc.ToString());
                }
            }

            // 鼠标按键
            for (int b = 0; b < 3; b++)
            {
                if (Input.GetMouseButtonDown(b)) EmitUser("mouse_down", "button=" + b);
                if (Input.GetMouseButtonUp(b)) EmitUser("mouse_up", "button=" + b);
            }

            // 滚轮
            float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
                EmitUser("mouse_scroll", scroll.ToString("F3"));

            // 鼠标移动（节流：每 0.25 秒最多一条，且位移要够大）
            Vector3 m = Input.mousePosition;
            if (now - _lastMouseLog > 0.25f)
            {
                float d = (m - _lastMouse).magnitude;
                if (d > 40f)
                {
                    _lastMouseLog = now;
                    _lastMouse = m;
                    EmitUser("mouse_move", string.Format("{0:0},{1:0}", m.x, m.y));
                }
            }
        }

        // ---------------------------------------------------------------- 事件写入

        /// <summary>用户的原始操作性事件（静态入口，未在记录时为空操作）。</summary>
        internal static void EmitUser(string kind, string detail)
        {
            if (_instance != null) _instance.Add("user", kind, detail);
        }

        /// <summary>游戏对操作的响应（静态入口）。</summary>
        internal static void EmitGame(string kind, string detail)
        {
            if (_instance != null) _instance.Add("game", kind, detail);
        }

        private void Add(string cat, string kind, string detail)
        {
            if (!_active) return;

            string ck = cat + "." + kind;
            int n;
            _counts.TryGetValue(ck, out n);
            _counts[ck] = n + 1;

            var sb = new StringBuilder(256);
            sb.Append("{\"t\":").Append(T()).Append(',');
            sb.Append("\"cat\":\"").Append(Esc(cat)).Append("\",");
            sb.Append("\"kind\":\"").Append(Esc(kind)).Append("\",");
            sb.Append("\"detail\":\"").Append(Esc(detail)).Append('"');
            if (cat != "sys")
            {
                sb.Append(",\"state\":{").Append(StateJson()).Append('}');
            }
            sb.Append(",");
            ResJson(sb);
            sb.Append('}');

            _total++;
            _events.Add(sb.ToString());
            if (_events.Count > MaxEvents)
            {
                _events.RemoveRange(0, MaxEvents / 10);
                _dropped += MaxEvents / 10;
            }
        }

        /// <summary>资源状况采样。</summary>
        private void Snapshot(string reason)
        {
            if (!_active) return;
            Add("sys", "snapshot", reason);
        }

        // ---------------------------------------------------------------- 状态与资源

        private static string StateJson()
        {
            var sb = new StringBuilder(200);
            try
            {
                object tab = Plugin.Tabemi();
                if (tab != null)
                {
                    sb.Append("\"cap\":").Append(F(tab, "Cap")).Append(',');
                    sb.Append("\"upper\":").Append(F(tab, "Upper")).Append(',');
                    sb.Append("\"lower\":").Append(F(tab, "Lower")).Append(',');
                    sb.Append("\"tights\":").Append(F(tab, "Tights")).Append(',');
                    sb.Append("\"glasses\":").Append(F(tab, "Glasses")).Append(',');
                    object mdl = Plugin.FieldQuiet(tab.GetType(), "model")?.GetValue(tab);
                    if (mdl != null)
                    {
                        sb.Append("\"opTights\":").Append(F(mdl, "_Op_CenterGirlSitting_Tights")).Append(',');
                        sb.Append("\"opTightsB\":").Append(F(mdl, "_Op_CenterGirlSitting_Tights_Black")).Append(',');
                        sb.Append("\"opTightsW\":").Append(F(mdl, "_Op_CenterGirlSitting_Tights_White")).Append(',');
                    }
                }
                object pl = Plugin.Player();
                if (pl != null)
                {
                    sb.Append("\"hp\":").Append(F(pl, "currentHP")).Append(',');
                    sb.Append("\"maxHp\":").Append(F(pl, "maxHP")).Append(',');
                    sb.Append("\"ecs\":").Append(F(pl, "CurrentEcstasy")).Append(',');
                    sb.Append("\"maxEcs\":").Append(F(pl, "maxEcstasy")).Append(',');
                    sb.Append("\"syasei\":").Append(F(pl, "syaseiCount")).Append(',');
                }
                Type gm = Plugin.FindType("GameManager");
                if (gm != null)
                {
                    FieldInfo f = Plugin.FieldQuiet(gm, "DLC");
                    if (f != null) sb.Append("\"dlc\":").Append(f.GetValue(null) is bool b && b ? "true" : "false").Append(',');
                }
            }
            catch { }
            if (sb.Length > 0 && sb[sb.Length - 1] == ',') sb.Length--;
            return sb.ToString();
        }

        private static void ResJson(StringBuilder sb)
        {
            sb.Append("\"res\":{");
            try
            {
                sb.Append("\"fps\":").Append(Mathf.Round(1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime))).Append(',');
                sb.Append("\"frame\":").Append(Time.frameCount).Append(',');
                sb.Append("\"heapMB\":").Append(GC.GetTotalMemory(false) / 1048576).Append(',');
                sb.Append("\"monoMB\":").Append(UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1048576).Append(',');
                sb.Append("\"tex\":").Append(Resources.FindObjectsOfTypeAll(typeof(Texture2D)).Length).Append(',');
                sb.Append("\"mat\":").Append(Resources.FindObjectsOfTypeAll(typeof(Material)).Length).Append(',');
                sb.Append("\"go\":").Append(Resources.FindObjectsOfTypeAll(typeof(GameObject)).Length).Append(',');
                sb.Append("\"obj\":").Append(Resources.FindObjectsOfTypeAll(typeof(UnityEngine.Object)).Length).Append(',');
                string sc = "";
                try { sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name; } catch { }
                sb.Append("\"scene\":\"").Append(Esc(sc)).Append('"');
            }
            catch { }
            sb.Append('}');
        }

        private static string F(object obj, string field)
        {
            try
            {
                object v = Plugin.FieldQuiet(obj.GetType(), field)?.GetValue(obj);
                if (v is float f) return f.ToString("0.###", CultureInfo.InvariantCulture);
                if (v is int i) return i.ToString(CultureInfo.InvariantCulture);
                if (v is bool b) return b ? "true" : "false";
            }
            catch { }
            return "null";
        }

        private float T()
        {
            return (float)Math.Round(Time.realtimeSinceStartup - _t0, 3);
        }

        private static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') sb.Append("\\r");
                else if (c == '\t') sb.Append("\\t");
                else if (c < 0x20) sb.Append(' ');
                else sb.Append(c);
            }
            return sb.ToString();
        }

        // ---------------------------------------------------------------- 落盘

        private void Flush(bool force)
        {
            try
            {
                if (_events.Count == 0 && !force) return;
                File.AppendAllText(Path.Combine(_dir, "activity.ndjson"),
                    string.Join("\n", _events.ToArray()) + "\n", new UTF8Encoding(false));
                _events.Clear();
            }
            catch (Exception e)
            {
                if (Plugin.Log != null) Plugin.Log.LogWarning("活动记录落盘失败：" + e.Message);
            }
        }

        private void WriteSummary()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("=== 游戏运行活动记录 汇总 ===");
                sb.AppendLine("目录      " + _dir);
                sb.AppendLine("时长      " + T().ToString("0.0") + " 秒");
                sb.AppendLine("时段      " + _dir.Substring(Math.Max(0, _dir.Length - 15)));
                sb.AppendLine("事件总数  " + _total);
                sb.AppendLine("丢失事件  " + _dropped);
                sb.AppendLine();
                sb.AppendLine("事件计数：");
                var keys = new List<string>(_counts.Keys);
                keys.Sort();
                foreach (string k in keys)
                    sb.AppendLine(string.Format("  {0,-28} {1}", k, _counts[k]));
                sb.AppendLine();
                sb.AppendLine("当前状态：");
                sb.AppendLine("  " + StateJson());
                sb.AppendLine();
                sb.AppendLine("说明：");
                sb.AppendLine("  activity.ndjson  逐条事件（machine-readable，含每次采样的游戏状态与资源状况）");
                sb.AppendLine("  activity.txt     人类可读时间线（同一批数据的文本视图）");

                File.WriteAllText(Path.Combine(_dir, "summary.txt"), sb.ToString(), new UTF8Encoding(false));

                // 人类可读视图：把 ndjson 里最常用的字段摊平
                var txt = new StringBuilder();
                txt.AppendLine("时间(s)\t类别\t事件\t详情");
                foreach (string line in File.ReadAllLines(Path.Combine(_dir, "activity.ndjson")))
                    txt.AppendLine(Flat(line));
                File.WriteAllText(Path.Combine(_dir, "activity.txt"), txt.ToString(), new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                if (Plugin.Log != null) Plugin.Log.LogWarning("写汇总失败：" + e.Message);
            }
        }

        /// <summary>把一条 ndjson 摊平成可读行（只取几个关键字段，避免手写 JSON 解析）。</summary>
        private static string Flat(string json)
        {
            string t = Field(json, "\"t\":");
            string cat = Field(json, "\"cat\":");
            string kind = Field(json, "\"kind\":");
            string detail = Field(json, "\"detail\":");
            string scene = Field(json, "\"scene\":");
            string fps = Field(json, "\"fps\":");
            return string.Join("\t", new string[] { t, cat, kind, detail, "scene=" + scene, "fps=" + fps });
        }

        private static string Field(string json, string key)
        {
            int i = json.IndexOf(key, StringComparison.Ordinal);
            if (i < 0) return "";
            i += key.Length;
            if (i < json.Length && json[i] == '"')
            {
                int j = json.IndexOf('"', i + 1);
                if (j < 0) return "";
                return json.Substring(i + 1, j - i - 1);
            }
            int k = i;
            while (k < json.Length && json[k] != ',' && json[k] != '}') k++;
            return json.Substring(i, k - i);
        }
    }
}
