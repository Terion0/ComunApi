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

        // Asignar un rol a un usuario
        [HttpPost("assignRole")]
        public async Task<IActionResult> AssignRoleToUser([FromBody] AssReRoleDTO dto)
        {
            var community = await _context.Communities.FindAsync(dto.CommunityId);
            if (community != null)
            {
                var userRole = await _context.CommunityRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == dto.UserId && ur.CommunityId == dto.CommunityId);

                if (userRole != null)
                    return BadRequest("El usuario ya tiene un rol asignado en esta comunidad.");

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
                            return Ok("Rol asignado correctamente.");
                        }
                        else
                            return NotFound("El rol no existe para esta comunidad.");
                    }
                    else
                        return Unauthorized("Solo el creador de la comunidad puede asignar roles.");
            }
            else
                return NotFound("Comunidad no encontrada.");
        }

        // Eliminar un rol de un usuario
        [HttpPost("removeRole")]
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
                            return Ok("Rol eliminado correctamente.");
                        }
                        else
                            return NotFound("Rol no encontrado para este usuario en la comunidad.");
                    }
                    else
                        return Unauthorized("Solo el creador de la comunidad puede eliminar roles.");
            }
            else
                return NotFound("Comunidad no encontrada.");
        }

        
        [HttpPost("createRole")]
        public async Task<IActionResult> CreateRole([FromBody] RoleCreateDTO dto)
        {
            var community = await _context.Communities.FindAsync(dto.CommunityId);
            if (community != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                if (userId == community.CreatorId) // Verifica que el que crea el rol es el creador
                    {
                        var existingRole = await _context.Roles
                            .FirstOrDefaultAsync(r => r.CommunityId == dto.CommunityId && r.RoleName == dto.RoleName);

                        if (existingRole != null)
                            return BadRequest("El rol ya existe en esta comunidad.");

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
                        return Ok("Rol creado correctamente.");
                 }
                 else
                 return Unauthorized("Solo el creador de la comunidad puede crear roles.");    
            }
            else
            return NotFound("Comunidad no encontrada.");
        }

        // Eliminar un rol
        // Eliminar un rol
        [HttpPost("deleteRole")]
        public async Task<IActionResult> DeleteRole([FromBody] DeleteRolDTO dto)
        {
            var community = await _context.Communities.FindAsync(dto.CommunityId);
            if (community != null)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                if (userId == community.CreatorId) 
                {
                    var role = await _context.Roles
                        .FirstOrDefaultAsync(r => r.Id == dto.RoleId && r.CommunityId == dto.CommunityId);

                    if (role != null)
                    {
                        _context.Roles.Remove(role);
                        await _context.SaveChangesAsync();
                        return Ok("Rol eliminado correctamente.");
                    }
                    else
                        return NotFound("Rol no encontrado en esta comunidad.");
                }
                else
                    return Unauthorized("Solo el creador de la comunidad puede eliminar roles.");
            }
            else
                return NotFound("Comunidad no encontrada.");
        }
          
      
    }
}
