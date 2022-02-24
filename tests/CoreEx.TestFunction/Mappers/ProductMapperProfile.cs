using AutoMapper;
using CoreEx.TestFunction.Models;

namespace CoreEx.TestFunction.Mappers
{
    internal class ProductMapperProfile : Profile
    {
        public ProductMapperProfile()
        {
            CreateMap<Product, BackendProduct>()
                .ForMember(d => d.Code, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Description, o => o.MapFrom(s => s.Name))
                .ForMember(d => d.RetailPrice, o => o.MapFrom(s => s.Price))
                .ReverseMap();
        }
    }
}