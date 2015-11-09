﻿using System;
using Binarysharp.MemoryManagement.Objects.BaseClasses;
using Binarysharp.MemoryManagement.Objects.Edits;
using ToolsSharp.Helpers;
using ToolsSharp.Managment;
using ToolsSharp.Tools.Logging.Default;


namespace Binarysharp.MemoryManagement.Internals
{
    /// <summary>
    ///     A manager class to handle function detours, and hooks.
    ///     <remarks>All credits to Apoc.</remarks>
    /// </summary>
    public class DetourManager : SafeManager<Detour>
    {
        #region Constructors, Destructors
        /// <summary>
        ///     Initializes a new instance of the <see cref="DetourManager" /> class.
        /// </summary>
        /// <param name="processMemory">The <see cref="ProcessMemory" /> Instance.</param>
        public DetourManager(ProcessMemory processMemory) : base(new DebugLog())
        {
            ProcessMemory = processMemory;
        }
        #endregion

        #region Public Properties, Indexers
        /// <summary>
        ///     The reference of the <see cref="ProcessMemory" /> object.
        /// </summary>
        protected ProcessMemory ProcessMemory { get; }
        #endregion

        /// <summary>
        ///     Creates a new Detour.
        /// </summary>
        /// <param name="target">
        ///     The original function to detour. (This delegate should already be registered via
        ///     Magic.RegisterDelegate)
        /// </param>
        /// <param name="newTarget">The new function to be called. (This delegate should NOT be registered!)</param>
        /// <param name="name">The name of the detour.</param>
        /// <returns>
        ///     A <see cref="Detour" /> object containing the required methods to apply, remove, and call the original
        ///     function.
        /// </returns>
        public Detour Create(Delegate target, Delegate newTarget, string name)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            if (newTarget == null)
            {
                throw new ArgumentNullException(nameof(newTarget));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (!AttributesHelper.HasUfpAttribute(target))
            {
                throw new Exception(
                    "The target delegate does not have the proper UnmanagedFunctionPointer attribute!");
            }
            if (!AttributesHelper.HasUfpAttribute(newTarget))
            {
                throw new Exception(
                    "The new target delegate does not have the proper UnmanagedFunctionPointer attribute!");
            }

            if (InternalItems.ContainsKey(name))
            {
                throw new ArgumentException($"The {name} detour already exists!", nameof(name));
            }
            InternalItems.Add(name, new Detour(target, newTarget, name, ProcessMemory));
            return InternalItems[name];
        }

        /// <summary>
        ///     Creates and applies new Detour.
        /// </summary>
        /// <param name="target">
        ///     The original function to detour. (This delegate should already be registered via
        ///     Magic.RegisterDelegate)
        /// </param>
        /// <param name="newTarget">The new function to be called. (This delegate should NOT be registered!)</param>
        /// <param name="name">The name of the detour.</param>
        /// <returns>
        ///     A <see cref="Detour" /> object containing the required methods to apply, remove, and call the original
        ///     function.
        /// </returns>
        public Detour CreateAndApply(Delegate target, Delegate newTarget, string name)
        {
            Create(target, newTarget, name);
            InternalItems[name].Enable();
            return InternalItems[name];
        }
    }
}