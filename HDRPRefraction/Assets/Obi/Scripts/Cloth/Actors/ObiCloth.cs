using UnityEngine;

namespace Obi
{
    [AddComponentMenu("Physics/Obi/Obi Cloth", 900)]
    [RequireComponent(typeof(MeshFilter))]
    public class ObiCloth : ObiClothBase, IVolumeConstraintsUser, ITetherConstraintsUser
    {
        [SerializeField] protected ObiClothBlueprint m_ClothBlueprint;

        // volume constraints:
        [SerializeField] protected bool _volumeConstraintsEnabled = true;
        [SerializeField] protected float _compressionCompliance = 0;
        [SerializeField] protected float _pressure = 1;

        // tethers
        [SerializeField] protected bool _tetherConstraintsEnabled = true;
        [SerializeField] protected float _tetherCompliance = 0;
        [SerializeField] [Range(0.1f, 2)]protected float _tetherScale = 1;

        [SerializeField] protected ObiClothRenderer m_renderer;

        public override ObiActorBlueprint sourceBlueprint
        {
            get { return m_ClothBlueprint; }
        }

        /// <summary>  
        /// The base cloth blueprint used by this actor.
        /// </summary>
        public override ObiClothBlueprintBase clothBlueprintBase
        {
            get { return m_ClothBlueprint; }
        }

        /// <summary>  
        /// Whether this actor's volume constraints are enabled.
        /// </summary>
        public bool volumeConstraintsEnabled
        {
            get { return _volumeConstraintsEnabled; }
            set { if (value != _volumeConstraintsEnabled) { _tetherConstraintsEnabled = value; SetConstraintsDirty(Oni.ConstraintType.Volume); } }
        }

        /// <summary>  
        /// Compliance of this actor's volume constraints.
        /// </summary>
        public float compressionCompliance
        {
            get { return _compressionCompliance; }
            set { _compressionCompliance = value; SetConstraintsDirty(Oni.ConstraintType.Volume); }
        }

        /// <summary>  
        /// Pressure multiplier applied by volume constraints.
        /// </summary>
        public float pressure
        {
            get { return _pressure; }
            set { _pressure = value; SetConstraintsDirty(Oni.ConstraintType.Volume); }
        }

        /// <summary>  
        /// Whether this actor's tether constraints are enabled.
        /// </summary>
        public bool tetherConstraintsEnabled
        {
            get { return _tetherConstraintsEnabled; }
            set { if (value != _tetherConstraintsEnabled) { _tetherConstraintsEnabled = value; SetConstraintsDirty(Oni.ConstraintType.Tether); } }
        }

        /// <summary>  
        /// Compliance of this actor's tether constraints.
        /// </summary>
        public float tetherCompliance
        {
            get { return _tetherCompliance; }
            set { _tetherCompliance = value; SetConstraintsDirty(Oni.ConstraintType.Tether); }
        }

        /// <summary>  
        /// Rest length scaling for this actor's tether constraints.
        /// </summary>
        public float tetherScale
        {
            get { return _tetherScale; }
            set { _tetherScale = value; SetConstraintsDirty(Oni.ConstraintType.Tether); }
        }

        public ObiClothBlueprint clothBlueprint
        {
            get { return m_ClothBlueprint; }
            set{
                if (m_ClothBlueprint != value)
                {
                    RemoveFromSolver();
                    ClearState();
                    m_ClothBlueprint = value;
                    AddToSolver();
                }
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            SetupRuntimeConstraints();
        }

        private void SetupRuntimeConstraints()
        {
            SetConstraintsDirty(Oni.ConstraintType.Distance);
            SetConstraintsDirty(Oni.ConstraintType.Bending);
            SetConstraintsDirty(Oni.ConstraintType.Aerodynamics);
            SetConstraintsDirty(Oni.ConstraintType.Volume);
            SetConstraintsDirty(Oni.ConstraintType.Tether);
            SetSelfCollisions(m_SelfCollisions);
            SetSimplicesDirty();
        }

    }

}