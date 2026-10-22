# YooAsset 源码与官方 Demo 测试工程

Unity **2022.3.62f3**，YooAsset **3.0.6**，Windows nginx **1.30.6**。源码版本与下载归档信息见 `ThirdPartySources.json`。

YooAsset 通过 **UPM Git 包**引入，固定 `3.0.6`：`https://github.com/tuyoogame/YooAsset.git?path=Assets/YooAsset#3.0.6`。库显示在 Unity 的 `Packages/YooAsset` 下；Space Shooter Sample 单独复制到 `Assets/Samples/Space Shooter`。依赖解析记录在 `Packages/packages-lock.json`。

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

`Run-UnityTask.ps1` 的默认 Unity 路径适用于原搭建机器；其他机器传入 `-UnityExe '你的 Unity.exe 完整路径'`。首次克隆的 v1 会使用当前示例资源（字号 38）；要重做增量实验，再修改一项资源并构建 v2。验证记录中的 38→44 是原始实验数据。

## 打开与运行

1. 用 Unity Hub 添加当前目录 `E:\workspace\YooAssetTest`，选择 2022.3.62f3。
2. 打开 `Assets/Samples/Space Shooter/Boot.unity`，或者点击 **YooAsset Test → 1. Configure and Open Demo**。
3. 首次学习可将 Boot 物体的 `Play Mode` 设为 `EditorSimulateMode`，直接 Play 即可运行官方 Demo，不需要 nginx。`Configure and Open Demo` 菜单会设置该模式。
4. 点击首页 **PLAY NOW** 进入太空战机游戏。

脚本可直接阅读、加断点；包内的 `Samples~/Space Shooter` 是官方原始示例，实际运行的是工程内 `Assets/Samples/Space Shooter`。不要同时在 `Assets` 下放置另一份 YooAsset 库源码。

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

首次搭建实验的服务端已发布 **test-v2**，此前 Windows 客户端内置 v1 的字号为 **38**。迁移到 UPM 后，工程内 Sample 从包重新复制，字号恢复官方默认 **38**；既有构建产物不代表当前工程重新构建结果。上面的 `Publish-Resources -Version test-v1` 可将服务端版本入口切回 v1，方便重新做实验。

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
6. `Packages/com.tuyoogame.yooasset/Runtime/ResourcePackage` → `ResourceManager` → `FileSystem` / `BundleCache` / `DownloadSystem`：沿 API 进入库实现。
7. `Packages/com.tuyoogame.yooasset/Editor/BundleCollector`、`BundleBuilder`：收集与打包。

前五项路径均相对于 `Assets/Samples/Space Shooter/`。原版对照可看包内 `Samples~/Space Shooter/`；磁盘实体由 Unity 管理在 `Library/PackageCache`。

## 本地集成改动

- YooAsset 库通过 UPM 引入官方版本，未新增资源管理封装或面板。
- 活动 Demo 的主机地址从 `http://127.0.0.1` 改为 `http://127.0.0.1:8080`。
- 配置 Enemy tag、Boot 构建场景、公司/产品名与独立的本地数据目录。
- `Assets/Editor/DemoProjectSetup.cs` 提供配置与构建菜单。
- `Assets/Testing/DemoSmokeProbe.cs` 仅在命令行传入 `-yooSmoke` 时启用，监听官方事件并自动确认下载，检查首页和 Prefab 生命周期后退出；正常运行不会启用。
- 实际验证结果、日志与限制见 `docs/validation.md`。
- 旧的 Assets 库源码与 Sample 保存在本机 `.downloads/upm-migration-*` 备份中；UPM 迁移说明见 `docs/upm-migration.md`。

官方项目：https://github.com/tuyoogame/YooAsset/tree/3.0.6
