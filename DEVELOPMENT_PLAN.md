# NesUnity 后续开发文档

## 1. 文档目的

本文档定义 NesUnity 从当前“可运行 SMB 的扫描线级模拟器”继续演进到“具备稳定规范基线的 NES 模拟器”的开发路线。

第一阶段目标不是一次性实现所有 NES 功能，而是先保证以下闭环稳定：

```text
ROM 加载 -> CPU 执行 -> PPU 渲染 -> 控制器输入 -> SMB 可玩
```

后续再扩展 Mapper、APU、调试器和跨平台支持。

## 2. 当前基线

### 已实现

- 6502 官方和非官方指令。
- CPU 地址模式、栈和基础内存映射。
- `nestest.nes` CPU 日志对照。
- iNES ROM 解析。
- NROM、CNROM、UxROM、MMC1 和 MMC3 Mapper。
- PPU 基础寄存器、NameTable、Palette 和 OAM。
- OAM DMA `$4014`。
- 一号手柄 `$4016/$4017`。
- 扫描线级背景和精灵渲染。
- VBlank、NMI 和基础 NTSC 帧时序。
- Unity 双缓冲纹理显示。
- `Nes.RunFrame()` 帧驱动接口。

### 当前限制

- PPU 还不是完整逐 dot、逐总线周期模拟。
- Sprite Overflow 仍是简化行为，未复现硬件 bug。
- 开放总线和部分寄存器副作用不完整。
- APU 已实现两个 Pulse、Triangle、Noise 通道、Frame Counter、`$4015` 状态、固定环形采样缓冲和 Unity 流式音频输出；DMC 仍为静音 stub。
- 目前支持 NROM、CNROM（Mapper 3）、UxROM（Mapper 2）、MMC1（Mapper 1）和 MMC3（Mapper 4）。
- PlayMode 已覆盖场景音频输出配置和启动播放状态。
- 当前只优先保证桌面 Unity 运行。

## 3. 设计原则

1. 先保持 CPU、PPU、Mapper、输入之间的接口稳定，再扩展功能。
2. 所有影响游戏行为的时序都必须有自动化测试。
3. 不使用 SMB 专用逻辑替代通用 NES 行为。
4. 静态画面测试和真实帧运行测试分开维护。
5. 不支持的 Mapper 或 ROM 必须明确失败，不能继续使用空引用。
6. 渲染层不能通过无界循环阻塞 Unity 主线程。
7. 优先实现可验证的 NTSC 行为，再扩展 PAL/Dendy。

## 4. 阶段一：建立规范基线

状态：已完成第一轮实现，当前 Unity EditMode 回归为 33 项全部通过。

### 目标

固定 CPU/PPU 时钟、帧边界、错误处理和测试输出，形成后续开发的稳定基线。

### 工作内容

- 明确 `Nes.Tick()` 为一个 CPU 周期，PPU 每周期运行 3 dots。
- 保留 `Nes.RunFrame()`，并设置最大周期保护。
- 统一 PPU 的 `scanline`、`dot`、`v`、`t`、`fine X` 和写入翻转位。
- 将 ROM 加载、Mapper 创建、CPU Halt 和帧超时转换为可观测状态。
- 移除测试对仓库内 `screen.png`、`result.txt`、`nametable.txt` 的隐式依赖。
- 确保测试运行不修改已跟踪的功能文件。
- PPU 暴露 `FineXScroll`、`AddressWriteToggle`、`Scanline`、`Dot` 和 `IsOddFrame` 只读状态，供测试和调试使用。
- 测试生成文件统一写入 `/tmp`，不再覆盖仓库内的基准截图和日志。

### 验收标准

- 连续运行 1000 帧无死循环或崩溃。
- `nestest.nes` CPU 对照继续通过。
- 不支持 Mapper 时 `PowerOn()` 返回失败。
- 测试运行不会覆盖仓库内的基准截图。
- `$2005/$2006/$2007` 地址状态、读取缓冲、控制器端口和 NMI 边沿均有自动化断言。

