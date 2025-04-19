using ComunApi.DbsContext;
using ComunApi.Models.DTO.DTOCommunity;
using ComunApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ComunApi.Models.DTO.DTOThread;
using ComunApi.Models.Intermediares;

namespace ComunApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] 
    public class CommunityController : ControllerBase
    {
        private readonly CoDbContext _context;
        private readonly ILogger<CommunityController> _logger;

        public CommunityController(CoDbContext context, ILogger<CommunityController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateCommunity([FromBody] CommunityCreateDTO CommunityDTO)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var community = new Community
            {
                ComName = CommunityDTO.ComName,
                ComPicture = CommunityDTO.ComPicture,
                ComBanner = CommunityDTO.ComBanner,
                ComDescription = CommunityDTO.ComDescription,
                CreatorId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Communities.Add(community);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Comunidad creada:" +community.ComName);

            return Ok("Comunidad creada");
        }

        [HttpPost("SubscribeToCommunity/{comid}")]
        [Authorize]
        public async Task<IActionResult> SubscribeToCommunity(int comid)
        {
            var community = await _context.Communities.FindAsync(comid);
            if (community != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                var existingSub = await _context.CommunitySubscriptions
                    .FirstOrDefaultAsync(cs => cs.UserId == userId && cs.CommunityId == community.Id);

                if (existingSub != null)
                {
                    _logger.LogWarning("El usuario ya está suscrito a esta comunidad");
                    return BadRequest("Ya estás suscrito a esta comunidad");
                }

                CommunitySubscriptions newSub = new()
                {
                    CommunityId = community.Id,
                    UserId = userId
                };
                _context.CommunitySubscriptions.Add(newSub);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Suscripción realizada");
                return Ok("Comunidad suscrita correctamente");
            }
            else
            {
                _logger.LogWarning("No se encontró la comunidad para suscribirse");
                return NotFound("No se encuentra la comunidad");
            }
        }

        [HttpGet("All")]
        public async Task<IActionResult> GetCommunities()
        {  
            var communities = await _context.Communities
                .Select(community => new CommunityListDTO
                {
                    Id = community.Id,
                    ComName = community.ComName,
                    ComPicture = community.ComPicture,
                })
                .ToListAsync();
            _logger.LogInformation("Listado de comunidades");
            return Ok(communities);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCommunityById(int id)
        {
            var community = await _context.Communities.FindAsync(id);
            if (community != null)
            {
                CommunityDetailDTO com = new CommunityDetailDTO
                {
                    Id = community.Id,
                    ComName = community.ComName,
                    ComPicture = community.ComPicture,
                    ComBanner = community.ComBanner,
                    ComDescription = community.ComDescription,
                    CreatorId = community.CreatorId,
                    Subscriptions = _context.CommunitySubscriptions.Count(r => r.CommunityId == community.Id)
                }; 
                _logger.LogInformation("Comunidad sacada");
                return Ok(com);
            }
            else
            {
                _logger.LogWarning("Comunidad no existe");
                return NotFound();
            }
        }
        [HttpGet("bycreator/{creatorId}")]
        public async Task<IActionResult> GetCommunitiesByCreatorId(int creatorId)
        {
          
                var communities = await _context.Communities
                    .Where(c => c.CreatorId == creatorId)
                    .Select(c => new CommunityListDTO
                    {
                        Id = c.Id,
                        ComName = c.ComName,
                        ComPicture = c.ComPicture
                    })
                    .ToListAsync();
                if (communities.Any())
                {
                    _logger.LogInformation("Saca las comundiades por creador");
                    return Ok(communities);
                }
                else
                {
                    _logger.LogInformation("Usuario sin comunidades");
                    return NotFound("No se encontraron comunidades creadas por este usuario.");
                }  
        }

        [Authorize]
        [HttpGet("bysubscrition/")]
        public async Task<IActionResult> GetCommunitiesBySubId()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var comsubs = await _context.CommunitySubscriptions
                .Where(cs => cs.UserId == userId)
                .Select(cs => new CommunityListDTO
                {
                    Id= cs.Community.Id,
                    ComName = cs.Community.ComName,
                    ComPicture = cs.Community.ComPicture
                })
                .ToListAsync();
            if (comsubs.Any()) {
                _logger.LogInformation("Saca las comundiades a las que está subscrito");
                return Ok(comsubs);
            }
            else
            {
                _logger.LogInformation("Usuario sin subscripciones");
                return NotFound("No se encontraron comunidades a las que esté subscritas.");
            }

        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateCommunity([FromBody] CommunityUpdateDTO CommunityDTO)
        {
            var community = await _context.Communities.FindAsync(CommunityDTO.Id);
            if (community != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                if (community.CreatorId == userId)
                {
                    community.ComName = CommunityDTO.ComName;
                    community.ComPicture = CommunityDTO.ComPicture;
                    community.ComBanner = CommunityDTO.ComBanner;
                    community.ComDescription = CommunityDTO.ComDescription;
                    community.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Actualización correcta");
                    return Ok("Comunidad actualizada correctamente");
                }
                else
                {
                    _logger.LogWarning("No eres el dueño");
                    return Forbid();
                }
            }
            else
            {
                _logger.LogWarning("No se encontró la comunidad a actualizar con ID {CommunityId}", CommunityDTO.Id);
                return NotFound("No se encuentra la comunidad");
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteCommunity(int id)
        {
            var community = await _context.Communities.FindAsync(id);
            if (community != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
               

                if (community.CreatorId == userId)
                {
                    _context.Communities.Remove(community);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Comunidad eliminada");
                    return Ok("Comunidad eliminada");
                }
                else {
                    _logger.LogWarning("No es su comunidad");
                    return Forbid("No es su comunidad");
                }
            }
            else {
                _logger.LogWarning("No existe esta comunidad");
                return NotFound("No existe");

            }       
        }

        [HttpDelete("UnsubscribeToCommunity/{comid}")]
        [Authorize]
        public async Task<IActionResult> UnsubscribeToCommunity(int comid)
        {
       
            var community = await _context.Communities.FindAsync(comid);
            if (community != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                var remSub = await _context.CommunitySubscriptions
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.CommunityId == community.Id);

                if (remSub != null)
                {
                    _context.CommunitySubscriptions.Remove(remSub);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Desubscripción realizada");
                    return Ok("Comunidad desubscrita correctamente");
                }
                else
                {
                    _logger.LogWarning("El usuario no está suscrito a esta comunidad");
                    return NotFound("No estás suscrito a esta comunidad");
                }
            }
            else
            {
                _logger.LogWarning("No se encontró la comunidad para desuscribirse");
                return NotFound("No se encuentra la comunidad");
            }
        }
    }

}
