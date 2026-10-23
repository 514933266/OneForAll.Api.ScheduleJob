using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Interfaces;
using ScheduleJob.Domain.Repositorys;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Querys
{
    /// <summary>
    /// 任务异常通知配置查询服务（只读，返回实体，不含 DTO）
    /// </summary>
    public class JobNotificationConfigQueryService : IJobNotificationConfigQueryService
    {
        private readonly IJobNotificationConfigRepository _repository;

        public JobNotificationConfigQueryService(IJobNotificationConfigRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// 查询全部通知配置
        /// </summary>
        /// <returns>列表</returns>
        public async Task<IEnumerable<JobNotificationConfig>> GetListAsync()
        {
            return await _repository.GetListAsync();
        }
    }
}
