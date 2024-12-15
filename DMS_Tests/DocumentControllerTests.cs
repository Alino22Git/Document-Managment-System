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
using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.IO;
using Minio;
using System.Net;

namespace DMS_Tests.Controllers
{
    public class DocumentControllerTests
    {
        private readonly Mock<IDocumentRepository> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<DocumentController>> _mockLogger;
        private readonly Mock<IRabbitMQPublisher> _mockPublisher;
        private readonly Mock<ElasticsearchClient> _mockElastic;
        private readonly Mock<IMinioClient> _mockMinio;
        private readonly DocumentController _controller;

        public DocumentControllerTests()
        {
            _mockRepo = new Mock<IDocumentRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<DocumentController>>();
            _mockPublisher = new Mock<IRabbitMQPublisher>();
            _mockElastic = new Mock<ElasticsearchClient>();
            _mockMinio = new Mock<IMinioClient>();

            _controller = new DocumentController(
                _mockElastic.Object,
                _mockRepo.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockPublisher.Object,
                _mockMinio.Object
            );
        }

        private IFormFile CreateMockFormFile(string content = "Test file content", string fileName = "testfile.pdf")
        {
            var fileMock = new Mock<IFormFile>();
            var fileContent = Encoding.UTF8.GetBytes(content);
            var ms = new MemoryStream(fileContent);
            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(ms.Length);
            return fileMock.Object;
        }

        #region UploadFile Tests

        [Fact]
        public async Task UploadFile_Successful_ReturnsCreatedAtAction()
        {
            // Arrange
            var uploadDto = new DocumentUploadDto
            {
                File = CreateMockFormFile(),
                Title = "My Document",
                FileType = "pdf"
            };

            _mockMinio.Setup(m => m.BucketExistsAsync(It.IsAny<Minio.DataModel.Args.BucketExistsArgs>(), default))
                      .ReturnsAsync(true);

            _mockMinio.Setup(m => m.PutObjectAsync(It.IsAny<Minio.DataModel.Args.PutObjectArgs>(), default))
         .ReturnsAsync(new Minio.DataModel.Response.PutObjectResponse(
             HttpStatusCode.OK,
             "uploads",
             new Dictionary<string, string>(),
             0L,
             "dummy-etag"
         ));

            _mockRepo.Setup(r => r.AddDocumentAsync(It.IsAny<Document>()))
                .Returns(Task.CompletedTask)
                .Callback((Document doc) => doc.Id = 42);

            _mockMapper.Setup(m => m.Map<DocumentDto>(It.IsAny<Document>()))
                .Returns((Document doc) => new DocumentDto
                {
                    Id = doc.Id,
                    Title = doc.Title,
                    FileType = doc.FileType,
                    Content = doc.Content
                });

            _mockPublisher.Setup(p => p.PublishDocumentCreated(It.IsAny<DocumentDto>()))
                          .Verifiable();

            var result = await _controller.UploadFile(uploadDto);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            dynamic returnValue = createdResult.Value;
            Assert.Equal(42, (int)returnValue.Id);
            Assert.Equal("My Document", (string)returnValue.Title);
            Assert.Equal("pdf", (string)returnValue.FileType);
            Assert.Equal("OCR wird verarbeitet...", (string)returnValue.Content);

            _mockPublisher.Verify();
        }

        [Fact]
        public async Task UploadFile_NoFileProvided_ReturnsBadRequest()
        {
            var uploadDto = new DocumentUploadDto
            {
                File = null,
                Title = "Doc Title",
                FileType = "pdf"
            };

            var result = await _controller.UploadFile(uploadDto);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Datei fehlt!", badRequest.Value);
        }

        [Fact]
        public async Task UploadFile_MissingTitle_ReturnsBadRequest()
        {
            var uploadDto = new DocumentUploadDto
            {
                File = CreateMockFormFile(),
                Title = "",
                FileType = "pdf"
            };

            var result = await _controller.UploadFile(uploadDto);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Dokumenttitel fehlt!", badRequest.Value);
        }

        [Fact]
        public async Task UploadFile_MissingFileType_ReturnsBadRequest()
        {
            var uploadDto = new DocumentUploadDto
            {
                File = CreateMockFormFile(),
                Title = "A Title",
                FileType = ""
            };

            var result = await _controller.UploadFile(uploadDto);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Dokumenttyp fehlt!", badRequest.Value);
        }

        [Fact]
        public async Task UploadFile_MinioUploadFails_ReturnsServerError()
        {
            var uploadDto = new DocumentUploadDto
            {
                File = CreateMockFormFile(),
                Title = "Failing Upload",
                FileType = "pdf"
            };

            _mockMinio.Setup(m => m.BucketExistsAsync(It.IsAny<Minio.DataModel.Args.BucketExistsArgs>(), default))
                      .ReturnsAsync(true);

            _mockMinio.Setup(m => m.PutObjectAsync(It.IsAny<Minio.DataModel.Args.PutObjectArgs>(), default))
                      .ThrowsAsync(new Minio.Exceptions.MinioException("Upload Error"));

            var result = await _controller.UploadFile(uploadDto);
            var serverError = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverError.StatusCode);
            Assert.Equal("Interner Serverfehler beim Hochladen der Datei.", serverError.Value);
        }

