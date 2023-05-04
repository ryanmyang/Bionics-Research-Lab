//attach this script to avatar
//read data from sensorManager, then move avatar bones
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SensorPlugin;

using System.Runtime.InteropServices;

public class AvatarSensor : MonoBehaviour
{
    MotionSensorManager sensorManager;

    public const int TPOSE = 0;
    public const int NPOSE = 1;
    public const int HPOSE = 2;
    public const int LPOSE = 3;
    public const int NPPOSE = 4;

    public UnityEngine.UI.Dropdown poseDropdown;
    public UnityEngine.UI.InputField LPoseInputField;

    public enum HandModes : int { Binary, Continuous, ThreeFingers }
    [Tooltip("Choose hand finger mode")]
    public HandModes handMode = HandModes.Binary;

    private float[] targetJointAngle_leftarm = new float[10];
    private float[] targetJointAngle_rightarm = new float[10];

    private float jointSpeed = 50.0f;
    private float bodySpeed = 1.0f;
    private float rotateSpeed = 50.0f;

    // The body root node
    protected Transform bodyRoot;

    private float targetAngle;

    //avatar test

    [NonSerialized]
    public Int64 playerId = 0;



    // Variable to hold all them bones. It will initialize the same size as initialRotations.
    protected Transform[] bones;

    // Rotations of the bones when the Kinect tracking starts.
    protected Quaternion[] initialRotations;
    protected Quaternion[] localRotations;
    protected bool[] isBoneDisabled;

    // Local rotations of finger bones
    protected Dictionary<HumanBodyBones, Quaternion> fingerBoneLocalRotations = new Dictionary<HumanBodyBones, Quaternion>();
    protected Dictionary<HumanBodyBones, Vector3> fingerBoneLocalAxes = new Dictionary<HumanBodyBones, Vector3>();

    // Initial position and rotation of the transform
    protected Vector3 initialPosition;
    protected Quaternion initialRotation;
    protected Vector3 initialHipsPosition;
    protected Quaternion initialHipsRotation;

    protected Vector3 offsetNodePos;
    protected Quaternion offsetNodeRot;
    protected Vector3 bodyRootPosition;


    // Calibration Offset Variables for Character Position.
    [NonSerialized]
    public bool offsetCalibrated = false;
    protected Vector3 offsetPos = Vector3.zero;
    //protected float xOffset, yOffset, zOffset;
    //private Quaternion originalRotation;

    private Animator animatorComponent = null;
    private HumanPoseHandler humanPoseHandler = null;
    private HumanPose humanPose = new HumanPose();


    // whether the parent transform obeys physics
    protected bool isRigidBody = false;

    private Transform leftFoot, rightFoot;

