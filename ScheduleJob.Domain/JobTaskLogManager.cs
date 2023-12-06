using AutoMapper;
using Microsoft.AspNetCore.Http;
using OneForAll.Core;
using OneForAll.Core.Extension;
using ScheduleJob.Domain.AggregateRoots;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Interfaces;
using ScheduleJob.Domain.Models;
using ScheduleJob.Domain.Repositorys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain
{
    /// <summary>
    /// 定时任务
    /// </summary>
    public class JobTaskLogManager : JobBaseManager, IJobTaskLogManager
    {
        private readonly IJobTaskLogRepository _repository;
        public JobTaskLogManager(
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            IJobTaskLogRepository repository) : base(mapper, httpContextAccessor)
        {
            _repository = repository;
        }

        /// <summary>
        /// 获取分页列表
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页数</param>
        /// <param name="appId">appid</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="type">日志类型</param>
        /// <param name="key">关键字</param>
        /// <param name="beiginTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>分页列表</returns>
        public async Task<PageList<JobTaskLog>> GetPageAsync(
            int pageIndex,
            int pageSize,
            string appId,
            string taskName,
            JobTaskLogTypeEnum type,
            string key,
            DateTime? beiginTime,
            DateTime? endTime)
        {
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;
            return await _repository.GetPageAsync(pageIndex, pageSize, appId, taskName, type, key, beiginTime, endTime);
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
            var data = await _repository.GetListAsync(w => w.CreateTime <= DateTime.Now.AddDays(-7));
            if (data.Any())
            {
                return await ResultAsync(() => _repository.DeleteRangeAsync(data));
            }
            return BaseErrType.Success;
        }
    }
}

