using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OneForAll.EFCore;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Repositorys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Repository
{
    /// <summary>
    /// 负责人
    /// </summary>
    public class JobMidTaskPersonRepository : Repository<JobMidTaskPerson>, IJobMidTaskPersonRepository
    {
        public JobMidTaskPersonRepository(DbContext context)
            : base(context)
        {
        }

        /// <summary>
        /// 查询负责人列表
        /// </summary>
        /// <param name="taskIds">定时任务id</param>
        /// <returns>列表</returns>
        public async Task<IEnumerable<JobMidTaskPerson>> GetListPersonAsync(IEnumerable<Guid> taskIds)
        {
            if (!taskIds.Any())
                return Enumerable.Empty<JobMidTaskPerson>();

            try
            {
                return await DbSet.Where(w => taskIds.Contains(w.JobTaskId)).ToListAsync();
            }
            catch(Exception ex)
            {
                // 低版本Sqlserver存在不兼容OPJSON的情况，改用SQL，后续时机合适时再移除
                var paramNames = Enumerable.Range(0, taskIds.Count()).Select(i => $"@p{i}").ToArray();
                var parameters = taskIds.Select((id, i) => new SqlParameter($"@p{i}", id)).ToArray();

                if (ex.Message.Contains("关键字 'WITH' 附近有语法错误"))
                {
                    var sql = $@"
                        SELECT [Id], [JobTaskId], [OAPersonId]
                        FROM [job_mid_task_person]
                        WHERE [JobTaskId] IN ({string.Join(",", paramNames)})";

                    return await DbSet.FromSqlRaw(sql, parameters).ToListAsync();
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
