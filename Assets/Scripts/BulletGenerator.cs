using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class BulletGenerator : MonoBehaviour
{
    [SerializeField] private EnnemyBullet Bullet;
    List<EnnemyBullet> BulletList = new List<EnnemyBullet>();

    [SerializeField,Range(0.1f,2f)] private float m_delay;
    [SerializeField,Range(1, 5)] private int m_spreadNbr;
    private float m_timer;
    [SerializeField,Range(0f,20f)] private float m_rotationSpeed;
    private float rotation = 0f;
    private void Awake()
    {
        for (int i = 0; i < 5; i++)
        {
            EnnemyBullet NewBullet = Instantiate(Bullet, transform.position, Quaternion.identity, transform);
            BulletList.Add(NewBullet);
            NewBullet.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        m_timer = m_delay;
    }

    void Update()
    {
        if (m_timer > 0)
            m_timer -= Time.deltaTime;
        else
        {
            m_timer += m_delay;
            Shoot();
        }
        rotation += Time.deltaTime * m_rotationSpeed;
    }

    void Shoot()
    {
        for (int i = 0; i < m_spreadNbr; i++)
        {
            Vector2 dir = (Quaternion.Euler(0, 0, (360 / m_spreadNbr) * i) * Quaternion.Euler(0,0, rotation) * Vector2.up).normalized;
            EnnemyBullet NewBullet = GetAvailableBullet();
            NewBullet.InitBullet(dir, transform.position);
        }
    }

    EnnemyBullet GetAvailableBullet()
    {
        for(int i=0; i<BulletList.Count; i++)
        {
            if (!BulletList[i].gameObject.activeInHierarchy)
            {
                BulletList[i].gameObject.SetActive(true);
                return BulletList[i];
            } 
        }

        EnnemyBullet NewBullet = Instantiate(Bullet, transform.position, Quaternion.identity, transform);
        BulletList.Add(NewBullet);
        return NewBullet;
    }
}