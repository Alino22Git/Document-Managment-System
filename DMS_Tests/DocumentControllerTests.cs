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
using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace DMS_Tests.Controllers
{
    public class DocumentControllerTests
    {
        private readonly Mock<IDocumentRepository> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<DocumentController>> _mockLogger;
        private readonly Mock<IRabbitMQPublisher> _mockPublisher;
        private readonly Mock<ElasticsearchClient> _elasticClientMock;
        private readonly DocumentController _controller;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public DocumentControllerTests()
        {
            _mockRepo = new Mock<IDocumentRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<DocumentController>>();
            _mockPublisher = new Mock<IRabbitMQPublisher>();

            // ElasticsearchClient mocken
            _elasticClientMock = new Mock<ElasticsearchClient>(new ElasticsearchClientSettings(new Uri("http://localhost:9200")));

            _controller = new DocumentController(
                _elasticClientMock.Object,
                _mockRepo.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockPublisher.Object
            );
        }

        #region Hilfsmethode zur Deserialisierung von SearchResponse<Document>

        private SearchResponse<Document> DeserializeSearchResponse(string json)
        {
            // Direkte Deserialisierung in SearchResponse<Document> mit System.Text.Json
            // Die JSON-Struktur entspricht den von Elasticsearch zurückgegebenen Daten.
            return JsonSerializer.Deserialize<SearchResponse<Document>>(json, _jsonOptions);
        }

        #endregion

        #region Vorhandene Tests (Beispielhaft)

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
            Assert.Equal(2, returnDocuments.Count());
        }

        [Fact]
        public async Task Create_ReturnsCreatedAtActionResult_WithCreatedDocument_AnonymousObject()
        {
            // Arrange
            var dtoItem = new DocumentDto { Title = "New Doc", FileType = "pdf" };
            var document = new Document { Id = 1, Title = "New Doc", FileType = "pdf" };

            // Mapper-Setup: Aus DocumentDto -> Document
            _mockMapper.Setup(m => m.Map<Document>(It.IsAny<DocumentDto>()))
                .Returns((DocumentDto d) => new Document { Id = 1, Title = d.Title, FileType = d.FileType });

            // Mapper-Setup: Aus Document -> DocumentDto
            _mockMapper.Setup(m => m.Map<DocumentDto>(It.IsAny<Document>()))
                .Returns((Document doc) => new DocumentDto { Id = doc.Id, Title = doc.Title, FileType = doc.FileType });

            // Repository-Setup: Erfolgreiches Hinzufügen des Dokuments
            _mockRepo.Setup(repo => repo.AddDocumentAsync(It.IsAny<Document>())).Returns(Task.CompletedTask);

            // Publisher-Setup: Kein Fehler beim Publishen
            _mockPublisher.Setup(p => p.PublishDocumentCreated(It.IsAny<DocumentDto>()));

            // Act
            var result = await _controller.Create(dtoItem);

            // Assert
            // Hier sollte nun wirklich ein CreatedAtActionResult zurückkommen.
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            var returnObject = createdAtActionResult.Value;

            // Reflection zur Überprüfung des anonymen Objekts
            var returnType = returnObject.GetType();
            var idProp = returnType.GetProperty("Id");
            var titleProp = returnType.GetProperty("Title");
            var fileTypeProp = returnType.GetProperty("FileType");
            var contentProp = returnType.GetProperty("Content");

            Assert.NotNull(idProp);
            Assert.NotNull(titleProp);
            Assert.NotNull(fileTypeProp);
            Assert.NotNull(contentProp);

            Assert.Equal(document.Id, (int)idProp.GetValue(returnObject));
            Assert.Equal(document.Title, (string)titleProp.GetValue(returnObject));
            Assert.Equal(document.FileType, (string)fileTypeProp.GetValue(returnObject));
            Assert.Equal("OCR wird verarbeitet...", (string)contentProp.GetValue(returnObject));

            _mockPublisher.Verify(p => p.PublishDocumentCreated(It.Is<DocumentDto>(d =>
                d.Id == document.Id && d.Title == "New Doc"
            )), Times.Once);
        }


        [Fact]
        public async Task Update_ReturnsNoContent_WhenUpdateIsSuccessful()
        {
            int testId = 1;
            var dtoItem = new DocumentUpdateDto { Id = testId, Title = "Updated Doc" };
            var existingDocument = new Document { Id = testId, Title = "Old Doc", FileType = "pdf" };

            _mockRepo.Setup(repo => repo.GetDocumentAsync(testId)).ReturnsAsync(existingDocument);
            _mockRepo.Setup(repo => repo.UpdateDocumentAsync(existingDocument)).Returns(Task.CompletedTask);
            _mockMapper.Setup(m => m.Map<DocumentDto>(existingDocument)).Returns(new DocumentDto { Id = testId, Title = "Updated Doc" });

            var result = await _controller.Update(testId, dtoItem);

            Assert.IsType<NoContentResult>(result);
            _mockPublisher.Verify(p => p.PublishDocumentCreated(It.Is<DocumentDto>(d => d.Id == testId && d.Title == "Updated Doc")), Times.Once);
            Assert.Equal("Updated Doc", existingDocument.Title);
        }

        #endregion

        #region SearchByFuzzy Tests

        [Fact]
        public async Task SearchByFuzzy_EmptySearchTerm_ReturnsBadRequest()
        {
            string emptySearchTerm = "";

            var result = await _controller.SearchByFuzzy(emptySearchTerm);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        [Fact]
        public async Task SearchByFuzzy_ValidSearchTermWithResults_ReturnsOkWithDocuments()
        {
            string searchTerm = "test";

            var jsonWithHits = @"
            {
              ""took"": 1,
              ""timed_out"": false,
              ""_shards"": {""total"":1,""successful"":1,""skipped"":0,""failed"":0},
              ""hits"": {
                ""total"": {""value"":2,""relation"":""eq""},
                ""hits"": [
                  {""_index"":""documents"",""_id"":""1"",""_score"":1.0,""_source"":{""id"":1,""title"":""Doc1"",""content"":""Some content""}},
                  {""_index"":""documents"",""_id"":""2"",""_score"":1.0,""_source"":{""id"":2,""title"":""Doc2"",""content"":""Other content""}}
                ]
              }
            }";

            var validResponse = DeserializeSearchResponse(jsonWithHits);

            _elasticClientMock
                .Setup(m => m.SearchAsync<Document>(It.IsAny<SearchRequest<Document>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(validResponse);

            var result = await _controller.SearchByFuzzy(searchTerm);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedDocs = Assert.IsAssignableFrom<IEnumerable<Document>>(okResult.Value);
            Assert.Equal(2, returnedDocs.Count());
        }

        [Fact]
        public async Task SearchByFuzzy_ValidSearchTermNoResults_ReturnsNotFound()
        {
            string searchTerm = "nohits";

            var jsonNoHits = @"
            {
              ""took"": 1,
              ""timed_out"": false,
              ""_shards"": {""total"":1,""successful"":1,""skipped"":0,""failed"":0},
              ""hits"": {
                ""total"": {""value"":0,""relation"":""eq""},
                ""hits"": []
              }
            }";

            var noHitsResponse = DeserializeSearchResponse(jsonNoHits);

            _elasticClientMock
                .Setup(m => m.SearchAsync<Document>(It.IsAny<SearchRequest<Document>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(noHitsResponse);

            var result = await _controller.SearchByFuzzy(searchTerm);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task SearchByFuzzy_InvalidResponse_ReturnsServerError()
        {
            string searchTerm = "test";

            // Wir simulieren einen Fehler, indem wir eine Exception werfen.
            _elasticClientMock
                .Setup(m => m.SearchAsync<Document>(It.IsAny<SearchRequest<Document>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Simulierter Fehler"));

            var result = await _controller.SearchByFuzzy(searchTerm);

            var serverErrorResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, serverErrorResult.StatusCode);

            // Hier verwenden wir dynamic anstatt IDictionary<string, object>
            dynamic returnValue = serverErrorResult.Value;
            Assert.NotNull(returnValue);
            Assert.Equal("Failed to search documents", (string)returnValue.message);
            Assert.Equal("Simulierter Fehler", (string)returnValue.details);
        }

        #endregion
    }
}
