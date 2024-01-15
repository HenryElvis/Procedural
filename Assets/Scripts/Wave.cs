using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wave : MonoBehaviour
{
    CircleCollider2D m_circleCollider;
    [SerializeField,Range(0.1f,2f)] float m_range;
    [SerializeField, Range(0.1f, 5f)] float m_appliedForce;
    [SerializeField, Range(0.1f, 2f)] float m_waveFrequency;
    float timer;
    Rigidbody2D m_playerRigidbody;
    
    private void Start()
    {
        m_circleCollider = GetComponent<CircleCollider2D>();
        m_circleCollider.radius = m_range;
        timer = m_waveFrequency;
    }
    private void Update()
    {
        if(timer > 0)
            timer -= Time.deltaTime;
        else
        {
            timer += m_waveFrequency;

            if(m_playerRigidbody != null)
                ApplyForce();
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (Player.Instance == null)
            return;
        if (collision.attachedRigidbody.gameObject != Player.Instance.gameObject)
            return;

        if (m_playerRigidbody == null)
            m_playerRigidbody = collision.attachedRigidbody;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (m_playerRigidbody != null)
            m_playerRigidbody = null;
    }

    void ApplyForce()
    {
        Vector2 dir = Player.Instance.transform.position - transform.position;
        dir.Normalize();
        m_playerRigidbody.AddForce(dir * m_appliedForce,ForceMode2D.Impulse);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position,m_range);
    }
}
