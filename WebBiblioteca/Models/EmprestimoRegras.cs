namespace WebBiblioteca.Models
{
    public static class EmprestimoRegras
    {
        // Valores já utilizados neste projeto; o PDF não fixa a taxa nem o bloqueio.
        public const decimal ValorPorDia = 0.50m;
        public const int LimiteAtrasos = 3;
        public const int DiasBloqueio = 7;

        public static int ObterDias(string? tipoUtilizador)
        {
            if (tipoUtilizador == "Professor")
            {
                return 30;
            }
            else if (tipoUtilizador == "Aluno")
            {
                return 15;
            }
            else if (tipoUtilizador == "Público em Geral")
            {
                return 7;
            }
            else
            {
                return 0;
            }
        }
    }
}