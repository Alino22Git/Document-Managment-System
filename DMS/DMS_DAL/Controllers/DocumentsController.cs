using Microsoft.AspNetCore.Mvc;
using DMS_DAL.Repositories;
using DMS_DAL.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DMS_DAL.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentRepository _repository;

        public DocumentsController(IDocumentRepository repository)
        {
            _repository = repository;
        }
    }
}
