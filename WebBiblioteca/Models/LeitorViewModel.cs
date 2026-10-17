using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace WebBiblioteca.Models
{
    public class LeitorViewModel
    {
        public int IdLeitor { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(100, ErrorMessage = "O nome pode conter no máximo {1} caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "O apelido pode conter no máximo {1} caracteres.")]
        public string? Apelido { get; set; }

        [Required(ErrorMessage = "O contacto é obrigatório.")]
        [MaxLength(20, ErrorMessage = "O contacto pode conter no máximo {1} caracteres.")]
        public string Contacto { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Introduza um email válido.")]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecione o tipo de leitor.")]
        [Display(Name = "Tipo de utilizador")]
        [RegularExpression("^(Aluno|Professor|Público em Geral)$", ErrorMessage = "Selecione Aluno, Professor ou Público em Geral.")]
        public string TipoUtilizador { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "O limite de empréstimos não pode ser negativo.")]
        [Display(Name = "Limite de empréstimos")]
        public int LimiteEmprestimos { get; set; } = 3;

        [BindNever]
        public bool TemConta { get; set; }

        [BindNever]
        public string? CodigoLeitor { get; set; }
    }
}