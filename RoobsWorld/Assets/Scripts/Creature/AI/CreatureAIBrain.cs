using UnityEngine;

namespace RoobsWorld.Creature.AI
{
    /// <summary>
    /// Dietary preference used to choose between herbivore, carnivore, or omnivore behavior trees.
    /// </summary>
    public enum CreatureDiet { Herbivore, Carnivore, Omnivore }

    /// <summary>
    /// MonoBehaviour that builds and ticks a behavior tree to control autonomous creature movement and feeding.
    /// </summary>
    public class CreatureAIBrain : MonoBehaviour
    {
        [SerializeField] private CreatureDiet _diet = CreatureDiet.Omnivore;
        [SerializeField] private float _tickRate = 0.2f;

        private BTNode _root;
        private BTContext _context;
        private float _tickTimer;
        private CreatureInstance _creature;

        public CreatureDiet Diet => _diet;
        public BTContext Context => _context;

        /// <summary>
        /// Initializes the AI brain for the given diet and constructs the underlying behavior tree.
        /// </summary>
        public void Initialize(CreatureDiet diet)
        {
            _diet = diet;
            _creature = GetComponent<CreatureInstance>();

            _context = new BTContext
            {
                Self = gameObject,
                Creature = _creature
            };

            _root = BuildTree();
        }

        private BTNode BuildTree()
        {
            var survive = new BTSequence(
                new IsHealthLow(0.25f),
                new BTSelector(
                    new FleeFromTarget(25f),
                    new Wander(20f)
                )
            );

            var fleeFromDanger = new BTSequence(
                new FindNearestThreat(15f),
                new FleeFromTarget(20f)
            );

            BTNode feedBehavior;
            if (_diet == CreatureDiet.Herbivore)
            {
                feedBehavior = new BTSequence(
                    new FindNearestFood(30f),
                    new MoveToTarget(1f)
                );
            }
            else if (_diet == CreatureDiet.Carnivore)
            {
                feedBehavior = new BTSequence(
                    new FindNearestPrey(25f),
                    new MoveToTarget(2f),
                    new AttackTarget(2.5f)
                );
            }
            else
            {
                feedBehavior = new BTSelector(
                    new BTSequence(new FindNearestFood(30f), new MoveToTarget(1f)),
                    new BTSequence(new FindNearestPrey(20f), new MoveToTarget(2f), new AttackTarget(2.5f))
                );
            }

            return new BTSelector(
                survive,
                fleeFromDanger,
                feedBehavior,
                new Wander(15f)
            );
        }

        private void Update()
        {
            if (_root == null || _context == null) return;

            _tickTimer -= Time.deltaTime;
            if (_tickTimer > 0) return;
            _tickTimer = _tickRate;

            _context.DeltaTime = _tickRate;
            _root.Tick(_context);
        }
    }
}
