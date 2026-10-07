namespace WebBiblioteca.Models
{
    public class ReservaAlteracao
    {
        public int IdReservaAlteracao { get; set; }
        public int IdReserva { get; set; }
        public Reserva? Reserva { get; set; }
        public DateTime DataAlteracao { get; set; }
        public string? Acao { get; set; }
        public int IdLivroAnterior { get; set; }
        public string? LivroAnterior { get; set; }
        public int IdLivroNovo { get; set; }
        public string? LivroNovo { get; set; }
        public int OrdemAnterior { get; set; }
        public int OrdemNova { get; set; }
    }
}