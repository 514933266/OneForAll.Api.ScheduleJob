# OneForAll.Api.ScheduleJob

基于 ASP.NET Core 8.0 + Quartz.NET 构建的分布式定时任务调度服务，提供任务注册、监控、告警及管理功能。

## 技术栈

- **.NET 8.0** / ASP.NET Core
- **Quartz.NET 3.15** - 任务调度引擎
- **Entity Framework Core 8.0** - ORM（SQL Server）
- **Autofac 9.0** - 依赖注入
- **AutoMapper 16.0** - 对象映射
- **JWT Bearer** - 身份认证

## 项目结构

```
OneForAll.Api.ScheduleJob/
├── ScheduleJob.Host/            # Web 主机层（API 入口、Quartz 配置、过滤器）
├── ScheduleJob.Application/     # 应用服务层（业务用例、DTO）
├── ScheduleJob.Domain/          # 领域层（实体、枚举、业务逻辑）
├── ScheduleJob.Repository/      # 数据访问层（EF Core 仓储）
├── ScheduleJob.HttpService/     # 外部 HTTP 服务客户端
└── ScheduleJob.Public/          # 公共模型
```

## 核心功能

### 任务注册与生命周期管理

远程应用通过 OpenAPI 接口注册定时任务，服务端维护任务的完整生命周期：

```
注册(Register) → 运行(Running) → 心跳(Heartbeat) → 下线(Downline)
```

### 任务状态

| 状态 | 说明 |
|------|------|
| Unstart | 未启动 / 已暂停 |
| Running | 运行中 |
| Error | 异常（节点失联或执行失败） |

### 运行模式

- **Center 模式** - 作为调度中心，统一管理和监控分布式任务节点
- **Job 模式** - 作为任务执行节点，向调度中心上报状态

### 内置调度任务

| 任务 | 周期 | 说明 |
|------|------|------|
| DeleteTaskLogJob | 每天 | 清理过期日志（心跳日志 1 天、运行日志 7 天、异常日志 15 天） |
| MonitorTaskStatusJob | 每 5 分钟 | 监控任务健康状态，检测失联节点并发送告警通知 |

### 并发控制机制

所有内置定时任务继承自 `BaseLockJob` 基类，基类自动处理分布式环境下的并发控制，防止同一任务在多个实例上同时执行。

#### 核心设计

采用**双表 + 数据库锁 + 乐观锁**三层防护机制：

| 表名 | 作用 |
|------|------|
| `job_running_lock` | 锁配置与运行状态（最大并发数、当前运行数、版本号） |
| `job_lock_holder` | 锁持有者记录（利用唯一索引 `(ClientId, TaskName, Version)` 防止重复获取） |

#### 执行流程

```
获取锁 → 执行业务逻辑 → 释放锁
  │                        │
  └── 失败则跳过本次执行    └── 减少计数 + 删除持有者记录
```

**1. 获取锁（TryAcquireLockAsync）**

- 使用 SQL Server `WITH (UPDLOCK, ROWLOCK)` 排他行锁查询锁配置，防止多个事务同时读取
- 若锁配置不存在，自动创建默认配置（允许 1 个并发）
- 若 `IsEnabled = false`，跳过并发控制，直接允许执行
- 若 `CurrentRunningCount >= MaxConcurrent`，返回失败，跳过本次执行
- 事务内执行两步原子操作：
  - **INSERT** `job_lock_holder` —— 利用唯一索引作为真正的分布式锁，极限并发下只有一个实例能插入成功
  - **UPDATE** `job_running_lock`（带 `WHERE Version = @originalVersion` 条件）—— 乐观锁作为二次校验

**2. 执行（ExecuteInternalAsync）**

- 子类必须实现的抽象方法，编写具体业务逻辑
- 若执行中抛出异常，自动记录异常日志

**3. 释放锁（ReleaseLockAsync + RemoveLockHolderAsync）**

- `finally` 块中确保锁一定被释放
- 减少 `CurrentRunningCount` 计数（无需乐观锁，因为只有持有者会操作）
- 删除对应的 `job_lock_holder` 记录

#### 并发竞争示意

```
实例A: SELECT(V=5) → INSERT Holder ✓ → UPDATE WHERE V=5 ✓ → 获取成功
实例B: SELECT(V=5) → INSERT Holder ✗（唯一约束冲突）        → 获取失败
```

#### 关键字段说明

