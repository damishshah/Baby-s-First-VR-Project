using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace Valve.VR.InteractionSystem
{
    public class TrackedHand
    {
        private const float openFingerAmount = 0.1f;
        private const float closedFingerAmount = 0.75f;
        private const float closedThumbAmount = 0.4f;
        private const float jumpSpeed = 10.0f;
        private const float handPositionMemoryInterval = 0.1f;
        private const int maxMemoryIntervalStorageSize = 10;

        public Hand hand;
        public List<ControllerPosition> pastControllerPositions { get; set; }

        public TrackedHand(Hand hand)
        {
            this.hand = hand;
            pastControllerPositions = new List<ControllerPosition>();
        }

        public void addCurrentPosition()
        {
            managePastControllerPositionsSize();
            pastControllerPositions.Insert(0, new ControllerPosition(
                this.getPosition(),
                this.getVelocity(),
                this.recognizeFist()));
        }

        public bool recognizeFist()
        {
            if (this.hand == null)
            {
                return false;
            }

            return this.hand.IsGrabbingWithType(GrabTypes.Grip);
        }

        public SteamVR_Behaviour_Skeleton getSkeleton()
        {
            return this.hand.skeleton;
        }

        public Vector3 getPosition()
        {
            return hand.trackedObject.transform.position;
        }

        public Vector3 getVelocity()
        {
            return this.hand.trackedObject.GetVelocity();
        }

        public Transform getTransform()
        {
            return this.hand.trackedObject.transform;
        }

        // Returns what would be the index in the pastControllerPosition array for the given number of seconds in the past. Does no checking if the index is within the array size.
        public int getIndexForPastTimeSeconds(float lookBackWindowInSeconds) { return (int)(lookBackWindowInSeconds / handPositionMemoryInterval); }

        public void invalidateTrackingMemory()
        {
            foreach (var controllerPosition in pastControllerPositions)
            {
                controllerPosition.isValid = false;
            }
        }

        private void managePastControllerPositionsSize()
        {
            while (pastControllerPositions.Count > maxMemoryIntervalStorageSize)
            {
                pastControllerPositions.RemoveAt(maxMemoryIntervalStorageSize);
            }
        }
    }
}