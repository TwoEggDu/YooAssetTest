# YooAsset Demo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在当前目录搭建可阅读、编译和运行的官方 Demo，并接入本地 nginx 资源服务。

**Architecture:** 同版本官方源码与 Space Shooter 示例作为工程内容。独立 nginx 托管发布目录，少量 Editor 辅助入口负责重复执行配置和构建，不替换官方运行状态机。

**Tech Stack:** Unity 2022.3.62f3、YooAsset 3.0.6、Windows nginx。

**Spec:** `docs/superpowers/specs/2026-10-21-yooasset-demo-design.md`

## Global Constraints

- 用户于 2026-10-22 明确要求开始搭建；在本会话直接执行，不再重复请求搭建授权。
- 使用官方 3.0.6 源码和同版本 Demo，保留原始 meta/GUID。
- HTTP 服务使用独立 nginx，监听 127.0.0.1:8080；不操作其他服务。
- 当前不是 Git 仓库，不自动初始化、提交或推送。
- 按实际运行结果报告，未执行的测试不得标记通过。

## Review Focus

- 同名包双重导入导致重复定义：检查 Packages 与 Assets，仅使用 Assets 源码。
- Sample 的采集路径改变：检查实际收集结果，构建非空资源包。
- 离线模式依赖内置清单：使用官方构建复制策略，并实际离线启动。
- URL 路径与资源输出错位：对客户端请求路径做 HTTP 检查。
- 缓存复用和新版本更新：使用原客户端运行两次并核对下载日志。

### Task 1: 官方源码与 Unity 工程

**Files:** `Assets/YooAsset/`、`Assets/Samples/Space Shooter/`、`Packages/manifest.json`、`ProjectSettings/`、`.gitignore`、`ThirdPartySources.json`。

**Interfaces:** 产生官方 Boot 场景与 DefaultPackage 采集配置，供后续构建。

- [x] 下载固定标签，记录来源、提交和归档哈希。
- [x] 导入官方源码和 Space Shooter；核对包依赖、Tags 与收集目录。
- [x] 使用指定 Unity 执行导入编译，检查退出状态与完整错误日志。

### Task 2: 独立 nginx 与资源构建

**Files:** `Tools/nginx/`、`Tools/Start-Nginx.ps1`、`Tools/Stop-Nginx.ps1`、`Server/`、必要的 `Assets/Editor/` 构建辅助入口、官方 Demo 主机地址。

**Interfaces:** 输入 DefaultPackage 的构建结果；产生 `/CDN/PC/v1.0/` 下的版本、清单与 Bundle。

- [x] 准备官方 Windows nginx、独立配置，运行 `nginx -t`。
- [x] 增加基于官方 API 的配置与构建入口；用真实构建验证采集路径与非空输出。
- [x] 启动独立 nginx，校验版本、清单和 Bundle 的 HTTP 返回。

### Task 3: 运行验证与交付

**Files:** `README.md`、`docs/validation.md`、`Logs/` 和 `Builds/` 中的实际产物。

**Interfaces:** 使用前两项的 Unity 工程、资源包和 HTTP 服务。

- [x] 运行模拟模式，验证进入游戏首页。
- [x] 构建离线与联机 Windows 客户端，检查真实运行日志。
- [x] 构建 v2 并使用原客户端测试更新与缓存；验证资源服务停止后的失败行为。
- [x] 核对最终文件与配置，记录验证证据、源码阅读入口、启动命令与限制。

## Execution ledger

- 2026-10-22：开始。目录仅有设计文档；无 .codegraph、无 Git 仓库。此任务为官方示例与配置集成，以真实 Unity 编译、资源构建、HTTP 和运行日志验证；不为低风险配置另写镜像单元测试。
- Task 1: 源码导入、原版编译、配置和 Windows 构建完成。源码包 SHA256 67E0096389B944945C35982035EE6EF3CDBDA06B3AF2A58D6AC0A98975C7533A。
- Task 2: nginx 配置检查及 /health.txt HTTP 200；test-v1 构建与发布完成。
- Ruling: 官方 Host 配置启用了 CopyBuiltinPackageManifest，且必须存在 BuiltinCatalog；无内置文件的首次测试明确失败。采用携带完整 v1 资源的官方配置，使用原客户端验证 v2 下载与缓存；不扩展零资源首包模式。
- Task 3: Editor 模拟模式、Offline 图形设备模式运行通过。后续联机验证使用重新构建且携带 v1 内置资源的客户端。
- Task 3: complete — v2 原客户端增量 1 个 Bundle/11652 字节，字号38→44；第二次启动Bundle HTTP请求0。最终代码重建、Editor模拟、Host v2缓存、停服Offline、停服Host失败流程均通过，见 docs/validation.md。
- Final review: 独立审查发现 1 个 P2 烟测预期误判；复现服务器在线时错误返回0，修正后同一负例返回2。完成单次修正与相关回归，无其他阻断项。
