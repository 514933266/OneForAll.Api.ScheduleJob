using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Enums
{
    /// <summary>
    /// 日志类型
    /// </summary>
    public enum JobTaskLogTypeEnum
    {
        /// <summary>
        /// 无
        /// </summary>
        None = -1,

        /// <summary>
        /// 注册
        /// </summary>
        Register = 0,

        /// <summary>
        /// 上线日志
        /// </summary>
        Online = 1,

        /// <summary>
        /// 下线日志
        /// </summary>
        Downline = 2,

        /// <summary>
        /// 运行日志
        /// </summary>
        Running = 3,

        /// <summary>
        /// 心跳日志
        /// </summary>
        Heartbeat = 4,

        /// <summary>
        /// 异常日志
        /// </summary>
        Error = 99
    }
}
