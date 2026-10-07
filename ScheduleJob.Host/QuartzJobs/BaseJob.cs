using Quartz;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Repositorys;
using ScheduleJob.Public.Models;
using System;
using System.Threading.Tasks;

namespace ScheduleJob.Host.QuartzJobs
{
    /// <summary>
    /// 定时任务业务基类 - 提供日志、监控状态记录等通用能力
    /// </summary>
    public abstract class BaseJob : IJob
    {
        protected readonly AuthConfig _authConfig;
        private readonly IScheduleJobService _scheduleJobService;
        private readonly IJobMonitorStateRepository _stateRepository;

        protected BaseJob(
            AuthConfig authConfig,
            IScheduleJobService scheduleJobService,
            IJobMonitorStateRepository stateRepository)
        {
            _authConfig = authConfig;
            _scheduleJobService = scheduleJobService;
            _stateRepository = stateRepository;
        }

        /// <summary>
        /// Quartz 执行入口
        /// </summary>
        public virtual async Task Execute(IJobExecutionContext context)
        {
            try
            {
                // 执行实际业务逻辑
                await ExecuteInternalAsync(context);
            }
            catch (Exception ex)
            {
                // 记录异常
                await LogExceptionAsync(context, GetTaskName(), ex);
            }
        }

        /// <summary>
        /// 子类必须实现的实际执行逻辑
        /// </summary>
        protected abstract Task ExecuteInternalAsync(IJobExecutionContext context);

        /// <summary>
        /// 获取任务名称（默认为类名）
        /// </summary>
        protected virtual string GetTaskName()
        {
            return GetType().Name;
        }

        /// <summary>
        /// 记录异常日志
        /// </summary>
        protected virtual async Task LogExceptionAsync(IJobExecutionContext context, string taskName, Exception ex)
        {
            if (_scheduleJobService != null)
            {
                await _scheduleJobService.AddLogAsync(
                    _authConfig?.ClientCode ?? "",
                    taskName,
                    $"任务 [{taskName}] 执行异常: {ex.Message}\r\n{ex.StackTrace}",
                    true);
            }
        }

        /// <summary>
        /// 记录日志（子类可直接调用）
        /// </summary>
        /// <param name="log">日志内容</param>
        /// <param name="isException">是否为异常日志</param>
        protected async Task AddLogAsync(string log, bool isException = false)
        {
            if (_scheduleJobService != null)
            {
                await _scheduleJobService.AddLogAsync(
                    _authConfig?.ClientCode ?? "",
                    GetTaskName(),
                    log,
                    isException);
            }
        }

        /// <summary>
        /// 查询上次监控状态（失败不影响监控主流程）
        /// </summary>
        /// <param name="clientCode">客户端编号</param>
        /// <param name="taskName">任务名称</param>
        /// <returns>状态</returns>
        protected async Task<JobMonitorState> GetMonitorStateAsync(string clientCode, string taskName)
        {
            try
            {
                return await _stateRepository.GetAsync(clientCode, taskName);
            }
            catch (Exception ex)
            {
                await AddLogAsync($"查询监控状态失败: {ex.Message}", true);
                return null;
            }
        }

        /// <summary>
        /// 记录监控状态（无记录则新增，否则更新；上次异常、本次正常时自动记录恢复时间）
        /// </summary>
        /// <param name="state">上次监控状态（可空）</param>
        /// <param name="clientCode">客户端编号</param>
        /// <param name="taskName">任务名称</param>
        /// <param name="status">本次监控状态</param>
        /// <param name="errorMessage">异常消息（本次异常时传入）</param>
        /// <param name="failCount">连续异常次数（本次异常时传入）</param>
        protected async Task AddStateAsync(
            JobMonitorState state,
            string clientCode,
            string taskName,
            JobMonitorStatusEnum status,
            string errorMessage = null,
            int failCount = 0)
        {
            try
            {
                var now = DateTime.UtcNow;
                var isAbnormal = status == JobMonitorStatusEnum.Abnormal;
                var lastError = isAbnormal ? errorMessage : null;
                var lastFailCount = isAbnormal ? failCount : 0;

                if (state == null)
                {
                    var newState = new JobMonitorState
                    {
                        Id = Guid.NewGuid(),
                        ClientCode = clientCode,
                        TaskName = taskName,
                        LastStatus = status,
                        LastErrorMessage = lastError,
                        FailCount = lastFailCount,
                        LastFailTime = isAbnormal ? now : (DateTime?)null,
                        CreateTime = now,
                        UpdateTime = now
                    };
                    await _stateRepository.AddAsync(newState);
                }
                else
                {
                    var isRecovered = state.LastStatus == JobMonitorStatusEnum.Abnormal && !isAbnormal;
                    state.LastStatus = status;
                    state.LastErrorMessage = lastError;
                    state.FailCount = lastFailCount;
                    if (isAbnormal)
                    {
                        state.LastFailTime = now;
                    }
                    if (isRecovered)
                    {
                        state.LastRecoverTime = now;
                    }
                    state.UpdateTime = now;
                    await _stateRepository.UpdateAsync(state);
                }
            }
            catch (Exception ex)
            {
                await AddLogAsync($"记录监控状态失败: {ex.Message}", true);
            }
        }
    }
}
