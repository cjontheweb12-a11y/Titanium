// Browser/ViewModels/Base/ObservableObject.cs
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Browser.ViewModels.Base
{
    /// <summary>
    /// Clase base que implementa la interfaz INotifyPropertyChanged.
    /// Motivo: Proporciona una implementación reutilizable de INotifyPropertyChanged
    /// para las ViewModels. Esto es fundamental para el enlace de datos en UWP,
    /// ya que permite que la UI se actualice automáticamente cuando las propiedades
    /// de la ViewModel cambian. Evita la repetición de código en cada ViewModel.
    /// </summary>
    public class ObservableObject : INotifyPropertyChanged
    {
        /// <summary>
        /// Evento que se dispara cuando una propiedad cambia.
        /// Motivo: Parte del contrato de INotifyPropertyChanged.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notifica a los suscriptores que una propiedad ha cambiado.
        /// Motivo: Método auxiliar para disparar el evento PropertyChanged de forma segura.
        /// </summary>
        /// <param name="propertyName">El nombre de la propiedad que cambió.
        /// Se infiere automáticamente por el compilador si no se especifica.</param>
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Establece el valor de una propiedad y dispara el evento PropertyChanged si el valor ha cambiado.
        /// Motivo: Método genérico para simplificar la implementación de propiedades que deben notificar cambios.
        /// Reduce la cantidad de código repetitivo para el manejo de propiedades.
        /// </summary>
        /// <typeparam name="T">El tipo de la propiedad.</typeparam>
        /// <param name="storage">Una referencia al campo de respaldo de la propiedad.</param>
        /// <param name="value">El nuevo valor de la propiedad.</param>
        /// <param name="propertyName">El nombre de la propiedad. Se infiere automáticamente.</param>
        /// <returns>True si el valor ha cambiado y el evento PropertyChanged fue disparado; de lo contrario, false.</returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            // Motivo: Comparar los valores para evitar notificar cambios innecesarios,
            // lo que optimiza el rendimiento del enlace de datos.
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            RaisePropertyChanged(propertyName);
            return true;
        }
    }
}
