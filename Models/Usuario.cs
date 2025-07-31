

using SQLite;

namespace Examen.Models
{
    public class Usuario
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Matricula { get; set; }

        public string Nombre { get; set; }

        public string Apellidos { get; set; }

        public bool EsAdministrador { get; set; }
    }
}