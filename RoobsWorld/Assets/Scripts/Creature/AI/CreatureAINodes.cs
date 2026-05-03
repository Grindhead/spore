using UnityEngine;

namespace RoobsWorld.Creature.AI
{
    /// <summary>
    /// Behavior node that locates the nearest GameObject tagged as Food within a radius.
    /// </summary>
    public class FindNearestFood : BTNode
    {
        private readonly float _range;

        public FindNearestFood(float range = 30f) { _range = range; }

        public override BTStatus Tick(BTContext ctx)
        {
            var hits = Physics.OverlapSphere(ctx.Self.transform.position, _range);
            Transform closest = null;
            float minDist = float.MaxValue;

            foreach (var hit in hits)
            {
                if (!hit.CompareTag("Food")) continue;
                float dist = Vector3.Distance(ctx.Self.transform.position, hit.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = hit.transform;
                }
            }

            if (closest != null)
            {
                ctx.Target = closest;
                ctx.MoveTarget = closest.position;
                return BTStatus.Success;
            }

            return BTStatus.Failure;
        }
    }

    /// <summary>
    /// Behavior node that finds the nearest creature whose attack significantly exceeds this creature's defense.
    /// </summary>
    public class FindNearestThreat : BTNode
    {
        private readonly float _range;

        public FindNearestThreat(float range = 15f) { _range = range; }

        public override BTStatus Tick(BTContext ctx)
        {
            var hits = Physics.OverlapSphere(ctx.Self.transform.position, _range);
            Transform closest = null;
            float minDist = float.MaxValue;

            foreach (var hit in hits)
            {
                var other = BTComponentLookup.FindComponentOnCollider<CreatureInstance>(hit);
                if (other == null || other == ctx.Creature || !other.IsAlive) continue;

                if (CreatureSizeRules.ShouldFleeFromPlayer(ctx.Creature, other)
                    || other.Stats.Attack > ctx.Creature.Stats.Defense * 1.5f)
                {
                    float dist = Vector3.Distance(ctx.Self.transform.position, hit.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = other.transform;
                    }
                }
            }

            if (closest != null)
            {
                ctx.Target = closest;
                return BTStatus.Success;
            }

            return BTStatus.Failure;
        }
    }

    /// <summary>
    /// Behavior node that finds nearby weak creatures suitable as prey.
    /// </summary>
    public class FindNearestPrey : BTNode
    {
        private readonly float _range;

        public FindNearestPrey(float range = 20f) { _range = range; }

        public override BTStatus Tick(BTContext ctx)
        {
            var hits = Physics.OverlapSphere(ctx.Self.transform.position, _range);
            Transform closest = null;
            float minDist = float.MaxValue;

            foreach (var hit in hits)
            {
                var other = BTComponentLookup.FindComponentOnCollider<CreatureInstance>(hit);
                if (other == null || other == ctx.Creature || !other.IsAlive) continue;
                if (CreatureSizeRules.ShouldFleeFromPlayer(ctx.Creature, other)) continue;

                if (other.Stats.Health < ctx.Creature.Stats.Attack * 3f)
                {
                    float dist = Vector3.Distance(ctx.Self.transform.position, hit.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = other.transform;
                    }
                }
            }

            if (closest != null)
            {
                ctx.Target = closest;
                ctx.MoveTarget = closest.position;
                return BTStatus.Success;
            }

            return BTStatus.Failure;
        }
    }

    /// <summary>
    /// Behavior node that moves a rigidbody toward the current move target until within an arrival distance.
    /// </summary>
    public class MoveToTarget : BTNode
    {
        private readonly float _arrivalDist;

        public MoveToTarget(float arrivalDist = 1.5f) { _arrivalDist = arrivalDist; }

        public override BTStatus Tick(BTContext ctx)
        {
            if (ctx.Self == null) return BTStatus.Failure;
            var rb = ctx.Self.GetComponent<Rigidbody>();
            if (rb == null) return BTStatus.Failure;

            Vector3 dir = (ctx.MoveTarget - ctx.Self.transform.position);
            dir.y = 0;

            if (dir.magnitude < _arrivalDist)
            {
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                return BTStatus.Success;
            }

            float speed = ctx.Creature != null ? ctx.Creature.Stats.Speed : 3f;
            Vector3 move = dir.normalized * speed;
            rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);

            if (dir.sqrMagnitude > 0.01f)
                ctx.Self.transform.rotation = Quaternion.Slerp(
                    ctx.Self.transform.rotation,
                    Quaternion.LookRotation(dir.normalized),
                    ctx.DeltaTime * 5f);

