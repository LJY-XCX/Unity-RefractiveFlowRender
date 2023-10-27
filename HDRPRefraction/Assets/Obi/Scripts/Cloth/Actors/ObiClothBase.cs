using UnityEngine;
using System;
using System.Collections;

namespace Obi
{
    public abstract class ObiClothBase : ObiActor, IDistanceConstraintsUser, IBendConstraintsUser, IAerodynamicConstraintsUser
    {

        [SerializeField] protected bool m_SelfCollisions = false;
        [SerializeField] protected bool m_OneSided = false;

        // distance constraints:
        [SerializeField] protected bool _distanceConstraintsEnabled = true;
        [SerializeField] protected float _stretchingScale = 1;
        [SerializeField] protected float _stretchCompliance = 0;
        [SerializeField] [Range(0, 1)] protected float _maxCompression = 0;

        // bend constraints:
        [SerializeField] protected bool _bendConstraintsEnabled = true;
        [SerializeField] protected float _bendCompliance = 0;
        [SerializeField] [Range(0, 0.1f)] protected float _maxBending = 0;
        [SerializeField] [Range(0, 0.1f)] protected float _plasticYield = 0;
        [SerializeField] protected float _plasticCreep = 0;

        // aerodynamics
        [SerializeField] protected bool _aerodynamicsEnabled = true;
        [SerializeField] protected float _drag = 0.05f;
        [SerializeField] protected float _lift = 0.05f;

        [HideInInspector] [NonSerialized] protected int trianglesOffset = 0;   /**< Offset of deformable triangles in curent solver*/

        /// <summary>  
        /// The base cloth blueprint used by this actor.
        /// </summary> 
        public abstract ObiClothBlueprintBase clothBlueprintBase
        {
            get;
        }

        /// <summary>  
        /// Whether particles colliding against this cloth will be projected using the cloth's surface normal.
        /// </summary>
        public bool oneSided
        {
            get { return m_OneSided; }
            set {if (value != m_OneSided) { m_OneSided = value; SetOneSided(m_OneSided); } }
        }

        /// <summary>  
        /// Whether particles in this actor colide with particles using the same phase value.
        /// </summary>
        public bool selfCollisions
        {
            get { return m_SelfCollisions; }
            set { if (value != m_SelfCollisions) { m_SelfCollisions = value; SetSelfCollisions(m_SelfCollisions); } }
        }

        /// <summary>  
        /// Whether this actor's distance constraints are enabled.
        /// </summary>
        public bool distanceConstraintsEnabled
        {
            get { return _distanceConstraintsEnabled; }
            set { if (value != _distanceConstraintsEnabled) { _distanceConstraintsEnabled = value; SetConstraintsDirty(Oni.ConstraintType.Distance); } }
        }

        /// <summary>  
        /// Scale value for this actor's distance constraints rest length.
        /// </summary>
        /// The default is 1. For instamce, a value of 2 will make the distance constraints twice as long, 0.5 will reduce their length in half.
        public float stretchingScale
        {
            get { return _stretchingScale; }
            set { _stretchingScale = value; SetConstraintsDirty(Oni.ConstraintType.Distance); }
        }

        /// <summary>  
        /// Compliance of this actor's stretch constraints.
        /// </summary>
        public float stretchCompliance
        {
            get { return Mathf.Max(0,_stretchCompliance); }
            set { _stretchCompliance = value; SetConstraintsDirty(Oni.ConstraintType.Distance); }
        }

        /// <summary>  
        /// Maximum compression this actor's distance constraints can undergo.
        /// </summary>
        /// This is expressed as a percentage of the scaled rest length. 
        public float maxCompression
        {
            get { return _maxCompression; }
            set { _maxCompression = value; SetConstraintsDirty(Oni.ConstraintType.Distance); }
        }

        /// <summary>  
        ///  Whether this actor's bend constraints are enabled.
        /// </summary>
        public bool bendConstraintsEnabled
        {
            get { return _bendConstraintsEnabled; }
            set { if (value != _bendConstraintsEnabled) { _bendConstraintsEnabled = value; SetConstraintsDirty(Oni.ConstraintType.Bending); } }
        }

