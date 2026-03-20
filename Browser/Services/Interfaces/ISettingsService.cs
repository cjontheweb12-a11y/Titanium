// Browser/Services/Interfaces/ISettingsService.cs
using Browser.Models;
using System.Threading.Tasks;

namespace Browser.Services.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de gestión de configuraciones de la aplicación.
    /// Motivo: Define el contrato para cargar y guardar las configuraciones de la aplicación.
    /// Usar una interfaz promueve la inyección de dependencias y la modularidad,
    /// permitiendo que las ViewModels dependan de una abstracción en lugar de una implementación concreta.
    /// Esto facilita el testing y la futura sustitución de la lógica de almacenamiento de configuraciones.
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Obtiene la instancia actual de las configuraciones de la aplicación.
        /// Motivo: Proporciona acceso a todas las configuraciones de forma centralizada.
        /// </summary>
        AppSettings CurrentSettings { get; }

        /// <summary>
        /// Carga las configuraciones de la aplicación desde el almacenamiento persistente de forma asíncrona.
        /// </summary>
        Task LoadSettingsAsync();

        /// <summary>
        /// Guarda las configuraciones actuales de la aplicación en el almacenamiento persistente de forma asíncrona.
        /// </summary>
        Task SaveSettingsAsync();
    }
}
