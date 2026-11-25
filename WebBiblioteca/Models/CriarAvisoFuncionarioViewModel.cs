using System.ComponentModel.DataAnnotations;

namespace WebBiblioteca.Models
{
    public class CriarAvisoFuncionarioViewModel
    {
        [Required(ErrorMessage = "A mensagem é obrigatória.")]
        [MaxLength(500, ErrorMessage = "A mensagem pode conter no máximo 500 caracteres.")]
        [Display(Name = "Mensagem")]
        public string Mensagem { get; set; } = string.Empty;
    }
}