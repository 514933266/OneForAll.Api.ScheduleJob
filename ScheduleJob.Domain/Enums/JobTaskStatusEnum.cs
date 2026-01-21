using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Enums
{
    /// <summary>
    /// 定时任务状态
    /// </summary>
    public enum JobTaskStatusEnum
    {
        /// <summary>
        /// 未开始
        /// </summary>
        Unstart = 0,

        /// <summary>
        /// 运行中
        /// </summary>
        Running = 1,

        /// <summary>
        /// 异常
        /// </summary>
        Error = 99
    }
}
