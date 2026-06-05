using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovementController : MonoBehaviour
{
    public float speedMove = 3f;
    public float speedTurn = 10f;
    public float jumpForce = 3f;
    public int maxJump = 2;
    public Joystick joystickInput;
    public Animator anim;

    Rigidbody rigid;
    Transform camTransform;
    int jumpCount;
    bool isGrounded = false;

    void Start()
    {
        rigid = GetComponent<Rigidbody>();
        camTransform = Camera.main.transform;
    }

    void FixedUpdate() => ProcessMove();

    void Update()
    {
        ProcessKeyboardJump();
    }

    void ProcessMove()
    {
        float h = Mathf.Abs(joystickInput ? joystickInput.Horizontal : 0) >
                  Mathf.Abs(Input.GetAxisRaw("Horizontal"))
            ? joystickInput.Horizontal : Input.GetAxisRaw("Horizontal");

        float v = Mathf.Abs(joystickInput ? joystickInput.Vertical : 0) >
                  Mathf.Abs(Input.GetAxisRaw("Vertical"))
            ? joystickInput.Vertical : Input.GetAxisRaw("Vertical");

        Vector3 forward = camTransform.forward; forward.y = 0; forward.Normalize();
        Vector3 right = camTransform.right; right.y = 0; right.Normalize();

        Vector3 moveDir = Vector3.ClampMagnitude(forward * v + right * h, 1f);

        if (moveDir.sqrMagnitude > 0.01f)
        {
            rigid.velocity = new Vector3(moveDir.x * speedMove, rigid.velocity.y, moveDir.z * speedMove);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), speedTurn * Time.fixedDeltaTime);
            anim.SetBool("Run", true);
        }
        else
        {
            rigid.velocity = new Vector3(0, rigid.velocity.y, 0);
            anim.SetBool("Run", false);
            rigid.angularVelocity = Vector3.zero;
        }
    }
    public void Jump()
    {
        if (jumpCount >= maxJump) return;
        PerformJump();
    }

    void ProcessKeyboardJump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (jumpCount >= maxJump) return;
            PerformJump();
        }
    }

    void PerformJump()
    {
        rigid.velocity = new Vector3(rigid.velocity.x, 0, rigid.velocity.z);
        rigid.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        jumpCount++;
        anim.SetTrigger("Jump");
    }

    void OnCollisionEnter(Collision col)
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
       //tuognw tác vật 

    }

   

    private void OnTriggerStay(Collider other)
    {
        if (other.transform.IsChildOf(transform) || other.gameObject == gameObject) return;

        isGrounded = true;
        jumpCount = 0;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.IsChildOf(transform) || other.gameObject == gameObject) return;

        isGrounded = false;
        if (jumpCount == 0)
        {
            jumpCount = 1;
        }
        
    }


}
