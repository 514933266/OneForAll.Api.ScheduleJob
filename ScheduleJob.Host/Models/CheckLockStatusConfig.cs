namespace ScheduleJob.Host.Models
{
    /// <summary>
    /// 锁状态检查配置
    /// </summary>
    public class CheckLockStatusConfig
    {
        /// <summary>
        /// 最低超时分钟数（默认30分钟）
        /// </summary>
        public int MinExpireMinutes { get; set; } = 30;

        /// <summary>
        /// 最高超时分钟数（默认360分钟，即6小时）
        /// </summary>
        public int MaxExpireMinutes { get; set; } = 360;

        /// <summary>
        /// 执行周期附加缓冲分钟数（阈值 = 任务执行周期 + 该值，默认60分钟）
        /// </summary>
        public int PeriodBufferMinutes { get; set; } = 60;
    }
}
