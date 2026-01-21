using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Enums
{
    /// <summary>
    /// 定时任务类型
    /// </summary>
    public enum JobTaskTypeEnum
    {
        /// <summary>
        /// 本地定时任务
        /// </summary>
        Local,

        /// <summary>
        /// Api类定时任务
        /// </summary>
        Api
    }
}
