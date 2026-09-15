using MenuDeDocumentos.Base;
using MenuDeDocumentos.Models;
using MenuDeDocumentos.Service.Interface;
using Microsoft.Data.SqlClient;
using System.Data;

namespace MenuDeDocumentos.Service;

public class DocumentoService : IDocumentoService
{
    private readonly string _connectionString;

    public DocumentoService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ConexionSIGOB")
            ?? throw new InvalidOperationException("La cadena de conexión 'ConexionSIGOB' no existe.");
    }

    public async Task<List<Documento>> ObtenerListaDocumentosAsync()
    {
        var lista = new List<Documento>();
        int codigoTramite = 1645;

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand("[dbo].[sp_PasanteObtenerDocumento]", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@CodigoDocumento", codigoTramite);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        int idAutoIncrement = 1;

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

        return lista;
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

    public async Task<byte[]?> ObtenerDocumentoDesdeBDAsync(int codigo, string nombreTabla)
    {
        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand("[dbo].[sp_PasanteObtenerDocumento]", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@CodigoDocumento", codigo);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            string colNombre = reader.GetOrdinal("documento") != -1 ? "documento" : "document";
            if (!reader.IsDBNull(reader.GetOrdinal(colNombre)))
            {
                return (byte[])reader[colNombre];
            }
        }

        return null;
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