using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace CollisionAvoidance
{
public class GazeController : CrowdSimulationMonoBehaviour
{
    public enum CurrentLookTarget{
        CollidedTarget,
        CurerntAvoidancetarget,
        MyDirection,
        CenterOfMass,
        CoordinationTarget,
        CustomFocalPoint
    }
    public Animator targetAnimator;
    protected Transform t_Neck;

    // GazeState channel from the pipeline coordinator
    private GazeState gazeState;

    // Serialized reference so we can re-resolve gazeState on Play mode entry
    [SerializeField, HideInInspector]
    private AgentPipelineCoordinator coordinator;

    public MotionMatchingSkinnedMeshRenderer targetRenderer;
    public AvatarParameterProxy avatarParameterProxy;

    //For experiment
    public bool onNeckRotation = true;

    private bool initialized = false;
    private Coroutine neckStateCoroutine;

    /// <summary>
    /// Single entry point for all dependencies. Called by AgentPrefabFactory / AvatarCreatorQuickGraph.
    /// Serializable refs (targetAnimator, targetRenderer, avatarParameterProxy, coordinator) survive
    /// Edit→Play transitions. Non-serializable state (gazeState, events, coroutine) is rebuilt in Awake.
    /// </summary>
    public virtual void InitializeDependencies(Animator anim, MotionMatchingSkinnedMeshRenderer renderer, AvatarParameterProxy pm, AgentPipelineCoordinator coord = null)
    {
        targetAnimator = anim;
        targetRenderer = renderer;
        avatarParameterProxy = pm;
        coordinator = coord;

        // Resolve non-serializable state from serialized refs
        SetupRuntimeState();
    }

    /// <summary>
    /// Rebuilds all non-serializable state from serialized references.
    /// Called from InitializeDependencies (Edit mode) and Awake (Play mode).
    /// </summary>
    private void SetupRuntimeState()
    {
        // Resolve t_Neck from Animator
        if (targetAnimator != null)
        {
            t_Neck = targetAnimator.GetBoneTransform(HumanBodyBones.Neck);
        }

        // Resolve gazeState from coordinator
        if (coordinator != null)
        {
            gazeState = coordinator.GazeState;
        }

        // Setup social relations
        if (avatarParameterProxy != null)
        {
            SetupSocialRelations();
        }

        // Subscribe to the gaze event
        if (targetRenderer != null)
        {
            targetRenderer.OnUpdateGaze -= UpdateGaze;
            targetRenderer.OnUpdateGaze += UpdateGaze;
        }

        // Start coroutine (only works in Play mode, safe to call in Edit mode — just won't run)
        if (neckStateCoroutine != null) StopCoroutine(neckStateCoroutine);
        if (Application.isPlaying)
        {
            neckStateCoroutine = StartCoroutine(UpdateNeckState(2.0f));
        }

        initialized = true;
    }

    /// <summary>
    /// Awake runs at Play mode entry. Re-resolves non-serializable state from serialized refs.
    /// </summary>
    protected virtual void Awake()
    {
        // Fallback: find coordinator from hierarchy if serialized ref is missing (old prefab)
        if (coordinator == null)
        {
            coordinator = GetComponentInParent<AgentPipelineCoordinator>();
        }

        // Re-initialize if any serialized ref exists
        if (targetAnimator != null || targetRenderer != null || coordinator != null)
        {
            SetupRuntimeState();
        }
    }

    private void SetupSocialRelations()
    {
        string mySocialRelations = avatarParameterProxy.GetGroupName();
        if(mySocialRelations == "Individual"){
            ifIndividual = true;
        }
    }

    protected virtual void OnDisable()
    {
        if (targetRenderer != null)
        {
            targetRenderer.OnUpdateGaze -= UpdateGaze;
        }
    }

    protected virtual void OnEnable()
    {
        // Re-subscribe after disable/enable cycle (only if already initialized)
        if (initialized && targetRenderer != null)
        {
            targetRenderer.OnUpdateGaze -= UpdateGaze;
            targetRenderer.OnUpdateGaze += UpdateGaze;
        }
        if (initialized && neckStateCoroutine == null && Application.isPlaying)
        {
            neckStateCoroutine = StartCoroutine(UpdateNeckState(2.0f));
        }
    }

    public virtual void UpdateGaze(object sender, EventArgs e)
    {
        if (t_Neck == null) return;
        AdjustVerticalEyeLevelPass();

        // Determine horizontal attraction point from GazeState or legacy path
        if (gazeState != null && onNeckRotation)
        {
            // Read desired direction from pipeline's GazeState channel
            Vector3 desiredDir = gazeState.DesiredLookAtDirection;
            horizontalAttractionPoint = desiredDir != Vector3.zero
                ? desiredDir.normalized
                : GetAgentForward();

            if (gazeState.HasExplicitTarget)
            {
                // Map GazePriority to CurrentLookTarget for debug display
                if (gazeState.TargetPriority >= GazePriority.Collision)
                    currentLookTarget = CurrentLookTarget.CollidedTarget;
                else if (gazeState.TargetPriority >= GazePriority.Decision)
                    currentLookTarget = CurrentLookTarget.CoordinationTarget;
                else if (gazeState.TargetPriority >= GazePriority.Prediction)
                    currentLookTarget = CurrentLookTarget.CurerntAvoidancetarget;
                else if (gazeState.TargetObject != null)
                    currentLookTarget = CurrentLookTarget.CustomFocalPoint;
                else
                    currentLookTarget = CurrentLookTarget.CenterOfMass;
            }
            else
            {
                currentLookTarget = CurrentLookTarget.MyDirection;
            }

            if (lookForward)
            {
                horizontalAttractionPoint = gazeState.DesiredLookAtDirection.normalized;
                currentLookTarget = CurrentLookTarget.MyDirection;
            }
        }
        else if (onNeckRotation)
        {
            // Legacy fallback when coordinator not available
            LookAtAttractionPointUpdater();
        }

        UpdateCurrentLookAtSave();
        HorizontalLookAtPass(currentLookAt, horizontalAttractionPoint, UnityEngine.Random.Range(0.3f, 1.0f));
        LookAtAdjustmentPass(neckRotationLimit);

        // Write back to GazeState for FOV feedback loop
        if (gazeState != null)
        {
            gazeState.CurrentLookAtDirection = currentLookAt;
            gazeState.SmoothedNeckRotation = saveLookAtRot;
        }
    }

    /// <summary>
    /// Returns the agent's visual forward direction from the Hips bone.
    /// MotionMatching drives the skeleton directly, so the Animator root transform
    /// stays at world-forward. The Hips bone reflects the actual character orientation.
    /// </summary>
    private Vector3 GetAgentForward()
    {
        if (targetAnimator != null)
        {
            Transform hips = targetAnimator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips != null) return hips.forward;
        }
        return Vector3.forward;
    }

    #region LOOK AT PASS
    /* * *
    *
    * LOOK AT PASS
    *
    * * */
    [Header("Look At Params")]
    [ReadOnly]
    public CurrentLookTarget currentLookTarget;
    protected Vector3 horizontalAttractionPoint;
    protected Quaternion saveLookAtRot = Quaternion.identity;
    protected Vector3 currentLookAt = Vector3.zero;
    protected bool ifIndividual = false;
    protected float neckRotationLimit = 40.0f;


    protected virtual void HorizontalLookAtPass(Vector3 currentLookAtDir, Vector3 targetLookAtDir, float rotationSpeed){
        if (currentLookAtDir == Vector3.zero || targetLookAtDir == Vector3.zero) return;
        Vector3 crossResult = Vector3.Cross(currentLookAtDir, targetLookAtDir);
        if (crossResult.y > 0)
        {
            saveLookAtRot *= Quaternion.Euler(0, rotationSpeed, 0);
        }
        else if (crossResult.y < 0)
        {
            saveLookAtRot *= Quaternion.Euler(0, -rotationSpeed, 0);
        }
        t_Neck.localRotation *= saveLookAtRot;
    }

    /// <summary>
    /// Fallback when GazeState is not available (no pipeline coordinator).
    /// </summary>
    protected virtual void LookAtAttractionPointUpdater(){
        horizontalAttractionPoint = GetAgentForward();
        currentLookTarget = CurrentLookTarget.MyDirection;
    }
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if(targetAnimator == null) return;
        Transform headBone = targetAnimator.GetBoneTransform(HumanBodyBones.Head);
        if(headBone == null) return;
        Vector3 eyePosition = headBone.position;
        float lineLength = 1.0f;
        float sphereSize = 0.02f;

        // 1. Attraction point (target) — color-coded by current look target type
        Color targetColor = GetGizmoColorForTarget(currentLookTarget);
        Gizmos.color = targetColor;
        Vector3 attractionDir = horizontalAttractionPoint.normalized;
        if (attractionDir != Vector3.zero)
        {
            Vector3 attractionEnd = eyePosition + new Vector3(attractionDir.x, 0f, attractionDir.z) * lineLength;
            Gizmos.DrawLine(eyePosition, attractionEnd);
            Gizmos.DrawSphere(attractionEnd, sphereSize);
        }

        // 2. Agent visual forward (magenta)
        Gizmos.color = Color.magenta;
        Vector3 fwd = GetAgentForward();
        Vector3 fwdEnd = eyePosition + new Vector3(fwd.x, 0f, fwd.z).normalized * lineLength;
        Gizmos.DrawLine(eyePosition, fwdEnd);
        Gizmos.DrawSphere(fwdEnd, sphereSize * 0.7f);

        // 3. Label showing debug info
        Handles.color = targetColor;
        string label = currentLookTarget.ToString();
        if (gazeState != null)
        {
            Vector3 d = gazeState.DesiredLookAtDirection;
            float angle = Vector3.SignedAngle(fwd, d, Vector3.up);
            label += lookForward ? " [LookFwd]" : "";
            label += $"\nDir:({d.x:F2},{d.z:F2}) ({angle:F0} deg)";
            label += gazeState.HasExplicitTarget ? $" P:{gazeState.TargetPriority}" : " NoTarget";
        }
        else
        {
            label += "\n[gazeState: NULL]";
        }
        Handles.Label(eyePosition + Vector3.up * 0.3f, label);
    }

    private static Color GetGizmoColorForTarget(CurrentLookTarget target)
    {
        switch (target)
        {
            case CurrentLookTarget.CollidedTarget:        return Color.red;
            case CurrentLookTarget.CurerntAvoidancetarget: return Color.yellow;
            case CurrentLookTarget.CoordinationTarget:    return Color.cyan;
            case CurrentLookTarget.CustomFocalPoint:      return Color.blue;
            case CurrentLookTarget.CenterOfMass:          return Color.white;
            case CurrentLookTarget.MyDirection:            return new Color(1f, 0.5f, 0f); // orange
            default:                                       return Color.magenta;
        }
    }
