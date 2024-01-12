using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnnemyBullet : MonoBehaviour
{
    [SerializeField] private float m_speed;
    private Vector2 m_direction;
    [SerializeField] private float m_lifetime = 1f;
    private float DecreasingLifetime;
    private Rigidbody2D m_Rigidbody;
    public void InitBullet(Vector2 Direction,Vector2 Origin)
    {
        m_direction = Direction;
        transform.position = Origin;
        DecreasingLifetime = m_lifetime;
    }

    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody2D>();
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
        m_Rigidbody.velocity = (Vector3)m_direction * m_speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (Player.Instance == null)
        {
            gameObject.SetActive(false);
            return;
        }
            
        if (collision.attachedRigidbody.gameObject != Player.Instance.gameObject)
        {
            gameObject.SetActive(false);
            return;
        }
        
        Player.Instance.ApplyHit(null);
        
    }
}
