using System.Collections;
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
    [SerializeField] private float delayBetweenTp = 3;
    [SerializeField] private float delayBetweenShoot = 0.5f;

    private Coroutine coroutineState = null;
    private Coroutine coroutineDelayInRoom = null;

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

    public void In()
    {
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

    }

    private void ThrowObject()
    {
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
        OpenDoor();

        void OpenDoor()
        {
            Debug.Log("<color=green>Door Open </color>");
        }
    }
}
