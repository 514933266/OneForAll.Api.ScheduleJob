using ScheduleJob.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.AggregateRoots
{
    /// <summary>
    /// 定时任务
    /// </summary>
    public class JobTask
    {
        [Key]
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
        /// 所属组名称
        /// </summary>
        [Required]
        [StringLength(50)]
        public string GroupName { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        [Required]
        public JobTaskStatusEnum Status { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [Required]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 运行节点名称
        /// </summary>
        [Required]
        [StringLength(50)]
        public string NodeName { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        [Required]
        [StringLength(50)]
        public string IpAddress { get; set; } = "127.0.0.0";

        /// <summary>
        /// 正则表达式
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Cron { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Column(TypeName = "datetime")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 修改时间
        /// </summary>
        [Required]
        [Column(TypeName = "datetime")]
        public DateTime UpdateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后运行时间
        /// </summary>
        [Required]
        [Column(TypeName = "datetime")]
        public DateTime RunningTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 心跳时间
        /// </summary>
        [Required]
        [Column(TypeName = "datetime")]
        public DateTime HeartbeatTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 备注
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Remark { get; set; }
    }
}
