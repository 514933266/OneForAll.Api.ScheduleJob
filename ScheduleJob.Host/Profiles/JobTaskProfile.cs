using AutoMapper;
using ScheduleJob.Application.Dtos;
using ScheduleJob.Domain.AggregateRoots;
using ScheduleJob.Domain.Models;

namespace ScheduleJob.Host.Profiles
{
    public class JobTaskProfile : Profile
    {
        public JobTaskProfile()
        {
            CreateMap<JobTask, JobTaskDto>();
            CreateMap<JobTaskForm, JobTask>();
            CreateMap<JobTaskRegisterForm, JobTask>();
        }
    }
}
