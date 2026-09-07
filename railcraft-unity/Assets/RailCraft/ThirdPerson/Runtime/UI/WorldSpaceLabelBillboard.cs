using UnityEngine;

namespace RailCraft.ThirdPerson.UI
{
    /// <summary>
    /// Keeps generated TextMesh labels readable from either side of the hall.
    /// The label remains upright and only rotates around world Y.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldSpaceLabelBillboard : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool keepUpright = true;

        public void Configure(Camera configuredCamera, bool configuredKeepUpright = true)
        {
            targetCamera = configuredCamera;
            keepUpright = configuredKeepUpright;
        }

        public static Quaternion CalculateFacingRotation(
            Vector3 labelPosition,
            Vector3 cameraPosition,
            bool upright)
        {
            // TextMesh renders its readable face opposite local +Z. Point +Z
            // away from the camera so the glyph front faces the viewer.
            var direction = labelPosition - cameraPosition;
            if (upright)
                direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return Quaternion.identity;
            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void LateUpdate()
        {
            var camera = targetCamera != null ? targetCamera : Camera.main;
            if (camera == null)
                return;

            transform.rotation = CalculateFacingRotation(
                transform.position,
                camera.transform.position,
                keepUpright);
        }
    }
}