    private GameObject offsetNode;
    public int count = 0;
    //golf
    public GameObject golfStickObj;
    private GameObject hand_l, hand_r;
    //
    // Use this for initialization
    void Start()
    {
        hand_l = GameObject.Find("thumb_01_l");
        hand_r = GameObject.Find("thumb_01_r");
        sensorManager = MotionSensorManager.Instance;

        // Add listener for the dropdown to call dropdown change function
        poseDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    private void OnDropdownValueChanged(int index)
    {
        
        Debug.Log("Selected option: " + poseDropdown.options[index].text);

        // Adjust index for the fact that 0 is not a pose
        if (index > 0) { 
        SetModelArmsInPose(index - 1);
        }
        // Reset dropdown
        //poseDropdown.value = 0;
    }

    void GetKeyInput()
    {
        //body trans
        if (Input.GetKey(KeyCode.RightArrow))
        {
            bodyRootPosition += Vector3.right * bodySpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            bodyRootPosition += Vector3.left * bodySpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            bodyRootPosition += Vector3.forward * bodySpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            bodyRootPosition += Vector3.back * bodySpeed * Time.deltaTime;
        }

        //body rot
        /*if (Input.GetKey(KeyCode.RightArrow))
        {
            targetAngle += Input.GetAxis("Horizontal") * Time.deltaTime * rotateSpeed;
            Quaternion targetRotation = Quaternion.AngleAxis(targetAngle, Vector3.up);
            transform.rotation = targetRotation;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            targetAngle += Input.GetAxis("Horizontal") * Time.deltaTime * rotateSpeed;
            Quaternion targetRotation = Quaternion.AngleAxis(targetAngle, Vector3.up);
            transform.rotation = targetRotation;
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            targetAngle += Input.GetAxis("Vertical") * Time.deltaTime * rotateSpeed;
            Quaternion targetRotation = Quaternion.AngleAxis(targetAngle, Vector3.right);
            transform.rotation = targetRotation;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            targetAngle += Input.GetAxis("Vertical") * Time.deltaTime * rotateSpeed;
            Quaternion targetRotation = Quaternion.AngleAxis(targetAngle, Vector3.right);
            transform.rotation = targetRotation;
        }*/
        //bone joint move
        //left
        //1
        if (Input.GetKey(KeyCode.Alpha1))
        {
            targetJointAngle_leftarm[0] += Time.deltaTime * jointSpeed;
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            targetJointAngle_leftarm[0] -= Time.deltaTime * jointSpeed;
        }
        //2
        if (Input.GetKey(KeyCode.Alpha2))
        {
            targetJointAngle_leftarm[1] += Time.deltaTime * jointSpeed;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            targetJointAngle_leftarm[1] -= Time.deltaTime * jointSpeed;
        }
        //3
        if (Input.GetKey(KeyCode.Alpha3))
        {
            targetJointAngle_leftarm[2] += Time.deltaTime * jointSpeed;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            targetJointAngle_leftarm[2] -= Time.deltaTime * jointSpeed;
        }
        //4
        if (Input.GetKey(KeyCode.Alpha4))
        {
            targetJointAngle_leftarm[3] += Time.deltaTime * jointSpeed;
        }
        else if (Input.GetKey(KeyCode.R))
        {
            targetJointAngle_leftarm[3] -= Time.deltaTime * jointSpeed;
        }
        //5
        if (Input.GetKey(KeyCode.Alpha5))
        {
            targetJointAngle_leftarm[4] += Time.deltaTime * jointSpeed;
        }
        else if (Input.GetKey(KeyCode.T))
        {
            targetJointAngle_leftarm[4] -= Time.deltaTime * jointSpeed;
        }
        //6
        if (Input.GetKey(KeyCode.Alpha6))
        {
            targetJointAngle_leftarm[5] += Time.deltaTime * jointSpeed;
        }
        else if (Input.GetKey(KeyCode.Y))
        {
            targetJointAngle_leftarm[5] -= Time.deltaTime * jointSpeed;
        }
        //7
        if (Input.GetKey(KeyCode.Alpha7))
        {
            targetJointAngle_leftarm[6] += Time.deltaTime * jointSpeed;
        }
        else if (Input.GetKey(KeyCode.U))
        {
            targetJointAngle_leftarm[6] -= Time.deltaTime * jointSpeed;
        }
        //8
        if (Input.GetKey(KeyCode.Alpha8))
        {
            if (handMode == HandModes.Binary)
                targetJointAngle_leftarm[7] = 100.0f;
            else
                targetJointAngle_leftarm[7] += Time.deltaTime * jointSpeed;
        }
        else if (Input.GetKey(KeyCode.I))
        {
            if (handMode == HandModes.Binary)
                targetJointAngle_leftarm[7] = 0.0f;
            else
                targetJointAngle_leftarm[7] -= Time.deltaTime * jointSpeed;
        }
        //9
        if (Input.GetKey(KeyCode.Alpha9))
        {
            targetJointAngle_leftarm[8] += Time.deltaTime * jointSpeed;
        }
        else if (Input.GetKey(KeyCode.O))
        {
            targetJointAngle_leftarm[8] -= Time.deltaTime * jointSpeed;
        }
        //10
        if (Input.GetKey(KeyCode.Alpha0))
        {
            targetJointAngle_leftarm[9] += Time.deltaTime * jointSpeed;
        }
        else if (Input.GetKey(KeyCode.P))
        {
            targetJointAngle_leftarm[9] -= Time.deltaTime * jointSpeed;
        }
        //right
        //1
        if (Input.GetKey(KeyCode.A))
        {
            targetJointAngle_rightarm[0] += Time.deltaTime * jointSpeed;
        }
        else if (Input.GetKey(KeyCode.Z))
        {
            targetJointAngle_rightarm[0] -= Time.deltaTime * jointSpeed;
        }
        //2
        if (Input.GetKey(KeyCode.S))
        {
            targetJointAngle_rightarm[1] += Time.deltaTime * jointSpeed;
        }
        else if (Input.GetKey(KeyCode.X))
        {
            targetJointAngle_rightarm[1] -= Time.deltaTime * jointSpeed;
        }
        //3
        if (Input.GetKey(KeyCode.D))
        {
            targetJointAngle_rightarm[2] += Time.deltaTime * jointSpeed;
        }
        else if (Input.GetKey(KeyCode.C))
        {
            targetJointAngle_rightarm[2] -= Time.deltaTime * jointSpeed;
        }
        //4
        if (Input.GetKey(KeyCode.F))
        {
            targetJointAngle_rightarm[3] += Time.deltaTime * jointSpeed;
        }
        else if (Input.GetKey(KeyCode.V))
        {
            targetJointAngle_rightarm[3] -= Time.deltaTime * jointSpeed;
        }
        //5
        if (Input.GetKey(KeyCode.G))
        {
            targetJointAngle_rightarm[4] += Time.deltaTime * jointSpeed;
        }
        else if (Input.GetKey(KeyCode.B))
        {
            targetJointAngle_rightarm[4] -= Time.deltaTime * jointSpeed;
        }
        //6
        if (Input.GetKey(KeyCode.H))
        {
            targetJointAngle_rightarm[5] += Time.deltaTime * jointSpeed;
        }
        else if (Input.GetKey(KeyCode.N))
        {
            targetJointAngle_rightarm[5] -= Time.deltaTime * jointSpeed;
        }
        //7
        if (Input.GetKey(KeyCode.J))
        {
            targetJointAngle_rightarm[6] += Time.deltaTime * jointSpeed;
        }
        else if (Input.GetKey(KeyCode.M))
        {
            targetJointAngle_rightarm[6] -= Time.deltaTime * jointSpeed;
        }
        //8
        if (Input.GetKey(KeyCode.K))
        {

            if (handMode == HandModes.Binary)
                targetJointAngle_rightarm[7] = 100.0f;
            else
                targetJointAngle_rightarm[7] += Time.deltaTime * jointSpeed;
        }
        else if (Input.GetKey(KeyCode.Comma))
        {

            if (handMode == HandModes.Binary)
                targetJointAngle_rightarm[7] = 0.0f;
            else
                targetJointAngle_rightarm[7] -= Time.deltaTime * jointSpeed;
        }
        //9
        if (Input.GetKey(KeyCode.L))
        {
            targetJointAngle_rightarm[8] += Time.deltaTime * jointSpeed;
        }
        else if (Input.GetKey(KeyCode.Period))
        {
            targetJointAngle_rightarm[8] -= Time.deltaTime * jointSpeed;
        }
        //10
        if (Input.GetKey(KeyCode.Semicolon))
        {
            targetJointAngle_rightarm[9] += Time.deltaTime * jointSpeed;
        }
        else if (Input.GetKey(KeyCode.Slash))
        {
            targetJointAngle_rightarm[9] -= Time.deltaTime * jointSpeed;
        }
    }

    void MoveBody()
    {
        transform.position = bodyRootPosition;
        RotateBoneJoints(0);
    }

    void Update()
    {
        count++;
        GetKeyInput();
        MoveBody();
        MoveGolfStick();
    }
    void MoveGolfStick()
    {
        if (golfStickObj != null)
        {
            //rot
            int sensorID = 15;
            //Quaternion eulerRotation = Quaternion.Euler(ttt);
            //Quaternion sensorRotation = Quaternion.Inverse(eulerRotation) * quatSensor[sensorID] * Quaternion.Inverse(Quaternion.AngleAxis(sensorBodyRotation, Vector3.up) * quatSensor_offset[sensorID]);
            //Quaternion sensorRotation = Quaternion.Inverse(eulerRotation) * sensorManager.getSensorRotation(sensorID);
            //Quaternion jointRotation = sensorRotation *eulerRotation;
            Quaternion jointRotation = sensorManager.getSensorRotation(sensorID);
            Quaternion newRotation = jointRotation;
            newRotation = initialRotation * newRotation;
            golfStickObj.transform.rotation = newRotation;
            //
            //Quaternion newRotation = Quaternion.Lerp(hand_l.transform.rotation, hand_r.transform.rotation, 0.5f);
            //golfStickObj.transform.rotation = newRotation;
            //pos
            if (hand_l != null && hand_r != null)
            {
                Vector3 newPos = (hand_l.transform.position + hand_r.transform.position) * 0.5f;
                golfStickObj.transform.position = newPos;
            }
        }
    }

    public void Awake()
    {
        // check for double start
        if (bones != null)
            return;
        if (!gameObject.activeInHierarchy)
            return;

        // inits the bones array
        bones = new Transform[31];

        // get the animator reference
        animatorComponent = GetComponent<Animator>();

        // Map bones to the points the Kinect tracks
        MapBones();
        // Set model's arms to be in T-pose, if needed

        // Initial rotations and directions of the bones.
        initialRotations = new Quaternion[bones.Length];
        localRotations = new Quaternion[bones.Length];
        isBoneDisabled = new bool[bones.Length];


        //SetModelArmsInTpose();
        
        SetModelArmsInPose(TPOSE);




        // enable all bones
        for (int i = 0; i < bones.Length; i++)
        {
            isBoneDisabled[i] = false;
        }


        // if parent transform uses physics
        isRigidBody = (gameObject.GetComponent<Rigidbody>() != null);

        // get the pose handler reference
        if (animatorComponent && animatorComponent.avatar && animatorComponent.avatar.isHuman)
        {
            //Transform hipsTransform = animator.GetBoneTransform(HumanBodyBones.Hips);
            //Transform rootTransform = hipsTransform.parent;
            Transform rootTransform = transform;

            humanPoseHandler = new HumanPoseHandler(animatorComponent.avatar, rootTransform);
            humanPoseHandler.GetHumanPose(ref humanPose);
        }
    }

    protected void TransformSpecialBoneFingers(int boneIndex, float[] fingerAngles)
    {
        float angle = 0;
        var startIndex = 0;
        var endIndex = 0;
        // get the animator component
        //Animator animatorComponent = GetComponent<Animator>();
        if (!animatorComponent)
            return;

        {
            // get the list of bones
            List<HumanBodyBones> alBones = specialIndex2MultiBoneMap[boneIndex + 2];
            startIndex = 1;
            endIndex = 3;
            angle = fingerAngles[0];
            for (int i = startIndex; i < endIndex; i++)
            {
                HumanBodyBones bone = alBones[i];
                Transform boneTransform = animatorComponent.GetBoneTransform(bone);

                // set the fist rotation
                if (boneTransform && fingerBoneLocalAxes[bone] != Vector3.zero)
                {
                    Quaternion qRotFinger = Quaternion.AngleAxis(angle, fingerBoneLocalAxes[bone]);
                    boneTransform.localRotation = fingerBoneLocalRotations[bone] * qRotFinger;
                }
            }
        }
        {
            List<HumanBodyBones> alBones = specialIndex2MultiBoneMap[boneIndex];
            startIndex = 0; //index finger
            endIndex = 3;
            angle = fingerAngles[1];
            for (int i = startIndex; i < endIndex; i++)
            {
                HumanBodyBones bone = alBones[i];
                Transform boneTransform = animatorComponent.GetBoneTransform(bone);

                // set the fist rotation
                if (boneTransform && fingerBoneLocalAxes[bone] != Vector3.zero)
                {
                    Quaternion qRotFinger = Quaternion.AngleAxis(angle, fingerBoneLocalAxes[bone]);
                    boneTransform.localRotation = fingerBoneLocalRotations[bone] * qRotFinger;
                }
            }
            startIndex = 3; //middle finger
            endIndex = 6;
            angle = fingerAngles[2];
            for (int i = startIndex; i < endIndex; i++)
            {
                HumanBodyBones bone = alBones[i];
                Transform boneTransform = animatorComponent.GetBoneTransform(bone);

                // set the fist rotation
                if (boneTransform && fingerBoneLocalAxes[bone] != Vector3.zero)
                {
                    Quaternion qRotFinger = Quaternion.AngleAxis(angle, fingerBoneLocalAxes[bone]);
                    boneTransform.localRotation = fingerBoneLocalRotations[bone] * qRotFinger;
                }
            }
            startIndex = 6; //ring finger
            endIndex = 9;
            angle = fingerAngles[3];
            for (int i = startIndex; i < endIndex; i++)
            {
                HumanBodyBones bone = alBones[i];
                Transform boneTransform = animatorComponent.GetBoneTransform(bone);

                // set the fist rotation
                if (boneTransform && fingerBoneLocalAxes[bone] != Vector3.zero)
                {
                    Quaternion qRotFinger = Quaternion.AngleAxis(angle, fingerBoneLocalAxes[bone]);
                    boneTransform.localRotation = fingerBoneLocalRotations[bone] * qRotFinger;
                }
            }
            startIndex = 9; //little finger
            endIndex = 12;
            angle = fingerAngles[4];
            for (int i = startIndex; i < endIndex; i++)
            {
                HumanBodyBones bone = alBones[i];
                Transform boneTransform = animatorComponent.GetBoneTransform(bone);

                // set the fist rotation
                if (boneTransform && fingerBoneLocalAxes[bone] != Vector3.zero)
                {
                    Quaternion qRotFinger = Quaternion.AngleAxis(angle, fingerBoneLocalAxes[bone]);
                    boneTransform.localRotation = fingerBoneLocalRotations[bone] * qRotFinger;
                }
            }
        }
    }

    // fist open/close with angle
    protected void TransformSpecialBoneFist(int boneIndex, float angle)
    {
        // get the animator component
        //Animator animatorComponent = GetComponent<Animator>();
        if (!animatorComponent)
            return;

        // get the list of bones
        List<HumanBodyBones> alBones = specialIndex2MultiBoneMap[boneIndex];

        for (int i = 0; i < alBones.Count; i++)
        {
            if (i < 1 && (boneIndex == 29 || boneIndex == 30))  // skip the first two thumb bones
                continue;

            HumanBodyBones bone = alBones[i];
            Transform boneTransform = animatorComponent.GetBoneTransform(bone);

            // set the fist rotation
            if (boneTransform && fingerBoneLocalAxes[bone] != Vector3.zero)
            {
                Quaternion qRotFinger = Quaternion.AngleAxis(angle, fingerBoneLocalAxes[bone]);
                boneTransform.localRotation = fingerBoneLocalRotations[bone] * qRotFinger;
            }
        }

    }

    // Apply the initial rotations fingers
    protected void TransformSpecialBoneUnfist(int boneIndex)
    {
        // get the animator component
        //Animator animatorComponent = GetComponent<Animator>();
        if (!animatorComponent)
            return;

        // get the list of bones
        List<HumanBodyBones> alBones = specialIndex2MultiBoneMap[boneIndex];

        for (int i = 0; i < alBones.Count; i++)
        {
            HumanBodyBones bone = alBones[i];
            Transform boneTransform = animatorComponent.GetBoneTransform(bone);

            // set the initial rotation
            if (boneTransform)
            {
                boneTransform.localRotation = fingerBoneLocalRotations[bone];
            }
        }
    }

    protected Quaternion Sensor2AvatarRot2(int sensorID, int boneIndex)
    {
        Quaternion eulerRotation = Quaternion.Euler(0, 0, 0);
        //Quaternion sensorRotation = Quaternion.Inverse(eulerRotation) * quatSensor[sensorID] * Quaternion.Inverse(Quaternion.AngleAxis(sensorBodyRotation, Vector3.up) * quatSensor_offset[sensorID]);
        Quaternion sensorRotation = Quaternion.Inverse(eulerRotation) * sensorManager.getSensorRotation(sensorID);
        Quaternion jointRotation = sensorRotation * eulerRotation;
        Quaternion newRotation = jointRotation * initialRotations[boneIndex];
        newRotation = initialRotation * newRotation;
        return newRotation;
    }


    // 
    public bool lockHands = false;
    public bool lockArms = false;
    public bool lockTorso = false;
    public bool lockLegs = false;

    protected void RotateBoneJoints(Int64 userId)
    {
        bool b_left_arm_enable = true;
        bool b_right_arm_enable = true;
        bool b_left_leg_enable = true;
        bool b_right_leg_enable = true;
        bool b_back_head_enable = true;
        float fLerpRatio = 0.9f;
        if (lockArms)
        {
            b_left_arm_enable = false;
            b_right_arm_enable = false;
        }
        if (lockLegs)
        {
            b_left_leg_enable = false;
            b_right_leg_enable = false;
        }
        if (lockTorso)
        {
            b_back_head_enable = false;
        }
        if (b_left_arm_enable)
        {
            if (true)
            {
                int sensorID = 0;
                int jointIndex = GetBoneIndexByJoint(JointList.JointType.ShoulderLeft, false);
                //bones[jointIndex].transform.rotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion newRotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion oldRotation = bones[jointIndex].transform.rotation;
                bones[jointIndex].transform.rotation = Quaternion.Lerp(oldRotation, newRotation, fLerpRatio);
                print("Shoulder Sensor Angle: " + newRotation + "Euler: " + newRotation.eulerAngles + "difference: " + Quaternion.Lerp(oldRotation, newRotation, fLerpRatio));
                //print("Shoulder Sensor Angle: " + bones[jointIndex].transform.rotation + "Euler: " + bones[jointIndex].transform.rotation.eulerAngles);

            }
            if (true)
            {
                int sensorID = 1;
                int jointIndex = GetBoneIndexByJoint(JointList.JointType.ElbowLeft, false);
                //bones[jointIndex].transform.rotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion newRotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion oldRotation = bones[jointIndex].transform.rotation;
                bones[jointIndex].transform.rotation = Quaternion.Lerp(oldRotation, newRotation, fLerpRatio);
            }
            if (!lockHands)
            {
                int sensorID = 2;
                int jointIndex = GetBoneIndexByJoint(JointList.JointType.WristLeft, false);
                //bones[jointIndex].transform.rotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion newRotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion oldRotation = bones[jointIndex].transform.rotation;
                bones[jointIndex].transform.rotation = Quaternion.Lerp(oldRotation, newRotation, fLerpRatio);
            }
        }
        if (b_right_arm_enable)
        {
            if (true)
            {
                int sensorID = 3;
                int jointIndex = GetBoneIndexByJoint(JointList.JointType.ShoulderRight, false);
                //bones[jointIndex].transform.rotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion newRotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion oldRotation = bones[jointIndex].transform.rotation;
                bones[jointIndex].transform.rotation = Quaternion.Lerp(oldRotation, newRotation, fLerpRatio);
            }
            if (true)
            {
                int sensorID = 4;
                int jointIndex = GetBoneIndexByJoint(JointList.JointType.ElbowRight, false);
                //bones[jointIndex].transform.rotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion newRotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion oldRotation = bones[jointIndex].transform.rotation;
                bones[jointIndex].transform.rotation = Quaternion.Lerp(oldRotation, newRotation, fLerpRatio);
            }
            if (!lockHands)
            {
                int sensorID = 5;
                int jointIndex = GetBoneIndexByJoint(JointList.JointType.WristRight, false);
                //bones[jointIndex].transform.rotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion newRotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion oldRotation = bones[jointIndex].transform.rotation;
                bones[jointIndex].transform.rotation = Quaternion.Lerp(oldRotation, newRotation, fLerpRatio);
            }
        }

        if (b_left_leg_enable)
        {
            if (true)
            {
                int sensorID = 6;
                int jointIndex = GetBoneIndexByJoint(JointList.JointType.HipLeft, false);
                //bones[jointIndex].transform.rotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion newRotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion oldRotation = bones[jointIndex].transform.rotation;
                bones[jointIndex].transform.rotation = Quaternion.Lerp(oldRotation, newRotation, fLerpRatio);
            }
            if (true)
            {
                int sensorID = 7;
                int jointIndex = GetBoneIndexByJoint(JointList.JointType.KneeLeft, false);
                //bones[jointIndex].transform.rotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion newRotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion oldRotation = bones[jointIndex].transform.rotation;
                bones[jointIndex].transform.rotation = Quaternion.Lerp(oldRotation, newRotation, fLerpRatio);
            }
            if (true)
            {
                int sensorID = 8;
                int jointIndex = GetBoneIndexByJoint(JointList.JointType.AnkleLeft, false);
                //bones[jointIndex].transform.rotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion newRotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion oldRotation = bones[jointIndex].transform.rotation;
                bones[jointIndex].transform.rotation = Quaternion.Lerp(oldRotation, newRotation, fLerpRatio);
            }
        }

        if (b_right_leg_enable)
        {
            if (true)
            {
                int sensorID = 9;
                int jointIndex = GetBoneIndexByJoint(JointList.JointType.HipRight, false);
                //bones[jointIndex].transform.rotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion newRotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion oldRotation = bones[jointIndex].transform.rotation;
                bones[jointIndex].transform.rotation = Quaternion.Lerp(oldRotation, newRotation, fLerpRatio);
            }
            if (true)
            {
                int sensorID = 10;
                int jointIndex = GetBoneIndexByJoint(JointList.JointType.KneeRight, false);
                //bones[jointIndex].transform.rotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion newRotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion oldRotation = bones[jointIndex].transform.rotation;
                bones[jointIndex].transform.rotation = Quaternion.Lerp(oldRotation, newRotation, fLerpRatio);
            }
            if (true)
            {
                int sensorID = 11;
                int jointIndex = GetBoneIndexByJoint(JointList.JointType.AnkleRight, false);
                //bones[jointIndex].transform.rotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion newRotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion oldRotation = bones[jointIndex].transform.rotation;
                bones[jointIndex].transform.rotation = Quaternion.Lerp(oldRotation, newRotation, fLerpRatio);
            }
        }

        if (b_back_head_enable)
        {
            if (true)
            {
                int sensorID = 12;
                int jointIndex = GetBoneIndexByJoint(JointList.JointType.SpineBase, false);
                //bones[jointIndex].transform.rotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion newRotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion oldRotation = bones[jointIndex].transform.rotation;
                bones[jointIndex].transform.rotation = Quaternion.Lerp(oldRotation, newRotation, fLerpRatio);
            }

            if (true)
            {
                int sensorID = 13;
                int jointIndex = GetBoneIndexByJoint(JointList.JointType.SpineShoulder, false);
                //bones[jointIndex].transform.rotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion newRotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion oldRotation = bones[jointIndex].transform.rotation;
                bones[jointIndex].transform.rotation = Quaternion.Lerp(oldRotation, newRotation, fLerpRatio);
            }

            if (true)
            {
                int sensorID = 14;
                int jointIndex = GetBoneIndexByJoint(JointList.JointType.Neck, false);
                //bones[jointIndex].transform.rotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion newRotation = Sensor2AvatarRot2(sensorID, jointIndex);
                Quaternion oldRotation = bones[jointIndex].transform.rotation;
                bones[jointIndex].transform.rotation = Quaternion.Lerp(oldRotation, newRotation, fLerpRatio);
            }

            ////////////////walk
            ////transform.rotation = Quaternion.Lerp(transform.rotation, jointRotation, 0.5f);

            //int jointIndexLeftFoot = GetBoneIndexByJoint(JointList.JointType.FootLeft, false);
            //int jointIndexLeftRight = GetBoneIndexByJoint(JointList.JointType.FootRight, false);
            //Vector3 pos_l_foot = bones[jointIndexLeftFoot].transform.position;
            //Vector3 pos_r_foot = bones[jointIndexLeftRight].transform.position;
            //float groundHeight = 0.05f;
            //Vector3 newPosition = transform.position;
            //if (pos_l_foot.y < groundHeight)
            //    bisleftFootOnGround = true;
            //else
            //    bisleftFootOnGround = false;
            //if (pos_r_foot.y < groundHeight)
            //    bisRightFootOnGround = true;
            //else
            //    bisRightFootOnGround = false;

            //if (bisleftFootOnGround && bisRightFootOnGround)
            //{
            //    newPosition = -(pos_l_foot + pos_r_foot) / 2.0f;
            //}
            //else if (bisleftFootOnGround)
            //{
            //    newPosition = -pos_l_foot;
            //}
            //else if (bisRightFootOnGround)
            //{
            //    newPosition = -pos_r_foot;
            //}
            //transform.position = Vector3.Lerp(transform.position, newPosition, 0.5f);
            //leftFootPos = pos_l_foot;
            //rightFootPos = pos_r_foot;
            //////////////////


            if (false)
            {
                int jointIndex = GetBoneIndexByJoint(JointList.JointType.SpineMid, false);
                Quaternion rotationSpineMid = bones[jointIndex].transform.rotation;

                jointIndex = GetBoneIndexByJoint(JointList.JointType.SpineShoulder, false);
                Quaternion rotationSpineShoulder = bones[jointIndex].transform.rotation;

                jointIndex = GetBoneIndexByJoint(JointList.JointType.HipLeft, false);
                Quaternion rotationHipLeft = bones[jointIndex].transform.rotation;

                jointIndex = GetBoneIndexByJoint(JointList.JointType.HipRight, false);
                Quaternion rotationHipRight = bones[jointIndex].transform.rotation;

                jointIndex = GetBoneIndexByJoint(JointList.JointType.SpineBase, false);
                Quaternion jointRotation;
                jointRotation = rotationSpineMid;
                //jointRotation = Quaternion.Lerp(rotationSpineMid, rotationHipLeft, 0.2f);
                //bones[jointIndex].transform.rotation = jointRotation;
                //transform.rotation = jointRotation;
                transform.position = initialPosition;


                Quaternion q_average = Quaternion.identity;
                List<Quaternion> qList = new List<Quaternion>();
                qList.Add(rotationSpineMid);
                qList.Add(rotationSpineShoulder);
                //qList.Add(Quaternion.identity); 
                float averageWeight = 1f / qList.Count;

                for (int i = 0; i < qList.Count; i++)
                {
                    Quaternion q = qList[i];

                    // based on [URL='https://forum.unity.com/members/lordofduct.66428/']lordofduct[/URL] response
                    q_average *= Quaternion.Slerp(Quaternion.identity, q, averageWeight);
                    //bones[jointIndex].transform.rotation = jointRotation;
                    //transform.rotation = jointRotation;
                }

                // output rotation - attach to some object, or whatever
                //this.transform.rotation = q_average;
            }
        }
    }
    public Vector3 leftFootPos, rightFootPos;
    public bool bisleftFootOnGround = true, bisRightFootOnGround = true;
    // applies the muscle limits for humanoid avatar
    private void CheckMuscleLimits()
    {
        if (humanPoseHandler == null)
            return;

        humanPoseHandler.GetHumanPose(ref humanPose);

        //Debug.Log(playerId + " - Trans: " + transform.position + ", body: " + humanPose.bodyPosition);

        bool isPoseChanged = false;

        float muscleMin = -1f;
        float muscleMax = 1f;

        for (int i = 0; i < humanPose.muscles.Length; i++)
        {
            if (float.IsNaN(humanPose.muscles[i]))
            {
                //humanPose.muscles[i] = 0f;
                continue;
            }

            if (humanPose.muscles[i] < muscleMin)
            {
                humanPose.muscles[i] = muscleMin;
                isPoseChanged = true;
            }
            else if (humanPose.muscles[i] > muscleMax)
            {
                humanPose.muscles[i] = muscleMax;
                isPoseChanged = true;
            }
        }

        if (isPoseChanged)
        {
            Quaternion localBodyRot = Quaternion.Inverse(transform.rotation) * humanPose.bodyRotation;

            // recover the body position & orientation
            //humanPose.bodyPosition = Vector3.zero;
            //humanPose.bodyPosition.y = initialHipsPosition.y;
            humanPose.bodyPosition = initialHipsPosition;
            humanPose.bodyRotation = localBodyRot; // Quaternion.identity;

            humanPoseHandler.SetHumanPose(ref humanPose);
            //Debug.Log("  Human pose updated.");
        }

    }
    // returns distance from the given transform to the underlying object. The player must be in IgnoreRaycast layer.
    protected virtual float GetTransformDistanceToGround(Transform trans)
    {
        if (!trans)
            return 0f;

        //		RaycastHit hit;
        //		if(Physics.Raycast(trans.position, Vector3.down, out hit, 2f, raycastLayers))
        //		{
        //			return -hit.distance;
        //		}
        //		else if(Physics.Raycast(trans.position, Vector3.up, out hit, 2f, raycastLayers))
        //		{
        //			return hit.distance;
        //		}
        //		else
        //		{
        //			if (trans.position.y < 0)
        //				return -trans.position.y;
        //			else
        //				return 1000f;
        //		}

        return -trans.position.y;
    }

    // Capture the initial rotations of the bones
    protected void GetInitialRotations()
    {
        // save the initial rotation
        if (offsetNode != null)
        {
            offsetNodePos = offsetNode.transform.position;
            offsetNodeRot = offsetNode.transform.rotation;
        }

        initialPosition = transform.position;
        initialRotation = transform.rotation;

        initialHipsPosition = bones[0] ? bones[0].localPosition : Vector3.zero;
        initialHipsRotation = bones[0] ? bones[0].localRotation : Quaternion.identity;

        //		if(offsetNode != null)
        //		{
        //			initialRotation = Quaternion.Inverse(offsetNodeRot) * initialRotation;
        //		}

        transform.rotation = Quaternion.identity;

        // save the body root initial position
        if (bodyRoot != null)
        {
            bodyRootPosition = bodyRoot.position;
        }
        else
        {
            bodyRootPosition = transform.position;
        }

        if (offsetNode != null)
        {
            bodyRootPosition = bodyRootPosition - offsetNodePos;
        }

        // save the initial bone rotations
        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] != null)
            {
                initialRotations[i] = bones[i].rotation;
                localRotations[i] = bones[i].localRotation;
            }
        }

        // get finger bones' local rotations
        //Animator animatorComponent = GetComponent<Animator>();
        foreach (int boneIndex in specialIndex2MultiBoneMap.Keys)
        {
            List<HumanBodyBones> alBones = specialIndex2MultiBoneMap[boneIndex];
            //Transform handTransform = animatorComponent.GetBoneTransform((boneIndex == 27 || boneIndex == 29) ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);

            for (int b = 0; b < alBones.Count; b++)
            {
                HumanBodyBones bone = alBones[b];
                Transform boneTransform = animatorComponent ? animatorComponent.GetBoneTransform(bone) : null;

                // get the finger's 1st transform
                Transform fingerBaseTransform = animatorComponent ? animatorComponent.GetBoneTransform(alBones[b - (b % 3)]) : null;
                //Vector3 vBoneDirParent = handTransform && fingerBaseTransform ? (handTransform.position - fingerBaseTransform.position).normalized : Vector3.zero;

                // get the finger's 2nd transform
                Transform baseChildTransform = fingerBaseTransform && fingerBaseTransform.childCount > 0 ? fingerBaseTransform.GetChild(0) : null;
                Vector3 vBoneDirChild = baseChildTransform && fingerBaseTransform ? (baseChildTransform.position - fingerBaseTransform.position).normalized : Vector3.zero;
                Vector3 vOrthoDirChild = Vector3.Cross(vBoneDirChild, Vector3.up).normalized;

                if (boneTransform)
                {
                    fingerBoneLocalRotations[bone] = boneTransform.localRotation;

                    if (vBoneDirChild != Vector3.zero)
                    {
                        fingerBoneLocalAxes[bone] = boneTransform.InverseTransformDirection(vOrthoDirChild).normalized;
                    }
                    else
                    {
                        fingerBoneLocalAxes[bone] = Vector3.zero;
                    }

                    //					Transform bparTransform = boneTransform ? boneTransform.parent : null;
                    //					Transform bchildTransform = boneTransform && boneTransform.childCount > 0 ? boneTransform.GetChild(0) : null;
                    //
                    //					// get the finger base transform (1st joint)
                    //					Transform fingerBaseTransform = animatorComponent.GetBoneTransform(alBones[b - (b % 3)]);
                    //					Vector3 vBoneDir2 = (handTransform.position - fingerBaseTransform.position).normalized;
                    //
                    //					// set the fist rotation
                    //					if(boneTransform && fingerBaseTransform && handTransform)
                    //					{
                    //						Vector3 vBoneDir = bchildTransform ? (bchildTransform.position - boneTransform.position).normalized :
                    //							(bparTransform ? (boneTransform.position - bparTransform.position).normalized : Vector3.zero);
                    //
                    //						Vector3 vOrthoDir = Vector3.Cross(vBoneDir2, vBoneDir).normalized;
                    //						fingerBoneLocalAxes[bone] = boneTransform.InverseTransformDirection(vOrthoDir);
                    //					}
                }
            }
        }

        // Restore the initial rotation
        transform.rotation = initialRotation;
    }









    /// <summary>
    /// Gets the bone index by joint type.
    /// </summary>
    /// <returns>The bone index.</returns>
    /// <param name="joint">Joint type</param>
    /// <param name="bMirrored">If set to <c>true</c> gets the mirrored joint index.</param>
    public int GetBoneIndexByJoint(JointList.JointType joint, bool bMirrored)
    {
        int boneIndex = -1;

        if (jointMap2boneIndex.ContainsKey(joint))
        {
            boneIndex = !bMirrored ? jointMap2boneIndex[joint] : mirrorJointMap2boneIndex[joint];
        }

        return boneIndex;
    }

    /// <summary>
    /// Gets the bone transform by index.
    /// </summary>
    /// <returns>The bone transform.</returns>
    /// <param name="index">Index</param>
    public Transform GetBoneTransform(int index)
    {
        if (index >= 0 && index < bones.Length)
        {
            return bones[index];
        }

        return null;
    }

    // Parse the L Pose input field for two floats in num1,num2 format, return 20,20 as default
    protected float[] getLPoseAngles()
    {
        Debug.Log("L Pose");
        string inputString = LPoseInputField.text;
        float[] result = new float[2];
        string[] parts = inputString.Split(',');
        if (parts.Length == 2 && float.TryParse(parts[0], out float x) && float.TryParse(parts[1], out float y))
        {
            result[0] = x;
            result[1] = y;
        }
        else
        {
            // Parsing failed, return default values
            result[0] = 20.0f;
            result[1] = 20.0f;
        }
        Debug.Log(result);
        return result;
    }


    // Set model's arms to chosen pose (Arms as if sitting in chair)
    public void SetModelArmsInPose(int poseChar)
    {
        // 0 T
        // 1 N
        // 2 H
        // 3 L
        // 4 NP
        // Default is T
        Vector3 vLeftUGoalDir = Vector3.left; // Upper arm goal
        Vector3 vLeftLGoalDir = Vector3.left; // Lower arm goal
        Vector3 vRightUGoalDir = Vector3.right; // Upper arm goal
        Vector3 vRightLGoalDir = Vector3.right; // Lower arm goal
        switch (poseChar)
        {
            case 0: //T Pose
                vLeftUGoalDir = Vector3.left; // Upper arm goal
                vLeftLGoalDir = Vector3.left; // Lower arm goal
                vRightUGoalDir = Vector3.right; // Upper arm goal
                vRightLGoalDir = Vector3.right; // Lower arm goal
                break;
            case 1: //N Pose
                vLeftUGoalDir = Vector3.down; // Upper arm goal
                vLeftLGoalDir = Vector3.down; // Lower arm goal
                vRightUGoalDir = Vector3.down; // Upper arm goal
                vRightLGoalDir = Vector3.down; // Lower arm goal
                break;
            case 2: //H Pose
                vLeftUGoalDir = Vector3.down; // Upper arm goal
                vLeftLGoalDir = Vector3.forward; // Lower arm goal
                vRightUGoalDir = Vector3.down; // Upper arm goal
                vRightLGoalDir = Vector3.forward; // Lower arm goal
                break;
            case 3: //L Pose Left
                float[] LAngles;
                LAngles = getLPoseAngles();
                vLeftUGoalDir = Quaternion.Euler(-LAngles[0], 0, 0) * Vector3.down; // Upper arm goal
                vLeftLGoalDir = Quaternion.Euler(0, 90-LAngles[1], 0) * Vector3.forward; // Lower arm goal
                vRightUGoalDir = Vector3.down; // Upper arm goal
                vRightLGoalDir = Vector3.forward; // Lower arm goal
                break;
            case 4: //N Pose
                vLeftUGoalDir = Vector3.up; // Upper arm goal
                vLeftLGoalDir = Vector3.up; // Lower arm goal
                vRightUGoalDir = Vector3.up; // Upper arm goal
                vRightLGoalDir = Vector3.up; // Lower arm goal
                break;


        }

        vLeftUGoalDir = transform.TransformDirection(vLeftUGoalDir); // Upper arm goal
        vLeftLGoalDir = transform.TransformDirection(vLeftLGoalDir); // Lower arm goal
        vRightUGoalDir = transform.TransformDirection(vRightUGoalDir); // Upper arm goal
        vRightLGoalDir = transform.TransformDirection(vRightLGoalDir);
        // LEFT SIDE
        Transform transLeftUarm = GetBoneTransform(GetBoneIndexByJoint(JointList.JointType.ShoulderLeft, false)); // animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        Transform transLeftLarm = GetBoneTransform(GetBoneIndexByJoint(JointList.JointType.ElbowLeft, false)); // animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        Transform transLeftHand = GetBoneTransform(GetBoneIndexByJoint(JointList.JointType.WristLeft, false)); // animator.GetBoneTransform(HumanBodyBones.LeftHand);

        if (transLeftUarm != null && transLeftLarm != null)
        {
            Vector3 vUarmLeftDir = transLeftLarm.position - transLeftUarm.position;
            float fUarmLeftAngle = Vector3.Angle(vUarmLeftDir, vLeftUGoalDir);

            if (Mathf.Abs(fUarmLeftAngle) >= 5f)
            {
                Quaternion vFixRotation = Quaternion.FromToRotation(vUarmLeftDir, vLeftUGoalDir);
                transLeftUarm.rotation = vFixRotation * transLeftUarm.rotation;
            }

            if (transLeftHand != null)
            {
                Vector3 vLarmLeftDir = transLeftHand.position - transLeftLarm.position;
                float fLarmLeftAngle = Vector3.Angle(vLarmLeftDir, vLeftLGoalDir);

                if (Mathf.Abs(fLarmLeftAngle) >= 5f)
                {
                    Quaternion vFixRotation = Quaternion.FromToRotation(vLarmLeftDir, vLeftLGoalDir);
                    transLeftLarm.rotation = vFixRotation * transLeftLarm.rotation;
                }
            }
        }


        // RIGHT SIDE
        Transform transRightUarm = GetBoneTransform(GetBoneIndexByJoint(JointList.JointType.ShoulderRight, false)); // animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        Transform transRightLarm = GetBoneTransform(GetBoneIndexByJoint(JointList.JointType.ElbowRight, false)); // animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        Transform transRightHand = GetBoneTransform(GetBoneIndexByJoint(JointList.JointType.WristRight, false)); // animator.GetBoneTransform(HumanBodyBones.RightHand);

        if (transRightUarm != null && transRightLarm != null)
        {
            Vector3 vUarmRightDir = transRightLarm.position - transRightUarm.position; // why is it a difference in position as opposed to the current rotation --> used to find the angle difference. This must mean that controlling the angle it needs to get to will control the final pose. There is only one goal angle since both lower and upper arms point in that direction.
            float fUarmRightAngle = Vector3.Angle(vUarmRightDir, vRightUGoalDir);

            if (Mathf.Abs(fUarmRightAngle) >= 5f)
            {
                Quaternion vFixRotation = Quaternion.FromToRotation(vUarmRightDir, vRightUGoalDir);
                transRightUarm.rotation = vFixRotation * transRightUarm.rotation;
            }

            if (transRightHand != null)
            {
                Vector3 vLarmRightDir = transRightHand.position - transRightLarm.position;
                float fLarmRightAngle = Vector3.Angle(vLarmRightDir, vRightLGoalDir);

                if (Mathf.Abs(fLarmRightAngle) >= 5f)
                {
                    Quaternion vFixRotation = Quaternion.FromToRotation(vLarmRightDir, vRightLGoalDir);
                    transRightLarm.rotation = vFixRotation * transRightLarm.rotation;
                }
            }
        }

        // Get initial bone rotations
        GetInitialRotations();
    }

    // If the bones to be mapped have been declared, map that bone to the model.
    protected virtual void MapBones()
    {
        //		// make OffsetNode as a parent of model transform.
        //		offsetNode = new GameObject(name + "Ctrl") { layer = transform.gameObject.layer, tag = transform.gameObject.tag };
        //		offsetNode.transform.position = transform.position;
        //		offsetNode.transform.rotation = transform.rotation;
        //		offsetNode.transform.parent = transform.parent;

        //		// take model transform as body root
        //		transform.parent = offsetNode.transform;
        //		transform.localPosition = Vector3.zero;
        //		transform.localRotation = Quaternion.identity;

        //bodyRoot = transform;

        // get bone transforms from the animator component
        //Animator animatorComponent = GetComponent<Animator>();

        for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
        {
            if (!boneIndex2MecanimMap.ContainsKey(boneIndex))
                continue;

            bones[boneIndex] = animatorComponent ? animatorComponent.GetBoneTransform(boneIndex2MecanimMap[boneIndex]) : null;
        }
    }


    // dictionaries to speed up bones' processing
    // the author of the terrific idea for kinect-joints to mecanim-bones mapping
    // along with its initial implementation, including following dictionary is
    // Mikhail Korchun (korchoon@gmail.com). Big thanks to this guy!
    protected static readonly Dictionary<int, HumanBodyBones> boneIndex2MecanimMap = new Dictionary<int, HumanBodyBones>
    {
        {0, HumanBodyBones.Hips},
        {1, HumanBodyBones.Spine},
        {2, HumanBodyBones.Chest},
        {3, HumanBodyBones.Neck},
//		{4, HumanBodyBones.Head},
		
		{5, HumanBodyBones.LeftUpperArm},
        {6, HumanBodyBones.LeftLowerArm},
        {7, HumanBodyBones.LeftHand},
//		{8, HumanBodyBones.LeftIndexProximal},
//		{9, HumanBodyBones.LeftIndexIntermediate},
//		{10, HumanBodyBones.LeftThumbProximal},
		
		{11, HumanBodyBones.RightUpperArm},
        {12, HumanBodyBones.RightLowerArm},
        {13, HumanBodyBones.RightHand},
//		{14, HumanBodyBones.RightIndexProximal},
//		{15, HumanBodyBones.RightIndexIntermediate},
//		{16, HumanBodyBones.RightThumbProximal},
		
		{17, HumanBodyBones.LeftUpperLeg},
        {18, HumanBodyBones.LeftLowerLeg},
        {19, HumanBodyBones.LeftFoot},
        {20, HumanBodyBones.LeftToes},

        {21, HumanBodyBones.RightUpperLeg},
        {22, HumanBodyBones.RightLowerLeg},
        {23, HumanBodyBones.RightFoot},
        {24, HumanBodyBones.RightToes},

        {25, HumanBodyBones.LeftShoulder},
        {26, HumanBodyBones.RightShoulder},
        {27, HumanBodyBones.LeftIndexProximal},
        {28, HumanBodyBones.RightIndexProximal},
        {29, HumanBodyBones.LeftThumbProximal},
        {30, HumanBodyBones.RightThumbProximal},
    };

    protected static readonly Dictionary<int, JointList.JointType> boneIndex2JointMap = new Dictionary<int, JointList.JointType>
    {
        {0, JointList.JointType.SpineBase},
        {1, JointList.JointType.SpineMid},
        {2, JointList.JointType.SpineShoulder},
        {3, JointList.JointType.Neck},
        {4, JointList.JointType.Head},

        {5, JointList.JointType.ShoulderLeft},
        {6, JointList.JointType.ElbowLeft},
        {7, JointList.JointType.WristLeft},
        {8, JointList.JointType.HandLeft},

        {9, JointList.JointType.HandTipLeft},
        {10, JointList.JointType.ThumbLeft},

        {11, JointList.JointType.ShoulderRight},
        {12, JointList.JointType.ElbowRight},
        {13, JointList.JointType.WristRight},
        {14, JointList.JointType.HandRight},

        {15, JointList.JointType.HandTipRight},
        {16, JointList.JointType.ThumbRight},

        {17, JointList.JointType.HipLeft},
        {18, JointList.JointType.KneeLeft},
        {19, JointList.JointType.AnkleLeft},
        {20, JointList.JointType.FootLeft},

        {21, JointList.JointType.HipRight},
        {22, JointList.JointType.KneeRight},
        {23, JointList.JointType.AnkleRight},
        {24, JointList.JointType.FootRight},
    };

    protected static readonly Dictionary<int, List<JointList.JointType>> specIndex2JointMap = new Dictionary<int, List<JointList.JointType>>
    {
        {25, new List<JointList.JointType> {JointList.JointType.ShoulderLeft, JointList.JointType.SpineShoulder} },
        {26, new List<JointList.JointType> {JointList.JointType.ShoulderRight, JointList.JointType.SpineShoulder} },
        {27, new List<JointList.JointType> {JointList.JointType.HandTipLeft, JointList.JointType.HandLeft} },
        {28, new List<JointList.JointType> {JointList.JointType.HandTipRight, JointList.JointType.HandRight} },
        {29, new List<JointList.JointType> {JointList.JointType.ThumbLeft, JointList.JointType.HandLeft} },
        {30, new List<JointList.JointType> {JointList.JointType.ThumbRight, JointList.JointType.HandRight} },
    };

    protected static readonly Dictionary<int, JointList.JointType> boneIndex2MirrorJointMap = new Dictionary<int, JointList.JointType>
    {
        {0, JointList.JointType.SpineBase},
        {1, JointList.JointType.SpineMid},
        {2, JointList.JointType.SpineShoulder},
        {3, JointList.JointType.Neck},
        {4, JointList.JointType.Head},

        {5, JointList.JointType.ShoulderRight},
        {6, JointList.JointType.ElbowRight},
        {7, JointList.JointType.WristRight},
        {8, JointList.JointType.HandRight},

        {9, JointList.JointType.HandTipRight},
        {10, JointList.JointType.ThumbRight},

        {11, JointList.JointType.ShoulderLeft},
        {12, JointList.JointType.ElbowLeft},
        {13, JointList.JointType.WristLeft},
        {14, JointList.JointType.HandLeft},

        {15, JointList.JointType.HandTipLeft},
        {16, JointList.JointType.ThumbLeft},

        {17, JointList.JointType.HipRight},
        {18, JointList.JointType.KneeRight},
        {19, JointList.JointType.AnkleRight},
        {20, JointList.JointType.FootRight},

        {21, JointList.JointType.HipLeft},
        {22, JointList.JointType.KneeLeft},
        {23, JointList.JointType.AnkleLeft},
        {24, JointList.JointType.FootLeft},
    };

    protected static readonly Dictionary<int, List<JointList.JointType>> specIndex2MirrorMap = new Dictionary<int, List<JointList.JointType>>
    {
        {25, new List<JointList.JointType> {JointList.JointType.ShoulderRight, JointList.JointType.SpineShoulder} },
        {26, new List<JointList.JointType> {JointList.JointType.ShoulderLeft, JointList.JointType.SpineShoulder} },
        {27, new List<JointList.JointType> {JointList.JointType.HandTipRight, JointList.JointType.HandRight} },
        {28, new List<JointList.JointType> {JointList.JointType.HandTipLeft, JointList.JointType.HandLeft} },
        {29, new List<JointList.JointType> {JointList.JointType.ThumbRight, JointList.JointType.HandRight} },
        {30, new List<JointList.JointType> {JointList.JointType.ThumbLeft, JointList.JointType.HandLeft} },
    };

    protected static readonly Dictionary<JointList.JointType, int> jointMap2boneIndex = new Dictionary<JointList.JointType, int>
    {
        {JointList.JointType.SpineBase, 0},
        {JointList.JointType.SpineMid, 1},
        {JointList.JointType.SpineShoulder, 2},
        {JointList.JointType.Neck, 3},
        {JointList.JointType.Head, 4},

        {JointList.JointType.ShoulderLeft, 5},
        {JointList.JointType.ElbowLeft, 6},
        {JointList.JointType.WristLeft, 7},
        {JointList.JointType.HandLeft, 8},

        {JointList.JointType.HandTipLeft, 9},
        {JointList.JointType.ThumbLeft, 10},

        {JointList.JointType.ShoulderRight, 11},
        {JointList.JointType.ElbowRight, 12},
        {JointList.JointType.WristRight, 13},
        {JointList.JointType.HandRight, 14},

        {JointList.JointType.HandTipRight, 15},
        {JointList.JointType.ThumbRight, 16},

        {JointList.JointType.HipLeft, 17},
        {JointList.JointType.KneeLeft, 18},
        {JointList.JointType.AnkleLeft, 19},
        {JointList.JointType.FootLeft, 20},

        {JointList.JointType.HipRight, 21},
        {JointList.JointType.KneeRight, 22},
        {JointList.JointType.AnkleRight, 23},
        {JointList.JointType.FootRight, 24},
    };

    protected static readonly Dictionary<JointList.JointType, int> mirrorJointMap2boneIndex = new Dictionary<JointList.JointType, int>
    {
        {JointList.JointType.SpineBase, 0},
        {JointList.JointType.SpineMid, 1},
        {JointList.JointType.SpineShoulder, 2},
        {JointList.JointType.Neck, 3},
        {JointList.JointType.Head, 4},

        {JointList.JointType.ShoulderRight, 5},
        {JointList.JointType.ElbowRight, 6},
        {JointList.JointType.WristRight, 7},
        {JointList.JointType.HandRight, 8},

        {JointList.JointType.HandTipRight, 9},
        {JointList.JointType.ThumbRight, 10},

        {JointList.JointType.ShoulderLeft, 11},
        {JointList.JointType.ElbowLeft, 12},
        {JointList.JointType.WristLeft, 13},
        {JointList.JointType.HandLeft, 14},

        {JointList.JointType.HandTipLeft, 15},
        {JointList.JointType.ThumbLeft, 16},

        {JointList.JointType.HipRight, 17},
        {JointList.JointType.KneeRight, 18},
        {JointList.JointType.AnkleRight, 19},
        {JointList.JointType.FootRight, 20},

        {JointList.JointType.HipLeft, 21},
        {JointList.JointType.KneeLeft, 22},
        {JointList.JointType.AnkleLeft, 23},
        {JointList.JointType.FootLeft, 24},
    };


    protected static readonly Dictionary<int, List<HumanBodyBones>> specialIndex2MultiBoneMap = new Dictionary<int, List<HumanBodyBones>>
    {
        {27, new List<HumanBodyBones> {  // left fingers
				HumanBodyBones.LeftIndexProximal,
                HumanBodyBones.LeftIndexIntermediate,
                HumanBodyBones.LeftIndexDistal,
                HumanBodyBones.LeftMiddleProximal,
                HumanBodyBones.LeftMiddleIntermediate,
                HumanBodyBones.LeftMiddleDistal,
                HumanBodyBones.LeftRingProximal,
                HumanBodyBones.LeftRingIntermediate,
                HumanBodyBones.LeftRingDistal,
                HumanBodyBones.LeftLittleProximal,
                HumanBodyBones.LeftLittleIntermediate,
                HumanBodyBones.LeftLittleDistal,
            }},
        {28, new List<HumanBodyBones> {  // right fingers
				HumanBodyBones.RightIndexProximal,
                HumanBodyBones.RightIndexIntermediate,
                HumanBodyBones.RightIndexDistal,
                HumanBodyBones.RightMiddleProximal,
                HumanBodyBones.RightMiddleIntermediate,
                HumanBodyBones.RightMiddleDistal,
                HumanBodyBones.RightRingProximal,
                HumanBodyBones.RightRingIntermediate,
                HumanBodyBones.RightRingDistal,
                HumanBodyBones.RightLittleProximal,
                HumanBodyBones.RightLittleIntermediate,
                HumanBodyBones.RightLittleDistal,
            }},
        {29, new List<HumanBodyBones> {  // left thumb
				HumanBodyBones.LeftThumbProximal,
                HumanBodyBones.LeftThumbIntermediate,
                HumanBodyBones.LeftThumbDistal,
            }},
        {30, new List<HumanBodyBones> {  // right thumb
				HumanBodyBones.RightThumbProximal,
                HumanBodyBones.RightThumbIntermediate,
                HumanBodyBones.RightThumbDistal,
            }},
    };

}
public class JointList
{
    public enum JointType : int
    {
        SpineBase = 0,
        SpineMid = 1,
        Neck = 2,
        Head = 3,
        ShoulderLeft = 4,
        ElbowLeft = 5,
        WristLeft = 6,
        HandLeft = 7,
        ShoulderRight = 8,
        ElbowRight = 9,
        WristRight = 10,
        HandRight = 11,
        HipLeft = 12,
        KneeLeft = 13,
        AnkleLeft = 14,
        FootLeft = 15,
        HipRight = 16,
        KneeRight = 17,
        AnkleRight = 18,
        FootRight = 19,
        SpineShoulder = 20,
        HandTipLeft = 21,
        ThumbLeft = 22,
        HandTipRight = 23,
        ThumbRight = 24
        //Count = 25
    }

}
