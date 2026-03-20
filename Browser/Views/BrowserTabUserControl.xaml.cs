// Browser/Views/BrowserTabUserControl.xaml.cs
using Browser.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Browser.Views
{
    /// <summary>
    /// UserControl para mostrar el contenido de una pestaña del navegador.
    /// Motivo: Esta View encapsula el control WebView y lo asocia a su ViewModel correspondiente.
    /// Se carga en el Frame principal de MainPage cuando se selecciona una pestaña.
    /// </summary>
    public sealed partial class BrowserTabUserControl : UserControl
    {
        /// <summary>
        /// Obtiene la ViewModel asociada a este UserControl.
        /// Motivo: Permite acceder a los datos y la lógica de negocio de la ViewModel desde el code-behind
        /// de la View.
        /// </summary>
        public BrowserTabViewModel ViewModel => DataContext as BrowserTabViewModel;

        /// <summary>
        /// Constructor de <see cref="BrowserTabUserControl"/>.
        /// Motivo: Inicializa los componentes de la UI y gestiona el evento Loaded para asociar
        /// el WebView a su ViewModel.
        /// </summary>
        public BrowserTabUserControl()
        {
            this.InitializeComponent();

            // Motivo: El evento Loaded se utiliza para asegurar que el control WebView ya esté
            // completamente inicializado antes de intentar asociarlo a la ViewModel.
            this.Loaded += BrowserTabUserControl_Loaded;
            this.Unloaded += BrowserTabUserControl_Unloaded;
        }

        /// <summary>
        /// Manejador para el evento Loaded del UserControl.
        /// Motivo: Se invoca una vez que el control se ha cargado completamente en el árbol visual.
        /// Aquí es donde se asocia el control WebView a la ViewModel.
        /// </summary>
        private void BrowserTabUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                // Motivo: Pasar la instancia del WebView al ViewModel para que este pueda controlarlo.
                // Esto mantiene la separación MVVM: la View posee el control UI, pero la ViewModel lo opera.
                ViewModel.SetWebView(WebViewControl);
                // Motivo: Refrescar el estado de la ViewModel para que la UI se actualice
                // con el estado actual del WebView (ej. si puede ir atrás/adelante).
                ViewModel.RefreshState();
            }
        }

        /// <summary>
        /// Manejador para el evento Unloaded del UserControl.
        /// Motivo: Se invoca cuando el control es eliminado del árbol visual.
        /// Es crucial limpiar los recursos aquí para evitar fugas de memoria,
        /// especialmente desasociando el WebView de la ViewModel.
        /// </summary>
        private void BrowserTabUserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                // Motivo: Al desasociar el WebView, se desuscriben todos los eventos
                // y se libera la referencia, permitiendo que el recolector de basura
                // reclame los recursos del WebView.
                ViewModel.SetWebView(null);
            }
        }
    }
}
