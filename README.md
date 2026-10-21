> ## 警告：本仓库面向成人内容
>
> **这是一个 18+ 仓库。**
>
> 它针对的游戏《Four Nights at the Burger Shop》是一款成人向作品，
> 说明书与变更记录里会提到该游戏的成人内容（但不含任何图片、模型或程序集）。
>
> **未满 18 岁请不要继续。** 如果你对成人内容不适，请离开此页面。
>
> 本仓库**不包含**游戏的任何素材 —— 没有贴图、没有模型、没有程序集。
> 只有修改器的源码、说明书和编译产物。

---

# 汉堡店的四个夜晚（Four Nights at the Burger Shop）· 数值修改器

给《Four Nights at the Burger Shop ～ハンバーガー食べながら食べられるミニゲーム～》做的
**BepInEx 插件 + 配套修改器**。游戏本体不会被改动。

> **这个游戏在中文社区也写作「汉堡店的四个夜晚」。**
> 如果你搜的是「汉堡店的四个夜晚修改器」「Four Nights at the Burger Shop 修改器」
> 「汉堡店四个夜晚 数值修改器」「burger shop 修改器」—— 找的就是这里。
> 日文原名是《Four Nights at the Burger Shop ～ハンバーガー食べながら食べられるミニゲーム～》。

> **本仓库不包含游戏的任何素材或程序集。**
> 编译需要你**自己从游戏安装目录**取引用 DLL（见下方「编译」）。
> 这是有意为之 —— 游戏资源受版权保护，不应该被再分发。

---

## 运行环境

完整的依赖清单。**列出来的每一项都写了"谁来提供"** ——
标"系统自带"或"安装程序自带"的，你不需要做任何事。

### 一、修改器（三个程序）

| 依赖 | 谁来提供 | 通常是否已具备 |
| --- | --- | --- |
| Windows（x64 或 x86） | 你的机器 | 必需 |
| **.NET Framework 4.7.2** | 系统 | Win10 1809+ / Win11 自带；Win7 SP1 / 8.1 要装 |
| Windows Forms | .NET 的一部分 | 装好 .NET 就有 |
| `System.IO.Compression` | .NET 的一部分 | 同上（安装程序解压用） |

