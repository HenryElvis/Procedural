using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SpikeBehavior : MonoBehaviour
{
    [SerializeField] private float delayUp;
    [SerializeField] private float delayDown;

    [SerializeField] Sprite spikeSprite;
    [SerializeField] Sprite noSpikeSprite;

    private BoxCollider2D _collider;
    private Coroutine _stateCoroutine;

    private SpriteRenderer _spriteRenderer;

    private enum SpikeState
    {
        UP,
        DOWN
    }

    private SpikeState _spikeState;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<BoxCollider2D>();
    }

    // Start is called before the first frame update
    private void Start()
    {
        _spriteRenderer.sprite = spikeSprite;
        SetState(SpikeState.UP);
    }

    private void SetState(SpikeState value)
    {
        if (_stateCoroutine != null)
            _stateCoroutine = null;

        _spikeState = value;

        UpdateState();
    }

    private void UpdateState()
    {
        switch (_spikeState)
        {
            case SpikeState.UP:
                Up();
                break;
            case SpikeState.DOWN:
                Down();
                break;
        }
    }

    private void Up()
    {
        _stateCoroutine = StartCoroutine(IDelay(delayUp, SpikeUp));

        void SpikeUp()
        {
            _collider.enabled = true;
            _spriteRenderer.sprite = spikeSprite;

            SetState(SpikeState.DOWN);
        }
    }

    private void Down()
    {
        _stateCoroutine = StartCoroutine(IDelay(delayUp, SpikeDown));

        void SpikeDown()
        {
            _collider.enabled = true;
            _spriteRenderer.sprite = noSpikeSprite;

            SetState(SpikeState.UP);
        }
    }

    private IEnumerator IDelay(float delay, UnityAction action)
    {
        yield return new WaitForSeconds(delay);
        action.Invoke();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (Player.Instance == null)
            return;
        if (collision.attachedRigidbody.gameObject != Player.Instance.gameObject)
            return;

        Player.Instance.ApplyHit(null);
    }
}
