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
using ComunApi.Models.DTO.DTOPages;
using ProfApi.Services;

namespace ComunApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] 
    public class CommunityController : ControllerBase
    {
        private readonly CoDbContext _context;
        private readonly ILogger<CommunityController> _logger;
        private readonly FileFolderService _fileFolderService;
        public CommunityController(CoDbContext context, ILogger<CommunityController> logger, FileFolderService fileFolderService)
        {
            _fileFolderService = fileFolderService;
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateCommunity([FromForm] CommunityCreateDTO CommunityDTO, IFormFile? profileImage, IFormFile? bannerImage)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            long maxSize = 5 * 1024 * 1024;

            if (string.IsNullOrEmpty(   CommunityDTO.ComName))
            {
                return BadRequest("El nombre de la comunidad es obligatorio.");
            }

            string profileImagePath = ""; 
            string bannerImagePath = "";

            if (profileImage != null && profileImage.Length > 0)
            {
                string profileExtension = Path.GetExtension(profileImage.FileName).ToLower();
                if (!_fileFolderService.IsValidExtension(profileExtension))
                    return BadRequest("Solo se permiten archivos JPG o PNG para la imagen de perfil.");

                if (!_fileFolderService.IsValidFileSize(profileImage.Length, maxSize))
                    return BadRequest("La imagen de perfil no debe exceder los 5 MB.");

                profileImagePath = await _fileFolderService.SaveFileAsync(profileImage, CommunityDTO.ComName, "community_images/Profile");
                if (profileImagePath == null)
                    return BadRequest("Error al guardar la imagen de perfil.");
            }

            if (bannerImage != null && bannerImage.Length > 0)
            {
                var bannerExtension = Path.GetExtension(bannerImage.FileName).ToLower();
                if (!_fileFolderService.IsValidExtension(bannerExtension))
                    return BadRequest("Solo se permiten archivos JPG o PNG para el banner.");

                if (!_fileFolderService.IsValidFileSize(bannerImage.Length, maxSize))
                    return BadRequest("El banner no debe exceder los 5 MB.");

                bannerImagePath = await _fileFolderService.SaveFileAsync(bannerImage, CommunityDTO.ComName, "community_images/Banner");
                if (bannerImagePath == null)
                    return BadRequest("Error al guardar el banner.");
            }

            var community = new Community
            {
                ComName = CommunityDTO.ComName,
                ComPicture = profileImagePath,
                ComBanner = bannerImagePath,
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
                community.CountSubscriptions += 1;
                _context.CommunitySubscriptions.Add(newSub);
                _context.Communities.Update(community);
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
                    Subscriptions = community.CountSubscriptions
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

        [HttpGet("All")]
        public async Task<IActionResult> GetCommunities([FromQuery] int pageNumber = 1, [FromQuery] string name = null)
        {
            int PageSize = 10;

            var communitiesQuery = _context.Communities.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                communitiesQuery = communitiesQuery.Where(c => c.ComName.Contains(name));
            }

            var total = await communitiesQuery.CountAsync();

            var communities = await communitiesQuery
                .OrderBy(c => c.Id)
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .Select(c => new CommunityListDTO
                {
                    Id = c.Id,
                    ComName = c.ComName,
                    ComPicture = c.ComPicture
                })
                .ToListAsync();

            PageDTO<CommunityListDTO> result = new()
            {
                Data = communities,
                PageNumber = pageNumber,
                PageSize = PageSize,
                TotalRecords = total,
            };

            _logger.LogInformation("Comunidades paginadas con filtro por nombre");
            return Ok(result);
        }


        [HttpGet("bycreator/{creatorId}")]
        public async Task<IActionResult> GetCommunitiesByCreatorId(int creatorId, [FromQuery] int pageNumber = 1, [FromQuery] string name = null)
        {
            int pageSize = 10;

            var communitiesQuery = _context.Communities
                .Where(c => c.CreatorId == creatorId);

            if (!string.IsNullOrEmpty(name))
            {
                communitiesQuery = communitiesQuery.Where(c => c.ComName.Contains(name));
            }

            var total = await communitiesQuery.CountAsync();

            var communitiesCreator = await communitiesQuery
                .OrderBy(c => c.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CommunityListDTO
                {
                    Id = c.Id,
                    ComName = c.ComName,
                    ComPicture = c.ComPicture
                })
                .ToListAsync();

            if (!communitiesCreator.Any())
            {
                _logger.LogInformation("Usuario sin comunidades");
                return NotFound("No se encontraron comunidades creadas por este usuario.");
            }

            var result = new PageDTO<CommunityListDTO>
            {
                Data = communitiesCreator,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = total
            };

            _logger.LogInformation("Comunidades por creador");
            return Ok(result);
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

        [HttpPut("")]
        [Authorize]
        public async Task<IActionResult> UpdateCommunity([FromForm] CommunityUpdateDTO CommunityDTO, IFormFile? newProfileImage, IFormFile? newBannerImage)
        {
            var community = await _context.Communities.FindAsync(CommunityDTO.Id);
            if (community != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                if (community.CreatorId == userId)
                {
                    long maxSize = 5 * 1024 * 1024;

                    if (newProfileImage != null && newProfileImage.Length > 0)
                    {
                        string profileExtension = Path.GetExtension(newProfileImage.FileName).ToLower();

                        if (!_fileFolderService.IsValidExtension(profileExtension))
                            return BadRequest("Solo se permiten archivos JPG o PNG para la imagen de perfil.");

                        if (!_fileFolderService.IsValidFileSize(newProfileImage.Length, maxSize))
                            return BadRequest("La imagen de perfil no debe exceder los 5 MB.");

                        if (!string.IsNullOrEmpty(community.ComPicture))
                        {
                            string oldProfilePath = Path.Combine(Directory.GetCurrentDirectory(), community.ComPicture.TrimStart('/'));
                            _fileFolderService.DeleteFile(oldProfilePath);
                        }

                        var newPath = await _fileFolderService.SaveFileAsync(newProfileImage, community.ComName, "community_images/Profile");
                        if (newPath == null)
                            return BadRequest("Error al guardar la nueva imagen de perfil.");

                        community.ComPicture = newPath;
                    }

                    if (newBannerImage != null && newBannerImage.Length > 0)
                    {
                        string bannerExtension = Path.GetExtension(newBannerImage.FileName).ToLower();

                        if (!_fileFolderService.IsValidExtension(bannerExtension))
                            return BadRequest("Solo se permiten archivos JPG o PNG para el banner.");

                        if (!_fileFolderService.IsValidFileSize(newBannerImage.Length, maxSize))
                            return BadRequest("El banner no debe exceder los 5 MB.");

                        if (!string.IsNullOrEmpty(community.ComBanner))
                        {
                            string oldBannerPath = Path.Combine(Directory.GetCurrentDirectory(), community.ComBanner.TrimStart('/'));
                            _fileFolderService.DeleteFile(oldBannerPath);
                        }

                        var newBannerPath = await _fileFolderService.SaveFileAsync(newBannerImage, community.ComName, "community_images/Banner");
                        if (newBannerPath == null)
                            return BadRequest("Error al guardar el nuevo banner.");

                        community.ComBanner = newBannerPath;
                    }

                    if (!string.IsNullOrEmpty(CommunityDTO.ComName))
                    {
                        community.ComName = CommunityDTO.ComName;
                    }

                    community.ComDescription = CommunityDTO.ComDescription;
                    community.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Comunidad actualizada");

                    return Ok("Comunidad actualizada correctamente");
                }
                else
                {
                    _logger.LogWarning("No eres el dueño");
                    return Forbid("No eres el dueño de esta comunidad");
                }
            }
            else
            {
                _logger.LogWarning("No se encontró la comunidad con ID {CommunityId}", CommunityDTO.Id);
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
                    if (!string.IsNullOrEmpty(community.ComPicture))
                    {
                        var profileImagePath = Path.Combine(Directory.GetCurrentDirectory(), community.ComPicture.TrimStart('/'));
                        if (!_fileFolderService.DeleteFile(profileImagePath))
                        {
                            _logger.LogWarning("No se pudo eliminar la imagen de perfil.");
                        }
                        else
                        {
                            _logger.LogInformation("Imagen de perfil eliminada.");
                        }
                    }

                    if (!string.IsNullOrEmpty(community.ComBanner))
                    {
                        var bannerImagePath = Path.Combine(Directory.GetCurrentDirectory(), community.ComBanner.TrimStart('/'));
                        if (!_fileFolderService.DeleteFile(bannerImagePath))
                        {
                            _logger.LogWarning("No se pudo eliminar la imagen de banner.");
                        }
                        else
                        {
                            _logger.LogInformation("Imagen de banner eliminada.");
                        }
                    }

                    _context.Communities.Remove(community);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Comunidad eliminada");
                    return Ok("Comunidad eliminada");
                }
                else
                {
                    _logger.LogWarning("No es su comunidad");
                    return Forbid("No es su comunidad");
                }
            }
            else
            {
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
                    community.CountSubscriptions -= 1;
                    _context.CommunitySubscriptions.Remove(remSub);
                    _context.Communities.Update(community);
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
