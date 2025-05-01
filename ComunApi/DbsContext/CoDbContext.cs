using ComunApi.Models;
using ComunApi.Models.Intermediares;
using Microsoft.EntityFrameworkCore;
using System;

namespace ComunApi.DbsContext
{
    public class CoDbContext : DbContext
    {
        public CoDbContext(DbContextOptions<CoDbContext> options) : base(options) { }

        public DbSet<Community> Communities { get; set; }
        public DbSet<ThreadCom> Threads { get; set; }
        public DbSet<Response> Responses { get; set; }
        public DbSet<ThreadImage> ThreadImages { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserCommunityRole> CommunityRoles { get; set; }
        public DbSet<CommunitySubscriptions> CommunitySubscriptions { get; set; }
        public DbSet<ThreadLikes> ThreadLikes { get; set; }
        public DbSet<ResponseLikes> ResponseLikes { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
    
            modelBuilder.Entity<Community>()
                .HasKey(c => c.Id); // Clave primaria

            modelBuilder.Entity<Community>()
                .HasMany(c => c.Threads) // Una comunidad tiene muchos hilos
                .WithOne(t => t.Community) // Un hilo pertenece a una comunidad
                .HasForeignKey(t => t.CommunityId) // Clave foránea en ThreadCom
                .OnDelete(DeleteBehavior.Cascade); // Eliminar hilos cuando se elimina una comunidad

            modelBuilder.Entity<Community>()
                .HasMany(c => c.UserRoles) // Una comunidad tiene muchos roles de usuario
                .WithOne(ur => ur.Community) // Un rol de usuario pertenece a una comunidad
                .HasForeignKey(ur => ur.CommunityId) // Clave foránea en UserCommunityRole
                .OnDelete(DeleteBehavior.Cascade); // Eliminar roles de usuario cuando se elimina una comunidad

            modelBuilder.Entity<Community>()
                .HasMany(c => c.Subscripciones) // Una comunidad tiene muchas subscripciones
                .WithOne(cs => cs.Community)    // Una subscripción pertenece a una comunidad
                .HasForeignKey(cs => cs.CommunityId) // Clave foránea en CommunitySubscriptions
                .OnDelete(DeleteBehavior.Cascade);   // Eliminar subscripciones cuando se elimina una comunidad

            modelBuilder.Entity<CommunitySubscriptions>()
                .HasKey(cs => new { cs.CommunityId, cs.UserId });



         
            modelBuilder.Entity<ThreadCom>()
                .HasKey(t => t.Id); // Clave primaria

            modelBuilder.Entity<ThreadCom>()
                .HasMany(t => t.Responses) // Un hilo tiene muchas respuestas
                .WithOne(r => r.Thread) // Una respuesta pertenece a un hilo
                .HasForeignKey(r => r.ThreadId) // Clave foránea en Response
                .OnDelete(DeleteBehavior.Cascade); // Eliminar respuestas cuando se elimina un hilo

            modelBuilder.Entity<ThreadCom>()
                .HasMany(t => t.Images) // Un hilo tiene muchas imágenes
                .WithOne(i => i.Thread) // Una imagen pertenece a un hilo
                .HasForeignKey(i => i.ThreadId) // Clave foránea en ThreadImage
                .OnDelete(DeleteBehavior.Cascade); // Eliminar imágenes cuando se elimina un hilo

            modelBuilder.Entity<ThreadLikes>()
                .HasKey(tl => new { tl.ThreadId, tl.UserId });

            modelBuilder.Entity<ThreadCom>()
                .HasMany(tc => tc.Likes)
                .WithOne(tl => tl.thread)
                .HasForeignKey(tl => tl.ThreadId)
                .OnDelete(DeleteBehavior.Cascade); // Si se borra el thread, se borran sus likes




            // Configuración de la tabla Response
            modelBuilder.Entity<Response>()
                .HasKey(r => r.Id); // Clave primaria

            modelBuilder.Entity<Response>()
                .HasMany(r => r.Responses) // Una respuesta puede tener respuestas secundarias (hilos de conversación)
                .WithOne(r => r.ParentResponse) // Una respuesta secundaria tiene una respuesta padre
                .HasForeignKey(r => r.ParentId) // Clave foránea en Response (referencia a ParentResponse)
                .OnDelete(DeleteBehavior.Cascade); // Eliminar respuestas secundarias cuando se elimina la respuesta padre

            modelBuilder.Entity<ResponseLikes>()
                .HasKey(rl => new { rl.ResponseId, rl.UserId });

            modelBuilder.Entity<Response>()
                .HasMany(r => r.Likes)
                .WithOne(rl => rl.Response)
                .HasForeignKey(rl => rl.ResponseId)
                .OnDelete(DeleteBehavior.Cascade); // Si se borra la respuesta, se borran sus likes

            // Configuración de la tabla UserCommunityRole
            modelBuilder.Entity<UserCommunityRole>()
                .HasKey(ur => new { ur.UserId, ur.CommunityId, ur.RoleId }); // Clave compuesta

            modelBuilder.Entity<UserCommunityRole>()
                .HasOne(ur => ur.Role) // Un UserCommunityRole tiene un rol
                .WithMany() // Los roles no necesitan una relación inversa con UserCommunityRoles
                .HasForeignKey(ur => ur.RoleId) // Clave foránea en UserCommunityRole
                .OnDelete(DeleteBehavior.Cascade); // Eliminar roles de usuario cuando se elimina un rol

            modelBuilder.Entity<UserCommunityRole>()
                .HasOne(ur => ur.Community) // Un UserCommunityRole tiene una comunidad
                .WithMany() // Las comunidades no necesitan una relación inversa con UserCommunityRoles
                .HasForeignKey(ur => ur.CommunityId) // Clave foránea en UserCommunityRole
                .OnDelete(DeleteBehavior.Cascade); // Eliminar roles de usuario cuando se elimina una comunidad

            modelBuilder.Entity<Role>()
                .HasKey(r => r.Id); // Clave primaria

            modelBuilder.Entity<Role>()
                .HasMany(r => r.UserCommunityRoles) // Un rol tiene muchos UserCommunityRoles
                .WithOne(ur => ur.Role) // Un UserCommunityRole pertenece a un rol
                .HasForeignKey(ur => ur.RoleId) // Clave foránea en UserCommunityRole
                .OnDelete(DeleteBehavior.Cascade); // Eliminar roles de usuario cuando se elimina un rol

            modelBuilder.Entity<Role>()
                  .HasOne(r => r.Community) // Un rol pertenece a una comunidad
                  .WithMany() // Una comunidad tiene muchos roles
                  .HasForeignKey(r => r.CommunityId) // Clave foránea en Role
                  .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }



    }
}


