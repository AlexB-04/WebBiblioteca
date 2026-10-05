using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebBiblioteca.Models
{
    public class Emprestimo
    {
        public int IdEmprestimo { get; set; }

        [Display(Name = "Leitor")]
        public int IdLeitor { get; set; }

        public Leitor? Leitor { get; set; }

        [Display(Name = "Data de empréstimo")]
        public DateTime DataEmprestimo { get; set; }

        [Display(Name = "Prazo de devolução")]
        public DateTime PrazoDevolucao { get; set; }

        public bool Eliminado { get; set; }

        public DateTime? DataEliminacao { get; set; }

        public ICollection<EmprestimoDetalhe> Detalhes { get; set; } = new List<EmprestimoDetalhe>();

        public ICollection<EmprestimoAlteracao> Alteracoes { get; set; } = new List<EmprestimoAlteracao>();

        public ICollection<Penalizacao> Penalizacoes { get; set; } = new List<Penalizacao>();
    }
}