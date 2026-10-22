# 搭建与验证记录

验证日期：2026-10-22（Asia/Shanghai）。

## 当前状态

- 工程目录：`E:\workspace\YooAssetTest`。
- Unity 2022.3.62f3，YooAsset 3.0.6，SBP 1.21.25，nginx 1.30.6。
- 源码提交：`3b4cfb36cc2e81b9558ab74d65fee2cf2eac2804`。
- 默认场景 `Assets/Samples/Space Shooter/Boot.unity`，Editor 模拟模式。
- Windows Host 与 Offline 客户端都携带 `test-v1` 内置资源。
- 本地 nginx 恢复运行于 `127.0.0.1:8080`，服务端版本入口为 `test-v2`。
- 活动 Demo 的 UIHome 版本文本字号由 38 改为 44，用作 v2 可观察的资源差异。

## 实际执行结果

| 验证项 | 结果 | 证据 |
| --- | --- | --- |
| 官方源码导入与编译 | 成功，Unity 正常退出 | `Logs/import.log` |
| 最终源码编译及两种客户端构建 | 成功，退出码 0 | `Logs/RebuildPlayers.log` |
| Editor Play Mode 模拟 | 成功进入首页，加载/实例化/销毁 asteroid01 并释放句柄，字号 44 | `Logs/RunEditorSmoke.log`，`SMOKE_PASS HOME_SCENE_AND_PREFAB_OK` |
| test-v1 资源构建 | 成功 | `Logs/build-initial.log`，`RESOURCES_BUILT test-v1` |
| test-v2 资源构建 | 成功，仅构建资源 | `Logs/BuildV2.log`，`RESOURCES_BUILT test-v2` |
| nginx 配置及 HTTP | `nginx -t` 通过；健康检查/版本文件 200；Bundle Range 请求 206 | `Server/logs/access.log`，范围 `bytes 0-15/5134` |
| Host v1 启动 | 成功，使用内置资源，字号 38，下载数 0 | `Logs/smoke-host-v1-final.log` |
| 原 Host 客户端更新 v2 | 成功，下载 1 个 Bundle，11,652 字节；首页 test-v2、字号 44 | `Logs/smoke-host-v2.log` |
| 客户端未重建证明 | v2 实验前后 EXE 与 Assembly-CSharp.dll SHA256 相同 | `Logs/host-before-v2.json` 与 `Logs/host-after-v2.json` |
| v2 缓存复用 | 成功，下载数 0；nginx 仅收到版本文件请求，Bundle 请求数 0 | `Logs/smoke-host-v2-cache.log` 与 `Logs/nginx-cache-requests.log` |
| 最终客户端再次加载 v2 | 成功，版本 test-v2，字号 44，下载数 0 | `Logs/smoke-host-v2-final.log` |
| nginx 停止时 Offline | 成功，仍进入 v1 首页，字号 38 | `Logs/smoke-offline-final.log` |
| nginx 停止时 Host | 正确触发版本请求失败，匹配特定预期标记 | `Logs/smoke-version-failure-final.log`，`SMOKE_PASS EXPECTED_VERSION_FAILURE` |
| 烟测负例 | 服务在线却要求版本失败时，正确返回 2 | `Logs/smoke-negative-final.log`，`SMOKE_FAIL Expected version request failure, but the workflow succeeded` |
| 服务停止/恢复及重复启动 | 独立 PID 校验、优雅退出、重启健康检查 200、重复启动识别已运行实例 | `Tools/Start-Nginx.ps1`、`Stop-Nginx.ps1` 实际执行输出 |
| 恢复服务后最终客户端重启 | 成功，test-v2/字号44/下载0，Bundle HTTP 请求0 | `Logs/smoke-host-recovered.log` 与 `Logs/nginx-final-cache-requests.log` |
| PowerShell 脚本语法 | 全部解析通过，错误数 0 | 最终 PowerShell Parser 检查输出 |
| 官方库源码完整性 | 原始 `.cs` 没有修改，全部原始 GUID 保留 | `Logs/source-integrity.json` |

`SMOKE_PASS EXPECTED_VERSION_FAILURE` 表示负向流程按预期发生，不表示网络请求成功。

v2 实验后的独立审查修正了烟测的预期判定，并重建了两个客户端，因此最终客户端校验和与“客户端未重建证明”的历史实验文件不同。资源增量实验期间的 EXE 和 DLL 确实保持不变；修正后的客户端又完成了 v2 加载、断网、离线与负例回归。

## 定位并修正的问题

1. 新加探针使用了旧版句柄字段 `LastError`，实际 3.0.6 使用 `Error`。编译报告后修正；最终编译通过。
2. 独立 nginx prefix 下缺少 `temp` 根目录，`nginx -t` 明确报错。启动脚本创建该目录后配置检查通过；空 PID 文件也已兼容。
3. 最初尝试无内置文件的 Host 客户端。官方 Demo 设置 `CopyBuiltinPackageManifest=true`，初始化读取内置版本失败。本工程改为保留官方运行流程并携带 v1 的首包；实际下载由 v2 增量实验验证。
4. 官方构建器拒绝复用已有资源版本目录，因此保留旧产物，使用 `RebuildPlayers` 重建客户端，不强制删除或覆盖资源构建目录。
5. 独立审查发现：带“预期版本失败”参数但服务在线时，探针可能误报普通成功。先复现错误退出码 0，再修正判定，最终负例正确退出 2，停服时特定失败标记仍通过。

早期失败日志为上述定位过程保留，不能代替表格中的最终验证结果。

## 验证边界

- 运行测试通过进程日志、官方事件、资源操作状态及 UI 组件属性确认；未进行人工截图验收和完整战斗玩法验收。
- Editor 模拟模式的下载为 YooAsset 虚拟下载，不计入真实 HTTP 下载证据。
- 无图形模式的早期测试会出现 Shader 不支持提示；最终 Windows 运行测试启用了正常图形设备。
- 停服测试覆盖版本请求失败和恢复服务后的再次启动；没有人工点击并验收官方更新窗口的全部重试按钮。
- 未测试 WebGL、微信/抖音小游戏、Android、iOS、弱网、文件损坏、下载中断恢复或生产 CDN。
- 本轮未搭建零资源首包；未更改系统服务、Git 仓库、远端分支或生产服务。
