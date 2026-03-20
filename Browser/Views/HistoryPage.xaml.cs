// Browser/Views/HistoryPage.xaml.cs
using Windows.UI.Xaml.Controls;

namespace Browser.Views
{
    /// <summary>
    /// Página para la visualización del historial de navegación.
    /// Motivo: Esta View es la interfaz de usuario para la página del historial.
    /// Su DataContext se establece en el XAML.
    /// </summary>
    public sealed partial class HistoryPage : Page
    {
        /// <summary>
        /// Constructor de <see cref="HistoryPage"/>.
        /// Motivo: Inicializa los componentes de la UI.
        /// </summary>
        public HistoryPage()
        {
            this.InitializeComponent();
        }
    }
}
