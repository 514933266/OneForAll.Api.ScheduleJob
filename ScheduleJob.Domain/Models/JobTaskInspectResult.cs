using System;

namespace ScheduleJob.Domain.Models
{
    /// <summary>
    /// 定时任务状态巡检判定结果（由 cron 与心跳/运行时间推算，不涉及数据访问）
    /// </summary>
    public class JobTaskInspectResult
    {
        /// <summary>
        /// 是否已超过最近一次预期执行的宽限期（需要主动探测节点存活）
        /// </summary>
        public bool IsPastGracePeriod { get; set; }

        /// <summary>
        /// 最近一次预期执行时间（UTC）
        /// </summary>
        public DateTime LastExpectedRunTime { get; set; }

        /// <summary>
        /// 是否已错过执行（已过宽限期，且最后运行时间早于预期执行时间）
        /// </summary>
        public bool IsMissed { get; set; }
    }
}
