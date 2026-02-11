# GPU Frame Animation 技术文档

## 概述

GPU Frame Animation 是一个基于 GPU Instancing 技术的高性能 2D 帧动画系统。该系统通过将帧序列打包到纹理图集中，并使用 GPU 进行帧切换渲染，实现了大量动画对象的高效批量渲染。

## 核心特性

- **GPU Instancing 批量渲染** - 使用相同材质的动画对象可合批渲染
- **材质缓存池** - 自动管理共享材质，优化 DrawCall
- **编辑器实时预览** - 支持 Editor 模式下的动画预览
- **忽略时间缩放** - 支持不受 Time.timeScale 影响的动画播放
- **事件系统** - 完整的动画开始/结束事件回调

---

## 架构设计

### 类关系图

```
GPUAnimManager (单例)
    │
    ├── 材质缓存池 (Dictionary<Texture2D, Material>)
    │
    └── GetSharedMaterial()
            │
            ▼
    GPUFrameAnimator (动画控制器)
        │
        ├── GPUAnimationParam[] (动画参数列表)
        │   ├── AnimName: string
        │   └── GpuAnim: GPUInstancedAnimation
        │
        └── 事件: EOnAnimStart, EOnAnimEnd
                │
                ▼
        GPUInstancedAnimation (核心动画组件)
            │
            ├── GPUAnimFlash (闪烁效果)
            │
            └── MaterialPropertyBlock (属性传递)
```

---

## 组件详解

### 1. GPUAnimManager

**文件**: [GPUAnimManager.cs](GPUAnimManager.cs)

**职责**: 单例管理器，负责材质缓存和全局时间管理

#### 核心成员

| 成员 | 类型 | 说明 |
|------|------|------|
| `Instance` | `static GPUAnimManager` | 线程安全的单例访问点 |
| `baseMaterial` | `Material` | 基础材质模板 |
| `_materialPool` | `Dictionary<Texture2D, Material>` | 材质缓存池 |

#### 关键方法

```csharp
public Material GetSharedMaterial(Texture2D tex)
```

- 根据纹理获取或创建共享材质
- 相同纹理的对象共享同一材质，实现合批

#### 全局 Shader 变量

```csharp
_UnscaledTime: Vector4  // (t/20, t, t*2, t*3)
```

模拟 Unity 内置 `_Time` 变量结构，支持忽略时间缩放的动画。

---

### 2. GPUInstancedAnimation

**文件**: [GPUInstancedAnimation.cs](GPUInstancedAnimation.cs)

**职责**: 核心动画组件，负责单组帧动画的播放控制

#### 配置参数

**Resources (资源)**
| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `mainTexture` | `Texture2D` | - | 帧序列图集 |
| `pixelsPerUnit` | `float` | 100 | 像素到单位转换比例 |
| `Pivot` | `Vector2` | (0.5, 0.5) | 旋转轴心位置 |
| `tintColor` | `Color` | White | 着色颜色 |

**Layout Settings (布局设置)**
| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `rows` | `int` | 8 | 图集行数 |
| `columns` | `int` | 8 | 图集列数 |
| `startFrame` | `int` | 0 | 起始帧索引 |
| `totalFrames` | `int` | 64 | 总帧数 |
| `fps` | `float` | 30 | 播放帧率 |
| `isLoop` | `bool` | true | 是否循环播放 |

**Play Settings (播放设置)**
| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `AutoPlay` | `bool` | false | 启用时自动播放 |
| `IgnoreTimeScale` | `bool` | false | 忽略时间缩放 |

#### Shader 属性映���

| Shader ID | 来源 | 用途 |
|-----------|------|------|
| `_Columns` | `columns` | 图集列数 |
| `_Rows` | `rows` | 图集行数 |
| `_StartFrame` | `startFrame` | 起始帧 |
| `_TotalFrames` | `totalFrames` | 总帧数 |
| `_FPS` | `fps` | 帧率 |
| `_Loop` | `isLoop` | 循环标志 |
| `_StartTime` | 计算值 | 播放起始时间 |
| `_PivotOffset` | `Pivot` | 轴心偏移 |
| `_Color` | `tintColor` | 着色颜色 |
| `_IsEditorPreview` | 编辑器 | 预览模式标志 |

