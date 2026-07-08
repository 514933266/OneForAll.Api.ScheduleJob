using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScheduleJob.Domain.Entities
{
    /// <summary>
    /// 定时任务运行锁
    /// </summary>
    public class JobRunningLock
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// 任务名称（唯一标识）
        /// </summary>
        [Required]
        [StringLength(100)]
        public string TaskName { get; set; }

        /// <summary>
        /// 客户端ID（机器名、IP或实例标识）
        /// </summary>
        [Required]
        [StringLength(100)]
        public string ClientId { get; set; }

        /// <summary>
        /// 最大并发数（默认1）
        /// </summary>
        [Required]
        public int MaxConcurrent { get; set; } = 1;

        /// <summary>
        /// 当前运行实例数
        /// </summary>
        [Required]
        public int CurrentRunningCount { get; set; } = 0;

        /// <summary>
        /// 是否启用并发控制
        /// </summary>
        [Required]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 版本号（用于乐观锁）
        /// </summary>
        [Required]
        public int Version { get; set; } = 0;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Column(TypeName = "datetime")]
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新时间
        /// </summary>
        [Required]
        [Column(TypeName = "datetime")]
        public DateTime UpdateTime { get; set; } = DateTime.UtcNow;
    }
}
