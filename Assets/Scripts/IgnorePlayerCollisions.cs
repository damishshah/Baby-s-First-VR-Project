﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IgnorePlayerCollisions : MonoBehaviour
{
 void OnCollisionEnter(Collision collision)
  {
      if (collision.gameObject.tag == "Player")
      {
          Physics.IgnoreCollision(collision.gameObject.GetComponent<Collider>(), GetComponent<Collider>());
      }
  }
}
