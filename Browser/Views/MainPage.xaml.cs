// Browser/Views/MainPage.xaml.cs
using Browser.Helpers;
using Browser.ViewModels;
using System;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Browser.Views
{
    /// <summary>
    /// Página principal del navegador.
    /// Motivo: Esta View es la interfaz de usuario principal de la aplicación.
    /// Se encarga de la interacción del usuario con los controles (botones, barra de dirección, pestañas)
    /// y enlaza estos eventos a los comandos y propiedades de la MainViewModel.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// Obtiene la ViewModel asociada a esta página.
        /// Motivo: Permite acceder a los datos y la lógica de negocio de la ViewModel desde el code-behind
        /// de la View, especialmente para eventos que no se pueden enlazar directamente (ej. QuerySubmitted de AutoSuggestBox).
        /// </summary>
        public MainViewModel ViewModel => DataContext as MainViewModel;

        /// <summary>
        /// Constructor de <see cref="MainPage"/>.
        /// Motivo: Inicializa los componentes de la UI y establece el contexto de datos.
        /// También registra el Frame de navegación principal con el NavigationService.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            // Motivo: Establecer el DataContext aquí asegura que la ViewModel esté disponible
            // para el enlace de datos desde el principio.
            this.DataContext = ServiceLocator.Resolve<MainViewModel>();

            // Motivo: El NavigationService necesita una referencia al Frame principal para controlar la navegación.
            // Se registra aquí porque el Frame `ContentFrame` ya existe en la UI de MainPage.
            ServiceLocator.Resolve<Services.Interfaces.INavigationService>().SetNavigationFrame(ContentFrame);

            // Motivo: Suscribirse al evento de cierre de la ventana para guardar el estado.
            // Esto es importante para la funcionalidad de "restaurar última sesión".
            Window.Current.Activated += OnWindowActivated;
        }

        /// <summary>
        /// Manejador para el evento Activated de la ventana.
        /// Motivo: Necesario para asegurar que el WebView pueda tomar foco de forma programática.
        /// </summary>
        private void OnWindowActivated(object sender, WindowActivatedEventArgs e)
        {
            // Puedes agregar lógica aquí si necesitas reaccionar a la activación de la ventana.
        }

        /// <summary>
        /// Se invoca cuando esta página está a punto de mostrarse en un Frame.
        /// Motivo: Se usa para realizar configuraciones o actualizaciones cuando la página se vuelve activa.
        /// </summary>
        /// <param name="e">Datos del evento que describen cómo se llegó a esta página.
        /// El parámetro de navegación se utiliza normalmente para configurar la página.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Motivo: Cuando se navega a MainPage, se asegura de que el Frame de contenido
            // muestre la pestaña seleccionada actualmente en el ViewModel.
            ViewModel.NavigateToSelectedTabContent();

            // Motivo: Restablecer el NavigationView a la pestaña de "Inicio" cuando se vuelve a MainPage.
            // Esto es una convención común para mantener la UI consistente.
            foreach (var item in NavigationViewControl.MenuItems)
            {
                if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == "Home")
                {
                    NavigationViewControl.SelectedItem = navItem;
                    break;
                }
            }
        }


        /// <summary>
        /// Manejador para el evento QuerySubmitted del AutoSuggestBox de la barra de direcciones.
        /// Motivo: Cuando el usuario presiona Enter o selecciona una sugerencia, este evento se dispara.
        /// Se utiliza para invocar el comando de navegación en la ViewModel.
        /// </summary>
        private void AddressBar_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            // Motivo: Si se proporciona una sugerencia, usarla; de lo contrario, usar el texto del cuadro.
            string urlToNavigate = args.ChosenSuggestion != null ? args.ChosenSuggestion.ToString() : args.QueryText;
            ViewModel?.NavigateCommand.Execute(urlToNavigate);
        }

        /// <summary>
        /// Manejador para el evento Loaded del AutoSuggestBox de la barra de direcciones.
        /// Motivo: Se asegura de que el control pueda manejar el evento KeyDown para la tecla Enter
        /// si no se maneja directamente en QuerySubmitted por alguna razón, o para fines de prueba.
        /// </summary>
        private void AddressBar_Loaded(object sender, RoutedEventArgs e)
        {
            // Motivo: Aquí podemos añadir lógica de inicialización específica para el AutoSuggestBox.
            // Por ejemplo, para auto-completado, pero esto está fuera del alcance de este ejercicio.
        }

        /// <summary>
        /// Establece el modo de pantalla completa para la interfaz de usuario de MainPage.
        /// Motivo: Cuando un WebView entra en modo de pantalla completa, es deseable ocultar
        /// las barras de navegación y pestañas de la aplicación para maximizar el área de contenido.
        /// </summary>
        /// <param name="isFullScreen">True para modo de pantalla completa, false para modo normal.</param>
        public void SetFullScreenMode(bool isFullScreen)
        {
            // Motivo: Ocultar o mostrar la barra de navegación y las pestañas.
            // Esto mejora la experiencia del usuario al ver videos o contenido inmersivo.
            if (isFullScreen)
            {
                NavigationViewControl.IsPaneVisible = false;
                NavigationViewControl.DisplayMode = NavigationViewDisplayMode.Compact; // Cambiar a modo compacto si no visible
                Grid.SetRowSpan(BrowserTabView, 1);
                BrowserTabView.Visibility = Visibility.Collapsed;
                Grid.SetRow(ContentFrame, 0); // Ocupar toda la altura
                Grid.SetRowSpan(ContentFrame, 3);
            }
            else
            {
                NavigationViewControl.IsPaneVisible = true;
                NavigationViewControl.DisplayMode = NavigationViewDisplayMode.Left; // Volver al modo normal
                BrowserTabView.Visibility = Visibility.Visible;
                Grid.SetRow(BrowserTabView, 1);
                Grid.SetRowSpan(BrowserTabView, 1);
                Grid.SetRow(ContentFrame, 2);
                Grid.SetRowSpan(ContentFrame, 1);
            }
        }
    }
}
