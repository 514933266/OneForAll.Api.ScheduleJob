using OneForAll.Core;
using ScheduleJob.Application.Interfaces;
using ScheduleJob.Domain.Enums;
using ScheduleJob.Domain.Interfaces;
using ScheduleJob.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Application
{
    /// <summary>
    /// 定时任务（OpenApi）
    /// </summary>
    public class ScheduleJobService : IScheduleJobService
    {
        private readonly IJobTaskManager _manager;
        private readonly IJobTaskLogManager _logManager;
        public ScheduleJobService(
            IJobTaskManager manager,
            IJobTaskLogManager logManager)
        {
            _manager = manager;
            _logManager = logManager;
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
        /// 心跳
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
            var retry = 0;
            var isAlive = false;
            while (retry <= 3)
            {
                retry++;
                isAlive = await _manager.CheckForSurvivalAsync(appId, taskName);
                if (isAlive)
                    break;

                await Task.Delay(TimeSpan.FromSeconds(10));
            }

            if (!isAlive)
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
            return BaseErrType.Success;
        }

        /// <summary>
        /// 日志
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="log">日志</param>
        /// <param name="isException">是否异常日志</param>
        /// <returns>结果</returns>
        public async Task<BaseErrType> AddLogAsync(string appId, string taskName, string log, bool isException)
        {
            var errType = await _manager.HeartbeatAsync(appId, taskName);
            if (errType == BaseErrType.Success)
            {
                await _logManager.AddAsync(new JobTaskLogForm()
                {
                    AppId = appId,
                    TaskName = taskName,
                    Content = log,
                    Type = isException ? JobTaskLogTypeEnum.Error : JobTaskLogTypeEnum.Running
                });
            }
            return errType;
        }
    }
}
