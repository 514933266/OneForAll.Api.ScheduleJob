using OneForAll.Core;
using OneForAll.EFCore;
using ScheduleJob.Domain.AggregateRoots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Repositorys
{
    /// <summary>
    /// 定时任务
    /// </summary>
    public interface IJobTaskRepository : IEFCoreRepository<JobTask>
    {
        /// <summary>
        /// 查询分页
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页数</param>
        /// <param name="key">关键字</param>
        /// <param name="groupName">所属分组</param>
        /// <param name="nodeName">所属节点</param>
        /// <returns>分页列表</returns>
        Task<PageList<JobTask>> GetPageAsync(int pageIndex, int pageSize, string key, string groupName, string nodeName);

        /// <summary>
        /// 查询列表
        /// </summary>
        /// <param name="ids">id集合</param>
        /// <returns>列表</returns>
        Task<IEnumerable<JobTask>> GetListAsync(IEnumerable<Guid> ids);
    }
}
