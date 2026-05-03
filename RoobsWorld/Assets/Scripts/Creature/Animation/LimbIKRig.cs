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
        }
    }
}
