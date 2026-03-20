// Browser/Models/DownloadItem.cs
using Windows.Networking.BackgroundTransfer;

namespace Browser.Models
{
    /// <summary>
    /// Modelo que representa un elemento de descarga en progreso o completado.
    /// Motivo: Encapsula toda la información relevante sobre una descarga, incluyendo
    /// su progreso, estado y la operación subyacente de BackgroundTransfer.
    /// Esto adhiere al patrón Model y facilita la visualización y gestión de descargas.
    /// </summary>
    public class DownloadItem
    {
        /// <summary>
        /// Obtiene o establece la URL de origen del archivo.
        /// Motivo: Necesario para identificar de dónde proviene la descarga y para posibles reintentos.
        /// </summary>
        public string SourceUrl { get; set; }

        /// <summary>
        /// Obtiene o establece el nombre de archivo guardado localmente.
        /// Motivo: El nombre que se le da al archivo en el sistema de archivos local.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Obtiene o establece el progreso actual de la descarga (0-100).
        /// Motivo: Permite a la interfaz de usuario mostrar una barra de progreso.
        /// </summary>
        public double Progress { get; set; }

        /// <summary>
        /// Obtiene o establece el estado actual de la descarga (Ej: En Progreso, Completado, Fallido).
        /// Motivo: Informa al usuario sobre la situación de la descarga.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Obtiene o establece la ruta completa del archivo descargado en el sistema local.
        /// Motivo: Permite abrir el archivo o la carpeta que lo contiene una vez completada la descarga.
        /// </summary>
        public string LocalFilePath { get; set; }

        /// <summary>
        /// Obtiene o establece la operación de descarga en segundo plano asociada.
        /// Motivo: Es una referencia directa a la operación del sistema UWP que maneja la descarga.
        /// Esto es crucial para controlar la descarga (pausar, reanudar, cancelar) y monitorear su estado.
        /// </summary>
        public DownloadOperation DownloadOperation { get; set; }
    }
}
