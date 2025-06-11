using ComunApi.Models.DTO.DTOResponse;
using ComunApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ComunApi.DbsContext;
using Microsoft.EntityFrameworkCore;
using ComunApi.Models.Intermediares;
using System.Threading;
using ComunApi.Models.DTO.DTOPages;
using ComunApi.Models.DTO.DTOThread;

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
                        return BadRequest(new { error = "Respuesta padre no encontrada." });
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Respuesta creada correctamente.");
                return Ok(new { error = "Respuesta creada correctamente" });
            }
            else {
                _logger.LogInformation("Thread no encontrado");
                return NotFound(new { error = "Thread no encontrado" });
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
                else
                {

                    ResponseLikes newLike = new()
                    {
                        ResponseId = response.Id,
                        UserId = userId
                    };
                    response.CountLikes += 1; 
                    _context.ResponseLikes.Add(newLike);
                    _context.Responses.Update(response);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Like a la respuesta realizado");
                    return Ok(new { error = "Like a la respuesta realizado correctamente" });
                }
            }
            else
            {
                _logger.LogWarning("No se encontró la respuesta para dar like");
                return NotFound(new { error = "No se encuentra la respuesta" });
            }
        }

     

        [HttpGet("{threadId}/Responses")]
        public async Task<IActionResult> GetResponses(int threadId, [FromQuery] int? parentId = null, [FromQuery] int pageNumber = 1)
        {
            int pageSize = 10;

            var query = _context.Responses.AsQueryable();

            query = query.Where(r => r.ThreadId == threadId);

            if (parentId == null)
                query = query.Where(r => r.ParentId == null);
            else
                query = query.Where(r => r.ParentId == parentId);

            var totalRecords = await query.CountAsync();

            var responses = await query
                .OrderBy(r => r.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ResponseDetailDTO
                {
                    Id = r.Id,
                    Content = r.Content,
                    IsDeleted = r.IsDeleted,
                    CreatorId = r.CreatorId,
                    Responses = r.CountResponses,
                    Likes = r.CountLikes
                })
                .ToListAsync();

            var result = new PageDTO<ResponseDetailDTO>
            {
                Data = responses,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };

            _logger.LogInformation("Respuestas paginadas recuperadas con éxito.");
            return Ok(result);
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
                    Likes = response.CountLikes
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
        public async Task<IActionResult> GetResponsesByCreatorId(int creatorId, [FromQuery] int pageNumber = 1)
        {
            int pageSize = 10;

            var totalResponses = await _context.Responses
                .Where(r => r.CreatorId == creatorId)
                .CountAsync();

            var responses = await _context.Responses
                .Where(r => r.CreatorId == creatorId)
                .OrderBy(r => r.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ResponseDetailDTO
                {
                    Id = r.Id,
                    CreatorId = r.CreatorId,
                    Content = r.Content,
                    Likes = r.CountLikes
                })
                .ToListAsync();

            if (responses.Any())
            {
                PageDTO<ResponseDetailDTO> result = new()
                {
                    Data = responses,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = totalResponses
                };
                _logger.LogInformation("Respuestas del creador obtenidas con éxito.");
                return Ok(result);
            }
            else
            {
                _logger.LogInformation("No hay respuestas creadas por este usuario.");
                return NotFound(new { error = "No se encontraron respuestas creadas por este usuario." });
            }
        }

        [HttpGet("byLike/{creatorId}")]
        public async Task<IActionResult> GetResponsesByLikeId(int creatorId, [FromQuery] int pageNumber = 1)
        {
            int pageSize = 10;

            var query = _context.ResponseLikes
                .Where(rl => rl.UserId == creatorId)
                .Include(rl => rl.Response); 

            int totalLikedResponses = await query.CountAsync();

            var responseLikes = await query
                .OrderBy(rl => rl.Response.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(rl => new ResponseDetailDTO
                {
                    Id = rl.Response.Id,
                    CreatorId = rl.Response.CreatorId,
                    Content = rl.Response.Content,
                    Likes = rl.Response.CountLikes,
                    IsDeleted = rl.Response.IsDeleted,
                    Responses = rl.Response.CountResponses
                })
                .ToListAsync();

            if (responseLikes.Any())
            {
                PageDTO<ResponseDetailDTO> result = new()
                {
                    Data = responseLikes,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = totalLikedResponses
                };
                _logger.LogInformation("Respuestas que el usuario ha dado like obtenidas con éxito.");
                return Ok(result);
            }
            else
            {
                _logger.LogInformation("El usuario no ha dado like a ninguna respuesta.");
                return NotFound(new { error = "No se encontraron respuestas a las que haya dado like." });
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

        [HttpPut("community/{idCom}/Response/{idResponse}")]
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
                return NotFound(new { error = "Respuesta  no encontrada." });
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
                    return BadRequest(new { error = "No has dado like a esta respuesta" });
                }

                else
                {
                    response.CountLikes -= 1;      
                    _context.ResponseLikes.Remove(existingLike);
                    _context.Responses.Update(response);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Like a la respuesta eliminado");
                    return Ok(new { error = "Like a la respuesta eliminado correctamente" });
                }
            }
            else
            {
                _logger.LogWarning("No se encontró la respuesta para eliminar el like");
                return NotFound(new { error = "No se encuentra la respuesta" });
            }
        }


        [HttpGet("HasLike/{responseId}")]
        [Authorize]
        public async Task<IActionResult> HasUserLikedResponse(int responseId)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            bool hasLike = await _context.ResponseLikes
                .AnyAsync(rl => rl.ResponseId == responseId && rl.UserId == userId);

            return Ok(hasLike);
        }
    }
}
