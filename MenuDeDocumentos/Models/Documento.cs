namespace MenuDeDocumentos.Models
{
    public class Documento
    {
        public string NoDocumento { get; set; } = "Ninguno seleccionado";
        public int Codigo { get; set; }
        public string? PdfUrl { get; set; }
        public string Nombre { get; set; } = string.Empty; // <-- Propiedad para el nombre descriptivo
        public string Categoria { get; set; } = string.Empty;
    }
}
