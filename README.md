# Unity Interactive — Unity 交互系统框架

## 概述

基于 Unity EventSystem + InputSystem 的交互框架。提供拖拽（Drag）、焦点（Focus）、选择（Click）、长按（Long Press）四种交互类型，通过特性驱动的 **交互案例（InteractCase）** 系统解耦 Subject-Target 交互逻辑，配合 LRU 调度和独立的协程长按检测器，实现高性能、可扩展的 UI 交互方案。

### 核心特性

- 🖱️ **四种交互接口** — `IDraggable` / `IFocusable` / `ISelectable` / `ILongPressHandler`，统一派生自 `IInteractive`
- ⚡ **自包含长按检测** — 每个组件实例独立协程计时器，按下时才运行，无按下时零 Update 开销
- 🔒 **拖拽与长按互斥** — Drag 触发后自动取消长按计时，Press 期间屏蔽拖拽，同一时刻互不干扰
- 🔌 **InputSystem 无关** — `EasyInput` 自动检测 Touchscreen / Mouse 设备，带 Legacy Input fallback
- 🧩 **特性驱动案例系统** — `[InteractCase]` 注册，反射自动发现，支持 Drag 路径和 LongPress 路径
- 📊 **LRU 调度** — 命中案例自动前移，下次优先匹配；按 Order 排序保证确定性
- 🔗 **泛型案例基类** — 泛型变体免去 `OnExecute` / `OnEnter` 内手动 `as` 类型转换
- 🛡️ **悬挂引用自动清理** — OnDisable 清理自身状态，Update 中检测已销毁对象并清除全局引用

---

## 目录结构

```
Assets/Plugins/unity-interactive/
├── Runtime/
│   ├── IInteractive.cs              # 接口定义（IInteractive, IDraggable, IFocusable, ISelectable, ILongPressHandler, IInteractCase, InteractContext）
│   ├── InteractiveComponent.cs      # 交互组件基类（EventSystem 事件 → protected virtual 方法 + 协程长按检测 + Drag/Press 互斥）
│   ├── UnityInteractive.cs          # 全局管理器（Singleton, 案例 LRU 调度, 全局交互状态跟踪, 运行时注册/注销）
│   ├── AbstractInteractCase.cs      # 交互案例基类（AbstractInteractCase, DragSubjectFocusTargetInteractCase, LongPressSubjectFocusTargetInteractCase + 泛型变体, InteractCaseAttribute）
│   └── EasyInput.cs                 # 输入抽象层（InputSystem Touch/Mouse 自动检测 + Legacy Input fallback + EventSystem 射线检测）
└── Editor/
    └── UnityInteractiveEditor.cs    # Inspector 编辑器扩展
```

---

## 快速开始

### 1. 创建可交互组件

继承 `InteractiveComponent`，按需重写对应的虚方法：

```csharp
using NuoYan.Interactive;
using UnityEngine.EventSystems;

public class DraggableItem : InteractiveComponent
{
    protected override void OnStartDrag(PointerEventData eventData) { /* 开始拖拽 */ }
    protected override void OnUpdateDrag(PointerEventData eventData) { /* 拖拽中（每帧） */ }
    protected override void OnStopDrag(PointerEventData eventData) { /* 拖拽结束 */ }

    protected override void OnStartPress(PointerEventData eventData) { /* 达到长按阈值 */ }
    protected override void OnPress(PointerEventData eventData) { /* 长按持续中 */ }
    protected override void OnStopPress(PointerEventData eventData) { /* 长按结束 */ }

    protected override void OnFocus(PointerEventData eventData) { /* 指针进入 */ }
    protected override void OnLostFocus(PointerEventData eventData) { /* 指针离开 */ }
    protected override void OnSelect(PointerEventData eventData) { /* 点击 */ }
}
```

> **注意**：GameObject 上需有 Collider / GraphicRaycaster 等让 EventSystem 能射线命中。

### 2. 启用/禁用交互能力

每个组件实例在 Inspector 中有三个开关，也可代码控制：

