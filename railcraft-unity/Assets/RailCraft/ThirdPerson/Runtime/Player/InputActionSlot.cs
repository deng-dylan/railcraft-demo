using System;
using UnityEngine.InputSystem;

namespace RailCraft.ThirdPerson.Player
{
    internal sealed class InputActionSlot : IDisposable
    {
        private InputAction action;
        private bool ownsAction;
        private bool wasEnabled;

        public InputAction Action => action;

        public void Bind(InputActionReference reference, Func<InputAction> fallbackFactory)
        {
            Release();

            action = reference == null ? fallbackFactory() : reference.action;
            ownsAction = reference == null;
            if (action == null)
                return;

            wasEnabled = action.enabled;
            if (!wasEnabled)
                action.Enable();
        }

        public void Release()
        {
            if (action == null)
                return;

            if (ownsAction)
            {
                action.Disable();
                action.Dispose();
            }
            else if (!wasEnabled)
            {
                action.Disable();
            }

            action = null;
            ownsAction = false;
            wasEnabled = false;
        }

        public void Dispose()
        {
            Release();
        }
    }
}
