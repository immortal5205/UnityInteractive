

# Unity Interactive

基于 Unity 的交互系统框架，提供统一的事件处理和交互案例管理机制。

## 特性

- **统一的交互接口**：定义 `IDraggable`（拖拽）、`IFocusable`（焦点）、`ISelectable`（选择）三种交互类型
- **输入抽象层**：`EasyInput` 支持鼠标和触摸输入，自动检测设备类型
- **交互案例系统**：通过 `IInteractCase` 和特性驱动的方式解耦交互逻辑
- **单例管理**：`UnityInteractive` 提供全局交互状态管理和事件分发
- **编辑器集成**：提供 Unity 编辑器扩展支持

## 核心接口

| 接口 | 说明 |
|------|------|
| `IInteractive` | 基础交互接口 |
| `IDraggable` | 拖拽交互，支持开始、进行中、结束拖拽 |
| `IFocusable` | 焦点交互，支持进入和离开焦点 |
| `ISelectable` | 选择交互，点击触发 |

## 使用方法

### 1. 创建可交互组件

继承 `InteractiveComponent` 并重写需要的方法：

```csharp
using UnityEngine;
using NuoYan.Interactive;

public class DraggableItem : InteractiveComponent
{
    protected override void OnStartDrag(PointerEventData eventData)
    {
        // 开始拖拽时的逻辑
    }

    protected override void OnUpdateDrag(PointerEventData eventData)
    {
        // 拖拽过程中的逻辑
    }

    protected override void OnStopDrag(PointerEventData eventData)
    {
        // 结束拖拽时的逻辑
    }
}
```

### 2. 定义交互案例

使用 `InteractCaseAttribute` 特性注册交互案例：

```csharp
using UnityEngine;
using NuoYan.Interactive;

[InteractCase(typeof(DraggableItem), typeof(FocusTarget))]
public class ItemToTargetInteractCase : DragSubjectFocusTargetInteractCase
{
    public ItemToTargetInteractCase() 
        : base(typeof(DraggableItem), typeof(FocusTarget)) { }

    protected override void OnExecute(IDraggable subject, IFocusable target)
    {
        // 处理拖拽到焦点的交互
    }

    protected override void OnEnter(IDraggable subject, IFocusable target)
    {
        // 进入目标时的回调
    }

    protected override void OnExit()
    {
        // 离开目标时的回调
    }
}
```

### 3. 配置流程

1. 在场景中添加 `UnityInteractive` 单例
2. 在需要交互的对象上添加相应的 `InteractiveComponent` 子类
3. 确保交互对象具有 `Collider` 组件供射线检测
4. 运行游戏即可自动触发交互事件

## 项目结构

```
NuoYan.Interactive/
├── Runtime/
│   ├── EasyInput.cs          # 输入抽象层
│   ├── IInteractive.cs       # 接口定义
│   ├── InteractiveComponent.cs  # 交互组件基类
│   ├── AbstractInteractCase.cs   # 交互案例基类
│   └── UnityInteractive.cs       # 核心管理器
└── Editor/
    └── UnityInteractiveEditor.cs  # 编辑器扩展
```

## 环境要求

- Unity 2019.4 或更高版本
- .NET Standard 2.0+

## 开源协议

MIT License