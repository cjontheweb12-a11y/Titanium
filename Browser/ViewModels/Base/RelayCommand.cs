// Browser/ViewModels/Base/RelayCommand.cs
using System;
using System.Windows.Input;

namespace Browser.ViewModels.Base
{
    /// <summary>
    /// Una implementación de ICommand que retransmite la funcionalidad a delegados.
    /// Motivo: Proporciona una forma simple y reutilizable de enlazar acciones de la UI
    /// (como clicks de botón) a métodos en una ViewModel. Esto es fundamental para el patrón MVVM,
    /// ya que desacopla la UI de la lógica de negocio y hace que las ViewModels sean más fáciles de probar.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute; // Motivo: El delegado que se ejecutará cuando se invoque el comando.
        private readonly Func<bool> _canExecute; // Motivo: El delegado que determina si el comando se puede ejecutar.

        /// <summary>
        /// Evento que se dispara cuando la capacidad de ejecución del comando puede haber cambiado.
        /// Motivo: Parte del contrato de ICommand. Permite a la UI actualizar el estado de los controles
        /// (ej. habilitar/deshabilitar un botón) basándose en las condiciones de _canExecute.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="RelayCommand"/>.
        /// Motivo: Constructor para comandos que siempre se pueden ejecutar.
        /// </summary>
        /// <param name="execute">El delegado de acción a ejecutar.</param>
        public RelayCommand(Action execute) : this(execute, null) { }

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="RelayCommand"/>.
        /// Motivo: Constructor para comandos cuya capacidad de ejecución se puede determinar
        /// mediante un delegado.
        /// </summary>
        /// <param name="execute">El delegado de acción a ejecutar.</param>
        /// <param name="canExecute">El delegado de función para comprobar la capacidad de ejecución.</param>
        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute), "El delegado de ejecución no puede ser nulo.");
            _canExecute = canExecute;
        }

        /// <summary>
        /// Define el método que determina si el comando se puede ejecutar en su estado actual.
        /// Motivo: Invoca el delegado _canExecute si está definido; de lo contrario, el comando siempre se puede ejecutar.
        /// </summary>
        /// <param name="parameter">Datos utilizados por el comando. Si el comando no requiere pasar datos, se puede establecer en null.</param>
        /// <returns>True si el comando se puede ejecutar; de lo contrario, false.</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        /// <summary>
        /// Define el método a invocar cuando se invoca el comando.
        /// Motivo: Invoca el delegado _execute.
        /// </summary>
        /// <param name="parameter">Datos utilizados por el comando. Si el comando no requiere pasar datos, se puede establecer en null.</param>
        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                _execute();
            }
        }

        /// <summary>
        /// Dispara el evento <see cref="CanExecuteChanged"/>.
        /// Motivo: Permite a las ViewModels notificar a la UI que la capacidad de ejecución de un comando ha cambiado,
        /// forzando a la UI a volver a evaluar el estado del botón/control enlazado.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Una implementación de ICommand genérica que retransmite la funcionalidad a delegados.
    /// Motivo: Permite pasar un parámetro de tipo específico al método de ejecución,
    /// lo que es útil para comandos que operan sobre un elemento de datos concreto (ej. un Bookmark).
    /// </summary>
    /// <typeparam name="T">El tipo del parámetro de comando.</typeparam>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="RelayCommand{T}"/>.
        /// Motivo: Constructor para comandos genéricos que siempre se pueden ejecutar.
        /// </summary>
        /// <param name="execute">El delegado de acción genérico a ejecutar.</param>
        public RelayCommand(Action<T> execute) : this(execute, null) { }

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="RelayCommand{T}"/>.
        /// Motivo: Constructor para comandos genéricos cuya capacidad de ejecución se puede determinar
        /// mediante un delegado.
        /// </summary>
        /// <param name="execute">El delegado de acción genérico a ejecutar.</param>
        /// <param name="canExecute">El delegado de función genérico para comprobar la capacidad de ejecución.</param>
        public RelayCommand(Action<T> execute, Func<T, bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute), "El delegado de ejecución no puede ser nulo.");
            _canExecute = canExecute;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                _execute((T)parameter);
            }
        }

        /// <summary>
        /// Dispara el evento <see cref="CanExecuteChanged"/>.
        /// Motivo: Permite a las ViewModels notificar a la UI que la capacidad de ejecución de un comando ha cambiado.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
