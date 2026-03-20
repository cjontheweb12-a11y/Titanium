// Browser/ViewModels/DownloadManagerViewModel.cs
using Browser.Models;
using Browser.Services.Interfaces;
using Browser.ViewModels.Base;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;

namespace Browser.ViewModels
{
    /// <summary>
    /// ViewModel para la página de gestión de descargas (DownloadManagerPage.xaml).
    /// Motivo: Proporciona la lógica para mostrar las descargas activas,
    /// controlar su estado (pausar, reanudar, cancelar) y abrir los archivos descargados.
    /// Adhiere al patrón MVVM.
    /// </summary>
    public class DownloadManagerViewModel : ObservableObject
    {
        private readonly IDownloadService _downloadService;

        /// <summary>
        /// Obtiene la colección de elementos de descarga activos.
        /// Motivo: Enlazada a la lista de descargas en la UI.
        /// ObservableCollection notifica a la UI sobre adiciones/eliminaciones y actualizaciones de progreso.
        /// </summary>
        public ObservableCollection<DownloadItem> ActiveDownloads => _downloadService.ActiveDownloads;

        private DownloadItem _selectedDownload;
        /// <summary>
        /// Obtiene o establece el elemento de descarga actualmente seleccionado en la UI.
        /// Motivo: Permite operar sobre una descarga específica (pausar, cancelar, abrir).
        /// </summary>
        public DownloadItem SelectedDownload
        {
            get => _selectedDownload;
            set
            {
                if (SetProperty(ref _selectedDownload, value))
                {
                    // Motivo: Actualizar la capacidad de ejecución de los comandos al cambiar la selección.
                    PauseDownloadCommand.RaiseCanExecuteChanged();
                    ResumeDownloadCommand.RaiseCanExecuteChanged();
                    CancelDownloadCommand.RaiseCanExecuteChanged();
                    OpenDownloadCommand.RaiseCanExecuteChanged();
                    OpenDownloadFolderCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Comando para pausar la descarga seleccionada.
        /// Motivo: Enlazado al botón "Pausar".
        /// </summary>
        public RelayCommand PauseDownloadCommand { get; }

        /// <summary>
        /// Comando para reanudar la descarga seleccionada.
        /// Motivo: Enlazado al botón "Reanudar".
        /// </summary>
        public RelayCommand ResumeDownloadCommand { get; }

        /// <summary>
        /// Comando para cancelar la descarga seleccionada.
        /// Motivo: Enlazado al botón "Cancelar".
        /// </summary>
        public RelayCommand CancelDownloadCommand { get; }

        /// <summary>
        /// Comando para abrir el archivo de descarga.
        /// Motivo: Enlazado al botón "Abrir Archivo".
        /// </summary>
        public RelayCommand OpenDownloadCommand { get; }

        /// <summary>
        /// Comando para abrir la carpeta que contiene el archivo descargado.
        /// Motivo: Enlazado al botón "Abrir Carpeta".
        /// </summary>
        public RelayCommand OpenDownloadFolderCommand { get; }


        /// <summary>
        /// Constructor de <see cref="DownloadManagerViewModel"/>.
        /// Motivo: Inicializa la ViewModel con las dependencias de servicio necesarias
        /// y configura los comandos.
        /// </summary>
        /// <param name="downloadService">Servicio de gestión de descargas.</param>
        public DownloadManagerViewModel(IDownloadService downloadService)
        {
            _downloadService = downloadService;

            // Motivo: Inicializar comandos, pasando los delegados para Execute y CanExecute.
            // Los comandos verifican el estado de la descarga seleccionada para determinar si pueden ejecutarse.
            PauseDownloadCommand = new RelayCommand(PauseDownload, () => SelectedDownload != null && SelectedDownload.Status == "Descargando...");
            ResumeDownloadCommand = new RelayCommand(ResumeDownload, () => SelectedDownload != null && SelectedDownload.Status == "Pausada");
            CancelDownloadCommand = new RelayCommand(CancelDownload, () => SelectedDownload != null && (SelectedDownload.Status == "Descargando..." || SelectedDownload.Status == "Pausada" || SelectedDownload.Status == "Reanudando..."));
            OpenDownloadCommand = new RelayCommand(async () => await OpenDownloadAsync(), () => SelectedDownload != null && SelectedDownload.Status == "Completado" && !string.IsNullOrEmpty(SelectedDownload.LocalFilePath));
            OpenDownloadFolderCommand = new RelayCommand(async () => await OpenDownloadFolderAsync(), () => SelectedDownload != null && !string.IsNullOrEmpty(SelectedDownload.LocalFilePath));

            // Motivo: Asegurarse de que el servicio de descargas haya cargado las descargas existentes.
            _ = _downloadService.DiscoverExistingDownloadsAsync();
        }

        /// <summary>
        /// Pausa la descarga seleccionada.
        /// Motivo: Delega la acción al servicio de descargas.
        /// </summary>
        private void PauseDownload()
        {
            if (SelectedDownload != null)
            {
                _downloadService.PauseDownload(SelectedDownload);
                PauseDownloadCommand.RaiseCanExecuteChanged();
                ResumeDownloadCommand.RaiseCanExecuteChanged();
                CancelDownloadCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Reanuda la descarga seleccionada.
        /// Motivo: Delega la acción al servicio de descargas.
        /// </summary>
        private void ResumeDownload()
        {
            if (SelectedDownload != null)
            {
                _downloadService.ResumeDownload(SelectedDownload);
                PauseDownloadCommand.RaiseCanExecuteChanged();
                ResumeDownloadCommand.RaiseCanExecuteChanged();
                CancelDownloadCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Cancela la descarga seleccionada.
        /// Motivo: Delega la acción al servicio de descargas.
        /// </summary>
        private void CancelDownload()
        {
            if (SelectedDownload != null)
            {
                _downloadService.CancelDownload(SelectedDownload);
                // No es necesario actualizar CanExecute aquí directamente,
                // ya que la descarga se eliminará de la colección,
                // deseleccionando automáticamente SelectedDownload.
            }
        }

        /// <summary>
        /// Abre el archivo descargado.
        /// Motivo: Permite al usuario acceder al contenido descargado.
        /// </summary>
        private async Task OpenDownloadAsync()
        {
            if (SelectedDownload == null || string.IsNullOrEmpty(SelectedDownload.LocalFilePath)) return;

            try
            {
                // Motivo: Obtener el archivo desde la ruta local.
                StorageFile file = await StorageFile.GetFileFromPathAsync(SelectedDownload.LocalFilePath);
                // Motivo: Abrir el archivo con la aplicación predeterminada del sistema.
                await Launcher.LaunchFileAsync(file);
            }
            catch (Exception ex)
            {
                // Motivo: Manejar errores al intentar abrir el archivo.
                // Mostrar un mensaje de error amigable al usuario.
                var dialog = new MessageDialog($"No se pudo abrir el archivo: {ex.Message}", "Error al Abrir Archivo");
                await dialog.ShowAsync();
            }
        }

        /// <summary>
        /// Abre la carpeta que contiene el archivo descargado.
        /// Motivo: Permite al usuario localizar el archivo en el explorador de archivos.
        /// </summary>
        private async Task OpenDownloadFolderAsync()
        {
            if (SelectedDownload == null || string.IsNullOrEmpty(SelectedDownload.LocalFilePath)) return;

            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(SelectedDownload.LocalFilePath);
                StorageFolder folder = await file.GetParentAsync();
                // Motivo: Abrir la carpeta que contiene el archivo.
                await Launcher.LaunchFolderAsync(folder);
            }
            catch (Exception ex)
            {
                // Motivo: Manejar errores al intentar abrir la carpeta.
                // Mostrar un mensaje de error amigable al usuario.
                var dialog = new MessageDialog($"No se pudo abrir la carpeta: {ex.Message}", "Error al Abrir Carpeta");
                await dialog.ShowAsync();
            }
        }
    }
}
