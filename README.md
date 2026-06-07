# FAST.Matrix

`FAST.Matrix` is the core Application Shell Framework and Micro-Frontend Orchestrator for the **FAST Ecosystem**. Built on top of **Blazor (.NET 8/9/10) Auto Render Mode**, it serves as the intelligent environment ("the grid") that dynamically hosts pluggable enterprise modules (**Applets**).

By packaging infrastructure concerns into a single, high-performance platform, `FAST.Matrix` handles layout rendering (AdminLTE theme), authentication, dynamic sidebar configurations, and state guards, allowing developers to focus purely on business logic.

## 🚀 Key Features

* **Dynamic Plugin Loading:** Dynamic runtime loading of independent applet DLLs on both Server and WASM environments.
* **Contextual UI Injection:** Applets can dynamically override the left sidebar with complex tree-views or inject customized action buttons into the global top toolbar.
* **Bifurcated DI Sandbox:** Isolated dependency injection spaces per Applet to prevent service collision.
* **Data Loss Prevention:** Built-in dynamic navigation locks that monitor unsaved transactional states across active viewports.

---

## 📖 Documentation & Architecture Guidelines

For comprehensive implementation blueprints, interface specifications, setup instructions, and code samples, please visit our official documentation:

👉 **[FAST.Matrix Documentation & Wiki Pages](https://github.com/aafent/FAST.Matrix/wiki)**

### Quick Links inside the Wiki:
* [Getting Started & Manifest Configuration](https://github.com/aafent/FAST.Matrix/wiki/Getting-Started)
* [The IApplet Lifecycle Contract](https://github.com/aafent/FAST.Matrix/wiki/IApplet-Contract)
* [Dynamic UI Interactivity & TreeView Management](https://github.com/aafent/FAST.Matrix/wiki/UI-Orchestration)
