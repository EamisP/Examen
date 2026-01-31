using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace Examen.Services
{
    public class SheetsService
    {
        // ========================= CONFIGURACIÓN =========================
        /// <summary>
        /// Modo debug: usa credenciales y SpreadsheetId de pruebas
        /// </summary>
        public static bool DebugMode = true;

        // SpreadsheetId para debug/pruebas
        private const string DEBUG_SPREADSHEET_ID = "1wkoiENU6aSPuLgZWOnGuP7sGn73K4wSTD9S0heDG18k";

        // SpreadsheetId para producción (configurar cuando esté listo)
        private const string PROD_SPREADSHEET_ID = "TU_SPREADSHEET_ID_PRODUCCION";

        // Archivos de credenciales (embedded resources)
        private const string DEBUG_CREDENTIALS_FILE = "credenciales_debug.json";
        private const string PROD_CREDENTIALS_FILE = "credenciales.json";

        // ========================= PROPIEDADES =========================
        private static string SpreadsheetId => DebugMode ? DEBUG_SPREADSHEET_ID : PROD_SPREADSHEET_ID;
        private static string CredentialsFileName => DebugMode ? DEBUG_CREDENTIALS_FILE : PROD_CREDENTIALS_FILE;

        private static SheetsService? _instance;
        private Google.Apis.Sheets.v4.SheetsService? _sheetsApi;
        private bool _initialized = false;

        // ========================= SINGLETON =========================
        public static SheetsService Instance
        {
            get
            {
                _instance ??= new SheetsService();
                return _instance;
            }
        }

        private SheetsService() { }

        // ========================= INICIALIZACIÓN =========================
        /// <summary>
        /// Inicializa el servicio de Google Sheets con las credenciales de Service Account
        /// </summary>
        public async Task InitAsync()
        {
            if (_initialized)
                return;

            try
            {
                // Cargar credenciales desde Raw Assets
                using var stream = await LoadCredentialsStream();
                if (stream == null)
                {
                    throw new Exception($"No se encontró el archivo de credenciales: {CredentialsFileName}");
                }

                var credential = GoogleCredential.FromStream(stream)
                    .CreateScoped(Google.Apis.Sheets.v4.SheetsService.Scope.Spreadsheets);

                _sheetsApi = new Google.Apis.Sheets.v4.SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Examen Asistencia"
                });

                _initialized = true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al inicializar Google Sheets: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Carga el stream del archivo de credenciales desde MauiAsset
        /// </summary>
        private async Task<Stream> LoadCredentialsStream()
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync(CredentialsFileName);
                // Copiamos a MemoryStream porque el stream original puede cerrarse
                var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando credenciales: {ex.Message}");
                return null;
            }
        }

        // ========================= GESTIÓN DE PESTAÑAS =========================
        /// <summary>
        /// Obtiene el nombre de la pestaña para el mes actual (formato: MES_AÑO)
        /// </summary>
        private string GetCurrentMonthSheetName()
        {
            var now = DateTime.Now;
            var culture = new CultureInfo("es-MX");
            var monthName = culture.DateTimeFormat.GetMonthName(now.Month).ToUpper();
            return $"{monthName}_{now.Year}";
        }

        /// <summary>
        /// Verifica si una pestaña existe en el spreadsheet
        /// </summary>
        private async Task<bool> SheetExistsAsync(string sheetName)
        {
            var spreadsheet = await _sheetsApi.Spreadsheets.Get(SpreadsheetId).ExecuteAsync();
            return spreadsheet.Sheets.Any(s => s.Properties.Title == sheetName);
        }

        /// <summary>
        /// Obtiene el SheetId de una pestaña por nombre
        /// </summary>
        private async Task<int?> GetSheetIdAsync(string sheetName)
        {
            var spreadsheet = await _sheetsApi.Spreadsheets.Get(SpreadsheetId).ExecuteAsync();
            var sheet = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.Title == sheetName);
            return sheet?.Properties.SheetId;
        }

        /// <summary>
        /// Crea una nueva pestaña con encabezados
        /// Nueva estructura: [Fecha, Correo, Entrada, Salida, Latitud, Longitud]
        /// </summary>
        private async Task CreateSheetWithHeadersAsync(string sheetName)
        {
            // 1. Crear la pestaña
            var addSheetRequest = new AddSheetRequest
            {
                Properties = new SheetProperties { Title = sheetName }
            };

            var batchRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request>
                {
                    new Request { AddSheet = addSheetRequest }
                }
            };

            await _sheetsApi.Spreadsheets.BatchUpdate(batchRequest, SpreadsheetId).ExecuteAsync();

            // 2. Agregar encabezados - NUEVA ESTRUCTURA
            var headers = new List<object> { "Fecha", "Correo", "Entrada", "Salida", "Latitud", "Longitud" };
            var range = $"{sheetName}!A1:F1";
            var valueRange = new ValueRange
            {
                Values = new List<IList<object>> { headers }
            };

            var updateRequest = _sheetsApi.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await updateRequest.ExecuteAsync();
        }

        /// <summary>
        /// Asegura que exista la pestaña del mes actual
        /// </summary>
        private async Task EnsureCurrentMonthSheetAsync()
        {
            var sheetName = GetCurrentMonthSheetName();
            if (!await SheetExistsAsync(sheetName))
            {
                await CreateSheetWithHeadersAsync(sheetName);
            }
        }

        // ========================= OPERACIONES DE DATOS =========================

        /// <summary>
        /// Resultado del estado de registro del usuario
        /// </summary>
        public enum EstadoRegistro
        {
            SinRegistro,        // No hay registro hoy - puede registrar ENTRADA
            SoloEntrada,        // Ya tiene entrada - puede registrar SALIDA
            EntradaYSalida      // Ya tiene ambos - no puede registrar más
        }

        /// <summary>
        /// Obtiene el estado de registro del usuario para hoy
        /// </summary>
        public async Task<(EstadoRegistro estado, int? rowIndex)> GetEstadoRegistroHoyAsync(string correo)
        {
            if (!_initialized)
                await InitAsync();

            var sheetName = GetCurrentMonthSheetName();

            // Si no existe la pestaña del mes, no hay registros
            if (!await SheetExistsAsync(sheetName))
                return (EstadoRegistro.SinRegistro, null);

            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var range = $"{sheetName}!A:D"; // Fecha, Correo, Entrada, Salida

            var getRequest = _sheetsApi.Spreadsheets.Values.Get(SpreadsheetId, range);
            var response = await getRequest.ExecuteAsync();

            if (response.Values == null)
                return (EstadoRegistro.SinRegistro, null);

            // Buscar registro del correo en la fecha de hoy
            for (int i = 1; i < response.Values.Count; i++) // Skip header (index 0)
            {
                var row = response.Values[i];
                if (row.Count >= 2)
                {
                    var fecha = row[0]?.ToString();
                    var correoRow = row[1]?.ToString();

                    if (fecha == today && correoRow?.Equals(correo, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        // Encontramos registro de hoy
                        var entrada = row.Count > 2 ? row[2]?.ToString() : "";
                        var salida = row.Count > 3 ? row[3]?.ToString() : "";

                        if (!string.IsNullOrWhiteSpace(entrada) && !string.IsNullOrWhiteSpace(salida))
                            return (EstadoRegistro.EntradaYSalida, i + 1); // +1 porque Sheets es 1-indexed

                        if (!string.IsNullOrWhiteSpace(entrada))
                            return (EstadoRegistro.SoloEntrada, i + 1);

                        return (EstadoRegistro.SinRegistro, i + 1);
                    }
                }
            }

            return (EstadoRegistro.SinRegistro, null);
        }

        /// <summary>
        /// Registra la ENTRADA del usuario (crea nueva fila)
        /// </summary>
        public async Task<bool> RegistrarEntradaAsync(string correo, double latitud, double longitud)
        {
            if (!_initialized)
                await InitAsync();

            // Verificar conectividad
            if (!await HasInternetConnectionAsync())
            {
                throw new Exception("No hay conexión a internet. No se puede registrar la asistencia.");
            }

            // Asegurar que existe la pestaña del mes actual
            await EnsureCurrentMonthSheetAsync();

            var sheetName = GetCurrentMonthSheetName();
            var now = DateTime.Now;

            // Crear fila de datos: [Fecha, Correo, Entrada, Salida(vacío), Latitud, Longitud]
            var rowData = new List<object>
            {
                now.ToString("yyyy-MM-dd"),
                correo,
                now.ToString("HH:mm:ss"),
                "", // Salida vacía
                latitud.ToString("F6", CultureInfo.InvariantCulture),
                longitud.ToString("F6", CultureInfo.InvariantCulture)
            };

            var range = $"{sheetName}!A:F";
            var valueRange = new ValueRange
            {
                Values = new List<IList<object>> { rowData }
            };

            var appendRequest = _sheetsApi.Spreadsheets.Values.Append(valueRange, SpreadsheetId, range);
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
            appendRequest.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

            await appendRequest.ExecuteAsync();
            return true;
        }

        /// <summary>
        /// Registra la SALIDA del usuario (actualiza fila existente)
        /// </summary>
        public async Task<bool> RegistrarSalidaAsync(string correo, int rowIndex, double latitud, double longitud)
        {
            if (!_initialized)
                await InitAsync();

            // Verificar conectividad
            if (!await HasInternetConnectionAsync())
            {
                throw new Exception("No hay conexión a internet. No se puede registrar la asistencia.");
            }

            var sheetName = GetCurrentMonthSheetName();
            var now = DateTime.Now;

            // Actualizar solo la columna de Salida (columna D = índice 4)
            var range = $"{sheetName}!D{rowIndex}";
            var valueRange = new ValueRange
            {
                Values = new List<IList<object>> { new List<object> { now.ToString("HH:mm:ss") } }
            };

            var updateRequest = _sheetsApi.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

            await updateRequest.ExecuteAsync();
            return true;
        }

        /// <summary>
        /// Método principal para registrar asistencia automáticamente
        /// Detecta si debe registrar entrada o salida
        /// </summary>
        public async Task<(bool exito, string mensaje)> RegistrarAsistenciaAutoAsync(string correo, double latitud, double longitud)
        {
            if (!_initialized)
                await InitAsync();

            var (estado, rowIndex) = await GetEstadoRegistroHoyAsync(correo);

            switch (estado)
            {
                case EstadoRegistro.SinRegistro:
                    await RegistrarEntradaAsync(correo, latitud, longitud);
                    return (true, $"✅ ENTRADA registrada a las {DateTime.Now:HH:mm:ss}");

                case EstadoRegistro.SoloEntrada:
                    if (rowIndex.HasValue)
                    {
                        await RegistrarSalidaAsync(correo, rowIndex.Value, latitud, longitud);
                        return (true, $"✅ SALIDA registrada a las {DateTime.Now:HH:mm:ss}");
                    }
                    return (false, "Error: No se encontró el registro de entrada.");

                case EstadoRegistro.EntradaYSalida:
                    return (false, "⚠️ Ya tienes registrada ENTRADA y SALIDA hoy. Contacta a un administrador si necesitas hacer cambios.");

                default:
                    return (false, "Error desconocido.");
            }
        }

        // ========================= UTILIDADES =========================
        /// <summary>
        /// Verifica si hay conexión a internet
        /// </summary>
        public async Task<bool> HasInternetConnectionAsync()
        {
            try
            {
                var current = Connectivity.Current.NetworkAccess;
                return current == NetworkAccess.Internet;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Valida que el correo termine con el dominio corporativo
        /// </summary>
        public static bool ValidarCorreoCorporativo(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
                return false;

            correo = correo.Trim().ToLower();
            return correo.EndsWith("@exploraie.com");
        }
    }

    /// <summary>
    /// Modelo para representar un registro de asistencia desde Sheets
    /// </summary>
    public class AsistenciaRecord
    {
        public string? Fecha { get; set; }
        public string? Correo { get; set; }
        public string? Entrada { get; set; }
        public string? Salida { get; set; }
        public string? Latitud { get; set; }
        public string? Longitud { get; set; }
    }
}
