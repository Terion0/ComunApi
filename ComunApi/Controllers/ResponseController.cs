using ComunApi.Models.DTO.DTOResponse;
using ComunApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ComunApi.DbsContext;
using Microsoft.EntityFrameworkCore;

namespace ComunApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResponsesController : ControllerBase
    {
        private readonly CoDbContext _context;
        private readonly ILogger<ResponsesController> _logger;

        public ResponsesController(CoDbContext context, ILogger<ResponsesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("{threadId}/Responses")]
        public async Task<IActionResult> GetMainResponses(int threadId)
        {  
            var responses = await _context.Responses
                .Where(response => response.ThreadId == threadId && response.ParentId == null)
                .Select(response => new ResponseDetailDTO
                {
                    Id = response.Id,
                    Content = response.Content,
                    IsDeleted = response.IsDeleted,
                    CreatorId = response.CreatorId,
                    Responses = response.CountResponses

                })
                .ToListAsync();
            _logger.LogInformation("Saca respuestas principales");
            return Ok(responses);
        }

     
        [HttpGet("Responses/{responseId}/Replies")]
        public async Task<IActionResult> GetRepliesForResponse(int responseId)
        {
            var replies = await _context.Responses
                .Where(response => response.ParentId == responseId && !response.IsDeleted)
                .Select(response => new ResponseDetailDTO
                {
                    Id = response.Id,
                    Content = response.Content,
                    IsDeleted = response.IsDeleted,
                    CreatorId = response.CreatorId,
                })
                .ToListAsync();

            if (replies.Any())
            {
                _logger.LogInformation("Respuestas sacadas con exito.");
                return Ok(replies);
            }
            else
            {
                _logger.LogWarning("No hay respuestas");
                return NotFound("No hay respuestas para esta respuesta.");
            }
        }
  
        [HttpGet("Response/{id}")]
        public async Task<IActionResult> GetResponseById(int id)
        {
            var response = await _context.Responses.FindAsync(id);

            if (response != null)
            {
                var responseDTO = new ResponseDetailDTO
                {
                    Id = response.Id,
                    Content = response.Content,
                    CreatorId = response.CreatorId,
                    Responses = response.CountResponses,       
                };
                _logger.LogInformation("Respuesta sacada correctamente.");
                return Ok(responseDTO);
            }
            else
            {
                _logger.LogWarning("Respuesta no existe");
                return NotFound();
            }
        }

        [HttpGet("bycreator/{creatorId}")]
        [Authorize]
        public async Task<IActionResult> GetResponsesByCreatorId(int creatorId)
        {
          
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (creatorId == userId)
            {
                var responses = await _context.Responses
                    .Where(r => r.CreatorId == creatorId)
                    .Select(r => new ResponseDetailDTO
                    {
                        Id = r.Id,
                        Content = r.Content,         
                    })
                    .ToListAsync();

                if (responses.Any())
                {
                    return Ok(responses);
                }
                else
                {
                    _logger.LogInformation("No hay respuestas");
                    return NotFound("No se encontraron respuestas creadas por este usuario.");
                }
            }
            else
            {
                _logger.LogInformation("Intento de entrar en listado de respuestas ajeno.");
                return Forbid("Solo puedes acceder a tus propias respuestas.");
            }
        }

        [HttpPost("CrearRespuesta")]
        [Authorize]
        public async Task<IActionResult> CreateResponse([FromBody] ResponseCreateDTO responseDTO)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        
            Response response = new Response
            {
                Content = responseDTO.Content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ThreadId = responseDTO.ThreadId,
                ParentId = responseDTO.ParentId,
                CreatorId = userId,
                IsDeleted = false
            };
            _context.Responses.Add(response);
            if (response.ParentId != -1)
            {
                Response padre = await _context.Responses.FindAsync(response.ParentId);
                padre.CountResponses = +1;
                _context.Responses.Update(padre);
            }
            await _context.SaveChangesAsync();

            _logger.LogInformation("Respuesta creada correctamente.");
            return Ok("Respuesta creada correctamente");
        }

      
        [HttpPut("UpdateaRespuesta")]
        [Authorize]
        public async Task<IActionResult> UpdateResponse([FromBody] ResponseUpdateDTO responseDTO)
        {
            var response = await _context.Responses.FindAsync(responseDTO.Id);

            if (response != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                if (response.CreatorId == userId)
                {
                    response.Content = responseDTO.Content;
                    response.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Respuesta actualizada correctamente.");
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning("El usuario no es el creador");
                    return Forbid();
                }
            }
            else
            {
                _logger.LogWarning("Respuesta no encontrada");
                return NotFound();
            }
        }

   
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteResponse(int id)
        {
            var response = await _context.Responses.FindAsync(id);

            if (response != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                if (response.CreatorId == userId)
                {
                    response.IsDeleted = true;                
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Respuesta marcada como eliminada");
                    return Ok("Respuesta marcada como eliminada");
                }
                else
                {
                    _logger.LogWarning("Respuesta no marcada como eliminada");
                    return Forbid("Respuesta no marcada como eliminada");
                }
            }
            else
            {
                _logger.LogWarning("Respuesta  no encontrada.");
                return NotFound("Respuesta  no encontrada.");
            }
        }
    }
}
