// Browser/ViewModels/MainViewModel.cs
using Browser.Models;
using Browser.Services.Interfaces;
using Browser.ViewModels.Base;
using Browser.Views;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Browser.ViewModels
{
    /// <summary>
    /// ViewModel principal de la aplicación.
    /// Motivo: Actúa como el orquestador central de la aplicación, gestionando la navegación
    /// general, las pestañas, y la interacción con los servicios. Es el ViewModel para MainPage.xaml.
    /// Se adhiere al patrón MVVM.
    /// </summary>
    public class MainViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly ISettingsService _settingsService;
        private readonly IHistoryService _historyService;
        private readonly IDownloadService _downloadService;

        private ObservableCollection<BrowserTabViewModel> _browserTabs;
        /// <summary>
        /// Obtiene la colección de ViewModels de pestañas del navegador.
        /// Motivo: Enlazada al control TabView en MainPage.xaml, esta colección
        /// representa todas las pestañas abiertas. ObservableCollection notifica a la UI
        /// sobre adiciones/eliminaciones de pestañas.
        /// </summary>
        public ObservableCollection<BrowserTabViewModel> BrowserTabs
        {
            get => _browserTabs;
            private set => SetProperty(ref _browserTabs, value);
        }

        private BrowserTabViewModel _selectedTab;
        /// <summary>
        /// Obtiene o establece la ViewModel de la pestaña actualmente seleccionada.
        /// Motivo: Enlazada a la propiedad SelectedItem del TabView. Permite
        /// cambiar la pestaña activa y actualizar la barra de direcciones y otros controles.
        /// </summary>
        public BrowserTabViewModel SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (SetProperty(ref _selectedTab, value))
                {
                    // Motivo: Cuando cambia la pestaña seleccionada, se actualizan las propiedades
                    // que dependen de ella para reflejar el estado de la nueva pestaña (ej. dirección, título).
                    if (_selectedTab != null)
                    {
                        _selectedTab.RefreshState(); // Asegurar que la nueva pestaña seleccionada actualice su estado
                    }
                    NavigateToSelectedTabContent();
                    UpdateNavigationCommands(); // Actualiza el estado de los botones de navegación.
                }
            }
        }

        private bool _isBackEnabled;
        /// <summary>
        /// Obtiene un valor que indica si el botón "Atrás" debe estar habilitado.
        /// Motivo: Enlazado al botón "Atrás" en la barra de navegación.
        /// </summary>
        public bool IsBackEnabled
        {
            get => _isBackEnabled;
            private set => SetProperty(ref _isBackEnabled, value);
        }

        private bool _isForwardEnabled;
        /// <summary>
        /// Obtiene un valor que indica si el botón "Adelante" debe estar habilitado.
        /// Motivo: Enlazado al botón "Adelante" en la barra de navegación.
        /// </summary>
        public bool IsForwardEnabled
        {
            get => _isForwardEnabled;
            private set => SetProperty(ref _isForwardEnabled, value);
        }

        private bool _isLoading;
        /// <summary>
        /// Obtiene un valor que indica si la pestaña actual está cargando contenido.
        /// Motivo: Enlazado a la visibilidad de la barra de progreso y el icono de refrescar/detener.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        private bool _isHttps;
        /// <summary>
        /// Obtiene un valor que indica si la página actual utiliza HTTPS.
        /// Motivo: Enlazado a la visibilidad del icono de candado de seguridad.
        /// </summary>
        public bool IsHttps
        {
            get => _isHttps;
            private set => SetProperty(ref _isHttps, value);
        }

        /// <summary>
        /// Comando para añadir una nueva pestaña.
        /// Motivo: Enlazado al botón de "añadir pestaña" en el TabView.
        /// </summary>
        public RelayCommand AddNewTabCommand { get; }

        /// <summary>
        /// Comando para cerrar una pestaña.
        /// Motivo: Enlazado al botón de "cerrar pestaña" dentro de cada TabViewItem.
        /// </summary>
        public RelayCommand<BrowserTabViewModel> CloseTabCommand { get; }

        /// <summary>
        /// Comando para navegar hacia atrás en la pestaña actual.
        /// Motivo: Enlazado al botón "Atrás".
        /// </summary>
        public RelayCommand GoBackCommand { get; }

        /// <summary>
        /// Comando para navegar hacia adelante en la pestaña actual.
        /// Motivo: Enlazado al botón "Adelante".
        /// </summary>
        public RelayCommand GoForwardCommand { get; }

        /// <summary>
        /// Comando para refrescar la página actual.
        /// Motivo: Enlazado al botón "Refrescar".
        /// </summary>
        public RelayCommand RefreshCommand { get; }

        /// <summary>
        /// Comando para detener la carga de la página actual.
        /// Motivo: Enlazado al botón "Detener".
        /// </summary>
        public RelayCommand StopCommand { get; }

        /// <summary>
        /// Comando para navegar a una URL específica desde la barra de direcciones.
        /// Motivo: Enlazado al evento de envío de texto de la barra de direcciones (ej. tecla Enter).
        /// </summary>
        public RelayCommand<string> NavigateCommand { get; }

        /// <summary>
        /// Comando para navegar a la página de inicio.
        /// Motivo: Enlazado al botón "Inicio".
        /// </summary>
        public RelayCommand GoHomeCommand { get; }

        /// <summary>
        /// Comando para añadir la página actual a marcadores.
        /// Motivo: Enlazado al botón de "añadir marcador".
        /// </summary>
        public RelayCommand AddBookmarkCommand { get; }

        /// <summary>
        /// Comando para navegar a una página de la aplicación (ej. Configuración, Marcadores).
        /// Motivo: Enlazado a los elementos del NavigationView.
        /// </summary>
        public RelayCommand<string> NavigateAppPageCommand { get; }

        /// <summary>
        /// Comando para manejar la selección de un elemento del NavigationView.
        /// Motivo: Controla a qué página interna de la aplicación se navega.
        /// </summary>
        public RelayCommand<NavigationViewSelectionChangedEventArgs> NavigationViewSelectionChangedCommand { get; }

        /// <summary>
        /// Constructor de <see cref="MainViewModel"/>.
        /// Motivo: Inicializa la ViewModel con las dependencias de servicio necesarias
        /// y configura los comandos. Sigue el principio de Inversión de Control.
        /// </summary>
        /// <param name="navigationService">Servicio de navegación.</param>
        /// <param name="settingsService">Servicio de configuración de la aplicación.</param>
        /// <param name="historyService">Servicio de historial de navegación.</param>
        /// <param name="downloadService">Servicio de descargas.</param>
        public MainViewModel(
            INavigationService navigationService,
            ISettingsService settingsService,
            IHistoryService historyService,
            IDownloadService downloadService)
        {
            _navigationService = navigationService;
            _settingsService = settingsService;
            _historyService = historyService;
            _downloadService = downloadService;

            BrowserTabs = new ObservableCollection<BrowserTabViewModel>();

            // Motivo: Se asignan los comandos a sus respectivos métodos.
            // Los comandos encapsulan la lógica de la acción y la capacidad de ejecución.
            AddNewTabCommand = new RelayCommand(AddNewTab);
            CloseTabCommand = new RelayCommand<BrowserTabViewModel>(CloseTab);
            GoBackCommand = new RelayCommand(GoBack, () => IsBackEnabled);
            GoForwardCommand = new RelayCommand(GoForward, () => IsForwardEnabled);
            RefreshCommand = new RelayCommand(Refresh, () => SelectedTab != null && !IsLoading);
            StopCommand = new RelayCommand(Stop, () => SelectedTab != null && IsLoading);
            NavigateCommand = new RelayCommand<string>(NavigateToUrl);
            GoHomeCommand = new RelayCommand(GoHome);
            AddBookmarkCommand = new RelayCommand(AddBookmark, () => SelectedTab != null && !string.IsNullOrEmpty(SelectedTab.CurrentUri));
            NavigateAppPageCommand = new RelayCommand<string>(NavigateToAppPage);
            NavigationViewSelectionChangedCommand = new RelayCommand<NavigationViewSelectionChangedEventArgs>(OnNavigationViewSelectionChanged);

            // Motivo: Restaurar la sesión o añadir la primera pestaña al inicio.
            // Se realiza de forma asíncrona para no bloquear el hilo de UI.
            _ = InitializeAsync();
        }

        /// <summary>
        /// Inicializa asincrónicamente el ViewModel principal.
        /// Motivo: Realiza tareas de inicialización que requieren operaciones asíncronas,
        /// como cargar configuraciones y restaurar la sesión.
        /// </summary>
        private async Task InitializeAsync()
        {
            await _settingsService.LoadSettingsAsync(); // Asegurarse de que las configuraciones estén cargadas.
            await _downloadService.DiscoverExistingDownloadsAsync(); // Cargar descargas pendientes.

            // Motivo: Determinar el comportamiento de inicio basado en la configuración del usuario.
            switch (_settingsService.CurrentSettings.StartupOption)
            {
                case StartupOption.HomePage:
                    AddNewTab(_settingsService.CurrentSettings.HomePageUrl);
                    break;
                case StartupOption.LastSession:
                    if (_settingsService.CurrentSettings.LastSessionTabUrls != null && _settingsService.CurrentSettings.LastSessionTabUrls.Any())
                    {
                        foreach (var url in _settingsService.CurrentSettings.LastSessionTabUrls)
                        {
                            AddNewTab(url);
                        }
                    }
                    else
                    {
                        AddNewTab(_settingsService.CurrentSettings.HomePageUrl);
                    }
                    break;
                case StartupOption.BlankPage:
                default:
                    AddNewTab("about:blank"); // Página en blanco para un inicio limpio.
                    break;
            }

            // Asegurarse de que al menos una pestaña esté abierta.
            if (!BrowserTabs.Any())
            {
                AddNewTab(_settingsService.CurrentSettings.HomePageUrl);
            }
            SelectedTab = BrowserTabs.FirstOrDefault();
        }

        /// <summary>
        /// Manejador para el evento de cambio de selección del NavigationView.
        /// Motivo: Controla la navegación a las diferentes secciones de la aplicación
        /// (ej. Marcadores, Historial, Configuración) dentro del Frame principal.
        /// </summary>
        /// <param name="args">Argumentos del evento.</param>
        private void OnNavigationViewSelectionChanged(NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                _navigationService.NavigateTo<SettingsPage>();
            }
            else if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                switch (selectedItem.Tag?.ToString())
                {
                    case "Bookmarks":
                        _navigationService.NavigateTo<BookmarksPage>();
                        break;
                    case "History":
                        _navigationService.NavigateTo<HistoryPage>();
                        break;
                    case "Downloads":
                        _navigationService.NavigateTo<DownloadManagerPage>();
                        break;
                    case "Home":
                        GoHome();
                        break;
                    default:
                        // Motivo: Si se selecciona un elemento que no es de navegación especial,
                        // y no es la página de inicio, se asume que se refiere al contenido del navegador.
                        // Volver a la página principal del navegador si se selecciona un elemento desconocido.
                        if (_navigationService.CurrentPageType != typeof(MainPage))
                        {
                            _navigationService.GoBack(); // Navegar de vuelta a MainPage si estamos en otra página.
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Navega a una página interna de la aplicación.
        /// Motivo: Un método de utilidad para el comando NavigateAppPageCommand.
        /// </summary>
        /// <param name="pageName">El nombre de la página a la que navegar.</param>
        private void NavigateToAppPage(string pageName)
        {
            switch (pageName)
            {
                case "Settings":
                    _navigationService.NavigateTo<SettingsPage>();
                    break;
                case "Bookmarks":
                    _navigationService.NavigateTo<BookmarksPage>();
                    break;
                case "History":
                    _navigationService.NavigateTo<HistoryPage>();
                    break;
                case "Downloads":
                    _navigationService.NavigateTo<DownloadManagerPage>();
                    break;
                case "Home":
                    GoHome();
                    break;
            }
        }

        /// <summary>
        /// Añade una nueva pestaña al navegador.
        /// Motivo: Crea una nueva instancia de BrowserTabViewModel y la añade a la colección.
        /// Si se proporciona una URL, la pestaña navega a esa URL.
        /// </summary>
        /// <param name="url">La URL inicial para la nueva pestaña. Opcional.</param>
        public void AddNewTab(string url = null)
        {
            // Motivo: Si no se proporciona una URL, usar la página de inicio configurada.
            var startUrl = url ?? _settingsService.CurrentSettings.HomePageUrl;

            // Motivo: Crear una nueva instancia de BrowserTabViewModel con sus dependencias.
            // Cada pestaña tiene su propia ViewModel, garantizando el aislamiento del estado.
            var newTabViewModel = new BrowserTabViewModel(_settingsService, _historyService, _downloadService);
            newTabViewModel.PropertyChanged += (s, e) =>
            {
                // Motivo: Suscribirse a los cambios de propiedad de las pestañas individuales
                // para actualizar el estado de los controles de navegación globales en el MainViewModel.
                if (SelectedTab == s) // Solo reaccionar a la pestaña seleccionada
                {
                    if (e.PropertyName == nameof(BrowserTabViewModel.CanGoBack) ||
                        e.PropertyName == nameof(BrowserTabViewModel.CanGoForward) ||
                        e.PropertyName == nameof(BrowserTabViewModel.IsLoading) ||
                        e.PropertyName == nameof(BrowserTabViewModel.IsHttps))
                    {
                        UpdateNavigationCommands();
                    }
                }
            };

            BrowserTabs.Add(newTabViewModel);
            SelectedTab = newTabViewModel; // Seleccionar la nueva pestaña automáticamente.

            // Motivo: Navegar a la URL inicial. Esto se hace después de que la pestaña se añade y selecciona
            // para que los eventos de WebView puedan actualizar correctamente el estado.
            newTabViewModel.NavigateCommand.Execute(startUrl);

            // Motivo: Guardar el estado de las pestañas para la funcionalidad "última sesión".
            UpdateLastSessionTabUrls();
        }

        /// <summary>
        /// Cierra una pestaña específica del navegador.
        /// Motivo: Elimina la ViewModel de la pestaña de la colección.
        /// </summary>
        /// <param name="tab">La ViewModel de la pestaña a cerrar.</param>
        public void CloseTab(BrowserTabViewModel tab)
        {
            if (tab == null) return;

            // Motivo: Desuscribirse de los eventos de la pestaña para evitar fugas de memoria.
            tab.Dispose();
            BrowserTabs.Remove(tab);

            // Motivo: Si no quedan pestañas, añadir una nueva por defecto.
            if (!BrowserTabs.Any())
            {
                AddNewTab();
            }
            // Motivo: Seleccionar la pestaña adyacente o la primera disponible después de cerrar una.
            else if (SelectedTab == tab)
            {
                SelectedTab = BrowserTabs.FirstOrDefault();
            }

            // Motivo: Guardar el estado de las pestañas para la funcionalidad "última sesión".
            UpdateLastSessionTabUrls();
        }

        /// <summary>
        /// Navega la pestaña seleccionada hacia atrás.
        /// Motivo: Delega la acción al comando de navegación de la pestaña actual.
        /// </summary>
        private void GoBack()
        {
            SelectedTab?.GoBackCommand.Execute(null);
        }

        /// <summary>
        /// Navega la pestaña seleccionada hacia adelante.
        /// Motivo: Delega la acción al comando de navegación de la pestaña actual.
        /// </summary>
        private void GoForward()
        {
            SelectedTab?.GoForwardCommand.Execute(null);
        }

        /// <summary>
        /// Refresca la página en la pestaña seleccionada.
        /// Motivo: Delega la acción al comando de navegación de la pestaña actual.
        /// </summary>
        private void Refresh()
        {
            SelectedTab?.RefreshCommand.Execute(null);
        }

        /// <summary>
        /// Detiene la carga de la página en la pestaña seleccionada.
        /// Motivo: Delega la acción al comando de navegación de la pestaña actual.
        /// </summary>
        private void Stop()
        {
            SelectedTab?.StopCommand.Execute(null);
        }

        /// <summary>
        /// Navega la pestaña seleccionada a la URL proporcionada.
        /// Motivo: Valida la URL y la formatea antes de delegar al comando de navegación de la pestaña.
        /// </summary>
        /// <param name="url">La URL o término de búsqueda a navegar.</param>
        private void NavigateToUrl(string url)
        {
            if (SelectedTab == null) return;

            // Motivo: Normalizar la URL para asegurar que sea un formato válido.
            // Si es un término de búsqueda, se formatea con el motor de búsqueda predeterminado.
            string formattedUrl = UrlHelper.FormatUrl(url,
                _settingsService.CurrentSettings.DefaultSearchEngine,
                _settingsService.CurrentSettings.CustomSearchEngineUrl);

            SelectedTab.NavigateCommand.Execute(formattedUrl);
        }

        /// <summary>
        /// Navega la pestaña seleccionada a la página de inicio configurada.
        /// Motivo: Utiliza la configuración del usuario para la página de inicio.
        /// </summary>
        private void GoHome()
        {
            if (SelectedTab == null) return;
            SelectedTab.NavigateCommand.Execute(_settingsService.CurrentSettings.HomePageUrl);
        }

        /// <summary>
        /// Añade la página actual de la pestaña seleccionada a los marcadores.
        /// Motivo: Crea un objeto Bookmark y utiliza el servicio de marcadores para guardarlo.
        /// Muestra una notificación al usuario.
        /// </summary>
        private async void AddBookmark() // async void para evitar advertencias en event handler
        {
            if (SelectedTab == null || string.IsNullOrWhiteSpace(SelectedTab.CurrentUri)) return;

            var newBookmark = new Bookmark
            {
                Title = SelectedTab.Title,
                Url = SelectedTab.CurrentUri
            };

            try
            {
                await _bookmarkService.AddBookmarkAsync(newBookmark);
                var dialog = new MessageDialog($"'${newBookmark.Title}' añadido a marcadores.", "Marcador Añadido");
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                // Motivo: Manejar errores de persistencia de marcadores.
                // Mostrar un mensaje de error amigable al usuario.
                var dialog = new MessageDialog($"Error al añadir marcador: {ex.Message}", "Error");
                await dialog.ShowAsync();
            }
        }

        /// <summary>
        /// Actualiza el estado de los comandos de navegación (Atrás, Adelante, Recargar, Detener).
        /// Motivo: Asegura que los botones de la UI reflejen correctamente la capacidad
        /// de la pestaña actualmente seleccionada para realizar estas acciones.
        /// </summary>
        private void UpdateNavigationCommands()
        {
            if (SelectedTab != null)
            {
                IsBackEnabled = SelectedTab.CanGoBack;
                IsForwardEnabled = SelectedTab.CanGoForward;
                IsLoading = SelectedTab.IsLoading;
                IsHttps = SelectedTab.IsHttps;
            }
            else
            {
                // Motivo: Deshabilitar todos los controles si no hay ninguna pestaña seleccionada.
                IsBackEnabled = false;
                IsForwardEnabled = false;
                IsLoading = false;
                IsHttps = false;
            }
            // Motivo: Notificar a los comandos que sus estados de CanExecute podrían haber cambiado.
            GoBackCommand.RaiseCanExecuteChanged();
            GoForwardCommand.RaiseCanExecuteChanged();
            RefreshCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
            AddBookmarkCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Navega el Frame principal de la aplicación para mostrar el contenido de la pestaña seleccionada.
        /// Motivo: Las pestañas del TabView no contienen directamente el WebView. En su lugar, el
        /// Frame principal se usa para mostrar el `BrowserTabUserControl` de la pestaña seleccionada.
        /// Esto permite reutilizar el Frame y gestionar el ciclo de vida de la UI de la pestaña.
        /// </summary>
        private void NavigateToSelectedTabContent()
        {
            if (SelectedTab != null)
            {
                // Motivo: Pasar la ViewModel de la pestaña seleccionada al BrowserTabUserControl.
                // Esto permite que el UserControl se enlace a los datos de la pestaña.
                _navigationService.NavigateTo<BrowserTabUserControl>(SelectedTab);
            }
            else
            {
                // Motivo: Si no hay pestañas seleccionadas, navegar a una página en blanco o un mensaje.
                _navigationService.NavigateTo<BlankPage>(); // Opcional: Crear una BlankPage para cuando no hay pestañas.
            }
        }

        /// <summary>
        /// Guarda las URLs de todas las pestañas abiertas para la funcionalidad de "última sesión".
        /// Motivo: Se llama cada vez que una pestaña se abre o cierra para mantener actualizado el estado
        /// de la sesión.
        /// </summary>
        private async void UpdateLastSessionTabUrls() // async void porque se llama desde property setter
        {
            _settingsService.CurrentSettings.LastSessionTabUrls.Clear();
            foreach (var tab in BrowserTabs)
            {
                if (!string.IsNullOrWhiteSpace(tab.CurrentUri) && tab.CurrentUri != "about:blank")
                {
                    _settingsService.CurrentSettings.LastSessionTabUrls.Add(tab.CurrentUri);
                }
            }
            await _settingsService.SaveSettingsAsync();
        }

        /// <summary>
        /// Método invocado cuando la aplicación se está cerrando.
        /// Motivo: Asegurarse de que el estado de las pestañas se guarde antes de la suspensión.
        /// </summary>
        public async Task OnAppClosing()
        {
            UpdateLastSessionTabUrls();
            await _settingsService.SaveSettingsAsync();
        }
    }

    /// <summary>
    /// Una página en blanco simple para usar cuando no hay pestañas.
    /// Motivo: Una alternativa a un control visual complejo cuando el TabView no tiene elementos seleccionados.
    /// </summary>
    public sealed partial class BlankPage : Page
    {
        public BlankPage()
        {
            this.InitializeComponent();
        }
    }
}