#### 事件接口

```csharp
public event Action<GPUInstancedAnimation> OnPlayStart;
public event Action<GPUInstancedAnimation> OnPlayFinished;
```

#### 公共方法

```csharp
// 播放动画
public void Play()

// 更新材质属性
public void UpdateProperties(float startTime)

// 设置着色颜色
public void SetTintColor(Color color)

// 更换动画类别材质
public void ChangeAnimationCategory(Material newCategoryMat)
```

#### 编辑器功能

- `Sync PPU from Texture` - 从纹理导入设置同步像素单位
- 实时预览动画第一帧
- 参数修改即时更新

---

### 3. GPUFrameAnimator

**文件**: [GPUFrameAnimator.cs](GPUFrameAnimator.cs)

**职责**: 高级动画控制器，管理多个动画片段并支持切换播放

#### 配置参数

| 参数 | 类型 | 说明 |
|------|------|------|
| `gpuAnimationParamList` | `List<GPUAnimationParam>` | 动画参数列表 |
| `defaultAnimationName` | `string` | 默认播放动画名称 |
| `autoPlay` | `bool` | 启用时自动播放默认动画 |

#### GPUAnimationParam 结构

```csharp
public class GPUAnimationParam
{
    public string AnimName;           // 动画名称
    public GPUInstancedAnimation GpuAnim; // 动画组件引用
}
```

#### 事件接口

```csharp
public event Action<string> EOnAnimStart;  // 动画开始事件
public event Action<string> EOnAnimEnd;    // 动画结束事件
```

#### 公共方法

```csharp
public void Play(string rAnimName)
```

- 按名称播放指定动画
- 自动隐藏其他所有动画子对象

#### 编辑器功能

```csharp
private void Reset()
```

自动扫描子对象，收集所有 `GPUInstancedAnimation` 组件并生成参数列表。

---

### 4. GPUAnimFlash

**文件**: [GPUAnimFlash.cs](GPUAnimFlash.cs)

**职责**: 为动画添加闪烁效果（如受击反馈）

#### 配置参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `flashGradient` | `Gradient` | - | 闪烁颜色渐变 |
| `defaultDuration` | `float` | 0.3 | 默认闪烁时长 |

#### 公共方法

```csharp
public void PlayFlash(float duration = -1f)
```

触发闪烁效果，`duration` 为 `-1` 时使用默认时长。

---

### 5. AtlasParser (Editor 工具)

**文件**: [Editor/Scripts/AtlasParser.cs](Editor/Scripts/AtlasParser.cs)

**职责**: 编辑器工具，用于解析 .atlas/.atlas.txt 文件并提取动画状态信息

#### 功能特性

| 功能 | 说明 |
|------|------|
| 文件���滤 | 自动忽略以 `bounds` 和 `origin` 开头的行 |
| 帧解析 | 根据最后一个下划线分割帧信息 |
| 状态合并 | 自动合并同一状态的帧序列 |
| 批量处理 | 支持多选文件批量解析 |

#### 使用方式

**方式一：菜单栏**
```
Tools > GPUFrameAnimation > Atlas Parser
```

**方式二：右键菜单（单个文件）**
```
选中 .atlas ���件 → 右键 → GPUFrameAnimation > Parse Atlas
```

**方式三：右键菜单（批量）**
```
选中多个 .atlas 文件 → 右键 → GPUFrameAnimation > Parse All Selected Atlas
```

#### 解析格式

**输入文件格式**:
```
hero4-initial-atk_0
hero4-initial-atk_1
...
hero4-initial-atk_29
hero4-initial-idle_0
hero4-initial-idle_1
...
bounds: ...
origin: ...
```

**输出结果格式**:
```
hero4-initial-atk@0@30
hero4-initial-idle@0@50
```

#### 数据结构

