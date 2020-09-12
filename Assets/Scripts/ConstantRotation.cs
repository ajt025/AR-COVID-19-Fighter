using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantRotation : MonoBehaviour
{
  public float rotationRate = 1;
  // Update is called once per frame
  void Update()
  {
    transform.Rotate(0, rotationRate * Time.deltaTime * 360, 0);
  }
}
