using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;

public class Bullet : MonoBehaviour
{
    public float ThrowSpeed = 5;
    public float maxPullDis;

    private float TimeSinceThown;
    private float birdVelocX, birdVelocY;
    private Vector3 ReleasePos;
    private Transform HookPoint;
    private Rigidbody2D myrb;
    private GameController gc;
    private LineRenderer TrajectoryLineRenderer;
    private BulletState bulletState;

    //Test collision
    private float nextPosX, nextPosY;
    public LayerMask enemyLayer;

    int frame;
    // Start is called before the first frame update
    void Start()
    {
        bulletState = BulletState.Idle;
        myrb = GetComponent<Rigidbody2D>();
        HookPoint = GameObject.FindGameObjectWithTag("HookPoint").transform;
        gc = FindObjectOfType<GameController>();
        TrajectoryLineRenderer = FindObjectOfType<LineRenderer>();
        TrajectoryLineRenderer.transform.position = Vector2.zero;
        frame = 0;
    }

    // Update is called once per frame
    void Update()
    {
        frame++;
        Debug.Log(frame);
        if (DetectCollisionNextFrame())
        {
            Debug.Log("Collided at frame: " + (frame + 1));
        }
        if (bulletState == BulletState.Pulling)
        {
            Vector3 pullingPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (Vector2.Distance(pullingPos, HookPoint.position) < maxPullDis)
            {
                transform.position = pullingPos;
            }else
            {
                transform.position = HookPoint.position + new Vector3(pullingPos.x - HookPoint.position.x, pullingPos.y - HookPoint.position.y, 0).normalized * maxPullDis;
            }
            transform.position += new Vector3(0, 0, 10);

            float distance = Vector2.Distance(HookPoint.position, transform.position);
            if (distance < 0.8f)
            {
                TrajectoryLineRenderer.enabled = false;
            }
            else
            {
                DisplayTrajectoryLineRenderer(distance);
            }
            ReleasePos = transform.position;

        }
    }

    private void FixedUpdate()
    {
        if (bulletState == BulletState.Flying)
        {
            if (DetectCollisionNextFrame()) return;
            TimeSinceThown += Time.fixedDeltaTime;

            float posX = ReleasePos.x + TimeSinceThown * birdVelocX;
            float posY = ReleasePos.y + TimeSinceThown * birdVelocY - 0.5f * Physics2D.gravity.magnitude * Mathf.Pow(TimeSinceThown, 2);
            transform.position = new Vector3(posX, posY);
        }
    }

    private void OnMouseDown()
    {
        if (bulletState != BulletState.Flying)
        {
            bulletState = BulletState.Pulling;
            myrb.isKinematic = true;
        }
    }

    private void OnMouseUp()
    {
        if (bulletState != BulletState.Flying)
        {
            bulletState = BulletState.Flying;
            myrb.isKinematic = false;
            TrajectoryLineRenderer.enabled = false;
            TimeSinceThown = 0;
            float distance = Vector2.Distance(HookPoint.position, transform.position);
            ThrowBird(distance);
            GetComponent<Collider2D>().isTrigger = false;
            StartCoroutine(DestroyCo());
           
        }
    }


    private void ThrowBird(float distance)
    {

        Vector2 direct = HookPoint.position - transform.position;

        Vector2 birdVeloc = direct * ThrowSpeed * distance;
        float alpha = Mathf.Atan2(birdVeloc.y, birdVeloc.x);
        birdVelocX = birdVeloc.magnitude * Mathf.Cos(alpha);
        birdVelocY = birdVeloc.magnitude * Mathf.Sin(alpha);

    }

    private IEnumerator DestroyCo()
    {
        yield return new WaitForSeconds(5f);
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Obstacle"))
        {
            Destroy(other.gameObject);
            Destroy(gameObject);
            gc.IncreaseScore();
        }

        if (other.gameObject.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }

    bool DetectCollisionNextFrame()
    {
        float nextFrameTime = TimeSinceThown + Time.deltaTime;
        nextPosX = ReleasePos.x + nextFrameTime * birdVelocX;
        nextPosY = ReleasePos.y + nextFrameTime * birdVelocY - 0.5f * Physics2D.gravity.magnitude * Mathf.Pow(TimeSinceThown, 2);
        Collider2D[] ColObject = Physics2D.OverlapCircleAll(new Vector2(nextPosX, nextPosY), GetComponent<CircleCollider2D>().radius, enemyLayer);
        if (ColObject.Length > 0) return true;
        else return false;
    }



    void DisplayTrajectoryLineRenderer(float distance)
    {
        TrajectoryLineRenderer.enabled = true;
        Vector2 v2 = new Vector2(HookPoint.position.x, HookPoint.position.y) - myrb.position;
        int segmentCount = 15;
        Vector2[] segments = new Vector2[segmentCount];

        segments[0] = transform.position;

        Vector2 segVelocity = v2 * ThrowSpeed * distance;
        for (int i = 1; i < segmentCount; i++)
        {
            float time = i * Time.fixedDeltaTime * 3;
            segments[i] = segments[0] + segVelocity * time + 0.5f * Physics2D.gravity * Mathf.Pow(time, 2);
        }

        TrajectoryLineRenderer.positionCount = segmentCount;
        for (int i = 0; i < segmentCount; i++)
            TrajectoryLineRenderer.SetPosition(i, segments[i]);
    }



}
