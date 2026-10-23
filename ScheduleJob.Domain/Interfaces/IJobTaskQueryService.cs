using OneForAll.Core;
using ScheduleJob.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Interfaces
{
    /// <summary>
    /// 定时任务查询服务（只读，返回实体，不含 DTO）
    /// </summary>
    public interface IJobTaskQueryService
    {
        /// <summary>
        /// 获取分页列表
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页数</param>
        /// <param name="key">关键字</param>
        /// <param name="groupName">所属分组</param>
        /// <param name="nodeName">所属节点</param>
        /// <param name="appId">应用程序id</param>
        /// <returns>分页列表</returns>
        Task<PageList<JobTask>> GetPageAsync(
            int pageIndex,
            int pageSize,
            string key,
            string groupName,
            string nodeName,
            string appId);

        /// <summary>
        /// 按id查询
        /// </summary>
        /// <param name="id">任务id</param>
        /// <returns>实体</returns>
        Task<JobTask> GetByIdAsync(Guid id);

        /// <summary>
        /// 查询负责人列表
        /// </summary>
        /// <param name="taskIds">定时任务id</param>
        /// <returns>列表</returns>
        Task<IEnumerable<JobMidTaskPerson>> GetListPersonAsync(IEnumerable<Guid> taskIds);

        /// <summary>
        /// 查询启用中的定时任务
        /// </summary>
        /// <param name="asNoTracking">是否非跟踪</param>
        /// <returns>列表</returns>
        Task<IEnumerable<JobTask>> GetListEnabledAsync(bool asNoTracking);
    }
}
