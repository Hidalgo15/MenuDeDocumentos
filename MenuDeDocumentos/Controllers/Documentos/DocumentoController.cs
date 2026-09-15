using Microsoft.AspNetCore.Mvc;
using MenuDeDocumentos.Models;
using MenuDeDocumentos.Service.Interface;

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
        [HttpGet("~/{codigo?}")]
        [HttpGet("")]
        [HttpGet("{codigo?}")]
        public async Task<IActionResult> Index(string? codigo)
        {
            // 1. Obtener la lista completa de documentos para el menú lateral
            // var listaDocumentos = await _documentoService.ObtenerListaDocumentosAsync();
            // ViewBag.Documentos = listaDocumentos;

            var model = new Documento();

            if (string.IsNullOrEmpty(codigo))
            {
                return View(model);
            }

            if (int.TryParse(codigo, out int codigoInt))
            {
                model.NoDocumento = $"DOC-{codigoInt}";
                model.Codigo = codigoInt;

                // Genera la URL resolviendo el parámetro directo en la plantilla de ruta
                ViewBag.PdfUrl = Url.Action("DescargarDocumento", "Documento", new { codigo = codigoInt });

                // Buscar el nombre del documento actual para mostrarlo en el encabezado del visor
                //var docActual = listaDocumentos.FirstOrDefault(d => d.Codigo == codigoInt);
                // ViewBag.NombreDocumentoActual = docActual?.Nombre ?? $"Documento #{codigoInt}";

                return View();
            }

            return BadRequest("El código proporcionado debe ser un número entero válido.");
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