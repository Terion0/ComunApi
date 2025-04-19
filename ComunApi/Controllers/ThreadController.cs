using ComunApi.DbsContext;
using ComunApi.Models.DTO.DTOCommunity;
using ComunApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ComunApi.Models.DTO.DTOThread;
using Microsoft.EntityFrameworkCore;

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

        [HttpGet("All")]
        public async Task<IActionResult> GetThreads()
        {
            var threads = await _context.Threads
                .Select(thread => new ThreadListDTO
                {
                    Id = thread.Id,
                    Title = thread.Title,
                    Images = thread.Images.Select(i => i.ImageUrl).ToList(),
                })
                .ToListAsync();

            _logger.LogInformation("Listado de hilos");
            return Ok(threads);
        }

        [HttpGet("AllByComunity/{id}")]
        public async Task<IActionResult> GetThreadsByCommunity(int id)
        {
            var threads = await _context.Threads
                .Where(thread => thread.CommunityId == id)
                .Select(thread => new ThreadListDTO
                {
                    Id = thread.Id,
                    Title = thread.Title,
                    Images = thread.Images.Select(i => i.ImageUrl).ToList(),
                })
                .ToListAsync();
            _logger.LogInformation("Listado de hilos por comunidad");
            return Ok(threads);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetThreadById(int id)
        {
            var thread = await _context.Threads.FindAsync(id);
            if (thread != null)
            {
                ThreadDetailDTO com = new ThreadDetailDTO
                {
                    Id = thread.Id,
                    Title = thread.Title,
                    Content = thread.Content,
                    CreatorId = thread.CreatorId,
                    Images = thread.Images.Select(i => i.ImageUrl).ToList(),           
                };
                _logger.LogInformation("Thread añadido.");
                return Ok(com);
            }
            else {
                _logger.LogWarning("Comunidad inexistente");
                return NotFound();
            }
           
        }


        [HttpGet("bycreator/{creatorId}")]
        [Authorize]
        public async Task<IActionResult> GetThreadsByCreatorId(int creatorId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (creatorId == userId)
            {
                var threads = await _context.Threads
                    .Where(t => t.CreatorId == creatorId)
                    .Select(t => new ThreadListDTO
                    {
                        Id = t.Id,
                        Title = t.Title,     
                        Images = t.Images.Select(i => i.ImageUrl).ToList(),
                      
                    })
                    .ToListAsync();

                if (threads.Any())
                {
                    _logger.LogInformation("Threads recuperados");
                    return Ok(threads);
                }
                else
                {
                    _logger.LogInformation("Uusario sin threads");
                    return NotFound("No se encontraron threads creados por este usuario.");
                }
            }
            else
            {
                _logger.LogInformation("No puede acceder a threads ajenos");
                return Forbid("Solo puedes acceder a tus propios threads.");
            }
        }


        [HttpPut("{id}")]
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

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteThread(int id)
        {
            var thread = await _context.Threads.FindAsync(id);
            if (thread != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                if (thread.CreatorId == userId)
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
    }
}
