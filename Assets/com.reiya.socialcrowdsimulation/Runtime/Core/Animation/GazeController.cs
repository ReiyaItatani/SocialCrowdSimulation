using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CollisionAvoidance{
[RequireComponent(typeof(ParameterManager))]
[RequireComponent(typeof(SocialBehaviour))]
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
    protected SkinnedMeshRenderer meshRenderer;
    protected Animator animator;
    protected Transform t_Neck;

    protected SocialBehaviour socialBehaviour;

    protected MotionMatchingSkinnedMeshRenderer motionMatchingSkinnedMeshRenderer;

    //For experiment
    public bool onNeckRotation = true;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        t_Neck = animator.GetBoneTransform(HumanBodyBones.Neck);

        socialBehaviour = GetComponent<SocialBehaviour>();

        ParameterManager parameterManager = GetComponent<ParameterManager>();
        string mySocialRelations = parameterManager.GetGroupName();
        if(mySocialRelations == "Individual"){
            ifIndividual = true;
        }

        GameObject body = FindObjectWithSkinnedMeshRenderer(gameObject);
        meshRenderer = body.GetComponentInChildren<SkinnedMeshRenderer>();
    }

    protected virtual void OnEnable()
    {
        //Subscribe the event
        motionMatchingSkinnedMeshRenderer = GetComponent<MotionMatchingSkinnedMeshRenderer>();
        motionMatchingSkinnedMeshRenderer.OnUpdateGaze += UpdateGaze;
        StartCoroutine(UpdateNeckState(2.0f));
    }

    protected virtual void OnDisable()
    {
        motionMatchingSkinnedMeshRenderer = GetComponent<MotionMatchingSkinnedMeshRenderer>();
        motionMatchingSkinnedMeshRenderer.OnUpdateGaze -= UpdateGaze;
        StopAllCoroutines();
    }

    GameObject FindObjectWithSkinnedMeshRenderer(GameObject parent)
    {
        if (parent.GetComponent<SkinnedMeshRenderer>() != null)
        {
            return parent;
        }

        foreach (Transform child in parent.transform)
        {
            GameObject found = FindObjectWithSkinnedMeshRenderer(child.gameObject);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    public virtual void UpdateGaze(object sender, EventArgs e)
    {
        ParameterUpdater();

        AdjustVerticalEyeLevelPass();
        //LookAt
        if(onNeckRotation){
            LookAtAttractionPointUpdater();
        }
        UpdateCurrentLookAtSave();
        HorizontalLookAtPass(currentLookAt, horizontalAttractionPoint, UnityEngine.Random.Range(0.3f, 1.0f));
        //LookAtAdjustmentPass
        LookAtAdjustmentPass(neckRotationLimit);
        //EyesMovement
        //Because AvatarSDK doesn't support blendshape, we need to check the target framework
#if MicrosoftRocketBox
        EyesMovementPass();
#endif
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
    protected Transform collidedTarget;
    protected Vector3 horizontalAttractionPoint;
    protected Quaternion saveLookAtRot = Quaternion.identity;
    protected Vector3 currentLookAt = Vector3.zero;
    protected bool ifIndividual = false;
    protected float neckRotationLimit = 40.0f;
    protected Vector3 currentCenterOfMass = Vector3.zero;
    protected Vector3 currentAvoidanceTarget = Vector3.zero;
    protected Vector3 currentAgentDirection = Vector3.zero;
    protected Transform avoidanceCoordinationTarget = null;
    protected Transform customFocalPoint = null;


    protected virtual void HorizontalLookAtPass(Vector3 currentLookAtDir, Vector3 targetLookAtDir, float rotationSpeed){
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

    //Custom Focal Point is for making agents look at a specific point(when they are in slowing radius area)
    protected virtual void ParameterUpdater(){
        //Update Params
        currentCenterOfMass         = socialBehaviour.GetCurrentCenterOfMass();
        currentAvoidanceTarget      = socialBehaviour.GetPotentialAvoidanceTarget();
        currentAgentDirection       = socialBehaviour.GetCurrentDirection();
        collidedTarget              = socialBehaviour.GetCollidedTarget();
        avoidanceCoordinationTarget = socialBehaviour.GetAvoidanceCoordinationTarget();
        customFocalPoint            = socialBehaviour.GetCustomFocalPoint();
    }

    protected virtual void LookAtAttractionPointUpdater(){
        if(collidedTarget != null){
            //when collide
            horizontalAttractionPoint = (collidedTarget.position - _cachedTransform.position).normalized;
            currentLookTarget = CurrentLookTarget.CollidedTarget;
        }else if(avoidanceCoordinationTarget != null){
            horizontalAttractionPoint = (avoidanceCoordinationTarget.position - _cachedTransform.position).normalized;
            currentLookTarget = CurrentLookTarget.CoordinationTarget;
        }else if(currentAvoidanceTarget != Vector3.zero){
            horizontalAttractionPoint = (currentAvoidanceTarget - _cachedTransform.position).normalized;
            currentLookTarget = CurrentLookTarget.CurerntAvoidancetarget;
        }else if(customFocalPoint != null){
            horizontalAttractionPoint = (customFocalPoint.position - _cachedTransform.position).normalized;
            currentLookTarget = CurrentLookTarget.CustomFocalPoint;
        }else{
            //in normal situation
            if (ifIndividual) {
                // if the agent is individual
                horizontalAttractionPoint = currentAgentDirection.normalized;
                currentLookTarget = CurrentLookTarget.MyDirection;
            } else{
                // if the agent is in a group
                horizontalAttractionPoint = currentCenterOfMass.normalized;
                currentLookTarget = CurrentLookTarget.CenterOfMass;
            }
            //checklookForward
            if(lookForward){
                horizontalAttractionPoint = currentAgentDirection.normalized;
                currentLookTarget = CurrentLookTarget.MyDirection;
            }
        }
    }
    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if(animator == null) return;
        Vector3 offset = new Vector3(0f, 0f, 0f);
        Vector3 eyePosition = animator.GetBoneTransform(HumanBodyBones.Head).transform.position + offset;
        Gizmos.color = Color.magenta;
        Vector3 targetPosition = _cachedTransform.position + horizontalAttractionPoint;
        Vector3 lineEndPoint = new Vector3(targetPosition.x, eyePosition.y, targetPosition.z);
        Gizmos.DrawLine(eyePosition, lineEndPoint);  
        float sphereSize = 0.02f; 
        Gizmos.DrawSphere(lineEndPoint, sphereSize);

        Gizmos.color = Color.green;
        Vector3 currentLookAt = _cachedTransform.position + GetCurrentLookAt();
        Vector3 currentLookAtEndPoint = new Vector3(currentLookAt.x, eyePosition.y, currentLookAt.z);
        Gizmos.DrawLine(eyePosition, currentLookAtEndPoint);  
        Gizmos.DrawSphere(currentLookAtEndPoint, sphereSize);
    }
    #endif

    //call this in fixed update
    protected virtual IEnumerator UpdateNeckState(float updateTime){

        while(true){
            CheckNeckRotation(GetCurrentLookAt(), GetCurrentAgentDirection(), neckRotationLimit);
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
        return currentAgentDirection;
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

    #region EYES PASS
    /* * *
    * 
    * EYES MOVEMENT PASS
    * 
    * * */

    protected int lookRight_Eyes = 112;
    protected int lookLeft_Eyes = 111;
    protected float blendValue;

    protected virtual void EyesMovementPass()
    {
        CalculateBlendValueBasedOnDirection(GetCurrentLookAt(), horizontalAttractionPoint);
    }

    protected virtual void ResetEyesBlendShape()
    {
        if (meshRenderer.GetBlendShapeWeight(lookRight_Eyes) > 0)
        {
            SetEyesBlendShape(lookRight_Eyes, blendValue);
        }
        else if (meshRenderer.GetBlendShapeWeight(lookLeft_Eyes) > 0)
        {
            SetEyesBlendShape(lookLeft_Eyes, blendValue);
        }
    }

    protected virtual void CalculateBlendValueBasedOnDirection(Vector3 currentDirection, Vector3 targetDirection)
    {
        float angle = Vector3.Angle(currentDirection, targetDirection);
        float sign = Mathf.Sign(Vector3.Cross(currentDirection, targetDirection).y);

        blendValue = Mathf.Clamp(angle / 90.0f * 100.0f, 0.0f, 100.0f);

        // Sign indicates targetDirection (1 for right, -1 for left)
        if (sign >= 0)
        {
            SetEyesBlendShape(lookLeft_Eyes, 0);
            SetEyesBlendShape(lookRight_Eyes, blendValue);
        }
        else
        {
            SetEyesBlendShape(lookLeft_Eyes, blendValue);
            SetEyesBlendShape(lookRight_Eyes, 0);
        }
    }

    protected virtual void SetEyesBlendShape(int blendShapeIndex, float value)
    {
        meshRenderer.SetBlendShapeWeight(blendShapeIndex, value);
    }

    protected virtual IEnumerator EyesWeightChanger(float originalWeight, float targetWeight, float duration)
    {
        float elapsedTime = 0;
        float initialWeight = originalWeight;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            blendValue = Mathf.Lerp(initialWeight, targetWeight, elapsedTime / duration);
            yield return new WaitForSeconds(Time.deltaTime);
        }

        blendValue = targetWeight;
    }
    #endregion


}
}