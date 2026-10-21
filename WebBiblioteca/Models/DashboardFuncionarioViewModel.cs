namespace WebBiblioteca.Models
{
    public class DashboardFuncionarioViewModel
    {
        public int EmprestimosAtivos { get; set; }

        public int EmprestimosAtrasados { get; set; }

        public int PenalizacoesPorPagar { get; set; }

        public int ReservasAtivas { get; set; }

        public int ReservasDisponiveis { get; set; }

        public int ReservasComPrazoExpirado { get; set; }

        public int LeitoresPendentesAprovacao { get; set; }

        public string? ErroDados { get; set; }

        public List<DashboardEventoViewModel> EventosRecentes { get; set; } = new List<DashboardEventoViewModel>();
    }

    public class DashboardEventoViewModel
    {
        public int IdReserva { get; set; }

        public DateTime Data { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;
    }
}