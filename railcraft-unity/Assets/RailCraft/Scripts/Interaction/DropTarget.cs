using System;
using UnityEngine;

namespace RailCraft.Interaction
{
    public sealed class DropTarget : MonoBehaviour
    {
        [SerializeField] private string targetId;
        [SerializeField] private string acceptedStepId;
        [SerializeField] private Transform snapAnchor;
        [SerializeField] private float snapDuration = 0.45f;
        [SerializeField] private float snapRadius = 0.75f;

        private IDragAuthorization authorization;

        public string TargetId => targetId;
        public string AcceptedStepId => acceptedStepId;
        public Transform SnapAnchor => snapAnchor;
        public float SnapDuration => snapDuration;
        public float SnapRadius => snapRadius;

        public void Configure(
            string configuredTargetId,
            string configuredAcceptedStepId,
            Transform configuredSnapAnchor,
            float configuredSnapDuration,
            float configuredSnapRadius)
        {
            targetId = configuredTargetId;
            acceptedStepId = configuredAcceptedStepId;
            snapAnchor = configuredSnapAnchor;
            snapDuration = Mathf.Max(0f, configuredSnapDuration);
            snapRadius = Mathf.Max(0f, configuredSnapRadius);
        }

        public void SetAuthorization(IDragAuthorization configuredAuthorization)
        {
            authorization = configuredAuthorization;
        }

        public bool CanAccept(string stepId)
        {
            return !string.IsNullOrEmpty(stepId)
                && string.Equals(stepId, acceptedStepId, StringComparison.Ordinal)
                && (authorization == null || authorization.CanDrag(stepId));
        }
    }
}
