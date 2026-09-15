using MenuDeDocumentos.Models;
using MenuDeDocumentos.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace MenuDeDocumentos.Controllers.Documentos
{
    [Route("")]
    [Route("Documento")]
    public class DocumentoController : Controller
    {
        private readonly IDocumentoService _documentoService;
        private const string NombreTabla = "cbs01";

        public DocumentoController(IDocumentoService documentoService)
        {
            _documentoService = documentoService;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        [HttpGet("{codigo:int}")]
        public async Task<IActionResult> Index(int? codigo)
        {
            // Solo cargar documentos si se proporciona un código válido
            if (codigo.HasValue && codigo.Value > 0)
            {
                var listaDocumentos = await _documentoService.ObtenerListaDocumentosAsync(codigo.Value);
                ViewBag.Documentos = listaDocumentos;

                if (listaDocumentos.Any())
                {
                    var docSeleccionado = listaDocumentos.FirstOrDefault(d => d.Codigo == codigo.Value) ?? listaDocumentos.First();

                    ViewBag.PdfUrl = Url.Action("DescargarDocumento", "Documento", new { codigo = docSeleccionado.Codigo });
                    ViewBag.NombreDocumentoActual = docSeleccionado.Nombre;
                }
            }
            else
            {
                // Sin código: menú vacío
                ViewBag.Documentos = new List<Documento>();
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

        [HttpGet("DescargarArchivoLocal/{codigo:int}")]
        public async Task<IActionResult> DescargarArchivoLocal(int codigo)
        {
            if (codigo <= 0)
            {
                return BadRequest("Código de documento inválido.");
            }

            // 1. Obtenemos el archivo comprimido de la BD
            byte[]? archivoBytes = await _documentoService.ObtenerDocumentoDesdeBDAsync(codigo, NombreTabla);

            if (archivoBytes == null || archivoBytes.Length == 0)
            {
                return NotFound("No se encontró el archivo comprimido en la BD.");
            }

            try
            {
                // 2. Descomprimimos el archivo
                byte[] pdfBytes = await _documentoService.DescomprimirDocumentoAsync(archivoBytes, codigo);

                // Opcional: Puedes buscar el nombre real del documento para usarlo en la descarga
                string nombreArchivo = $"Documento_{codigo}.pdf";

                // 3. Retornamos el archivo indicando que debe descargarse como adjunto
                return File(pdfBytes, "application/pdf", nombreArchivo);
            }
            catch (Exception)
            {
                return NotFound("No se pudo procesar o descomprimir el archivo para su descarga.");
            }
        }
    }
}