**.NET Framework 4.7.2 下载**（只有 Win7 / 8.1 需要）：
[离线安装包](https://dotnet.microsoft.com/download/dotnet-framework/net472)

### 二、插件（`BurgerShopModder.dll`）

| 依赖 | 谁来提供 | 通常是否已具备 |
| --- | --- | --- |
| BepInEx 5.4.23.5 | **安装程序自带** | 不用管 |
| ├ `BepInEx/core/BepInEx.dll` 等 | 安装程序自带 | 不用管 |
| ├ `0Harmony.dll`（HarmonyX） | 安装程序自带 | 不用管 |
| ├ `Mono.Cecil.dll` | 安装程序自带 | 不用管 |
| ├ `MonoMod.*.dll` | 安装程序自带 | 不用管 |
| ├ `winhttp.dll`（Doorstop 注入器） | 安装程序自带 | 不用管 |
| └ `doorstop_config.ini` | 安装程序自带 | 不用管 |
| Unity 2022.1 的 Mono 运行时 | **游戏自带** `MonoBleedingEdge` | 不用管 |
| 游戏程序集（`Assembly-CSharp.dll` 等） | 游戏自带 | 不用管 |

> **只有手动安装**（自己把 `.dll` 放进 `BepInEx/plugins/`）才需要先有 BepInEx。
> 手动安装的话去 [BepInEx Releases](https://github.com/BepInEx/BepInEx/releases) 下
> **5.4.23.5 x64**（这个游戏的 Unity 是 64 位的）。

### 三、游戏本体

| 依赖 | 谁来提供 | 通常是否已具备 |
| --- | --- | --- |
| Windows | 你的机器 | 必需 |
| **显卡驱动** | 显卡厂商 | 需要自己保持较新 |
| `d3d11.dll` — Direct3D 11（主渲染器） | 系统 | Win10 / 11 自带 |
| `dxgi.dll` | 系统 | 自带 |
| `d3d9.dll` — Direct3D 9（回退） | 系统 | 自带 |
| `opengl32.dll` — OpenGL（回退） | 系统 | 自带 |
| `vulkan-1.dll` — Vulkan（可选） | 显卡驱动 | 装好驱动就有 |
| `d3dcompiler_47.dll` — 着色器编译 | 系统 | Win10 / 11 自带；**Win7 / 8 可能缺** |
| `xinput1_3.dll` — 手柄支持 | 系统 | Win10 自带；**Win7 / 8 可能缺** |
| `dsound.dll` — DirectSound | 系统 | 自带 |

**Win7 / 8 上如果缺 `d3dcompiler_47.dll` 或 `xinput1_3.dll`**，
装一次 [DirectX 终端运行时](https://www.microsoft.com/download/details.aspx?id=35) 即可
（包很小）。这两个都在里面。

> 顺带一提：`UnityPlayer.dll` 自己也引用 `winhttp.dll` ——
> 而 BepInEx 的 Doorstop 正是靠替换这个 DLL 来注入的。所以它是个很自然的注入点。

### 四、不需要装的东西

| 不需要 | 为什么 |
| --- | --- |
| BepInEx | 安装程序自带（内嵌在 exe 里） |
| .NET SDK / .NET 运行时（CoreCLR） | 用的是 .NET **Framework**，不是 .NET 5+ |
| **Visual C++ 运行库** | `UnityPlayer.dll` 不引用 `vcruntime140` / `msvcp140` |
| Mono | 游戏目录自带 `MonoBleedingEdge` |
| 管理员权限 | 装在自己的游戏目录里即可 |
| 联网 | 安装程序里的 BepInEx 是内嵌的，不会去下载 |
| Java / Python / Node 之类 | 与本项目无关 |

### 五、排查顺序

游戏跑不起来时，**按这个顺序查**，别一上来就怀疑修改器：

1. **不开修改器，直接启动游戏** —— 能进游戏吗？
2. 进不去 → 显卡驱动 / DirectX 运行时的问题，和修改器无关
3. 能进去但按 F9 没反应 → 才轮到查修改器（见下方「常见问题」）

---

## 支持的游戏版本

**这款游戏没有版本号。** exe 的 `FileVersion` 填的是 Unity 的版本，`app.info` 里只有厂商名和标题
（`幻夢劇場シャルロッテ | Four Nights at the Burger Shop`）。

所以下面给的是**可以自己核对的事实**，不是一句"支持 v1.0"。

### 开发与测试对着的这一份

| 项目 | 值 |
| --- | --- |
| **引擎** | Unity **2022.1.24f1** (`709dddfb713f`) |
| **游戏程序集** | `Four Nights at the Burger Shop_Data/Managed/Assembly-CSharp.dll` |
| **大小** | 293888 字节 |
| **SHA256** | `9f2c64db2dc259d4347aeb00ef644507a22aee9d5a3b66328cb27babda9a0bb4` |

自己核对：

```powershell
Get-FileHash "<游戏目录>\Four Nights at the Burger Shop_Data\Managed\Assembly-CSharp.dll" -Algorithm SHA256
```

**对得上** → 和这个修改器开发时的是同一份，全部功能都是对着它验证的。

**对不上** → 不一定会坏，但**没验证过**。见下。

### 为什么用程序集指纹而不是版本号

这个修改器靠 Harmony 给游戏里的**具名方法**挂补丁：

```
TabemiControl.Osiri叩かれる / UpdateAtt / Osiri解除 / 頭叩かれるSit
Live2D_Animation_SitOsiri.Event_OsiriSyasei*
MenuControl.Timing / BuildMenu
```

**方法名或签名一变，对应的补丁就挂不上。** 版本号是发布者随便起的，程序集才是真正决定能不能挂的东西。

### 版本对不上会怎样

不会崩，也不会损坏存档。表现是**部分功能无效**：

- 插件启动时会在日志里报「找不到 xxx」或「挂载失败」
- 面板会显示「挂载点 36 个」这个数字 —— **少了就说明有补丁没挂上**
- 游戏里按 F9 还能打开面板，但对应那一栏的改动不生效

日志位置：`<游戏目录>/BepInEx/LogOutput.log`。
搜 `找不到` 或 `挂载` 就能看到哪些没成功。

### 插件会自己核对

不用手动比对。插件启动时会读一次游戏程序集，结论写在两处：

- **日志**：`<游戏目录>/BepInEx/LogOutput.log` 里搜「程序集核对」，
  会看到「与开发时一致」或「与开发时【不一致】」
- **面板**：「系统 → 取证 / 自检」的第一行就是它。不一致时会多出一个
  「看详情（程序集指纹）」按钮，点开能看到期望值与实际值

**只报告，不拦截** —— 版本不同不一定坏，硬拦会把「或许能用」变成「一定不能用」。

### 如果你的是别的版本

两个办法：

1. **告诉我**（仓库 Issue / bilibili / 邮箱都行）—— 如果能拿到那份程序集，
   把方法名和签名对一遍就能知道差在哪里
2. **自己看** —— 面板的「系统 → 取证 / 自检」里有绑定检查，会把没挂上的列出来

> 这个游戏有 DLC 和不同发布渠道的版本。**不同渠道的构建可能不一样**，
> 如果你是别的来源，先核对上面那个 SHA256 再装。

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

`ENGINEERING-NOTES.md` 有 **40 条**历史记录 + **4 篇**附录 + **4 条** S 系列，
每条都写了**症状 / 根因 / 修法 / 教训**。开头有一张**目录**，可以直接跳到某一条。

`KNOWN-ISSUES.md` 是它的**派生视图** —— 只列还没解决的，
每条的状态一行写清，编号与工程笔记对应。

踩过的坑包括：

- Harmony 挂错类会**静默失败**（同一个方法名可能属于不同的类）
- 游戏的动画有**两种播法**（混合器 `*.State` 与层上 `Play` 的 `*.CurrentState`），调速逻辑两种都要覆盖
- **鼠标与触摸走两条完全不同的路**（静态字段 vs 方法返回值）
- 三份参数下，`set` 命令落在哪个物理键上**取决于当时的姿势**

---

## 代码签名

**三个可执行文件都带签名**，签的是 **S.M.Y.T.**（自签证书，不是商业证书）。

```
汉堡店修改器-安装程序.exe    O=S.M.Y.T., CN=S.M.Y.T. Code Signing
数值修改器.exe              O=S.M.Y.T., CN=S.M.Y.T. Code Signing
BurgerShopModder.dll       O=S.M.Y.T., CN=S.M.Y.T. Code Signing
```

自己可以核实：

```powershell
Get-AuthenticodeSignature "BurgerShopModder-Installer.exe" |
    Select-Object Status, @{n='Signer';e={$_.SignerCertificate.Subject}}
```

### 为什么是自签，不是商业证书

一张证书的 **organization 字段是发证方填的**。商业 CA 按一个**经过审核的法人实体**去填，
它要看营业执照。S.M.Y.T. 不是公司，也不该为了一个字段去变成公司。

所以自签不是"买不起证书的将就"，**它是唯一能如实写明这是谁做的那种形式** ——
信任的根留在做东西的人手里，而不是留在某个能单方面吊销、改条款、或者哪天不存在的公司那里。

### 这意味着什么，说清楚

| | 商业 CA | 自签（本项目） |
| --- | --- | --- |
| organization 字段 | 来自被审核的**公司** | 自己的声明 |
| 信任根 | CA 的，不公开、不可审计 | **你的，你自己持有** |
| 被吊销 | CA 可以单方面吊销 | 没有人能，也不需要 |
| 在信任该证书的机器上验通 | 是 | 是 |
| **在陌生人的机器上验通** | 是 | **否** |
| **消除 SmartScreen 警告** | 是 | **否** |

**所以：这个签名不解决 SmartScreen 警告，也不解决杀毒误报。**

第一次运行时仍可能出现「Windows 已保护你的电脑」——
点「更多信息」→「仍要运行」。

（这一条**即使换了商业证书也可能出现**：SmartScreen 还要累积"下载量信誉"。）

### 杀毒软件可能报毒，原因是可以解释的

| 项目里的东西 | 为什么会被误判 |
| --- | --- |
| `winhttp.dll`（BepInEx 的 Doorstop） | 它**替换游戏目录里的系统 DLL** 来注入代码，这正是外挂的典型手法 |
| `BurgerShopModder.dll` | 运行时修改游戏内存里的数值 |
| 安装程序 | 往游戏目录写文件、改 `doorstop_config.ini` |

**行为上它确实像外挂**，因为它就是。区别只在于：它是你自愿装的，只改这一个游戏。

### 怎么确认你下到的文件没被改过

**签名和 SHA256 一起用。**

签名证明「这是 S.M.Y.T. 签的」，校验和证明「你拿到的和发布的是同一份」。

```powershell
# 1. 下载 exe 和 SHA256SUMS.txt 到同一个目录
# 2. 算你自己的哈希
Get-FileHash "BurgerShopModder-Installer.exe" -Algorithm SHA256
# 3. 和 SHA256SUMS.txt 里的那行比，一样就没被动过
```

v1.0.6 的值是：

```
0fd0b0cc1ad8c770c91ac0603e55691c7f09c1f163f97ccd615f6e648a530a84  BurgerShopModder-Installer.exe
bda4e32aa3c158d0df65f93c4ba6ca2ba8b36833d88a8f6eb9be9cba31c25a82  BurgerShopModder-Trainer.exe
a43f5e4e3b24dcdebab3424511134346cfed1f3365d06aa88ae1172b3360ab44  BurgerShopModder.dll
```

**校验和对不上就别运行**，那说明文件在中途被改过。

每次签名都记在仓库里的 [`SIGNINGS.ndjson`](SIGNINGS.ndjson) ——
同时记签名【前】和【后】的 SHA256，所以它回答两件事：
打进签名的是哪个文件，签完发布的是哪个哈希。
本机路径已去掉。

### 直说的话

**校验和能证明「你拿到的和我发布的是同一份」，但证明不了「我发布的东西本身是善意的」。**
这个判断只能你自己做。仓库是全开的，代码都在这儿。

## 常见问题

### 装的时候报「此 BackgroundWorker 声明它不报告进度」

**这是 v1.0.0 和 v1.0.1 的 bug，v1.0.2 已修。**

```
此 BackgroundWorker 声明它不报告进度。
请修改 WorkerReportsProgress 以声明它报告进度。
```

请到 [Releases](https://github.com/Nesarf/BurgerShop-FourNights-Mod/releases)
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
