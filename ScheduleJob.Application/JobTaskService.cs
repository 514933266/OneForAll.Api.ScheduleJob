using AutoMapper;
using Azure;
using Microsoft.AspNetCore.Mvc;
using NPOI.SS.Formula.Functions;
using OneForAll.Core;
using OneForAll.Core.Extension;
using OneForAll.Core.OAuth;
using ScheduleJob.Application.Dtos;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.AggregateRoots;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Interfaces;
using ScheduleJob.Domain.Models;
using ScheduleJob.Domain.Repositorys;
using ScheduleJob.HttpService.Interfaces;
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
        private readonly IJobTaskRepository _repository;
        private readonly IJobTaskPersonContactRepository _contactRepository;
        private readonly IScheduleJobHttpService _jobHttpService;
        public JobTaskService(
            IMapper mapper,
            IJobTaskManager manager,
            IJobTaskLogManager logManager,
            IJobTaskRepository repository,
            IJobTaskPersonContactRepository contactRepository,
            IScheduleJobHttpService jobHttpService)
        {
            _mapper = mapper;
            _manager = manager;
            _logManager = logManager;
            _repository = repository;
            _contactRepository = contactRepository;
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
        /// <returns>分页列表</returns>
        public async Task<PageList<JobTaskDto>> GetPageAsync(int pageIndex, int pageSize, string key, string groupName, string nodeName)
        {
            var data = await _manager.GetPageAsync(pageIndex, pageSize, key, groupName, nodeName);
            var items = _mapper.Map<IEnumerable<JobTask>, IEnumerable<JobTaskDto>>(data.Items);
            var persons = await _contactRepository.GetListPersonAsync(items.Select(s => s.Id));
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
                    Content = $"定时任务{(form.Id == Guid.Empty ? "注册" : "上线")},所属分组【{form.GroupName}】，运行节点【{form.NodeName}】,表达式【{form.Cron}】",
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

        /// <summary>
        /// 改变定时任务状态
        /// </summary>
        /// <param name="id">任务id</param>
        /// <param name="status">状态</param>
        /// <param name="loginUser">登录用户</param>
        /// <returns></returns>
        public async Task<BaseErrType> ChangeStatusAsync(Guid id, JobTaskStatusEnum status, LoginUser loginUser)
        {
            var data = await _repository.FindAsync(id);
            if (data == null) return BaseErrType.DataNotFound;
            if (data.Status == status) return BaseErrType.Success;
            if (!data.IsEnabled) return BaseErrType.Overflow;
            if (data.NodeName == "本地") return BaseErrType.DataNotMatch;
            if (status != JobTaskStatusEnum.Unstart && status != JobTaskStatusEnum.Running)
                return BaseErrType.NotAllow;

            var effected = 0;
            var content = "";
            var response = new BaseMessage();
            if (data.Status == JobTaskStatusEnum.Running && status == JobTaskStatusEnum.Unstart)
            {
                response = await _jobHttpService.StopAsync(data.NodeName, data.Name);
                content = $"{loginUser.Name}主动暂停定时任务，结果：{response.Message}";
            }
            else if (data.Status == JobTaskStatusEnum.Unstart && status == JobTaskStatusEnum.Running)
            {
                response = await _jobHttpService.ResumeAsync(data.NodeName, data.Name);
                content = $"{loginUser.Name}主动重启定时任务，结果：{response.Message}";
            }
            if (response.Status)
            {
                data.Status = status;
                data.UpdateTime = DateTime.Now;
                effected = await _repository.SaveChangesAsync();
            }

            // 记录操作日志
            await _logManager.AddAsync(new JobTaskLogForm()
            {
                AppId = data.AppId,
                TaskName = data.Name,
                Content = content,
                Type = JobTaskLogTypeEnum.Running
            });
            return effected > 0 ? BaseErrType.Success : BaseErrType.Fail;
        }

        /// <summary>
        /// 执行一次定时任务
        /// </summary>
        /// <param name="id">任务id</param>
        /// <param name="loginUser">登录用户</param>
        /// <returns></returns>
        public async Task<BaseErrType> ExcuteAsync(Guid id, LoginUser loginUser)
        {
            var data = await _repository.FindAsync(id);
            if (data == null) return BaseErrType.DataNotFound;
            if (!data.IsEnabled) return BaseErrType.Overflow;
            if (data.Status != JobTaskStatusEnum.Running) return BaseErrType.NotAllow;

            var response = await _jobHttpService.ExcuteAsync(data.NodeName, data.Name);
            var effected = 0;
            if (response.Status)
            {
                data.UpdateTime = DateTime.Now;
                effected = await _repository.SaveChangesAsync();
            }

            // 记录操作日志
            await _logManager.AddAsync(new JobTaskLogForm()
            {
                AppId = data.AppId,
                TaskName = data.Name,
                Content = $"{loginUser.Name}主动运行一次任务，结果：{response.Message}",
                Type = JobTaskLogTypeEnum.Running
            });
            return effected > 0 ? BaseErrType.Success : BaseErrType.Fail;
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

