using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Enums;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Interfaces
{
    /// <summary>
    /// 定时任务监控状态（记录各任务上次巡检结果、连续失败次数与恢复时间）
    /// </summary>
    public interface IJobMonitorStateManager
    {
        /// <summary>
        /// 查询上次监控状态（失败不影响监控主流程）
        /// </summary>
        /// <param name="clientCode">客户端编号</param>
        /// <param name="taskName">任务名称</param>
        /// <returns>状态</returns>
        Task<JobMonitorState> GetStateAsync(string clientCode, string taskName);

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
        Task<bool> SaveStateAsync(
            JobMonitorState state,
            string clientCode,
            string taskName,
            JobMonitorStatusEnum status,
            string errorMessage = null,
            int failCount = 0);
    }
}