```csharp
item.EnableInteractive = false;  // 完全关闭所有交互
item.EnableDrag = false;         // 仅关闭拖拽
item.EnableLongPress = false;    // 仅关闭长按
```

所有虚方法均受对应开关门控，子类无需重复检查。

### 3. 配置全局长按参数

场景中 `UnityInteractive` GameObject 的 Inspector 面板：

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `LongPressThresholdTime` | 1.5s | 按下多久后触发长按 |
| `LongPressInterval` | 1s | 长按触发后 `OnPress` 调用间隔。≤0 表示每帧调用 |

### 4. 定义交互案例

使用 `[InteractCase]` 特性 + 泛型基类声明 Subject 拖拽/长按到 Target 上的行为：

```csharp
using NuoYan.Interactive;

[InteractCase(typeof(DraggableItem), typeof(DropZone), order: 10)]
public class ItemToSlotCase : DragSubjectFocusTargetInteractCase<DraggableItem, DropZone>
{
    protected override void OnEnter(DraggableItem item, DropZone zone)
    {
        zone.Highlight(true);
    }

    protected override void OnExecute(DraggableItem item, DropZone zone)
    {
        item.transform.position = zone.transform.position;
    }

    protected override void OnExit()
    {
        // 离开时清理（实例 ID 自动追踪，无需手动记录上次对象）
    }
}
```

> 案例在 `UnityInteractive.InitOnce()` 中通过反射自动发现并注册，无需手动添加。

---

## 架构概览

```
                       ┌──────────────────────────────┐
                       │      UnityInteractive         │
                       │   (Singleton, Update 驱动)     │
                       │                              │
                       │  CurrentDraggable             │
                       │  CurrentFocusable             │
                       │  CurrentLongPress             │
                       │  AllInteractCase (Dictionary) │
                       │  ActiveCases (LRU List)       │
                       └──────────┬───────────────────┘
                                  │ 每帧 Match
                       ┌──────────▼───────────────────┐
                       │   IInteractCase.Execute()     │
                       │  Subject × Target 类型匹配    │
                       │  首个命中即停止，LRU 前移      │
                       └──────────┬───────────────────┘
                                  │
             ┌────────────────────┼──────────────────────┐
             ▼                    ▼                      ▼
      IDraggable             IFocusable          ILongPressHandler
      (BeginDrag/Drag/       (PointerEnter/      (PointerDown + 协程计时
       EndDrag)               PointerExit)        → OnBeginLongPress/
                                                  OnLongPress/
                                                  OnEndLongPress)
             │                    │                      │
             └────────────────────┴──────────────────────┘
                                  │
                       ┌──────────▼───────────────────┐
                       │   InteractiveComponent        │
                       │  EventSystem → protected 虚方法 │
                       │  Drag/Press 互斥 + 协程计时     │
                       │  Enable 开关门控               │
                       └──────────────────────────────┘
```

### 核心机制

| 机制 | 实现 | 说明 |
|------|------|------|
| 长按检测 | `LongPressRoutine()` 协程 | 仅按下时运行，`yield return null` 逐帧累加；无按下时零 Update 开销 |
| Drag/Press 互斥 | `GetInteractState(Drag)` / `GetInteractState(Press)` | Drag 开始时 `CancelLongPress()`；Press 协程中检测到 Drag 则跳过累加 |
| Click 抑制 | `m_SuppressNextClick` | 长按结束后自动抑制紧随的 `OnPointerClick`，避免双击逻辑双触发 |
| 案例匹配 | `Update()` 每帧遍历 | 按 Order 升序，首个 `Execute` 返回 true 即停；命中案例 LRU 移至队首 |
| 悬挂清理 | `OnDisable` + `Update` 开头 | 组件失活时清理协程和状态；Update 中 `IsValid` 检查销毁引用并清零 |
| 输入抽象 | `EasyInput` 静态类 | 优先检查 Touchscreen 活跃触摸 → 走触摸路径；否则走 Mouse；均不可用 fallback 到 `Input.GetMouseButton*` |

---

## API 参考

### InteractiveComponent（交互组件基类）

