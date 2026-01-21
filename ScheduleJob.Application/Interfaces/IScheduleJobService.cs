using OneForAll.Core;
using ScheduleJob.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Application.Interfaces
{
    /// <summary>
    /// 定时任务（OpenApi）
    /// </summary>
    public interface IScheduleJobService
    {
        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="form">表单</param>
        /// <returns>结果</returns>
        Task<BaseErrType> RegisterAsync(JobTaskRegisterForm form);

        /// <summary>
        /// 定时服务下线
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <returns>结果</returns>
        Task<BaseErrType> DownLineAsync(string appId, string taskName);

        /// <summary>
        /// 心跳
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <returns>结果</returns>
        Task<BaseErrType> HeartbeatAsync(string appId, string taskName);

        /// <summary>
        /// 日志
        /// </summary>
        /// <param name="appId">应用程序id</param>
        /// <param name="taskName">定时任务名称</param>
        /// <param name="log">日志</param>
        /// <param name="isException">是否异常日志</param>
        /// <returns>结果</returns>
        Task<BaseErrType> AddLogAsync(string appId, string taskName, string log, bool isException);
    }
}
