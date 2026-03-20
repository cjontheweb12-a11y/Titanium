// Browser/Helpers/ServiceLocator.cs
using System;
using System.Collections.Generic;

namespace Browser.Helpers
{
    /// <summary>
    /// Un Service Locator simple para la inyección de dependencias.
    /// Motivo: Proporciona una forma centralizada de registrar y resolver instancias de servicios
    /// sin la complejidad de un contenedor de IoC completo para una aplicación UWP pequeña/mediana.
    /// Permite que las ViewModels y otros componentes accedan a los servicios sin conocer sus implementaciones concretas,
    /// lo que mejora el acoplamiento débil y la testabilidad.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>(); // Motivo: Almacena las instancias o fábricas de servicios.
        private static readonly Dictionary<Type, Type> _serviceTypes = new Dictionary<Type, Type>(); // Motivo: Almacena mapeos de interfaz a implementación.

        /// <summary>
        /// Registra una implementación de servicio para una interfaz.
        /// Motivo: Permite que el Service Locator sepa qué clase concreta instanciar cuando se solicita una interfaz.
        /// </summary>
        /// <typeparam name="TInterface">La interfaz del servicio.</typeparam>
        /// <typeparam name="TImplementation">La implementación concreta del servicio.</typeparam>
        public static void Register<TInterface, TImplementation>()
            where TImplementation : TInterface
        {
            _serviceTypes[typeof(TInterface)] = typeof(TImplementation);
        }

        /// <summary>
        /// Registra una instancia de servicio ya existente.
        /// Motivo: Útil para singletons o servicios que requieren un constructor con parámetros
        /// que no pueden ser resueltos automáticamente por un constructor por defecto.
        /// </summary>
        /// <typeparam name="TInterface">La interfaz del servicio.</typeparam>
        /// <param name="instance">La instancia del servicio a registrar.</param>
        public static void Register<TInterface>(TInterface instance)
        {
            _services[typeof(TInterface)] = instance;
        }

        /// <summary>
        /// Resuelve una instancia de servicio.
        /// Motivo: Permite a los consumidores obtener una instancia del servicio deseado.
        /// Si el servicio ya está instanciado (singleton), lo devuelve; de lo contrario,
        /// crea una nueva instancia (o la resuelve a través de la implementación registrada).
        /// </summary>
        /// <typeparam name="TInterface">La interfaz del servicio a resolver.</typeparam>
        /// <returns>La instancia del servicio.</returns>
        /// <exception cref="InvalidOperationException">Se lanza si el servicio no está registrado.</exception>
        public static TInterface Resolve<TInterface>()
        {
            Type interfaceType = typeof(TInterface);

            // Motivo: Si ya tenemos una instancia registrada, la devolvemos (singleton).
            if (_services.TryGetValue(interfaceType, out object serviceInstance))
            {
                return (TInterface)serviceInstance;
            }

            // Motivo: Si no hay una instancia, intentamos crearla usando el mapeo de tipos.
            if (_serviceTypes.TryGetValue(interfaceType, out Type implementationType))
            {
                // Motivo: Intentar resolver las dependencias del constructor recursivamente.
                // Esto permite la inyección de dependencias básicas (ej. SettingsService en MainViewModel).
                var constructor = implementationType.GetConstructors()[0]; // Asume un constructor simple.
                var parameters = constructor.GetParameters();
                var dependencies = new List<object>();

                foreach (var param in parameters)
                {
                    // Motivo: Para cada parámetro del constructor, intentar resolverlo a través del ServiceLocator.
                    // Esto implementa una forma rudimentaria de "resolución recursiva".
                    var resolveMethod = typeof(ServiceLocator).GetMethod(nameof(Resolve)).MakeGenericMethod(param.ParameterType);
                    dependencies.Add(resolveMethod.Invoke(null, null));
                }

                // Motivo: Crear la instancia del servicio y registrarla como singleton para futuras solicitudes.
                serviceInstance = Activator.CreateInstance(implementationType, dependencies.ToArray());
                _services[interfaceType] = serviceInstance; // Registrar como singleton después de la primera creación.
                return (TInterface)serviceInstance;
            }

            throw new InvalidOperationException($"Servicio no registrado: {interfaceType.FullName}");
        }

        /// <summary>
        /// Reinicia el Service Locator, borrando todos los registros de servicios.
        /// Motivo: Útil para escenarios de pruebas o si se necesita recrear el entorno de servicios.
        /// </summary>
        public static void Reset()
        {
            _services.Clear();
            _serviceTypes.Clear();
        }
    }
}
