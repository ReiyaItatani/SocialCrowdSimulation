using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

namespace CollisionAvoidance
{

public enum UpperBodyAnimationState
{
    // The 'Walking' animation state.
    // Used for activities within a group, e.g., smoking, holding a bug, carrying luggage.
    // Utilizes Motion Matching technology.
    Walk,

    // The 'Talking' animation state.
    // Used for communication within a group.
    // Utilizes Unity's animation system.
    Talk,

    // The 'Using Smartphone' animation state.
    // Used for individual activities, e.g., listening to music, texting, making calls.
    // Utilizes Unity's animation system.
    SmartPhone
}

/// <summary>
/// Manages animation states for a character.
/// Gaze logic lives in the pipeline's GazeState channel.
/// Responsibilities:
///   - Animation state management (Walk/Talk/SmartPhone)
///   - CustomFocalPoints (serialized list, wired to SocialGazeFilter by coordinator)
/// </summary>
public class SocialBehaviour : MonoBehaviour
{
        private static int _animParamID_Walk = Animator.StringToHash(UpperBodyAnimationState.Walk.ToString());
        private static int _animParamID_Talk = Animator.StringToHash(UpperBodyAnimationState.Talk.ToString());
        private static int _animParamID_SmartPhone = Animator.StringToHash(UpperBodyAnimationState.SmartPhone.ToString());

    protected const float AnimationStateUpdateMinTime = 5.0f;
    protected const float AnimationStateUpdateMaxTime = 10.0f;
    [Range(0,1)]
    public float WalkAnimationProbability = 0.5f;

    [Header("Conversation")]
    public AudioSource audioSource;
    public AudioClip[] audioClips;

    [Header("Animation")]
    public MotionMatchingSkinnedMeshRenderer targetRenderer;
    protected AvatarMaskData initialAvatarMask;

    public AvatarParameterProxy targetParameterProxy;

    [Header("AnimationState")]
    [ReadOnly]
    public UpperBodyAnimationState currentAnimationState = UpperBodyAnimationState.Walk;
    public GameObject smartPhone;
    public Animator targetAnimator;
    protected bool onSmartPhone = true;

    protected bool isInitialized = false;
    public bool IsInitialized{
        get{
            return isInitialized;
        }
    }

    private static int GetAnimParamID(UpperBodyAnimationState state)
    {
        if (state == UpperBodyAnimationState.Walk)
        {
            return _animParamID_Walk;
        }

        if (state == UpperBodyAnimationState.Talk)
        {
            return _animParamID_Talk;
        }

        if (state == UpperBodyAnimationState.SmartPhone)
        {
            return _animParamID_SmartPhone;
        }

        return -1;
    }

    protected virtual void InitSocialBehaviour(){
        if(isInitialized == true){
            return;
        }

        if (targetRenderer != null)
        {
            initialAvatarMask = targetRenderer.AvatarMask;
        }

        if(smartPhone != null){
            SetSmartPhoneActiveBasedOnSocialRelations(smartPhone);
        }

        FollowMotionMatching();
        isInitialized = true;
    }

    public virtual void InitializeDependencies(Animator anim, MotionMatchingSkinnedMeshRenderer renderer, AvatarParameterProxy pm)
    {
        targetAnimator = anim;
        targetRenderer = renderer;
        targetParameterProxy = pm;
        
        InitSocialBehaviour();
    }

    protected virtual void Awake()
    {
        // For backwards compatibility: if components exist on the same GameObject, get them
        if (targetParameterProxy == null) targetParameterProxy = GetComponent<AvatarParameterProxy>();
        if (targetAnimator == null) targetAnimator = GetComponent<Animator>();
        if (targetRenderer == null) targetRenderer = GetComponent<MotionMatchingSkinnedMeshRenderer>();

        InitSocialBehaviour();
    }

    protected virtual void OnEnable()
    {
        StartCoroutine(UpdateAnimationState());
    }

    protected virtual void OnDisable()
    {
        StopAllCoroutines();
    }

    #region Animation State Control
    /// <summary>
    /// Continuously updates the current animation state based on social relations and group members.
    /// </summary>
    protected virtual IEnumerator UpdateAnimationState()
    {
        while (true)
        {
            List<GameObject> groupAgents = targetParameterProxy.GetGroupAgents();
            //Determine Random Animation State based on social relations
            currentAnimationState = DetermineAnimationState(groupAgents);

            bool isCurrentlyTalking = currentAnimationState == UpperBodyAnimationState.Talk;
            if(isCurrentlyTalking && groupAgents != null && groupAgents.Count > 1){
                bool areAgentsClose = AreAgentsAndSelfCloseToAveragePos(groupAgents, gameObject);
                currentAnimationState = areAgentsClose ? UpperBodyAnimationState.Talk : UpperBodyAnimationState.Walk;
            }

            TriggerUnityAnimation(currentAnimationState);

            if (currentAnimationState == UpperBodyAnimationState.Walk)
            {
                FollowMotionMatching();
            }

            yield return new WaitForSeconds(UnityEngine.Random.Range(AnimationStateUpdateMinTime, AnimationStateUpdateMaxTime));
        }
    }

