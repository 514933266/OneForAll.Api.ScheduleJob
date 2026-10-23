using OneForAll.Core;
using OneForAll.EFCore;
using ScheduleJob.Domain.Entities;
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
        /// 查询列表
        /// </summary>
        /// <param name="ids">id集合</param>
        /// <returns>列表</returns>
        Task<IEnumerable<JobTask>> GetListAsync(IEnumerable<Guid> ids);

        /// <summary>
        /// 按应用程序id+任务名查询（不限启用状态，用于注册）
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="name">任务名称</param>
        /// <returns>实体</returns>
        Task<JobTask> GetByAppAndNameAsync(string appId, string name);

        /// <summary>
        /// 按应用程序id+任务名查询已启用任务
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="name">任务名称</param>
        /// <returns>实体</returns>
        Task<JobTask> GetEnabledByAppAndNameAsync(string appId, string name);

        /// <summary>
        /// 查询已启用任务列表（心跳更新用，taskName 为空则不限定任务名）
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">任务名称</param>
        /// <returns>列表</returns>
        Task<List<JobTask>> GetListEnabledAsync(string appId, string taskName);

        /// <summary>
        /// 查询已启用列表
        /// </summary>
        /// <returns>列表</returns>
        Task<IEnumerable<JobTask>> GetListEnabledAsync(bool asNoTracking);

        /// <summary>
        /// 查询所有任务列表（用于锁状态检查等需要全量任务信息的场景）
        /// </summary>
        /// <param name="asNoTracking">是否不跟踪</param>
        /// <returns>列表</returns>
        Task<IEnumerable<JobTask>> GetListAllAsync(bool asNoTracking);
    }
}
