using AutoMapper;
using DMS_REST_API.DTO;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMS_Tests
{
    public class DocumentTests
    {

        [Fact]
        public void DocumentDTO_Mapping_ShouldBeValid()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<DMS_REST_API.Mappings.MappingProfile>());
            var mapper = config.CreateMapper();
            var document = new DMS_DAL.Entities.Document { Id = 1, Title = "Test Document", FileType = "pdf" };
            var dto = mapper.Map<DocumentDto>(document);
            Assert.Equal(document.Title, dto.Title);
            Assert.Equal(document.FileType, dto.FileType);
            
        }
    }
}
