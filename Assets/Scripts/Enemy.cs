using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
  public float floatAmplitude;
  public float floatRate;
  public float rotationRate;
  public Vector3 destination { get; set; }
  public bool active { get; set; }

  float originalY;

  // Start is called before the first frame update
  void Start()
  {
    originalY = transform.position.y;
    active = true;
  }

  // Update is called once per frame
  void Update()
  {
    if (active)
    {
      var pos = transform.position;
      transform.Rotate(0, rotationRate * Time.deltaTime * 360, 0);
      pos.y = originalY + Mathf.Sin(Time.time * floatRate * Mathf.PI) * floatAmplitude;
      transform.position = pos;

      GetComponent<Rigidbody>().velocity = (destination - pos).normalized / 10;
    }
  }
  private void OnCollisionEnter(Collision collision)
  {
    if (active)
    {
      var proj = collision.gameObject.GetComponentInChildren<Projectile>();
      if (proj != null && proj.active)
      {
        GetComponentInChildren<Rigidbody>().useGravity = true;
        // Enemy destoryed, update game variables accordingly
        Destroy(gameObject, 1);
        active = false;
        proj.active = false;

        if (GameSystem.instance.gameState == GameSystem.GameState.Running)
          GameSystem.instance.scoreText.text = "Score: " + ++GameSystem.instance.numOfPoints;
      }
    }
  }
}
