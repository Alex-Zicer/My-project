# Player State Machine Refactor

## 目标

本轮重构的目标是让代码状态机和当前的 `Knight.controller` 在“逻辑层”对齐，而不是把 Animator 里的所有状态 1:1 搬到 C#。

核心原则：

- 代码状态机只管理会影响输入接收、物理控制、可中断规则的状态。
- Animator 继续管理纯表现细节状态，例如 `Idle`、`Run`、`IdleToRun`、`RunToIdle`、`DashToIdle`、`WallJump`。
- `PlayerAnimationDriver` 仍然是唯一允许写 Animator 参数和 Trigger 的模块。

## 已完成

### 1. 扩展代码状态枚举

文件：`Assets/Scripts/Interface/IPlayerState.cs`

新增代码状态：

- `WallSlide`
- `Dash`
- `Action`

新增上下文枚举：

- `PlayerJumpKind`：`None` / `Normal` / `Double` / `Wall`
- `PlayerActionKind`：`None` / `Slash`

这一步的目的，是把“跳跃类型”和“动作类型”从散落布尔值里拿出来，避免后续状态进入时再猜当前要做什么。

### 2. 给 PlayerData 增加新状态所需参数

文件：`Assets/Data/Player/PlayerData.cs`

新增字段：

- `dashSpeed`
- `dashDuration`
- `dashCooldown`
- `wallSlideSpeed`
- `wallJumpHorizontalSpeed`
- `wallJumpForce`
- `slashDuration`

这些参数让 Dash、WallSlide、WallJump、Slash 的行为不再硬编码在状态类里。

### 3. 扩展动画驱动层

文件：`Assets/Scripts/Player/PlayerAnimationDriver.cs`

已新增：

- `Dash` Trigger 哈希和 `TriggerDash()`

已改动：

- `TriggerJump()` 现在会先 reset `Jump` / `DoubleJump`
- `TriggerSlash()` 会先 reset `Slash`
- `TriggerHurt()` 会先 reset `Hurt`

这样做的目的是减少 Trigger 残留带来的排查歧义。

### 4. 重构 PlayerController

文件：`Assets/Scripts/Player/PlayerController.cs`

已完成的结构调整：

- 新增调试开关：`enableStateDebugLogs`
- 新增挂起上下文：`_pendingJumpKind`、`_pendingActionKind`
- 新增 Dash 冷却字段：`_nextDashReadyTime`
- 新增公开事实：`CanWallSlide`、`FacingDirectionX`、`DefaultGravityScale`
- `Update()` 顺序改为：采样输入 -> 地面检测 -> 墙体检测 -> 状态机 Update -> Animator 同步

输入处理已改为“请求状态切换”而不是“直接裸写动画”：

- `OnJumpPerformed()`：根据当前状态和墙体/地面情况决定 `PlayerJumpKind`
- `OnDashPerformed()`：只在 `Movement` 相位进入 `Dash`
- `OnAttackPerformed()`：不再直接触发 Slash，而是进入 `Action`

新的状态注册已接入：

- `PlayerWallSlideState`
- `PlayerDashState`
- `PlayerActionState`

另外：

- `TakeDamage()` 现在会清空挂起 Jump/Action 上下文，避免受击后残留请求污染状态切换。

### 5. 重构状态基类

文件：`Assets/Scripts/Player/PlayerState/PlayerStateBase.cs`

已新增：

- `ReturnToLocomotionState()`

行为：

- 在地面返回 `Movement`
- 离地且可贴墙时返回 `WallSlide`
- 其余情况返回 `Fall`

已新增：

- `FaceWorldDirection(float direction)`

目的：

- 统一处理 Knight 默认朝左时的翻转逻辑
- 供 Dash / WallJump 这种“按世界方向强制朝向”的状态使用

### 6. 重构现有状态

#### PlayerMovementState

文件：`Assets/Scripts/Player/PlayerState/PlayerMovementState.cs`

现在负责：

