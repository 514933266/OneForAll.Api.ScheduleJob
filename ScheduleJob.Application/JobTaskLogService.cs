using AutoMapper;
using OneForAll.Core;
using ScheduleJob.Application.Dtos;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.AggregateRoots;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Application
{
    /// <summary>
    /// 定时任务日志
    /// </summary>
    public class JobTaskLogService : IJobTaskLogService
    {
        private readonly IMapper _mapper;
        private readonly IJobTaskLogManager _manager;
        public JobTaskLogService(
            IMapper mapper,
            IJobTaskLogManager manager)
        {
            _mapper = mapper;
            _manager = manager;
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
        public async Task<PageList<JobTaskLogDto>> GetPageAsync(
            int pageIndex,
            int pageSize,
            string appId,
            string taskName,
            JobTaskLogTypeEnum type,
            string key,
            DateTime? beiginTime,
            DateTime? endTime)
        {
            var data = await _manager.GetPageAsync(pageIndex, pageSize, appId, taskName, type, key, beiginTime, endTime);
            var items = _mapper.Map<IEnumerable<JobTaskLog>, IEnumerable<JobTaskLogDto>>(data.Items);
            return new PageList<JobTaskLogDto>(data.Total, data.PageSize, data.PageIndex, items);
        }
    }
}