```csharp
public class AnimationStateInfo
{
    public string stateName;      // 状态名
    public int startFrameIndex;   // 起始帧索引
    public int frameCount;        // 总帧数
}
```

---

### 6. GPUFrameAnimatorDemo (Demo 脚本)

**文件**: [Demo/GPUFrameAnimatorDemo.cs](Demo/GPUFrameAnimatorDemo.cs)

**职责**: 功能演示脚本，展示 GPUFrameAnimator 的完整用法

#### 功能特性

| 功能 | 说明 |
|------|------|
| 动画选择 | 下拉菜单选择要播放的动画 |
| 播放控制 | 播放按钮、上一个/下一个切换 |
| 事件监听 | 显示动画开始/结束事件 |
| 循环播放 | Toggle 开关控制循环播放 |
| 状态显示 | 实时显示当前播放状态 |
| 事件日志 | 带时间戳的事件记录，自动清理 |

#### 公共方法

```csharp
// 播放指定动画
public void PlayAnimation(string animName)

// 通过索引播放动画
public void PlayAnimationByIndex(int index)

// 播放下一个动画
public void PlayNextAnimation()

// 播放上一个动画
public void PlayPreviousAnimation()
```

---

## 程序集定义

**文件**: [GPUFrameAnimation.asmdef](GPUFrameAnimation.asmdef)

```json
{
    "name": "GPUFrameAnimation"
}
```

独立的程序集定义，支持作为独立模块引用。

---

## 使用示例

### 基础用法

```csharp
// 1. 创建动画对象
GameObject animObj = new GameObject("MyAnimation");
GameObject child = GameObject.CreatePrimitive(PrimitiveType.Quad);
child.transform.parent = animObj.transform;

// 2. 添加动画组件
GPUInstancedAnimation anim = animObj.AddComponent<GPUInstancedAnimation>();
anim.mainTexture = myTextureAtlas;    // 8x8 帧图集
anim.rows = 8;
anim.columns = 8;
anim.totalFrames = 64;
anim.fps = 30;
anim.AutoPlay = true;

// 3. 播放动画
anim.Play();
```

### 多动画切换

```csharp
// 使用 GPUFrameAnimator 管理多个动画
GPUFrameAnimator animator = gameObject.AddComponent<GPUFrameAnimator>();
animator.defaultAnimationName = "Idle";
animator.autoPlay = true;

// 切换动画
animator.Play("Run");
animator.Play("Attack");
```

### 闪烁效果

```csharp
GPUAnimFlash flash = gameObject.AddComponent<GPUAnimFlash>();

// 配置渐变色
Gradient gradient = new Gradient();
GradientColorKey[] colorKeys = {
    new GradientColorKey(Color.white, 0f),
    new GradientColorKey(Color.red, 0.5f),
    new GradientColorKey(Color.white, 1f)
};
gradient.colorKeys = colorKeys;
flash.flashGradient = gradient;

// 触发闪烁
flash.PlayFlash(0.3f);
```

### 事件监听

```csharp
// GPUInstancedAnimation 事件
anim.OnPlayStart += (a) => Debug.Log("Animation started");
anim.OnPlayFinished += (a) => Debug.Log("Animation finished");

// GPUFrameAnimator 事件
animator.EOnAnimStart += (name) => Debug.Log($"Started: {name}");
animator.EOnAnimEnd += (name) => Debug.Log($"Ended: {name}");
```

### Atlas 文件解析 (Editor)

```csharp
// 使用 AtlasParser 工具解析 .atlas 文件
using GPUAnimation.Editor;

string filePath = "path/to/hero.atlas";
List<AnimationStateInfo> states = AtlasParser.ParseAtlasFile(filePath);

// 合并帧信息为完整动画状态
List<AnimationStateInfo> mergedStates = AtlasParser.MergeFrameInfos(states);

// 输出解析结果
foreach (var state in mergedStates)
{
    Debug.Log($"{state.stateName}@{state.startFrameIndex}@{state.frameCount}");
}
// 输出: hero4-initial-atk@0@30
```

---

## Shader 要求

该系统需要使用支持以下属性的 Shader：

