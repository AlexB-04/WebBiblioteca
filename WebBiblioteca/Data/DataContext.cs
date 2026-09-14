using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Models;

namespace WebBiblioteca.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }

        public DbSet<Categoria> Categorias { get; set; }

        public DbSet<Livro> Livros { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Categoria>()
                .HasKey(c => c.IdCategoria);

            modelBuilder.Entity<Livro>()
                .HasKey(l => l.IdLivro);

            modelBuilder.Entity<Livro>()
                .HasOne(l => l.CategoriaAtual)
                .WithMany()
                .HasForeignKey(l => l.IdCategoria)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}