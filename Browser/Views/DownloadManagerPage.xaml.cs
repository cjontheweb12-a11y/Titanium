// Browser/Views/DownloadManagerPage.xaml.cs
using Windows.UI.Xaml.Controls;

namespace Browser.Views
{
    /// <summary>
    /// Página para la gestión de descargas.
    /// Motivo: Esta View es la interfaz de usuario para la página del gestor de descargas.
    /// Su DataContext se establece en el XAML.
    /// </summary>
    public sealed partial class DownloadManagerPage : Page
    {
        /// <summary>
        /// Constructor de <see cref="DownloadManagerPage"/>.
        /// Motivo: Inicializa los componentes de la UI.
        /// </summary>
        public DownloadManagerPage()
        {
            this.InitializeComponent();
        }
    }
}
