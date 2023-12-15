using UnityEngine;

namespace lethalCompanyRevive.Misc
{
    public readonly struct ObjectPlacement<T, M>
        where T : Transform
        where M : MonoBehaviour
    {
        public readonly T TargetObject;
        public readonly M GameObject;
        public readonly Vector3 PositionOffset;
        public readonly Vector3 RotationOffset;

        public ObjectPlacement(T targetObject, M gameObject, Vector3 positionOffset, Vector3 rotationOffset)
        {
            TargetObject = targetObject;
            GameObject = gameObject;
            PositionOffset = positionOffset;
            RotationOffset = rotationOffset;
        }
    }

    public readonly struct ObjectPlacements<T, M>
        where T : Transform
        where M : MonoBehaviour
    {
        public readonly ObjectPlacement<T, M> Placement;
        public readonly ObjectPlacement<T, M> PreviousPlacement;

        public ObjectPlacements(ObjectPlacement<T, M> placement, ObjectPlacement<T, M> previousPlacement)
        {
            Placement = placement;
            PreviousPlacement = previousPlacement;
        }
    }
}
