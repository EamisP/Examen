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

        public Task<int> InsertarAsistenciaAsync(Asistencia asistencia)
        {
            return _db.InsertAsync(asistencia);
        }

        public Task<List<Asistencia>> GetAsistenciasAsync()
        {
            return _db.Table<Asistencia>().OrderByDescending(a => a.FechaHora).ToListAsync();
        }

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
