using ScheduleJob.Application.Dtos;
using System.Threading.Tasks;

namespace ScheduleJob.Application.Interfaces
{
    /// <summary>
    /// 定时任务状态巡检（供调度任务调用）
    /// </summary>
    public interface IJobTaskMonitorService
    {
        /// <summary>
        /// 巡检所有启用中的定时任务：超过预期执行宽限期的主动探测节点存活，异常任务标记为错误状态
        /// </summary>
        /// <returns>巡检结果（含执行日志）</returns>
        Task<JobTaskMonitorDto> InspectAsync();
    }
}
