using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace WebBiblioteca.Models
{
    public class EmprestimoViewModel
    {
        [Display(Name = "Leitor")]
        [Range(1, int.MaxValue, ErrorMessage = "Selecione um leitor.")]
        public int IdLeitor { get; set; }

        [Display(Name = "Livros")]
        [MinLength(1, ErrorMessage = "Selecione pelo menos um livro.")]
        public List<int> IdsLivros { get; set; } = new List<int>();

        [ValidateNever]
        public List<Leitor> Leitores { get; set; } = new List<Leitor>();

        [ValidateNever]
        public List<Livro> Livros { get; set; } = new List<Livro>();
    }

    public class PrazoEmprestimoViewModel
    {
        public int IdEmprestimo { get; set; }

        [Display(Name = "Novo prazo de devolução")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Indique o prazo de devolução.")]
        public DateTime? PrazoDevolucao { get; set; }

        [ValidateNever]
        public Emprestimo? Emprestimo { get; set; }
    }

    public class OperacaoEmprestimoViewModel
    {
        public Emprestimo Emprestimo { get; set; } = new Emprestimo();
        public int IdLivro { get; set; }
        public int IdPenalizacao { get; set; }
        public string Acao { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
        public string? Erro { get; set; }
    }
}