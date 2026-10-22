# YooAsset 官方 Demo 测试工程

日期：2026-10-21

## 目标与范围

用户希望通过运行、调试官方 Demo 熟悉 YooAsset 源码，同时验证基础资源加载与资源热更新。HTTP 静态资源服务使用 nginx。

首轮使用本机 Unity 2022.3.62f3，验证 Editor 与 Windows。采用官方 3.0.6 标签，记录对应提交，源码和 Demo 使用同一版本。首轮完成标准为官方 Demo 可运行，并有实际打包、资源下载、资源更新及缓存复用的验证记录。

## 实现选择

采用官方源码与 Space Shooter Demo，沿用其 Boot、PatchManager、PatchWindow 及 FsmNode 更新流程。YooAsset 源码放在工程 Assets/YooAsset 中供直接阅读与断点调试；Space Shooter 从同版本 Samples~ 导入到 Assets 下供 Unity 编译和导入。保留官方资源及 meta 的 GUID，避免重复导入相同源码或资源。

与仅安装 UPM 包相比，此方式便于阅读与调试源码；与自建测试面板相比，可直接沿官方示例理解调用路径。首轮不额外开发资源管理封装或测试面板。

运行所需的采集配置、场景、Tags、包依赖与 Player 配置以该版本 Demo 的真实文件为依据核对。必要修正集中记录，保留可对照官方代码的结构。

## 官方流程与阅读入口

已核对 3.0.6：

- `Boot.cs` 初始化 YooAssets、创建更新窗口并启动 PatchManager。
- `PatchManager.cs` 注册初始化、查询版本、更新清单、创建下载器、下载资源、下载完成、清理缓存和启动游戏的状态节点。
- `FsmInitializePackage.cs` 提供 EditorSimulateMode、OfflinePlayMode、HostPlayMode 等模式。
- PC 的默认资源服务路径为 `http://127.0.0.1/CDN/PC/v1.0`。

源码依据：

- https://github.com/tuyoogame/YooAsset/blob/3.0.6/Assets/YooAsset/Samples~/Space%20Shooter/GameScript/Runtime/Boot.cs
- https://github.com/tuyoogame/YooAsset/blob/3.0.6/Assets/YooAsset/Samples~/Space%20Shooter/GameScript/Runtime/PatchLogic/PatchManager.cs
- https://github.com/tuyoogame/YooAsset/blob/3.0.6/Assets/YooAsset/Samples~/Space%20Shooter/GameScript/Runtime/PatchLogic/FsmNode/FsmInitializePackage.cs

## nginx 资源服务

在工程内提供独立 nginx 配置、静态资源目录与操作说明。默认监听 `127.0.0.1:8080`，Demo 的主机地址作对应最小修改；资源路径保持 `/CDN/PC/v1.0/`。如端口已占用，先检查占用者，不停止其他服务。

保留 Demo 的 `DefaultPackage`。根据官方构建输出，将客户端实际请求的版本文件、清单文件和 Bundle 发布到 nginx 对应目录，不假设输出文件命名或额外拼接包名层级。

`v1.0` 为 Demo 的 URL 路径段；本次实验的资源包版本号分别使用 `test-v1` 与 `test-v2`，与 URL 路径段区分。第二次发布保留旧 Bundle，先放入新 Bundle 与清单，最后替换版本入口文件。

当前 PATH 未找到 nginx，进程检查也未发现 nginx.exe。实施时先定位可用安装；如确实没有，在工程内使用独立 nginx，避免修改系统服务或其他 nginx 实例。

## 验证顺序

1. Unity 导入、编译，打开官方 Boot 场景。
2. EditorSimulateMode 跑通更新 UI 并进入官方游戏，确认资源加载正常。
3. 构建 `test-v1` 资源与带内置资源的 Windows 客户端，验证 OfflinePlayMode。
4. 启动本工程 nginx，校验版本、清单和 Bundle 的实际 HTTP 请求。
5. HostPlayMode 验证远端资源下载与进入游戏。
6. 修改一项可见的官方 Demo 资源并构建 `test-v2`，使用原客户端更新，确认更新后的内容生效。
7. 同版本再次启动，检查下载器待下载文件数与 nginx 请求记录，验证已缓存 Bundle 不重复下载；版本、清单请求单独统计。
8. 停止本工程资源服务，观察官方失败提示和恢复后的重试行为。

每项记录实际结果与日志位置；未执行的项目明确标记。编译成功不替代运行、更新或缓存验证。

## 交付内容

- 可由 Unity 打开的工程与同版本官方源码、Space Shooter Demo。
- 必要的官方示例配置修正及其说明。
- 独立 nginx 配置和启动、停止说明。
- 从 Boot 到更新状态机再到加载代码的简短阅读路线。
- 实际执行的验证结果与尚未验证的范围。

工程初始化不包含推送、远端发布或修改现有系统服务。当前目录未发现 Git 仓库，因此本设计文件暂不做 Git 提交。
