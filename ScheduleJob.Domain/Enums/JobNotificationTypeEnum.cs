using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Enums
{
    /// <summary>
    /// 消息通知类型
    /// </summary>
    public enum JobNotificationTypeEnum
    {
        /// <summary>
        /// 企业微信机器人
        /// </summary>
        WxQtRoot = 0,

        /// <summary>
        /// 钉钉机器人
        /// </summary>
        DingTalkRobot = 1,
    }
}
