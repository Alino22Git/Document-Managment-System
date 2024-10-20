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
            Assert.Equal($"{document.Title} mapped", dto.Title);
            Assert.Equal(document.FileType, dto.FileType);
            
        }
        [Fact]
        public void Document_Validation_ShouldFail_WhenNameIsEmpty()
        {
            var document = new DMS_DAL.Entities.Document { FileType = "pdf" };
            var context = new ValidationContext(document, null, null);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(document, context, validationResults, true);
            Assert.False(isValid);
        }
    }
}
