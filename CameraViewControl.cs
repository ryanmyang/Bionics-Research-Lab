using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraViewControl : MonoBehaviour
{

    public GameObject avatar;
    public MotorLearningGameOculusTouch motorLearning;
    int arm;
    int plane;
    // Start is called before the first frame update
    void Start()
    {
        // motorLearning = gameObject.GetComponent<MotorLearningGameOculusTouch>();
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void updateCameraView() {
        Vector3 posCamera = new Vector3(.0f, 1.72f, -1.436f);
        Vector3 rotCamera = new Vector3(10.0f, 0.0f, 0.0f); ;
        Vector3 avatarPos = new Vector3(0, 0, 0);
        avatarPos = avatar.transform.position;
        motorLearning.getArmAndPlane(out int arm, out int plane);
        Debug.LogWarning("Arm: " + arm.ToString() + ", Plane: " + plane.ToString());

        if(plane == 0)
        {
            if (arm == 0)
            {
                posCamera = new Vector3(0.0f, 1.72f, -1.436f);
                rotCamera = new Vector3(10.0f, 0.0f, 0.0f);
            }
            else
            {
                posCamera = new Vector3(0.0f, 1.72f, -1.436f);
                rotCamera = new Vector3(10.0f, 0.0f, 0.0f);
            }
        }
        else if (plane == 1) 
        {
            if (arm == 0)
            {  posCamera = new Vector3(2.0f, 1.5f, 0.0f);
                rotCamera = new Vector3(10.0f, -90.0f, 0.0f);
                
            }
            else
            {
              posCamera = new Vector3(-2.0f, 1.5f, 0.0f);
                rotCamera = new Vector3(10.0f, 90.0f, 0.0f);
            }
        }
        else
        {
            if (arm == 0)
            {
                posCamera = new Vector3(0.0f, 2.5f, 0.0f);
                rotCamera = new Vector3(80.0f, 0.0f, 0.0f);
            }
            else
            {
                posCamera = new Vector3(0.0f, 2.5f, 0.0f);
                rotCamera = new Vector3(80.0f, 0.0f, 0.0f);
            }
        }
        // Debug.LogWarning("Setting Position to: " + (avatarPos + posCamera).ToString());
        transform.position = avatarPos + posCamera;
        transform.eulerAngles = rotCamera;
    }


    
}
