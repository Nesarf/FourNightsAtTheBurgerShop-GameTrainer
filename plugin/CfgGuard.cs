// ============================================================================
// 配置文件的并发写保护 —— 修订号 + 锁 + 合并
//
// 背景：插件的 cfg 有两个写入者
//   · 游戏内的 BepInEx：SaveOnConfigSet 默认 true，每次改一个 ConfigEntry.Value
//     就会用【内存里的全部值】重写整个文件
//   · 训练器 数值修改器.exe：逐行原地编辑，只换 key = value 那一行
// 二者并发时会【静默互相覆盖】：训练器写完之后，游戏里随便动一下滑块，
// BepInEx 就会用内存里的旧值把训练器的修改整片抹掉，且没有任何提示。
//
// 方案（C）：让两个写入者都遵守同一套规矩
//   1. 修订号 sidecar：<cfg>.rev，内容是递增整数
//   2. 锁文件：<cfg>.lock，独占创建作为互斥量
//   3. 写之前：取锁 → 读 rev → 与自己读到时的 rev 比对
//        · 一致   → 直接写
//        · 不一致 → 对方改过：先【重读】把对方的值吃进来，再把本次改动重放上去
//   4. 写完：递增 rev → 释放锁
//   5. 取不到锁：明确报告"另一个写入者正在写"，而不是静默丢数据
//
// 这套东西刻意做得很朴素 —— 没有平台特定的文件锁 API，只用
// FileMode.CreateNew 的原子性 + 短暂重试，跨 .NET Framework / .NET 都能跑。
// ============================================================================
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace BurgerShopModder
{
    internal static class CfgGuard
    {
        private static long _revAtRead = -1;
        private static readonly object _gate = new object();

        internal static string RevPath(string cfg) { return cfg + ".rev"; }
        internal static string LockPath(string cfg) { return cfg + ".lock"; }

        /// <summary>读修订号。文件不存在视为 0。</summary>
        internal static long ReadRev(string cfg)
        {
            try
            {
                string p = RevPath(cfg);
                if (!File.Exists(p)) return 0;
                long v;
                if (long.TryParse(File.ReadAllText(p, Encoding.UTF8).Trim(),
                                  NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                    return v;
            }
            catch { }
            return 0;
        }

        /// <summary>记住"我读到的是哪一版"，写之前用来判断有没有被人改过。</summary>
        internal static void NoteRead(string cfg) { _revAtRead = ReadRev(cfg); }

        /// <summary>只有两个写入者都遵守它才有意义，所以这里不做任何"自动纠正"。</summary>
        internal sealed class Lock : IDisposable
        {
            private string _path;
            private FileStream _fs;
            internal Lock(string path, FileStream fs) { _path = path; _fs = fs; }
            public void Dispose()
            {
                try { if (_fs != null) { _fs.Dispose(); _fs = null; } } catch { }
                try { if (File.Exists(_path)) File.Delete(_path); } catch { }
            }
        }

        /// <summary>
        /// 取锁。用 CreateNew 的原子性做互斥，失败就短暂重试。
        /// 超过 timeoutMs 仍拿不到 → 返回 null（调用方应明确报告，而不是硬写）。
        /// </summary>
        internal static Lock Acquire(string cfg, int timeoutMs = 2000)
        {
            string p = LockPath(cfg);
            int waited = 0;
            while (true)
            {
                try
                {
                    FileStream fs = new FileStream(p, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    byte[] who = Encoding.UTF8.GetBytes(
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                    fs.Write(who, 0, who.Length);
                    fs.Flush();
                    return new Lock(p, fs);
                }
                catch (IOException)
                {
                    // 已被别人持有；也可能是上个进程崩溃留下的僵尸锁 —— 用时间兜底
                    try
                    {
                        if (File.Exists(p))
                        {
                            TimeSpan age = DateTime.Now - File.GetLastWriteTime(p);
                            if (age.TotalSeconds > 20) { File.Delete(p); continue; }
                        }
                    }
                    catch { }
                    if (waited >= timeoutMs) return null;
                    Thread.Sleep(40);
                    waited += 40;
                }
                catch { return null; }
            }
        }

        /// <summary>写完递增修订号。</summary>
        internal static void Bump(string cfg)
        {
            try
            {
                long v = ReadRev(cfg) + 1;
                File.WriteAllText(RevPath(cfg), v.ToString(CultureInfo.InvariantCulture),
                                  new UTF8Encoding(false));
                _revAtRead = v;
            }
            catch { }
        }

        /// <summary>
        /// 准备写：取锁并判断是否被外部改过。
        /// 返回 (锁, 是否被改过)；锁为 null 表示没拿到（调用方应放弃写入并报告）。
        /// </summary>
        internal static Lock PrepareWrite(string cfg, out bool changedExternally)
        {
            changedExternally = false;
            Lock lk = Acquire(cfg);
            if (lk == null) return null;
            long now = ReadRev(cfg);
            if (_revAtRead >= 0 && now != _revAtRead) changedExternally = true;
            return lk;
        }
    }
}
