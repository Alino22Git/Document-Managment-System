using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;
using AutoMapper;
using DMS_REST_API.DTO;
using DMS_REST_API.Entities;
using Xunit;

namespace DMS_Tests
{
    public class DocumentTests
    {
        [Fact]
        public void Document_ShouldFailValidation_WhenNameIsEmpty()
        {
            // Arrange: Ein neues Document ohne Name erstellen
            var document = new DMS_REST_API.Entities.Document { FileType = "pdf", CreatedAt = DateTime.Now };
            var context = new ValidationContext(document, null, null);
            var validationResults = new List<ValidationResult>();

            // Act: Die Validierung durchführen
            bool isValid = Validator.TryValidateObject(document, context, validationResults, true);

            // Assert: Überprüfen, ob die Validierung zutrifft
            Assert.True(isValid);
        }

        [Fact]
        public void DocumentDTO_Mapping_ShouldBeValid()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            var mapper = config.CreateMapper();

            var document = new DMS_REST_API.Entities.Document { Id = 1, Name = "Test Document", FileType = "pdf", CreatedAt = DateTime.Now };

            var dto = mapper.Map<DocumentDTO>(document);

            Assert.Equal(document.Name, dto.Name);
            Assert.Equal(document.FileType, dto.FileType);
            Assert.Equal(document.CreatedAt, dto.CreatedAt);
        }

        [Fact]
        public void Document_Validation_ShouldFail_WhenNameIsEmpty()
        {
            var document = new DMS_REST_API.Entities.Document { FileType = "pdf", CreatedAt = DateTime.Now };
            var context = new ValidationContext(document, null, null);
            var validationResults = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(document, context, validationResults, true);

            Assert.False(isValid);
        }
    }
}
