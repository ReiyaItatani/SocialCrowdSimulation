using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CollisionAvoidance{

/// <summary>
/// This script is responsible for calculating the social forces applied to each agent.
/// </summary>
public class ForceSolver : BasePathController
{
    // --------------------------------------------------------------------------
    // Collision Avoidance Force ------------------------------------------------
    [Header("Parameters For Basic Collision Avoidance")]
    [HideInInspector] [ReadOnly] public GameObject currentAvoidanceTarget;
    protected Vector3 avoidanceVector = Vector3.zero; // Direction of basic collision avoidance
    [HideInInspector] public float avoidanceWeight = 2.0f; // Weight for basic collision avoidance

    // --------------------------------------------------------------------------
    // Collision Response -------------------------------------------------------
    public event EventDelegate OnMutualGaze;
    public delegate void EventDelegate(GameObject targetAgent);

    // --------------------------------------------------------------------------
    // To Goal Direction --------------------------------------------------------
    [Header("Parameters For Goal Direction")]
    protected Vector3 currentGoal;
    protected Vector3 toGoalVector = Vector3.zero; // Direction to goal
    [HideInInspector] public float toGoalWeight = 2.0f; // Weight for goal direction

    // --------------------------------------------------------------------------
    // Anticipated Collision Avoidance -------------------------------------------
    [Header("Parameters For Anticipated Collision Avoidance")]
    [HideInInspector] [ReadOnly] public GameObject potentialAvoidanceTarget;
    protected Vector3 avoidNeighborsVector = Vector3.zero; // Direction for anticipated collision avoidance
    [HideInInspector] public float avoidNeighborWeight = 2.0f; // Weight for anticipated collision avoidance
    protected float minTimeToCollision = 5.0f;
    protected float collisionDangerThreshold = 4.0f;

    // --------------------------------------------------------------------------
    // Social Behavior and Non-verbal Communication -----------------------------
    [Header("Social Behaviour, Non-verbal Communication")]
    protected bool onCollide = false;
    protected bool onMoving = false;
    protected GameObject collidedAgent;
    // --------------------------------------------------------------------------
    // Force From Group ---------------------------------------------------------
    [Header("Group Force, Group Category")]
    [ReadOnly] public string groupName;
    [HideInInspector] public float groupForceWeight = 0.5f;
    protected Vector3 groupForce = Vector3.zero;

    // --------------------------------------------------------------------------
    // Repulsion Force From Wall ------------------------------------------------
    protected Vector3 wallRepForce;
    [HideInInspector] public float wallRepForceWeight = 0.2f;

    // --------------------------------------------------------------------------
    // Group Collider Manager For Group Behaviour -------------------------------
    public GroupManager groupManager;

    protected virtual void InitForceSolver(){
        // CurrentPosition = agentPathManager.PrevTargetNodePosition;
        currentGoal     = agentPathManager.CurrentTargetNodePosition;  
    }

    protected virtual void StartUpdateForce(){
        StartCoroutine(UpdateToGoalVector(0.1f));
        StartCoroutine(UpdateAvoidanceVector(0.1f, 0.3f));
        StartCoroutine(UpdateAvoidNeighborsVector(0.1f, 0.3f));
        StartCoroutine(UpdateGroupForce(0.1f, GetGroupName()));
        StartCoroutine(UpdateWallForce(0.2f, 0.5f));
    }

    #region UPDATE SIMULATION
    protected override void OnUpdate(){
        // Predict the future positions and directions
        for (int i = 0; i < NumberPredictionPos; i++)
        {
            SimulatePath(DatabaseDeltaTime * TrajectoryPosPredictionFrames[i], CurrentPosition, out PredictedPositions[i], out PredictedDirections[i]);
        }

        //Update Current Position and Direction
        SimulatePath(Time.deltaTime, CurrentPosition, out CurrentPosition, out CurrentDirection);

        //Prevent agents from intersection
        base.OnUpdate();
    }

    protected virtual void SimulatePath(float time, Vector3 _currentPosition, out Vector3 nextPosition, out Vector3 direction)
    {
        //Gradually decrease speed
        // float distanceToGoal = Vector3.Distance(_currentPosition, currentGoal);
        // currentSpeed = distanceToGoal < slowingRadius ? Mathf.Lerp(minSpeed, currentSpeed, distanceToGoal / slowingRadius) : currentSpeed;

        //Move Agent
        direction = (      toGoalWeight    *            toGoalVector + 
                        avoidanceWeight    *         avoidanceVector + 
                    avoidNeighborWeight    *    avoidNeighborsVector + 
                    groupForceWeight       *              groupForce +
                    wallRepForceWeight     *            wallRepForce
                    ).normalized;
        direction = new Vector3(direction.x, 0f, direction.z);

        //Check collision
        if(onCollide){
            Vector3    myDir = GetCurrentDirection();
            Vector3    myPos = GetCurrentPosition();
            Vector3 otherDir = collidedAgent.GetComponent<IParameterManager>().GetCurrentDirection();
            Vector3 otherPos = collidedAgent.GetComponent<IParameterManager>().GetCurrentPosition();
            Vector3   offset = otherPos - myPos;

            offset = new Vector3(offset.x, 0f, offset.z);
            float dotProduct = Vector3.Dot(myDir, otherDir);
            float angle = 0.1f;

            if(onMoving){
                if (dotProduct <= -angle){
                    //anti-parallel
                    bool isParallel = false;
                    direction = CheckOppoentDir(myDir, myPos, otherDir, otherPos, out isParallel);
                    nextPosition = _currentPosition + direction * 0.1f * time;
                }else{
                    //parallel
                    if(Vector3.Dot(offset, GetCurrentDirection()) > 0){
                        //If the other agent is in front of you
                        nextPosition = _currentPosition;
                    }else{
                        //If you are in front of the other agent
                        nextPosition = _currentPosition + direction * CurrentSpeed * time;
                    }
                }
            }else{
                //Take a step back
                float speedOfStepBack = 0.3f;
                nextPosition = _currentPosition - offset * speedOfStepBack * time;
            }
        }else{
            nextPosition = _currentPosition + direction * CurrentSpeed * time;
        }
    }

    protected virtual Vector3 CheckOppoentDir(Vector3 myDirection, Vector3 myPosition, Vector3 otherDirection, Vector3 otherPosition, out bool isParallel){
        Vector3 offset = (otherPosition - myPosition).normalized;
        Vector3 right= Vector3.Cross(Vector3.up, offset).normalized;
        if(Vector3.Dot(right, myDirection)>0 && Vector3.Dot(right, otherDirection)>0 || Vector3.Dot(right, myDirection)<0 && Vector3.Dot(right, otherDirection)<0){
            //Potential to collide
            isParallel = true;
            return GetReflectionVector(myDirection, offset);
        }
        isParallel = false;
        return myDirection;
    }

    protected virtual Vector3 GetReflectionVector(Vector3 targetVector, Vector3 baseVector)
    {
        targetVector = targetVector.normalized;
        baseVector = baseVector.normalized;
        float cosTheta = Vector3.Dot(targetVector, baseVector); // p・x = cos θ
        Vector3 q = 2 * cosTheta * baseVector - targetVector;   // q = 2cos θ・x - p
        return q;
    }
    #endregion

    /**********************************************************************************************
    * Goal Direction Update:
    * This section of the code is responsible for recalculating and adjusting the target direction.
    * It ensures that the object is always oriented or moving towards its intended goal or target.
    ***********************************************************************************************/
    #region TO GOAL FORCE
    protected virtual IEnumerator UpdateToGoalVector(float updateTime){
        while(true){
            toGoalVector = (GetCurrentGoal() - (Vector3)GetCurrentPosition()).normalized;
            yield return new WaitForSeconds(updateTime);
        }
    }

    #endregion

    /***********************************************************************************************
    * Collision Avoidance Logic[Nuria HiDAC]:
    * This section of the code ensures that objects do not overlap or intersect with each other.
    * It provides basic mechanisms to detect potential collisions and take preventive actions.
    ***********************************************************************************************/
    #region BASIC COLLISION AVOIDANCE FORCE
    protected virtual IEnumerator UpdateAvoidanceVector(float updateTime, float transitionTime)
    {
        float elapsedTime = 0.0f;
        while(true){
            //To use Box Collider
            //List<GameObject> othersInAvoidanceArea = collisionAvoidance.GetOthersInAvoidanceArea();
            //To use FOV
            List<GameObject> othersInAvoidanceArea = collisionAvoidance.GetOthersInFOV();
            List<GameObject> othersOnPath          = collisionAvoidance.GetOthersInAvoidanceArea();
            Vector3 myPositionAtNearestApproach    = Vector3.zero;
            Vector3 otherPositionAtNearestApproach = Vector3.zero;

            //Update CurrentAvoidance Target
            if(othersInAvoidanceArea != null){
                currentAvoidanceTarget = DecideUrgentAvoidanceTarget(GetCurrentDirection(), GetCurrentPosition(), GetCurrentSpeed(), 
                                                                     othersInAvoidanceArea, 
                                                                     minTimeToCollision, collisionDangerThreshold, 
                                                                     out myPositionAtNearestApproach, out otherPositionAtNearestApproach);  
                //Check if the CurrentAvoidance Target is on the path way
                if(!othersOnPath.Contains(currentAvoidanceTarget)){
                    //if the CurrentAvoidance Target is not on the path way
                    currentAvoidanceTarget = null;
                }            
            }

            //Calculate Avoidance Force
            if (currentAvoidanceTarget != null)
            {
                Vector3 currentPosition          = GetCurrentPosition();
                Vector3 currentDirection         = GetCurrentDirection();
                Vector3 avoidanceTargetPosition  = currentAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentPosition();
                Vector3 avoidanceTargetAvoidanceVector = currentAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentAvoidanceVector();

                avoidanceVector = ComputeAvoidanceVector(currentAvoidanceTarget, currentDirection, currentPosition);

                //Check opponent dir
                if(avoidanceTargetAvoidanceVector != Vector3.zero && Vector3.Dot(currentDirection, avoidanceVector) < 0.5){
                    bool isParallel = false;
                    avoidanceVector = CheckOppoentDir(avoidanceVector, currentPosition, avoidanceTargetAvoidanceVector ,avoidanceTargetPosition, 
                                                      out isParallel);
                    if(isParallel){
                        OnMutualGaze?.Invoke(currentAvoidanceTarget);
                    }
                }

                //gradually increase the avoidance force considering the distance 
                Vector3 colliderSize = collisionAvoidance.GetAvoidanceColliderSize();
                float   agentRadius  = collisionAvoidance.GetAgentCollider().radius;
                avoidanceVector = avoidanceVector*(1.0f-Vector3.Distance(avoidanceTargetPosition, 
                                                                         currentPosition)/(Mathf.Sqrt(colliderSize.x/2*colliderSize.x/2+colliderSize.z*colliderSize.z)+agentRadius*2));

                //Group or Individual
                avoidanceVector *= TagChecker(currentAvoidanceTarget);
                
                elapsedTime = 0.0f;
            }
            else
            {
                elapsedTime += Time.deltaTime;
                if(transitionTime > elapsedTime){
                    avoidanceVector = Vector3.Lerp(avoidanceVector, Vector3.zero, elapsedTime/transitionTime);
                    yield return new WaitForSeconds(Time.deltaTime);
                }else{
                    avoidanceVector = Vector3.zero;
                    elapsedTime = 0.0f;
                }
            }
            yield return new WaitForSeconds(updateTime);
        }
    }

    protected virtual Vector3 ComputeAvoidanceVector(GameObject avoidanceTarget, Vector3 _currentDirection, Vector3 _currentPosition)
    {
        Vector3 directionToAvoidanceTarget = (avoidanceTarget.transform.position - _currentPosition).normalized;
        Vector3 upVector;

        //0.9748 → approximately 13.4 degrees.
        if (Vector3.Dot(directionToAvoidanceTarget, _currentDirection) >= 0.9748f)
        {
            upVector = Vector3.up;
        }
        else
        {
            upVector = Vector3.Cross(directionToAvoidanceTarget, _currentDirection);
        }
        return Vector3.Cross(upVector, directionToAvoidanceTarget).normalized;
    }

    protected virtual float TagChecker(GameObject Target){
        if(Target.CompareTag("Group")){
            Target.GetComponent<CapsuleCollider>();
            float radius = Target.GetComponent<CapsuleCollider>().radius;
            return radius + 2f;
        }else if(Target.CompareTag("Agent")){
            return 1f;
        }
        return 1f;
    }

    protected virtual GameObject DecideUrgentAvoidanceTarget(Vector3 myDirection, Vector3 myPosition, float mySpeed, List<GameObject> others, float minTimeToCollision, float collisionDangerThreshold, out Vector3 myPositionAtNearestApproach, out Vector3 otherPositionAtNearestApproach){
        GameObject _currentAvoidanceTarget = null;
        myPositionAtNearestApproach = Vector3.zero;
        otherPositionAtNearestApproach = Vector3.zero;
        foreach(GameObject other in others){
            //Skip self
            if(other == collisionAvoidance.GetAgentGameObject()){
                continue;
            }
            IParameterManager otherParameterManager = other.GetComponent<IParameterManager>();
            Vector3 otherDirection = otherParameterManager.GetCurrentDirection();
            Vector3 otherPosition  = otherParameterManager.GetCurrentPosition();
            float   otherSpeed     = otherParameterManager.GetCurrentSpeed();

            // predicted time until nearest approach of "me" and "other"
            float time = PredictNearestApproachTime (myDirection, myPosition, mySpeed, 
                                                     otherDirection, otherPosition, otherSpeed);
            //Debug.Log("time:"+time);
            if ((time >= 0) && (time < minTimeToCollision)){
                //Debug.Log("Distance:"+computeNearestApproachPositions (time, CurrentPosition, CurrentDirection, CurrentSpeed, otherParameterManager.GetRawCurrentPosition(), otherParameterManager.GetCurrentDirection(), otherParameterManager.GetCurrentSpeed()));
                if (ComputeNearestApproachPositions (time, 
                                                     myPosition, myDirection, mySpeed, 
                                                     otherPosition, otherDirection, otherSpeed, 
                                                     out myPositionAtNearestApproach, out otherPositionAtNearestApproach) 
                                                     < collisionDangerThreshold)
                {
                    minTimeToCollision = time;
                    _currentAvoidanceTarget = other;
                }
            }
        }
        return _currentAvoidanceTarget;
    }

    #endregion

    /***********************************************************************************************************
    * Anticipated Collision Avoidance[Reynolds 1987]:
    * This section of the code handles scenarios where objects might collide in the future(prediction).
    ************************************************************************************************************/
    #region ANTICIPATED COLLISION AVOIDANCE
    protected virtual IEnumerator UpdateAvoidNeighborsVector(float updateTime, float transitionTime){
        while(true){
            if(currentAvoidanceTarget != null){
                avoidNeighborsVector = Vector3.zero;
            }else{

                //Get Agents//
                List<GameObject> Agents = new List<GameObject>();
                if(groupManager!=null && groupManager.GetOnGroupCollider()){
                    //if the agent is in a group and in certain distance, shared FOV happens
                    Agents = groupManager.GetAgentsInSharedFOV();
                }else{
                    //if the agent is "not" in a group
                    Agents = collisionAvoidance.GetOthersInFOV();
                }
                if(Agents == null) yield return null;

                //Calculate Anticipated Collision Avoidance Force//
                Vector3 newAvoidNeighborsVector;
                if(groupManager!=null && groupManager.GetOnGroupCollider()){
                    //if the agent is in a group and in certain distance
                    Vector3 groupCurrentDirection = groupManager.GetGroupParameterManager().GetCurrentDirection();
                    Vector3 groupCurrentPosition  = groupManager.GetGroupParameterManager().GetCurrentPosition();
                    float   groupCurrentSpeed     = groupManager.GetGroupParameterManager().GetCurrentSpeed();

                    newAvoidNeighborsVector       = SteerToAvoidNeighbors(groupCurrentDirection, groupCurrentPosition, groupCurrentSpeed,
                                                                          Agents, minTimeToCollision, collisionDangerThreshold);
                }else{
                    //If the agent is "not" in a group
                    newAvoidNeighborsVector       = SteerToAvoidNeighbors(GetCurrentDirection(), GetCurrentPosition(), GetCurrentSpeed(), Agents, 
                                                                          minTimeToCollision, collisionDangerThreshold);
                }

                //Check if the agent is a part of a group, then the agent will have more strong force to avoid collision//
                if(potentialAvoidanceTarget != null){
                    newAvoidNeighborsVector *= TagChecker(potentialAvoidanceTarget);
                }
                yield return StartCoroutine(AvoidNeighborsVectorGradualTransition(transitionTime, avoidNeighborsVector, newAvoidNeighborsVector));
            }
            yield return new WaitForSeconds(updateTime);
        }
    }

    protected virtual Vector3 SteerToAvoidNeighbors (Vector3 myDirection, Vector3 myPosition, float mySpeed, List<GameObject> others, float minTimeToCollision, float collisionDangerThreshold)
    {
        float steer = 0;
        // potentialAvoidanceTarget = null;
        Vector3 myPositionAtNearestApproach    = Vector3.zero;
        Vector3 otherPositionAtNearestApproach = Vector3.zero;
        potentialAvoidanceTarget = DecideUrgentAvoidanceTarget(myDirection, myPosition, mySpeed, 
                                                               others, 
                                                               minTimeToCollision, collisionDangerThreshold, 
                                                               out myPositionAtNearestApproach, out otherPositionAtNearestApproach);

        if(potentialAvoidanceTarget != null){
            // parallel: +1, perpendicular: 0, anti-parallel: -1
            float parallelness = Vector3.Dot(myDirection, potentialAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentDirection());
            float angle = 0.707f;

            if (parallelness < -angle)
            {
                // anti-parallel "head on" paths:
                // steer away from future threat position
                Vector3 offset = otherPositionAtNearestApproach - myPosition;
                Vector3 rightVector = Vector3.Cross(myDirection, Vector3.up);

                float sideDot = Vector3.Dot(offset, rightVector);
                //If there is the predicted potential collision agent on your right side:SideDot>0, steer should be -1(left side)
                steer = (sideDot > 0) ? -1.0f : 1.0f;
            }
            else
            {
                if (parallelness > angle)
                {
                    // parallel paths: steer away from threat
                    Vector3 offset = potentialAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentPosition() - myPosition;
                    Vector3 rightVector = Vector3.Cross(myDirection, Vector3.up);

                    float sideDot = Vector3.Dot(offset, rightVector);
                    steer = (sideDot > 0) ? -1.0f : 1.0f;
                }
                else
                {
                    // perpendicular paths: steer behind threat
                    // (only the slower of the two does this)
                    // if(potentialAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentSpeed() <= GetCurrentSpeed()){
                    //     Vector3 rightVector = Vector3.Cross(myDirection, Vector3.up);
                    //     float sideDot = Vector3.Dot(rightVector, potentialAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentDirection());
                    //     steer = (sideDot > 0) ? -1.0f : 1.0f;
                    // }

                    if(GetCurrentSpeed() <= potentialAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentSpeed()){
                        Vector3 rightVector = Vector3.Cross(myDirection, Vector3.up);
                        float sideDot = Vector3.Dot(rightVector, potentialAvoidanceTarget.GetComponent<IParameterManager>().GetCurrentDirection());
                        steer = (sideDot > 0) ? -1.0f : 1.0f;
                    }
                }
            }
        }
        return Vector3.Cross(myDirection, Vector3.up) * steer;
    }

    protected virtual float PredictNearestApproachTime (Vector3 myDirection, Vector3 myPosition, float mySpeed, Vector3 otherDirection, Vector3 otherPosition, float otherSpeed)
    {
        Vector3 relVelocity = otherDirection*otherSpeed - myDirection*mySpeed;
        float      relSpeed = relVelocity.magnitude;
        Vector3  relTangent = relVelocity / relSpeed;
        Vector3 relPosition = myPosition - otherPosition;
        float    projection = Vector3.Dot(relTangent, relPosition); 

        if (relSpeed == 0) return 0;

        return projection / relSpeed;
    }

    protected virtual float ComputeNearestApproachPositions (float time, Vector3 myPosition, Vector3 myDirection, float mySpeed, Vector3 otherPosition, Vector3 otherDirection, float otherSpeed, out Vector3 myPositionAtNearestApproach, out Vector3 otherPositionAtNearestApproach)
    {
        Vector3    myTravel = myDirection * mySpeed * time;
        Vector3     myFinal =  myPosition +    myTravel;
        Vector3 otherTravel = otherDirection * otherSpeed * time;
        Vector3  otherFinal = otherPosition + otherTravel;

        // xxx for annotation
        myPositionAtNearestApproach = myFinal;
        otherPositionAtNearestApproach = otherFinal;

        return Vector3.Distance(myFinal, otherFinal);
    }

    protected virtual IEnumerator AvoidNeighborsVectorGradualTransition(float duration, Vector3 initialVector, Vector3 targetVector){
        float elapsedTime = 0.0f;
        Vector3 initialavoidNeighborsVector = initialVector;
        while(elapsedTime < duration){
            elapsedTime += Time.deltaTime;
            avoidNeighborsVector = Vector3.Slerp(initialavoidNeighborsVector, targetVector, elapsedTime/duration);
            yield return new WaitForSeconds(Time.deltaTime);
        }
        avoidNeighborsVector = targetVector;

        yield return null;
    }
    #endregion

    /******************************************************************************************************************************
    * Force from Group[Moussaid et al. (2010)]:
    * This section of the code calculates the collective force exerted by or on a group of objects.
    * It takes into account the interactions and influences of multiple objects within a group to determine the overall force or direction.
    ********************************************************************************************************************************/
    #region GROUP FORCE
    //protected float socialInteractionWeight = 1.0f;
    protected float cohesionWeight = 2.0f;
    protected float repulsionForceWeight = 1.5f;
    protected float alignmentForceWeight = 1.5f;

    protected virtual IEnumerator UpdateGroupForce(float updateTime, string  _groupName){
        if(_groupName == "Individual"){
            groupForce = Vector3.zero;
        }else{
            List<GameObject> groupAgents = groupManager.GetGroupAgents();
            CapsuleCollider  agentCollider = collisionAvoidance.GetAgentCollider();
            float              agentRadius = agentCollider.radius;
            GameObject     agentGameObject = collisionAvoidance.GetAgentGameObject();

            while(true){
                Vector3  _currentPosition = GetCurrentPosition();   
                Vector3 _currentDirection = GetCurrentDirection();  

                Vector3    cohesionForce = CalculateCohesionForce (groupAgents, cohesionWeight,       agentGameObject, _currentPosition);
                Vector3   repulsionForce = CalculateRepulsionForce(groupAgents, repulsionForceWeight, agentGameObject, _currentPosition, agentRadius);
                Vector3   alignmentForce = CalculateAlignment     (groupAgents, alignmentForceWeight, agentGameObject, _currentDirection, agentRadius);
                Vector3    newGroupForce = (cohesionForce + repulsionForce + alignmentForce).normalized;

                //Vector3 AdjustPosForce = Vector3.zero;
                //Vector3  headDirection = socialBehaviour.GetCurrentLookAt();
                // if(headDirection!=null){
                //     float GazeAngle = CalculateGazingAngle(groupAgents, _currentPosition, headDirection, fieldOfView);
                //     AdjustPosForce = CalculateAdjustPosForce(socialInteractionWeight, GazeAngle, headDirection);
                // }
                //Vector3 newGroupForce = (AdjustPosForce + cohesionForce + repulsionForce + alignmentForce).normalized;

                StartCoroutine(GroupForceGradualTransition(updateTime, groupForce, newGroupForce));

                yield return new WaitForSeconds(updateTime);
            }
        }
    }

    protected virtual float CalculateGazingAngle(List<GameObject> groupAgents, Vector3 currentPos, Vector3 currentDir, float angleLimit, GameObject myself)
    {
        Vector3            centerOfMass = CalculateCenterOfMass(groupAgents, myself);
        Vector3 directionToCenterOfMass = centerOfMass - currentPos;

        float             angle = Vector3.Angle(currentDir, directionToCenterOfMass);
        float neckRotationAngle = 0f;

        if (angle > angleLimit)
        {
            neckRotationAngle = angle - angleLimit;
        }

        return neckRotationAngle;
    }

    protected virtual Vector3 CalculateAdjustPosForce(float socialInteractionWeight, float headRot, Vector3 currentDir){
        float adjustment = 0.05f;
        return -socialInteractionWeight * headRot * currentDir *adjustment;
    }

    protected virtual Vector3 CalculateCohesionForce(List<GameObject> groupAgents, float cohesionWeight, GameObject myself, Vector3 currentPos){
        //float threshold = (groupAgents.Count-1)/2;
        //float threshold = (groupAgents.Count)/2;
        float safetyDistance = 0.05f;
        float threshold = groupAgents.Count * 0.3f + safetyDistance;
        Vector3 centerOfMass = CalculateCenterOfMass(groupAgents, myself);
        float dist = Vector3.Distance(currentPos, centerOfMass);
        float judgeWithinThreshold = 0;
        if(dist > threshold){
            judgeWithinThreshold = 1;
        }
        Vector3 toCenterOfMassDir = (centerOfMass - currentPos).normalized;

        return judgeWithinThreshold*cohesionWeight*toCenterOfMassDir;
    }

    protected virtual Vector3 CalculateRepulsionForce(List<GameObject> groupAgents, float repulsionForceWeight, GameObject myself, Vector3 currentPos, float agentRadius){
        Vector3 repulsionForceDir = Vector3.zero;
        foreach(GameObject agent in groupAgents){
            //skip myselfVector3.Cross
            if(agent == myself) continue;
            Vector3 toOtherDir = agent.transform.position - currentPos;
            float dist = Vector3.Distance(currentPos, agent.transform.position);
            float threshold = 0;
            float safetyDistance = 0.05f;
            if(dist < 2*agentRadius + safetyDistance){
                threshold = 1.0f / dist;
            }
            toOtherDir = toOtherDir.normalized;
            repulsionForceDir += threshold*repulsionForceWeight*toOtherDir;
        }
        return -repulsionForceDir;
    }

    protected virtual Vector3 CalculateAlignment(List<GameObject> groupAgents, float alignmentForceWeight, GameObject myself, Vector3 currentDirection, float agentRadius){
        Vector3 steering = Vector3.zero;
        int neighborsCount = 0;

        foreach (GameObject go in groupAgents)
        {
            if (go != myself)
            {
                Vector3 otherDirection = go.GetComponent<IParameterManager>().GetCurrentDirection();
                steering += otherDirection;
                neighborsCount++;
            }
            // float alignmentAngle  = 0.7f;
            // if (InBoidNeighborhood(go, myself, agentRadius * 3, agentRadius * 6, alignmentAngle, currentDirection))
            // {
            //     Vector3 otherDirection = go.GetComponent<IParameterManager>().GetCurrentDirection();
            //     steering += otherDirection;
            //     neighborsCount++;
            // }
        }

        if (neighborsCount > 0)
        {
            //steering =  ((steering / neighborsCount) - currentDirection).normalized;
            steering = steering * alignmentForceWeight;
        }

        return steering * alignmentForceWeight;
    }

    protected virtual bool InBoidNeighborhood(GameObject other, GameObject myself, float minDistance, float maxDistance, float cosMaxAngle, Vector3 currentDirection){
        if (other == myself)
        {
            return false;
        }
        else
        {
            float dist = Vector3.Distance(other.transform.position, myself.transform.position);
            Vector3 offset = other.transform.position - myself.transform.position;

            if (dist < minDistance)
            {
                return true;
            }
            else if (dist > maxDistance)
            {
                return false;
            }
            else
            {
                Vector3 unitOffset = offset.normalized;
                float forwardness = Vector3.Dot(currentDirection, unitOffset);
                return forwardness > cosMaxAngle;
            }
        }
    }

    protected virtual Vector3 CalculateCenterOfMass(List<GameObject> groupAgents, GameObject myself)
    {
        if (groupAgents == null || groupAgents.Count == 0)
        {
            return Vector3.zero;
        }

        Vector3 sumOfPositions = Vector3.zero;
        int count = 0;

        foreach (GameObject go in groupAgents)
        {
            if (go != null && go != myself) 
            {
                sumOfPositions += go.transform.position;
                count++; 
            }
        }

        if (count == 0) 
        {
            return Vector3.zero;
        }

        return sumOfPositions / count;
    }

    protected virtual IEnumerator GroupForceGradualTransition(float duration, Vector3 initialVector, Vector3 targetVector){
        float elapsedTime = 0.0f;
        Vector3 initialGroupForce = initialVector;
        while(elapsedTime < duration){
            elapsedTime += Time.deltaTime;
            groupForce = Vector3.Slerp(initialGroupForce, targetVector, elapsedTime/duration);
            yield return new WaitForSeconds(Time.deltaTime);
        }
        groupForce = targetVector;

        yield return null;
    }
    #endregion

    /********************************************************************************************************************************
    * Wall force[Nuria HiDAC]:
    * This section of the code is dedicated to modifying the speed of an object based on certain conditions or criteria.
    * It ensures that the object maintains an appropriate speed, possibly in response to environmental factors, obstacles, or other objects.
    ********************************************************************************************************************************/
    #region WALL FORCE
    protected virtual IEnumerator UpdateWallForce(float updateTime, float transitionTime){
        while(true){
            GameObject currentWallTarget = collisionAvoidance.GetCurrentWallTarget();
            if(currentWallTarget != null){
                NormalVector normalVector = currentWallTarget.GetComponent<NormalVector>();
                wallRepForce = normalVector.CalculateNormalVectorFromWall(GetCurrentPosition());
            }else{
                yield return StartCoroutine(WallForceGradualTransition(transitionTime, wallRepForce, Vector3.zero));
            }
            yield return new WaitForSeconds(updateTime);
        }
    }

    protected virtual IEnumerator WallForceGradualTransition(float duration, Vector3 initialVector, Vector3 targetVector){
        float elapsedTime = 0.0f;
        Vector3 initialWallForce = initialVector;
        while(elapsedTime < duration){
            elapsedTime += Time.deltaTime;
            wallRepForce = Vector3.Slerp(initialWallForce, targetVector, elapsedTime/duration);
            yield return new WaitForSeconds(Time.deltaTime);
        }
        wallRepForce = targetVector;

        yield return null;
    }
    #endregion
    
    /********************************************************************************************************************************
    * Get and Set Methods:
    * This section of the code contains methods to retrieve (get) and update (set) the values of properties or attributes of an object.
    * These methods ensure controlled access and potential validation when changing the state of the object.
    ********************************************************************************************************************************/
    #region GET AND SET
    public Vector3 GetCurrentGoal(){
        return agentPathManager.CurrentTargetNodePosition;
    }
    public string GetGroupName(){
        return groupName;
    }
    public List<GameObject> GetGroupAgents(){
        if(groupManager == null) return null;
        return groupManager.GetGroupAgents();
    }
    public GameObject GetPotentialAvoidanceTarget()
    {
        return potentialAvoidanceTarget;
    }
    public Vector3 GetCurrentAvoidanceVector(){
        return avoidanceVector;
    }
    public CollisionAvoidanceController GetCollisionAvoidanceController(){
        return collisionAvoidance;
    }
    #endregion
}
}