```hlsl
// 必需属性
_Columns          // 图集列数
_Rows             // 图集行数
_StartFrame       // 起始帧索引
_TotalFrames      // 总帧数
_FPS              // 播放帧率
_Loop             // 是否循环
_StartTime        // 播放起始时间
_PivotOffset      // 轴心偏移 (xy)
_Color            // 着色颜色
_IsEditorPreview  // 编辑器预览标志
_IgnoreTimeScale  // 忽略时间缩放
_UnscaledTime     // 全局非缩放时间 (由 GPUAnimManager 设置)
```

---

## 性能优化要点

1. **材质共享** - 相同纹理的对象自动共享材质，实现 GPU Instancing 合批
2. **MaterialPropertyBlock** - 使用属性块而非材质实例，避免材质克隆
3. **纹理图集** - 将多帧打包到单张纹理中，减少纹理切换
4. **双精度时间** - 支持忽略时间缩放的时间计算

---

## 目录结构

```
GPUFrameAnimation/
├── Scripts/                          # 运行时脚本
│   ├── GPUAnimManager.cs           # 材质管理单例
│   ├── GPUInstancedAnimation.cs    # 核心动画组件
│   ├── GPUFrameAnimator.cs         # 动画控制器
│   ├── GPUAnimFlash.cs             # 闪烁效果
│   └── GPUFrameAnimation.asmdef    # 程序集定义
│
├── Editor/                            # 编辑器脚本
│   └── Scripts/
│       └── AtlasParser.cs            # .atlas 文件解析工具
│
├── Demo/                              # 演示场景
│   ├── Test/
│   │   ├── UserTest.cs              # 批量生成测试
│   │   ├── UserTestAnimator.cs       # 动画切换测试
│   │   └── GPUInstanceTest.cs       # 实例化测试
│   └── GPUFrameAnimatorDemo.cs       # 动画演示脚本
│
└── README.md                          # 本文档
```

---

## 命名空间

所有组件位于 `GPUAnimation` 命名空间下：

```csharp
using GPUAnimation;
```

---

## 依赖关系

- **Unity**: MeshRenderer, MaterialPropertyBlock
- **Shader**: Custom/GPUFrameAnimation (需自定义实现)
- **DOTween**: 项目中使用 DOTween (可选)

---

## 版本信息

### 运行时组件

| 组件 | 文件 | 行数 |
|------|------|------|
| GPUAnimManager | [Scripts/GPUAnimManager.cs](Scripts/GPUAnimManager.cs) | ~100 |
| GPUInstancedAnimation | [Scripts/GPUInstancedAnimation.cs](Scripts/GPUInstancedAnimation.cs) | ~315 |
| GPUFrameAnimator | [Scripts/GPUFrameAnimator.cs](Scripts/GPUFrameAnimator.cs) | ~155 |
| GPUAnimFlash | [Scripts/GPUAnimFlash.cs](Scripts/GPUAnimFlash.cs) | ~75 |

### 编辑器工具

| 组件 | 文件 | 行数 |
|------|------|------|
| AtlasParser | [Editor/Scripts/AtlasParser.cs](Editor/Scripts/AtlasParser.cs) | ~400 |

### Demo 脚本

| 组件 | 文件 | 行数 |
|------|------|------|
| GPUFrameAnimatorDemo | [Demo/GPUFrameAnimatorDemo.cs](Demo/GPUFrameAnimatorDemo.cs) | ~180 |
| UserTest | [Demo/Test/UserTest.cs](Demo/Test/UserTest.cs) | ~50 |
| UserTestAnimator | [Demo/Test/UserTestAnimator.cs](Demo/Test/UserTestAnimator.cs) | ~30 |
| GPUInstanceTest | [Demo/Test/GPUInstanceTest.cs](Demo/Test/GPUInstanceTest.cs) | ~35 |

### 代码质量

- ✅ 遵循 Unity C# 编程规范
- ✅ 完整的中文注释
- ✅ 空引用安全检查
- ✅ 编辑器实时预览支持
- ✅ Demo 命名空间组织