- 地面移动
- 进入时重置跳跃次数
- 离地时根据情况进入 `Fall` 或 `WallSlide`

#### PlayerJumpState

文件：`Assets/Scripts/Player/PlayerState/PlayerJumpState.cs`

现在负责：

- 管理跳跃次数
- 区分普通跳、二段跳、墙跳
- 在 `Enter()` 根据 `PlayerJumpKind` 施加一次性起跳速度
- 下降时切 `Fall`
- 贴墙下落时切 `WallSlide`
- 落地时切 `Land`

新增的核心方法：

- `TryConsumeStandardJump(bool isGrounded, out PlayerJumpKind jumpKind)`
- `TryConsumeWallJump(out PlayerJumpKind jumpKind)`
- `RemainingJumps`

#### PlayerFallState

文件：`Assets/Scripts/Player/PlayerState/PlayerFallState.cs`

现在负责：

- 纯下落相位
- 落地时进 `Land`
- 贴墙下落时进 `WallSlide`

#### PlayerLandState

文件：`Assets/Scripts/Player/PlayerState/PlayerLandState.cs`

现在负责：

- 落地恢复窗口
- 落地时重置跳跃次数
- 离地则回 `Fall`
- 计时结束则回到可移动相位

#### PlayerHurtState

文件：`Assets/Scripts/Player/PlayerState/PlayerHurtState.cs`

已改为：

- 硬直结束后返回 `ReturnToLocomotionState()`

#### PlayerDeadState

文件：`Assets/Scripts/Player/PlayerState/PlayerDeadState.cs`

已修正文案错误，行为保持终止态。

### 7. 新增状态文件

#### PlayerActionState

已修复：

- 早期版本里 `CanTransitionTo()` 只允许 `Hurt/Dead`，会导致 Slash 结束后永远卡在 `Action`
- 当前版本改为：锁定期间只允许 `Hurt/Dead` 打断，锁定结束后允许退出到可移动相位

已修复：

- 早期版本在 `Land` 中没有持续驱动水平速度，地面摩擦可能让角色一进 `Land` 就明显掉速甚至接近 0
- 当前版本在 `Land.FixedUpdate()` 中继续执行 `SmoothSpeed()`，并保留一个很短的落地窗口

文件：`Assets/Scripts/Player/PlayerState/PlayerActionState.cs`

当前用途：

- 承接 `Slash`
- 用 `PlayerActionKind` 决定动作类型
- 通过 `slashDuration` 作为动作锁定窗口
- 动作结束后返回可移动相位

注意：

- 当前 `Enter()` 中不再重新决定动作逻辑，只消费 `PlayerController` 事先写好的挂起上下文。

#### PlayerDashState

文件：`Assets/Scripts/Player/PlayerState/PlayerDashState.cs`

当前用途：

- 处理地面 Dash
- 在 Dash 期间把重力置零
- 按固定方向和速度维持 Dash
- 结束后恢复重力并回到可移动相位

#### PlayerWallSlideState

文件：`Assets/Scripts/Player/PlayerState/PlayerWallSlideState.cs`

当前用途：

- 在贴墙离地下滑时接管状态
- 限制最大下落速度为 `wallSlideSpeed`
- 落地则进 `Land`
- 脱离贴墙条件则回 `Fall`

### 8. 保留旧文件但标记废弃

文件：`Assets/Scripts/Player/PlayerState/PlayerAttackState.cs`

当前没有删除该文件，因为本项目规则禁止未经允许删除项目文件。已将其改为说明性占位，明确真正的动作状态已迁移到 `PlayerActionState.cs`。

## 当前代码状态机结构

### 代码层状态

- `Movement`
- `Jump`
- `Fall`
- `Land`
- `WallSlide`
- `Dash`
- `Action`
- `Hurt`
- `Dead`

### 仍然只存在于 Animator 的表现状态

- `Idle`
- `Run`
- `IdleToRun`
- `RunToIdle`
- `DashToIdle`
- `WallJump`
- `Slash` 的具体表现片段

