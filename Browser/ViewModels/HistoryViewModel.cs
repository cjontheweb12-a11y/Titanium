// Browser/ViewModels/HistoryViewModel.cs
using Browser.Models;
using Browser.Services.Interfaces;
using Browser.ViewModels.Base;
using Browser.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace Browser.ViewModels
{
    /// <summary>
    /// ViewModel para la página de visualización del historial (HistoryPage.xaml).
    /// Motivo: Proporciona la lógica para cargar, mostrar y borrar el historial de navegación.
    /// Adhiere al patrón MVVM.
    /// </summary>
    public class HistoryViewModel : ObservableObject
    {
        private readonly IHistoryService _historyService;
        private readonly INavigationService _navigationService;

        private ObservableCollection<HistoryEntry> _historyEntries;
        /// <summary>
        /// Obtiene la colección de entradas del historial.
        /// Motivo: Enlazada a la lista del historial en la UI.
        /// ObservableCollection notifica a la UI sobre adiciones/eliminaciones.
        /// </summary>
        public ObservableCollection<HistoryEntry> HistoryEntries
        {
            get => _historyEntries;
            set => SetProperty(ref _historyEntries, value);
        }

        /// <summary>
        /// Comando para cargar todas las entradas del historial.
        /// Motivo: Se llama al cargar la página del historial.
        /// </summary>
        public RelayCommand LoadHistoryCommand { get; }

        /// <summary>
        /// Comando para borrar todo el historial.
        /// Motivo: Enlazado al botón "Borrar Historial".
        /// </summary>
        public RelayCommand ClearHistoryCommand { get; }

        /// <summary>
        /// Comando para navegar a una entrada específica del historial.
        /// Motivo: Se activa al hacer clic en un elemento del historial en la lista.
        /// </summary>
        public RelayCommand<HistoryEntry> NavigateToHistoryCommand { get; }

        /// <summary>
        /// Constructor de <see cref="HistoryViewModel"/>.
        /// Motivo: Inicializa la ViewModel con las dependencias de servicio necesarias
        /// y configura los comandos.
        /// </summary>
        /// <param name="historyService">Servicio de gestión del historial.</param>
        /// <param name="navigationService">Servicio de navegación.</param>
        public HistoryViewModel(IHistoryService historyService, INavigationService navigationService)
        {
            _historyService = historyService;
            _navigationService = navigationService;
            HistoryEntries = new ObservableCollection<HistoryEntry>();

            // Motivo: Inicializar comandos, pasando los delegados para Execute.
            LoadHistoryCommand = new RelayCommand(async () => await LoadHistoryAsync());
            ClearHistoryCommand = new RelayCommand(async () => await ClearHistoryAsync());
            NavigateToHistoryCommand = new RelayCommand<HistoryEntry>(NavigateToHistory, (entry) => entry != null);

            // Motivo: Cargar historial al construir la ViewModel.
            _ = LoadHistoryAsync();
        }

        /// <summary>
        /// Carga todas las entradas del historial del servicio y actualiza la colección.
        /// Motivo: Método asíncrono para no bloquear la UI durante la carga de datos.
        /// </summary>
        private async Task LoadHistoryAsync()
        {
            try
            {
                var entries = await _historyService.GetHistoryAsync();
                HistoryEntries.Clear(); // Limpiar la colección existente.
                foreach (var entry in entries)
                {
                    HistoryEntries.Add(entry); // Añadir las entradas del historial cargadas.
                }
            }
            catch (Exception ex)
            {
                // Motivo: Manejar errores de carga del historial.
                // Mostrar un mensaje de error amigable al usuario.
                var dialog = new MessageDialog($"Error al cargar el historial: {ex.Message}", "Error de Carga");
                await dialog.ShowAsync();
            }
        }

        /// <summary>
        /// Borra todo el historial de navegación.
        /// Motivo: Permite al usuario eliminar su historial de navegación por razones de privacidad.
        /// </summary>
        private async Task ClearHistoryAsync()
        {
            // Motivo: Confirmar la eliminación para evitar borrados accidentales.
            var confirmDialog = new MessageDialog("¿Está seguro de que desea borrar todo el historial de navegación?", "Confirmar Eliminación");
            confirmDialog.Commands.Add(new UICommand("Sí") { Id = 0 });
            confirmDialog.Commands.Add(new UICommand("No") { Id = 1 });
            confirmDialog.DefaultCommandIndex = 1;
            confirmDialog.CancelCommandIndex = 1;

            var result = await confirmDialog.ShowAsync();

            if ((int)result.Id == 0) // Si el usuario confirma "Sí"
            {
                try
                {
                    await _historyService.ClearHistoryAsync();
                    HistoryEntries.Clear(); // Limpiar la colección en la UI.
                    var dialog = new MessageDialog("El historial de navegación ha sido borrado.", "Historial Borrado");
                    await dialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    var dialog = new MessageDialog($"Error al borrar el historial: {ex.Message}", "Error");
                    await dialog.ShowAsync();
                }
            }
        }

        /// <summary>
        /// Navega al navegador principal y carga la URL de la entrada del historial.
        /// Motivo: Permite al usuario reabrir una página visitada desde la lista del historial.
        /// </summary>
        /// <param name="entry">La entrada del historial a navegar.</param>
        private void NavigateToHistory(HistoryEntry entry)
        {
            if (entry == null) return;

            // Motivo: Navegar de vuelta a la página principal del navegador.
            _navigationService.NavigateTo<MainPage>();

            // Motivo: Acceder al MainViewModel para iniciar una nueva navegación con la URL del historial.
            // Esto es una ligera desviación del MVVM estricto si se accede directamente,
            // pero es práctico aquí. Una alternativa sería un sistema de mensajes global.
            if (Window.Current.Content is Frame rootFrame && rootFrame.Content is MainPage mainPage)
            {
                if (mainPage.DataContext is MainViewModel mainViewModel)
                {
                    mainViewModel.AddNewTab(entry.Url);
                    mainViewModel.SelectedTab.NavigateCommand.Execute(entry.Url);
                }
            }
        }
    }
}
