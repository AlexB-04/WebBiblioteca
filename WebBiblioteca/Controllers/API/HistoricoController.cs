using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebBiblioteca.Data;
using WebBiblioteca.Models;
using Microsoft.EntityFrameworkCore;

namespace WebBiblioteca.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Leitor")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class HistoricoController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly ILeitorRepository _leitorRepository;
        private readonly IEmprestimoRepository _emprestimoRepository;

        public HistoricoController(UserManager<User> userManager,
            ILeitorRepository leitorRepository,
            IEmprestimoRepository emprestimoRepository)
        {
            _userManager = userManager;
            _leitorRepository = leitorRepository;
            _emprestimoRepository = emprestimoRepository;
        }

        // GET /api/Historico/leituras
        // O ID é opcional apenas para verificar e recusar pedidos de outra pessoa.
        [HttpGet("leituras")]
        public async Task<IActionResult> GetLeituras([FromQuery] int? idLeitor)
        {
            var utilizador = await _userManager.GetUserAsync(User);
            if (utilizador == null)
            {
                return Unauthorized();
            }

            if (!utilizador.Aprovado || utilizador.DeveAlterarPassword || !utilizador.EmailConfirmed)
            {
                return Forbid();
            }

            var leitor = await _leitorRepository.GetByUserIdAsync(utilizador.Id);
            if (leitor == null)
            {
                return NotFound(new { mensagem = "Não existe uma ficha de leitor associada a esta conta." });
            }

            if (idLeitor.HasValue)
            {
                if (idLeitor.Value <= 0)
                {
                    return BadRequest(new { mensagem = "O ID do leitor deve ser positivo." });
                }

                if (idLeitor.Value != leitor.IdLeitor)
                {
                    return Forbid();
                }
            }

            var emprestimos = await _emprestimoRepository.GetAll(true)
                .Where(emprestimo => emprestimo.IdLeitor == leitor.IdLeitor)
                .OrderByDescending(emprestimo => emprestimo.DataEmprestimo)
                .ThenByDescending(emprestimo => emprestimo.IdEmprestimo)
                .ToListAsync();

            var leituras = HistoricoLeituras.Preparar(emprestimos);
            return Ok(leituras);
        }
    }
}