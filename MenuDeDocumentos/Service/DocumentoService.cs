using MenuDeDocumentos.Models;
using MenuDeDocumentos.Service.Interface;
using MenuDeDocumentos.Utils;
using Microsoft.Data.SqlClient;

namespace MenuDeDocumentos.Service;

public class DocumentoService : IDocumentoService
{
    private readonly string _connectionString;

    public DocumentoService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ConexionSIGOB")
            ?? throw new InvalidOperationException("La cadena de conexión 'ConexionSIGOB' no existe.");
    }

    public async Task<List<Documento>> ObtenerListaDocumentosAsync(int codigo)
    {
        var lista = new List<Documento>();
        int idAutoIncrement = 1;

        await EjecucionSpUtils.ExecuteStoredProcedureAsync(_connectionString, codigo, async reader =>
        {
            while (await reader.ReadAsync())
            {
                int ordinalCodigo = HasColumn(reader, "codigo") ? reader.GetOrdinal("codigo") : -1;
                int codigoFinal = ordinalCodigo != -1 ? Convert.ToInt32(reader[ordinalCodigo]) : idAutoIncrement++;

                lista.Add(new Documento
                {
                    Codigo = codigoFinal,
                    Categoria = reader["molde"].ToString() ?? "General",
                    Nombre = reader["nombre"].ToString() ?? "Sin Título"
                });
            }
        });

        return lista;
    }

    public async Task<byte[]?> ObtenerDocumentoDesdeBDAsync(int codigo, string nombreTabla)
    {
        byte[]? resultado = null;

        await EjecucionSpUtils.ExecuteStoredProcedureAsync(_connectionString, codigo, async reader =>
        {
            if (await reader.ReadAsync())
            {
                string colNombre = HasColumn(reader, "documento") ? "documento" : "document";
                if (!reader.IsDBNull(reader.GetOrdinal(colNombre)))
                {
                    resultado = (byte[])reader[colNombre];
                }
            }
        });

        return resultado;
    }

    // Función auxiliar para verificar si existe una columna por su nombre
    private static bool HasColumn(SqlDataReader reader, string columnName)
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    public async Task<byte[]> DescomprimirDocumentoAsync(byte[] archivoComprimido, int codigo)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "VisorDocumentos");
        Directory.CreateDirectory(tempDir);

        string rutaComprimida = Path.Combine(tempDir, $"Documento_{codigo}.zlib");
        string? rutaDescomprimida = null;

        try
        {
            await File.WriteAllBytesAsync(rutaComprimida, archivoComprimido);
            rutaDescomprimida = ZLIBSIGOB.DescomprimirArchivoZLIB(rutaComprimida);

            if (string.IsNullOrEmpty(rutaDescomprimida) || !File.Exists(rutaDescomprimida))
            {
                throw new FileNotFoundException("No se generó el archivo descomprimido.");
            }

            return await File.ReadAllBytesAsync(rutaDescomprimida);
        }
        finally
        {
            if (File.Exists(rutaComprimida)) File.Delete(rutaComprimida);
            if (!string.IsNullOrEmpty(rutaDescomprimida) && File.Exists(rutaDescomprimida)) File.Delete(rutaDescomprimida);
        }
    }
}