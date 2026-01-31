/*
 * ============================================================================
 * CÓDIGO OBSOLETO - SQLite reemplazado por Google Sheets API
 * ============================================================================
 * Este servicio ya no se usa para la funcionalidad principal de asistencia.
 * La nueva implementación usa SheetsService.cs
 * 
 * Mantenido por compatibilidad con las páginas de administración (CRUD).
 * TODO: Migrar las páginas de administración a Google Sheets si es necesario.
 * ============================================================================
 */

using SQLite;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Examen.Models;
using Microsoft.Maui.Storage;
using System;
using System.Linq;

namespace Examen.Services
{
    /// <summary>
    /// [LEGACY] Servicio de SQLite - Usado solo para páginas de administración
    /// Para asistencia, usar SheetsService.cs
    /// </summary>
    public class SQLiteService
    {
        private SQLiteAsyncConnection _db;

        public async Task InitAsync()
        {
            if (_db != null)
                return;

            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "asistencia.db3");
            _db = new SQLiteAsyncConnection(dbPath);
            await _db.CreateTableAsync<Usuario>();
            await _db.CreateTableAsync<UbicacionPermitida>();
            await _db.CreateTableAsync<Asistencia>();
        }

        public Task<List<Usuario>> GetUsuariosAsync()
        {
            return _db.Table<Usuario>().ToListAsync();
        }

        public Task<int> AddUsuarioAsync(Usuario usuario)
        {
            return _db.InsertAsync(usuario);
        }

        public Task<int> UpdateUsuarioAsync(Usuario usuario)
        {
            return _db.UpdateAsync(usuario);
        }

        public Task<int> DeleteUsuarioAsync(Usuario usuario)
        {
            return _db.DeleteAsync(usuario);
        }
        public Task<Usuario> GetUsuarioPorMatriculaAsync(string matricula)
        {
            return _db.Table<Usuario>().FirstOrDefaultAsync(u => u.Matricula == matricula);
        }

        public Task<UbicacionPermitida> GetPrimeraUbicacionPermitidaAsync()
        {
            return _db.Table<UbicacionPermitida>().FirstOrDefaultAsync();
        }

        // Métodos para gestionar ubicaciones permitidas
        public Task<List<UbicacionPermitida>> GetUbicacionesAsync()
        {
            return _db.Table<UbicacionPermitida>().ToListAsync();
        }

        public Task<int> InsertarUbicacionAsync(UbicacionPermitida ubicacion)
        {
            return _db.InsertAsync(ubicacion);
        }

        public Task<int> UpdateUbicacionAsync(UbicacionPermitida ubicacion)
        {
            return _db.UpdateAsync(ubicacion);
        }

        public Task<int> EliminarUbicacionAsync(UbicacionPermitida ubicacion)
        {
            return _db.DeleteAsync(ubicacion);
        }

        // ============================================================================
        // MÉTODOS OBSOLETOS - La asistencia ahora se guarda en Google Sheets
        // ============================================================================

        [Obsolete("Usar SheetsService.RegistrarAsistenciaAsync en su lugar")]
        public Task<int> InsertarAsistenciaAsync(Asistencia asistencia)
        {
            return _db.InsertAsync(asistencia);
        }

        [Obsolete("Usar SheetsService.GetRegistrosHoyAsync en su lugar")]
        public Task<List<Asistencia>> GetAsistenciasAsync()
        {
            return _db.Table<Asistencia>().OrderByDescending(a => a.FechaHora).ToListAsync();
        }

        [Obsolete("Usar SheetsService para consultas de asistencia")]
        public Task<List<Asistencia>> GetAsistenciasPorFechaAsync(DateTime fecha)
        {
            var inicio = fecha.Date;
            var fin = inicio.AddDays(1);

            return _db.Table<Asistencia>()
                .Where(a => a.FechaHora >= inicio && a.FechaHora < fin)
                .OrderByDescending(a => a.FechaHora)
                .ToListAsync();
        }
    }
}