## 5. 阶段二：完善 PPU 规范行为

状态：基础 `v/t/x/w`、NTSC odd-frame、扫描线地址采样、NMI 沿检测、secondary OAM 选择、CNROM/Mapper 3、UxROM/Mapper 2、MMC1/Mapper 1 和 MMC3/Mapper 4 已完成第一版；仍需补充逐 dot 总线行为和测试 ROM。

### 目标

解决滚屏边界、VBlank、NMI 和 PPU 地址寄存器行为问题。

### 工作内容

- `$2000` 只更新 `t` 的 nametable 位和控制位。
- `$2005` 更新 coarse X、fine X、coarse Y、fine Y。
- `$2006` 在第二次写入时将 `t` 复制到 `v`。
- `$2007` 按 `v` 读写，并按照 `$2000` 的增量递增。
- 按 NTSC 2C02 规则实现：
  - 可见扫描线 `0-239`。
  - VBlank 从扫描线 241 开始。
  - pre-render line 为 261。
  - VBlank、Sprite 0 Hit、Sprite Overflow 的清除时机。
  - odd frame 的短帧行为。
- 在扫描线开始时保存渲染地址，避免 CPU 更新 VRAM 时污染已完成画面。
- 实现水平/垂直 coarse scroll 递增和 nametable 切换。
- 实现 dot 257 的水平复制和 pre-render line 的垂直复制。
- 完善 Palette 镜像和 PPUDATA 读取缓冲。

### 测试

- PPU 帧点数和奇数帧长度。
- VBlank 起止和 NMI 触发次数。
- `$2002` 读取后的状态清除。
- `$2005/$2006` 写入翻转位。
- coarse X/Y、fine X/Y 和 nametable 翻转。
- 水平、垂直和四屏镜像。
- 滚屏跨 nametable 边界。
- Pattern Table 选择。

## 6. 阶段三：完善精灵系统

### 目标

让 SMB 中的 Mario、敌人、金币、砖块和管道精灵稳定显示。

### 工作内容

- 完善 OAM DMA 的 513/514 CPU stall。
- 支持 OAM 地址回绕。
- 支持 8x8 和 8x16 精灵。
- 支持水平翻转、垂直翻转、透明色和四个 Sprite Palette。
- 实现精灵优先级。
- 每条扫描线最多选择 8 个精灵。
- 实现 Sprite Overflow 标志。
- 实现 Sprite 0 Hit 的透明像素、左右边界和裁剪条件。

### 测试

- 8x8/8x16 图形读取。
- 翻转和优先级。
- OAM DMA 数据与 stall 周期。
- Sprite 0 Hit。
- Sprite Overflow。
- 左侧 8 像素裁剪。

## 7. 阶段四：控制器与 SMB 集成

### 目标

完成“标题画面 -> 开始游戏 -> 第一关基本操作”的闭环。

### 控制器规范

一号手柄默认映射：

| NES 按键 | Unity 键位 |
| --- | --- |
| A | `Z` |
| B | `X` |
| Select | `Right Shift` |
| Start | `Enter` |
| Up/Down/Left/Right | 方向键 |

控制器必须支持：

- Strobe。
- 按键锁存。
- 8 次串行移位读取。
- 超过 8 次读取的稳定返回值。
- `$4016/$4017` 端口高位行为。

### SMB 验收

- 标题画面稳定显示。
- `Enter` 能进入第一关。
- 方向键能移动 Mario。
- `Z` 能跳跃。
- `X` 能执行跑动相关操作。
- 滚屏时 HUD 不跳动，nametable 边界不闪烁。
- 连续运行 5 分钟无卡死。

## 8. 阶段五：Mapper 扩展

推荐顺序：

1. CNROM / Mapper 3：已完成第一版。
2. UxROM / Mapper 2：已完成第一版。
3. MMC1 / Mapper 1：已完成第一版；已补充动态 mirroring 和 PRG-RAM 保护。
4. MMC3 / Mapper 4：已完成 PRG/CHR bank、动态 mirroring、PRG-RAM 保护和扫描线级 IRQ 第一版；逐 dot A12 IRQ 仍未覆盖。

