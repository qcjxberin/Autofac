﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Autofac.Util;

namespace Autofac.Injection
{
    /// <summary>
    /// Binds a constructor to the parameters that will be used when it is invoked.
    /// </summary>
    public class ConstructorParameterBinding
    {
        readonly ConstructorInfo _ci;
        readonly Func<object>[] _valueRetrievers;
        readonly bool _canInstantiate;

        /// <summary>
        /// The constructor on the target type. The actual constructor used
        /// might differ, e.g. if using a dynamic proxy.
        /// </summary>
        public ConstructorInfo TargetConstructor { get { return _ci; } }

        /// <summary>
        /// True if the binding is valid.
        /// </summary>
        public bool CanInstantiate { get { return _canInstantiate; } }

        /// <summary>
        /// Construct a new ConstructorParameterBinding.
        /// </summary>
        /// <param name="ci">ConstructorInfo to bind.</param>
        /// <param name="availableParameters">Available parameters.</param>
        /// <param name="context">Context in which to construct instance.</param>
        public ConstructorParameterBinding(
            ConstructorInfo ci,
            IEnumerable<Parameter> availableParameters,
            IComponentContext context)
        {
            _canInstantiate = true;
            _ci = Enforce.ArgumentNotNull(ci, "ci");
            Enforce.ArgumentNotNull(availableParameters, "availableParameters");
            Enforce.ArgumentNotNull(context, "context");

            var parameters = ci.GetParameters();
            _valueRetrievers = new Func<object>[parameters.Length];

            for (int i = 0; i < parameters.Length; ++i)
            {
                var pi = parameters[i];
                bool foundValue = false;
                foreach (var param in availableParameters)
                {
                    Func<object> valueRetriever = null;
                    if (param.CanSupplyValue(pi, context, out valueRetriever))
                    {
                        _valueRetrievers[i] = valueRetriever;
                        foundValue = true;
                        break;
                    }
                }
                if (!foundValue)
                {
                    _canInstantiate = false;
                    break;
                }
            }
        }

        /// <summary>
        /// Invoke the constructor with the parameter bindings.
        /// </summary>
        /// <returns>The constructed instance.</returns>
        public object Instantiate()
        {
            if (!CanInstantiate)
                throw new InvalidOperationException();

            var values = new object[_valueRetrievers.Length];
            for (int i = 0; i < _valueRetrievers.Length; ++i)
                values[i] = _valueRetrievers[i].Invoke();

            return TargetConstructor.Invoke(values);
        }
    }
}