继承自 `MonoBehaviour`，实现 `IDraggable`, `IFocusable`, `ISelectable`, `ILongPressHandler`。

**配置属性（Inspector 可见）：**

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `EnableInteractive` | `bool` | `true` | 总开关，关闭后所有交互事件静默 |
| `EnableDrag` | `bool` | `true` | 拖拽开关 |
| `EnableLongPress` | `bool` | `true` | 长按开关 |

**可重写虚方法：**

| 方法 | 对应事件 | 触发时机 |
|------|---------|---------|
| `OnStartDrag(PointerEventData)` | BeginDrag | 开始拖拽 |
| `OnUpdateDrag(PointerEventData)` | Drag | 拖拽中（每帧） |
| `OnStopDrag(PointerEventData)` | EndDrag | 拖拽结束 |
| `OnFocus(PointerEventData)` | PointerEnter | 指针进入 |
| `OnLostFocus(PointerEventData)` | PointerExit | 指针离开 |
| `OnSelect(PointerEventData)` | PointerClick | 点击（长按后自动抑制） |
| `OnStartPress(PointerEventData)` | OnBeginLongPress | 达到 `LongPressThresholdTime`，**仅一次** |
| `OnPress(PointerEventData)` | OnLongPress | 长按持续中，受 `LongPressInterval` 节流 |
| `OnStopPress(PointerEventData)` | OnEndLongPress | 松手、拖拽开始、组件失活 |

**查询方法：**

```csharp
// 检查交互状态（所有 key 均为 true 才返回 true）
bool isDragging = comp.GetInteractState(InteractiveComponent.Drag);
bool isPressing = comp.GetInteractState(InteractiveComponent.Press);
bool isFocused  = comp.GetInteractState(InteractiveComponent.Focus);
```

### UnityInteractive（全局管理器）

Singleton，自动创建 DontDestroyOnLoad GameObject。

**状态查询：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `CurrentDraggable` | `IDraggable` | 当前拖拽中的对象 |
| `CurrentFocusable` | `IFocusable` | 当前指针悬停的对象 |
| `CurrentLongPress` | `ILongPressHandler` | 当前按下中的对象 |
| `CurrentInteractCase` | `IInteractCase` | 当前命中的交互案例 |

**案例管理：**

| 方法 | 说明 |
|------|------|
| `RegisterInteractCase(IInteractCase, bool)` | 运行时注册案例 |
| `UnregisterInteractCase<T>()` | 运行时注销案例 |
| `EnableInteractCase<T>()` | 运行时启用案例 |
| `DisableInteractCase<T>()` | 运行时禁用案例 |

**状态设置：**

| 方法 | 说明 |
|------|------|
| `SetCurrentDraggable(IDraggable)` | 设置当前拖拽对象（传 null 清除） |
| `SetCurrentFocusable(IFocusable)` | 设置当前焦点对象 |
| `SetCurrentLongPress(ILongPressHandler)` | 设置当前长按对象 |

### EasyInput（输入抽象）

纯静态方法，无实例化。

**指针状态查询：**

```csharp
// 按下
bool down = EasyInput.PointerDown();
bool down = EasyInput.PointerDown(index, out int fingerId, out Vector2 position);

// 抬起
bool up = EasyInput.PointerUp();
bool up = EasyInput.PointerUp(index, out int fingerId);

// 移动/按住
bool move = EasyInput.PointerMove();
bool move = EasyInput.PointerMove(index, out int fingerId, out Vector2 position);

// 当前指针下的 GameObject（EventSystem 射线检测）
if (EasyInput.TryGetCurrentPointRayCast(out GameObject hit))
{
    // hit 为射线命中的首个 GameObject
}
```

**设备选择逻辑：**

| 条件 | 路径 | 多指支持 |
|------|------|---------|
| `Touchscreen.current` 有活跃触摸 | 触摸路径 | ✓（通过 index 参数） |
| `Mouse.current` 存在 | 鼠标路径 | — |
| 以上均无 | `Input.GetMouseButton*` fallback | — |

### IInteractCase（交互案例接口）

