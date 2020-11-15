using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace Valve.VR.InteractionSystem
{
    public class ControllerPosition
    {
        public Vector3 position { get; set; }
        public Vector3 velocity { get; set; }
        public bool isGripping { get; set; }
        public bool isValid { get; set; }

        public ControllerPosition(Vector3 position, Vector3 velocity, bool isGripping)
        {
            this.position = position;
            this.velocity = velocity;
            this.isGripping = isGripping;
            this.isValid = true;
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