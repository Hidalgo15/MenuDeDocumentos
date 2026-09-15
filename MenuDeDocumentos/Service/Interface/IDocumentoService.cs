using MenuDeDocumentos.Models;

namespace MenuDeDocumentos.Service.Interface
{
    public interface IDocumentoService
    {
        Task<List<Documento>> ObtenerListaDocumentosAsync(int codigo);
        Task<byte[]?> ObtenerDocumentoDesdeBDAsync(int codigo, string nombreTabla);
        Task<byte[]> DescomprimirDocumentoAsync(byte[] archivoComprimido, int codigo);
    }
}
