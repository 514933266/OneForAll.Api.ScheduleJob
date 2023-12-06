using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ScheduleJob.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Models
{
    /// <summary>
    /// 定时任务
    /// </summary>
    public class JobTaskForm
    {
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// 应用程序id
        /// </summary>
        [Required]
        [StringLength(50)]
        public string AppId { get; set; }

        /// <summary>
        /// 应用程序密钥
        /// </summary>
        [Required]
        [StringLength(50)]
        public string AppSecret { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        /// <summary>
        /// 所属组名称
        /// </summary>
        [Required]
        [StringLength(50)]
        public string GroupName { get; set; }

        /// <summary>
        /// 正则表达式
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Cron { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Remark { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        [Required]
        public JobTaskStatusEnum Status { get; set; }
    }
}
