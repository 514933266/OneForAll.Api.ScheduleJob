using OneForAll.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.HttpService.Interfaces
{
    /// <summary>
    /// 功能权限校验服务
    /// </summary>
    public interface ISysPermissionCheckHttpService
    {
        /// <summary>
        /// 验证功能权限
        /// </summary>
        /// <returns>返回消息</returns>
        Task<BaseMessage> ValidateAuthorization(string controller, string action);
    }
}
