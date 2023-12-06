using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleJob.HttpService.Models
{
    /// <summary>
    /// 权限验证模型
    /// </summary>
    public class SysPermissionCheckRequest
    {
        /// <summary>
        /// 机构id
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// 用户id
        /// </summary>
        public Guid SysUserId { get; set; }

        /// <summary>
        /// 控制器名称
        /// </summary>
        public string Controller { get; set; }

        /// <summary>
        /// 方法名称
        /// </summary>
        public string Action { get; set; }
    }
}
