using System;
using UnityEngine;

public class Stairs : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        DungeonGeneratorV2.Instance.IncrementDifficulty();
    }
}
