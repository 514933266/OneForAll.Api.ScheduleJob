using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.HttpService.Models
{
    /// <summary>
    /// 微信机器人
    /// </summary>
    public class UmsWechatQyRobotTextRequest
    {
        /// <summary>
        /// 机器人地址
        /// </summary>
        [Required]
        public string WebhookUrl { get; set; }

        /// <summary>
        /// 发送内容
        /// </summary>
        [Required]
        public string Content { get; set; }

        /// <summary>
        /// 接收消息的用户id
        /// </summary>
        public List<string> UserIds { get; set; } = new List<string>();

        /// <summary>
        /// 接收消息的用户手机列表
        /// </summary>
        public List<string> Mobiles { get; set; } = new List<string>();
    }
}
