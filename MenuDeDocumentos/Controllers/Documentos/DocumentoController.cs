using MenuDeDocumentos.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace MenuDeDocumentos.Controllers.Documentos
{
    [Route("Documento")]
    public class DocumentoController : Controller
    {
        private readonly IDocumentoService _documentoService;
        private const string NombreTabla = "cbs01";

        public DocumentoController(IDocumentoService documentoService)
        {
            _documentoService = documentoService;
        }

        [HttpGet("~/")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(int? codigo)
        {
            var listaDocumentos = await _documentoService.ObtenerListaDocumentosAsync();
            ViewBag.Documentos = listaDocumentos;

            if (listaDocumentos.Any())
            {
                var docSeleccionado = codigo.HasValue
                    ? listaDocumentos.FirstOrDefault(d => d.Codigo == codigo.Value) ?? listaDocumentos.First()
                    : listaDocumentos.First();

                ViewBag.PdfUrl = Url.Action("DescargarDocumento", "Documento", new { codigo = docSeleccionado.Codigo });
                ViewBag.NombreDocumentoActual = docSeleccionado.Nombre;
            }

            return View();
        }

        [HttpGet("DescargarDocumento/{codigo:int}")]
        public async Task<IActionResult> DescargarDocumento(int codigo)
        {
            if (codigo <= 0)
            {
                return BadRequest("Código de documento inválido.");
            }

            byte[]? archivoBytes = await _documentoService.ObtenerDocumentoDesdeBDAsync(codigo, NombreTabla);

            if (archivoBytes == null || archivoBytes.Length == 0)
            {
                return NotFound("No se encontró el archivo comprimido en la BD.");
            }

            try
            {
                byte[] pdfBytes = await _documentoService.DescomprimirDocumentoAsync(archivoBytes, codigo);
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception)
            {
                return NotFound("No se pudo procesar o descomprimir el archivo.");
            }
        }
    }
}