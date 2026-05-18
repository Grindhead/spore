using UnityEngine;

namespace RoobsWorld.Creature.Animation
{
    /// <summary>
    /// Holds IK state for a single limb and applies FABRIK to its joint transforms.
    /// </summary>
    public class LimbIKRig
    {
        public Transform[] Joints;
        public Vector3 FootTarget;
        public Vector3 FloorNormal = Vector3.up;
        public Vector3 FootRestPosition;
        public Vector3 StepOrigin;
        public Vector3 StepDestination;
        public float StepProgress;
        public bool IsStepping;
        public float PhaseOffset;

        public void UpdateIK()
        {
            if (Joints == null || Joints.Length < 2) return;

            var chain = new Vector3[Joints.Length];
            for (int i = 0; i < Joints.Length; i++)
                chain[i] = Joints[i].position;

            var solved = FABRIKSolver.Solve(chain, FootTarget, 0.005f, 8);

            for (int i = 1; i < Joints.Length; i++)
                Joints[i].position = solved[i];

            Vector3 up = FloorNormal.sqrMagnitude > 0.0001f ? FloorNormal.normalized : Vector3.up;
            for (int i = 0; i < Joints.Length - 1; i++)
            {
                Vector3 toChild = Joints[i + 1].position - Joints[i].position;
                if (toChild.sqrMagnitude < 0.000001f) continue;

                Joints[i].rotation = Quaternion.LookRotation(toChild.normalized, up);
            }
        }
    }
}
