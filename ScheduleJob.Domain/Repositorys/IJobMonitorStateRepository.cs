using OneForAll.EFCore;
using ScheduleJob.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Repositorys
{
    /// <summary>
    /// 监控状态
    /// </summary>
    public interface IJobMonitorStateRepository : IEFCoreRepository<JobMonitorState>
    {
        /// <summary>
        /// 查询单条监控状态
        /// </summary>
        /// <param name="clientCode">客户端编号</param>
        /// <param name="taskName">任务名称</param>
        /// <returns>状态</returns>
        Task<JobMonitorState> GetAsync(string clientCode, string taskName);
    }
}
