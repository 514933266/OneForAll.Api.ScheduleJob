using OneForAll.Core;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Interfaces;
using ScheduleJob.Domain.Repositorys;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Querys
{
    /// <summary>
    /// 定时任务查询服务（只读，返回实体，不含 DTO）
    /// </summary>
    public class JobTaskQueryService : IJobTaskQueryService
    {
        private readonly IJobTaskRepository _repository;
        private readonly IJobMidTaskPersonRepository _contactRepository;

        public JobTaskQueryService(
            IJobTaskRepository repository,
            IJobMidTaskPersonRepository contactRepository)
        {
            _repository = repository;
            _contactRepository = contactRepository;
        }

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
        public async Task<PageList<JobTask>> GetPageAsync(
            int pageIndex,
            int pageSize,
            string key,
            string groupName,
            string nodeName,
            string appId)
        {
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;
            return await _repository.GetPageAsync(pageIndex, pageSize, key, groupName, nodeName, appId);
        }

        /// <summary>
        /// 按id查询
        /// </summary>
        /// <param name="id">任务id</param>
        /// <returns>实体</returns>
        public async Task<JobTask> GetByIdAsync(Guid id)
        {
            return await _repository.FindAsync(id);
        }

        /// <summary>
        /// 查询负责人列表
        /// </summary>
        /// <param name="taskIds">定时任务id</param>
        /// <returns>列表</returns>
        public async Task<IEnumerable<JobMidTaskPerson>> GetListPersonAsync(IEnumerable<Guid> taskIds)
        {
            return await _contactRepository.GetListPersonAsync(taskIds);
        }

        /// <summary>
        /// 查询启用中的定时任务
        /// </summary>
        /// <param name="asNoTracking">是否非跟踪</param>
        /// <returns>列表</returns>
        public async Task<IEnumerable<JobTask>> GetListEnabledAsync(bool asNoTracking)
        {
            return await _repository.GetListEnabledAsync(asNoTracking);
        }
    }
}
