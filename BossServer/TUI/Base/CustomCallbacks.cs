﻿using System.IO;

namespace TerrariaUI.Base
{
    /// <summary>
    /// Collection of custom callbacks.
    /// </summary>
    public class CustomCallbacks
    {
        /// <summary>
        /// Callback for applying custom actions on Update().
        /// </summary>
        public Action<VisualObject> Update { get; set; }
        /// <summary>
        /// Callback for applying custom actions on PostUpdate().
        /// </summary>
        public Action<VisualObject> PostUpdate { get; set; }
        /// <summary>
        /// Callback for self size change.
        /// </summary>
        public Func<VisualObject, (int, int)> GetSize { get; set; }
        /// <summary>
        /// Callback for custom checking if user can touch this node.
        /// </summary>
        public Func<VisualObject, Touch, bool> CanTouch { get; set; }
        /// <summary>
        /// Callback for applying custom actions on Apply().
        /// </summary>
        public Action<VisualObject> Apply { get; set; }
        /// <summary>
        /// Callback for applying custom actions on PostApply().
        /// </summary>
        public Action<VisualObject> PostApply { get; set; }
        /// <summary>
        /// Callback for custom pulse event handling.
        /// </summary>
        public Action<VisualObject, PulseType> Pulse { get; set; }
        /// <summary>
        /// Callback for custom post pulse event handling.
        /// </summary>
        public Action<VisualObject, PulseType> PostPulse { get; set; }
        /// <summary>
        /// Callback for custom resource loading.
        /// </summary>
        public Action<VisualObject> Load { get; set; }
        /// <summary>
        /// Callback for custom resource releasing.
        /// </summary>
        public Action<VisualObject> Dispose { get; set; }
        /// <summary>
        /// Callback for custom database data read.
        /// </summary>
        public Action<VisualObject, BinaryReader> DBRead { get; set; }
        /// <summary>
        /// Callback for custom database data write
        /// </summary>
        public Action<VisualObject, BinaryWriter> DBWrite { get; set; }
        /// <summary>
        /// Callback for custom database user data read
        /// </summary>
        public Action<VisualObject, BinaryReader, int> UDBRead { get; set; }
        /// <summary>
        /// Callback for custom database user data write
        /// </summary>
        public Action<VisualObject, BinaryWriter, int> UDBWrite { get; set; }

        /// <summary>
        /// Collection of custom callbacks.
        /// </summary>
        public CustomCallbacks() { }

        /// <summary>
        /// Collection of custom callbacks.
        /// </summary>
        public CustomCallbacks(CustomCallbacks callbacks)
        {
            Update = callbacks.Update;
            PostUpdate = callbacks.PostUpdate;
            GetSize = callbacks.GetSize;
            CanTouch = callbacks.CanTouch;
            Apply = callbacks.Apply;
            Pulse = callbacks.Pulse;
            PostPulse = callbacks.PostPulse;
            Load = callbacks.Load;
            Dispose = callbacks.Dispose;
            DBRead = callbacks.DBRead;
            DBWrite = callbacks.DBWrite;
            UDBRead = callbacks.UDBRead;
            UDBWrite = callbacks.UDBWrite;
        }
    }
}