#endif

    //call this in fixed update
    protected virtual IEnumerator UpdateNeckState(float updateTime){

        while(true){
            Vector3 lookDir = gazeState != null ? gazeState.CurrentLookAtDirection : GetCurrentLookAt();
            Vector3 agentDir = gazeState != null ? gazeState.DesiredLookAtDirection : GetCurrentAgentDirection();
            CheckNeckRotation(lookDir, agentDir, neckRotationLimit);
            yield return new WaitForSeconds(updateTime);
        }
    }

    protected virtual void CheckNeckRotation(Vector3 _currentLookAt, Vector3 myDirection, float _neckRotationLimit, float lookAtForwardDuration = 2.0f, float probability = 0.5f){
        float currentNeckRotation = Vector3.Angle(_currentLookAt.normalized, myDirection.normalized);
        if(UnityEngine.Random.Range(0.0f, 1.0f) < probability){
            if(currentNeckRotation >= _neckRotationLimit && lookForward == false){
                StartCoroutine(TemporalLookAtForward(lookAtForwardDuration));
            }
        }
    }

    protected bool lookForward = false;
    protected virtual IEnumerator TemporalLookAtForward(float duration){
        if(lookForward == false){
            lookForward = true;
            yield return new WaitForSeconds(duration);
            lookForward = false;
        }
        yield return null;
    }

    protected virtual void UpdateCurrentLookAtSave(float angleLimit = 40.0f){
        saveLookAtRot = LimitRotation(saveLookAtRot, angleLimit);
        currentLookAt = saveLookAtRot * t_Neck.forward;
    }

    public Vector3 GetCurrentLookAt(){
        return currentLookAt;
    }

    public Vector3 GetCurrentAgentDirection(){
        return gazeState != null ? gazeState.DesiredLookAtDirection : Vector3.forward;
    }

    protected virtual void AdjustVerticalEyeLevelPass(){
        Vector3 horizontalForward = Vector3.Scale(t_Neck.forward, new Vector3(1, 0, 1)).normalized;
        Quaternion horizontalRotation = Quaternion.LookRotation(horizontalForward, Vector3.up);
        t_Neck.localRotation *= Quaternion.Inverse(t_Neck.rotation) * horizontalRotation;
    }

    protected virtual void LookAtAdjustmentPass(float angleLimit = 40.0f){
        t_Neck.localRotation = LimitRotation(t_Neck.localRotation, angleLimit);
    }
    public static Quaternion LimitRotation(Quaternion rotation, float angleLimit)
    {
        Vector3 eulerRotation = rotation.eulerAngles;

        //eulerRotation.x = ClampAngle(eulerRotation.x, angleLimit);
        eulerRotation.y = ClampAngle(eulerRotation.y, angleLimit);
        //eulerRotation.z = ClampAngle(eulerRotation.z, angleLimit);

        return Quaternion.Euler(eulerRotation);
    }

    protected static float ClampAngle(float angle, float limit)
    {
        if (angle > 180f) angle -= 360f;

        return Mathf.Clamp(angle, -limit, limit);
    }
    #endregion
}
}
