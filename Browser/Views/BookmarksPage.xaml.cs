// Browser/Views/BookmarksPage.xaml.cs
using Windows.UI.Xaml.Controls;

namespace Browser.Views
{
    /// <summary>
    /// Página para la gestión de marcadores.
    /// Motivo: Esta View es la interfaz de usuario para la página de marcadores.
    /// Su DataContext se establece en el XAML.
    /// </summary>
    public sealed partial class BookmarksPage : Page
    {
        /// <summary>
        /// Constructor de <see cref="BookmarksPage"/>.
        /// Motivo: Inicializa los componentes de la UI.
        /// </summary>
        public BookmarksPage()
        {
            this.InitializeComponent();
        }
    }
}
