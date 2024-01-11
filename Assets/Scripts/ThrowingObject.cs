using UnityEngine;

public class ThrowingObject : MonoBehaviour
{
    private Vector2 direction;

    [SerializeField] private float speed = 5f;
    [SerializeField] private float delayBeforeDestroyingObject = 3f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    private void OnEnable()
    {
        direction = ((Vector3)direction - transform.position).normalized;

        Color color = Random.ColorHSV(0.5f, 1);
        spriteRenderer.color = color;

        direction = Player.Instance.gameObject.transform.position;
        Destroy(gameObject, delayBeforeDestroyingObject);
    }

    // Update is called once per frame
    private void Update()
    {
        transform.position += (Vector3)direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        direction = Vector2.zero;
        speed = 0f;

        if (collision.gameObject.transform.parent.CompareTag("Player"))
        {
            Player.Instance.ApplyHit(null);
            Destroy(gameObject);
        }
    }
}