建议将 Mapper 接口统一为：

```csharp
byte CpuRead(ushort address);
void CpuWrite(ushort address, byte value);
byte PpuRead(ushort address);
void PpuWrite(ushort address, byte value);
void TickCpu();
void TickPpu();
```

每个 Mapper 必须有独立 ROM 测试或最小构造测试，且不能破坏 NROM 回归。

## 9. 阶段六：APU

状态：两个 Pulse、Triangle、Noise 通道、Frame Counter、`$4015` 状态、NES 非线性混音、固定环形采样缓冲和 Unity 流式音频输出已完成第一版；DMC 仍未完成。

### 目标

先实现 SMB 主要音效和音乐，再追求完整音频兼容。

### 实现顺序

1. APU Frame Counter。
2. Pulse 1。
3. Pulse 2。
4. Triangle（已完成第一版）。
5. Noise（已完成第一版）。
6. DMC（寄存器 stub -> DMA 播放器）。
7. NES 混音公式（已完成第一版）。
8. Unity 音频环形缓冲（已完成第一版）。

模拟器核心与 Unity 输出解耦：

```csharp
public interface IAudioSink
{
    void PushSamples(float[] samples);
}
```

音频输出不能阻塞 CPU/PPU 主循环，也不能持续创建临时数组。

## 10. 阶段七：性能与跨平台

桌面版稳定后再处理：

- StreamingAssets 在 Android/iOS 上的异步加载。
- CPU/PPU 执行中的 GC 分配检查。
- 纹理上传优化。
- 音频线程与模拟线程边界。
- ROM 选择和错误提示。
- 模拟时钟与 Unity 帧率解耦。
- 可选 CPU/PPU 调试器。

## 11. 测试矩阵

### CPU

- 全指令表存在性。
- `nestest.nes` 日志对照。
- 地址模式和分页周期。
- 中断向量、BRK、NMI、IRQ、RTI。

### PPU

- 寄存器读写。
- `v/t/x/w`。
- 帧时序和 VBlank。
- NameTable/Palette 镜像。
- 背景滚屏。
- 精灵、Sprite 0 Hit、Sprite Overflow。

### 系统

- OAM DMA。
- 控制器 Strobe 和串行读取。
- Mapper 加载失败。
- `RunFrame()` 最大周期保护。
- SMB 多帧运行。

### Unity

- 场景启动。
- RawImage 比例和 Point Filter。
- PlayMode 运行 60 秒。
- PlayMode 输入流程。
- PlayMode 运行 5 分钟无异常。

## 12. 精度边界

当前计划的目标是逐步接近 NES 规范，而不是立即承诺完整兼容。

已覆盖的规范范围：

- NTSC 基础帧时序。
- VBlank/NMI。
- PPU `v/t/x/w` 基础滚屏更新。
- NameTable 镜像。
- NROM。
- OAM DMA。
- 一号手柄。
- 背景和精灵合成。
- Pulse、Triangle、Noise 音频与 Unity 流式播放。

尚未覆盖：

- APU DMC 播放。
- 其他 Mapper。
- PAL/Dendy 时序。
- 完整逐 dot PPU 总线行为。
- Sprite Overflow 硬件 bug。
- 完整开放总线行为。

## 13. 推荐执行顺序

1. 完成 PPU `v/t/x/w` 和帧时序测试。
2. 修复滚屏边界和 HUD 稳定性。
3. 完善精灵评估和 Sprite 0 Hit。
4. 完成 SMB 输入和 PlayMode 测试。
5. 实现 CNROM、UxROM、MMC1、MMC3。
6. 完善 APU DMC 播放和音频滤波。
7. 做性能、调试器和移动端适配。

每个阶段完成后都必须先通过现有回归测试，再进入下一阶段。
