using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements.Experimental;

public class AIController : MonoBehaviour
{
    // Target the AI will detect and chase whatever has the label "Player"
    [SerializeField] GameObject target;

    // The vision of the AI
    [SerializeField] GameObject eyeArea;

    // Vision configuration
    // how far the AI can see
    [SerializeField] float viewDistance = 10f;
    // field of view in degrees
    [SerializeField] float viewAngle = 90f;      

    // Hearing configuration
    [SerializeField] float hearingDistance = 5f;

    // Last position where the AI saw the target
    Vector3 lastKnownTargetPos;
    bool hasLastKnownPosition = false;

    // How long the AI will search the last known position before giving up
    [SerializeField] float investigateDuration = 5f;

    // Timer for how long the AI has been investigating
    float investigateTimer = 0f;

    // how close before it counts to arrive at investigation point
    [SerializeField] float investigateArrivalThreshold = 1.0f;

    // How close is "caught"
    [SerializeField] float catchDistance = 1.5f;


    // Pathfinding component used to move on the NavMesh
    NavMeshAgent agent;

    // World-space label to show the current AI state
    [SerializeField] TMPro.TextMeshPro textMeshPro;

    // Current state of the AI (see AIStates enum below)
    [SerializeField] AIStates state = AIStates.PATROL;

    // Patrol configuration
    [SerializeField] GameObject[] patrolPoints;
    // index of current patrol point
    [SerializeField] int patrolIndex = 0;
    // time to wait at each point
    [SerializeField] float waitTimeSec = 2f;
    // accumulated wait time at current point
    [SerializeField] float timeSpentAtPoint = 0f;


    

    void Start()
    {
        // NavMeshAgent used for pathfinding
        agent = GetComponent<NavMeshAgent>();

        // check to catch setup errors in the editor
        if (agent == null)
            Debug.LogWarning($"{name}: No NavMeshAgent found on this GameObject.");

        if (eyeArea == null)
            Debug.LogWarning($"{name}: eyeArea is not assigned.");

        if (textMeshPro == null)
            Debug.LogWarning($"{name}: textMeshPro is not assigned.");

        if (patrolPoints == null || patrolPoints.Length == 0)
            Debug.LogWarning($"{name}: patrolPoints is empty. PATROL state will be no-op.");
    }


    void Update()
    {
        // Update the floating label (if present)
        SetLabel();

        // Run sensing logic (vision, hearing)
        ScanTarget();

        // Run behaviour for the current AI state
        switch (state)
        {
            case AIStates.PATROL:
                HandlePatrol();
                break;

            case AIStates.INVESTIGATE:
                HandleInvestigate();
                break;

            case AIStates.CHASE:
                HandleChase();
                break;
        }
    }


   
    private void SetLabel()
    {
        // Displays the current AI state above the character as text
        string stateStr = state switch
        {
            AIStates.PATROL => "Patrol",
            AIStates.INVESTIGATE => "Investigate",
            AIStates.CHASE => "Chase",
            _ => "N/A",
        };

        if (textMeshPro == null)
            return;

        textMeshPro.SetText(stateStr);

        // Make the label face the camera so it is easy to read
        //if (Camera.main != null)
        //{
        //    Transform t = textMeshPro.transform;
        //    t.LookAt(Camera.main.transform);
        //}


    }


    
    private void HandlePatrol()
    {
        // Move between patrol points, pausing at each for waitTimeSec seconds

        // Skip if patrol is not configured or agent is missing
        if (patrolPoints == null || patrolPoints.Length == 0 || agent == null)
            return;

        // Keep index within bounds of the array
        patrolIndex = Mathf.Clamp(patrolIndex, 0, Mathf.Max(0, patrolPoints.Length - 1));

        GameObject patrolPoint = patrolPoints[patrolIndex];
        if (patrolPoint == null)
            return;

        float distance = Vector3.Distance(transform.position, patrolPoint.transform.position);

        if (distance < 2f)
        {
            // Already at this point, therefore the AI waits
            timeSpentAtPoint += Time.deltaTime;

            // Once AI waited long enough it advance to the next point
            if (timeSpentAtPoint >= waitTimeSec)
            {
                // (index + 1) % length wraps back to 0 when we reach the end
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                // reset wait timer
                timeSpentAtPoint = 0f;
            }
        }
        else
        {
            //  keep moving towards this patrol point if its not reached yet
            agent.SetDestination(patrolPoint.transform.position);
        }
    }

    private void HandleChase()
    {
        // if agent doesnt exist or we have no target, skip
        if (agent == null)
            return;

        if (target == null)
        {
            state = AIStates.PATROL;
            return;
        }

        // Does the AI have line of sight to the target
        GameObject visible = HasLineOfSight(target);

        if (visible != null && visible.CompareTag("Player"))
        {
            // Still see the player -> keep chasing, update last known pos
            lastKnownTargetPos = target.transform.position;
            hasLastKnownPosition = true;

            agent.SetDestination(target.transform.position);

            // Check for "caught"
            float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
            if (distanceToTarget <= catchDistance)
            {
                Debug.Log($"{name}: Caught the player!");
                // TODO: trigger game over / restart here
                state = AIStates.PATROL;
                target = null;
                hasLastKnownPosition = false;
            }
        }
        else
        {
            // Lost line of sight change state to INVESTIGATE if we have a last known position
            if (hasLastKnownPosition)
            {
                state = AIStates.INVESTIGATE;
                investigateTimer = 0f;
                agent.SetDestination(lastKnownTargetPos);
            }
            else
            {
                // if there is no last known position, go back to PATROL
                state = AIStates.PATROL;
                target = null;
            }
        }
    }


