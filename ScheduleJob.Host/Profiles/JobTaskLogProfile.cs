using AutoMapper;
using ScheduleJob.Application.Dtos;
using ScheduleJob.Domain.Entities;
using ScheduleJob.Domain.Models;


namespace ScheduleJob.Host.Profiles
{
    public class JobTaskLogProfile : Profile
    {
        public JobTaskLogProfile()
        {
            CreateMap<JobTaskLog, JobTaskLogDto>();
            CreateMap<JobTaskLogForm, JobTaskLog>();
        }
    }
}
