using UnityEngine;
using Unity.Mathematics;
using MotionMatching;
using TrajectoryFeature = MotionMatching.MotionMatchingData.TrajectoryFeature;
using Unity.Collections;

namespace CollisionAvoidance{
    public class BasePathController : MotionMatchingCharacterController
    {
        // --------------------------------------------------------------------------
        // Motion Matching ----------------------------------------------------------
        [Header("Motion Matching Parameters")]
        public string TrajectoryPositionFeatureName = "FuturePosition";
        public string TrajectoryDirectionFeatureName = "FutureDirection";
        //Warning:current position is not the current position of the agent itself when the parent transform is not (0.0f, 0.0f, 0.0f);
        //To get current position of the agent you have to use GetCurrentPosition()
        protected Vector3[] PredictedPositions;
        protected Vector3[] PredictedDirections;
        [HideInInspector, Range(0.0f, 2.0f), Tooltip("Max distance between SimulationBone and SimulationObject")] 
        public float MaxDistanceMMAndCharacterController = 0.1f; // Max distance between SimulationBone and SimulationObject
        [HideInInspector, Range(0.0f, 2.0f), Tooltip("Time needed to move half of the distance between SimulationBone and SimulationObject")] 
        public float PositionAdjustmentHalflife = 0.1f; // Time needed to move half of the distance between SimulationBone and SimulationObject
        [HideInInspector, Range(0.0f, 2.0f), Tooltip("Ratio between the adjustment and the character's velocity to clamp the adjustment")] 
        public float PosMaximumAdjustmentRatio = 0.1f; // Ratio between the adjustment and the character's velocity to clamp the adjustment
        // --------------------------------------------------------------------------
        // Features for motion matching ---------------------------------------------
        protected int TrajectoryPosFeatureIndex;
        protected int TrajectoryRotFeatureIndex;
        protected int[] TrajectoryPosPredictionFrames;
        protected int[] TrajectoryRotPredictionFrames;
        protected int NumberPredictionPos { get { return TrajectoryPosPredictionFrames.Length; } }
        protected int NumberPredictionRot { get { return TrajectoryRotPredictionFrames.Length; } }
        // --------------------------------------------------------------------------
        // Collision Avoidance  -----------------------------------------------------
        [Header("Collision Avoidance Parameters")]
        public CollisionAvoidanceController collisionAvoidance;
        public AgentPathManager agentPathManager;
        protected Vector3 CurrentPosition;
        protected Vector3 CurrentDirection;
        protected float CurrentSpeed = 1.0f;

        protected virtual void InitMotionMathing(){
            // Get the feature indices
            TrajectoryPosFeatureIndex = -1;
            TrajectoryRotFeatureIndex = -1;
            for (int i = 0; i < MotionMatching.MMData.TrajectoryFeatures.Count; ++i)
            {
                if (MotionMatching.MMData.TrajectoryFeatures[i].Name == TrajectoryPositionFeatureName) TrajectoryPosFeatureIndex = i;
                if (MotionMatching.MMData.TrajectoryFeatures[i].Name == TrajectoryDirectionFeatureName) TrajectoryRotFeatureIndex = i;
            }
            Debug.Assert(TrajectoryPosFeatureIndex != -1, "Trajectory Position Feature not found");
            Debug.Assert(TrajectoryRotFeatureIndex != -1, "Trajectory Direction Feature not found");

            TrajectoryPosPredictionFrames = MotionMatching.MMData.TrajectoryFeatures[TrajectoryPosFeatureIndex].FramesPrediction;
            TrajectoryRotPredictionFrames = MotionMatching.MMData.TrajectoryFeatures[TrajectoryRotFeatureIndex].FramesPrediction;
            // TODO: generalize this, allow for different number of prediction frames
            Debug.Assert(TrajectoryPosPredictionFrames.Length == TrajectoryRotPredictionFrames.Length, "Trajectory Position and Trajectory Direction Prediction Frames must be the same for PathCharacterController");
            for (int i = 0; i < TrajectoryPosPredictionFrames.Length; ++i)
            {
                Debug.Assert(TrajectoryPosPredictionFrames[i] == TrajectoryRotPredictionFrames[i], "Trajectory Position and Trajectory Direction Prediction Frames must be the same for PathCharacterController");
            }

            PredictedPositions  = new Vector3[NumberPredictionPos];
            PredictedDirections = new Vector3[NumberPredictionRot];
        }

