using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Enums
{
    /// <summary>
    /// 监控状态
    /// </summary>
    public enum JobMonitorStatusEnum
    {
        /// <summary>
        /// 正常
        /// </summary>
        Normal = 0,

        /// <summary>
        /// 异常
        /// </summary>
        Abnormal = 1,
    }
}
