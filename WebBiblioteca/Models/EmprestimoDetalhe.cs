using System;
using System.ComponentModel.DataAnnotations;

namespace WebBiblioteca.Models
{
    public class EmprestimoDetalhe
    {
        public int IdEmprestimoDetalhe { get; set; }

        public int IdEmprestimo { get; set; }

        public Emprestimo? Emprestimo { get; set; }

        [Display(Name = "Livro")]
        public int IdLivro { get; set; }

        public Livro? Livro { get; set; }

        [Display(Name = "Data de devolução")]
        public DateTime? DataDevolucao { get; set; }
    }
}