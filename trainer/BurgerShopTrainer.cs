using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BurgerShopTrainer
{
    /// <summary>
    /// Four Nights at the Burger Shop — 数值修改器 · 外部编辑器
    /// 读写 BepInEx 插件的 cfg 文件，不改动游戏本体。
    /// </summary>
    internal static class Program
    {
        internal const string GameFolderName = "Four Nights at the Burger Shop ～ハンバーガー食べながら食べられるミニゲーム～";
        internal const string GameExe = "Four Nights at the Burger Shop.exe";
        internal const string ConfigRelative = @"BepInEx\config\nesarf.burgershop.modder.cfg";

        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        internal static string FindGameRoot()
        {
            // 1) 编辑器放在游戏目录里 → 直接用当前目录
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, GameExe)))
                return AppDomain.CurrentDomain.BaseDirectory;

            // 2) 放在 D:\ 之类的上级目录 → 找同名子目录
            string[] roots = { "D:\\", "E:\\", "C:\\" };
            foreach (string root in roots)
            {
                string p = Path.Combine(root, GameFolderName);
                if (File.Exists(Path.Combine(p, GameExe))) return p;
            }

            // 3) 让用户自己指
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "请选择游戏根目录（里面有 " + GameExe + "）";
                if (dlg.ShowDialog() == DialogResult.OK) return dlg.SelectedPath;
            }
            return null;
        }
    }

    internal sealed class Setting
    {
        public string Section;
        public string Key;
        public string Comment;
        public bool IsBool;
        public double Min;
        public double Max;
        public bool IsInt;
        public string Value;
    }

    internal sealed class MainForm : Form
    {
        private readonly string _root;
        private readonly string _cfgPath;
        private readonly List<Setting> _settings = new List<Setting>();
        private readonly Dictionary<string, Control> _controls = new Dictionary<string, Control>();
        private readonly Dictionary<string, Label> _valueLabels = new Dictionary<string, Label>();
        private Label _statusLabel;

        private static readonly Color Ink = Color.FromArgb(28, 32, 44);
        private static readonly Color Accent = Color.FromArgb(158, 128, 255);

        private readonly string[] Order =
        {
            "GodMode", "NoEcstasy", "EcstasyResist", "MaxBurgerNum", "SyaseiCount",
            "PowerUnlock", "TabemiPowerMul", "FellaSpeedPlus", "SitSpeedPlus",
            "FrozenClock", "ClockFullTime", "GameSpeed", "UnlockAllDays"
        };

        private static readonly Dictionary<string, string[]> Meta = new Dictionary<string, string[]>
        {
            // key => { 分组, 中文名, 备注 }
            { "GodMode",         new[] { "玩家（你）", "无敌 · HP 锁满", "被榨取也不掉血" } },
            { "NoEcstasy",       new[] { "玩家（你）", "绝顶值归零", "对方无法让你射精" } },
            { "EcstasyResist",   new[] { "玩家（你）", "绝顶抗性", "0~1，1 = 完全免疫" } },
            { "MaxBurgerNum",    new[] { "玩家（你）", "连吃汉堡上限", "原版 10" } },
            { "SyaseiCount",     new[] { "玩家（你）", "射精次数", "影响每天的具材数，重进当天生效" } },

            { "PowerUnlock",     new[] { "吃汉（对方）", "攻击力强化", "拉高伤害与绝顶值" } },
            { "TabemiPowerMul",  new[] { "吃汉（对方）", "攻击力倍率", "配合上面的开关" } },
            { "FellaSpeedPlus",  new[] { "吃汉（对方）", "口交速度加成", "0~1" } },
            { "SitSpeedPlus",    new[] { "吃汉（对方）", "骑乘速度加成", "0~1" } },

            { "FrozenClock",     new[] { "流程", "冻结时钟", "无限待在店里" } },
            { "ClockFullTime",   new[] { "流程", "一天总时长（秒）", "原版 180" } },
            { "GameSpeed",       new[] { "流程", "游戏速度", "1 = 原速" } },
            { "UnlockAllDays",   new[] { "流程", "解锁全部章节", "写入 PlayerPrefs，持久生效" } }
        };

        public MainForm()
        {
            _root = Program.FindGameRoot();
            Text = "汉堡店 数值修改器";

            // 程序带 DPI 感知清单（原生清晰，不被系统位图拉伸）；
            // 但不交给 WinForms 自动缩放——它在中文系统上会按字体度量算出非 1 的比值把布局压小。
            // 改为自己按 DeviceDpi 显式乘算，行为可预期。
            AutoScaleMode = AutoScaleMode.None;

            ClientSize = S(560, 640);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            Font = new Font("Microsoft YaHei UI", 9.5f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            if (_root == null)
            {
                MessageBox.Show("没找到游戏目录。\n\n请把本编辑器放到游戏根目录（与 " + Program.GameExe + " 同级）再运行。",
                    "找不到游戏", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Load += (s, e) => Close();
                return;
            }

            _cfgPath = Path.Combine(_root, Program.ConfigRelative);
            _revAtLoad = ReadRev();

            Label title = new Label
            {
                Text = "数值修改器 · 外部编辑器",
                Font = new Font("Microsoft YaHei UI", 15f, FontStyle.Bold),
                ForeColor = Ink,
                Location = P(20, 14),
                AutoSize = true
            };
            Controls.Add(title);

            Label sub = new Label
            {
                Text = "改完点「保存」，再进游戏生效。热键 F9 也能在游戏里开面板。",
                ForeColor = Color.FromArgb(110, 116, 132),
                Location = P(22, 46),
                AutoSize = true
            };
            Controls.Add(sub);

            // 作者与反馈渠道 —— 使用者遇到问题 / 想报 bug / 想要别的游戏版本时用得上
            Label contact = new Label
            {
                Text = "反馈 / 建议 / 其他游戏版本的适配需求：nesarfpersonal@163.com",
                ForeColor = Color.FromArgb(64, 110, 170),
                Location = P(22, 68),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            contact.Click += (s3, e3) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(
                        "mailto:nesarfpersonal@163.com?subject=" +
                        Uri.EscapeDataString("汉堡店修改器 反馈") +
                        "&body=" + Uri.EscapeDataString(
                            "游戏版本：" + (char)10 +
                            "问题描述：" + (char)10 +
                            "（可附上 BepInEx 目录下的 LogOutput.log 与本工具的 trainer_dpi.log）"))
                    { UseShellExecute = true });
                }
                catch { }
            };
            Controls.Add(contact);

            // 状态栏：显示当前是"可写"还是"游戏运行中，保存会被覆盖"
            _statusLabel = new Label
            {
                Text = "",
                ForeColor = Color.FromArgb(110, 116, 132),
                Location = P(22, 88),
                AutoSize = true
            };
            Controls.Add(_statusLabel);

            if (!File.Exists(_cfgPath))
            {
                Label warn = new Label
                {
                    Text = "尚未找到配置文件。先启动一次游戏（让 BepInEx 生成 cfg），再回来改。",
                    ForeColor = Color.FromArgb(200, 80, 60),
                    Location = P(22, 70),
                    Size = S(510, 40)
                };
                Controls.Add(warn);
            }

            BuildRows();
            BuildButtons();

            // 诊断：窗口真正显示后记录尺寸，便于确认高分屏行为
            Shown += (s2, e2) =>
            {
                try
                {
                    File.WriteAllText(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "trainer_dpi.log"),
                        string.Format(
                            "DeviceDpi={0}{1}ScaleFactor={2}{1}DesignedClient=560x640{1}" +
                            "ActualClient={3}x{4}{1}ActualOuter={5}x{6}{1}AutoScaleMode={7}{1}",
                            DeviceDpi, Environment.NewLine, ScaleFactor(),
                            ClientSize.Width, ClientSize.Height, Width, Height, AutoScaleMode));
                }
                catch { }
            };
        }

        // -----------------------------------------------------------------
        // DPI 缩放：设计稿按 96 DPI 写，运行时按实际 DPI 乘算
        // -----------------------------------------------------------------
        private float _scale = 1f;

        private float ScaleFactor()
        {
            if (_scale <= 0f || Math.Abs(_scale - 1f) < 0.0001f)
            {
                int dpi = DeviceDpi > 0 ? DeviceDpi : 96;
                _scale = dpi / 96f;
            }
            return _scale;
        }

        private Point P(int x, int y)
        {
            float k = ScaleFactor();
            return new Point((int)Math.Round(x * k), (int)Math.Round(y * k));
        }

        private Size S(int w, int h)
        {
            float k = ScaleFactor();
            return new Size((int)Math.Round(w * k), (int)Math.Round(h * k));
        }

        private void BuildRows()
        {
            LoadSettings();

            var panel = new Panel
            {
                Location = P(16, 78),
                Size = S(528, 470),
                AutoScroll = true,
                BackColor = Color.FromArgb(249, 249, 252)
            };
            Controls.Add(panel);

            int y = 10;
            string lastSection = null;

            foreach (string key in Order)
            {
                Setting st = _settings.Find(s => s.Key == key);
                if (st == null) continue;

                string[] meta = Meta[key];
                string section = meta[0];
                if (section != lastSection)
                {
                    Label head = new Label
                    {
                        Text = "— " + section + " —",
                        Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Bold),
                        ForeColor = Accent,
                        Location = P(10, y),
                        AutoSize = true
                    };
                    panel.Controls.Add(head);
                    y += 30;
                    lastSection = section;
                }

                Label lbl = new Label
                {
                    Text = meta[1],
                    Location = P(16, y + 2),
                    Size = S(122, 22),
                    ForeColor = Ink
                };
                panel.Controls.Add(lbl);

                if (st.IsBool)
                {
                    CheckBox cb = new CheckBox
                    {
                        Location = P(146, y),
                        Size = S(46, 24),
                        Checked = st.Value == "true" || st.Value == "1"
                    };
                    panel.Controls.Add(cb);
                    _controls[key] = cb;

                    Label note = new Label
                    {
                        Text = meta[2],
                        Location = P(198, y + 4),
                        Size = S(310, 20),
                        ForeColor = Color.FromArgb(130, 136, 150)
                    };
                    panel.Controls.Add(note);

                    y += 40;
                    continue;
                }
                else
                {
                    double cur = 0;
                    double.TryParse(st.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out cur);

                    TrackBar tb = new TrackBar
                    {
                        Minimum = 0,
                        Maximum = 1000,
                        TickStyle = TickStyle.None,
                        Location = P(146, y - 4),
                        Size = S(240, 32),
                        Value = ToBar(cur, st.Min, st.Max),
                        SmallChange = 1,
                        LargeChange = 10
                    };
                    panel.Controls.Add(tb);
                    _controls[key] = tb;

                    Label val = new Label
                    {
                        Location = P(402, y + 4),
                        Size = S(88, 20),
                        ForeColor = Ink,
                        TextAlign = ContentAlignment.MiddleRight,
                        Text = FormatValue(cur, st.IsInt)
                    };
                    panel.Controls.Add(val);
                    _valueLabels[key] = val;

                    Setting captured = st;
                    tb.ValueChanged += (s, e) =>
                    {
                        double v = FromBar(tb.Value, captured.Min, captured.Max, captured.IsInt);
                        val.Text = FormatValue(v, captured.IsInt);
                    };

                    Label hint = new Label
                    {
                        Text = meta[2],
                        Location = P(16, y + 24),
                        Size = S(478, 18),
                        ForeColor = Color.FromArgb(150, 155, 170),
                        Font = new Font("Microsoft YaHei UI", 8.2f)
                    };
                    panel.Controls.Add(hint);

                    y += 54;
                    continue;
                }
            }
        }

        private static string FormatValue(double v, bool isInt)
        {
            return isInt
                ? ((int)Math.Round(v)).ToString()
                : v.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static int ToBar(double v, double min, double max)
        {
            if (max <= min) return 0;
            double r = (v - min) / (max - min);
            return (int)Math.Round(Math.Max(0, Math.Min(1, r)) * 1000);
        }

        private static double FromBar(int bar, double min, double max, bool isInt)
        {
            double v = min + (max - min) * (bar / 1000.0);
            if (isInt) v = Math.Round(v);
            return v;
        }

        private void BuildButtons()
        {
            Button save = MakeButton("保存", 16, 560, Accent, Color.White);
            save.Click += (s, e) =>
            {
                if (SaveSettings())
                    MessageBox.Show("已保存。\n\n游戏里随时按 F9 也能打开修改面板。", "完成",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            Button launch = MakeButton("保存并启动游戏", 116, 560, Color.FromArgb(70, 78, 100), Color.White);
            launch.Size = S(140, 34);
            launch.Click += (s, e) =>
            {
                if (SaveSettings())
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = Path.Combine(_root, Program.GameExe),
                            WorkingDirectory = _root
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("启动失败：" + ex.Message, "出错");
                    }
                }
            };

            Button open = MakeButton("打开游戏目录", 272, 560, Color.FromArgb(235, 235, 242), Ink);
            open.Size = S(120, 34);
            open.Click += (s, e) => Process.Start("explorer.exe", "\"" + _root + "\"");

            Button reset = MakeButton("恢复默认", 408, 560, Color.FromArgb(235, 235, 242), Ink);
            reset.Size = S(120, 34);
            reset.Click += (s, e) =>
            {
                foreach (Setting st in _settings)
                {
                    if (st.IsBool)
                    {
                        ((CheckBox)_controls[st.Key]).Checked = false;
                    }
                    else
                    {
                        double def = DefaultOf(st.Key);
                        ((TrackBar)_controls[st.Key]).Value = ToBar(def, st.Min, st.Max);
                    }
                }
            };
        }

        private static double DefaultOf(string key)
        {
            switch (key)
            {
                case "MaxBurgerNum": return 10;
                case "TabemiPowerMul": return 10;
                case "ClockFullTime": return 180;
                case "GameSpeed": return 1;
                default: return 0;
            }
        }

        private Button MakeButton(string text, int x, int y, Color back, Color fore)
        {
            return new Button
            {
                Text = text,
                Location = P(x, y),
                Size = S(94, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = fore,
                Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold)
            };
        }

        // -----------------------------------------------------------------
        // cfg 读写
        // -----------------------------------------------------------------
        private void LoadSettings()
        {
            _settings.Clear();
            if (!File.Exists(_cfgPath)) return;

            string[] lines = File.ReadAllLines(_cfgPath, Encoding.UTF8);
            string section = "";
            string comment = "";

            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Substring(1, line.Length - 2);
                    continue;
                }
                if (line.StartsWith("#"))
                {
                    string c = line.TrimStart('#').Trim();
                    if (c.Length > 0 && !c.StartsWith("Setting type") && !c.StartsWith("Default value")
                        && !c.StartsWith("Acceptable value range") && !c.StartsWith("Settings file"))
                        comment = c;
                    if (c.StartsWith("Acceptable value range"))
                    {
                        Match m = Regex.Match(c, @"From\s+(-?[\d.]+)\s+to\s+(-?[\d.]+)");
                        if (m.Success && _settings.Count > 0)
                        {
                            Setting last = _settings[_settings.Count - 1];
                            last.Min = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                            last.Max = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                        }
                    }
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0) continue;

                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();

                bool isBool = value == "true" || value == "false";
                bool isInt = !isBool && Regex.IsMatch(value, @"^-?\d+$");

                _settings.Add(new Setting
                {
                    Section = section,
                    Key = key,
                    Value = value,
                    IsBool = isBool,
                    IsInt = isInt,
                    Comment = comment,
                    Min = isBool ? 0 : (isInt ? 0 : 0),
                    Max = isBool ? 0 : (isInt ? 100 : 1)
                });
                comment = "";
            }
        }

        // ====================================================================
        // 配置并发写保护（与插件侧的 CfgGuard 遵守同一套协议）
        //
        // 协议：<cfg>.rev 存递增修订号；<cfg>.lock 用 CreateNew 的原子性作互斥量。
        //   写前：取锁 → 核对 rev → 若对方改过，本方法【本来就是重读文件再改】，
        //         所以天然完成了合并（只替换自己管的键，对方的改动原样保留）
        //   写后：递增 rev → 释放锁
        //
        // 有了它，"游戏开着的时候用训练器改配置"就不会再被 BepInEx 静默抹掉了：
        // 插件下次落盘会发现 rev 变了，先 Reload 采纳这里的值，再重放它自己那次改动。
        // ====================================================================
        private string RevPath { get { return _cfgPath + ".rev"; } }
        private string LockPath { get { return _cfgPath + ".lock"; } }
        private long _revAtLoad = -1;

        private long ReadRev()
        {
            try
            {
                if (!File.Exists(RevPath)) return 0;
                long v;
                if (long.TryParse(File.ReadAllText(RevPath, Encoding.UTF8).Trim(),
                                  NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) return v;
            }
            catch { }
            return 0;
        }

        private void BumpRev()
        {
            try { File.WriteAllText(RevPath, (ReadRev() + 1).ToString(CultureInfo.InvariantCulture), new UTF8Encoding(false)); }
            catch { }
        }

        /// <summary>取锁：CreateNew 原子互斥 + 短暂重试；僵尸锁（超过 20 秒）自动清理。</summary>
        private FileStream AcquireLock(out string err)
        {
            err = null;
            int waited = 0;
            while (true)
            {
                try { return new FileStream(LockPath, FileMode.CreateNew, FileAccess.Write, FileShare.None); }
                catch (IOException)
                {
                    try
                    {
                        if (File.Exists(LockPath) && (DateTime.Now - File.GetLastWriteTime(LockPath)).TotalSeconds > 20)
                            File.Delete(LockPath);
                    }
                    catch { }
                    if (waited >= 2000) { err = "另一个写入者正在写（游戏内的修改器？），请稍后重试。"; return null; }
                    Thread.Sleep(40); waited += 40;
                }
                catch (Exception ex) { err = ex.Message; return null; }
            }
        }

        private void ReleaseLock(FileStream fs)
        {
            try { if (fs != null) fs.Dispose(); } catch { }
            try { if (File.Exists(LockPath)) File.Delete(LockPath); } catch { }
        }

        /// <summary>状态栏文案：当前是否处于可安全保存的状态。</summary>
        private string DescribeWriteState()
        {
            try
            {
                if (!File.Exists(_cfgPath)) return "配置文件尚未生成 —— 先启动一次游戏。";
                if (GameRunning())
                    return "游戏正在运行：保存仍然有效（已做修订号合并），但建议改完重启游戏让面板同步。";
                return "游戏未运行：可直接保存。rev " + ReadRev();
            }
            catch { return ""; }
        }

        private static bool GameRunning()
        {
            try
            {
                string name = Path.GetFileNameWithoutExtension(Program.GameExe);
                return Process.GetProcessesByName(name).Length > 0;
            }
            catch { return false; }
        }

        private bool SaveSettings()
        {
            if (!File.Exists(_cfgPath))
            {
                MessageBox.Show("还没有配置文件。\n\n先启动一次游戏，让 BepInEx 生成它，再来保存。",
                    "无法保存", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // 收集新值
            var newValues = new Dictionary<string, string>();
            foreach (Setting st in _settings)
            {
                Control c;
                if (!_controls.TryGetValue(st.Key, out c)) continue;

                if (st.IsBool)
                {
                    newValues[st.Key] = ((CheckBox)c).Checked ? "true" : "false";
                }
                else
                {
                    double v = FromBar(((TrackBar)c).Value, st.Min, st.Max, st.IsInt);
                    newValues[st.Key] = st.IsInt
                        ? ((int)v).ToString(CultureInfo.InvariantCulture)
                        : v.ToString("0.###", CultureInfo.InvariantCulture);
                }
            }

            string lockErr;
            FileStream lk = AcquireLock(out lockErr);
            if (lk == null)
            {
                MessageBox.Show(lockErr, "暂时无法保存", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            try
            {
                // 重读的是【当下】的文件内容 —— 这一步顺带完成了与插件的合并：
                // 我们只替换自己管的键，对方在这期间写进去的改动原样保留。
                string[] lines = File.ReadAllLines(_cfgPath, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    int eq = line.IndexOf('=');
                    if (eq <= 0 || line.TrimStart().StartsWith("#")) continue;

                    string key = line.Substring(0, eq).Trim();
                    string v;
                    if (newValues.TryGetValue(key, out v))
                    {
                        lines[i] = key + " = " + v;
                    }
                }
                File.WriteAllLines(_cfgPath, lines, new UTF8Encoding(false));
                BumpRev();
                if (_statusLabel != null) _statusLabel.Text = DescribeWriteState();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("写入失败：" + ex.Message, "出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally { ReleaseLock(lk); }
        }
    }
}