            return BTStatus.Running;
        }
    }

    /// <summary>
    /// Behavior node that moves a rigidbody away from the current target until a safe distance is reached.
    /// </summary>
    public class FleeFromTarget : BTNode
    {
        private readonly float _safeDist;

        public FleeFromTarget(float safeDist = 20f) { _safeDist = safeDist; }

        public override BTStatus Tick(BTContext ctx)
        {
            if (ctx.Target == null) return BTStatus.Failure;
            var rb = ctx.Self.GetComponent<Rigidbody>();
            if (rb == null) return BTStatus.Failure;

            Vector3 away = ctx.Self.transform.position - ctx.Target.position;
            away.y = 0;

            if (away.magnitude > _safeDist)
            {
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                return BTStatus.Success;
            }

            float speed = ctx.Creature != null ? ctx.Creature.Stats.Speed * 1.2f : 4f;
            Vector3 move = away.normalized * speed;
            rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);

            if (away.sqrMagnitude > 0.01f)
                ctx.Self.transform.rotation = Quaternion.Slerp(
                    ctx.Self.transform.rotation,
                    Quaternion.LookRotation(away.normalized),
                    ctx.DeltaTime * 5f);

            return BTStatus.Running;
        }
    }

    /// <summary>
    /// Behavior node that periodically applies damage to the current target when in range.
    /// </summary>
    public class AttackTarget : BTNode
    {
        private readonly float _range;
        private float _cooldown;

        public AttackTarget(float range = 2f) { _range = range; }

        public override BTStatus Tick(BTContext ctx)
        {
            if (ctx.Target == null) return BTStatus.Failure;
            _cooldown -= ctx.DeltaTime;
            if (_cooldown > 0) return BTStatus.Running;

            float dist = Vector3.Distance(ctx.Self.transform.position, ctx.Target.position);
            if (dist > _range) return BTStatus.Failure;

            var targetCollider = ctx.Target.GetComponent<Collider>() ?? ctx.Target.GetComponentInChildren<Collider>();
            var target = targetCollider != null
                ? BTComponentLookup.FindComponentOnCollider<CreatureInstance>(targetCollider)
                : ctx.Target.GetComponent<CreatureInstance>();
            if (target != null && target.IsAlive)
            {
                if (CreatureSizeRules.ShouldFleeFromPlayer(ctx.Creature, target))
                    return BTStatus.Failure;

                target.TakeDamage(ctx.Creature.Stats.Attack);
                _cooldown = 1f;
                return target.IsAlive ? BTStatus.Running : BTStatus.Success;
            }

            return BTStatus.Failure;
        }
    }

    /// <summary>
    /// Behavior node that chooses random points within a radius and walks between them.
    /// </summary>
    public class Wander : BTNode
    {
        private const float MemoryCellSize = 2.5f;
        private const int RecentCellCapacity = 16;
        private const int CandidateCount = 8;

        private readonly float _radius;
        private Vector3 _wanderTarget;
        private bool _hasTarget;

        public Wander(float radius = 15f) { _radius = radius; }

        public override BTStatus Tick(BTContext ctx)
        {
            CreatureMovementMemory.RecordRecentLocation(
                ctx.RecentMoveCells,
                ctx.Self.transform.position,
                MemoryCellSize,
                RecentCellCapacity);

            if (!_hasTarget)
            {
                Vector3 center = ctx.Self.transform.position;
                Vector3[] candidates = new Vector3[CandidateCount];
                for (int i = 0; i < candidates.Length; i++)
                {
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float dist = Random.Range(_radius * 0.3f, _radius);
                    candidates[i] = center + new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);
                }

                _wanderTarget = CreatureMovementMemory.SelectLeastVisitedCandidate(
                    center,
                    candidates,
                    ctx.RecentMoveCells,
                    MemoryCellSize,
                    Mathf.Min(_radius * 0.45f, 4f));
                _hasTarget = true;
                ctx.MoveTarget = _wanderTarget;
            }

            var rb = ctx.Self.GetComponent<Rigidbody>();
            if (rb == null) return BTStatus.Failure;

            Vector3 dir = _wanderTarget - ctx.Self.transform.position;
            dir.y = 0;

            if (dir.magnitude < 1f)
            {
                _hasTarget = false;
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                return BTStatus.Success;
            }

            float speed = ctx.Creature != null ? ctx.Creature.Stats.Speed * 0.5f : 2f;
            Vector3 move = dir.normalized * speed;
            rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);

            if (dir.sqrMagnitude > 0.01f)
                ctx.Self.transform.rotation = Quaternion.Slerp(
                    ctx.Self.transform.rotation,
                    Quaternion.LookRotation(dir.normalized),
                    ctx.DeltaTime * 3f);

            return BTStatus.Running;
        }
    }

    /// <summary>
    /// Condition node that succeeds when the creature's health is below a fraction of maximum.
    /// </summary>
    public class IsHealthLow : BTNode
    {
        private readonly float _threshold;

        public IsHealthLow(float threshold = 0.3f) { _threshold = threshold; }

        public override BTStatus Tick(BTContext ctx)
        {
            if (ctx.Creature == null) return BTStatus.Failure;
            float ratio = ctx.Creature.CurrentHealth / Mathf.Max(1f, ctx.Creature.Stats.Health);
            return ratio < _threshold ? BTStatus.Success : BTStatus.Failure;
        }
    }

    internal static class BTComponentLookup
    {
        internal static T FindComponentOnCollider<T>(Collider hit) where T : Component
        {
            if (hit == null) return null;
            var direct = hit.GetComponent<T>();
            if (direct != null) return direct;

            var attachedBody = hit.attachedRigidbody;
            if (attachedBody != null)
            {
                var bodyComponent = attachedBody.GetComponent<T>();
                if (bodyComponent != null) return bodyComponent;
            }

            var inParent = hit.GetComponentInParent<T>();
            if (inParent != null) return inParent;

            return hit.GetComponentInChildren<T>();
        }
    }
}