        [Fact]
        public async Task UploadFile_RabbitMQPublishFails_StillReturnsCreated()
        {
            var uploadDto = new DocumentUploadDto
            {
                File = CreateMockFormFile(),
                Title = "Doc with RabbitMQ Fail",
                FileType = "pdf"
            };

            _mockMinio.Setup(m => m.BucketExistsAsync(It.IsAny<Minio.DataModel.Args.BucketExistsArgs>(), default))
                      .ReturnsAsync(true);

            _mockMinio.Setup(m => m.PutObjectAsync(It.IsAny<Minio.DataModel.Args.PutObjectArgs>(), default))
          .ReturnsAsync(new Minio.DataModel.Response.PutObjectResponse(
              HttpStatusCode.OK,          
              "uploads",                  
              new Dictionary<string, string>(), 
              0L,                         
              "dummy-etag"                
          ));


            _mockRepo.Setup(r => r.AddDocumentAsync(It.IsAny<Document>()))
                .Returns(Task.CompletedTask)
                .Callback((Document doc) => doc.Id = 99);

            _mockMapper.Setup(m => m.Map<DocumentDto>(It.IsAny<Document>()))
                .Returns((Document doc) => new DocumentDto { Id = doc.Id, Title = doc.Title, FileType = doc.FileType });

            _mockPublisher.Setup(p => p.PublishDocumentCreated(It.IsAny<DocumentDto>()))
                          .Throws(new Exception("RabbitMQ down"));

            var result = await _controller.UploadFile(uploadDto);
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            dynamic returnValue = createdResult.Value;
            Assert.Equal(99, (int)returnValue.Id);
            Assert.Equal("Doc with RabbitMQ Fail", (string)returnValue.Title);
        }

        #endregion

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

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenDocumentNotExists()
        {
            int testId = 1;
            // Document nicht vorhanden -> null zurückgeben
            _mockRepo.Setup(repo => repo.GetDocumentAsync(testId)).ReturnsAsync((Document)null);

            var result = await _controller.GetById(testId);
            Assert.IsType<NotFoundObjectResult>(result);
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
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_ReturnsBadRequest_WhenModelIsNull()
        {
            var result = await _controller.Create(null);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task Update_ReturnsNoContent_WhenUpdateIsSuccessful()
        {
            int testId = 1;
            var existingDocument = new Document { Id = testId, Title = "Old Doc", FileType = "pdf" };
            var dtoUpdateItem = new DocumentUpdateDto { Id = testId, Title = "Updated Doc" };

            _mockRepo.Setup(repo => repo.GetDocumentAsync(testId)).ReturnsAsync(existingDocument);
            _mockRepo.Setup(repo => repo.UpdateDocumentAsync(existingDocument)).Returns(Task.CompletedTask);

            var result = await _controller.Update(testId, dtoUpdateItem);

            Assert.IsType<NoContentResult>(result);
            Assert.Equal("Updated Doc", existingDocument.Title);
            Assert.Equal("pdf", existingDocument.FileType);
        }

        [Fact]
        public async Task Update_ReturnsBadRequest_WhenModelIsNull()
        {
            var result = await _controller.Update(1, null);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsBadRequest_WhenIdsDontMatch()
        {
            int testId = 1;
            var dtoUpdateItem = new DocumentUpdateDto { Id = testId + 1, Title = "Old Doc" };

            var result = await _controller.Update(testId, dtoUpdateItem);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsBadRequest_WhenModelStateIsInvalid()
        {
            int testId = 1;
            var existingDocument = new Document { Id = testId, Title = "Old Doc", FileType = "pdf" };
            var dtoUpdateItem = new DocumentUpdateDto { Id = testId, Title = "" };
            _controller.ModelState.AddModelError("Title", "The Document name cannot be empty.");

            _mockRepo.Setup(repo => repo.GetDocumentAsync(testId)).ReturnsAsync(existingDocument);
            _mockRepo.Setup(repo => repo.UpdateDocumentAsync(existingDocument)).Returns(Task.CompletedTask);

            var result = await _controller.Update(testId, dtoUpdateItem);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenDocumentDoesNotExist()
        {
            int testId = 1;
            // Dokument nicht vorhanden
            _mockRepo.Setup(repo => repo.GetDocumentAsync(testId)).ReturnsAsync((Document)null);

            var dtoUpdateItem = new DocumentUpdateDto { Id = testId, Title = "Updated Doc" };

            var result = await _controller.Update(testId, dtoUpdateItem);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_ReturnsNoContent_WhenDeleteIsSuccessful()
        {
            int testId = 1;
            var existingDocument = new Document { Id = testId, Title = "Old Doc", FileType = "pdf" };

            _mockRepo.Setup(repo => repo.GetDocumentAsync(testId)).ReturnsAsync(existingDocument);
            _mockRepo.Setup(repo => repo.DeleteDocumentAsync(testId)).Returns(Task.CompletedTask);

            var result = await _controller.Delete(testId);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenIdDoesntExist()
        {
            int testId = 1;
            // Dokument nicht vorhanden => null

            var result = await _controller.Delete(testId);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

    }
}
