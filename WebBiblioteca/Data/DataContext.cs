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

        public DbSet<Emprestimo> Emprestimos { get; set; }

        public DbSet<EmprestimoDetalhe> EmprestimoDetalhes { get; set; }

        public DbSet<EmprestimoAlteracao> EmprestimoAlteracoes { get; set; }

        public DbSet<Penalizacao> Penalizacoes { get; set; }

        public DbSet<Reserva> Reservas { get; set; }

        public DbSet<ReservaAlteracao> ReservaAlteracoes { get; set; }

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

            modelBuilder.Entity<Emprestimo>()
                .HasKey(emprestimo => emprestimo.IdEmprestimo);

            modelBuilder.Entity<Emprestimo>()
                .HasOne(emprestimo => emprestimo.Leitor)
                .WithMany()
                .HasForeignKey(emprestimo => emprestimo.IdLeitor)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmprestimoDetalhe>()
                .HasKey(detalhe => detalhe.IdEmprestimoDetalhe);

            modelBuilder.Entity<EmprestimoDetalhe>()
                .HasOne(detalhe => detalhe.Emprestimo)
                .WithMany(emprestimo => emprestimo.Detalhes)
                .HasForeignKey(detalhe => detalhe.IdEmprestimo)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmprestimoDetalhe>()
                .HasOne(detalhe => detalhe.Livro)
                .WithMany()
                .HasForeignKey(detalhe => detalhe.IdLivro)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmprestimoAlteracao>()
                .HasKey(alteracao => alteracao.IdEmprestimoAlteracao);

            modelBuilder.Entity<EmprestimoAlteracao>()
                .HasOne(alteracao => alteracao.Emprestimo)
                .WithMany(emprestimo => emprestimo.Alteracoes)
                .HasForeignKey(alteracao => alteracao.IdEmprestimo)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Penalizacao>()
                .HasKey(penalizacao => penalizacao.IdPenalizacao);

            modelBuilder.Entity<Penalizacao>()
                .Property(penalizacao => penalizacao.Valor)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Penalizacao>()
                .HasIndex(penalizacao => penalizacao.IdEmprestimoDetalhe)
                .IsUnique();

            modelBuilder.Entity<Penalizacao>()
                .HasOne(penalizacao => penalizacao.Emprestimo)
                .WithMany(emprestimo => emprestimo.Penalizacoes)
                .HasForeignKey(penalizacao => penalizacao.IdEmprestimo)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Penalizacao>()
                .HasOne(penalizacao => penalizacao.Detalhe)
                .WithMany()
                .HasForeignKey(penalizacao => penalizacao.IdEmprestimoDetalhe)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reserva>()
                .HasKey(reserva => reserva.IdReserva);

            modelBuilder.Entity<Reserva>()
                .HasOne(reserva => reserva.Leitor)
                .WithMany()
                .HasForeignKey(reserva => reserva.IdLeitor)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reserva>()
                .HasOne(reserva => reserva.Livro)
                .WithMany()
                .HasForeignKey(reserva => reserva.IdLivro)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReservaAlteracao>()
                .HasKey(alteracao => alteracao.IdReservaAlteracao);

            modelBuilder.Entity<ReservaAlteracao>()
                .HasOne(alteracao => alteracao.Reserva)
                .WithMany(reserva => reserva.Alteracoes)
                .HasForeignKey(alteracao => alteracao.IdReserva)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}