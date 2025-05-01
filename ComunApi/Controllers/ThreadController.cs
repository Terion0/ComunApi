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

namespace ComunApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ThreadController : ControllerBase
    {
        private readonly CoDbContext _context;
        private readonly ILogger<ThreadController> _logger;

        public ThreadController(CoDbContext context, ILogger<ThreadController> logger)
        {
            _context = context;
            _logger = logger;
        }

       
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateThread([FromBody] ThreadCreateDTO ThreadDTO)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var com = await _context.Communities.FindAsync(ThreadDTO.CommunityId);
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

                if (ThreadDTO.Images != null)
                {
                    foreach (string imageUrl in ThreadDTO.Images)
                    {
                        var threadImage = new ThreadImage
                        {
                            ThreadId = thread.Id,
                            ImageUrl = imageUrl
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

        [HttpGet("{idCom}")]
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
        public async Task<IActionResult> UpdateThread([FromBody] ThreadUpdateDTO ThreadDTO)
        {
            var thread = await _context.Threads.FindAsync(ThreadDTO.Id);
            if (thread != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                if (thread.CreatorId == userId)
                {
                    thread.Title = ThreadDTO.Title;
                    thread.Content = ThreadDTO.Content;
                    thread.UpdatedAt = DateTime.UtcNow;

                    if (ThreadDTO.Images != null)
                    {
                      
                        var existingImages = _context.ThreadImages.Where(imagen => imagen.ThreadId == thread.Id).ToList();
                        _context.ThreadImages.RemoveRange(existingImages);       
                        foreach (var imageUrl in ThreadDTO.Images)
                        {
                            var threadImage = new ThreadImage
                            {
                                ThreadId = thread.Id,
                                ImageUrl = imageUrl
                            };
                            _context.ThreadImages.Add(threadImage);
                        }
                    }

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

        [HttpDelete("community/{idCom}/Thread/{idCom}")]
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
