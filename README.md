# NuistSmart (南信大校园智能助手)

NuistSmart 是一款基于 **WinUI 3** 和 **MVVM 架构** 开发的南京信息工程大学（NUIST）专属校园智能助手系统。旨在为南信大师生提供便捷、智能的校园生活服务体验，集合了统一身份验证、图书馆服务、校园网关、AI校园助手、校车抢票以及校园黄页等核心功能。

## 🌟 核心特性 (Features)

* **🔒 统一身份认证 (NUIST CAS Login)**
  * 内嵌 `WebView2` 实现统一身份认证页面无缝登录。
  * 通过注入自定义 CSS/JS 实现"视觉脱壳"，隐藏多余网页元素。
  * URL拦截机制，防止登录后不必要的重定向。
  * 后台静默抓取用户信息（姓名、学号）并在底部状态栏全局绑定展示。

* **🤖 智能 AI 助手 (AI Campus Assistant)**
  * 深度整合 **Gemini 2.5 Flash** 大模型（兼容 OpenAI API 协议）。
  * 结合本地 LiteDB 数据库缓存的校园公告进行动态总结、Context-Aware（上下文感知）智能问答。
  * 在问答聊天界面完整支持 **Markdown 渲染**，无论是结构化文本或是代码块都能完美展现。

* **📚 图书馆一站式服务 (Library Services)**
  * **新书推荐与检索：** 对接图书馆系统 JSON API 自动抓取书籍数据及封面，包含稳健的图片回源加载机制（Fallback Sources）。
  * **详情浏览：** 通过内嵌 `WebView2` 实现从详情检索到原生阅读的无缝切换浏览体验。

* **🚌 校园巴士自动化服务 (Bus Ticket Grabber)**
  * 深度集成 Python 自动化机制。
  * 内置本地 HTTPS 代理 (基于 `Titanium.Web.Proxy`) 自动抓取认证 Token。
  * 全异步轮询实现校车抢票，可在应用内查看日志面板并实时管控运行状态。

* **📖 校园黄页与通讯录 (Campus Directory)**
  * 后台静默自动化抓取，突破服务端校验及访问频率限制。
  * XHR 请求拦截与分块异步获取，全面确保抓取稳定性与隐蔽性。
  * 离线数据持久化：采用 **LiteDB** 本地数据库缓存数据，随时可离线查阅或供 AI 读取作为语料。

## 🛠 技术栈 (Technology Stack)

* **UI 框架**: WinUI 3 (Windows App SDK)
* **开发语言**: C#, XAML, Python (用于特定脚本/扩展组件)
* **架构模式**: MVVM (Model-View-ViewModel)
* **数据库**: LiteDB (轻量级本地 NoSQL 数据库)
* **核心组件**:
  * `WebView2`: 用于内嵌网页呈现与前端脚本注入
  * `Titanium.Web.Proxy`: 用于本地HTTPS抓包与Token截获
  * Markdown 渲染控件支持

## 🚀 快速开始 (Getting Started)

### 环境依赖

* Windows 10 (Version 1809) 或更高版本。
* 安装有 `.NET 8.0` 或更高版本的 SDK。
* **Visual Studio 2022** (推荐包含 `.NET 桌面开发` 和 `通用 Windows 平台开发` 工作负载)。
* 安装 Windows App SDK (WinUI 3 必需)。

### 编译与运行

1. 下载或克隆本项目到本地：

   ```bash
   git clone <你的项目地址>
   cd NuistSmart
   ```

2. 使用 Visual Studio 2022 打开项目根目录下的 `NuistSmart.slnx` 解决方案文件。
3. 还原 NuGet 包。
4. 在调试目标中选择所支持的 CPU 架构（如 `x64`），然后按 `F5` 启动调试。

## 📂 项目结构描述

* **`Models/`**: 数据模型与实体定义。
* **`ViewModels/`**: MVVM 模式的视图模型逻辑，绑定调度与服务。
* **`Views/`**: XAML 页面与 UI 组件 (如 `LoginPage`, `ShellPage`, `LibrarySearchPage`, `NewsPage` 等)。
* **`Services/`**: 核心后台服务 (如 `LibraryService`, `AiService`, `DbService`, 自动化抓取脚本与代理服务等)。
* **`DbReader/`**: 本地 LiteDB 数据库相关的解析器或工具子项目。

## 📝 证书与协议 (License)

请参阅项目中附带的相关开源许可（如 MIT/GPL 等，根据实际确认修改）。
