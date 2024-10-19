using AutoMapper;
using DMS_REST_API.Entities;
using DMS_REST_API.DTO;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<DocumentDTO, Document>();
        CreateMap<Document, DocumentDTO>();
    }
}
