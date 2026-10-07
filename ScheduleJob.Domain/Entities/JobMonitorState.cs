using ScheduleJob.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Domain.Entities
{
    /// <summary>
    /// 监控状态
    /// </summary>
    public class JobMonitorState
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Key]
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        [Required]
        [StringLength(100)]
        public string TaskName { get; set; }

        /// <summary>
        /// 客户端编号
        /// </summary>
        [Required]
        [StringLength(50)]
        public string ClientCode { get; set; }

        /// <summary>
        /// 上次监控状态
        /// </summary>
        [Required]
        public JobMonitorStatusEnum LastStatus { get; set; } = JobMonitorStatusEnum.Normal;

        /// <summary>
        /// 上次异常消息
        /// </summary>
        public string LastErrorMessage { get; set; }

        /// <summary>
        /// 连续异常次数
        /// </summary>
        [Required]
        public int FailCount { get; set; } = 0;

        /// <summary>
        /// 最近一次异常时间
        /// </summary>
        [Column(TypeName = "datetime")]
        public DateTime? LastFailTime { get; set; }

        /// <summary>
        /// 最近一次恢复时间
        /// </summary>
        [Column(TypeName = "datetime")]
        public DateTime? LastRecoverTime { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Column(TypeName = "datetime")]
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 修改时间
        /// </summary>
        [Required]
        [Column(TypeName = "datetime")]
        public DateTime UpdateTime { get; set; } = DateTime.UtcNow;
    }
}
