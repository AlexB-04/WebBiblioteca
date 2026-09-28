using System.ComponentModel.DataAnnotations;

namespace WebBiblioteca.Models
{
    public class Leitor
    {
        public int IdLeitor { get; set; }

        [Display(Name = "Nome")]
        [MaxLength(100, ErrorMessage = "O nome pode conter no máximo {1} caracteres.")]
        public string? Nome { get; set; }

        [Display(Name = "Contacto")]
        [MaxLength(20, ErrorMessage = "O contacto pode conter no máximo {1} caracteres.")]
        public string? Contacto { get; set; }

        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Introduza um email válido.")]
        [MaxLength(256, ErrorMessage = "O email pode conter no máximo {1} caracteres.")]
        public string? Email { get; set; }

        [Display(Name = "Tipo de utilizador")]
        [MaxLength(30)]
        public string? TipoUtilizador { get; set; }

        [Display(Name = "Limite de empréstimos")]
        public int LimiteEmprestimos { get; set; }

        public int Atrasos { get; set; }

        public DateTime? BloqueadoAte { get; set; }

        public string? UserId { get; set; }

        public User? User { get; set; }
    }
}