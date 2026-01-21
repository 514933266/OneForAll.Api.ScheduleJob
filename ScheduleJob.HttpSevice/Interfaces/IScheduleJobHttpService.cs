using OneForAll.Core;
using ScheduleJob.HttpService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.HttpService.Interfaces
{
    /// <summary>
    /// 定时任务调度中心
    /// </summary>
    public interface IScheduleJobHttpService
    {
        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="request">实体</param>
        /// <returns></returns>
        Task<BaseMessage> RegisterAsync(JobRegisterRequest request);

        /// <summary>
        /// 定时服务下线
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <returns结果</returns>
        Task<BaseMessage> DownLineAsync(string appId, string taskName);

        /// <summary>
        /// 定时服务运行日志
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="log">日志内容</param>
        /// <returns结果</returns>
        Task<BaseMessage> LogAsync(string appId, string taskName, string log);

        /// <summary>
        /// 暂停定时服务
        /// </summary>
        /// <param name="taskName">定时任务名称</param>
        /// <returns结果</returns>
        Task<BaseMessage> StopAsync(string url, string taskName);

        /// <summary>
        /// 重启定时服务
        /// </summary>
        /// <param name="taskName">定时任务名称</param>
        /// <returns结果</returns>
        Task<BaseMessage> ResumeAsync(string url, string taskName);

        /// <summary>
        /// 执行一次定时服务
        /// </summary>
        /// <param name="taskName">定时任务名称</param>
        /// <returns结果</returns>
        Task<BaseMessage> ExcuteAsync(string url, string taskName);
    }
}
