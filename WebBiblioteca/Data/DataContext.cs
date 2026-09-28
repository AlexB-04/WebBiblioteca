using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace WebBiblioteca.Data
{
    public class DataContext : IdentityDbContext<User>
    {
        public DataContext(DbContextOptions<DataContext> options) 
            : base(options)
        {

        }

        public DbSet<Categoria> Categorias { get; set; }

        public DbSet<Livro> Livros { get; set; }

        public DbSet<Leitor> Leitores { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Categoria>()
                .HasKey(categoria => categoria.IdCategoria);

            modelBuilder.Entity<Livro>()
                .HasKey(livro => livro.IdLivro);

            modelBuilder.Entity<Livro>()
                .HasOne(livro => livro.CategoriaAtual)
                .WithMany()
                .HasForeignKey(livro => livro.IdCategoria)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Leitor>()
                .HasKey(leitor => leitor.IdLeitor);

            modelBuilder.Entity<Leitor>()
                .HasIndex(leitor => leitor.Email)
                .IsUnique();

            modelBuilder.Entity<Leitor>()
                .HasIndex(leitor => leitor.UserId)
                .IsUnique();

            modelBuilder.Entity<Leitor>()
                .HasOne(leitor => leitor.User)
                .WithMany()
                .HasForeignKey(leitor => leitor.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}