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
        private const float openFingerAmount = 0.1f;
        private const float closedFingerAmount = 0.9f;
        private const float closedThumbAmount = 0.4f;
        private const float jumpSpeed = 10.0f;
        private const float handPositionMemoryInterval = 0.1f;
        private const int maxMemoryIntervalStorageSize = 10;
        private const float lookBackWindowInSeconds = 0.3f;
        private const float velocityThreshold = 0.3f;
        private const float verticalMoveDistanceThreshold = 0.1f;
        private const float minimalDistanceThreshold = 0.1f;

        Player player;
        Rigidbody playerRigidBody;
        bool lastPeaceSignState = false;
        bool abilityActivated = false;
        bool wallIsMaking = false;
        bool rockIsActive = false;

        Hand[] hands = new Hand[2];
        List<ControllerPositions> pastControllerPositions;

        void Start()
        {
            // Object instantiation
            player = GetComponent<Player>();
            playerRigidBody = GetComponent<Rigidbody>();
            hands[0] = player.leftHand;
            hands[1] = player.rightHand;
            clearTrackingMemory();

            // Start repeating processes
            InvokeRepeating("UpdateAtInterval", 0, handPositionMemoryInterval);

            // TODO: Move the below to a game manager script
            // SteamVR housekeeping
            Teleport.instance.CancelTeleportHint();
        }

        private void UpdateAtInterval()
        {
            pastControllerPositions.Add(
                getCurrentControllerPositionForTwoHands(hands[0], hands[1]));

            managePastControllerPositionsSize();
        }

        public void managePastControllerPositionsSize()
        {
            if (pastControllerPositions.Count > maxMemoryIntervalStorageSize)
            {
                pastControllerPositions.RemoveAt(0);
            }
        }

        private void Update()
        {
            // If an ability was just activated but was finished, invalidate all controller memory positions:
            //  We do not want gestures performed while an ability is activating to trigger an accidental follow-up activation.
            if (abilityActivated && !wallIsMaking && !rockIsActive)
            {
                invalidateControllerPositions();
                abilityActivated = false;
            }

            analyzeStaticGestures(hands[0]);
            analyzeDynamicGestures(hands[0]);
        }

        private void jump()
        {
            Debug.Log("Jumped");
            playerRigidBody.AddForce(Vector2.up * jumpSpeed); ;
        }

        private void clearTrackingMemory()
        {
            pastControllerPositions = new List<ControllerPositions>();
        }

        private void invalidateControllerPositions()
        {
            foreach (var controllerPositions in pastControllerPositions)
            {
                controllerPositions.isValid = false;
            }
        }

        // ------------------------------------------------------- Dynamic Gesture Recognition -------------------------------------------------------

        private void analyzeDynamicGestures(Hand hand)
        {
            // TODO: Figure out if our recognizeFist method needs augmentation for different controller types
            //          Might need to use: hand.IsGrabbingWithType(GrabTypes.Grip)
            if (!abilityActivated)
            {
                // Check wall making gesture
                if (bothFistsMovingUp(pastControllerPositions, lookBackWindowInSeconds))
                {
                    Debug.Log("Triggering wall summon!");
                    abilityActivated = true;
                    wallIsMaking = true;
                    summonWall();
                }
                // Check rock lifting gesture
                else if (leftFistMovingUp(pastControllerPositions, lookBackWindowInSeconds))
                {
                    Debug.Log("Triggering left rock summon!");
                    abilityActivated = true;
                    rockIsActive = true;
                    summonRock(hand);
                }
                else if (rightFistMovingUp(pastControllerPositions, lookBackWindowInSeconds))
                {
                    Debug.Log("Triggering right rock summon!");
                    abilityActivated = true;
                    rockIsActive = true;
                    summonRock(hand.otherHand);
                }
            }
        }

        private bool bothFistsMovingUp(List<ControllerPositions> pastControllerPositions, float lookBackWindowInSeconds)
        {
            bool bothFistsMovingUp = leftFistMovingUp(pastControllerPositions, lookBackWindowInSeconds) && rightFistMovingUp(pastControllerPositions, lookBackWindowInSeconds);

            if (bothFistsMovingUp)
            {
                Debug.Log("Both hands moving up!");
                return true;
            }

            return false;
        }

        // There is a method for both the right/left hands for overall design readibility here, I'd like to consider refactoring these into one method in the future.
        private bool leftFistMovingUp(List<ControllerPositions> pastControllerPositions, float lookBackWindowInSeconds)
        {
            ControllerPositions latestPositions = pastControllerPositions[pastControllerPositions.Count - 1];
            int lookBackWindowIndex = (int)(lookBackWindowInSeconds / handPositionMemoryInterval);

            // Validate that we have enough position memory
            if (pastControllerPositions.Count < lookBackWindowIndex)
            {
                Debug.Log("Not enough look back positions");
                return false;
            }

            for (int i = 1; i <= lookBackWindowIndex; i++)
            {
                int indexFromEnd = pastControllerPositions.Count - i;
                // Validate validity requirement
                if (!pastControllerPositions[indexFromEnd].isValid)
                {
                    Debug.Log("Not enough valid look back positions");
                    return false;
                }

                if (!isControllerPositionFistAndMovingUp(pastControllerPositions[indexFromEnd].left, velocityThreshold))
                {
                    Debug.Log("Left hand not a fist or moving fast enough");
                    return false;
                }
            }

            // Validate distance requirement over lookback window
            float leftHandDistanceMoved = latestPositions.left.distanceBetweenY(pastControllerPositions[pastControllerPositions.Count - lookBackWindowIndex].left);
            if (leftHandDistanceMoved < verticalMoveDistanceThreshold)
            {
                Debug.Log("Left hand distance threshold not met");
                return false;
            }

            return true;
        }

        private bool rightFistMovingUp(List<ControllerPositions> pastControllerPositions, float lookBackWindowInSeconds)
        {
            ControllerPositions latestPositions = pastControllerPositions[pastControllerPositions.Count - 1];
            int lookBackWindowIndex = (int)(lookBackWindowInSeconds / handPositionMemoryInterval);

            // Validate that we have enough position memory
            if (pastControllerPositions.Count < lookBackWindowIndex)
            {
                Debug.Log("Not enough look back positions");
                return false;
            }

            for (int i = 1; i <= lookBackWindowIndex; i++)
            {
                int indexFromEnd = pastControllerPositions.Count - i;
                // Validate validity requirement
                if (!pastControllerPositions[indexFromEnd].isValid)
                {
                    Debug.Log("Not enough valid look back positions");
                    return false;
                }

                if (!isControllerPositionFistAndMovingUp(pastControllerPositions[indexFromEnd].right, velocityThreshold))
                {
                    Debug.Log("Right hand not a fist or moving fast enough");
                    return false;
                }
            }

            // Validate distance requirement over lookback window
            float rightHandDistanceMoved = latestPositions.right.distanceBetweenY(pastControllerPositions[pastControllerPositions.Count - lookBackWindowIndex].right);
            if (rightHandDistanceMoved < verticalMoveDistanceThreshold)
            {
                Debug.Log("Right hand Distance threshold not met");
                return false;
            }

            return true;
        }

        // ------------------------------------------------------- Static Gesture Recognition -------------------------------------------------------

        private void analyzeStaticGestures(Hand hand)
        {
            if (hand.skeleton != null)
            {
                recognizePeaceSign(hand.skeleton);
            }
            if (hand.otherHand.skeleton != null)
            {
                recognizePeaceSign(hand.otherHand.skeleton);
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
        private bool recognizeFist(Hand hand)
        {
            if (hand == null) {
                return false;
            }

            SteamVR_Behaviour_Skeleton skeleton = hand.skeleton;
            if (skeleton != null)
            {
                return (
                    // skeleton.thumbCurl >= closedThumbAmount &&
                    skeleton.indexCurl >= closedFingerAmount &&
                    skeleton.middleCurl >= closedFingerAmount &&
                    skeleton.ringCurl >= closedFingerAmount //&& 
                                                            // skeleton.pinkyCurl >= closedFingerAmount
                    );
            }
            return abilityActivated;
        }

        // ------------------------------------------------------- Wall Making -------------------------------------------------------

        // TODO: These placeholder objects need to be refactored and passed into the below methods
        GameObject wallObject = null;
        GameObject rockObject = null;
        private const float speedMultiplier = 7.0f;
        private const float wallForwardDistance = 1.5f;

        private void summonWall()
        {
            if (wallObject != null)
            {
                Destroy(wallObject);
            }

            GameObject wall = spawnWall();

            Vector3 finalWallPosition = new Vector3(wall.transform.position.x, wall.transform.localScale.y * 0.5f, wall.transform.position.z);
            StartCoroutine(wallRisingCoroutine(wall, finalWallPosition, speedMultiplier * getHandsSpeed()));
        }

        private GameObject spawnWall()
        {
            wallObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallObject.transform.localScale = new Vector3(2f, 2f, .25f);

            spawnObjectInFrontOfPlayer(wallObject, wallForwardDistance);
            moveObjectBelowGround(wallObject);
            faceObjectToPlayer(wallObject);

            return wallObject;
        }

        IEnumerator wallRisingCoroutine(GameObject obj, Vector3 finalObjectPosition, float speed)
        {
            yield return riseAtSpeed(obj, finalObjectPosition, speedMultiplier * getHandsSpeed());

            wallIsMaking = false;
            Debug.Log("wall making complete");
        }

        // ------------------------------------------------------- Rock Floating -------------------------------------------------------

        float rockSummonSpeed = .1f;
        float rockForwardDistance = .7f;
        float rockDragForce = 7f;
        private void summonRock(Hand hand)
        {
            if (rockObject != null)
            {
                Destroy(rockObject);
            }

            GameObject rock = spawnRock();

            Vector3 finalRockPosition = new Vector3(rock.transform.position.x, transform.Find("SteamVRObjects/VRCamera").transform.position.y, rock.transform.position.z);
            StartCoroutine(rockRisingCoroutine(rock, finalRockPosition, rockSummonSpeed, hand, rockForwardDistance));
        }

        private GameObject spawnRock()
        {
            rockObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rockObject.transform.localScale = new Vector3(.25f, .25f, .25f);
            rockObject.AddComponent<Rigidbody>();
            rockObject.GetComponent<Rigidbody>().useGravity = false;
            rockObject.GetComponent<Rigidbody>().drag = rockDragForce;

            spawnObjectInFrontOfPlayer(rockObject, rockForwardDistance);
            moveObjectBelowGround(rockObject);
            faceObjectToPlayer(rockObject);

            return rockObject;
        }

        IEnumerator rockRisingCoroutine(GameObject obj, Vector3 finalObjectPosition, float speed, Hand hand, float rockForwardDistance)
        {
            yield return floatInFrontOfHand(obj, hand, rockForwardDistance);

            Debug.Log("rock sequence complete");
            rockObject.GetComponent<Rigidbody>().useGravity = true;
            rockObject.GetComponent<Rigidbody>().drag = 0f;
            rockIsActive = false;
        }

        // ------------------------------------------------------- Util Methods -------------------------------------------------------

        private void spawnObjectInFrontOfPlayer(GameObject obj, float forwardDistance)
        {
            obj.transform.position = transform.Find("SteamVRObjects/VRCamera").transform.position + (transform.Find("SteamVRObjects/VRCamera").transform.forward * forwardDistance);
        }

        private void moveObjectBelowGround(GameObject obj)
        {
            obj.transform.position = new Vector3(obj.transform.position.x, -.5f * obj.transform.localScale.y, obj.transform.position.z);
        }

        private void faceObjectToPlayer(GameObject obj)
        {
            obj.transform.LookAt(transform.Find("SteamVRObjects/VRCamera").transform);
            obj.transform.eulerAngles = new Vector3(0f, obj.transform.eulerAngles.y, 0f);
        }

        IEnumerator riseAtSpeed(GameObject obj, Vector3 finalPosition, float speed)
        {
            float step = speed * Time.deltaTime;

            while (Vector3.Distance(obj.transform.position,finalPosition) > minimalDistanceThreshold)
            {
                obj.transform.position = Vector3.MoveTowards(obj.transform.position, finalPosition, speed);
                yield return null;
            }
        }

        float floatSpeed = 30f;
        float floatMovementThreshold = .3f;
        IEnumerator floatInFrontOfHand(GameObject obj, Hand hand, float forwardDistance) {
            while (rockIsActive && recognizeFist(hand)) {
                Vector3 finalPosition = getFloatingPositionInFrontOfHand(hand, forwardDistance);

                if (Vector3.Distance(obj.transform.position, finalPosition) > floatMovementThreshold) {
                    Vector3 direction = finalPosition - obj.transform.position;
                    obj.GetComponent<Rigidbody>().AddForce(direction*floatSpeed);
                }

                yield return null;
            }
        }

        private Vector3 getFloatingPositionInFrontOfHand(Hand hand, float forwardDistance) {
            return hand.trackedObject.transform.position + (-1f * hand.trackedObject.transform.up * forwardDistance);
        }

        // ------------------------------------------------------- Hand Util Methods -------------------------------------------------------

        private ControllerPositions getCurrentControllerPositionForTwoHands(Hand hand, Hand otherHand)
        {
            return new ControllerPositions(
                new ControllerPosition(
                    hand.trackedObject.transform.position,
                    hand.trackedObject.GetVelocity(),
                    recognizeFist(hand)),
                new ControllerPosition(
                    otherHand.trackedObject.transform.position,
                    otherHand.trackedObject.GetVelocity(),
                    recognizeFist(otherHand))
            );
        }

        private float getHandsSpeed()
        {
            ControllerPositions initialPosition = pastControllerPositions[pastControllerPositions.Count / 2];
            float rightStartPositionY = initialPosition.right.position.y;
            return (pastControllerPositions[pastControllerPositions.Count - 1].right.position.y - initialPosition.right.position.y) * Time.deltaTime;
        }

        private bool isControllerPositionFistAndMovingUp(ControllerPosition controllerPosition, float velocityThreshold)
        {
            // Validate grip requirement
            if (!controllerPosition.isGripping)
            {
                Debug.Log("Hands were not gripped long enough");
                return false;
            }
            // Validate velocity requirement
            if (controllerPosition.velocity.y < velocityThreshold)
            {
                Debug.Log("Velocity threshold not met");
                return false;
            }

            Debug.Log("Fist was moving up!");
            return true;
        }

        // ------------------------------------------------------- Private Classes -------------------------------------------------------

        private class ControllerPositions
        {
            public ControllerPosition left { get; set; }
            public ControllerPosition right { get; set; }
            public bool isValid { get; set; }

            public ControllerPositions(ControllerPosition left, ControllerPosition right, bool isValid)
            {
                this.left = left;
                this.right = right;
                this.isValid = isValid;
            }

            public ControllerPositions(ControllerPosition left, ControllerPosition right) : this(left, right, true) { }

            override public string ToString()
            {
                return "left:{" + left + "}, right:{" + right + "}";
            }
        }

        private class ControllerPosition
        {
            public Vector3 position { get; set; }
            public Vector3 velocity { get; set; }
            public bool isGripping { get; set; }

            public ControllerPosition(Vector3 position, Vector3 velocity, bool isGripping)
            {
                this.position = position;
                this.velocity = velocity;
                this.isGripping = isGripping;
            }

            public float distanceBetweenY(ControllerPosition otherController)
            {
                return this.position.y - otherController.position.y;
            }

            override public string ToString()
            {
                return "position:{" + position + "}, velocity:{" + velocity + "}, isGripping:{" + isGripping + "}";
            }
        }
    }
}