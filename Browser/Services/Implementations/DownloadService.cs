// Browser/Services/Implementations/DownloadService.cs
using Browser.Models;
using Browser.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.Web;

namespace Browser.Services.Implementations
{
    /// <summary>
    /// Implementación del servicio de gestión de descargas.
    /// Motivo: Proporciona la lógica para iniciar, monitorear y controlar descargas
    /// utilizando la API BackgroundTransfer de UWP. Esto permite que las descargas
    /// continúen incluso si la aplicación es suspendida.
    /// </summary>
    public class DownloadService : IDownloadService
    {
        private ObservableCollection<DownloadItem> _activeDownloads = new ObservableCollection<DownloadItem>();
        private CancellationTokenSource _cts; // Motivo: Para cancelar descargas activas.

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public ObservableCollection<DownloadItem> ActiveDownloads => _activeDownloads;

        /// <summary>
        /// Constructor de la clase <see cref="DownloadService"/>.
        /// Motivo: Inicializa el servicio y comienza a buscar descargas pendientes.
        /// </summary>
        public DownloadService()
        {
            _cts = new CancellationTokenSource();
            // Motivo: Descubrir descargas existentes en el constructor para restaurar el estado
            // si la aplicación se cerró mientras había descargas en curso.
            DiscoverExistingDownloadsAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    // Motivo: Registrar el error si no se pueden restaurar las descargas.
                    System.Diagnostics.Debug.WriteLine($"Error discovering downloads: {task.Exception?.Message}");
                }
            });
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task DiscoverExistingDownloadsAsync()
        {
            // Motivo: Recuperar operaciones de descarga que estaban en progreso antes de que la aplicación se cerrara.
            // Esto es crucial para la persistencia del estado de las descargas.
            var operations = await BackgroundDownloader.GetCurrentDownloadsAsync();

            foreach (var operation in operations)
            {
                // Si la descarga ya está en nuestra lista (por ejemplo, si la aplicación se suspendió y reanudó rápidamente),
                // no la agregamos de nuevo.
                if (_activeDownloads.Any(d => d.DownloadOperation.Guid == operation.Guid)) continue;

                var downloadItem = new DownloadItem
                {
                    SourceUrl = operation.RequestedUri.OriginalString,
                    FileName = operation.ResultFile?.Name ?? "Unknown File", // El nombre del archivo podría no estar disponible si no se guardó el resultado aún.
                    LocalFilePath = operation.ResultFile?.Path,
                    DownloadOperation = operation,
                    Progress = 0,
                    Status = "Reanudando..."
                };

                // Asignar el nombre del archivo si es posible
                if (operation.ResultFile != null)
                {
                    downloadItem.FileName = operation.ResultFile.Name;
                    downloadItem.LocalFilePath = operation.ResultFile.Path;
                }
                else if (operation.Progress.TotalBytesToReceive > 0)
                {
                    // Intentar derivar el nombre del archivo de la URI si no está disponible aún
                    downloadItem.FileName = System.IO.Path.GetFileName(operation.RequestedUri.LocalPath);
                }

                _activeDownloads.Add(downloadItem);
                AttachProgressAndCompletionHandlers(downloadItem);

                // Reanudar la operación si no está completada o pausada.
                if (operation.Progress.Status == BackgroundTransferStatus.Running ||
                    operation.Progress.Status == BackgroundTransferStatus.PausedByApplication ||
                    operation.Progress.Status == BackgroundTransferStatus.PausedByCostedNetwork ||
                    operation.Progress.Status == BackgroundTransferStatus.PausedNoNetwork)
                {
                    // Se reanuda la descarga; el handler de progreso actualizará el estado.
                    await operation.AttachAsync();
                }
                else if (operation.Progress.Status == BackgroundTransferStatus.Completed)
                {
                    downloadItem.Progress = 100;
                    downloadItem.Status = "Completado";
                }
                else if (operation.Progress.Status == BackgroundTransferStatus.Error)
                {
                    downloadItem.Status = "Error";
                }
                else if (operation.Progress.Status == BackgroundTransferStatus.Canceled)
                {
                    downloadItem.Status = "Cancelado";
                }
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public async Task<DownloadItem> StartDownloadAsync(Uri uri, string fileName, StorageFile fileSavePickerResult)
        {
            BackgroundDownloader downloader = new BackgroundDownloader();
            DownloadOperation downloadOperation = downloader.CreateDownload(uri, fileSavePickerResult);

            var downloadItem = new DownloadItem
            {
                SourceUrl = uri.OriginalString,
                FileName = fileName,
                LocalFilePath = fileSavePickerResult.Path,
                DownloadOperation = downloadOperation,
                Progress = 0,
                Status = "Iniciando..."
            };

            _activeDownloads.Add(downloadItem);
            AttachProgressAndCompletionHandlers(downloadItem);

            try
            {
                // Motivo: Iniciar la operación de descarga en segundo plano.
                // El monitoreo de progreso y completado se maneja en los handlers adjuntos.
                Progress<DownloadOperation> progressCallback = new Progress<DownloadOperation>(DownloadProgress);
                await downloadOperation.StartAsync().AsTask(_cts.Token, progressCallback);

                downloadItem.Status = "Completado";
                downloadItem.Progress = 100;
            }
            catch (OperationCanceledException)
            {
                downloadItem.Status = "Cancelado";
                // Motivo: Limpiar la descarga cancelada si se desea, o mantenerla para registro.
                // _activeDownloads.Remove(downloadItem);
            }
            catch (Exception ex)
            {
                // Motivo: Manejar errores de descarga y actualizar el estado.
                // Mostrar un mensaje de error amigable al usuario.
                WebErrorStatus webErrorStatus = BackgroundTransferError.GetStatus(ex.HResult);
                downloadItem.Status = $"Error: {webErrorStatus.ToString()} - {ex.Message}";
                var dialog = new MessageDialog($"Error al descargar '{fileName}': {ex.Message}", "Error de Descarga");
                await dialog.ShowAsync();
                // Opcional: Eliminar el archivo parcial.
                // await fileSavePickerResult.DeleteAsync(StorageDeleteOption.Default);
            }
            return downloadItem;
        }

        /// <summary>
        /// Adjunta handlers de progreso y completado a la operación de descarga.
        /// Motivo: Centraliza la lógica para actualizar el DownloadItem con el progreso y el estado final.
        /// </summary>
        /// <param name="downloadItem">El DownloadItem a monitorear.</param>
        private void AttachProgressAndCompletionHandlers(DownloadItem downloadItem)
        {
            IProgress<DownloadOperation> progressCallback = new Progress<DownloadOperation>(DownloadProgress);
            downloadItem.DownloadOperation.AttachAsync().AsTask(_cts.Token, progressCallback);
        }

        /// <summary>
        /// Handler para el progreso de la descarga.
        /// Motivo: Se invoca regularmente para actualizar el progreso de la UI.
        /// </summary>
        /// <param name="operation">La operación de descarga en progreso.</param>
        private void DownloadProgress(DownloadOperation operation)
        {
            int progress = 0;
            if (operation.Progress.TotalBytesToReceive > 0)
            {
                progress = (int)(operation.Progress.BytesReceived * 100 / operation.Progress.TotalBytesToReceive);
            }

            var downloadItem = _activeDownloads.FirstOrDefault(d => d.DownloadOperation.Guid == operation.Guid);
            if (downloadItem != null)
            {
                downloadItem.Progress = progress;
                downloadItem.Status = GetStatusString(operation.Progress.Status);

                if (operation.Progress.Status == BackgroundTransferStatus.Completed)
                {
                    downloadItem.Progress = 100;
                    downloadItem.Status = "Completado";
                }
            }
        }

        /// <summary>
        /// Obtiene una cadena representativa del estado de la descarga.
        /// Motivo: Traduce el enum BackgroundTransferStatus a una cadena legible para el usuario.
        /// </summary>
        /// <param name="status">El estado de la descarga.</param>
        /// <returns>Una cadena localizada que describe el estado.</returns>
        private string GetStatusString(BackgroundTransferStatus status)
        {
            // Motivo: Proporcionar mensajes de estado amigables en español.
            switch (status)
            {
                case BackgroundTransferStatus.Running: return "Descargando...";
                case BackgroundTransferStatus.PausedByApplication: return "Pausada";
                case BackgroundTransferStatus.PausedCostedNetwork: return "Pausada (red de pago)";
                case BackgroundTransferStatus.PausedNoNetwork: return "Pausada (sin red)";
                case BackgroundTransferStatus.Completed: return "Completado";
                case BackgroundTransferStatus.Canceled: return "Cancelado";
                case BackgroundTransferStatus.Error: return "Error";
                case BackgroundTransferStatus.Idle: return "Esperando";
                case BackgroundTransferStatus.Starting: return "Iniciando...";
                default: return "Desconocido";
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void CancelDownload(DownloadItem downloadItem)
        {
            if (downloadItem?.DownloadOperation != null)
            {
                try
                {
                    downloadItem.DownloadOperation.Cancel();
                    downloadItem.Status = "Cancelado";
                    // Motivo: Eliminar el DownloadItem de la lista activa después de la cancelación.
                    _activeDownloads.Remove(downloadItem);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error canceling download: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void PauseDownload(DownloadItem downloadItem)
        {
            if (downloadItem?.DownloadOperation != null && downloadItem.DownloadOperation.Progress.Status == BackgroundTransferStatus.Running)
            {
                try
                {
                    downloadItem.DownloadOperation.Pause();
                    downloadItem.Status = "Pausada";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error pausing download: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void ResumeDownload(DownloadItem downloadItem)
        {
            if (downloadItem?.DownloadOperation != null &&
                (downloadItem.DownloadOperation.Progress.Status == BackgroundTransferStatus.PausedByApplication ||
                 downloadItem.DownloadOperation.Progress.Status == BackgroundTransferStatus.PausedByCostedNetwork ||
                 downloadItem.DownloadOperation.Progress.Status == BackgroundTransferStatus.PausedNoNetwork))
            {
                try
                {
                    downloadItem.DownloadOperation.Resume();
                    downloadItem.Status = "Reanudando...";
                    // Motivo: Reiniciar el seguimiento del progreso si se reanuda la descarga.
                    AttachProgressAndCompletionHandlers(downloadItem);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error resuming download: {ex.Message}");
                }
            }
        }
    }
}
