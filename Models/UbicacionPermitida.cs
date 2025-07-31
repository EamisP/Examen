using SQLite;

namespace Examen.Models
{
    public class UbicacionPermitida
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string NombreLugar { get; set; }

        public double Latitud { get; set; }

        public double Longitud { get; set; }

        public double RadioMetros { get; set; } // radio permitido para registrar asistencia
    }
}