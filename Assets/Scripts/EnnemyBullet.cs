using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnnemyBullet : MonoBehaviour
{
    [SerializeField] private float m_speed;
    private Vector2 m_direction;
    [SerializeField] private float m_lifetime = 1f;
    private float DecreasingLifetime;
    public void InitBullet(Vector2 Direction,Vector2 Origin)
    {
        m_direction = Direction;
        transform.position = Origin;
        DecreasingLifetime = m_lifetime;
    }

    public void Update()
    {
       
        if(DecreasingLifetime > 0)
            DecreasingLifetime -= Time.deltaTime;
        else
            gameObject.SetActive(false);
        
    }

    private void FixedUpdate()
    {
        transform.position += (Vector3)m_direction * m_speed * 0.1f;
    }
}
