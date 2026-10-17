using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBiblioteca.Data;
using WebBiblioteca.Helpers;
using WebBiblioteca.Models;

namespace WebBiblioteca.Controllers.API
{
    [Route("api/[controller]/{id?}")]
    [ApiController]
    [Authorize(Roles = "Funcionario")]
    public class LeitoresController : Controller
    {
        private readonly ILeitorRepository _leitorRepository;
        private readonly IUserHelper _userHelper;
        private readonly IImageHelper _imageHelper;

        public LeitoresController(ILeitorRepository leitorRepository, IUserHelper userHelper, IImageHelper imageHelper)
        {
            _leitorRepository = leitorRepository;
            _userHelper = userHelper;
            _imageHelper = imageHelper;
        }

        [HttpGet]
        public async Task<IActionResult> GetLeitores(int? id, string? nome, string? codigo)
        {
            if (id == null)
            {
                var leitores = _leitorRepository.GetAll();
                if (!string.IsNullOrWhiteSpace(nome))
                {
                    string texto = nome.Trim().ToUpper();
                    leitores = leitores.Where(leitor => leitor.Nome != null && leitor.Nome.ToUpper().Contains(texto));
                }
                if (!string.IsNullOrWhiteSpace(codigo))
                {
                    string texto = codigo.Trim().ToUpper();
                    leitores = leitores.Where(leitor => leitor.CodigoLeitor != null && leitor.CodigoLeitor.ToUpper().Contains(texto));
                }
                return Ok(await leitores.OrderBy(leitor => leitor.Nome).Select(leitor => new
                {
                    leitor.IdLeitor,
                    leitor.Nome,
                    leitor.Contacto,
                    leitor.Email,
                    leitor.TipoUtilizador,
                    leitor.LimiteEmprestimos,
                    leitor.CodigoLeitor,
                    leitor.Atrasos,
                    leitor.BloqueadoAte
                }).ToListAsync());
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
                leitor.CodigoLeitor,
                leitor.Atrasos,
                leitor.BloqueadoAte
            });
        }

        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Put(int id, [FromBody] LeitorViewModel model)
        {
            var leitor = await _leitorRepository.GetByIdAsync(id);
            if (leitor == null)
            {
                return NotFound();
            }

            // Aceitar pedidos antigos que mantêm o nome completo da conta.
            // Para alterar o nome de uma conta, enviar Nome e Apelido separados, como no formulário.
            if (leitor.UserId != null && model.Apelido == null && model.Nome.Trim() == leitor.Nome)
            {
                var user = await _userHelper.GetUserByIdAsync(leitor.UserId);
                if (user != null)
                {
                    model.Nome = user.FirstName ?? string.Empty;
                    model.Apelido = user.LastName;
                }
            }

            try
            {
                var result = await _userHelper.UpdateLeitorAsync(leitor, model);
                if (!result.Succeeded)
                {
                    return BadRequest(string.Join(" ", result.Errors.Select(error => error.Description)));
                }
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível alterar o leitor. Verifique os dados e tente novamente.");
            }
            return await GetLeitores(id, null, null);
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLeitor(int id)
        {
            var leitor = await _leitorRepository.GetByIdAsync(id);
            if (leitor == null)
            {
                return NotFound();
            }
            var user = leitor.UserId == null ? null : await _userHelper.GetUserByIdAsync(leitor.UserId);
            try
            {
                var result = await _userHelper.DeleteLeitorAsync(leitor);
                if (!result.Succeeded)
                {
                    return BadRequest(string.Join(" ", result.Errors.Select(error => error.Description)));
                }
                _imageHelper.TryDeleteImage(user?.ImageUrl, "users");
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível eliminar o leitor. Verifique os registos associados.");
            }
            return Ok();
        }
    }
}