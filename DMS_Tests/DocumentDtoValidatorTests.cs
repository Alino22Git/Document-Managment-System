using Xunit;
using DMS_REST_API.DTO;
using FluentValidation.TestHelper;

namespace DMS_Tests
{
    public class DocumentDtoValidatorTests
    {
        private readonly DocumentDtoValidator _validator;

        public DocumentDtoValidatorTests()
        {
            _validator = new DocumentDtoValidator();
        }

        [Fact]
        public void Should_Have_Error_When_Title_Is_Empty()
        {
            
            var dto = new DocumentDto { Title = "", FileType = "pdf" };

           
            var result = _validator.TestValidate(dto);

            
            result.ShouldHaveValidationErrorFor(doc => doc.Title)
                  .WithErrorMessage("The Document name cannot be empty.");
        }

        [Fact]
        public void Should_Have_Error_When_Title_Exceeds_MaxLength()
        {
            
            var dto = new DocumentDto { Title = new string('a', 101), FileType = "pdf" };

            
            var result = _validator.TestValidate(dto);

           
            result.ShouldHaveValidationErrorFor(doc => doc.Title)
                  .WithErrorMessage("The Document name must not exceed 100 chars.");
        }

        [Fact]
        public void Should_Have_Error_When_FileType_Is_Null()
        {
            
            var dto = new DocumentDto { Title = "Valid Title", FileType = null };

            
            var result = _validator.TestValidate(dto);

            
            result.ShouldHaveValidationErrorFor(doc => doc.FileType)
                  .WithErrorMessage("The Filetype must be specified.");
        }

        [Fact]
        public void Should_Not_Have_Error_When_Dto_Is_Valid()
        {
            
            var dto = new DocumentDto { Title = "Valid Title", FileType = "pdf" };

           
            var result = _validator.TestValidate(dto);

            
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
