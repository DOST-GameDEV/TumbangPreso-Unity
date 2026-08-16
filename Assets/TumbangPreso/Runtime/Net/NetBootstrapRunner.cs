using System;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>
    /// Runs one action on the first frame after the boot scene exists, then removes itself.
    ///
    /// ⚠️ ITS OWN FILE BECAUSE IT IS A MonoBehaviour. One per file, named after the class, or
    /// the built player reports `The file 'levelN' is corrupted!` and dies on load. That rule
    /// has already cost this port a whole pass.
    /// </summary>
    public sealed class NetBootstrapRunner : MonoBehaviour
    {
        private Action _action;

        public void Bind(Action action) => _action = action;

        private void Start()
        {
            var run = _action;
            _action = null;

            try { run?.Invoke(); }
            finally { Destroy(gameObject); }
        }
    }
}
