using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common;
using IntelligenceHub.Common.Extensions;
using static IntelligenceHub.Common.GlobalVariables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace IntelligenceHub.Controllers
{
    /// <summary>
    /// This controller is used to manage RAG indexes.
    /// </summary>
    [Route("Rag")]
    [ApiController]
    public class RagController : ControllerBase
    {
        private readonly IRagLogic _ragLogic;

        /// <summary>
        /// Initializes a new instance of the <see cref="RagController"/> class.
        /// </summary>
        /// <param name="ragLogic">The business logic for managing RAG indexes.</param>
        public RagController(IRagLogic ragLogic)
        {
            _ragLogic = ragLogic;
        }

        /// <summary>
        /// This endpoint is used to get a RAG index by name.
        /// </summary>
        /// <param name="index">The name of the index.</param>
        /// <returns>An <see cref="IActionResult"/> containing the index metadata.</returns>
        [HttpGet]
        [Route("Index/{index}")]
        [SwaggerOperation(OperationId = "GetIndexAsync")]
        [ProducesResponseType(typeof(IndexMetadata), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get([FromRoute] string index)
        {
            try
            {
                if (string.IsNullOrEmpty(index)) return BadRequest($"Invalid index name: '{index}'");
                var response = await _ragLogic.GetRagIndex(index);
                if (response.IsSuccess) return Ok(response.Data);
                else if (response.StatusCode == APIResponseStatusCodes.NotFound) return NotFound(response.ErrorMessage);
                return BadRequest(response.ErrorMessage);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to get all RAG indexes.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> containing a list of index metadata.</returns>
        [HttpGet]
        [Route("Index/All")]
        [SwaggerOperation(OperationId = "GetAllIndexesAsync")]
        [ProducesResponseType(typeof(IEnumerable<IndexMetadata>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var response = await _ragLogic.GetAllIndexesAsync();
                return Ok(response.Data ?? new List<IndexMetadata>());
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to create a new RAG index.
        /// </summary>
        /// <param name="indexDefinition">The definition of the index.</param>
        /// <returns>An <see cref="IActionResult"/> containing the created index metadata.</returns>
        [HttpPost]
        [Route("Index")]
        [SwaggerOperation(OperationId = "CreateIndexAsync")]
        [ProducesResponseType(typeof(IndexMetadata), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateIndex([FromBody] IndexMetadata indexDefinition)
        {
            try
            {
                if (indexDefinition is null) return BadRequest("The request body is malformed.");
                var response = await _ragLogic.CreateIndex(indexDefinition);
                if (response.IsSuccess) return Ok(indexDefinition);
                else if (response.StatusCode == APIResponseStatusCodes.BadRequest) return BadRequest(response.ErrorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, response.ErrorMessage);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to configure an existing RAG index.
        /// </summary>
        /// <param name="index">The name of the index.</param>
        /// <param name="indexDefinition">The new definition of the index.</param>
        /// <returns>An <see cref="IActionResult"/> containing the updated index metadata.</returns>
        [HttpPost]
        [Route("Index/Configure/{index}")]
        [SwaggerOperation(OperationId = "ConfigureIndexAsync")]
        [ProducesResponseType(typeof(IndexMetadata), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConfigureIndex([FromRoute] string index, [FromBody] IndexMetadata indexDefinition)
        {
            try
            {
                if (indexDefinition is null) return BadRequest("The request body is malformed.");
                var response = await _ragLogic.ConfigureIndex(indexDefinition);
                if (response.IsSuccess) return Ok(indexDefinition);
                else if (response.StatusCode == APIResponseStatusCodes.NotFound) return NotFound(response.ErrorMessage);
                else if (response.StatusCode == APIResponseStatusCodes.InternalError) return StatusCode(StatusCodes.Status500InternalServerError, response.ErrorMessage);
                return BadRequest(response.ErrorMessage);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to query a RAG index.
        /// </summary>
        /// <param name="index">The name of the index.</param>
        /// <param name="query">The query to perform against the index.</param>
        /// <returns>An <see cref="IActionResult"/> containing a collection of documents.</returns>
        [HttpGet]
        [Route("Index/{index}/Query/{query}")]
        [SwaggerOperation(OperationId = "QueryIndexAsync")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> QueryIndex([FromRoute] string index, [FromRoute] string query)
        {
            try
            {
                if (string.IsNullOrEmpty(query)) return BadRequest("The required route parameter, 'query', is null or empty.");
                var response = await _ragLogic.QueryIndex(index, query);
                if (response.IsSuccess) return Ok(response.Data);
                else if (response.StatusCode == APIResponseStatusCodes.NotFound) return NotFound(response.ErrorMessage);
                return BadRequest(response.ErrorMessage);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to run an index update.
        /// </summary>
        /// <param name="index">The name of the index.</param>
        /// <returns>An empty <see cref="IActionResult"/>.</returns>
        [HttpPost]
        [Route("Index/{index}/Run")]
        [SwaggerOperation(OperationId = "RunIndexUpdateAsync")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RunIndexUpdate([FromRoute] string index)
        {
            try
            {
                if (string.IsNullOrEmpty(index)) return BadRequest($"Invalid index name: '{index}'");
                var response = await _ragLogic.RunIndexUpdate(index);
                if (response.IsSuccess) return NoContent();
                else if (response.StatusCode == APIResponseStatusCodes.NotFound) return NotFound(response.ErrorMessage);
                else if (response.StatusCode == APIResponseStatusCodes.InternalError) return StatusCode(StatusCodes.Status500InternalServerError, response.ErrorMessage);
                return BadRequest(response.ErrorMessage);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to delete a RAG index.
        /// </summary>
        /// <param name="index">The name of the index.</param>
        /// <returns>An empty <see cref="IActionResult"/>.</returns>
        [HttpDelete]
        [Route("Index/Delete/{index}")]
        [SwaggerOperation(OperationId = "DeleteIndexAsync")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteIndex([FromRoute] string index)
        {
            try
            {
                if (string.IsNullOrEmpty(index)) return BadRequest($"Invalid index name: '{index}'");
                var response = await _ragLogic.DeleteIndex(index);
                if (response.IsSuccess) return NoContent();
                else if (response.StatusCode == APIResponseStatusCodes.NotFound) return NotFound(response.ErrorMessage);
                else if (response.StatusCode == APIResponseStatusCodes.InternalError) return StatusCode(StatusCodes.Status500InternalServerError, response.ErrorMessage);
                else return BadRequest(response.ErrorMessage);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to get all documents in a RAG index.
        /// </summary>
        /// <param name="index">The name of the index.</param>
        /// <param name="count">The number of documents to retrieve in the current batch.</param>
        /// <param name="page">The number of pages to offset the collection.</param>
        /// <returns>An <see cref="IActionResult"/> containing a collection of documents.</returns>
        [HttpGet]
        [Route("Index/{index}/Document/{count}/Page/{page}")]
        [SwaggerOperation(OperationId = "GetIndexDocumentsAsync")]
        [ProducesResponseType(typeof(IEnumerable<IndexDocument>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllDocuments([FromRoute] string index, [FromRoute] int count, [FromRoute] int page)
        {
            try
            {
                if (page < 1) return BadRequest("Page must be 1 or greater");
                if (count < 1) return BadRequest("Count must be 1 or greater");
                if (string.IsNullOrEmpty(index)) return BadRequest($"Invalid index name: '{index}'");

                var response = await _ragLogic.GetAllDocuments(index, count, page);
                if (response.IsSuccess) return Ok(response.Data ?? new List<IndexDocument>());
                return BadRequest(response.ErrorMessage);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to get a document from a RAG index.
        /// </summary>
        /// <param name="index">The name of the index.</param>
        /// <param name="document">The name of the document.</param>
        /// <returns>An <see cref="IActionResult"/> containing the document.</returns>
        [HttpGet]
        [Route("index/{index}/document/{document}")]
        [SwaggerOperation(OperationId = "GetDocumentAsync")]
        [ProducesResponseType(typeof(IndexDocument), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDocument([FromRoute] string index, [FromRoute] string document)
        {
            try
            {
                var response = await _ragLogic.GetDocument(index, document);
                if (response.IsSuccess) return Ok(response.Data);
                else if (response.StatusCode == APIResponseStatusCodes.NotFound) return NotFound(response.ErrorMessage);
                return BadRequest(response.ErrorMessage);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to upsert documents to a RAG index.
        /// </summary>
        /// <param name="index">The name of the index.</param>
        /// <param name="documentUpsertRequest">An array of documents to upsert.</param>
        /// <returns>An <see cref="IActionResult"/> containing the upserted documents.</returns>
        [HttpPost]
        [Route("index/{index}/Document")]
        [SwaggerOperation(OperationId = "UpsertDocumentAsync")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpsertDocuments([FromRoute] string index, [FromBody] RagUpsertRequest documentUpsertRequest)
        {
            try
            {
                if (documentUpsertRequest == null || documentUpsertRequest.Documents.Count < 1) return BadRequest("The request body is malformed or contains less than 1 document.");
                if (string.IsNullOrEmpty(index)) return BadRequest($"Invalid index name: '{index}'.");
                foreach (var doc in documentUpsertRequest.Documents) if (doc.Title.Contains('/') || doc.Title.Contains('\\')) return BadRequest($"Document titles cannot contain slashes. Document: {doc.Title}");
                var response = await _ragLogic.UpsertDocuments(index, documentUpsertRequest);
                if (response.IsSuccess) return Ok(documentUpsertRequest);
                else if (response.StatusCode == APIResponseStatusCodes.NotFound) return NotFound(response.ErrorMessage);
                else if (response.StatusCode == APIResponseStatusCodes.InternalError) return StatusCode(StatusCodes.Status500InternalServerError, response.ErrorMessage);
                else return BadRequest(response.ErrorMessage);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to delete documents from a RAG index.
        /// </summary>
        /// <param name="index">The name of the index.</param>
        /// <param name="commaDelimitedDocNames">A comma delimited string of document titles.</param>
        /// <returns>An <see cref="IActionResult"/> containing the number of documents that were deleted.</returns>
        [HttpDelete]
        [Route("index/{index}/Document/{commaDelimitedDocNames}")]
        [SwaggerOperation(OperationId = "DeleteDocumentsAsync")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteDocuments([FromRoute] string index, [FromRoute] string commaDelimitedDocNames)
        {
            try
            {
                var documents = commaDelimitedDocNames.ToStringArray();
                if (string.IsNullOrEmpty(index)) return BadRequest($"Invalid index name: '{index}'.");
                if (documents.Length < 1) return BadRequest("No document names were provided in the request route.");
                var response = await _ragLogic.DeleteDocuments(index, documents);
                if (response.IsSuccess) return Ok(response.Data);
                else if (response.Data != commaDelimitedDocNames.ToStringArray().Length) return NotFound($"Some or all of the provided documents were not found in the index '{index}'.");
                else return BadRequest(response.ErrorMessage);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }
    }
}
