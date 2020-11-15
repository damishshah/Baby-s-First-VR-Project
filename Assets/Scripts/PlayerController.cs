using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace Valve.VR.InteractionSystem
{
    public class PlayerController : MonoBehaviour
    {
        public GameObject wallPrefab;

        private const float openFingerAmount = 0.1f;
        private const float closedFingerAmount = 0.9f;
        private const float closedThumbAmount = 0.4f;
        private const float jumpSpeed = 10.0f;
        private const float handPositionMemoryInterval = 0.1f;
        private const int maxMemoryIntervalStorageSize = 10;
        private const float lookBackWindowInSeconds = 0.3f;
        private const float velocityThreshold = 0.3f;
        private const float verticalMoveDistanceThreshold = 0.35f;
        private const float punchDistanceThreshold = 0.25f;
        private const float minimalDistanceThreshold = 0.1f;
        private const float rockPunchActivationDistance = 0.6f;

        Player player;
        Rigidbody playerRigidBody;
        bool lastPeaceSignState = false;
        bool abilityActivated = false;
        bool wallIsMaking = false;
        bool rockIsActive = false;
        bool rightHandAbilityIsActive = false;

        TrackedHand leftHand;
        TrackedHand rightHand;

        void Start()
        {
            // Object instantiation
            player = GetComponent<Player>();
            playerRigidBody = GetComponent<Rigidbody>();
            leftHand = new TrackedHand(player.leftHand);
            rightHand = new TrackedHand(player.rightHand);

            // Start repeating processes
            InvokeRepeating("UpdateAtInterval", 0, handPositionMemoryInterval);

            // TODO: Move the below to a game manager script
            // SteamVR housekeeping
            Teleport.instance.CancelTeleportHint();
        }

        private void UpdateAtInterval()
        {
            leftHand.addCurrentPosition();
            rightHand.addCurrentPosition();
        }

        private void Update()
        {
            // If an ability was just activated but was finished, invalidate all controller memory positions:
            //  We do not want gestures performed while an ability is activating to trigger an accidental follow-up activation.
            if (abilityActivated && !wallIsMaking && !rockIsActive)
            {
                leftHand.invalidateTrackingMemory();
                rightHand.invalidateTrackingMemory();
                abilityActivated = false;
            }

            analyzeStaticGestures();
            analyzeDynamicGestures();
        }

        private void jump()
        {
            Debug.Log("Jumped");
            playerRigidBody.AddForce(Vector2.up * jumpSpeed); ;
        }

        // ------------------------------------------------------- Dynamic Gesture Recognition -------------------------------------------------------

        float bothHandsGestureTimer = 0.0f;
        float waitTime = 0.15f;
        private void analyzeDynamicGestures()
        {
            // TODO: Figure out if our recognizeFist method needs augmentation for different controller types
            //          Might need to use: hand.IsGrabbingWithType(GrabTypes.Grip)
            if (!abilityActivated)
            {
                // Check wall making gesture
                if (bothFistsMovingUp())
                {
                    Debug.Log("Triggering wall summon!");
                    abilityActivated = true;
                    wallIsMaking = true;
                    summonWall();
                }
                // Check rock lifting gesture
                else if (isFistAndMovingUp(leftHand) && bothHandsGestureTimer <= 0.0)
                {
                    Debug.Log("Triggering left rock summon!");
                    abilityActivated = true;
                    rockIsActive = true;
                    summonRock(leftHand);
                }
                else if (isFistAndMovingUp(rightHand) && bothHandsGestureTimer <= 0.0)
                {
                    Debug.Log("Triggering right rock summon!");
                    abilityActivated = true;
                    rockIsActive = true;
                    rightHandAbilityIsActive = true;
                    summonRock(rightHand);
                }
            }
            else if (abilityActivated && rockIsActive) {
                TrackedHand punchingHand = rightHandAbilityIsActive ? leftHand : rightHand;
                if (isFistAndMovingInDirection(punchingHand, lookBackWindowInSeconds, rockObject.transform.position - punchingHand.getPosition(), punchDistanceThreshold)) {
                    if ((rockObject.transform.position - punchingHand.getPosition()).magnitude < rockPunchActivationDistance) {
                        Debug.Log("Triggering rock punch!");
                        rockIsActive = false;
                        punchRock(rockObject.transform.position - punchingHand.pastControllerPositions[punchingHand.getIndexForPastTimeSeconds(lookBackWindowInSeconds)-1].position, punchingHand.getVelocity().sqrMagnitude * 250f);
                    }
                }
            }
        }

        // Check if both fists are moving up. If only one is moving up, give the other one a change to catch up.
        private bool bothFistsMovingUp() { 
            if (isFistAndMovingUp(leftHand) && isFistAndMovingUp(rightHand))
            {
                return true;
            }
            else if (isFistAndMovingUp(leftHand) || isFistAndMovingUp(rightHand))
            {
                bothHandsGestureTimer = updateTimer(bothHandsGestureTimer, waitTime, Time.deltaTime);
            }
            return false;
        }

        private float updateTimer(float timer, float waitTime, float step) {
            if (timer <= 0.0) {
                timer = waitTime;
            } else if (timer > 0.0) {
                timer -= step;
            }

            return timer;
        }

        private bool isFistAndMovingUp(TrackedHand hand)
        {
            return isFistAndMovingInDirection(hand, lookBackWindowInSeconds, Vector3.up, verticalMoveDistanceThreshold);
        }

        private bool isFistAndMovingInDirection(TrackedHand hand, float lookBackWindowInSeconds, Vector3 direction, float distanceThreshold)
        {
            int lookBackWindowIndex = hand.getIndexForPastTimeSeconds(lookBackWindowInSeconds);

            // Validate that we have enough position memory
            if (hand.pastControllerPositions.Count < lookBackWindowIndex)
            {
                // Debug.Log("Not enough look back positions");
                return false;
            }

            for (int i = 0; i < lookBackWindowIndex; i++)
            {
                // Validate validity requirement
                if (!hand.pastControllerPositions[i].isValid)
                {
                    // Debug.Log("Not enough valid look back positions");
                    return false;
                }

                // Validate grip requirement
                if (!hand.pastControllerPositions[i].isGripping)
                {
                    // Debug.Log("Hands were not gripped long enough");
                    return false;
                }

                // Validate distance travelled requirement
                Vector3 distanceTravelled = hand.getPosition() - hand.pastControllerPositions[i].position;
                float distanceTravelledInDirection = Vector3.Dot(distanceTravelled, direction.normalized);
                if (distanceTravelledInDirection > distanceThreshold) {
                    return true;
                }
            }

            return false;
        }

        // ------------------------------------------------------- Static Gesture Recognition -------------------------------------------------------

        private void analyzeStaticGestures()
        {
            if (leftHand.getSkeleton() != null)
            {
                recognizePeaceSign(leftHand.getSkeleton());
            }
            if (rightHand.getSkeleton() != null)
            {
                recognizePeaceSign(rightHand.getSkeleton());
            }
        }

        private void recognizePeaceSign(SteamVR_Behaviour_Skeleton skeleton)
        {
            if ((skeleton.indexCurl <= openFingerAmount && skeleton.middleCurl <= openFingerAmount) &&
                (skeleton.thumbCurl >= closedThumbAmount && skeleton.ringCurl >= closedFingerAmount && skeleton.pinkyCurl >= closedFingerAmount))
            {
                PeaceSignRecognized(true);
            }
            else
            {
                PeaceSignRecognized(false);
            }
        }

        private void PeaceSignRecognized(bool currentPeaceSignState)
        {
            if (lastPeaceSignState == false && currentPeaceSignState == true)
            {
                jump();
            }

            lastPeaceSignState = currentPeaceSignState;
        }
        private bool recognizePlayerFist(TrackedHand hand)
        {
            if (hand == null)
            {
                return false;
            }

            SteamVR_Behaviour_Skeleton skeleton = hand.getSkeleton();
            if (skeleton != null)
            {
                return hand.recognizeFist();
            }

            return abilityActivated;
        }

        // ------------------------------------------------------- Wall Making -------------------------------------------------------

        // TODO: These placeholder objects need to be refactored and passed into the below methods
        GameObject wallObject = null;
        private const float wallRisingSpeed = 7f;
        private const float wallForwardDistance = 1.5f;

        private void summonWall()
        {
            GameObject wall = spawnWall();

            Vector3 finalWallPosition = new Vector3(wall.transform.position.x, Terrain.activeTerrain.SampleHeight(wall.transform.position), wall.transform.position.z);
            StartCoroutine(wallRisingCoroutine(wall, finalWallPosition, wallRisingSpeed));
        }

        private GameObject spawnWall()
        {
            wallObject = GameObject.Instantiate(wallPrefab);
            spawnObjectInFrontOfPlayer(wallObject, wallForwardDistance);
            moveObjectBelowGround(wallObject);
            faceObjectToPlayer(wallObject);

            return wallObject;
        }

        IEnumerator wallRisingCoroutine(GameObject obj, Vector3 finalObjectPosition, float speed)
        {
            yield return riseAtSpeed(obj, finalObjectPosition, speed);

            wallIsMaking = false;
            Debug.Log("wall making complete");
        }

        // ------------------------------------------------------- Rock Floating -------------------------------------------------------

        GameObject rockObject = null;
        float rockSummonSpeed = .1f;
        float rockForwardFloatDistance = .7f;
        float rockDragForce = 7f;
        private void summonRock(TrackedHand hand)
        {
            GameObject rock = spawnRock();

            StartCoroutine(rockAbilityCoroutine(rock, rockSummonSpeed, hand, rockForwardFloatDistance));
        }

        public Material rockMaterial;
        private GameObject spawnRock()
        {
            rockObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rockObject.transform.localScale = new Vector3(.25f, .25f, .25f);
            rockObject.AddComponent<Rigidbody>();
            rockObject.GetComponent<Rigidbody>().useGravity = false;
            rockObject.GetComponent<Rigidbody>().drag = rockDragForce;
            rockObject.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Continuous;
            rockObject.GetComponent<Renderer>().material = rockMaterial;
            rockObject.AddComponent<DestroyOnImpact>();

            spawnObjectInFrontOfPlayer(rockObject, rockForwardFloatDistance);
            moveObjectBelowGround(rockObject);
            faceObjectToPlayer(rockObject);

            return rockObject;
        }

        IEnumerator rockAbilityCoroutine(GameObject obj, float speed, TrackedHand hand, float rockForwardFloatDistance)
        {
            yield return floatInFrontOfHand(obj, hand, rockForwardFloatDistance);

            Debug.Log("rock sequence complete");
            rockObject.GetComponent<Rigidbody>().useGravity = true;
            rockObject.GetComponent<Rigidbody>().drag = 0f;
            resetRockAbility();
        }

        private void punchRock(Vector3 direction, float force)
        {
            rockObject.GetComponent<Rigidbody>().AddForce(direction * force);
        }

        // ------------------------------------------------------- Util Methods -------------------------------------------------------

        private void spawnObjectInFrontOfPlayer(GameObject obj, float forwardDistance)
        {
            obj.transform.position = transform.Find("SteamVRObjects/VRCamera").transform.position + (transform.Find("SteamVRObjects/VRCamera").transform.forward * forwardDistance);
        }

        private void moveObjectBelowGround(GameObject obj)
        {
            obj.transform.position = new Vector3(obj.transform.position.x, Terrain.activeTerrain.SampleHeight(obj.transform.position) - obj.transform.localScale.y, obj.transform.position.z);
        }

        private void faceObjectToPlayer(GameObject obj)
        {
            obj.transform.LookAt(transform.Find("SteamVRObjects/VRCamera").transform);
            obj.transform.eulerAngles = new Vector3(0f, obj.transform.eulerAngles.y, 0f);
        }

        IEnumerator riseAtSpeed(GameObject obj, Vector3 finalPosition, float speed)
        {
            float step = speed * Time.deltaTime;

            while (Vector3.Distance(obj.transform.position, finalPosition) > minimalDistanceThreshold)
            {
                obj.transform.position = Vector3.MoveTowards(obj.transform.position, finalPosition, step);
                yield return null;
            }
        }

        float floatSpeed = 30f;
        float floatMovementThreshold = .3f;
        IEnumerator floatInFrontOfHand(GameObject obj, TrackedHand hand, float forwardDistance)
        {
            while (rockIsActive && recognizePlayerFist(hand))
            {
                Vector3 finalPosition = getFloatingPositionInFrontOfHand(hand, forwardDistance);

                if (Vector3.Distance(obj.transform.position, finalPosition) > floatMovementThreshold)
                {
                    Vector3 direction = finalPosition - obj.transform.position;
                    obj.GetComponent<Rigidbody>().AddForce(direction * floatSpeed);
                }

                yield return null;
            }
        }

        private Vector3 getFloatingPositionInFrontOfHand(TrackedHand hand, float forwardDistance)
        {
            return hand.getPosition() + (-1f * hand.getTransform().up * forwardDistance);
        }

        private void resetRockAbility() {
            rockIsActive = false;
            rightHandAbilityIsActive = false;
        }
    }
}