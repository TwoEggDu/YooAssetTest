# YooAsset UPM 与 Sample 迁移记录

日期：2026-10-22。

## 当前结构

- `Packages/manifest.json` 通过 Git UPM 依赖引入 `com.tuyoogame.yooasset`。
- Git URL：`https://github.com/tuyoogame/YooAsset.git?path=Assets/YooAsset#3.0.6`。
- 版本：3.0.6；`packages-lock.json` 锁定提交 `3b4cfb36cc2e81b9558ab74d65fee2cf2eac2804`。
- Unity 逻辑包路径：`Packages/com.tuyoogame.yooasset`；本机实体由 Unity 管理在 `Library/PackageCache/com.tuyoogame.yooasset@3b4cfb36cc`。
- 工程内运行的示例：`Assets/Samples/Space Shooter`，从解析成功的上述包内 `Samples~/Space Shooter` 重新复制。
- `Assets/YooAsset` 与其根 meta 已移出工程，不再编译重复的库源码。

## 备份与保留的设置

旧库源码、旧 Sample、迁移前 manifest/lock 及用户 Boot 场景保存在本机 `.downloads/upm-migration-20261022-151820`，该目录已被 Git 忽略。

- 保留迁移前用户设置的 Boot 离线模式（PlayMode=2）。
- 保留 `com.unity.ide.visualstudio` 2.0.28 及相应 lock 依赖。
- 保留工作区已有的 `.vsconfig` 和 `YooAssetTest.slnx`。
- 新 Sample 的 nginx 地址仍为 `http://127.0.0.1:8080`。
- 新 Sample 恢复官方 UIHome 字号 38，旧实验的字号 44 仍在备份中。
- 配置工具按原始 GUID 修正并保存了重新复制后的资源采集设置。

与包内原始 Sample 对照，全部 332 个文件存在、原始 GUID 均相同。差异仅为 Boot 模式、nginx 地址和 Unity 保存后的资源采集配置。

## 本轮验证

1. UPM 解析与现有脚本编译成功，Unity 退出码 0：`Logs/upm-resolve.log`。
2. 重新复制 Sample 后，执行 `DemoProjectSetup.RunEditorSmoke`；导入、编译、采集及模拟构建通过，Unity 退出码 0。
3. 实际进入 `scene_home`，完成 `asteroid01` 加载、实例化、销毁实例和句柄释放；首页显示 `Simulate`，字号 38；日志含 `SMOKE_PASS HOME_SCENE_AND_PREFAB_OK`：`Logs/upm-editor-smoke.log`。
4. 校验锁文件提交、移除 Assets 库副本、文件完整性、GUID 和保留设置：`Logs/upm-migration-verification.json`。
5. 测试期间配置工具临时设置模拟模式，进程结束后已恢复用户迁移前的离线模式；Unity 保持关闭。

本次迁移没有重新构建 Windows 客户端或发布新的远端资源。先前生成的 Windows 客户端、Bundles 和 nginx 文件仍是历史实验产物；其验证见 `validation.md`。本机日志是生成文件，后续使用相同任务名运行时可能覆盖同名日志。
