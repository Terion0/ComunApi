using ComunApi.Models.Intermediares;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System;
using ComunApi.DbsContext;
using ComunApi.Models;
using ComunApi.Models.DTO.DTORoles;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Humanizer;
using ComunApi.Models.DTO.DTOCommunity;

namespace ComunApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly CoDbContext _context;

        public RolesController(CoDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpGet("RolesComunidad/{idCommunity}")]
        public async Task<IActionResult> GetCommunityRoles(int idCommunity) {

            var community = await _context.Communities.FindAsync(idCommunity);
            if (community != null)
            {     
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                if (userId == community.CreatorId)
                {
                    var rols = await _context.Roles
                        .Where(r =>  r.CommunityId == idCommunity)
                        .Select(ro => new RoleListDTO
                        {
                            Id= ro.Id,
                            Name=ro.RoleName,
                        })
                        .ToListAsync();
                            
                    if (rols != null)
                    {
                        return Ok(rols);
                    }
                    else
                        return NotFound(new { error = "El rol no existe para esta comunidad." });
                }
                else
                    return Unauthorized(new { error = "Solo el creador de la comunidad puede ver roles." });
            }
            else
                return NotFound(new { error = "Comunidad no encontrada." });
        }


        [HttpGet("Comunidad/{idCommunity}/Rol/{idRol}")]
        [Authorize]
        public async Task<IActionResult> GetCommunityUserRoles(int idCommunity, int idRol)
        {
            var community = await _context.Communities.FindAsync(idCommunity);
            if (community != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                if (userId == community.CreatorId)
                {
                    var userRols = await _context.CommunityRoles
                        .Where(r => r.CommunityId == idCommunity && r.RoleId==idRol)
                        .Select(ro => ro.UserId)
                        .ToListAsync();

                    if (userRols != null)
                    {
                        return Ok(userRols);
                    }
                    else
                        return NotFound(new { error = "Sin roles" });
                }
                else
                    return Unauthorized(new { error = "Solo el creador de la comunidad puede ver roles." });
            }
            else
                return NotFound(new { error = "Comunidad no encontrada." });

        }


        [HttpPost("assignRole")]
        [Authorize]
        public async Task<IActionResult> AssignRoleToUser([FromBody] AssReRoleDTO dto)
        {
            var community = await _context.Communities.FindAsync(dto.CommunityId);
            if (community != null)
            {
                var userRole = await _context.CommunityRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == dto.UserId && ur.CommunityId == dto.CommunityId);

                if (userRole != null)
                    return BadRequest(new { error = "El usuario ya tiene un rol asignado en esta comunidad." });

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);  
                    if (userId == community.CreatorId) 
                    {
                        var role = await _context.Roles
                            .FirstOrDefaultAsync(r => r.Id == dto.RoleId && r.CommunityId == dto.CommunityId);

                        if (role != null)
                        {
                            var newUserRole = new UserCommunityRole
                            {
                                UserId = dto.UserId,
                                CommunityId = dto.CommunityId,
                                RoleId = dto.RoleId
                            };

                            _context.CommunityRoles.Add(newUserRole);
                            await _context.SaveChangesAsync();
                            return Ok(new { error = "Rol asignado correctamente." });
                        }
                        else
                            return NotFound(new { error = "El rol no existe para esta comunidad." });
                    }
                    else
                        return Unauthorized(new { error = "Solo el creador de la comunidad puede asignar roles." });
            }
            else
                return NotFound(new { error = "Comunidad no encontrada." });
        }

        [HttpDelete("removeRole")]
        [Authorize]
        public async Task<IActionResult> RemoveRoleFromUser([FromBody] AssReRoleDTO dto)
        {
            var community = await _context.Communities.FindAsync(dto.CommunityId);
            if (community != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);           
                    if (userId == community.CreatorId) 
                    {
                        var userRole = await _context.CommunityRoles
                            .FirstOrDefaultAsync(ur => ur.UserId == dto.UserId && ur.CommunityId == dto.CommunityId);

                        if (userRole != null)
                        {
                            _context.CommunityRoles.Remove(userRole);
                            await _context.SaveChangesAsync();
                            return Ok(new { error = "Rol eliminado correctamente." });
                        }
                        else
                            return NotFound(new { error = "Rol no encontrado para este usuario en la comunidad." });
                    }
                    else
                        return Unauthorized(new { error = "Solo el creador de la comunidad puede eliminar roles." });
            }
            else
                return NotFound(new { error = "Comunidad no encontrada." });
        }

        
        [HttpPost("createRole")]
        [Authorize]
        public async Task<IActionResult> CreateRole([FromBody] RoleCreateDTO dto)
        {
            var community = await _context.Communities.FindAsync(dto.CommunityId);
            if (community != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                if (userId == community.CreatorId)
                    {
                        var existingRole = await _context.Roles
                            .FirstOrDefaultAsync(r => r.CommunityId == dto.CommunityId && r.RoleName == dto.RoleName);

                        if (existingRole != null)
                            return BadRequest(new { error = "El rol ya existe en esta comunidad." });

                        var newRole = new Role
                        {
                            CommunityId = dto.CommunityId,
                            RoleName = dto.RoleName,
                            CanDeleteThreads = dto.CanDeleteThreads,
                            CanDeleteResponses = dto.CanDeleteResponses,
                            CanBanUsers = dto.CanBanUsers
                        };

                        _context.Roles.Add(newRole);
                        await _context.SaveChangesAsync();
                        return Ok(new { error = "Rol creado correctamente." });
                 }
                 else
                 return Unauthorized(new { error = "Solo el creador de la comunidad puede crear roles." });    
            }
            else
            return NotFound(new { error = "Comunidad no encontrada." });
        }

        
        [HttpDelete("community/{communId}/Rol{rolId}")]
        [Authorize]
        public async Task<IActionResult> DeleteRole(int communId, int rolId)
        {
            var community = await _context.Communities.FindAsync(communId);
            if (community != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                if (userId == community.CreatorId) 
                {
                    var role = await _context.Roles
                        .FirstOrDefaultAsync(r => r.Id == rolId && r.CommunityId == communId);

                    if (role != null)
                    {
                        _context.Roles.Remove(role);
                        await _context.SaveChangesAsync();
                        return Ok(new { error = "Rol eliminado correctamente." });
                    }
                    else
                        return NotFound(new { error = "Rol no encontrado en esta comunidad." });
                }
                else
                    return Unauthorized(new { error = "Solo el creador de la comunidad puede eliminar roles." });
            }
            else
                return NotFound(new { error = "Comunidad no encontrada." });
        }

        [HttpPut("Community/{comunityId}")]
        [Authorize]
        public async Task<IActionResult> updateRole(int comunityId,[FromBody] RoleUpdateDTO dto)
        {
            var community = await _context.Communities.FindAsync(comunityId);
            if (community != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                if (userId == community.CreatorId)
                {
                    var role = await _context.Roles
                        .FirstOrDefaultAsync(r => r.Id == dto.Id);

                    if (role != null)
                    {
                        role.RoleName = dto.RoleName;
                        role.CanBanUsers = dto.CanBanUsers;
                        role.CanDeleteResponses = dto.CanBanUsers;
                        role.CanDeleteThreads = role.CanDeleteThreads;
                        _context.Roles.Update(role);
                        await _context.SaveChangesAsync();
                        return Ok(new { error = "Rol updateado correctamente." });
                    }
                    else
                        return NotFound(new { error = "Rol no encontrado en esta comunidad." });
                }
                else
                    return Unauthorized(new { error = "Solo el creador de la comunidad puede eliminar roles." });
            }
            else
                return NotFound(new { error = "Comunidad no encontrada." });
        }




    }
}
