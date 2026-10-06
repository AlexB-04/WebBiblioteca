using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Data;
using WebBiblioteca.Models;

namespace WebBiblioteca.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservasController : Controller
    {
        private readonly IReservaRepository _reservaRepository;
        private readonly ILeitorRepository _leitorRepository;
        private readonly ILivroRepository _livroRepository;

        public ReservasController(
            IReservaRepository reservaRepository,
            ILeitorRepository leitorRepository,
            ILivroRepository livroRepository)
        {
            _reservaRepository = reservaRepository;
            _leitorRepository = leitorRepository;
            _livroRepository = livroRepository;
        }

        [HttpGet]
        [Route("{id?}")]
        public async Task<IActionResult> GetReservas(int? id, int? idLeitor, int? idLivro, bool? ativa)
        {
            if (id == null)
            {
                var reservasFiltradas = _reservaRepository.GetAll();

                if (idLeitor.HasValue)
                {
                    reservasFiltradas = reservasFiltradas
                        .Where(reserva => reserva.IdLeitor == idLeitor.Value);
                }

                if (idLivro.HasValue)
                {
                    reservasFiltradas = reservasFiltradas
                        .Where(reserva => reserva.IdLivro == idLivro.Value);
                }

                if (ativa.HasValue)
                {
                    reservasFiltradas = reservasFiltradas
                        .Where(reserva => reserva.Ativa == ativa.Value);
                }

                var reservas = await reservasFiltradas
                    .OrderBy(reserva => reserva.IdLivro)
                    .ThenBy(reserva => reserva.Ordem)
                    .ThenBy(reserva => reserva.IdReserva)
                    .ToListAsync();

                return Ok(reservas.Select(reserva => new
                {
                    reserva.IdReserva,
                    reserva.IdLeitor,
                    Leitor = reserva.Leitor == null ? null : reserva.Leitor.Nome,
                    reserva.IdLivro,
                    Livro = reserva.Livro == null ? null : reserva.Livro.Titulo,
                    reserva.DataReserva,
                    reserva.Ordem,
                    reserva.Ativa,
                    reserva.DataDisponivel
                }).ToList());
            }

            var reservaEncontrada = await _reservaRepository.GetByIdAsync(id.Value);

            if (reservaEncontrada == null ||
                (idLeitor.HasValue && reservaEncontrada.IdLeitor != idLeitor.Value) ||
                (idLivro.HasValue && reservaEncontrada.IdLivro != idLivro.Value) ||
                (ativa.HasValue && reservaEncontrada.Ativa != ativa.Value))
            {
                return NotFound();
            }

            return Ok(new
            {
                reservaEncontrada.IdReserva,
                reservaEncontrada.IdLeitor,
                Leitor = reservaEncontrada.Leitor == null ? null : reservaEncontrada.Leitor.Nome,
                reservaEncontrada.IdLivro,
                Livro = reservaEncontrada.Livro == null ? null : reservaEncontrada.Livro.Titulo,
                reservaEncontrada.DataReserva,
                reservaEncontrada.Ordem,
                reservaEncontrada.Ativa,
                reservaEncontrada.DataDisponivel
            });
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Reserva reserva)
        {
            if (reserva == null)
            {
                return BadRequest("Dados da reserva inválidos.");
            }

            if (reserva.IdLeitor <= 0)
            {
                return BadRequest("Indique um leitor válido.");
            }

            if (reserva.IdLivro <= 0)
            {
                return BadRequest("Indique um livro válido.");
            }

            var leitor = await _leitorRepository.GetByIdAsync(reserva.IdLeitor);

            if (leitor == null)
            {
                return BadRequest("O leitor indicado não existe.");
            }

            var livro = await _livroRepository.GetByIdAsync(reserva.IdLivro);

            if (livro == null)
            {
                return BadRequest("O livro indicado não existe.");
            }

            if (livro.ExemplaresDisponiveis > 0)
            {
                return BadRequest("Este livro ainda tem exemplares disponíveis. Deve ser feito um empréstimo.");
            }

            bool reservaJaExiste = await _reservaRepository
                .ReservaAtivaExisteAsync(reserva.IdLeitor, reserva.IdLivro);

            if (reservaJaExiste)
            {
                return BadRequest("Este leitor já tem uma reserva ativa deste livro.");
            }

            bool atingiuLimite = await _reservaRepository
                .AtingiuLimiteReservasAsync(reserva.IdLeitor);

            if (atingiuLimite)
            {
                return BadRequest("O leitor atingiu o limite de reservas ativas.");
            }

            try
            {
                bool criada = await _reservaRepository.CreateAsync(reserva);

                if (!criada)
                {
                    return BadRequest("Não foi possível criar a reserva. Verifique os dados e a disponibilidade do livro.");
                }
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível guardar a reserva. Verifique o leitor e o livro indicados.");
            }

            var reservaCriada = await _reservaRepository.GetByIdAsync(reserva.IdReserva);

            if (reservaCriada == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                reservaCriada.IdReserva,
                reservaCriada.IdLeitor,
                Leitor = reservaCriada.Leitor == null ? null : reservaCriada.Leitor.Nome,
                reservaCriada.IdLivro,
                Livro = reservaCriada.Livro == null ? null : reservaCriada.Livro.Titulo,
                reservaCriada.DataReserva,
                reservaCriada.Ordem,
                reservaCriada.Ativa,
                reservaCriada.DataDisponivel
            });
        }
    }
}