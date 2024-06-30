using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleJob.HttpService.Models
{
    /// <summary>
    /// 数据资源服务配置
    /// </summary>
    public class HttpServiceConfig
    {
        /// <summary>
        /// 权限验证接口
        /// </summary>
        public string SysPermissionCheck { get; set; } = "SysPermissionCheck";

        /// <summary>
        /// Api日志
        /// </summary>
        public string SysApiLog { get; set; } = "SysApiLog";

        /// <summary>
        /// 异常日志
        /// </summary>
        public string SysExceptionLog { get; set; } = "SysExceptionLog";

        /// <summary>
        /// 全局异常日志
        /// </summary>
        public string SysGlobalExceptionLog { get; set; } = "SysGlobalExceptionLog";

        /// <summary>
        /// 定时任务调度中心
        /// </summary>
        public string ScheduleJob { get; set; } = "ScheduleJob";

        /// <summary>
        /// 企业微信通知
        /// </summary>
        public string UmsWechatQyRobot { get; set; } = "UmsWechatQyRobot";

        /// <summary>
        /// 企业微信通知地址
        /// </summary>
        public string UmsWechatQyRobotWebhookUrl { get; set; } = "UmsWechatQyRobotWebhookUrl";

        /// <summary>
        /// OA人员档案
        /// </summary>
        public string OAPerson { get; set; } = "OAPerson";

    }
}
