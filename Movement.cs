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

    [Header("Character State")]
    [SerializeField] string positionstate = "";
    [SerializeField] bool isGrounded = false;

    [Header("In Air Controls")]
    [SerializeField] float MaxAirSpeed = 3.2f;
    [SerializeField] float AirStrafeForce = 20f;
    [SerializeField]float jumpforce = 5.5f;
    Vector3 wishdir = Vector3.zero;
    float ADkey;
    float WSkey;

    [Header("On Ground Control")]
    [SerializeField] float MaxGroundSpeed = 4.76f;





    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
       // Cursor.visible = false;
    }
    private void Update()
    {
        GetJump();
        Vector3 vel = new Vector3(rb.linearVelocity.x,0f,rb.linearVelocity.z);
        showvelospeed.text = vel.magnitude.ToString("0.0");
        //print($"Velocity = {vel.magnitude}");
    }
    void FixedUpdate()
    {
        if(isGrounded)
        {
            positionstate = "Ground";
        }
        else if(!isGrounded)
        {
            positionstate = "Air";
        }
        
        this.transform.rotation = new Quaternion(0f, cam.transform.rotation.y, 0f, cam.transform.rotation.w);       
        ADkey = Input.GetAxis("Horizontal");
        WSkey = Input.GetAxis("Vertical");
        wishdir = transform.right * ADkey + transform.forward * WSkey; // �Ӥ����纷��᡹ X ᡹ Z �������ѹ˹�Ңͧ���ͧ , align Wish move direction and View direction                                                                     
        switch (positionstate)
        {
            case "Ground":
                print("we on ground");
                break;
            case "Air": 
                Airacceletrate(wishdir.normalized);
                break;
        }
    }
    void GetJump()
    {
        if (isGrounded && Input.GetButton("Jump"))
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpforce, rb.linearVelocity.z);
        }
    }
    void Airacceletrate(Vector3 wishdir)
    {
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

        bool isaway = Vector3.Dot(wishdir, projv) <= 0f; // ��Ǩ��ȷҧ���仵ç���͡��ѧ��ҧ�͡仨ҡ projection , check if it moving away or moving toward from the projection vel
        // ����������������� length �ͧ projection �ѧ���¡��� maxspeed ���� �ѹ�͡�ҡ projection
        if (isaway || projv.magnitude < MaxAirSpeed)
        {
            Vector3 applyforce = wishdir * AirStrafeForce; //�ٳ��ȷҧ����ͧ���仡Ѻ Force , scale dir with airstrafeforce

            // �ӡѴ����������������Թ maxspeed
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
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag.Equals("Ground"))
        {
            isGrounded = true;
           
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag.Equals("Ground"))
        {
            isGrounded = false;

        }
        
    }
   

}
