using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
  public bool active { get; set; }
  void Start()
  {
    active = true;
  }
  private void OnCollisionEnter(Collision collision)
  {
    var collidedObj = collision.gameObject.GetComponentInChildren<Enemy>();
    if (collidedObj != null) {
      Rigidbody projectileBody = GetComponentInChildren<Rigidbody>();
      projectileBody.useGravity = true;

      Destroy(gameObject, 2);
    }
  }
}
