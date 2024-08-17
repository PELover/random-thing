using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Movement : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] Rigidbody rb;
    [SerializeField] GameObject cam;
    [SerializeField] Text showvelospeed;
    [SerializeField] GameObject groundcheckobj;
    GameObject touchchecker;
    Touchgroundchecker thistouchchecker;


    [Header("Character State")]
    [SerializeField] string positionstate = "";
    [SerializeField] bool isGrounded = false;
    [SerializeField] bool isDuck = false;
    [SerializeField] bool isLadder = false;

    [Header("In Air Controls")]
    [SerializeField] float MaxAirSpeed = 3.2f;
    [SerializeField] float AirStrafeForce = 20f;
    [SerializeField]float jumpforce = 5.5f;
    Vector3 wishdir = Vector3.zero;
    float ADkey;
    float WSkey;

    [Header("On Ground Control")]
    [SerializeField] bool frictioninclude = false;
    [SerializeField] float MaxGroundSpeed = 4.76f;
    [SerializeField] float friction = 8f;

    [Header("On Ladder Control")]
    [SerializeField] float climbingspeed = 0f;
    [SerializeField] float ladderjumpforce = 0f;
    Vector3 laddermove = Vector3.zero;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        GetDuck();
        feetscheck();
        GetJump();
        Vector3 vel = new Vector3(rb.linearVelocity.x,0f,rb.linearVelocity.z);
        showvelospeed.text = vel.magnitude.ToString("0.0");
        TouchGroundTime();
    }

    void FixedUpdate()
    {
        if (isGrounded)
        {
            if(isLadder)
            {
                positionstate = "Ladder";
            }
            else
            {
                positionstate = "Ground";
            }           
        }
        else if (!isGrounded)
        {
            if (isLadder)
            {
                positionstate = "Ladder";
            }
            else
            {
                positionstate = "Air";
            }
        }
        this.transform.rotation = new Quaternion(0f, cam.transform.rotation.y, 0f, cam.transform.rotation.w);
        ADkey = Input.GetAxis("Horizontal");
        WSkey = Input.GetAxis("Vertical");
        wishdir = transform.right * ADkey + transform.forward * WSkey; // นำค่าไปเก็บที่แกน X แกน Z ตามการหันหน้าของกล้อง , align Wish move direction and View direction                                                                     
        switch (positionstate)
        {
            case "Ground":
                GroundAccelerate(wishdir);
                break;
            case "Air": 
                Airacceletrate(wishdir.normalized);
                break;
            case "Ladder":
                LadderAccelerate();
                break;
        }
    }   

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag.Equals("Ladder"))
        {
            isLadder = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
            rb.useGravity = false;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag.Equals("Ladder"))
        {
            isLadder = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.useGravity = true;
        }
    }

    void TouchGroundTime()
    {
        if (rb.linearVelocity.y < 0.15f)
        {
            frictioninclude = true;
        }
        if(rb.linearVelocity.y > 0.2f)
        {
            frictioninclude = false;
        }
    }

    void feetscheck()
    {
        if (thistouchchecker == null)
        {
            thistouchchecker = gameObject.AddComponent<Touchgroundchecker>();
        }
        isGrounded = thistouchchecker.istouchgroundchecker;
    }
    void GetJump()
    {
        if (isGrounded && (Input.GetButton("Jump") || Input.GetAxis("Mouse ScrollWheel") < 0f))
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpforce, rb.linearVelocity.z);          
        }
    }

    void GetDuck()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isDuck = true;
            this.transform.localScale = new Vector3(1.37f, 1.03f, 1.37f);
        }
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            isDuck = false;
            this.transform.localScale = new Vector3(1.37f, 1.37f, 1.37f);
        }
    }

    void Airacceletrate(Vector3 wishdir)
    {
        rb.linearDamping = 0f;
        /*                      
         view
         ^
         |           v
         |          /|
         |         / |
         |        /  |
         |       /   |
         |      /    |
         |     /     |
         |    /      |
         |   /       |
         |  /        |
         | /       __|
         |/___>___|__|________> wishdir
         |---projv---|
        */

        Vector3 callinearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 projv = Vector3.Project(callinearVelocity, wishdir);

        /*                      
                                view
                                ^
                                |           v
                                |          /|
                                |         / |
                                |        /  |
                                |       /   |
                                |      /    |
                                |     /     |
                                |    /      |
                                |   /       |
                                |  /        |
                                | /       __|
            wishdir <___________|/__>____|__|
                                |---projv---|
       */

        bool isaway = Vector3.Dot(wishdir, projv) <= 0f; // ตรวจทิศทางที่ไปตรงหรือกำลังห่างออกไปจาก projection , check if it moving away or moving toward from the projection vel
        // เพิ่มความเร็วให้ถ้า length ของ projection ยังน้อยกว่า maxspeed หรือ หันออกจาก projection
        if (isaway || projv.magnitude < MaxAirSpeed)
        {
            Vector3 applyforce = wishdir * AirStrafeForce; //คูณทิศทางที่ต้องการไปกับ Force , scale dir with airstrafeforce

            // จำกัดการเร่งไม่ให้เร่งเกิน maxspeed
            if (!isaway)
            {
                applyforce = Vector3.ClampMagnitude(applyforce, MaxAirSpeed - projv.magnitude);
            }
            else
            {
                applyforce = Vector3.ClampMagnitude(applyforce, MaxAirSpeed + projv.magnitude);
            }
            rb.AddForce(applyforce, ForceMode.VelocityChange);
        }
    }

    void GroundAccelerate(Vector3 wishdir)
    {
        if (!isDuck)
        {
            rb.AddForce(wishdir * MaxGroundSpeed * 10f, ForceMode.Force);
        }
        if(isDuck)
        {
            rb.AddForce(wishdir * (MaxGroundSpeed*50/100) * 10f, ForceMode.Force);
        }
        if (frictioninclude)
        {
            rb.linearDamping = friction;
        }
        else
        { 
            rb.linearDamping = 0f;
        }
    }

    void LadderAccelerate()
    {
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        laddermove = transform.up * WSkey + transform.right * ADkey;
        laddermove.Normalize();
        float posy = cam.transform.rotation.eulerAngles.x;
        if(posy > 90)
        {
            posy -= 360;
        }
        posy /= 90;
        if(posy > 0.12)
        {
            posy = 1;
        }
        if(posy < -0.12)
        {
            posy = -1;
        }
        posy *= -1;
        rb.linearVelocity = new Vector3(laddermove.x * climbingspeed, laddermove.y * climbingspeed * posy, rb.linearVelocity.z);                       
        if (isGrounded)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            if (WSkey < 0)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, laddermove.y * climbingspeed, wishdir.z);
            }
        }
        Vector3 ifjump = new Vector3(0f, 0f, ladderjumpforce);
        if(Input.GetButton("Jump"))
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.AddForce(ifjump, ForceMode.Impulse);
        }
    }    
}