    protected virtual UpperBodyAnimationState DetermineAnimationState(List<GameObject> groupAgents)
    {
        bool isIndividual = targetParameterProxy.GetGroupName() == "Individual" || groupAgents == null || groupAgents.Count <= 1;
        return UnityEngine.Random.value < WalkAnimationProbability ? UpperBodyAnimationState.Walk : (isIndividual ? UpperBodyAnimationState.SmartPhone : UpperBodyAnimationState.Talk);
    }

    protected virtual void FollowMotionMatching()
    {
        TriggerUnityAnimation(UpperBodyAnimationState.Walk);
        if (targetRenderer != null) targetRenderer.AvatarMask = null;
    }

    protected virtual void TriggerUnityAnimation(UpperBodyAnimationState animationState)
    {
        //Update current animation state
        currentAnimationState = animationState;
        if (targetRenderer != null) targetRenderer.AvatarMask = initialAvatarMask;

        foreach (UpperBodyAnimationState state in Enum.GetValues(typeof(UpperBodyAnimationState)))
        {
            if (targetAnimator != null) targetAnimator.SetBool(GetAnimParamID(state), state == animationState);
        }
        if(animationState == UpperBodyAnimationState.SmartPhone || animationState == UpperBodyAnimationState.Talk){
            TryPlayAudio(0.0f);
        }
    }

    protected virtual void SetSmartPhoneActiveBasedOnSocialRelations(GameObject smartPhone)
    {
        bool isIndividual = targetParameterProxy.GetGroupName() == "Individual";

        if(isIndividual){
            smartPhone.SetActive(true);
            onSmartPhone = true;
        }
        else{
            smartPhone.SetActive(false);
            onSmartPhone = false;
        }
    }

    public virtual UpperBodyAnimationState GetUpperBodyAnimationState(){
        return currentAnimationState;
    }

    /// <summary>
    /// Determines if the calling object (self) and a group of agents are all sufficiently close to their common average pos.
    /// The distance threshold is set to half the number of agents in the group. If all agents and the self are within this
    /// threshold from the average pos, the function returns true; otherwise, it returns false.
    /// </summary>
    /// <param name="groupAgents">List of agent GameObjects to be checked.</param>
    /// <returns>True if all agents and self are close to the average pos, false otherwise.</returns>
    protected virtual bool AreAgentsAndSelfCloseToAveragePos(List<GameObject> groupAgents, GameObject myself)
    {
        // Calculate the center of mass, including the calling object itself
        Vector3 averagePos = CalculateAveragePosition(groupAgents);

        // Set the distance threshold to half the number of agents
        float agentRadius = 0.3f;
        float safetyDistance = 0.1f;
        float thresholdDistance =agentRadius * groupAgents.Count + safetyDistance;

        // Check if the calling object (self) is within the threshold distance from the center of mass
        if (Vector3.Distance(myself.transform.position, averagePos) > thresholdDistance)
        {
            return false;
        }

        // Check if at least one agent in the group (excluding myself) is within the threshold distance from the average position
        bool isAnyAgentClose = false;
        foreach (GameObject agent in groupAgents)
        {
            if (agent != myself && Vector3.Distance(agent.transform.position, averagePos) <= thresholdDistance)
            {
                isAnyAgentClose = true;
                break;
            }
        }

        // If at least one agent (excluding myself) is close, and myself is also close, return true
        if (isAnyAgentClose)
        {
            return true;
        }

        // If no agent (excluding myself) is close enough, return false
        return false;

    }

    protected virtual Vector3 CalculateAveragePosition(List<GameObject> agents)
    {
        Vector3 combinedPosition = Vector3.zero;
        foreach (GameObject agent in agents)
        {
            combinedPosition += agent.transform.position;
        }
        return combinedPosition / agents.Count;
    }

    #endregion

    public virtual void TryPlayAudio(float PlayAudioProbability)
    {
        if (audioSource != null && audioClips.Length > 0 && UnityEngine.Random.value < PlayAudioProbability)
        {
            audioSource.clip = audioClips[UnityEngine.Random.Range(0, audioClips.Length)];
            audioSource.Play();
        }
    }

    #region GET and SET
    public virtual bool GetOnSmartPhone(){
        return onSmartPhone;
    }

    public List<GameObject> CustomFocalPoints = new List<GameObject>();

    #endregion
}
}
