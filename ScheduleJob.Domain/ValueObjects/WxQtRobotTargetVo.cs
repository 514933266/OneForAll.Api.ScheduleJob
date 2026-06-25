using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.ValueObjects
{
    /// <summary>
    /// 企业微信机器人
    /// </summary>
    public class WxQtRobotTargetVo
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
