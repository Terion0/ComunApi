using ComunApi.Models.DTO.DTOResponse;
using ComunApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ComunApi.DbsContext;
using Microsoft.EntityFrameworkCore;
using ComunApi.Models.Intermediares;

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

        [HttpPost("CrearRespuesta")]
        [Authorize]
        public async Task<IActionResult> CreateResponse([FromBody] ResponseCreateDTO responseDTO)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var thread = await _context.Threads.FindAsync(responseDTO.ThreadId);
            if (thread != null)
            {

                Response response = new Response
                {
                    Content = responseDTO.Content,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ThreadId = responseDTO.ThreadId,
                    ParentId = responseDTO.ParentId,
                    CreatorId = userId,
                    IsDeleted = false,
                    CountResponses = 0,

                };

                _context.Responses.Add(response);

                if (response.ParentId.HasValue)
                {
                    Response padre = await _context.Responses.FindAsync(response.ParentId.Value);

                    if (padre != null)
                    {
                        padre.CountResponses += 1;
                        _context.Responses.Update(padre);
                    }
                    else
                    {
                        _logger.LogWarning($"No se encontró la respuesta padre con ID {response.ParentId}");
                        return BadRequest("Respuesta padre no encontrada.");
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Respuesta creada correctamente.");
                return Ok("Respuesta creada correctamente");
            }
            else {
                _logger.LogInformation("Thread no encontrado");
                return NotFound("Thread no encontrado");
            }
        }

        [HttpPost("LikeResponse/{responseId}")]
        [Authorize]
        public async Task<IActionResult> LikeResponse(int responseId)
        {
            var response = await _context.Responses.FindAsync(responseId);
            if (response != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

       
                var existingLike = await _context.ResponseLikes
                    .FirstOrDefaultAsync(rl => rl.UserId == userId && rl.ResponseId == response.Id);

                if (existingLike != null)
                {
                    _logger.LogWarning("El usuario ya ha dado like a esta respuesta");
                    return BadRequest("Ya has dado like a esta respuesta");
                }

      
                ResponseLikes newLike = new()
                {
                    ResponseId = response.Id,
                    UserId = userId
                };
                _context.ResponseLikes.Add(newLike);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Like a la respuesta realizado");
                return Ok("Like a la respuesta realizado correctamente");
            }
            else
            {
                _logger.LogWarning("No se encontró la respuesta para dar like");
                return NotFound("No se encuentra la respuesta");
            }
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
                    Responses = response.CountResponses,
                    Likes = _context.ResponseLikes.Count(rl => rl.ResponseId == response.Id)


                })
                .ToListAsync();
            _logger.LogInformation("Saca respuestas principales");
            return Ok(responses);
        }
     
        [HttpGet("{responseId}/Replies")]
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
                    Likes = _context.ResponseLikes.Count(rl => rl.ResponseId == response.Id)
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
                return Ok("No hay respuestas para esta respuesta.");
            }
        }
  
        [HttpGet("{idResponse}")]
        public async Task<IActionResult> GetResponseById(int idResponse)
        {
            var response = await _context.Responses.FindAsync(idResponse);

            if (response != null)
            {
                var responseDTO = new ResponseDetailDTO
                {
                    Id = response.Id,
                    Content = response.Content,
                    CreatorId = response.CreatorId,
                    Responses = response.CountResponses,
                    Likes = _context.ResponseLikes.Count(rl => rl.ResponseId == response.Id)
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
        public async Task<IActionResult> GetResponsesByCreatorId(int creatorId)
        { 
                var responses = await _context.Responses
                    .Where(r => r.CreatorId == creatorId)
                    .Select(r => new ResponseDetailDTO
                    {
                        Id = r.Id,
                        CreatorId=r.CreatorId,
                        Content = r.Content,
                        Likes = _context.ResponseLikes.Count(rl => rl.ResponseId == r.Id)
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

        [HttpGet("byLike/{creatorId}")]
        public async Task<IActionResult> GetResponsesByLikeId(int creatorId)
        {

            var responseLikes = await _context.ResponseLikes
                .Where(rl => rl.UserId == creatorId)
                .Include(rl => rl.Response) 
                .Select(rl => new ResponseDetailDTO
                {
                    Id = rl.Response.Id, 
                    Content = rl.Response.Content,
                    Likes = _context.ResponseLikes.Count(r => r.ResponseId == rl.ResponseId)
                })
                .ToListAsync();

            if (responseLikes.Any())
            {
                _logger.LogInformation("Respuestas que el usuario ha dado like");
                return Ok(responseLikes);
            }
            else
            {
                _logger.LogInformation("El usuario no ha dado like a ninguna respuesta");
                return NotFound("No se encontraron respuestas a las que haya dado like.");
            }
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
                    return Ok();
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

        [HttpPut("community/{idCom}/Response{idResponse}")]
        [Authorize]
        public async Task<IActionResult> DeleteResponse(int idResponse, int idCom)
        {
            bool canDeleteResponse = false;
            var response = await _context.Responses.FindAsync(idResponse);

            if (response != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var userRol = await _context.CommunityRoles
                .Where(ucr => ucr.UserId == userId && ucr.CommunityId == idCom)
                .Include(ucr => ucr.Role)
                .Select(ucr => ucr.Role)
                .FirstOrDefaultAsync();

                if (userRol != null)
                {
                    if (userRol.CanDeleteResponses)
                    {
                        canDeleteResponse = userRol.CanDeleteResponses;
                    }
                }
                if (response.CreatorId == userId || canDeleteResponse)
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

        [HttpDelete("UnlikeResponse/{responseId}")]
        [Authorize]
        public async Task<IActionResult> UnlikeResponse(int responseId)
        {
            var response = await _context.Responses.FindAsync(responseId);
            if (response != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                var existingLike = await _context.ResponseLikes
                    .FirstOrDefaultAsync(rl => rl.UserId == userId && rl.ResponseId == response.Id);

                if (existingLike == null)
                {
                    _logger.LogWarning("El usuario no ha dado like a esta respuesta");
                    return BadRequest("No has dado like a esta respuesta");
                }

                _context.ResponseLikes.Remove(existingLike);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Like a la respuesta eliminado");
                return Ok("Like a la respuesta eliminado correctamente");
            }
            else
            {
                _logger.LogWarning("No se encontró la respuesta para eliminar el like");
                return NotFound("No se encuentra la respuesta");
            }
        }
    }
}
