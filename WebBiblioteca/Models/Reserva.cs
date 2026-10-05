using System.ComponentModel.DataAnnotations;

namespace WebBiblioteca.Models
{
    public class Reserva
    {
        public int IdReserva { get; set; }

        [Display(Name = "Leitor")]
        public int IdLeitor { get; set; }

        public Leitor? Leitor { get; set; }

        [Display(Name = "Livro")]
        public int IdLivro { get; set; }

        public Livro? Livro { get; set; }

        [Display(Name = "Data da reserva")]
        public DateTime DataReserva { get; set; }

        [Display(Name = "Posição na fila")]
        public int Ordem { get; set; }

        [Display(Name = "Ativa")]
        public bool Ativa { get; set; }

        [Display(Name = "Disponível desde")]
        public DateTime? DataDisponivel { get; set; }
    }
}