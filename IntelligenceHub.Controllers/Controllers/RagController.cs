using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common;
using IntelligenceHub.Common.Extensions;
using static IntelligenceHub.Common.GlobalVariables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntelligenceHub.Controllers
{
    /// <summary>
    /// This controller is used to manage RAG indexes.
    /// </summary>
    [Route("Rag")]
    [ApiController]
    [Authorize(Policy = ElevatedAuthPolicy)]
    public class RagController : ControllerBase
    {
        private readonly IRagLogic _ragLogic;

        /// <summary>
        /// This controller is used to manage RAG indexes.
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
        /// <returns>The definition of the new index.</returns>
        [HttpGet]
        [Route("Index/{index}")]
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
                if (response == null) return NotFound();
                else return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        /// <summary>
        /// This endpoint is used to get all RAG indexes.
        /// </summary>
        /// <returns>A list of index definitions.</returns>
        [HttpGet]
        [Route("Index/All")]
        [ProducesResponseType(typeof(IEnumerable<IndexMetadata>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var response = await _ragLogic.GetAllIndexesAsync();
                if (response is not null && response.Count() > 0) return Ok(response);
                else return NotFound();
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
        /// <returns>An empty ObjectResult.</returns>
        [HttpPost]
        [Route("Index")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateIndex([FromBody] IndexMetadata indexDefinition)
        {
            try
            {
                if (indexDefinition is null) return BadRequest("The request body is malformed.");
                var response = await _ragLogic.CreateIndex(indexDefinition);
                if (response) return NoContent();
                else return BadRequest();
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
        /// <returns>An empty ObjectResult.</returns>
        [HttpPost]
        [Route("Index/Configure/{index}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConfigureIndex([FromRoute] string index, [FromBody] IndexMetadata indexDefinition)
        {
            try
            {
                if (indexDefinition is null) return BadRequest("The request body is malformed.");
                var response = await _ragLogic.ConfigureIndex(indexDefinition);
                if (response) return NoContent();
                else return NotFound();
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
        /// <returns>An ObjectResult containing a collection of documents.</returns>
        [HttpGet]
        [Route("Index/{index}/Query/{query}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> QueryIndex([FromRoute] string index, [FromRoute] string query)
        {
            try
            {
                var response = await _ragLogic.QueryIndex(index, query);
                if (response == null) return NotFound();
                else if (response.Count > 0) return Ok(response);
                else return BadRequest();
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
        /// <returns>An empty ObjectResult.</returns>
        [HttpPost]
        [Route("Index/{index}/Run")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RunIndexUpdate([FromRoute] string index)
        {
            try
            {
                if (string.IsNullOrEmpty(index)) return BadRequest($"Invalid index name: '{index}'");

                var response = await _ragLogic.RunIndexUpdate(index);
                if (response) return NoContent();
                else return BadRequest();
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
        /// <returns>An empty ObjectResult.</returns>
        [HttpDelete]
        [Route("Index/Delete/{index}")]
        [ProducesResponseType(typeof(CompletionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteIndex([FromRoute] string index)
        {
            try
            {
                if (string.IsNullOrEmpty(index)) return BadRequest($"Invalid index name: '{index}'");

                var response = await _ragLogic.DeleteIndex(index);
                if (response) return NoContent();
                else return NotFound();
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
        /// <returns>An ObjectResult containing a collection of documents.</returns>
        [HttpGet]
        [Route("Index/{index}/Document/{count}/Page/{page}")]
        [ProducesResponseType(typeof(IEnumerable<IndexDocument>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllDocuments([FromRoute] string index, [FromRoute] int count, [FromRoute] int page)
        {
            try
            {
                if (page < 1) return BadRequest("Page must be 1 or greater");
                if (count < 1) return BadRequest("Count must be 1 or greater");
                if (string.IsNullOrEmpty(index)) return BadRequest($"Invalid index name: '{index}'");

                var response = await _ragLogic.GetAllDocuments(index, count, page); // going to need to add pagination here
                if (response != null && response.Count() > 0) return Ok(response);
                else return NotFound();
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
        /// <returns>An ObjectResult containing the document.</returns>
        [HttpGet]
        [Route("index/{index}/document/{document}")]
        [ProducesResponseType(typeof(IndexDocument), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDocument([FromRoute] string index, [FromRoute] string document)
        {
            try
            {
                var response = await _ragLogic.GetDocument(index, document);
                if (response is not null) return Ok(response);
                else return NotFound();
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
        /// <returns>An ObjectResult containing a boolean to indicate success or failure.</returns>
        [HttpPost]
        [Route("index/{index}/Document")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpsertDocuments([FromRoute] string index, [FromBody] RagUpsertRequest documentUpsertRequest)
        {
            try
            {
                if (documentUpsertRequest == null || documentUpsertRequest.Documents.Count < 1) return BadRequest("The request body is malformed or contains less than 1 document");
                if (string.IsNullOrEmpty(index)) return BadRequest($"Invalid index name: '{index}'");
                var response = await _ragLogic.UpsertDocuments(index, documentUpsertRequest);
                if (response) return Ok(response);
                else return BadRequest();
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
        /// <returns>An ObjectResult containing an int indicating the number of documents that were deleted.</returns>
        [HttpDelete]
        [Route("index/{index}/Document/{commaDelimitedDocNames}")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteDocuments([FromRoute] string index, [FromRoute] string commaDelimitedDocNames)
        {
            try
            {
                var documents = commaDelimitedDocNames.ToStringArray();
                if (string.IsNullOrEmpty(index)) return BadRequest($"Invalid index name: '{index}'");
                if (documents.Length < 1) return BadRequest("No document names where provided in the request route.");
                var response = await _ragLogic.DeleteDocuments(index, documents);
                if (response < 1) return NotFound();
                else return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }
    }
}