        /// <summary>  
        ///  Compliance of this actor's bend constraints.
        /// </summary>
        public float bendCompliance
        {
            get { return _bendCompliance; }
            set { _bendCompliance = value; SetConstraintsDirty(Oni.ConstraintType.Bending); }
        }

        /// <summary>  
        ///  Max bending value that constraints can undergo before resisting bending.
        /// </summary>
        public float maxBending
        {
            get { return _maxBending; }
            set { _maxBending = value; SetConstraintsDirty(Oni.ConstraintType.Bending); }
        }

        /// <summary>  
        /// Threshold for plastic behavior. 
        /// </summary>
        /// Once bending goes above this value, a percentage of the deformation (determined by <see cref="plasticCreep"/>) will be permanently absorbed into the cloth's rest shape.
        public float plasticYield
        {
            get { return _plasticYield; }
            set { _plasticYield = value; SetConstraintsDirty(Oni.ConstraintType.Bending); }
        }

        /// <summary>  
        /// Percentage of deformation that gets absorbed into the rest shape per second, once deformation goes above the <see cref="plasticYield"/> threshold.
        /// </summary>
        public float plasticCreep
        {
            get { return _plasticCreep; }
            set { _plasticCreep = value; SetConstraintsDirty(Oni.ConstraintType.Bending); }
        }

        /// <summary>  
        ///   Whether this actor's aerodynamic constraints are enabled.
        /// </summary>
        public bool aerodynamicsEnabled
        {
            get { return _aerodynamicsEnabled; }
            set { if (value != _aerodynamicsEnabled) { _aerodynamicsEnabled = value; SetConstraintsDirty(Oni.ConstraintType.Aerodynamics); } }
        }

        /// <summary>  
        /// Aerodynamic drag value.
        /// </summary>
        public float drag
        {
            get { return _drag; }
            set { _drag = value; SetConstraintsDirty(Oni.ConstraintType.Aerodynamics); }
        }

        /// <summary>  
        /// Aerodynamic lift value.
        /// </summary>
        public float lift
        {
            get { return _lift; }
            set { _lift = value; SetConstraintsDirty(Oni.ConstraintType.Aerodynamics); }
        }

        /// <summary>  
        /// Whether this actor applies external forces in a custom way. 
        /// </summary>
        /// In case of cloth, this is true as forces are interpreted as wind.
        public override bool usesCustomExternalForces
        {
            get { return true; }
        }

        public override void LoadBlueprint(ObiSolver solver)
        {
            base.LoadBlueprint(solver);

            // find our offset in the deformable triangles array.
            trianglesOffset = solver.implementation.GetDeformableTriangleCount();

            // Send deformable triangle indices to the solver:
            UpdateDeformableTriangles();

            SetSelfCollisions(m_SelfCollisions);
            SetOneSided(m_OneSided);
        }

        public override void UnloadBlueprint(ObiSolver solver)
        {
            int index = m_Solver.actors.IndexOf(this);

            if (index >= 0 && sourceBlueprint != null && clothBlueprintBase.deformableTriangles != null)
            {
                // remove triangles:
                solver.implementation.RemoveDeformableTriangles(clothBlueprintBase.deformableTriangles.Length / 3, trianglesOffset);

                // update all following actor's triangle offset:
                for (int i = index + 1; i < m_Solver.actors.Count; i++)
                {
                    ObiClothBase clothActor = solver.actors[i] as ObiClothBase;
                    if (clothActor != null)
                        clothActor.trianglesOffset -= clothBlueprintBase.deformableTriangles.Length / 3;
                }
            }

            base.UnloadBlueprint(solver);
        }

        public virtual void UpdateDeformableTriangles()
        {
            if (clothBlueprintBase != null && clothBlueprintBase.deformableTriangles != null)
            {
                // Send deformable triangle indices to the solver:
                int[] solverTriangles = new int[clothBlueprintBase.deformableTriangles.Length];
                for (int i = 0; i < clothBlueprintBase.deformableTriangles.Length; ++i)
                {
                    solverTriangles[i] = solverIndices[clothBlueprintBase.deformableTriangles[i]];
                }
                solver.implementation.SetDeformableTriangles(solverTriangles, solverTriangles.Length / 3, trianglesOffset);
            }
        }
    }
}
