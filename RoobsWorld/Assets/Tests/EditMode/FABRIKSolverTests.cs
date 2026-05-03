using NUnit.Framework;
using RoobsWorld.Creature.Animation;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class FABRIKSolverTests
    {
        [Test]
        public void Solve_Moves_EndEffector_Toward_Target_Within_Reach()
        {
            var chain = new Vector3[]
            {
                Vector3.zero,
                Vector3.up,
                Vector3.up * 2f
            };
            var target = new Vector3(1f, 1f, 0f);

            var result = FABRIKSolver.Solve(chain, target, tolerance: 0.01f, maxIterations: 20);

            float dist = Vector3.Distance(result[^1], target);
            Assert.Less(dist, 0.05f, "End effector should reach the target");
        }

        [Test]
        public void Solve_Preserves_Segment_Lengths()
        {
            var chain = new Vector3[]
            {
                Vector3.zero,
                Vector3.up * 1.5f,
                Vector3.up * 3f
            };
            var target = new Vector3(2f, 0f, 0f);

            float originalLen0 = Vector3.Distance(chain[0], chain[1]);
            float originalLen1 = Vector3.Distance(chain[1], chain[2]);

            var result = FABRIKSolver.Solve(chain, target, tolerance: 0.01f, maxIterations: 20);

            float solvedLen0 = Vector3.Distance(result[0], result[1]);
            float solvedLen1 = Vector3.Distance(result[1], result[2]);

            Assert.AreEqual(originalLen0, solvedLen0, 0.01f, "Segment 0 length must be preserved");
            Assert.AreEqual(originalLen1, solvedLen1, 0.01f, "Segment 1 length must be preserved");
        }

        [Test]
        public void Solve_Unreachable_Target_Extends_Toward_It()
        {
            var chain = new Vector3[]
            {
                Vector3.zero,
                Vector3.up,
                Vector3.up * 2f
            };
            var target = new Vector3(100f, 0f, 0f);

            var result = FABRIKSolver.Solve(chain, target, tolerance: 0.01f, maxIterations: 10);

            Vector3 dirToTarget = (target - result[0]).normalized;
            Vector3 endDir = (result[^1] - result[0]).normalized;
            float dot = Vector3.Dot(dirToTarget, endDir);
            Assert.Greater(dot, 0.9f, "Chain should extend toward the unreachable target");
        }

        [Test]
        public void Solve_Root_Stays_Fixed()
        {
            var rootPos = new Vector3(3f, 1f, 2f);
            var chain = new Vector3[]
            {
                rootPos,
                rootPos + Vector3.up,
                rootPos + Vector3.up * 2f
            };
            var target = rootPos + new Vector3(1f, 0.5f, 0f);

            var result = FABRIKSolver.Solve(chain, target, tolerance: 0.01f, maxIterations: 20);

            Assert.AreEqual(rootPos.x, result[0].x, 0.001f);
            Assert.AreEqual(rootPos.y, result[0].y, 0.001f);
            Assert.AreEqual(rootPos.z, result[0].z, 0.001f);
        }

        [Test]
        public void Solve_SingleJoint_Points_At_Target()
        {
            var chain = new Vector3[]
            {
                Vector3.zero,
                Vector3.up
            };
            var target = Vector3.right;

            var result = FABRIKSolver.Solve(chain, target, tolerance: 0.01f, maxIterations: 10);

            float dist = Vector3.Distance(result[1], target);
            Assert.Less(dist, 0.05f);
        }

        [Test]
        public void Solve_Already_At_Target_Returns_Same_Chain()
        {
            var chain = new Vector3[]
            {
                Vector3.zero,
                Vector3.up,
                Vector3.up * 2f
            };
            var target = Vector3.up * 2f;

            var result = FABRIKSolver.Solve(chain, target, tolerance: 0.01f, maxIterations: 5);

            for (int i = 0; i < chain.Length; i++)
            {
                Assert.AreEqual(chain[i].x, result[i].x, 0.01f);
                Assert.AreEqual(chain[i].y, result[i].y, 0.01f);
                Assert.AreEqual(chain[i].z, result[i].z, 0.01f);
            }
        }

        [Test]
        public void Solve_FourJoint_Chain_Reaches_Target()
        {
            var chain = new Vector3[]
            {
                Vector3.zero,
                new Vector3(0, 1, 0),
                new Vector3(0, 2, 0),
                new Vector3(0, 3, 0)
            };
            var target = new Vector3(2f, 1f, 0f);

            var result = FABRIKSolver.Solve(chain, target, tolerance: 0.01f, maxIterations: 30);

            float dist = Vector3.Distance(result[^1], target);
            Assert.Less(dist, 0.1f, "4-joint chain should reach target within total reach");
        }
    }
}
