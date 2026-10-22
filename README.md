# YooAsset 源码与官方 Demo 测试工程

Unity **2022.3.62f3**，YooAsset **3.0.6**，Windows nginx **1.30.6**。源码版本与下载归档信息见 `ThirdPartySources.json`。

## 首次克隆后的准备

仓库包含源码、示例资源、Unity 配置和脚本。Unity 缓存、资源构建结果、Windows 客户端、验证日志及 nginx 二进制不会随仓库提交；下文的已验证状态记录的是原始搭建环境。

Editor 模拟模式可以直接运行。要测试 Windows 客户端和 HTTP 更新，先在工程根目录准备 nginx，再构建 v1 与客户端：

```powershell
New-Item -ItemType Directory -Path .downloads -Force | Out-Null
Invoke-WebRequest -Uri 'https://nginx.org/download/nginx-1.30.6.zip' -OutFile '.downloads/nginx-1.30.6.zip'
# 下载归档 SHA256 可与 ThirdPartySources.json 中的记录核对。
Get-FileHash '.downloads/nginx-1.30.6.zip' -Algorithm SHA256
Expand-Archive -LiteralPath '.downloads/nginx-1.30.6.zip' -DestinationPath Tools
# 先用 Unity 打开并完成导入，再关闭当前工程的 Editor 后执行。
.\Tools\Run-UnityTask.ps1 -Task BuildInitial
.\Tools\Publish-Resources.ps1 -Version test-v1
.\Tools\Start-Nginx.ps1
```

`Run-UnityTask.ps1` 的默认 Unity 路径适用于原搭建机器；其他机器传入 `-UnityExe '你的 Unity.exe 完整路径'`。首次克隆的 v1 会使用当前示例资源（字号 44）；要重做增量实验，再修改一项资源并构建 v2。验证记录中的 38→44 是原始实验数据。

## 打开与运行

1. 用 Unity Hub 添加当前目录 `E:\workspace\YooAssetTest`，选择 2022.3.62f3。
2. 打开 `Assets/Samples/Space Shooter/Boot.unity`，或者点击 **YooAsset Test → 1. Configure and Open Demo**。
3. Boot 物体的 `Play Mode` 默认为 `EditorSimulateMode`，直接 Play 即可运行官方 Demo，不需要 nginx。
4. 点击首页 **PLAY NOW** 进入太空战机游戏。

脚本可直接阅读、加断点；`Assets/YooAsset/Samples~` 是官方原始示例，Unity 不编译这个隐藏示例目录；实际运行的是 `Assets/Samples/Space Shooter`。

## nginx 与 Windows 客户端

在工程根目录的 PowerShell 中执行：

```powershell
.\Tools\Start-Nginx.ps1
.\Tools\Publish-Resources.ps1 -Version test-v1
```

- 资源目录：`Server/html/CDN/PC/v1.0/`
- 健康检查：<http://127.0.0.1:8080/health.txt>
- 资源浏览：<http://127.0.0.1:8080/CDN/PC/v1.0/>
- 访问记录：`Server/logs/access.log`
- 离线客户端：`Builds/Offline/YooAssetTest.exe`
- 联机客户端：`Builds/Host/YooAssetTest.exe`

联机客户端内置 test-v1。nginx 发布新版本后，运行同一个客户端，在官方更新窗口确认下载。离线客户端始终使用自身内置的 v1。

当前服务端已经发布 **test-v2**，活动 Demo 的首页版本字号为 **44**；Windows 客户端内置 v1 的字号为 **38**。上面的 `Publish-Resources -Version test-v1` 可将服务端版本入口切回 v1，方便重新做实验。

停止本工程的 nginx：

```powershell
.\Tools\Stop-Nginx.ps1
```

服务只监听本机地址。端口占用时启动脚本会报错；不会关闭其他服务。重复启动会识别本工程已运行的 nginx。

## 构建和热更新实验

Unity 菜单 **YooAsset Test** 提供 v1/v2 资源及两种客户端的构建入口。官方 **YooAsset → Bundle Collector / Bundle Builder** 窗口仍可直接使用。

已存在的资源版本目录不会覆盖。要尝试新的内容，使用新的版本号；辅助脚本目前提供 `test-v1` 和 `test-v2` 两个实验版本。可在 `DemoProjectSetup.cs` 中新增不同版本入口。

资源输出为 `Bundles/StandaloneWindows64/DefaultPackage/<资源版本>/`。URL 中的 `v1.0` 是 Demo 的发布路径，资源版本是 `test-v1`、`test-v2`，二者不同。

```powershell
# 仅适用于尚未构建 test-v2 的目录；当前工程已构建，直接发布即可。
# Unity 没有打开时，也可以批量执行同样的菜单入口。
.\Tools\Run-UnityTask.ps1 -Task BuildV2
.\Tools\Publish-Resources.ps1 -Version test-v2
```

构建 v2 前，修改一项资源，例如 `GameRes/UIPanel/UIHome.prefab` 的 `version` 文本字号。用原来的 Host 客户端运行，检查首页显示新资源版本和修改后的字号。再次运行，同版本的缓存 Bundle 应不再重复下载。

`BuildInitial` 仅用于从未构建过 v1 的新目录，依次构建 v1、Host 和 Offline。已有 v1 时使用 `RebuildPlayers` 重建客户端。Host 模式保留官方 `CopyBuiltinPackageManifest=true`，需要内置清单和 Catalog；本工程使用携带 v1 的首包，不测试零资源首包。

## 源码阅读路线

1. `GameScript/Runtime/Boot.cs`：启动入口，初始化事件和 YooAsset。
2. `GameScript/Runtime/PatchLogic/PatchManager.cs`：官方更新状态机和用户重试事件。
3. `PatchLogic/FsmNode/FsmInitializePackage.cs`：运行模式与文件系统，nginx 地址也在这里。
4. `FsmRequestPackageVersion` → `FsmUpdatePackageManifest` → `FsmCreateDownloader` → `FsmDownloadPackageFiles`：版本、清单、下载。
5. `GameScript/Runtime/GameLogic/GameManager.cs`：场景加载；`WindowLogic` 和 `Behaviour` 中阅读资源使用。
6. `Assets/YooAsset/Runtime/ResourcePackage` → `ResourceManager` → `FileSystem` / `BundleCache` / `DownloadSystem`：沿 API 进入库实现。
7. `Assets/YooAsset/Editor/BundleCollector`、`BundleBuilder`：收集与打包。

前五项路径均相对于 `Assets/Samples/Space Shooter/`。原版对照可看 `Assets/YooAsset/Samples~/Space Shooter/`。

## 本地集成改动

- YooAsset 库源码保留官方版本，未新增资源管理封装或面板。
- 活动 Demo 的主机地址从 `http://127.0.0.1` 改为 `http://127.0.0.1:8080`。
- 配置 Enemy tag、Boot 构建场景、公司/产品名与独立的本地数据目录。
- `Assets/Editor/DemoProjectSetup.cs` 提供配置与构建菜单。
- `Assets/Testing/DemoSmokeProbe.cs` 仅在命令行传入 `-yooSmoke` 时启用，监听官方事件并自动确认下载，检查首页和 Prefab 生命周期后退出；正常运行不会启用。
- 实际验证结果、日志与限制见 `docs/validation.md`。
- Unity 自动为两个原始 `.meta` 补全 importer 默认字段；原始资源 GUID 均未改变，详情见 `Logs/source-integrity.json`。

官方项目：https://github.com/tuyoogame/YooAsset/tree/3.0.6