也就是说：

- 代码只负责“我现在处于什么逻辑相位”
- Animator 负责“这个相位现在该播哪一段具体动画”

## 与 Knight.controller 的当前对齐关系

已核对到的事实：

- `Knight.controller` 有 `SubSM Grounded`
- `Knight.controller` 有 `SubSM Airborne`
- `Knight.controller` 有 `SubSM Action`
- 触发器参数包括：`Jump`、`DoubleJump`、`Dash`、`Slash`、`Hurt`
- 连续参数包括：`HorizontalSpeed`、`VerticalSpeed`、`IsGround`、`IsTouchWall`、`WallDownSpeed`、`IsDead`

当前代码已经开始按这个结构喂数据：

- Dash 通过 `TriggerDash()` 驱动
- Slash 通过 `ActionState` 触发 `TriggerSlash()`
- WallSlide 通过 `IsTouchWall + WallDownSpeed` 驱动
- WallJump 仍由 Animator 根据 `Jump Trigger + 墙体条件` 自己过渡，不额外做 1:1 的 C# 状态

## 已完成的验证

已完成：

- 所有本次修改文件的脚本级错误检查通过
- 全工程错误检查通过

这说明：

- 新增枚举没有打断现有引用
- 新增状态类、控制器改动和动画驱动改动可以通过当前 C# 编译层检查

## 还需要做的事

### 1. 在 Unity Play Mode 做真实行为验证

重点验证以下链路：

1. `Movement -> Jump -> Fall -> Land -> Movement`
2. `Movement -> Jump -> Jump(Double) -> Fall -> Land -> Movement`
3. `Fall -> WallSlide -> Jump(Wall) -> Fall`
4. `Movement -> Dash -> Movement`
5. `Movement/Jump/Fall -> Action(Slash) -> Movement/Fall/WallSlide`
6. `Any -> Hurt -> Locomotion`
7. `Any -> Dead`

### 2. 重点复测最初报告的问题

必须重点复测：

- `DoubleJump -> Fall -> Land -> Idle/Run` 后，角色不能在没有新的跳跃输入时再次进入 `Jump`

如果仍复现，需要开 `enableStateDebugLogs`，然后看：

- 是否真的出现了 `Movement/Action/Land -> Jump` 的代码状态切换日志
- 还是代码状态没切，只有 Animator 视觉上回到了 Jump

如果是后者，就应该继续排查 `Knight.controller` 的 Trigger 消费和过渡条件，而不是再改 C# 状态机。

### 3. 在 Inspector / 数据资产里补配置

当前新增参数已经在 `PlayerData` 中可用，但需要去对应的 `PlayerData` 资产里检查和调参：

- `dashSpeed`
- `dashDuration`
- `dashCooldown`
- `wallSlideSpeed`
- `wallJumpHorizontalSpeed`
- `wallJumpForce`
- `slashDuration`

如果项目使用 JSON 覆盖 `PlayerData`，也要确认 JSON 是否需要同步这些字段。

### 4. 视手感决定是否继续细化

当前实现故意保持轻量，还没有做这些扩展：

- Slash 动作结束由动画事件驱动
- Dash 无敌帧
- Dash 空中可用或次数限制
- WallSlide 朝向锁定策略
- ActionState 的更细 cancel window
- Jump buffer / coyote time

这些都可以在现有结构上继续加，不需要再推翻整个状态机。

## 后续 AI 接手建议

如果下一个 AI 要继续做，请按下面顺序：

1. 先在 Unity 里开 `enableStateDebugLogs` 跑 Play Mode，确认原始 bug 是否已消失。
2. 如果 bug 仍在，先区分“代码状态真的进 Jump”还是“只是 Animator 看起来进 Jump”。
3. 再根据 Play Mode 结果决定要不要继续改 `Knight.controller` 里的 Jump / DoubleJump / Land 过渡条件。
4. 最后再做手感层优化，不要直接把手感调参与结构性问题混在一起处理。
