using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Domain.Values;

namespace FAFS.Destinations
{
    // Entidad principal que representa un Destino Turístico en la base de datos.
    // AuditedAggregateRoot agrega automáticamente campos de auditoría (quién y cuándo creó/editó).
    public class Destination : AuditedAggregateRoot<Guid>
    {
        public string Name { get; private set; } = string.Empty; // Nombre del destino (ej: Torre Eiffel)
        public string Country { get; private set; } = string.Empty; // País (ej: Francia)
        public string City { get; private set; } = string.Empty; // Ciudad (ej: París)
        public string PhotoUrl { get; private set; } = string.Empty; // Enlace a la foto del lugar
        public Coordinates Coordinates { get; private set; } = default!; // Coordenadas geográficas (Objeto de Valor)
        public DateTime LastUpdated { get; private set; } = DateTime.Now; // Última actualización local

        // Constructor vacío requerido por Entity Framework Core para mapear la base de datos
        protected Destination() { }

        public Destination(
            Guid id,
            string name,
            string country,
            string city,
            string photoUrl,
            DateTime lastUpdated,
            Coordinates coordinates
        ) : base(id)
        {
            Name = name;
            Country = country;
            City = city;
            PhotoUrl = photoUrl;
            LastUpdated = lastUpdated;
            Coordinates = coordinates;
        }
    }

    // Objeto de Valor (Value Object) para encapsular la Latitud y Longitud geográficas de forma atómica.
    public class Coordinates : ValueObject
    {
        public string Latitude { get; private set; } = string.Empty;
        public string Longitude { get; private set; } = string.Empty;

        // Constructor requerido por EF Core
        protected Coordinates() { }

        public Coordinates(string latitude, string longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        // ABP compara objetos de valor por sus atributos atómicos en vez de por su referencia en memoria
        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Latitude;
            yield return Longitude;
        }
    }
}
