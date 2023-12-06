using AutoMapper;
using OneForAll.Core;
using ScheduleJob.Application.Dtos;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.AggregateRoots;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Interfaces;
using ScheduleJob.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Application
{
    /// <summary>
    /// 定时任务
    /// </summary>
    public class JobTaskService : IJobTaskService
    {
        private readonly IMapper _mapper;
        private readonly IJobTaskManager _manager;
        private readonly IJobTaskLogManager _logManager;
        public JobTaskService(
            IMapper mapper,
            IJobTaskManager manager,
            IJobTaskLogManager logManager)
        {
            _mapper = mapper;
            _manager = manager;
            _logManager = logManager;
        }

        /// <summary>
        /// 获取分页列表
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页数</param>
        /// <param name="key">关键字</param>
        /// <param name="groupName">所属分组</param>
        /// <param name="nodeName">所属节点</param>
        /// <returns>分页列表</returns>
        public async Task<PageList<JobTaskDto>> GetPageAsync(int pageIndex, int pageSize, string key, string groupName, string nodeName)
        {
            var data = await _manager.GetPageAsync(pageIndex, pageSize, key, groupName, nodeName);
            var items = _mapper.Map<IEnumerable<JobTask>, IEnumerable<JobTaskDto>>(data.Items);
            return new PageList<JobTaskDto>(data.Total, data.PageSize, data.PageIndex, items);
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="form">表单</param>
        /// <returns>结果</returns>
        public async Task<BaseErrType> RegisterAsync(JobTaskRegisterForm form)
        {
            var errType = await _manager.RegisterAsync(form);
            if (errType == BaseErrType.Success)
            {
                await _logManager.AddAsync(new JobTaskLogForm()
                {
                    AppId = form.AppId,
                    TaskName = form.Name,
                    Content = $"定时任务{(form.Id == Guid.Empty ? "注册" : "上线")}，运行节点【{form.NodeName}】",
                    Type = form.Id == Guid.Empty ? JobTaskLogTypeEnum.Register : JobTaskLogTypeEnum.Online
                });
            }
            return errType;
        }

        /// <summary>
        /// 更改心跳状态
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <returns>结果</returns>
        public async Task<BaseErrType> HeartbeatAsync(string appId, string taskName)
        {
            var errType = await _manager.HeartbeatAsync(appId, taskName);
            if (errType == BaseErrType.Success)
            {
                await _logManager.AddAsync(new JobTaskLogForm()
                {
                    AppId = appId,
                    TaskName = taskName,
                    Content = "定时任务心跳",
                    Type = JobTaskLogTypeEnum.Heartbeat
                });
            }
            return errType;
        }

        /// <summary>
        /// 定时服务下线
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <returns>结果</returns>
        public async Task<BaseErrType> DownLineAsync(string appId, string taskName)
        {
            var errType = await _manager.ChangeStatusAsync(appId, taskName, JobTaskStatusEnum.Unstart);
            if (errType == BaseErrType.Success)
            {
                await _logManager.AddAsync(new JobTaskLogForm()
                {
                    AppId = appId,
                    TaskName = taskName,
                    Content = "定时任务下线",
                    Type = JobTaskLogTypeEnum.Downline
                });
            }
            return errType;
        }

        /// <summary>
        /// 启用/禁用
        /// </summary>
        /// <param name="id">实体id</param>
        /// <returns>结果</returns>
        public async Task<BaseErrType> SetIsEnabledAsync(Guid id)
        {
            return await _manager.SetIsEnabledAsync(id);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="ids">任务id</param>
        /// <returns>结果</returns>
        public async Task<BaseErrType> DeleteAsync(IEnumerable<Guid> ids)
        {
            return await _manager.DeleteAsync(ids);
        }

        /// <summary>
        /// 更改心跳状态
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="log">日志内容</param>
        /// <returns>结果</returns>
        public async Task<BaseErrType> AddLogAsync(string appId, string taskName, string log)
        {
            var errType = await _manager.HeartbeatAsync(appId, taskName);
            if (errType == BaseErrType.Success)
            {
                await _logManager.AddAsync(new JobTaskLogForm()
                {
                    AppId = appId,
                    TaskName = taskName,
                    Content = log,
                    Type = JobTaskLogTypeEnum.Running
                });
            }
            return errType;
        }
    }
}

