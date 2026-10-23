namespace ScheduleJob.Application.Dtos
{
    /// <summary>
    /// 定时任务状态巡检结果
    /// </summary>
    public class JobTaskMonitorDto
    {
        /// <summary>
        /// 巡检任务总数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 发现异常的任务数
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// 执行日志（供调度任务逐条落库）
        /// </summary>
        public System.Collections.Generic.List<string> Logs { get; set; } = new System.Collections.Generic.List<string>();
    }
}
