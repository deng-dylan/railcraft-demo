using UnityEngine;

namespace RailCraft.ThirdPerson.Player
{
    public readonly struct InteractionContext
    {
        public InteractionContext(GameObject player)
        {
            Player = player;
        }

        public GameObject Player { get; }
    }
}
