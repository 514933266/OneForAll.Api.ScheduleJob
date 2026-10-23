using AutoMapper;
using OneForAll.Core;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Interfaces;
using ScheduleJob.Domain.Models;
using ScheduleJob.Domain.Repositorys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Managers
{
    /// <summary>
    /// 定时任务日志
    /// </summary>
    public class JobTaskLogManager : ScheduleJobDomainManager, IJobTaskLogManager
    {
        private readonly IMapper _mapper;
        private readonly IJobTaskLogRepository _repository;
        public JobTaskLogManager(
            IMapper mapper,
            ICurrentUser currentUser,
            IJobTaskLogRepository repository) : base(currentUser)
        {
            _mapper = mapper;
            _repository = repository;
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="form">表单</param>
        /// <returns>结果</returns>
        public async Task<BaseErrType> AddAsync(JobTaskLogForm form)
        {
            var data = _mapper.Map<JobTaskLogForm, JobTaskLog>(form);
            return await ResultAsync(() => _repository.AddAsync(data));
        }

        /// <summary>
        /// 删除7天前的日志
        /// </summary>
        /// <returns>结果</returns>
        public async Task<BaseErrType> DeleteListSevenDayAsync()
        {
            var data = await _repository.GetListCreatedBeforeAsync(DateTime.UtcNow.AddDays(-7));
            if (data.Any())
            {
                return await ResultAsync(() => _repository.DeleteRangeAsync(data));
            }
            return BaseErrType.Success;
        }

        /// <summary>
        /// 按类型分批删除截止时间之前的日志
        /// </summary>
        /// <param name="type">日志清理类型</param>
        /// <param name="cutoff">截止时间（UTC）</param>
        /// <param name="batchSize">单批删除上限</param>
        /// <returns>本批删除数量</returns>
        public async Task<int> ClearBatchAsync(JobTaskLogClearTypeEnum type, DateTime cutoff, int batchSize)
        {
            var take = batchSize > 0 ? batchSize : 500;

            IEnumerable<JobTaskLog> data;
            switch (type)
            {
                case JobTaskLogClearTypeEnum.Running:
                    data = await _repository.GetListRunningBeforeAsync(cutoff, take);
                    break;
                case JobTaskLogClearTypeEnum.Error:
                    data = await _repository.GetListErrorBeforeAsync(cutoff, take);
                    break;
                default:
                    data = await _repository.GetListNormalBeforeAsync(cutoff, take);
                    break;
            }

            var list = data?.ToList() ?? new List<JobTaskLog>();
            if (!list.Any()) return 0;

            return await _repository.BulkDeleteAsync(list);
        }
    }
}
