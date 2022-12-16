namespace My.Hr.Business.Services
{
    public class AutoMapperProfile : AutoMapper.Profile
    {
        public AutoMapperProfile()
        {
            // Need to explicitly map type to type, otherwise Map(srce, dest) just returns srce - seems like a bug to me, but apparently by design: https://github.com/AutoMapper/AutoMapper/issues/656
            CreateMap<Models.Employee, Models.Employee>();
        }
    }
}