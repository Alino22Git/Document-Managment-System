using AutoMapper;
using DMS_REST_API.DTO;
using DMS_DAL.Entities;


namespace DMS_REST_API.Mappings
{
    public class MappingProfile: Profile
    {
        public MappingProfile()
        {
            CreateMap<Document, DocumentDto>()
                .ForMember(dest => dest.Id, opt
                    => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Title, opt
                    => opt.MapFrom(src => $"{src.Title} mapped"))
                .ReverseMap()
                .ForMember(dest => dest.Id, opt
                    => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Title, opt
                    => opt.MapFrom(src => (src.Title ?? string.Empty).Replace(" mapped", "")))
                ;
        }
    }
}
