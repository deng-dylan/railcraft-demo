using System;
using UnityEngine;

namespace RailCraft.ThirdPerson.Player
{
    /// <summary>
    /// Shared input gate used by the player, camera and interaction scanner.
    /// Quiz and menu presenters can lock all three systems with one call.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThirdPersonInputLock : MonoBehaviour
    {
        [SerializeField] private bool inputLocked;

        public bool InputLocked => inputLocked;

        public event Action<bool> InputLockChanged;

        public void SetInputLocked(bool locked)
        {
            if (inputLocked == locked)
                return;

            inputLocked = locked;
            InputLockChanged?.Invoke(inputLocked);
        }
    }
}
