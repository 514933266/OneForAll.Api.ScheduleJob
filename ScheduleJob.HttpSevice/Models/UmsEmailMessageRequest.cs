using System.ComponentModel.DataAnnotations;

namespace ScheduleJob.HttpService.Models
{
    /// <summary>
    /// 邮件消息请求
    /// </summary>
    public class UmsEmailMessageRequest
    {
        /// <summary>
        /// 收件人邮箱（多个以,号隔开）
        /// </summary>
        [Required]
        public string To { get; set; }

        /// <summary>
        /// 抄送邮箱（多个以,号隔开）
        /// </summary>
        public string Cc { get; set; }

        /// <summary>
        /// 邮件主题
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Subject { get; set; }

        /// <summary>
        /// 邮件正文
        /// </summary>
        [Required]
        public string Body { get; set; }

        /// <summary>
        /// 是否为Html内容
        /// </summary>
        public bool IsHtml { get; set; } = true;

    }
}
