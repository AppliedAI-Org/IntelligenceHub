using IntelligenceHub.API.DTOs;
using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.Business.Interfaces;
using IntelligenceHub.Common;
using IntelligenceHub.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntelligenceHub.Controllers
{
    [Route("Rag")]
    [ApiController]
    [Authorize(Policy = "AdminPolicy")]
    public class RagController : ControllerBase
    {
        private readonly IRagLogic _ragLogic;

        public RagController(IRagLogic ragLogic) 
        {
            _ragLogic = ragLogic;
        }

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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

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
                if (response) return Ok();
                else return BadRequest();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

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
                if (response) return Ok();
                else return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

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
                if (response) return Ok();
                else return BadRequest();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

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
                if (response) return Ok();
                else return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }
    }
}
