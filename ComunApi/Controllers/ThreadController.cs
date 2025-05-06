using ComunApi.DbsContext;
using ComunApi.Models.DTO.DTOCommunity;
using ComunApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ComunApi.Models.DTO.DTOThread;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using ComunApi.Models.Intermediares;
using ComunApi.Models.DTO.DTOPages;
using Microsoft.Extensions.FileProviders;
using ProfApi.Services;
using System.IO;

namespace ComunApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ThreadController : ControllerBase
    {
        private readonly CoDbContext _context;
        private readonly ILogger<ThreadController> _logger;
        private readonly FileFolderService _fileFolderService;

        public ThreadController(CoDbContext context, ILogger<ThreadController> logger, FileFolderService fileFolderService)
        {
            _context = context;
            _logger = logger;
            _fileFolderService = fileFolderService;
        }

       
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateThread([FromForm] ThreadCreateDTO ThreadDTO)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var com = await _context.Communities.FindAsync(ThreadDTO.CommunityId);
            long maxSize = 5 * 1024 * 1024;

            if (string.IsNullOrEmpty(ThreadDTO.Title))
            {
                return BadRequest("El nombre de la comunidad es obligatorio.");
            }

            if (com != null)
            {
                var thread = new ThreadCom
                {
                    Title = ThreadDTO.Title,
                    Content = ThreadDTO.Content,
                    CommunityId = ThreadDTO.CommunityId,
                    CreatorId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
             
                _context.Threads.Add(thread);
                await _context.SaveChangesAsync();

                string profileImagePath="";
                if (ThreadDTO.Images != null)
                {
                    for (int i=0;i<ThreadDTO.Images.Count;i++)
                    {
                        if (ThreadDTO.Images[i] != null && ThreadDTO.Images[i].Length > 0)
                        {
                            string profileExtension = Path.GetExtension(ThreadDTO.Images[i].FileName).ToLower();
                            if (!_fileFolderService.IsValidExtension(profileExtension))
                                return BadRequest("Solo se permiten archivos JPG o PNG para la imagen de perfil.");

                            if (!_fileFolderService.IsValidFileSize(ThreadDTO.Images[i].Length, maxSize))
                                return BadRequest("La imagen de perfil no debe exceder los 5 MB.");

                            string picName = $"{thread.Title}{i}{ThreadDTO.Images[i].FileName}";
                            profileImagePath = await _fileFolderService.SaveFileAsync(ThreadDTO.Images[i],picName, "community_images/Threads");
                            if (profileImagePath == null)
                                return BadRequest("Error al guardar la imagen de perfil.");
                        }

                        var threadImage = new ThreadImage
                        {
                            ThreadId = thread.Id,
                            ImageUrl = profileImagePath
                        };
                        _context.ThreadImages.Add(threadImage);
                    }

                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Thread creado");
                return Ok("Thread creado");
            }
            else
            {
                _logger.LogInformation("No existe la comunidad");
                return NotFound("No existe la comunidad");
            }
        }

        [HttpPost("LikeThread/{threadId}")]
        [Authorize]
        public async Task<IActionResult> LikeThread(int threadId)
        {
            var thread = await _context.Threads.FindAsync(threadId);
            if (thread != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                var existingLike = await _context.ThreadLikes
                    .FirstOrDefaultAsync(tl => tl.UserId == userId && tl.ThreadId == thread.Id);

                if (existingLike != null)
                {
                    _logger.LogWarning("El usuario ya ha dado like a este hilo");
                    return BadRequest("Ya has dado like a este hilo");
                }
                else
                {
                    ThreadLikes newLike = new()
                    {
                        ThreadId = thread.Id,
                        UserId = userId
                    };
                    thread.CountLikes += 1;
                    _context.ThreadLikes.Add(newLike);
                    _context.Threads.Update(thread);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Like al hilo realizado");
                    return Ok("Like al hilo realizado correctamente");
                }
            }
            else
            {
                _logger.LogWarning("No se encontró el hilo para dar like");
                return NotFound("No se encuentra el hilo");
            }
        }

        [HttpGet("All")]
        public async Task<IActionResult> GetThreads([FromQuery] int pageNumber = 1, [FromQuery] string name = null)
        {
            int pageSize = 10;

            var threadsQuery = _context.Threads.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                threadsQuery = threadsQuery.Where(t => t.Title.Contains(name));
            }

            var total = await threadsQuery.CountAsync();

            var threadsList = await threadsQuery
                .OrderBy(t => t.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new ThreadListDTO
                {
                    Id = t.Id,
                    Title = t.Title,
                    Images = t.Images.Select(i => i.ImageUrl).ToList(),
                    Likes = t.CountLikes
                })
                .ToListAsync();

            if (!threadsList.Any())
            {
                _logger.LogInformation("No se encontraron hilos.");
                return NotFound("No se encontraron hilos.");
            }

            PageDTO<ThreadListDTO> result = new()
            {
                Data = threadsList,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = total
            };

            _logger.LogInformation("Hilos recuperados exitosamente.");
            return Ok(result);
        }


        [HttpGet("AllByComunity/{idCom}")]
        public async Task<IActionResult> GetThreadsByCommunity(int idCom, [FromQuery] int pageNumber = 1, [FromQuery] string name = null)
        {
            int pageSize = 10;

            var threadsQuery = _context.Threads
                .Where(t => t.CommunityId == idCom);

            // Filtrar por nombre de hilo si se proporciona
            if (!string.IsNullOrEmpty(name))
            {
                threadsQuery = threadsQuery.Where(t => t.Title.Contains(name));
            }

            var total = await threadsQuery.CountAsync();

            var threadsCom = await threadsQuery
                .OrderBy(t => t.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new ThreadListDTO
                {
                    Id = t.Id,
                    Title = t.Title,
                    Images = t.Images.Select(i => i.ImageUrl).ToList(),
                    Likes = t.CountLikes
                })
                .ToListAsync();

            var result = new PageDTO<ThreadListDTO>
            {
                Data = threadsCom,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = total
            };

            _logger.LogInformation("Listado de hilos por comunidad");
            return Ok(result);
        }



        [HttpGet("bycreator/{creatorId}")]
        public async Task<IActionResult> GetThreadsByCreatorId(int creatorId, [FromQuery] int pageNumber = 1)
        {
            int PageSize = 10;

            var threads = _context.Threads
                .Where(t => t.CreatorId == creatorId);

            var total = await threads.CountAsync();

            var threadsCreator = await threads
                .OrderBy(t => t.Id)
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .Select(t => new ThreadListDTO
                {
                    Id = t.Id,
                    Title = t.Title,
                    Images = t.Images.Select(i => i.ImageUrl).ToList(),
                    Likes = t.CountLikes
                })
                .ToListAsync();

            if (!threadsCreator.Any())
            {
                _logger.LogInformation("Usuario sin threads");
                return NotFound("No se encontraron threads creados por este usuario.");
            }

            var result = new PageDTO<ThreadListDTO>
            {
                Data = threadsCreator,
                PageNumber = pageNumber,
                PageSize = PageSize,
                TotalRecords = total
            };

            _logger.LogInformation("Threads recuperados por creador");
            return Ok(result);
        }

        [HttpGet("byLike/{creatorId}")]
        public async Task<IActionResult> GetThreadsByLikeId(int creatorId, [FromQuery] int pageNumber = 1)
        {
            const int PageSize = 10;

            var threads = _context.ThreadLikes
                .Where(cs => cs.UserId == creatorId)
                .Include(cs => cs.thread)
                .ThenInclude(t => t.Images);

            var total = await threads.CountAsync();

            var threadLikes = await threads
                .OrderBy(cs => cs.thread.Id)
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .Select(cs => new ThreadListDTO
                {
                    Id = cs.thread.Id,
                    Title = cs.thread.Title,
                    Images = cs.thread.Images.Select(i => i.ImageUrl).ToList(),
                    Likes = cs.thread.CountLikes
                })
                .ToListAsync();

            if (!threadLikes.Any())
            {
                _logger.LogInformation("Usuario sin likes");
                return NotFound("No se encontraron likes a threads.");
            }

            var result = new PageDTO<ThreadListDTO>
            {
                Data = threadLikes,
                PageNumber = pageNumber,
                PageSize = PageSize,
                TotalRecords = total
            };

            _logger.LogInformation("Threads con likes del usuario");
            return Ok(result);
        }

        [HttpGet("{idThread}")]
        public async Task<IActionResult> GetThreadById(int idThread)
        {
            var threadDto = await _context.Threads
                .Where(t => t.Id == idThread)
                .Select(t => new ThreadDetailDTO
                {
                    Id = t.Id,
                    Title = t.Title,
                    Content = t.Content,
                    Community = new CommunityListDTO
                    {
                        Id = t.Community.Id,
                        ComName = t.Community.ComName,
                        ComPicture = t.Community.ComPicture
                    },
                    CreatorId = t.CreatorId,
                    Images = t.Images.Select(i => i.ImageUrl).ToList(),
                    Likes = t.CountLikes
                })
                .FirstOrDefaultAsync();

            if (threadDto != null)
            {
                _logger.LogInformation("Thread añadido.");
                return Ok(threadDto);
            }
            else
            {
                _logger.LogWarning("Hilo inexistente");
                return NotFound();
            }
        }

        [HttpPut()]
        [Authorize]
        public async Task<IActionResult> UpdateThread([FromForm] ThreadUpdateDTO ThreadDTO)
        {
            var thread = await _context.Threads
                .Include(t => t.Images)
                .FirstOrDefaultAsync(t => t.Id == ThreadDTO.Id);

            long maxSize = 5 * 1024 * 1024;
            if (thread != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                if (thread.CreatorId == userId)
                {
                    thread.Title = ThreadDTO.Title;
                    thread.Content = ThreadDTO.Content;
                    thread.UpdatedAt = DateTime.UtcNow;

                    List<ThreadImage> onServer = thread.Images.ToList();  
                    if (ThreadDTO.Images != null)
                    {
                        if (onServer.Count > 0) 
                        {
                            List<string> imagesToUpdate = ThreadDTO.Images.Select(file => file.FileName).ToList();
                            List<string> serverImages = onServer.Select(img => img.ImageUrl).ToList();
                            List<string> imagesToAggregate = imagesToUpdate.Where(img => !serverImages.Contains(img)).ToList();
                            List<string>imagesToDelete = serverImages.Where(img => !imagesToUpdate.Contains(img)).ToList();

                            ThreadDTO.Images = ThreadDTO.Images
                                .Where(file => imagesToAggregate.Contains(file.FileName))
                                .ToList();

                            onServer = onServer
                                .Where(img => imagesToDelete.Contains(img.ImageUrl)).ToList();
                        }

                        List<string> imagePaths = new List<string>();

                        for (int i = 0; i < ThreadDTO.Images.Count; i++)
                        {
                            if (ThreadDTO.Images[i] != null && ThreadDTO.Images[i].Length > 0)
                            {
                                string profileExtension = Path.GetExtension(ThreadDTO.Images[i].FileName).ToLower();
                                if (!_fileFolderService.IsValidExtension(profileExtension))
                                    return BadRequest("Solo se permiten archivos JPG o PNG para la imagen de perfil.");

                                if (!_fileFolderService.IsValidFileSize(ThreadDTO.Images[i].Length, maxSize))
                                    return BadRequest("La imagen de perfil no debe exceder los 5 MB.");

                                string picName = $"{thread.Title}{i}{ThreadDTO.Images[i].FileName}";
                                string  profileImagePath = await _fileFolderService.SaveFileAsync(ThreadDTO.Images[i], picName, "community_images/Threads");
                                if (profileImagePath == null)
                                    return BadRequest("Error al guardar la imagen de perfil.");

                                imagePaths.Add(profileImagePath);

                            }

                            foreach (var path in imagePaths)
                            {
                                var threadImage = new ThreadImage
                                {
                                    ThreadId = thread.Id,
                                    ImageUrl = path
                                };
                                _context.ThreadImages.Add(threadImage);
                            }
                        }
                        
                    }
                    foreach (ThreadImage path in onServer)
                    {
                        var imagepath = Path.Combine(Directory.GetCurrentDirectory(), path.ImageUrl.TrimStart('/'));
                        if (!_fileFolderService.DeleteFile(imagepath))
                        {
                            _logger.LogWarning("No se pudo eliminar la imagen.");
                        }
                        else
                        {
                            _logger.LogInformation("Imagen de thread eliminada.");
                        }
                    }
                    _context.ThreadImages.RemoveRange(onServer);

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Thread actualizado correctamente");
                    return Ok("Thread actualizado correctamente");
                }
                else
                {
                    _logger.LogWarning("No eres el creador del thread");
                    return Forbid("No tienes permisos para actualizar este thread");
                }
            }
            else
            {
                _logger.LogWarning("No se encontró el thread");
                return NotFound("No se encuentra el thread");
            }
        }


        [HttpDelete("community/{idCom}/Thread/{idThread}")]
        [Authorize]
        public async Task<IActionResult> DeleteThread(int idThread, int idCom)
        {
            bool canDeleteThread = false;
            var thread = await _context.Threads.FindAsync(idThread);
            if (thread != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var userRol = await _context.CommunityRoles
                .Where(ucr => ucr.UserId == userId && ucr.CommunityId == idCom)
                .Include(ucr => ucr.Role) 
                .Select(ucr => ucr.Role)  
                .FirstOrDefaultAsync();

                if (userRol != null)
                {
                    if ( userRol.CanDeleteThreads)
                    {
                        canDeleteThread = userRol.CanDeleteThreads;
                    }
                }
                if (thread.CreatorId == userId || canDeleteThread)
                {
                    var images = await _context.ThreadImages
                        .Where(img => img.ThreadId == thread.Id)
                        .Select(img => img.ImageUrl)
                        .ToListAsync();

                    if (images.Count > 0)
                    {
                        foreach (string path in images)
                        {
                            var imagepath = Path.Combine(Directory.GetCurrentDirectory(), path.TrimStart('/'));
                            if (!_fileFolderService.DeleteFile(imagepath))
                            {
                                _logger.LogWarning("No se pudo eliminar la imagen.");
                            }
                            else
                            {
                                _logger.LogInformation("Imagen de thread eliminada.");
                            }
                        }
                    }

                    _context.Threads.Remove(thread);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Thread eliminado");
                    return Ok("Thread eliminado");
                }
                else
                {
                    _logger.LogWarning("No es su thread");
                    return Forbid("No es su thread");
                }
            }
            else
            {
                _logger.LogWarning("No existe este thread");
                return NotFound("No existe este thread");

            }




        }

        [HttpDelete("UnlikeThread/{threadId}")]
        [Authorize]
        public async Task<IActionResult> UnlikeThread(int threadId)
        {
            var thread = await _context.Threads.FindAsync(threadId);
            if (thread != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                var existingLike = await _context.ThreadLikes
                    .FirstOrDefaultAsync(tl => tl.UserId == userId && tl.ThreadId == thread.Id);

                if (existingLike == null)
                {
                    _logger.LogWarning("El usuario no ha dado like a este hilo");
                    return BadRequest("No has dado like a este hilo");
                }
                else
                {
                    thread.CountLikes -= 1;      
                    _context.ThreadLikes.Remove(existingLike);
                    _context.Threads.Update(thread);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Like al hilo eliminado");
                    return Ok("Like al hilo eliminado correctamente");
                }
            }
            else
            {
                _logger.LogWarning("No se encontró el hilo para eliminar el like");
                return NotFound("No se encuentra el hilo");
            }
        }

    }
}
