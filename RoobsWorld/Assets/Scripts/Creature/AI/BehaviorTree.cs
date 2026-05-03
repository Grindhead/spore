using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Creature.AI
{
    /// <summary>
    /// Execution status values returned from behavior tree nodes.
    /// </summary>
    public enum BTStatus { Success, Failure, Running }

    /// <summary>
    /// Shared context passed through behavior tree ticks, holding references and transient data.
    /// </summary>
    public class BTContext
    {
        public GameObject Self;
        public CreatureInstance Creature;
        public Transform Target;
        public Vector3 MoveTarget;
        public float DeltaTime;
        public Queue<Vector2Int> RecentMoveCells = new();
        public Dictionary<string, object> Blackboard = new();
    }

    /// <summary>
    /// Base node in the behavior tree hierarchy.
    /// </summary>
    public abstract class BTNode
    {
        public abstract BTStatus Tick(BTContext ctx);
    }

    /// <summary>
    /// Composite node that runs children in order until one fails or all succeed.
    /// </summary>
    public class BTSequence : BTNode
    {
        private readonly BTNode[] _children;
        private int _runningIndex;

        public BTSequence(params BTNode[] children)
        {
            _children = children;
        }

        public override BTStatus Tick(BTContext ctx)
        {
            for (int i = _runningIndex; i < _children.Length; i++)
            {
                var status = _children[i].Tick(ctx);
                if (status == BTStatus.Running)
                {
                    _runningIndex = i;
                    return BTStatus.Running;
                }
                if (status == BTStatus.Failure)
                {
                    _runningIndex = 0;
                    return BTStatus.Failure;
                }
            }

            _runningIndex = 0;
            return BTStatus.Success;
        }
    }

    /// <summary>
    /// Composite node that tries children in order until one succeeds or all fail.
    /// </summary>
    public class BTSelector : BTNode
    {
        private readonly BTNode[] _children;
        private int _runningIndex;

        public BTSelector(params BTNode[] children)
        {
            _children = children;
        }

        public override BTStatus Tick(BTContext ctx)
        {
            for (int i = _runningIndex; i < _children.Length; i++)
            {
                var status = _children[i].Tick(ctx);
                if (status == BTStatus.Success)
                {
                    _runningIndex = 0;
                    return BTStatus.Success;
                }
                if (status == BTStatus.Running)
                {
                    _runningIndex = i;
                    return BTStatus.Running;
                }
            }

            _runningIndex = 0;
            return BTStatus.Failure;
        }
    }

    /// <summary>
    /// Decorator that inverts <see cref="BTStatus.Success"/> and <see cref="BTStatus.Failure"/> from its child.
    /// </summary>
    public class BTInverter : BTNode
    {
        private readonly BTNode _child;

        public BTInverter(BTNode child) { _child = child; }

        public override BTStatus Tick(BTContext ctx)
        {
            var status = _child.Tick(ctx);
            return status switch
            {
                BTStatus.Success => BTStatus.Failure,
                BTStatus.Failure => BTStatus.Success,
                _ => BTStatus.Running
            };
        }
    }

    /// <summary>
    /// Decorator that repeats its child a fixed number of times before succeeding.
    /// </summary>
    public class BTRepeater : BTNode
    {
        private readonly BTNode _child;
        private readonly int _count;
        private int _current;

        public BTRepeater(BTNode child, int count)
        {
            _child = child;
            _count = count;
        }

        public override BTStatus Tick(BTContext ctx)
        {
            if (_current >= _count)
            {
                _current = 0;
                return BTStatus.Success;
            }

            var status = _child.Tick(ctx);
            if (status == BTStatus.Running) return BTStatus.Running;

            _current++;
            return _current >= _count ? BTStatus.Success : BTStatus.Running;
        }
    }
}
