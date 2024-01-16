using System.Collections;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class VideurChaseAndAttack : MonoBehaviour
{
    public enum VideurState
    {
        EnterRoom,
        ChaseAndAttack,
        Die
    }

    public VideurState videurState;

    [SerializeField] private float speed = 1f;

    private Coroutine coroutineState = null;
    private SpriteRenderer spriteRenderer;

    [SerializeField] private GameObject throwingObject;
    [SerializeField] private Transform throwingTransform;
    [SerializeField] private float delayStartChasing = 0.5f;
    [SerializeField] private float delayBetweenShoot = 0.5f;
    [SerializeField] private float rangeDamage = 0.5f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        SetState(VideurState.EnterRoom);
    }

    private void SetState(VideurState value)
    {
        if (coroutineState != null)
            coroutineState = null;

        videurState = value;

        UpdateState();
    }

    private void UpdateState()
    {
        switch (videurState)
        {
            case VideurState.EnterRoom:
                EnterRoom();
                break;
            case VideurState.ChaseAndAttack:
                ChaseAndAttack();
                break;
            case VideurState.Die:
                Die();
                break;
        }
    }

    private void EnterRoom()
    {
        spriteRenderer.color = Color.blue;

        coroutineState = StartCoroutine(IEnterRoom());

        IEnumerator IEnterRoom()
        {
            yield return new WaitForSeconds(delayStartChasing);
            SetState(VideurState.ChaseAndAttack);
        }
    }

    private void ChaseAndAttack()
    {
        spriteRenderer.color = Color.red;

        if (coroutineState == null)
            coroutineState = StartCoroutine(ChaseAndAttack());

        IEnumerator ChaseAndAttack()
        {
            StartCoroutine(Chase());
            StartCoroutine(Attack());

            yield return null;
        }

        IEnumerator Chase()
        {
            while (videurState != VideurState.Die)
            {
                MoveToPlayer();

                yield return null;
            }
        }

        IEnumerator Attack()
        {
            while (videurState != VideurState.Die)
            {
                Instantiate(throwingObject, throwingTransform.position, Quaternion.identity);
                yield return new WaitForSeconds(delayBetweenShoot);
            }
        }
    }

    private void MoveToPlayer()
    {
        Vector3 target = Player.Instance.transform.position;

        if (Vector2.Distance(transform.position, target) < rangeDamage)
            Player.Instance.ApplyHit();

        transform.position += (target - transform.position).normalized * speed * Time.deltaTime;
    }

    private void Die()
    {
        spriteRenderer.color = Color.gray;
        Destroy(gameObject);
    }
}
