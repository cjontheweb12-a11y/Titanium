// Browser/Services/Interfaces/IDownloadService.cs
using Browser.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;

namespace Browser.Services.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de gestión de descargas.
    /// Motivo: Define el contrato para iniciar y monitorear descargas.
    /// Usar una interfaz promueve la inyección de dependencias y la modularidad,
    /// permitiendo que las ViewModels dependan de una abstracción en lugar de una implementación concreta.
    /// Esto facilita el testing y la futura sustitución de la lógica de descargas.
    /// </summary>
    public interface IDownloadService
    {
        /// <summary>
        /// Obtiene la colección de elementos de descarga activos.
        /// Motivo: Permite a la interfaz de usuario enlazar y mostrar el estado de todas las descargas activas.
        /// ObservableCollection es ideal para actualizaciones en tiempo real de la UI.
        /// </summary>
        ObservableCollection<DownloadItem> ActiveDownloads { get; }

        /// <summary>
        /// Inicia una nueva descarga de forma asíncrona.
        /// </summary>
        /// <param name="uri">La URI del recurso a descargar.</param>
        /// <param name="fileName">El nombre de archivo sugerido para guardar.</param>
        /// <param name="fileSavePickerResult">El StorageFile donde se guardará el archivo.</param>
        /// <returns>El DownloadItem creado para la descarga.</returns>
        Task<DownloadItem> StartDownloadAsync(Uri uri, string fileName, Windows.Storage.StorageFile fileSavePickerResult);

        /// <summary>
        /// Reanuda todas las descargas pendientes o previamente registradas.
        /// Motivo: Permite a la aplicación restaurar el estado de las descargas tras un reinicio o suspensión.
        /// </summary>
        Task DiscoverExistingDownloadsAsync();

        /// <summary>
        /// Cancela una descarga activa.
        /// </summary>
        /// <param name="downloadItem">El elemento de descarga a cancelar.</param>
        void CancelDownload(DownloadItem downloadItem);

        /// <summary>
        /// Pausa una descarga activa.
        /// Motivo: Permite al usuario detener temporalmente una descarga.
        /// </summary>
        /// <param name="downloadItem">El elemento de descarga a pausar.</param>
        void PauseDownload(DownloadItem downloadItem);

        /// <summary>
        /// Reanuda una descarga pausada.
        /// Motivo: Permite al usuario continuar una descarga previamente pausada.
        /// </summary>
        void ResumeDownload(DownloadItem downloadItem);
    }
}
