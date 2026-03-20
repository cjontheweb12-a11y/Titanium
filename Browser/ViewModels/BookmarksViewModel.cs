// Browser/ViewModels/BookmarksViewModel.cs
using Browser.Models;
using Browser.Services.Interfaces;
using Browser.ViewModels.Base;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace Browser.ViewModels
{
    /// <summary>
    /// ViewModel para la página de gestión de marcadores (BookmarksPage.xaml).
    /// Motivo: Proporciona la lógica para cargar, añadir, editar y eliminar marcadores,
    /// y gestiona la interfaz de usuario para mostrar estos marcadores.
    /// Adhiere al patrón MVVM.
    /// </summary>
    public class BookmarksViewModel : ObservableObject
    {
        private readonly IBookmarkService _bookmarkService;
        private readonly INavigationService _navigationService;

        private ObservableCollection<Bookmark> _bookmarks;
        /// <summary>
        /// Obtiene la colección de marcadores.
        /// Motivo: Enlazada a la lista de marcadores en la UI.
        /// ObservableCollection notifica a la UI sobre adiciones/eliminaciones.
        /// </summary>
        public ObservableCollection<Bookmark> Bookmarks
        {
            get => _bookmarks;
            set => SetProperty(ref _bookmarks, value);
        }

        private Bookmark _selectedBookmark;
        /// <summary>
        /// Obtiene o establece el marcador actualmente seleccionado en la UI.
        /// Motivo: Permite editar o eliminar el marcador seleccionado.
        /// </summary>
        public Bookmark SelectedBookmark
        {
            get => _selectedBookmark;
            set
            {
                if (SetProperty(ref _selectedBookmark, value))
                {
                    // Motivo: Cuando se selecciona un marcador, sus detalles se cargan para edición.
                    if (value != null)
                    {
                        EditingTitle = value.Title;
                        EditingUrl = value.Url;
                    }
                    else
                    {
                        EditingTitle = string.Empty;
                        EditingUrl = string.Empty;
                    }
                    // Motivo: Actualizar la capacidad de ejecución de los comandos de edición/eliminación.
                    EditBookmarkCommand.RaiseCanExecuteChanged();
                    DeleteBookmarkCommand.RaiseCanExecuteChanged();
                    NavigateToBookmarkCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _newBookmarkTitle;
        /// <summary>
        /// Obtiene o establece el título para un nuevo marcador.
        /// Motivo: Enlazado al TextBox de "añadir nuevo marcador".
        /// </summary>
        public string NewBookmarkTitle
        {
            get => _newBookmarkTitle;
            set => SetProperty(ref _newBookmarkTitle, value);
        }

        private string _newBookmarkUrl;
        /// <summary>
        /// Obtiene o establece la URL para un nuevo marcador.
        /// Motivo: Enlazado al TextBox de "añadir nuevo marcador".
        /// </summary>
        public string NewBookmarkUrl
        {
            get => _newBookmarkUrl;
            set => SetProperty(ref _newBookmarkUrl, value);
        }

        private string _editingTitle;
        /// <summary>
        /// Obtiene o establece el título del marcador que se está editando.
        /// Motivo: Enlazado al TextBox de edición.
        /// </summary>
        public string EditingTitle
        {
            get => _editingTitle;
            set
            {
                if (SetProperty(ref _editingTitle, value))
                {
                    EditBookmarkCommand.RaiseCanExecuteChanged(); // Reevaluar si el comando de edición puede ejecutarse.
                }
            }
        }

        private string _editingUrl;
        /// <summary>
        /// Obtiene o establece la URL del marcador que se está editando.
        /// Motivo: Enlazado al TextBox de edición.
        /// </summary>
        public string EditingUrl
        {
            get => _editingUrl;
            set
            {
                if (SetProperty(ref _editingUrl, value))
                {
                    EditBookmarkCommand.RaiseCanExecuteChanged(); // Reevaluar si el comando de edición puede ejecutarse.
                }
            }
        }

        /// <summary>
        /// Comando para cargar todos los marcadores.
        /// Motivo: Se llama al cargar la página de marcadores.
        /// </summary>
        public RelayCommand LoadBookmarksCommand { get; }

        /// <summary>
        /// Comando para añadir un nuevo marcador.
        /// Motivo: Enlazado al botón "Añadir Marcador".
        /// </summary>
        public RelayCommand AddBookmarkCommand { get; }

        /// <summary>
        /// Comando para editar el marcador seleccionado.
        /// Motivo: Enlazado al botón "Guardar Cambios" de edición.
        /// </summary>
        public RelayCommand EditBookmarkCommand { get; }

        /// <summary>
        /// Comando para eliminar el marcador seleccionado.
        /// Motivo: Enlazado al botón "Eliminar Marcador".
        /// </summary>
        public RelayCommand DeleteBookmarkCommand { get; }

        /// <summary>
        /// Comando para navegar a un marcador específico.
        /// Motivo: Se activa al hacer clic en un marcador en la lista.
        /// </summary>
        public RelayCommand<Bookmark> NavigateToBookmarkCommand { get; }


        /// <summary>
        /// Constructor de <see cref="BookmarksViewModel"/>.
        /// Motivo: Inicializa la ViewModel con las dependencias de servicio necesarias
        /// y configura los comandos.
        /// </summary>
        /// <param name="bookmarkService">Servicio de gestión de marcadores.</param>
        /// <param name="navigationService">Servicio de navegación.</param>
        public BookmarksViewModel(IBookmarkService bookmarkService, INavigationService navigationService)
        {
            _bookmarkService = bookmarkService;
            _navigationService = navigationService;
            Bookmarks = new ObservableCollection<Bookmark>();

            // Motivo: Inicializar comandos, pasando los delegados para Execute y CanExecute.
            LoadBookmarksCommand = new RelayCommand(async () => await LoadBookmarksAsync());
            AddBookmarkCommand = new RelayCommand(async () => await AddBookmarkAsync(), () => !string.IsNullOrWhiteSpace(NewBookmarkTitle) && !string.IsNullOrWhiteSpace(NewBookmarkUrl) && Uri.IsWellFormedUriString(UrlHelper.EnsureHttps(NewBookmarkUrl), UriKind.Absolute));
            EditBookmarkCommand = new RelayCommand(async () => await EditBookmarkAsync(), () => SelectedBookmark != null && !string.IsNullOrWhiteSpace(EditingTitle) && !string.IsNullOrWhiteSpace(EditingUrl) && Uri.IsWellFormedUriString(UrlHelper.EnsureHttps(EditingUrl), UriKind.Absolute));
            DeleteBookmarkCommand = new RelayCommand(async () => await DeleteBookmarkAsync(), () => SelectedBookmark != null);
            NavigateToBookmarkCommand = new RelayCommand<Bookmark>(NavigateToBookmark, (bookmark) => bookmark != null);

            // Motivo: Cargar marcadores al construir la ViewModel.
            _ = LoadBookmarksAsync();
        }

        /// <summary>
        /// Carga todos los marcadores del servicio y actualiza la colección.
        /// Motivo: Método asíncrono para no bloquear la UI durante la carga de datos.
        /// </summary>
        private async Task LoadBookmarksAsync()
        {
            try
            {
                var bookmarks = await _bookmarkService.GetBookmarksAsync();
                Bookmarks.Clear(); // Limpiar la colección existente.
                foreach (var bookmark in bookmarks)
                {
                    Bookmarks.Add(bookmark); // Añadir los marcadores cargados.
                }
            }
            catch (Exception ex)
            {
                // Motivo: Manejar errores de carga de marcadores.
                // Mostrar un mensaje de error amigable al usuario.
                var dialog = new MessageDialog($"Error al cargar marcadores: {ex.Message}", "Error de Carga");
                await dialog.ShowAsync();
            }
        }

        /// <summary>
        /// Añade un nuevo marcador a la colección y lo persiste.
        /// Motivo: Método asíncrono para operaciones de persistencia.
        /// </summary>
        private async Task AddBookmarkAsync()
        {
            if (!Uri.IsWellFormedUriString(UrlHelper.EnsureHttps(NewBookmarkUrl), UriKind.Absolute))
            {
                var dialog = new MessageDialog("La URL del nuevo marcador no es válida.", "Error de URL");
                await dialog.ShowAsync();
                return;
            }

            var newBookmark = new Bookmark
            {
                Title = NewBookmarkTitle,
                Url = UrlHelper.EnsureHttps(NewBookmarkUrl), // Asegurarse de que la URL tenga un esquema.
                AddedDate = DateTime.Now
            };

            try
            {
                await _bookmarkService.AddBookmarkAsync(newBookmark);
                Bookmarks.Insert(0, newBookmark); // Añadir al principio para que sea visible rápidamente.
                NewBookmarkTitle = string.Empty; // Limpiar campos después de añadir.
                NewBookmarkUrl = string.Empty;
                AddBookmarkCommand.RaiseCanExecuteChanged(); // Reevaluar la capacidad de añadir.
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog($"Error al añadir marcador: {ex.Message}", "Error");
                await dialog.ShowAsync();
            }
        }

        /// <summary>
        /// Edita el marcador seleccionado y lo persiste.
        /// Motivo: Permite al usuario corregir el título o la URL de un marcador existente.
        /// </summary>
        private async Task EditBookmarkAsync()
        {
            if (SelectedBookmark == null) return;
            if (!Uri.IsWellFormedUriString(UrlHelper.EnsureHttps(EditingUrl), UriKind.Absolute))
            {
                var dialog = new MessageDialog("La URL del marcador editado no es válida.", "Error de URL");
                await dialog.ShowAsync();
                return;
            }

            var updatedBookmark = new Bookmark
            {
                Id = SelectedBookmark.Id,
                Title = EditingTitle,
                Url = UrlHelper.EnsureHttps(EditingUrl),
                AddedDate = SelectedBookmark.AddedDate // Mantener la fecha original.
            };

            try
            {
                await _bookmarkService.UpdateBookmarkAsync(updatedBookmark);
                // Motivo: Actualizar el elemento en la colección observable.
                var index = Bookmarks.IndexOf(SelectedBookmark);
                if (index != -1)
                {
                    Bookmarks[index] = updatedBookmark; // Reemplazar con la versión actualizada.
                    // Nota: Si Bookmark fuera un ObservableObject, solo necesitaríamos actualizar las propiedades.
                    // Para un objeto simple, reasignar el elemento en la colección ObservableCollection es una forma sencilla de actualizar la UI.
                }
                SelectedBookmark = null; // Deseleccionar después de la edición.
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog($"Error al actualizar marcador: {ex.Message}", "Error");
                await dialog.ShowAsync();
            }
        }

        /// <summary>
        /// Elimina el marcador seleccionado.
        /// Motivo: Permite al usuario borrar marcadores no deseados.
        /// </summary>
        private async Task DeleteBookmarkAsync()
        {
            if (SelectedBookmark == null) return;

            // Motivo: Confirmar la eliminación para evitar borrados accidentales.
            var confirmDialog = new MessageDialog($"¿Está seguro de que desea eliminar el marcador '{SelectedBookmark.Title}'?", "Confirmar Eliminación");
            confirmDialog.Commands.Add(new UICommand("Sí") { Id = 0 });
            confirmDialog.Commands.Add(new UICommand("No") { Id = 1 });
            confirmDialog.DefaultCommandIndex = 1;
            confirmDialog.CancelCommandIndex = 1;

            var result = await confirmDialog.ShowAsync();

            if ((int)result.Id == 0) // Si el usuario confirma "Sí"
            {
                try
                {
                    await _bookmarkService.DeleteBookmarkAsync(SelectedBookmark.Id);
                    Bookmarks.Remove(SelectedBookmark);
                    SelectedBookmark = null; // Deseleccionar después de eliminar.
                }
                catch (Exception ex)
                {
                    var dialog = new MessageDialog($"Error al eliminar marcador: {ex.Message}", "Error");
                    await dialog.ShowAsync();
                }
            }
        }

        /// <summary>
        /// Navega al navegador principal y carga la URL del marcador.
        /// Motivo: Permite al usuario abrir un marcador directamente desde la lista de marcadores.
        /// </summary>
        /// <param name="bookmark">El marcador a navegar.</param>
        private void NavigateToBookmark(Bookmark bookmark)
        {
            if (bookmark == null) return;

            // Motivo: Navegar de vuelta a la página principal del navegador.
            _navigationService.NavigateTo<MainPage>();

            // Motivo: Acceder al MainViewModel para iniciar una nueva navegación con la URL del marcador.
            // Esto es una ligera desviación del MVVM estricto si se accede directamente,
            // pero es práctico aquí. Una alternativa sería un sistema de mensajes global.
            if (Window.Current.Content is Frame rootFrame && rootFrame.Content is MainPage mainPage)
            {
                if (mainPage.DataContext is MainViewModel mainViewModel)
                {
                    mainViewModel.AddNewTab(bookmark.Url);
                    mainViewModel.SelectedTab.NavigateCommand.Execute(bookmark.Url);
                }
            }
        }
    }
}