    private void HandleInvestigate()
    {
        // this method makes the AI go to the last known position of the target
        if (agent == null)
            return;

        if (!hasLastKnownPosition)
        {
            // Nothing to investigate
            state = AIStates.PATROL;
            return;
        }

        float dist = Vector3.Distance(transform.position, lastKnownTargetPos);

        // Go to the last known position of the target
        if (dist > investigateArrivalThreshold)
        {
            // keep heading to the last known position
            agent.SetDestination(lastKnownTargetPos);

            // dont count time until we arrive
            investigateTimer = 0f;
            return;
        }

        // at the last known position, stand and wait
        investigateTimer += Time.deltaTime;

        if (investigateTimer >= investigateDuration)
        {
            //After wait timer give up and go back to PATROL
            hasLastKnownPosition = false;
            target = null;
            state = AIStates.PATROL;
        }
    }



    private void ScanTarget()
    {
        // Run all sensing logic (vision now, hearing later)
        LookForTarget();
        // ListenForTarget(); // not implemented yet
    }


    
    private void LookForTarget()
    {
        // Scan for the player using a vision cone
        if (eyeArea == null)
            return;

        // Get all colliders within our view radius
        Collider[] objs = Physics.OverlapSphere(transform.position, viewDistance);

        foreach (Collider obj in objs)
        {
            // Ignore nulls and AI collider
            if (obj == null || obj.gameObject == gameObject)
                continue;

            Transform targetObj = obj.transform;

            // Direction from our eyes to this potential target
            Vector3 targetDir = (targetObj.position - eyeArea.transform.position).normalized;

            // Only consider objects inside AI field of view
            if (Vector3.Angle(eyeArea.transform.forward, targetDir) < viewAngle * 0.5f)
            {
                // Confirm that nothing blocks the view
                GameObject potentialTarget = HasLineOfSight(obj.gameObject);

                // If we can see the target, lock on and switch to chase
                if (potentialTarget != null && potentialTarget.CompareTag("Player"))
                {
                    target = potentialTarget.transform.root.gameObject;

                    // store a snapshot of where we saw the player
                    lastKnownTargetPos = potentialTarget.transform.position;
                    hasLastKnownPosition = true;

                    // always go into chase when we see the player
                    state = AIStates.CHASE;

                    // no need to check any further colliders this frame
                    return;
                }

            }
        }
    }


    
    private GameObject HasLineOfSight(GameObject losTarget)
    {
        // Casts a ray from the eyes to the target to check for an unobstructed line of sight
        if (eyeArea == null || losTarget == null)
            return null;

        Vector3 origin = eyeArea.transform.position;
        Vector3 dir = losTarget.transform.position - origin;

        // Avoid zero length direction
        if (dir.sqrMagnitude < Mathf.Epsilon)
            return null;

        RaycastHit hit;
        bool didHit = Physics.Raycast(origin, dir.normalized, out hit, viewDistance);

        // Visualise the ray in the Scene view
        Debug.DrawRay(origin, dir, Color.cyan);

        if (!didHit || hit.collider == null)
            return null;

        // Ensure the collider we hit belongs to the intended target
        if (hit.collider.transform.root == losTarget.transform.root)
        {
            // Another FOV check to be extra strict about angle
            Vector3 targetDir = (losTarget.transform.position - eyeArea.transform.position).normalized;
            if (Vector3.Angle(eyeArea.transform.forward, targetDir) < viewAngle * 0.5f)
                return hit.collider.gameObject;
        }

        return null;
    }


    private void ListenForTarget()
    {
        // pass for now
    }


   
    private void OnDrawGizmosSelected()
    {
        // Draw debug gizmos in the editor to show vision and hearing ranges
        // Vision radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        // Hearing radius
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, hearingDistance);

        if (eyeArea == null)
            return;

        // Direction vectors for the left/right edges of the vision cone
        Vector3 rightBoundary = GetDirectionFromAngle(viewAngle / 2f, false) * viewDistance;
        Vector3 leftBoundary = GetDirectionFromAngle(-viewAngle / 2f, false) * viewDistance;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(eyeArea.transform.position, eyeArea.transform.position + rightBoundary);
        Gizmos.DrawLine(eyeArea.transform.position, eyeArea.transform.position + leftBoundary);

        // Draw forward direction for reference
        Gizmos.color = Color.green;
        Gizmos.DrawLine(eyeArea.transform.position,
                        eyeArea.transform.position + eyeArea.transform.forward * viewDistance);
    }


    
    private Vector3 GetDirectionFromAngle(float angleInDegrees, bool isGlobal)
    {
        // Convert an angle in degrees into a direction vector on the XZ plane
        float angle = angleInDegrees;

        // If using local space, offset by the yaw which is the  Y rotation of the eye or AI
        if (!isGlobal)
        {
            float yaw = (eyeArea != null) ? eyeArea.transform.eulerAngles.y
                                          : transform.eulerAngles.y;
            angle += yaw;
        }

        float rad = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
    }
}

public enum AIStates
{
    PATROL,
    INVESTIGATE,
    CHASE
}
