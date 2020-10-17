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

        Player player;
        Rigidbody playerRigidBody;
        bool lastPeaceSignState = false;
        bool abilityActivated = false;
        bool wallIsMaking = false;

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
            // InvokeRepeating("UpdateEverySecond", 0, 1.0f);

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

        private void invalidateControllerPositions() {
            foreach (var controllerPositions in pastControllerPositions) {
                controllerPositions.isValid = false;
            } 
        }

        // ------------------------------------------------------- Dynamic Gesture Recognition -------------------------------------------------------

        private void analyzeDynamicGestures(Hand hand)
        {
            // TODO: Figure out if our recognizeFist method needs augmentation for different controller types
            //          Might need to use: hand.IsGrabbingWithType(GrabTypes.Grip)
            if (!abilityActivated && !wallIsMaking)
            {
                if (bothFistsMovingUp(hand, hand.otherHand, pastControllerPositions, lookBackWindowInSeconds))
                {
                    Debug.Log("Triggering wall summon!");
                    abilityActivated = true;
                    wallIsMaking = true;
                    summonWall();
                }
            }

            if (!recognizeFist(hand) && !recognizeFist(hand.otherHand) && !wallIsMaking)
            {
                abilityActivated = false;
            }
        }

        private bool bothFistsMovingUp(Hand hand1, Hand hand2, List<ControllerPositions> pastControllerPositions, float lookBackWindowInSeconds)
        {
            ControllerPositions latestPositions = pastControllerPositions[pastControllerPositions.Count - 1];
            int lookBackWindowIndex = (int)(lookBackWindowInSeconds / handPositionMemoryInterval);

            // Validate that we have enough position memory
            if (pastControllerPositions.Count < lookBackWindowIndex) {
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
                // Validate grip requirement
                if (!pastControllerPositions[indexFromEnd].left.isGripping || !pastControllerPositions[indexFromEnd].right.isGripping)
                {
                    Debug.Log("Hands were not gripped long enough");
                    return false;
                }
                // Validate velocity requirement
                if (pastControllerPositions[indexFromEnd].left.velocity.y < velocityThreshold ||
                    pastControllerPositions[indexFromEnd].right.velocity.y < velocityThreshold)
                {
                    Debug.Log("Velocity threshold not met");
                    return false;
                }
                // Validate distance requirement over lookback window
                if (i == lookBackWindowIndex)
                {
                    if (!isDistanceTravelledGreaterThanThreshold(pastControllerPositions[indexFromEnd], latestPositions, verticalMoveDistanceThreshold))
                    {
                        Debug.Log("Distance threshold not met");
                        return false;
                    }
                }
            }

            Debug.Log("Both hands moving up!");
            invalidateControllerPositions();
            return true;
        }

        private bool isDistanceTravelledGreaterThanThreshold(ControllerPositions startPostiion, ControllerPositions endPosition, float threshold)
        {
            return endPosition.left.position.y - startPostiion.left.position.y > threshold &&
                    endPosition.right.position.y - startPostiion.right.position.y > threshold;
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

        GameObject cube = null;
        private const float speedMultiplier = 7.0f;

        private void summonWall()
        {
            if (cube != null)
            {
                Destroy(cube);
            }

            GameObject wall = spawnWall();

            Vector3 finalWallPosition = new Vector3(wall.transform.position.x, wall.transform.localScale.y * 0.5f, wall.transform.position.z);
            StartCoroutine(wallRisingCoroutine(wall, finalWallPosition, speedMultiplier * getHandSpeed()));
        }

        private GameObject spawnWall()
        {
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.localScale = new Vector3(2f, 2f, .25f);

            spawnObjectInFrontOfPlayer(cube);
            moveObjectBelowGround(cube);
            faceObjectToPlayer(cube);

            return cube;
        }

        IEnumerator wallRisingCoroutine(GameObject wall, Vector3 finalWallPosition, float speed)
        {
            yield return riseAtSpeed(wall, finalWallPosition, speedMultiplier * getHandSpeed());

            if (wall.transform.position == finalWallPosition)
            {
                wallIsMaking = false;
                Debug.Log("wall making complete");
            }
        }

        // ------------------------------------------------------- Util Methods -------------------------------------------------------

        private void spawnObjectInFrontOfPlayer(GameObject obj)
        {
            obj.transform.position = transform.Find("SteamVRObjects/VRCamera").transform.position + (transform.Find("SteamVRObjects/VRCamera").transform.forward * 1.5f);
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

        IEnumerator riseAtSpeed(GameObject itemToRise, Vector3 finalPosition, float speed)
        {
            float step = speed * Time.deltaTime;

            while (itemToRise.transform.position != finalPosition)
            {
                itemToRise.transform.position = Vector3.MoveTowards(itemToRise.transform.position, finalPosition, speed);
                yield return null;
            }
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

        private float getHandSpeed()
        {
            ControllerPositions initialPosition = pastControllerPositions[pastControllerPositions.Count / 2];
            float rightStartPositionY = initialPosition.right.position.y;
            return (pastControllerPositions[pastControllerPositions.Count - 1].right.position.y - initialPosition.right.position.y) * Time.deltaTime;
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

            override public string ToString()
            {
                return "position:{" + position + "}, velocity:{" + velocity + "}, isGripping:{" + isGripping + "}";
            }
        }
    }
}