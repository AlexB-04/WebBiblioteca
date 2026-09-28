using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Data;
using WebBiblioteca.Models;

namespace WebBiblioteca.Controllers.API
{
    [Route("api/[controller]/{id?}")]
    [ApiController]
    public class LeitoresController : Controller
    {
        private readonly ILeitorRepository _leitorRepository;

        public LeitoresController(ILeitorRepository leitorRepository)
        {
            _leitorRepository = leitorRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetLeitores(int? id, string? nome)
        {
            if (id == null)
            {
                var leitoresFiltrados = _leitorRepository.GetAll();

                if (!string.IsNullOrWhiteSpace(nome))
                {
                    nome = nome.Trim();

                    leitoresFiltrados = leitoresFiltrados
                        .Where(leitorDaLista =>
                            leitorDaLista.Nome != null &&
                            leitorDaLista.Nome.Contains(nome));
                }

                var leitores = await leitoresFiltrados
                    .OrderBy(leitorDaLista => leitorDaLista.Nome)
                    .Select(leitorDaLista => new
                    {
                        leitorDaLista.IdLeitor,
                        leitorDaLista.Nome,
                        leitorDaLista.Contacto,
                        leitorDaLista.Email,
                        leitorDaLista.TipoUtilizador,
                        leitorDaLista.LimiteEmprestimos,
                        leitorDaLista.Atrasos,
                        leitorDaLista.BloqueadoAte
                    })
                    .ToListAsync();

                return Ok(leitores);
            }

            var leitor = await _leitorRepository.GetByIdAsync(id.Value);

            if (leitor == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                leitor.IdLeitor,
                leitor.Nome,
                leitor.Contacto,
                leitor.Email,
                leitor.TipoUtilizador,
                leitor.LimiteEmprestimos,
                leitor.Atrasos,
                leitor.BloqueadoAte
            });
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Leitor leitor)
        {
            if (leitor == null)
            {
                return BadRequest("Dados do leitor inválidos.");
            }

            if (string.IsNullOrWhiteSpace(leitor.Nome))
            {
                return BadRequest("O nome do leitor é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(leitor.Contacto))
            {
                return BadRequest("O contacto do leitor é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(leitor.Email))
            {
                return BadRequest("O email do leitor é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(leitor.TipoUtilizador))
            {
                return BadRequest("O tipo de utilizador é obrigatório.");
            }

            if (leitor.LimiteEmprestimos < 0)
            {
                return BadRequest("O limite de empréstimos não pode ser negativo.");
            }

            leitor.Nome = leitor.Nome.Trim();
            leitor.Contacto = leitor.Contacto.Trim();
            leitor.Email = leitor.Email.Trim();
            leitor.TipoUtilizador = leitor.TipoUtilizador.Trim();

            if (leitor.TipoUtilizador != "Aluno" &&
                leitor.TipoUtilizador != "Professor" &&
                leitor.TipoUtilizador != "Público em Geral")
            {
                return BadRequest("O tipo deve ser Aluno, Professor ou Público em Geral.");
            }

            bool emailJaExiste = _leitorRepository.GetAll()
                .Any(outroLeitor => outroLeitor.Email == leitor.Email);

            if (emailJaExiste)
            {
                return BadRequest("Já existe um leitor com esse email.");
            }

            try
            {
                await _leitorRepository.CreateAsync(leitor);
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível guardar o leitor. Verifique os dados e o email.");
            }

            return Ok(new
            {
                leitor.IdLeitor,
                leitor.Nome,
                leitor.Contacto,
                leitor.Email,
                leitor.TipoUtilizador,
                leitor.LimiteEmprestimos,
                leitor.Atrasos,
                leitor.BloqueadoAte
            });
        }

        [HttpPut]
        public async Task<IActionResult> Put(int id, [FromBody] Leitor leitor)
        {
            if (leitor == null)
            {
                return BadRequest("Dados do leitor inválidos.");
            }

            var leitorExistente = await _leitorRepository.GetByIdAsync(id);

            if (leitorExistente == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(leitor.Nome))
            {
                return BadRequest("O nome do leitor é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(leitor.Contacto))
            {
                return BadRequest("O contacto do leitor é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(leitor.Email))
            {
                return BadRequest("O email do leitor é obrigatório.");
            }

            if (string.IsNullOrWhiteSpace(leitor.TipoUtilizador))
            {
                return BadRequest("O tipo de utilizador é obrigatório.");
            }

            if (leitor.LimiteEmprestimos < 0)
            {
                return BadRequest("O limite de empréstimos não pode ser negativo.");
            }

            leitor.Nome = leitor.Nome.Trim();
            leitor.Contacto = leitor.Contacto.Trim();
            leitor.Email = leitor.Email.Trim();
            leitor.TipoUtilizador = leitor.TipoUtilizador.Trim();

            if (leitor.TipoUtilizador != "Aluno" &&
                leitor.TipoUtilizador != "Professor" &&
                leitor.TipoUtilizador != "Público em Geral")
            {
                return BadRequest("O tipo deve ser Aluno, Professor ou Público em Geral.");
            }

            bool emailJaExiste = _leitorRepository.GetAll()
                .Any(outroLeitor => outroLeitor.Email == leitor.Email && outroLeitor.IdLeitor != id);

            if (emailJaExiste)
            {
                return BadRequest("Já existe outro leitor com esse email.");
            }

            leitorExistente.Nome = leitor.Nome;
            leitorExistente.Contacto = leitor.Contacto;
            leitorExistente.Email = leitor.Email;
            leitorExistente.TipoUtilizador = leitor.TipoUtilizador;
            leitorExistente.LimiteEmprestimos = leitor.LimiteEmprestimos;

            try
            {
                await _leitorRepository.UpdateAsync(leitorExistente);
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível alterar o leitor. Verifique os dados e o email.");
            }

            return Ok(new
            {
                leitorExistente.IdLeitor,
                leitorExistente.Nome,
                leitorExistente.Contacto,
                leitorExistente.Email,
                leitorExistente.TipoUtilizador,
                leitorExistente.LimiteEmprestimos,
                leitorExistente.Atrasos,
                leitorExistente.BloqueadoAte
            });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteLeitor(int id)
        {
            var leitor = await _leitorRepository.GetByIdAsync(id);

            if (leitor == null)
            {
                return NotFound();
            }

            try
            {
                await _leitorRepository.DeleteAsync(leitor);
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível eliminar o leitor. Verifique os registos associados.");
            }

            return Ok();
        }
    }
}