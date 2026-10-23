using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Interfaces;
using ScheduleJob.Domain.Repositorys;
using System;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Managers
{
    /// <summary>
    /// 定时任务监控状态
    /// </summary>
    public class JobMonitorStateManager : ScheduleJobDomainManager, IJobMonitorStateManager
    {
        private readonly IJobMonitorStateRepository _repository;

        public JobMonitorStateManager(
            ICurrentUser currentUser,
            IJobMonitorStateRepository repository) : base(currentUser)
        {
            _repository = repository;
        }

        /// <summary>
        /// 查询上次监控状态（失败不影响监控主流程）
        /// </summary>
        /// <param name="clientCode">客户端编号</param>
        /// <param name="taskName">任务名称</param>
        /// <returns>状态</returns>
        public async Task<JobMonitorState> GetStateAsync(string clientCode, string taskName)
        {
            try
            {
                return await _repository.GetAsync(clientCode, taskName);
            }
            catch
            {
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
        /// <returns>是否成功</returns>
        public async Task<bool> SaveStateAsync(
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
                    await _repository.AddAsync(newState);
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
                    await _repository.UpdateAsync(state);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
