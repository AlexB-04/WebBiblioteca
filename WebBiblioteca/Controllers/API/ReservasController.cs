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
                    reserva.DataDisponivel,
                    Alteracoes = reserva.Alteracoes
                        .OrderBy(alteracao => alteracao.DataAlteracao)
                        .ThenBy(alteracao => alteracao.IdReservaAlteracao)
                        .Select(alteracao => new
                        {
                            alteracao.IdReservaAlteracao,
                            alteracao.DataAlteracao,
                            alteracao.Acao,
                            alteracao.IdLivroAnterior,
                            alteracao.LivroAnterior,
                            alteracao.IdLivroNovo,
                            alteracao.LivroNovo,
                            alteracao.OrdemAnterior,
                            alteracao.OrdemNova
                        }).ToList()
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
                reservaEncontrada.DataDisponivel,
                Alteracoes = reservaEncontrada.Alteracoes
                    .OrderBy(alteracao => alteracao.DataAlteracao)
                    .ThenBy(alteracao => alteracao.IdReservaAlteracao)
                    .Select(alteracao => new
                    {
                        alteracao.IdReservaAlteracao,
                        alteracao.DataAlteracao,
                        alteracao.Acao,
                        alteracao.IdLivroAnterior,
                        alteracao.LivroAnterior,
                        alteracao.IdLivroNovo,
                        alteracao.LivroNovo,
                        alteracao.OrdemAnterior,
                        alteracao.OrdemNova
                    }).ToList()
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

            bool temExemplaresLivres = await _reservaRepository
                .TemExemplaresLivresAsync(livro.IdLivro);

            if (temExemplaresLivres)
            {
                return BadRequest("Este livro ainda tem exemplares livres para empréstimo.");
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
                reservaCriada.DataDisponivel,
                Alteracoes = reservaCriada.Alteracoes
                    .OrderBy(alteracao => alteracao.DataAlteracao)
                    .ThenBy(alteracao => alteracao.IdReservaAlteracao)
                    .Select(alteracao => new
                    {
                        alteracao.IdReservaAlteracao,
                        alteracao.DataAlteracao,
                        alteracao.Acao,
                        alteracao.IdLivroAnterior,
                        alteracao.LivroAnterior,
                        alteracao.IdLivroNovo,
                        alteracao.LivroNovo,
                        alteracao.OrdemAnterior,
                        alteracao.OrdemNova
                    }).ToList()
            });
        }

        [HttpPut]
        [Route("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Reserva reserva)
        {
            if (reserva == null)
            {
                return BadRequest("Dados da reserva inválidos.");
            }

            var reservaExistente = await _reservaRepository.GetByIdAsync(id);

            if (reservaExistente == null)
            {
                return NotFound();
            }

            if (!reservaExistente.Ativa)
            {
                return BadRequest("Não é possível alterar uma reserva inativa.");
            }

            if (reservaExistente.DataDisponivel.HasValue)
            {
                return BadRequest("Esta reserva já está disponível para levantamento. Cancele-a para reservar outro livro.");
            }

            if (reserva.IdLivro <= 0)
            {
                return BadRequest("Indique um livro válido.");
            }

            if (reservaExistente.IdLivro == reserva.IdLivro)
            {
                return BadRequest("A reserva já corresponde a este livro.");
            }

            var livro = await _livroRepository.GetByIdAsync(reserva.IdLivro);

            if (livro == null)
            {
                return BadRequest("O livro indicado não existe.");
            }

            bool temExemplaresLivres = await _reservaRepository
                .TemExemplaresLivresAsync(livro.IdLivro);

            if (temExemplaresLivres)
            {
                return BadRequest("Este livro ainda tem exemplares livres para empréstimo.");
            }

            bool reservaJaExiste = await _reservaRepository
                .ReservaAtivaExisteAsync(reservaExistente.IdLeitor, reserva.IdLivro);

            if (reservaJaExiste)
            {
                return BadRequest("Este leitor já tem uma reserva ativa deste livro.");
            }

            try
            {
                // O ID da rota identifica a reserva; só o novo livro vem do Body.
                bool alterada = await _reservaRepository.UpdateAsync(id, reserva.IdLivro);

                if (!alterada)
                {
                    return BadRequest("Não foi possível alterar a reserva. Verifique o estado da reserva e o livro indicado.");
                }
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível guardar a alteração da reserva. Verifique os dados associados.");
            }

            var reservaAtualizada = await _reservaRepository.GetByIdAsync(id);

            if (reservaAtualizada == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                reservaAtualizada.IdReserva,
                reservaAtualizada.IdLeitor,
                Leitor = reservaAtualizada.Leitor == null ? null : reservaAtualizada.Leitor.Nome,
                reservaAtualizada.IdLivro,
                Livro = reservaAtualizada.Livro == null ? null : reservaAtualizada.Livro.Titulo,
                reservaAtualizada.DataReserva,
                reservaAtualizada.Ordem,
                reservaAtualizada.Ativa,
                reservaAtualizada.DataDisponivel,
                Alteracoes = reservaAtualizada.Alteracoes
                    .OrderBy(alteracao => alteracao.DataAlteracao)
                    .ThenBy(alteracao => alteracao.IdReservaAlteracao)
                    .Select(alteracao => new
                    {
                        alteracao.IdReservaAlteracao,
                        alteracao.DataAlteracao,
                        alteracao.Acao,
                        alteracao.IdLivroAnterior,
                        alteracao.LivroAnterior,
                        alteracao.IdLivroNovo,
                        alteracao.LivroNovo,
                        alteracao.OrdemAnterior,
                        alteracao.OrdemNova
                    }).ToList()
            });
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var reserva = await _reservaRepository.GetByIdAsync(id);

            if (reserva == null)
            {
                return NotFound();
            }

            if (!reserva.Ativa)
            {
                return BadRequest("Esta reserva já está inativa.");
            }

            try
            {
                bool cancelada = await _reservaRepository.DeleteAsync(id);

                if (!cancelada)
                {
                    return BadRequest("Não foi possível cancelar a reserva. Verifique o estado e os dados associados.");
                }
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível guardar o cancelamento da reserva. Verifique os dados associados.");
            }

            return Ok();
        }
    }
}