**InteractCaseAttribute 参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| `subject` | `Type` | Subject 类型（拖拽/长按的主体） |
| `target` | `Type` | Target 类型（焦点目标） |
| `enableExecuteOnLoad` | `bool` | 初始化后是否立即启用，默认 `true` |
| `order` | `int` | 匹配优先级，越小越先，默认 `0` |

**案例基类选择：**

| 基类 | 适用场景 | OnEnter / OnExecute 参数 |
|------|---------|--------------------------|
| `DragSubjectFocusTargetInteractCase` | 仅 Drag × Focus | `IDraggable` / `IFocusable` |
| `DragSubjectFocusTargetInteractCase<TSubject, TTarget>` | 同上 + 泛型 | 强类型 `TSubject` / `TTarget`（推荐） |
| `LongPressSubjectFocusTargetInteractCase` | 仅 LongPress × Focus | `ILongPressHandler` / `IFocusable` |
| `LongPressSubjectFocusTargetInteractCase<TSubject, TTarget>` | 同上 + 泛型 | 强类型 `TSubject` / `TTarget`（推荐） |
| `AbstractInteractCase` | Drag + LongPress 双路径 | 各自 `OnDrag*` / `OnLongPress*` |

---

## 长按检测详解

长按检测完全自包含在 `InteractiveComponent` 内部，不依赖 UnityInteractive 的 Update：

```
OnPointerDown
  │
  ├─ m_Pressing = true, m_PressTime = 0
  ├─ StartCoroutine(LongPressRoutine())    ← 协程启动
  └─ SetCurrentLongPress(this)
        │
        ▼
  LongPressRoutine (yield return null 每帧)
        │
        ├─ PressTime < ThresholdTime  → 继续累加
        ├─ PressTime >= ThresholdTime → OnBeginLongPress (一次)
        │                               + OnLongPress (首次 + 按 Interval 节流)
        └─ GetInteractState(Drag)     → 跳过累加（拖拽已开始）

OnPointerUp
  │
  ├─ StopCoroutine(LongPressRoutine)
  ├─ if (m_LongPressFired) → OnEndLongPress
  ├─ m_SuppressNextClick = true           ← 抑制紧随的 click
  └─ SetCurrentLongPress(null)
```

**节流规则**：`LongPressInterval <= 0` → `OnPress` **每帧**调用；`> 0` → 按秒间隔调用。默认 1 秒调用一次。

**取消时机**：拖拽开始（`OnBeginDrag` 中调用 `CancelLongPress()`）、组件 `OnDisable`、GameObject 销毁。

---

## 常见问题

**Q: 长按不触发，一直是拖拽？**
A: 检查 `EnableLongPress` 是否为 `true`，以及 `LongPressThresholdTime` 是否合理。拖拽只要像素移动超过 EventSystem 的 `pixelDragThreshold` 就会触发，此时长按会被取消（Drag/Press 互斥）。

**Q: 为什么 OnPointerClick 在长按后不触发？**
A: 刻意设计。长按结束后 `m_SuppressNextClick = true`，抑制紧随的 click 事件，避免长按操作被误判为点击。

**Q: 多个案例可能匹配同一个 Subject-Target 组合，如何控制优先级？**
A: 设置 `[InteractCase(..., order: N)]`，Order 越小的案例越先被匹配。首个返回 `true` 的案例命中后停止遍历。

**Q: 如何运行时动态切换案例的启用状态？**
A: 调用 `UnityInteractive.Instance.EnableInteractCase<T>()` / `DisableInteractCase<T>()`。

**Q: 为什么 OnStopPress 中 `CurrentPlacedItemType` 可能为 null？**
A: 长按过程中外部可能调用了 `RemoveItem()` 清空槽位。应在 `OnStopPress` 中做空检查。

**Q: `InteractiveComponent` 适用场景？**
A: 如果需要继承统一基类、利用单例状态追踪和案例系统 → 用 `InteractiveComponent`。
---

## 依赖

- `UnityEngine.InputSystem`（Input System Package）— 新输入系统设备检测
- `UnityEngine.EventSystems`（UGUI）— 事件数据和射线检测

---

## 许可

MIT License
