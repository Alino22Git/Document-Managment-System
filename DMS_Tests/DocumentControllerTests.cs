using Xunit;
using Moq;
using AutoMapper;
using Microsoft.Extensions.Logging;
using DMS_REST_API.Controllers;
using DMS_DAL.Repositories;
using DMS_REST_API.DTO;
using DMS_DAL.Entities;
using DMS_REST_API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DMS_Tests.Controllers
{
    public class DocumentControllerTests
    {
        private readonly Mock<IDocumentRepository> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<DocumentController>> _mockLogger;
        private readonly Mock<IRabbitMQPublisher> _mockPublisher;
        private readonly DocumentController _controller;

        public DocumentControllerTests()
        {
            _mockRepo = new Mock<IDocumentRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<DocumentController>>();
            _mockPublisher = new Mock<IRabbitMQPublisher>();

            _controller = new DocumentController(
                _mockRepo.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockPublisher.Object
            );
        }

        #region Get Tests

        [Fact]
        public async Task Get_ReturnsOkResult_WithListOfDocuments()
        {
            
            var documents = new List<Document>
            {
                new Document { Id = 1, Title = "Doc1", FileType = "pdf" },
                new Document { Id = 2, Title = "Doc2", FileType = "docx" }
            };
            var dtoDocuments = new List<DocumentDto>
            {
                new DocumentDto { Id = 1, Title = "Doc1", FileType = "pdf" },
                new DocumentDto { Id = 2, Title = "Doc2", FileType = "docx" }
            };

            _mockRepo.Setup(repo => repo.GetAllDocumentsAsync()).ReturnsAsync(documents);
            _mockMapper.Setup(m => m.Map<IEnumerable<DocumentDto>>(documents)).Returns(dtoDocuments);

            
            var result = await _controller.Get();

           
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnDocuments = Assert.IsAssignableFrom<IEnumerable<DocumentDto>>(okResult.Value);
            Assert.Equal(2, ((List<DocumentDto>)returnDocuments).Count);
        }

        [Fact]
        public async Task GetById_ReturnsOkResult_WithDocument()
        {
           
            int testId = 1;
            var document = new Document { Id = testId, Title = "Doc1", FileType = "pdf" };
            var dtoDocument = new DocumentDto { Id = testId, Title = "Doc1", FileType = "pdf" };

            _mockRepo.Setup(repo => repo.GetDocumentAsync(testId)).ReturnsAsync(document);
            _mockMapper.Setup(m => m.Map<DocumentDto>(document)).Returns(dtoDocument);

            
            var result = await _controller.GetById(testId);

            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnDocument = Assert.IsType<DocumentDto>(okResult.Value);
            Assert.Equal(testId, returnDocument.Id);
        }

        

        #endregion

        #region Create Tests

        [Fact]
        public async Task Create_ReturnsCreatedAtActionResult_WithCreatedDocument()
        {
            
            var dtoItem = new DocumentDto { Title = "New Doc", FileType = "pdf" };
            var document = new Document { Id = 1, Title = "New Doc", FileType = "pdf" };
            var createdDto = new DocumentDto { Id = 1, Title = "New Doc", FileType = "pdf" };

            _mockMapper.Setup(m => m.Map<Document>(dtoItem)).Returns(document);
            _mockRepo.Setup(repo => repo.AddDocumentAsync(document)).Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<DocumentDto>(document)).Returns(createdDto);

            
            var result = await _controller.Create(dtoItem);

            
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            var returnDocument = Assert.IsType<DocumentDto>(createdAtActionResult.Value);
            Assert.Equal(createdDto.Id, returnDocument.Id);
            Assert.Equal(createdDto.Title, returnDocument.Title);
            Assert.Equal(createdDto.FileType, returnDocument.FileType);

            _mockPublisher.Verify(p => p.PublishDocumentCreated(createdDto), Times.Once);
        }

       

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenModelStateIsInvalid()
        {
            
            var dtoItem = new DocumentDto { Title = "", FileType = "pdf" };
            _controller.ModelState.AddModelError("Title", "The Document name cannot be empty.");

            
            var result = await _controller.Create(dtoItem);

            
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.False(_controller.ModelState.IsValid);
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task Update_ReturnsNoContent_WhenUpdateIsSuccessful()
        {
           
            int testId = 1;
            var dtoItem = new DocumentDto { Id = testId, Title = "Updated Doc", FileType = "docx" };
            var existingDocument = new Document { Id = testId, Title = "Old Doc", FileType = "pdf" };

            _mockRepo.Setup(repo => repo.GetDocumentAsync(testId)).ReturnsAsync(existingDocument);
            _mockRepo.Setup(repo => repo.UpdateDocumentAsync(existingDocument)).Returns(Task.CompletedTask);

           
            var result = await _controller.Update(testId, dtoItem);

            
            Assert.IsType<NoContentResult>(result);
            _mockPublisher.Verify(p => p.PublishDocumentUpdated(dtoItem), Times.Once);
            Assert.Equal("Updated Doc", existingDocument.Title);
            Assert.Equal("docx", existingDocument.FileType);
        }

       

        

        #endregion

        

    }
}
