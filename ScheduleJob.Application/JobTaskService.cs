using AutoMapper;
using OneForAll.Core;
using OneForAll.Core.OAuth;
using ScheduleJob.Application.Dtos;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Interfaces;
using ScheduleJob.Domain.Models;
using ScheduleJob.HttpService.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IJobTaskQueryService _queryService;
        private readonly IScheduleJobHttpService _jobHttpService;
        public JobTaskService(
            IMapper mapper,
            IJobTaskManager manager,
            IJobTaskLogManager logManager,
            IJobTaskQueryService queryService,
            IScheduleJobHttpService jobHttpService)
        {
            _mapper = mapper;
            _manager = manager;
            _logManager = logManager;
            _queryService = queryService;
            _jobHttpService = jobHttpService;
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
        public async Task<PageList<JobTaskDto>> GetPageAsync(
            int pageIndex,
            int pageSize,
            string key,
            string groupName,
            string nodeName,
            string appId)
        {
            var data = await _queryService.GetPageAsync(pageIndex, pageSize, key, groupName, nodeName, appId);
            var items = _mapper.Map<IEnumerable<JobTask>, IEnumerable<JobTaskDto>>(data.Items);
            var persons = await _queryService.GetListPersonAsync(items.Select(s => s.Id).ToList());
            return new PageList<JobTaskDto>(data.Total, data.PageSize, data.PageIndex, items);
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
        /// 改变定时任务状态
        /// </summary>
        /// <param name="id">任务id</param>
        /// <param name="status">状态</param>
        /// <param name="loginUser">登录用户</param>
        /// <returns></returns>
        public async Task<BaseErrType> ChangeStatusAsync(Guid id, JobTaskStatusEnum status, LoginUser loginUser)
        {
            var data = await _queryService.GetByIdAsync(id);
            if (data == null) return BaseErrType.DataNotFound;
            if (data.Status == status) return BaseErrType.Success;
            if (!data.IsEnabled) return BaseErrType.Overflow;
            if (data.NodeName == "本地") return BaseErrType.DataNotMatch;
            if (status != JobTaskStatusEnum.Unstart && status != JobTaskStatusEnum.Running)
                return BaseErrType.NotAllow;

            var errType = BaseErrType.Fail;
            var content = "";
            var response = new BaseMessage();
            if (data.Status == JobTaskStatusEnum.Running && status == JobTaskStatusEnum.Unstart)
            {
                response = await _jobHttpService.StopAsync(data.NodeName, data.Name);
                content = $"{loginUser.UserName}主动暂停定时任务，结果：{response.Message}";
                if (response.Status)
                    errType = await _manager.ChangeStatusByIdAsync(id, status);
            }
            else if (data.Status == JobTaskStatusEnum.Unstart && status == JobTaskStatusEnum.Running)
            {
                response = await _jobHttpService.ResumeAsync(data.NodeName, data.Name);
                content = $"{loginUser.UserName}主动重启定时任务，结果：{response.Message}";
                if (response.Status)
                    errType = await _manager.ChangeStatusByIdAsync(id, status);
            }

            // 记录操作日志
            await _logManager.AddAsync(new JobTaskLogForm()
            {
                AppId = data.AppId,
                TaskName = data.Name,
                Content = content,
                Type = JobTaskLogTypeEnum.Running
            });
            return errType;
        }

        /// <summary>
        /// 执行一次定时任务
        /// </summary>
        /// <param name="id">任务id</param>
        /// <param name="loginUser">登录用户</param>
        /// <returns></returns>
        public async Task<BaseErrType> ExcuteAsync(Guid id, LoginUser loginUser)
        {
            var data = await _queryService.GetByIdAsync(id);
            if (data == null) return BaseErrType.DataNotFound;
            if (!data.IsEnabled) return BaseErrType.Overflow;
            if (data.Status != JobTaskStatusEnum.Running) return BaseErrType.NotAllow;

            var response = await _jobHttpService.ExcuteAsync(data.NodeName, data.Name);
            var errType = BaseErrType.Fail;
            if (response.Status)
            {
                errType = await _manager.UpdateUpdateTimeByIdAsync(id);
            }

            // 记录操作日志
            await _logManager.AddAsync(new JobTaskLogForm()
            {
                AppId = data.AppId,
                TaskName = data.Name,
                Content = $"{loginUser.UserName}主动运行一次任务，结果：{response.Message}",
                Type = JobTaskLogTypeEnum.Running
            });
            return errType;
        }

        /// <summary>
        /// 设置负责人
        /// </summary>
        /// <param name="id">任务id</param>
        /// <param name="personIds">人员id</param>
        /// <returns></returns>
        public async Task<BaseErrType> AddPersonsAsync(Guid id, IEnumerable<Guid> personIds)
        {
            return await _manager.AddPersonsAsync(id, personIds);
        }
    }
}

