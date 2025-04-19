using ComunApi.DbsContext;
using ComunApi.Models.DTO.DTOCommunity;
using ComunApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ComunApi.Models.DTO.DTOThread;

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
        [Authorize]
        public async Task<IActionResult> GetCommunitiesByCreatorId(int creatorId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (creatorId == userId)
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
            else
            {
                _logger.LogInformation("Acceso denegado por acceso a comundiades ajenas");
                return Forbid("Solo puedes acceder a tus propias comunidades.");
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
    }

}
