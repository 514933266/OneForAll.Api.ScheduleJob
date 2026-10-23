using OneForAll.Core;
using ScheduleJob.Domain.Entities;

namespace ScheduleJob.Domain.Interfaces
{
    /// <summary>
    /// 任务异常通知配置查询服务（只读，返回实体，不含 DTO）
    /// </summary>
    public interface IJobNotificationConfigQueryService
    {
        /// <summary>
        /// 查询全部通知配置
        /// </summary>
        /// <returns>列表</returns>
        System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<JobNotificationConfig>> GetListAsync();
    }
}
