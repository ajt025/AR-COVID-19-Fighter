using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBoundaryTrigger : MonoBehaviour
{
  private void OnTriggerEnter(Collider other)
  {
    if (other.gameObject.GetComponent<Enemy>() != null)
    {
      // Enemy has passed the boundary, update game variables accordingly
      Destroy(other.gameObject);

      if (GameSystem.instance.gameState == GameSystem.GameState.Running)
        GameSystem.instance.livesText.text = "Lives: " + --GameSystem.instance.numOfLives;
    }
  }
}
