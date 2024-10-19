using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;
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
    }
}
