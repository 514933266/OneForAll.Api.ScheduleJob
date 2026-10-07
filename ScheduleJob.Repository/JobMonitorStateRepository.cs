using Microsoft.EntityFrameworkCore;
using OneForAll.EFCore;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Repositorys;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Repository
{
    /// <summary>
    /// 监控状态
    /// </summary>
    public class JobMonitorStateRepository : Repository<JobMonitorState>, IJobMonitorStateRepository
    {
        public JobMonitorStateRepository(DbContext context)
            : base(context)
        {
        }

        /// <summary>
        /// 查询单条监控状态
        /// </summary>
        /// <param name="clientCode">客户端编号</param>
        /// <param name="taskName">任务名称</param>
        /// <returns>状态</returns>
        public async Task<JobMonitorState> GetAsync(string clientCode, string taskName)
        {
            return await DbSet.FirstOrDefaultAsync(w => w.ClientCode == clientCode && w.TaskName == taskName);
        }
    }
}