| 字段 | 说明 |
|------|------|
| `MaxConcurrent` | 最大并发数，默认 1（同一时刻最多 1 个实例执行） |
| `CurrentRunningCount` | 当前正在运行的实例数 |
| `Version` | 乐观锁版本号，每次成功获取锁时递增 |
| `IsEnabled` | 是否启用并发控制，设为 false 可跳过锁检查 |

### 监控与告警

- 基于心跳检测节点存活状态
- 基于 Cron 表达式计算预期执行时间，对比实际执行时间判断异常
- HTTP 探针主动探测任务节点（`/api/Startups`）
- 支持**企业微信机器人**和**钉钉机器人**告警通知

## API 接口

### OpenAPI（无需认证）

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/ScheduleJobs` | 注册定时任务 |
| PATCH | `/api/ScheduleJobs/{appId}/{taskName}/Heartbeats` | 发送心跳 |
| DELETE | `/api/ScheduleJobs/{appId}/{taskName}` | 任务下线 |
| POST | `/api/ScheduleJobs/{appId}/{taskName}/Logs` | 上报执行日志 |

### 管理接口（需 JWT 认证）

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/JobTasks/{pageIndex}/{pageSize}` | 分页查询任务列表 |
| PATCH | `/api/JobTasks/{id}/IsEnabled` | 启用 / 禁用任务 |
| PATCH | `/api/JobTasks/Batch/IsDeleted` | 批量删除任务 |
| PATCH | `/api/JobTasks/{id}/Status` | 变更任务状态（启动 / 暂停） |
| POST | `/api/JobTasks/{id}/Excute` | 立即执行一次 |
| POST | `/api/JobTasks/{id}/Persons` | 分配任务负责人 |
| GET | `/api/JobTaskLogs/{pageIndex}/{pageSize}` | 分页查询执行日志 |

## 数据库

使用 SQL Server，数据库名 `OneForAll.ScheduleJob`，主要表：

| 表名 | 说明 |
|------|------|
| job_task | 任务定义（应用ID、名称、Cron、状态、节点信息等） |
| job_task_log | 执行日志（类型：注册/上线/下线/运行/心跳/异常） |
| job_notification_config | 告警通知配置（企业微信/钉钉 Webhook） |
| job_mid_task_person | 任务负责人关联 |
| job_running_lock | 定时任务运行锁（并发控制配置与计数） |
| job_lock_holder | 锁持有者记录（唯一索引防重复获取） |

## 配置说明

`appsettings.json` 关键配置项：

```json
{
  "Urls": ["http://*:5087"],
  "ConnectionStrings": {
    "Default": "SQL Server 连接字符串"
  },
  "Auth": {
    "JwtKey": "JWT 签名密钥",
    "Issuer": "令牌签发地址",
    "ClientId": "客户端ID",
    "ClientSecret": "客户端密钥"
  },
  "Quartz": {
    "IsEnabled": false,
    "Mode": "Center",
    "AppId": "OneForAll.ScheduleJob",
    "GroupName": "定时任务调度",
    "NodeName": "本地",
    "ScheduleJobs": [
      {
        "TypeName": "DeleteTaskLogJob",
        "Cron": "0 0 0 1/1 * ?",
        "Data": "{\"HeartbeatDays\":1,\"RunningDays\":3,\"ErrorDays\":3}"
      },
      {
        "TypeName": "MonitorTaskStatusJob",
        "Cron": "0 0/5 * * * ?"
      }
    ]
  }
}
```

| 配置项 | 说明 |
|--------|------|
| `Quartz.IsEnabled` | 是否启用 Quartz 调度器 |
| `Quartz.Mode` | 运行模式：`Center`（调度中心）/ `Job`（执行节点） |
| `Quartz.AppId` | 当前应用标识 |
| `Quartz.ScheduleJobs` | 内置调度任务列表，每项包含类名、Cron 表达式和可选参数 |

## 外部服务依赖

本服务作为 OneForAll 微服务体系的一部分，依赖以下服务：

| 服务 | 配置键 | 说明 |
|------|--------|------|
| SysBase | `HttpService.SysBase` | 基础系统服务 |
| SysLog | `HttpService.SysLog` | 日志服务 |
| SysUms | `HttpService.SysUms` | 消息服务（发送企业微信/钉钉通知） |
| OA | `HttpService.OA` | OA 系统（人员信息） |

## 启动运行

1. 确保 SQL Server 已就绪，并更新 `appsettings.json` 中的连接字符串
2. 配置 JWT 认证参数（与统一认证中心一致）
3. 按需配置 Quartz 调度参数

```bash
dotnet restore
dotnet run --project ScheduleJob.Host
```

服务默认监听 `http://*:5087`。
