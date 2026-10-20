namespace WebBiblioteca.Models
{
    public class HistoricoViewModel
    {
        public string NomeLeitor { get; set; } = string.Empty;
        public string? CodigoLeitor { get; set; }
        public List<HistoricoLeituraDto> Leituras { get; set; } = new List<HistoricoLeituraDto>();
        public List<HistoricoPrazoViewModel> AlteracoesPrazo { get; set; } = new List<HistoricoPrazoViewModel>();
        public List<HistoricoReservaViewModel> Reservas { get; set; } = new List<HistoricoReservaViewModel>();
        public List<HistoricoAlteracaoReservaViewModel> AlteracoesReservas { get; set; } = new List<HistoricoAlteracaoReservaViewModel>();
    }

    public class HistoricoPrazoViewModel
    {
        public int IdEmprestimo { get; set; }
        public DateTime DataAlteracao { get; set; }
        public DateTime PrazoAnterior { get; set; }
        public DateTime PrazoNovo { get; set; }
    }

    public class HistoricoReservaViewModel
    {
        public int IdReserva { get; set; }
        public string TituloLivro { get; set; } = string.Empty;
        public DateTime DataReserva { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int Ordem { get; set; }
        public DateTime? DataDisponivel { get; set; }
    }

    public class HistoricoAlteracaoReservaViewModel
    {
        public int IdReserva { get; set; }
        public DateTime DataAlteracao { get; set; }
        public string Acao { get; set; } = string.Empty;
        public string? LivroAnterior { get; set; }
        public string? LivroNovo { get; set; }
        public int OrdemAnterior { get; set; }
        public int OrdemNova { get; set; }
    }
}