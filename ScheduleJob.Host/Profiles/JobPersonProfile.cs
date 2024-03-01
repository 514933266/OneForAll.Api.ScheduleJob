using AutoMapper;
using ScheduleJob.Application.Dtos;
using ScheduleJob.Domain.AggregateRoots;
using ScheduleJob.Domain.Models;

namespace ScheduleJob.Host.Profiles
{
    public class JobPersonProfile : Profile
    {
        public JobPersonProfile()
        {
            CreateMap<JobPerson, JobPersonDto>();
            CreateMap<JobPersonForm, JobPerson>();
        }
    }
}