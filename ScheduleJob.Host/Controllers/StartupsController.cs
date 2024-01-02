using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OneForAll.Core;
using ScheduleJob.Application.Dtos;
using System.Threading.Tasks;
using ScheduleJob.Host.Models;
using ScheduleJob.Domain.Models;
using ScheduleJob.Application.Interfaces;
using System.Collections.Generic;
using ScheduleJob.Public.Models;
using ScheduleJob.Host.Filters;

namespace ScheduleJob.Host.Controllers
{
    [Route("api/[controller]")]
    public class StartupsController : Controller
    {
        public StartupsController()
        {
        }

        [HttpGet]
        public string Get()
        {
            return "项目启动成功...";
        }
    }
}
