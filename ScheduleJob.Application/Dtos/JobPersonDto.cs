using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleJob.Application.Dtos
{
    /// <summary>
    /// 人员信息
    /// </summary>
    public class JobPersonDto
    {
        /// <summary>
        /// 唯一键
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 联系方式
        /// </summary>
        public string Mobile { get; set; }
    }
}
