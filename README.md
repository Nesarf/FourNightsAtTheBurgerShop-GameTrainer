# Four Nights at the Burger Shop · 数值修改器

给《Four Nights at the Burger Shop ～ハンバーガー食べながら食べられるミニゲーム～》做的
**BepInEx 插件 + 配套修改器**。游戏本体不会被改动。

> **本仓库不包含游戏的任何素材或程序集。**
> 编译需要你**自己从游戏安装目录**取引用 DLL（见下方「编译」）。
> 这是有意为之 —— 游戏资源受版权保护，不应该被再分发。

---

## 运行环境

### 需要什么

| 需要 | 说明 |
| --- | --- |
| **Windows** | 游戏本身是 Windows 的（Unity 2022.1 / Mono），修改器跟着 |
| **.NET Framework 4.7.2** | 三个程序都编译到这个版本 |
| **游戏本体** | 已安装、能正常启动 |

**.NET Framework 4.7.2 一般不用自己装。**

- Windows 10 1809 及以后、Windows 11 —— **系统自带**（自带的是 4.7.2 或更高）
- Windows 7 SP1 / 8.1 —— 需要单独装一次：
  [.NET Framework 4.7.2 离线安装包](https://dotnet.microsoft.com/download/dotnet-framework/net472)

### 不需要什么

- **不需要装 BepInEx** —— 安装程序自带（BepInEx 5.4.23.5，已经打进 exe 里）
- **不需要装 .NET SDK / 运行时 / Visual C++ 运行库**
- **不需要管理员权限**（装在自己的游戏目录里）
- **不需要联网**（安装程序里的 BepInEx 是内嵌的，不会去下载）

只有**手动安装**（自己把 `BurgerShopModder.dll` 放进 `BepInEx/plugins/`）才需要先有 BepInEx。

### 三个程序分别要什么

| 程序 | 要什么 |
| --- | --- |
| `汉堡店修改器-安装程序.exe` | 只要能跑 .NET Framework 4.7.2 的 Windows |
| `数值修改器.exe` | 同上（它和游戏完全独立，可以单独开来改配置） |
| `BurgerShopModder.dll` | 游戏 + BepInEx 5.x |

---

## 里面有什么

| 目录 | 内容 |
| --- | --- |
| `plugin/` | BepInEx 插件源码（Harmony 补丁、游戏内命令通道、配置守卫、活动记录器） |
| `trainer/` | 配套修改器 `数值修改器.exe` 的源码（在游戏外改配置） |
| `installer/` | 安装程序源码（把 BepInEx 与插件装进游戏目录，单文件分发） |
| `tools/` | 调试用脚本（驱动游戏、快照配置、逻辑交叉检查） |
| `dist/` | 编译好的成品 |
| `使用说明.md` | **完整说明书**（功能、参数、排查） |
| `KNOWN-ISSUES.md` | **当前未解决的问题**（只放还没修的） |
| `ENGINEERING-NOTES.md` | **工程笔记**：每个坑的症状 / 根因 / 修法 / 教训（40 条历史记录） |
| `CHANGELOG.md` | 版本级变更 |

## 直接下载用（不编译）

`dist/` 里就是成品：

- `汉堡店修改器-安装程序.exe` —— 双击，选游戏目录，装完即可
- `数值修改器.exe` —— 在游戏外编辑配置
- `BurgerShopModder.dll` —— 插件本体（手动安装时放进 `BepInEx/plugins/`）

---

## 编译

**前置**：.NET SDK（`net472` 目标，Windows）、BepInEx 5.4.x。

### 1. 准备引用

插件需要游戏的程序集才能编译，但**本仓库不含它们**。请：

1. 在仓库根目录建 `refs/` 目录
2. 从**你的**游戏安装目录复制这些（通常在 `<游戏目录>/<游戏>_Data/Managed/`）：

```
UnityEngine.dll
UnityEngine.CoreModule.dll
UnityEngine.IMGUIModule.dll
UnityEngine.InputLegacyModule.dll
UnityEngine.TextRenderingModule.dll
UnityEngine.ImageConversionModule.dll
UnityEngine.PhysicsModule.dll
UnityEngine.UI.dll
UnityEngine.UIModule.dll
UnityEngine.TextMeshPro.dll        （如有）
```

3. 从 BepInEx 发行包复制：

```
BepInEx.dll
0Harmony.dll
```

### 2. 编译

```bash
dotnet build plugin/BurgerShopModder.csproj -c Release
dotnet build trainer/BurgerShopTrainer.csproj -c Release
```

产物落在 `dist/`。

### 3. 安装程序（可选）

```bash
powershell -ExecutionPolicy Bypass -File installer/make_payload.ps1
dotnet build installer/Installer.csproj -c Release
```

`make_payload.ps1` 会把 `dist/` 的成品与 BepInEx 打包成 `installer/payload.zip`，
再内嵌进安装程序 exe。

---

## 功能概览

面板按**你正在跟角色的哪个姿势互动**分六栏：

```
玩家 / 口交 / 背榨 / 正骑 / 换装 / 系统
```

同一套机制在三栏各叫各的名字 —— **口交「吸取」、背榨「索取」、正骑「榨取」**，
而且**参数三份独立**（`_Fella` / `_Osiri` / `_Sit`），改一栏不影响另外两栏。

详细说明见 **[`使用说明.md`](使用说明.md)**。

---

## 调试用命令通道

插件提供一个**文件式命令通道**，可以用脚本驱动游戏（不碰键鼠），
写 `BepInEx/l2d_dump/cmd/in.txt` 即可：

```bash
bash tools/gcmd.sh states          # 状态机快照
bash tools/gcmd.sh manman          # HitArea_Manman 诊断
bash tools/gcmd.sh xcheck          # 逻辑交叉检查（23 条不变量）
bash tools/gcmd.sh demand status   # 索取/榨取模式状态
```

脚本需要 `tools/gamepath.txt` 指向你的游戏目录（**该文件不入库**）：

```
<游戏目录>
```

---

## 文档

| 想找什么 | 去哪 |
| --- | --- |
| 怎么用、每个参数是什么 | [`使用说明.md`](使用说明.md) |
| **现在还有什么没修** | [`KNOWN-ISSUES.md`](KNOWN-ISSUES.md) |
| **某个功能为什么长这样**（逆向发现、踩过的坑） | [`ENGINEERING-NOTES.md`](ENGINEERING-NOTES.md) |
| 每个版本改了什么 | [`CHANGELOG.md`](CHANGELOG.md) |

`ENGINEERING-NOTES.md` 里有 40 条历史记录，每条都写了**症状 / 根因 / 修法 / 教训**。踩过的坑包括：

- Harmony 挂错类会**静默失败**（同一个方法名可能属于不同的类）
- 游戏的动画有**两种播法**（混合器 `*.State` 与层上 `Play` 的 `*.CurrentState`），调速逻辑两种都要覆盖
- **鼠标与触摸走两条完全不同的路**（静态字段 vs 方法返回值）
- 三份参数下，`set` 命令落在哪个物理键上**取决于当时的姿势**

---

## 常见问题

### 装的时候报「此 BackgroundWorker 声明它不报告进度」

**这是 v1.0.0 和 v1.0.1 的 bug，v1.0.2 已修。**

```
此 BackgroundWorker 声明它不报告进度。
请修改 WorkerReportsProgress 以声明它报告进度。
```

请到 [Releases](https://github.com/Nesarf/FourNightsAtTheBurgerShop-GameTrainer/releases)
下最新的 `BurgerShopModder-Installer.exe`。

**不用先卸载**，直接下新的重装即可。

（这条提醒留着，因为可能还有人手里是旧版。）

### 装完进游戏按 F9 没反应

按顺序查：

1. **装对目录了吗** —— 游戏目录里应该能看到 `BepInEx` 文件夹和 `winhttp.dll`。
   如果装到别的地方去了，用安装程序的「卸载还原」清掉再重装。
2. **游戏是不是从别的入口启动的** —— 有些启动器会跳过 `winhttp.dll` 的注入。
   直接双击游戏目录里的 `Four Nights at the Burger Shop.exe` 试试。
3. **看日志** —— 游戏目录下 `BepInEx/LogOutput.log`。
   正常加载会有 `数值修改器已加载 — F9 开关面板` 这一行。

### 想恢复成原版

用安装程序的「卸载还原」按钮，或者运行游戏目录里的 `卸载还原.ps1`。

**它是按安装清单精确删除的**：只删这次装进去的文件，
不会碰你自己原有的 BepInEx，也不会碰你的存档和配置。

### 配置文件在哪

```
<游戏目录>/BepInEx/config/nesarf.burgershop.modder.cfg
```

从旧版本升级时**会自动迁移**（旧格式的值会搬到新格式，并自动备份）。

## 许可

代码以 MIT 许可发布（见 `LICENSE`）。

**游戏本体、其素材与程序集的一切权利归原作者所有。**
本项目只包含自己编写的代码，不包含也不分发任何游戏内容。
