using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum State
{
    Nothing,
    In,
    Chase,
    TPAroundPlayer,
    ThrowObject,
    Out
}

public class Videur : MonoBehaviour
{
    [SerializeField] private State state;
    [SerializeField] private float delayInRoom = 10;
    [SerializeField] private float speed = 3f;

    [SerializeField] private GameObject throwingObject;
    [SerializeField] private Transform throwingTransform;
    [SerializeField] private float rangeTp = 1.5f;
    [SerializeField] private float delayBetweenTp = 1.5f;
    [SerializeField] private float delayBetweenShoot = 0.5f;

    private Coroutine coroutineState = null;
    private Coroutine coroutineDelayInRoom = null;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        SetState(State.In);
    }

    public void SetState(State value)
    {
        if (coroutineState != null)
            coroutineState = null;

        if (coroutineState == null)
            coroutineDelayInRoom = null;

        state = value;

        UpdateState();
    }

    private void Update()
    {
        //if (Player.Instance.gameObject)
        //    transform.LookAt(Player.Instance.gameObject.transform);
    }

    private void UpdateState()
    {
        switch (state)
        {
            case State.Nothing:
                Nothing();
                break;
            case State.In:
                In();
                break;
            case State.Chase:
                Chase();
                break;
            case State.TPAroundPlayer:
                TPAroundPlayer();
                break;
            case State.ThrowObject:
                ThrowObject();
                break;
            case State.Out:
                Out();
                break;
        }
    }

    private void Nothing()
    {
        spriteRenderer.enabled = false;
    }

    public void In()
    {
        spriteRenderer.enabled = true;
        spriteRenderer.color = Color.yellow;

        switch (Random.Range(0, 3))
        {
            case 0:
                SetState(State.Chase);
                Debug.Log("Chase");
                break;
            case 1:
                SetState(State.TPAroundPlayer);
                Debug.Log("TPAroundPlayer");
                break;
            case 2:
                SetState(State.ThrowObject);
                Debug.Log("ThrowObject");
                break;
            default:
                break;
        }

        if (coroutineDelayInRoom == null)
            coroutineDelayInRoom = StartCoroutine(IDelayInRoom());

        CloseDoor();

        IEnumerator IDelayInRoom()
        {
            Debug.Log("<color=blue>Started </color>");

            yield return new WaitForSeconds(delayInRoom);

            SetState(State.Out);
        }

        void CloseDoor()
        {
            Debug.Log("<color=red>Door Closed </color>");
        }
    }

    private void Chase()
    {
        spriteRenderer.color = Color.red;

        if (coroutineState == null)
            coroutineState = StartCoroutine(IChasePlayer());

        IEnumerator IChasePlayer()
        {
            while (state == State.Chase)
            {
                MoveTo(Player.Instance.gameObject.transform.position);

                yield return null;
            }
        }
    }

    private void MoveTo(Vector2 target)
    {
        transform.position += ((Vector3)target - transform.position).normalized * speed * Time.deltaTime;
    }

    private void TPAroundPlayer()
    {
        spriteRenderer.color = Color.blue;

        if (coroutineState == null)
            coroutineState = StartCoroutine(ITPAroundPlayer());

        IEnumerator ITPAroundPlayer()
        {
            while (state == State.TPAroundPlayer)
            {
                Vector3 targetForTp = FindPositionNearestToPlayer();

                yield return new WaitForSeconds(delayBetweenTp);
                transform.position = targetForTp;
            }
        }
    }

    private void ThrowObject()
    {
        spriteRenderer.color = Color.magenta;

        if (coroutineState == null)
            coroutineState = StartCoroutine(IDelayShoot());

        IEnumerator IDelayShoot()
        {
            while (state == State.ThrowObject)
            {
                Instantiate(throwingObject, throwingTransform.position, Quaternion.identity);
                yield return new WaitForSeconds(delayBetweenShoot);
            }
        }
    }

    private void Out()
    {
        spriteRenderer.color = Color.green;

        if (coroutineState == null)
            coroutineState = StartCoroutine(IEscape());

        OpenDoor();

        void OpenDoor()
        {
            Debug.Log("<color=green>Door Open </color>");
        }

        IEnumerator IEscape()
        {
            while (state == State.Out)
            {
                if (Vector2.Distance(transform.position, FindDoor()) > 0.5f)
                    MoveTo(FindDoor());
                else
                    SetState(State.Nothing);

                yield return null;
            }
        }
    }

    private Vector3 FindDoor()
    {
        GameObject door = GameObject.FindGameObjectWithTag("Door");

        if (!door)
            return Vector3.zero;

        return door.transform.position;
    }

    private Vector3 FindPositionNearestToPlayer()
    {
        return Player.Instance.gameObject.transform.position + (Vector3)Random.insideUnitCircle * rangeTp;
    }
}