        protected override void OnUpdate(){
            //Prevent agents from intersection
            AdjustCharacterPosition();
            ClampSimulationBone();
        }

        protected virtual void AdjustCharacterPosition()
        {
            float3 characterController = GetCurrentPosition();
            float3 motionMatching = MotionMatching.transform.position;
            float3 differencePosition = characterController - motionMatching;
            // Damp the difference using the adjustment halflife and dt
            float3 adjustmentPosition = Spring.DampAdjustmentImplicit(differencePosition, PositionAdjustmentHalflife, Time.deltaTime);
            // Clamp adjustment if the length is greater than the character velocity
            // multiplied by the ratio
            float maxLength = PosMaximumAdjustmentRatio * math.length(MotionMatching.Velocity) * Time.deltaTime;
            if (math.length(adjustmentPosition) > maxLength)
            {
                adjustmentPosition = maxLength * math.normalize(adjustmentPosition);
            }
            // Move the simulation bone towards the simulation object
            MotionMatching.SetPosAdjustment(adjustmentPosition);
        }

        protected virtual void ClampSimulationBone()
        {
            // Clamp Position
            float3 characterController = GetCurrentPosition();
            float3 motionMatching = MotionMatching.transform.position;
            if (math.distance(characterController, motionMatching) > MaxDistanceMMAndCharacterController)
            {
                float3 newMotionMatchingPos = MaxDistanceMMAndCharacterController * math.normalize(motionMatching - characterController) + characterController;
                MotionMatching.SetPosAdjustment(newMotionMatchingPos - motionMatching);
            }
        }

#region GET AND SET
        public float3 GetCurrentPosition()
        {
            return transform.position + CurrentPosition;
        }
        public Vector3 GetCurrentDirection()
        {
            return CurrentDirection;
        }
        public float GetCurrentSpeed()
        {
            return CurrentSpeed;
        }
        public override float3 GetWorldInitPosition()
        {
            return agentPathManager.CurrentTargetNodePosition + this.transform.position;
        }
        public override float3 GetWorldInitDirection()
        {
            Vector3 dir = agentPathManager.CurrentTargetNodePosition - agentPathManager.PrevTargetNodePosition;
            return dir.normalized;
        }
        protected virtual Vector3 GetWorldPredictedPos(int index)
        {
            return PredictedPositions[index] + transform.position;
        }
        protected virtual Vector3 GetWorldPredictedDir(int index)
        {
            return PredictedDirections[index];
        }
        
        public override void GetTrajectoryFeature(TrajectoryFeature feature, int index, Transform character, NativeArray<float> output)
        {
            if (!feature.SimulationBone) Debug.Assert(false, "Trajectory should be computed using the SimulationBone");
            switch (feature.FeatureType)
            {
                case TrajectoryFeature.Type.Position:
                    Vector3 world = GetWorldPredictedPos(index);
                    Vector3 local = character.InverseTransformPoint(new Vector3(world.x, 0.0f, world.z));
                    output[0] = local.x;
                    output[1] = local.z;
                    break;
                case TrajectoryFeature.Type.Direction:
                    Vector3 worldDir = GetWorldPredictedDir(index);
                    Vector3 localDir = character.InverseTransformDirection(new Vector3(worldDir.x, 0.0f, worldDir.z));
                    output[0] = localDir.x;
                    output[1] = localDir.z;
                    break;
                default:
                    Debug.Assert(false, "Unknown feature type: " + feature.FeatureType);
                    break;
            }
        }
#endregion
    }
}
