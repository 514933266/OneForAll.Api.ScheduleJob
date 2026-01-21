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
        public string SysBase { get; set; } = "SysBase";

        /// <summary>
        /// 权限验证接口
        /// </summary>
        public string SysLog { get; set; } = "SysLog";

        /// <summary>
        /// 权限验证接口
        /// </summary>
        public string SysUms { get; set; } = "SysUms";

        /// <summary>
        /// 权限验证接口
        /// </summary>
        public string SysJob { get; set; } = "SysJob";

        /// <summary>
        /// 权限验证接口
        /// </summary>
        public string OA { get; set; } = "OA";

    }
}
