// DMS_REST_API/Mappings/MappingProfile.cs
using AutoMapper;
using DMS_REST_API.DTO;
using DMS_DAL.Entities;
using System.IO;

namespace DMS_REST_API.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Mapping zwischen Document und DocumentDto
            CreateMap<Document, DocumentDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.FileType, opt => opt.MapFrom(src => src.FileType))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
                .ReverseMap()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.FileType, opt => opt.MapFrom(src => src.FileType))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content));
            // Mapping zwischen DocumentUploadDto und Document
            CreateMap<DocumentUploadDto, Document>()
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.FileType, opt => opt.MapFrom(src => src.FileType))
                .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => Path.GetFileName(src.File.FileName)));
        }
    }
}