namespace ScheduleJob.Domain.ValueObjects
{
    /// <summary>
    /// 钉钉机器人通知目标配置
    /// </summary>
    public class DingTalkRobotTargetVo
    {
        /// <summary>
        /// 机器人地址
        /// </summary>
        public string WebhookUrl { get; set; }

        /// <summary>
        /// 签名密钥
        /// </summary>
        public string Sign { get; set; }
    }
}
