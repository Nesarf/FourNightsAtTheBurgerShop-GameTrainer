using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace BurgerShopInstaller
{
    internal static class Program
    {
        internal const string AppTitle = "汉堡店 数值修改器 · 安装程序";
        internal const string Version = "1.0.2";
        internal const string GameExe = "Four Nights at the Burger Shop.exe";
        internal const string GameFolderName = "Four Nights at the Burger Shop ～ハンバーガー食べながら食べられるミニゲーム～";
        internal const string PayloadResource = "payload.zip";
        internal const string ManifestName = "burger-shop-mod.installed";

        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 静默模式（给脚本 / 自动化用）：
            //   汉堡店修改器-安装程序.exe --install   [游戏目录]
            //   汉堡店修改器-安装程序.exe --uninstall [游戏目录]
            if (args.Length >= 1 && (args[0] == "--install" || args[0] == "--uninstall"))
            {
                ConsoleHelper.Attach();
                bool uninstall = args[0] == "--uninstall";
                string root = args.Length >= 2 ? args[1] : Installer.LocateGame();
                if (root == null)
                {
                    Console.Error.WriteLine("找不到游戏目录，请显式传入。");
                    Environment.ExitCode = 2;
                    return;
                }
                try
                {
                    if (uninstall)
                    {
                        int n = Installer.Uninstall(root, null);
                        Console.WriteLine("已卸载，删除 " + n + " 个文件：" + root);
                        if (Installer.LastUninstallPending)
                            Console.WriteLine("注意：有文件被游戏占用，已登记为下次重启时自动删除。");
                    }
                    else
                    {
                        if (!File.Exists(Path.Combine(root, Program.GameExe)))
                        {
                            Console.Error.WriteLine("该目录里没有 " + Program.GameExe + "：" + root);
                            Environment.ExitCode = 2;
                            return;
                        }
                        int n = Installer.Install(root, s => Console.WriteLine("  " + s.Percent + "%  " + s.Text));
                        Console.WriteLine("安装完成，" + n + " 个文件：" + root);
                        Console.WriteLine("游戏里按 F9 打开数值面板；也可运行「数值修改器.exe」预先设置。");
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine((uninstall ? "卸载" : "安装") + "失败：" + e.Message);
                    Environment.ExitCode = 1;
                }
                return;
            }

            Application.Run(new MainForm());
        }
    }

    /// <summary>WinExe 默认没有控制台；静默模式下把自己挂到调用方的控制台上，让输出可见。</summary>
    internal static class ConsoleHelper
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int pid);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        internal static void Attach()
        {
            if (!AttachConsole(-1)) AllocConsole();
            try
            {
                var stdout = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
                Console.SetOut(stdout);
                var stderr = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
                Console.SetError(stderr);
            }
            catch { }
        }
    }

    /// <summary>
    /// 删除被占用的文件。winhttp.dll 刚被游戏加载过时，Windows 会短暂锁住它；
    /// 先重试，实在删不掉就登记为「下次重启时删除」。
    /// </summary>
    internal static class FileEraser
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        private static extern bool MoveFileEx(string existing, string newName, int flags);

        private const int MOVEFILE_DELAY_UNTIL_REBOOT = 0x4;

        /// <returns>true = 已删除；false = 已登记重启后删除</returns>
        internal static bool Delete(string path)
        {
            for (int attempt = 0; attempt < 4; attempt++)
            {
                try
                {
                    File.Delete(path);
                    return true;
                }
                catch (IOException)
                {
                    System.Threading.Thread.Sleep(400);
                }
                catch (UnauthorizedAccessException)
                {
                    System.Threading.Thread.Sleep(400);
                }
            }

            // 兜底：登记到重启后的 PendingFileRenameOperations
            try
            {
                if (MoveFileEx(path, null, MOVEFILE_DELAY_UNTIL_REBOOT)) return false;
            }
            catch { }

            throw new IOException("文件被占用且无法登记重启删除：" + path);
        }
    }

    internal sealed class Installer
    {
        internal sealed class Step
        {
            public string Text;
            public int Percent;
        }

        /// <summary>在常见位置找游戏根目录。</summary>
        internal static string LocateGame()
        {
            // 1) 本程序自己就在游戏目录里
            string self = AppDomain.CurrentDomain.BaseDirectory;
            if (File.Exists(Path.Combine(self, Program.GameExe))) return self;

            // 2) 各盘符下的标准目录名
            var drives = new List<string>();
            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
                if (d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable)
                    drives.Add(d.RootDirectory.FullName);
            }
            foreach (string root in drives)
            {
                try
                {
                    string p = Path.Combine(root, Program.GameFolderName);
                    if (File.Exists(Path.Combine(p, Program.GameExe))) return p;
                }
                catch { }
            }

            // 3) 注册表：从卸载信息里反查安装路径
            foreach (string hive in new[] { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                                            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall" })
            {
                try
                {
                    using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(hive))
                    {
                        if (key == null) continue;
                        foreach (string sub in key.GetSubKeyNames())
                        {
                            using (var k = key.OpenSubKey(sub))
                            {
                                if (k == null) continue;
                                string name = k.GetValue("DisplayName") as string;
                                if (string.IsNullOrEmpty(name)) continue;
                                if (name.IndexOf("Burger Shop", StringComparison.OrdinalIgnoreCase) < 0) continue;
                                string loc = k.GetValue("InstallLocation") as string;
                                if (!string.IsNullOrEmpty(loc) && File.Exists(Path.Combine(loc, Program.GameExe)))
                                    return loc;
                            }
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        internal static bool GameRunning()
        {
            string want = Path.GetFileNameWithoutExtension(Program.GameExe);
            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    if (p.ProcessName.IndexOf("Burger", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                    if (string.Equals(p.ProcessName, want, StringComparison.OrdinalIgnoreCase)) return true;
                }
                catch { }
                finally { p.Dispose(); }
            }
            return false;
        }

        /// <summary>读安装清单：相对路径 → sha256（空串表示安装前已存在）。</summary>
        internal static Dictionary<string, string> ReadManifest(string root)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string f = Path.Combine(root, Program.ManifestName);
            if (!File.Exists(f)) return map;
            foreach (string line in File.ReadAllLines(f, Encoding.UTF8))
            {
                if (line.Length == 0 || line[0] == '#') continue;
                string[] parts = line.Split('\t');
                if (parts.Length >= 2) map[parts[0]] = parts[1];
            }
            return map;
        }

        internal static string Sha256(string path)
        {
            using (var sha = SHA256.Create())
            using (var s = File.OpenRead(path))
            {
                byte[] h = sha.ComputeHash(s);
                var sb = new StringBuilder(h.Length * 2);
                foreach (byte b in h) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>安装（或重装）。返回写入的文件数。</summary>
        internal static int Install(string root, Action<Step> report)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            using (Stream raw = asm.GetManifestResourceStream(Program.PayloadResource))
            {
                if (raw == null) throw new InvalidOperationException("安装包内缺少载荷资源，文件可能已损坏。");

                var installed = new List<string>();
                using (var zip = new ZipArchive(raw, ZipArchiveMode.Read))
                {
                    int total = zip.Entries.Count, done = 0;
                    foreach (ZipArchiveEntry e in zip.Entries)
                    {
                        done++;
                        report?.Invoke(new Step { Text = "解出 " + e.FullName, Percent = 5 + done * 70 / Math.Max(1, total) });

                        string rel = e.FullName.Replace('/', Path.DirectorySeparatorChar);
                        string dest = Path.Combine(root, rel);

                        // 【目录条目要单独处理】
                        // 载荷 zip 里有 BepInEx/config/ 这样的目录条目，FullName 以 / 结尾。
                        // 对它们调 File.Create 会抛 DirectoryNotFoundException
                        // （"未能找到路径的一部分"）—— 因为路径以分隔符结尾。
                        if (e.FullName.EndsWith("/") || e.FullName.EndsWith("\\"))
                        {
                            if (!string.IsNullOrEmpty(dest)) Directory.CreateDirectory(dest);
                            continue;
                        }

                        string dir = Path.GetDirectoryName(dest);
                        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                        bool existedBefore = File.Exists(dest);
                        using (Stream src = e.Open())
                        using (var outFile = File.Create(dest))
                        {
                            src.CopyTo(outFile);
                        }

                        // 记录 hash；安装前就存在的文件记为空串 —— 卸载时不动它们
                        installed.Add(rel + "\t" + (existedBefore ? "" : Sha256(dest)));
                    }
                }

                report?.Invoke(new Step { Text = "写入安装清单", Percent = 80 });

                string manifest = Path.Combine(root, Program.ManifestName);
                var sb = new StringBuilder();
                sb.AppendLine("# 汉堡店数值修改器 安装清单 —— 卸载时按此表精确还原");
                sb.AppendLine("# 格式：相对路径 <TAB> sha256（空 = 安装前已存在，卸载不删）");
                sb.AppendLine("# 安装时间 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine("# 版本 " + Program.Version);
                foreach (string line in installed) sb.AppendLine(line);
                File.WriteAllText(manifest, sb.ToString(), new UTF8Encoding(false));

                report?.Invoke(new Step { Text = "完成", Percent = 100 });
                return installed.Count;
            }
        }

        /// <summary>卸载：只删清单里属于本次安装的文件，并清理空目录。</summary>
        internal static int Uninstall(string root, Action<Step> report)
        {
            var manifest = ReadManifest(root);
            if (manifest.Count == 0)
                throw new InvalidOperationException("这个目录里没有安装记录（找不到 " + Program.ManifestName + "）。");

            int removed = 0, skipped = 0, pending = 0;
            var errors = new List<string>();
            var dirs = new List<string>();

            // 先处理文件；顺序无所谓，但目录要最后清
            foreach (var kv in manifest)
            {
                string full = Path.Combine(root, kv.Key);
                string dir = Path.GetDirectoryName(full);
                if (!string.IsNullOrEmpty(dir)) dirs.Add(dir);

                if (!File.Exists(full)) continue;

                // 安装前已存在的文件（hash 为空）一律不删
                if (string.IsNullOrEmpty(kv.Value))
                {
                    skipped++;
                    report?.Invoke(new Step { Text = "保留原有文件 " + kv.Key, Percent = 50 * removed / Math.Max(1, manifest.Count) });
                    continue;
                }
                try
                {
                    if (FileEraser.Delete(full)) removed++;
                    else pending++;
                }
                catch (Exception ex)
                {
                    errors.Add(kv.Key + "：" + ex.Message);
                }
                report?.Invoke(new Step { Text = "删除 " + kv.Key, Percent = 50 + 40 * (removed + pending) / Math.Max(1, manifest.Count) });
            }

            // 自底向上删空目录
            dirs.Sort((a, b) => b.Length.CompareTo(a.Length));
            foreach (string d in dirs)
            {
                try
                {
                    if (Directory.Exists(d) && Directory.GetFileSystemEntries(d).Length == 0)
                        Directory.Delete(d);
                }
                catch { }
            }

            // 兜底：清掉 BepInEx 自己生成的运行期文件/目录（它们不在载荷里，不会被清单覆盖）。
            // 但如果这个目录里还有别人装的 BepInEx 内容（有 .dll），就整体跳过，别破坏用户的 mod 环境。
            string bepRoot = Path.Combine(root, "BepInEx");
            if (Directory.Exists(bepRoot))
            {
                bool foreign = false;
                try
                {
                    foreach (string f in Directory.GetFiles(bepRoot, "*.dll", SearchOption.AllDirectories))
                    {
                        foreign = true;
                        break;
                    }
                }
                catch { foreign = true; }

                if (!foreign)
                {
                    foreach (string rel in new[]
                    {
                        @"BepInEx\cache", @"BepInEx\l2d_dump", @"BepInEx\config", @"BepInEx\patchers", @"BepInEx\plugins"
                    })
                    {
                        try
                        {
                            string p = Path.Combine(root, rel);
                            if (Directory.Exists(p)) Directory.Delete(p, true);
                        }
                        catch { }
                    }
                    foreach (string rel in new[]
                    {
                        @"BepInEx\LogOutput.log", @"BepInEx\output_log.txt",
                        @"BepInEx\config\BepInEx.cfg", @"BepInEx\config\nesarf.burgershop.modder.cfg"
                    })
                    {
                        try
                        {
                            string p = Path.Combine(root, rel);
                            if (File.Exists(p)) FileEraser.Delete(p);
                        }
                        catch { }
                    }
                }
            }
            else
            {
                // 老路径：万一没有 BepInEx 根目录也顺手清一下散落文件
                foreach (string rel in new[] { @"BepInEx\LogOutput.log", @"BepInEx\output_log.txt" })
                {
                    string p = Path.Combine(root, rel);
                    try { if (File.Exists(p)) FileEraser.Delete(p); } catch { }
                }
            }

            // 清单本身最后删；若删不掉（被占用）就留到重启
            string selfManifest = Path.Combine(root, Program.ManifestName);
            if (File.Exists(selfManifest))
            {
                try
                {
                    if (!FileEraser.Delete(selfManifest)) pending++;
                }
                catch (Exception ex) { errors.Add(Program.ManifestName + "：" + ex.Message); }
            }

            // 清单删掉后再试一次清空目录
            if (pending == 0)
            {
                foreach (string d in dirs)
                {
                    try
                    {
                        if (Directory.Exists(d) && Directory.GetFileSystemEntries(d).Length == 0)
                            Directory.Delete(d);
                    }
                    catch { }
                }

                // 自底向上递归清掉空目录（含 BepInEx 这种运行期才建出来的空壳）
                RemoveEmptyDirs(Path.Combine(root, "BepInEx"));
            }

            string summary = "完成：删除 " + removed + " 个文件"
                           + (skipped > 0 ? "，保留 " + skipped + " 个原有文件" : "")
                           + (pending > 0 ? "，" + pending + " 个被占用、已登记重启后删除" : "")
                           + (errors.Count > 0 ? "，失败 " + errors.Count + " 个" : "");
            foreach (string err in errors) report?.Invoke(new Step { Text = "× " + err, Percent = 100 });

            report?.Invoke(new Step { Text = summary, Percent = 100 });

            if (errors.Count > 0)
            {
                var ex = new InvalidOperationException(
                    "有 " + errors.Count + " 个文件没能删除（多为游戏仍在运行占用）：\n" + string.Join("\n", errors.ToArray()));
                ex.Data["pending"] = pending;
                throw ex;
            }

            LastUninstallPending = (pending > 0);
            return removed;
        }

        /// <summary>上次卸载是否有文件被登记为重启后删除。</summary>
        internal static bool LastUninstallPending;

        /// <summary>自底向上删除空目录（只删空的，不会碰任何有内容的目录）。</summary>
        private static void RemoveEmptyDirs(string path)
        {
            try
            {
                if (!Directory.Exists(path)) return;
                foreach (string sub in Directory.GetDirectories(path))
                    RemoveEmptyDirs(sub);
                if (Directory.GetFileSystemEntries(path).Length == 0)
                    Directory.Delete(path);
            }
            catch { }
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly TextBox _path = new TextBox();
        private readonly TextBox _log = new TextBox();
        private readonly Button _install = new Button();
        private readonly Button _uninstall = new Button();
        private readonly Button _browse = new Button();
        private readonly Label _status = new Label();
        private readonly ProgressBar _bar = new ProgressBar();
        private readonly CheckBox _launch = new CheckBox();
        private bool _busy;

        private static readonly Color Ink = Color.FromArgb(28, 32, 44);
        private static readonly Color Accent = Color.FromArgb(158, 128, 255);

        public MainForm()
        {
            Text = Program.AppTitle;
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(940, 540);   // 右侧留出立绘的位置
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            Font = new Font("Microsoft YaHei UI", 9.5f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            LoadArtIcon();
            BuildUi();
            Detect();
        }

        /// <summary>从内嵌资源设置窗口图标。</summary>
        private void LoadArtIcon()
        {
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using (var st = asm.GetManifestResourceStream("art.ico"))
                {
                    if (st != null) Icon = new Icon(st);
                }
            }
            catch { }
        }

        private void BuildUi()
        {
            // 立绘（内嵌资源，保持单文件分发）
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                using (var st = asm.GetManifestResourceStream("art.png"))
                {
                    if (st != null)
                    {
                        var img = Image.FromStream(st);
                        var art = new PictureBox
                        {
                            Image = img,
                            Location = new Point(660, 50),
                            Size = new Size(img.Width, img.Height),
                            SizeMode = PictureBoxSizeMode.Zoom,
                            BackColor = Color.Transparent
                        };
                        Controls.Add(art);

                        var sep = new Panel
                        {
                            Location = new Point(624, 20),
                            Size = new Size(1, 500),
                            BackColor = Color.FromArgb(226, 228, 236)
                        };
                        Controls.Add(sep);
                    }
                }
            }
            catch { }

            var title = new Label
            {
                Text = "数值修改器 · 安装程序",
                Font = new Font("Microsoft YaHei UI", 15f, FontStyle.Bold),
                ForeColor = Ink,
                Location = new Point(20, 14),
                AutoSize = true
            };
            Controls.Add(title);

            var sub = new Label
            {
                Text = "安装 BepInEx 与修改器插件；游戏本体不会被改动。版本 " + Program.Version,
                ForeColor = Color.FromArgb(110, 116, 132),
                Location = new Point(22, 46),
                AutoSize = true
            };
            Controls.Add(sub);

            var dirLabel = new Label
            {
                Text = "游戏目录",
                ForeColor = Ink,
                Location = new Point(22, 82),
                AutoSize = true
            };
            Controls.Add(dirLabel);

            _path.Location = new Point(22, 102);
            _path.Size = new Size(470, 26);
            Controls.Add(_path);

            _browse.Text = "浏览…";
            _browse.Location = new Point(500, 101);
            _browse.Size = new Size(96, 28);
            _browse.FlatStyle = FlatStyle.Flat;
            _browse.BackColor = Color.FromArgb(235, 235, 242);
            _browse.Click += (s, e) => Browse();
            Controls.Add(_browse);

            _status.Location = new Point(22, 140);
            _status.Size = new Size(574, 22);
            _status.ForeColor = Color.FromArgb(90, 96, 112);
            Controls.Add(_status);

            _bar.Location = new Point(22, 164);
            _bar.Size = new Size(574, 8);
            _bar.Style = ProgressBarStyle.Continuous;
            Controls.Add(_bar);

            _log.Location = new Point(22, 184);
            _log.Size = new Size(574, 240);
            _log.Multiline = true;
            _log.ReadOnly = true;
            _log.ScrollBars = ScrollBars.Vertical;
            _log.BackColor = Color.FromArgb(249, 249, 252);
            _log.BorderStyle = BorderStyle.FixedSingle;
            _log.Font = new Font("Consolas", 8.5f);
            Controls.Add(_log);

            _launch.Text = "  装完启动游戏";
            _launch.Location = new Point(22, 434);
            _launch.Size = new Size(200, 24);
            _launch.ForeColor = Ink;
            Controls.Add(_launch);

            _install.Text = "安装";
            _install.Location = new Point(300, 430);
            _install.Size = new Size(140, 36);
            _install.FlatStyle = FlatStyle.Flat;
            _install.BackColor = Accent;
            _install.ForeColor = Color.White;
            _install.Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Bold);
            _install.Click += (s, e) => DoInstall();
            Controls.Add(_install);

            _uninstall.Text = "卸载还原";
            _uninstall.Location = new Point(450, 430);
            _uninstall.Size = new Size(146, 36);
            _uninstall.FlatStyle = FlatStyle.Flat;
            _uninstall.BackColor = Color.FromArgb(235, 235, 242);
            _uninstall.ForeColor = Ink;
            _uninstall.Click += (s, e) => DoUninstall();
            Controls.Add(_uninstall);
        }

        private void Detect()
        {
            string found = Installer.LocateGame();
            if (found != null)
            {
                _path.Text = found;
                _status.Text = "已自动找到游戏目录。";
                _status.ForeColor = Color.FromArgb(60, 130, 90);
                Log("找到游戏：" + found);
            }
            else
            {
                _status.Text = "没有自动找到游戏目录，请点「浏览…」手动选择。";
                _status.ForeColor = Color.FromArgb(200, 120, 40);
                Log("未自动定位到游戏目录。");
            }
            RefreshButtons();
        }

        private void RefreshButtons()
        {
            bool hasManifest = Installer.ReadManifest(_path.Text.Trim()).Count > 0;
            _uninstall.Enabled = !_busy && hasManifest && Directory.Exists(_path.Text.Trim());
        }

        private void Browse()
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "选择游戏根目录（里面要有 " + Program.GameExe + "）";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _path.Text = dlg.SelectedPath;
                    ValidateSelection();
                }
            }
        }

        private bool ValidateSelection()
        {
            string root = _path.Text.Trim();
            if (!Directory.Exists(root))
            {
                Fail("目录不存在：" + root);
                return false;
            }
            if (!File.Exists(Path.Combine(root, Program.GameExe)))
            {
                Fail("这个目录里没有 " + Program.GameExe + "，请选择正确的游戏根目录。");
                return false;
            }
            if (Installer.GameRunning())
            {
                Fail("游戏正在运行，请先关闭游戏再安装（会占用 winhttp.dll）。");
                return false;
            }
            return true;
        }

        private void Fail(string msg)
        {
            _status.Text = msg;
            _status.ForeColor = Color.FromArgb(205, 70, 60);
            Log("× " + msg);
            RefreshButtons();
        }

        private void Log(string line)
        {
            _log.AppendText(line + Environment.NewLine);
            TryTrace(line);
        }

        /// <summary>把过程同时写一份到 exe 同级的 installer.log，便于事后核验与排错。</summary>
        private static void TryTrace(string line)
        {
            try
            {
                string p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "installer.log");
                File.AppendAllText(p, DateTime.Now.ToString("HH:mm:ss") + "  " + line + Environment.NewLine,
                    new UTF8Encoding(false));
            }
            catch { }
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            _install.Enabled = !busy;
            _browse.Enabled = !busy;
            _path.Enabled = !busy;
            _uninstall.Enabled = !busy && Installer.ReadManifest(_path.Text.Trim()).Count > 0;
        }

        private void DoInstall()
        {
            if (!ValidateSelection()) return;
            string root = _path.Text.Trim();

            if (Installer.ReadManifest(root).Count > 0)
            {
                if (MessageBox.Show("检测到这个目录里已经装过。\n继续会覆盖安装（设置文件也会被重置为默认）。\n\n要继续吗？",
                        Program.AppTitle, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
                    return;
            }

            SetBusy(true);
            _bar.Value = 0;
            _log.Clear();
            TryTrace("=== 开始安装: " + root);

            var worker = new BackgroundWorker();
            // 【必须显式声明】BackgroundWorker 默认 WorkerReportsProgress = false，
            // 此时调 ReportProgress() 会抛 InvalidOperationException，
            // 用户看到的就是「此 BackgroundWorker 声明它不报告进度」。
            worker.WorkerReportsProgress = true;
            worker.DoWork += (s, e) =>
            {
                Installer.Install(root, step => worker.ReportProgress(step.Percent, step.Text));
            };
            worker.ProgressChanged += (s, e) =>
            {
                string t = e.UserState as string;
                if (t != null) Log("· " + t);
                _bar.Value = Math.Max(0, Math.Min(100, e.ProgressPercentage));
            };
            worker.RunWorkerCompleted += (s, e) =>
            {
                SetBusy(false);
                if (e.Error != null)
                {
                    Fail("安装失败：" + e.Error.Message);
                    return;
                }
                _bar.Value = 100;
                Log("");
                Log("(自然) 安装完成。");
                Log("  · 游戏里按 F9 打开数值面板");
                Log("  · 也可以运行「数值修改器.exe」在进游戏前改");
                Log("  · 想还原就点「卸载还原」或运行「卸载还原.ps1」");
                _status.Text = "安装完成。";
                _status.ForeColor = Color.FromArgb(60, 130, 90);
                RefreshButtons();

                if (_launch.Checked)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = Path.Combine(root, Program.GameExe),
                            WorkingDirectory = root
                        });
                    }
                    catch (Exception ex) { Log("× 启动游戏失败：" + ex.Message); }
                }
            };
            worker.RunWorkerAsync();
        }

        private void DoUninstall()
        {
            string root = _path.Text.Trim();
            if (!Directory.Exists(root)) { Fail("目录不存在。"); return; }
            if (Installer.GameRunning()) { Fail("游戏正在运行，请先关闭游戏。"); return; }

            if (MessageBox.Show("将从游戏目录删除 BepInEx 与修改器，游戏回到原样。\n\n（安装前就存在的文件会保留）\n\n确定卸载？",
                    Program.AppTitle, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
                return;

            SetBusy(true);
            _log.Clear();
            _bar.Value = 0;
            TryTrace("=== 开始卸载: " + root);

            var worker = new BackgroundWorker();
            // 同上：不声明就会在第一次 ReportProgress 时抛异常
            worker.WorkerReportsProgress = true;
            worker.DoWork += (s, e) =>
            {
                Installer.Uninstall(root, step => worker.ReportProgress(step.Percent, step.Text));
            };
            worker.ProgressChanged += (s, e) =>
            {
                string t = e.UserState as string;
                if (t != null) Log("· " + t);
                _bar.Value = Math.Max(0, Math.Min(100, e.ProgressPercentage));
            };
            worker.RunWorkerCompleted += (s, e) =>
            {
                SetBusy(false);
                if (e.Error != null) { Fail("卸载失败：" + e.Error.Message); return; }
                _bar.Value = 100;
                Log("");
                Log("(自然) 已还原。游戏现在是原版状态。");
                _status.Text = "已卸载还原。";
                _status.ForeColor = Color.FromArgb(60, 130, 90);
                RefreshButtons();
            };
            worker.RunWorkerAsync();
        }
    }
}
