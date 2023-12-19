using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using System;
using TMPro;
[RequireComponent(typeof(AudioSource))]
public class MotorLearningGameOculusTouch : MonoBehaviour {
    string strGameName = "MotorLearning";
    DeviceConfig deviceConfig;
    public ControllerHardwareType controllerHardwareType;
    private GameObject hand_l, hand_r, body_JNT, LocalAvatar;
    //public AudioClip audioclipCannonRotate;
    public AudioClip audioclipLaunchBall;
    public AudioClip audioclipTouchSphere;
    public AudioClip audioclipGameWin;
    public AudioClip audioclipGameFinished;
    public AudioClip audioclipButtonClick;
    AudioSource audioSource;
    public Camera sceneCamera;
    public bool bGameRunning = true;
    public bool bGamePause = false;

    public Dropdown dropdownPlaneSelect;
    protected static FlowerGame instance = null;
    int gameLevel = 1;
    const int max_gameLevel = 5;
    //float radius_touch = 0.1f;
    enum GamePlayMode { Left, Right, Bilateral };
    GamePlayMode gamePlaymode = GamePlayMode.Right;
    //private GameObject[] sphereObj;
    //private GameObject spheresAllObj;
    int shpereIndex = 0;
    int cannonRotateIndex = 0;//different rotation stages
    int cannonRotatedCnt = 0;//how many times rotated? if> max then end of game
    const int maxRotateIndex = 10;
    //float sphereScale = 1.0f;
    const int max_gameObjNum = 1;
    GameMenuControl game_MenuControl;
    bool b_rotateCannon = false;
    int i_delay_cnt = 0;
    //public ParticleSystem particle;

    //private float fStartTime = 0f;
    private float fCurrentTime = 0f;
    private float fPreviousTime = 0f;
    public float gameRuningTime = 0f;
    //fCurrentTime = Time.time;
    //            string sRelTime = string.Format("{0:F3}", (fCurrentTime - fStartTime));

    int i_gameResult_lauched_count = 0;
    int i_gameResult_touched_count = 0;
    int i_gameResult_total_balls = max_gameObjNum * maxRotateIndex;
    float f_gameResult_touched_percent = 0;
    float f_gameResult_progress = 0; 
    float f_gameResult_trajectory_distance_left = 0;
    float f_gameResult_trajectory_distance_right = 0;
    Vector3 pre_hand_pos_left = Vector3.zero;
    Vector3 pre_hand_pos_right = Vector3.zero;
    Vector3 leftHand_speed = Vector3.zero;
    Vector3 rightHand_speed = Vector3.zero;
    //   AvatarData avatarData = null;
    //text display
    public Text txtGameMode;
    public Text txtGameTime;
    public Text txtBallTouched;
    public Text txtTrajectory;
    public Text txtGameLevel;
    public Text txtGameBranch;
    //progress
    public Text txtProgress;
    public Image ImageProgress;
    //timeout for each play
    float timeBeginCannonRotate = 0;
    const float max_timeCannonRotate = 0;//3.5f;
    float timeoutPlay = 0;
    float max_timeoutPlay = 5;// + max_timeCannonRotate; //s
    public Text txtTimeout;
    public Image ImageTimeout;
    public Text txtGameFinished;
    //
    private Camera Camera_Exo_FP;
    private Camera Camera1;
    private GameObject cannonBallBase;
    public GameObject CannonBallObj;
    public GameObject CannonObj;
    float last_launch_time = 0;
    //left mirror for bilateral
    //private GameObject CannonMirrorObj;
    //private GameObject cannonBallBaseMirror;
    //bilateral touch
    int left_touching, right_touching;
    const int max_touching_duration = 60;

    //explosion
    float durationExplosion = 0;
    public ParticleExamples particleSystemsExplosion;
    private GameObject currentGOExplosion;

    //////////
    public Vector3 ballPositionAdjustment = new Vector3(0f, 0f, 0f);//gui slider
    //Vector3 rightShoulderPos = new Vector3(0.25f, 1.4f, 0f);//right/up/front
    Vector3 avatarShoulderCenterPos = new Vector3(0, 1.40f, 0);//oculus avatar
    //Vector3 avatarBallOffset = new Vector3(0.25f, 0.2f, 0.1f);
    float waitTime_initialize;
    Vector3 avatarShoulder_body_JNT_Offset = new Vector3(0, 0.48f, 0);
    Vector3 avartarBallBaseOffset = new Vector3(0, 0, 1.1f); //oculus head/shoulder center and ballBase position offset (vertial view)
    float appliedBallForce = 30.0f;
    //log
    DataGameResultLog gameResultLog;
    DataOculusTouch DataOculus;
    public bool b_enableMotionDataCollection = true;

    // New MotorLearning Stuff

    private GameObject leftGameOrigin = null;
    private GameObject rightGameOrigin = null;

    public Material lineMaterial;

    private LineRenderer lineRendererLeft;
    private LineRenderer lineRendererRight;

    [Serializable]
    public class TimeVector3
    {
        public float time;
        public Vector3 position;

        public TimeVector3(float time, Vector3 vectorValue)
        {
            this.time = time;
            this.position = vectorValue;
        }
    }
    public List<TimeVector3> leftArmPosList = new List<TimeVector3>();
    public List<TimeVector3> rightArmPosList = new List<TimeVector3>();
    

    public float pointPrecision;
    public float gameRadius;

    public enum PlaneType
    {
        XY,
        YZ,
        ZX
    }
    public List<TrailRenderer> TrailRendererList = new List<TrailRenderer>();

    public PlaneType selectedPlane = PlaneType.ZX;
    public TextMeshPro stageNumberLabelPrefab;

    Vector3[] targetLocations = new Vector3[8];

    public void OnDropdownValueChanged(int index) {
        switch (index) {
            case 0:
                selectedPlane = PlaneType.ZX;
                break;
            case 1:
                selectedPlane = PlaneType.XY;
                break;
            case 2:
                selectedPlane = PlaneType.YZ;
                break;
        }
        resetGame();
        buttonClickSound();
    }

    
    
    void Start()
    {

        TrailRenderer[] trailRenderers = FindObjectsOfType<TrailRenderer>();
        TrailRendererList.AddRange(trailRenderers);
        leftGameOrigin = GameObject.Find("LeftGameOrigin");
        rightGameOrigin = GameObject.Find("RightGameOrigin");

        lineRendererLeft = leftGameOrigin.AddComponent<LineRenderer>();
        lineRendererLeft.material = lineMaterial; // Assign your line material in the Inspector
        lineRendererLeft.positionCount = 0;
        lineRendererLeft.startWidth = 0.01f;
        lineRendererLeft.endWidth = 0.01f;

        lineRendererRight = rightGameOrigin.AddComponent<LineRenderer>();
        lineRendererRight.material = lineMaterial; // Assign your line material in the Inspector
        lineRendererRight.positionCount = 0;
        lineRendererRight.startWidth = 0.01f;
        lineRendererRight.endWidth = 0.01f;

        
        deviceConfig = new DeviceConfig();
        deviceConfig.hardwareType = controllerHardwareType;
        if (deviceConfig.hardwareType == ControllerHardwareType.OculusTouch)
        {
            if (gameObject.name != "LocalAvatar")
            {
                Destroy(gameObject);
            }
        }
        else
        {
            if (gameObject.name != "Avatar2ArmExo")
            {
                Destroy(gameObject);
            }
        }
        if (deviceConfig.hardwareType == ControllerHardwareType.OculusTouch)
        {
            //disable exo obj
            GameObject gameObj1;
            gameObj1 = GameObject.Find("Camera_Exo_FP");
            if (gameObj1 != null)
                Camera_Exo_FP = gameObj1.GetComponent<Camera>();
            if (Camera_Exo_FP != null)
                Camera_Exo_FP.gameObject.SetActive(false);

            gameObj1 = GameObject.Find("Avatar2ArmExo");
            if (gameObj1 != null)
                gameObj1.SetActive(false);
            //
            LocalAvatar = GameObject.Find("LocalAvatar");
            if (LocalAvatar != null)
            {
                //LocalAvatar.gameObject.SetActive(true);
                hand_l = getChildGameObject(LocalAvatar, "hand_left");
                hand_r = getChildGameObject(LocalAvatar, "hand_right");
            }
        }
        else if (deviceConfig.hardwareType == ControllerHardwareType.Exoskeleton || deviceConfig.hardwareType == ControllerHardwareType.Kinect)
        {
            //disable touch obj
            GameObject gameObj1 = GameObject.Find("OculusTouch");
            if (gameObj1 != null)
                gameObj1.gameObject.SetActive(false);
            //
            hand_l = GameObject.Find("hand_l");
            hand_r = GameObject.Find("hand_r");

        }
        //LocalAvatar = GameObject.Find("LocalAvatar");
        //hand_l = getChildGameObject(LocalAvatar, "hand_left");
        //hand_r = getChildGameObject(LocalAvatar, "hand_right");
        //        avatarData = new AvatarData();

        cannonBallBase = GameObject.Find("CannonBallBase");

        CannonObj = GameObject.Find("Cannon");

        //Camera1 = GameObject.Find("Camera1").GetComponent<Camera>();
        GameObject camObj = GameObject.Find("Camera1");
        if(camObj!=null)
            Camera1 = camObj.GetComponent<Camera>();

        //setShperePosition();
        resetGame();
        audioSource = GetComponent<AudioSource>();
        game_MenuControl = GameMenuControl.Instance;
        //       ovrManager = OVRManager.instance;
        //Physics.gravity = new Vector3(0, -1.0F, 0);
        //log
        gameResultLog = new DataGameResultLog();
        gameResultLog.strGameName = strGameName;
        gameResultLog.StartGameResultFile();
        //ui
        initUIButtons();
        //data
        if (deviceConfig.hardwareType == ControllerHardwareType.OculusTouch)
        {
            DataOculus = new DataOculusTouch();
            DataOculus.gameName = strGameName;
        }
        //load level
        // OpenLevelFile(gameLevel);
    }

    // Draws target lines around origin
    void DrawGame(LineRenderer lrr, GameObject originObject)
    {
        Vector3 origin = originObject.transform.position;
        Debug.LogWarning("Entered DrawGame");
        DrawLine(lrr, origin + targetLocations[1], origin + targetLocations[5]);
        DrawLine(lrr, origin + targetLocations[0], origin + targetLocations[4]);
        DrawLine(lrr, origin + targetLocations[2], origin + targetLocations[6]);
        DrawLine(lrr, origin + targetLocations[3], origin + targetLocations[7]);
        for (int i = 0; i < targetLocations.Length; i++) {
           TextMeshPro thisLabel = Instantiate(stageNumberLabelPrefab, originObject.transform);
           thisLabel.transform.position = origin + targetLocations[i];
           thisLabel.text = i.ToString();
        }

    }


    void setupTargetLocations()
    {
        targetLocations[1] = GetAxisVector(selectedPlane, 1.0f, 1.0f) * gameRadius;
        targetLocations[5] = GetAxisVector(selectedPlane, -1.0f, -1.0f) * gameRadius;
        targetLocations[3] = GetAxisVector(selectedPlane, 1.0f, -1.0f) * gameRadius;
        targetLocations[7] = GetAxisVector(selectedPlane, -1.0f, 1.0f) * gameRadius;
        targetLocations[0] = GetAxisVector(selectedPlane, 0.0f, 1.0f) * gameRadius;
        targetLocations[4] = GetAxisVector(selectedPlane, 0.0f, -1.0f) * gameRadius;
        targetLocations[2] = GetAxisVector(selectedPlane, 1.0f, 0.0f) * gameRadius;
        targetLocations[6] = GetAxisVector(selectedPlane, -1.0f, 0.0f) * gameRadius;
    }


    // Draws line from start to end to middle
    void DrawLine(LineRenderer lineRenderer, Vector3 start, Vector3 end)
    {
        lineRenderer.positionCount += 3;
        int index = lineRenderer.positionCount - 3;
        lineRenderer.SetPosition(index, start);
        lineRenderer.SetPosition(index + 1, end);
        lineRenderer.SetPosition(index + 2, (end+start)/2);
    }

    Vector3 GetAxisVector(PlaneType planeType, float x, float y)
    {
        switch (planeType)
        {
            case PlaneType.XY:
                return new Vector3(x, y, 0);
            case PlaneType.YZ:
                return new Vector3(0, x, y);
            case PlaneType.ZX:
                return new Vector3(y, 0, x);
            default:
                return Vector3.zero;
        }
    }

    void MotionDataCollection()
    {
        if (b_enableMotionDataCollection == false)
            return;
        if (bGameRunning == true && bGamePause == false)
        {
            if (deviceConfig.hardwareType == ControllerHardwareType.OculusTouch)
            {
                DataOculus.myOculusData.time = gameRuningTime;
                Vector3 shoulderCenterPos = body_JNT.transform.position + new Vector3(0, 0.48f, 0);
                DataOculus.myOculusData.shoulder_x = shoulderCenterPos.x;
                DataOculus.myOculusData.shoulder_y = shoulderCenterPos.y;
                DataOculus.myOculusData.shoulder_z = shoulderCenterPos.z;

                Transform hand_trans = hand_l.transform;
                DataOculus.myOculusData.hand_left.x = hand_trans.position.x;
                DataOculus.myOculusData.hand_left.y = hand_trans.position.y;
                DataOculus.myOculusData.hand_left.z = hand_trans.position.z;
                DataOculus.myOculusData.hand_left.qw = hand_trans.rotation.w;
                DataOculus.myOculusData.hand_left.qx = hand_trans.rotation.x;
                DataOculus.myOculusData.hand_left.qy = hand_trans.rotation.y;
                DataOculus.myOculusData.hand_left.qz = hand_trans.rotation.z;
                DataOculus.myOculusData.grasp_left = 0;

                hand_trans = hand_r.transform;
                DataOculus.myOculusData.hand_right.x = hand_trans.position.x;
                DataOculus.myOculusData.hand_right.y = hand_trans.position.y;
                DataOculus.myOculusData.hand_right.z = hand_trans.position.z;
                DataOculus.myOculusData.hand_right.qw = hand_trans.rotation.w;
                DataOculus.myOculusData.hand_right.qx = hand_trans.rotation.x;
                DataOculus.myOculusData.hand_right.qy = hand_trans.rotation.y;
                DataOculus.myOculusData.hand_right.qz = hand_trans.rotation.z;
                DataOculus.myOculusData.grasp_right = 0;

                DataOculus.saveRecordData();
            }
            else if (deviceConfig.hardwareType == ControllerHardwareType.Exoskeleton)
            {

            }
            else if (deviceConfig.hardwareType == ControllerHardwareType.Kinect)
            {

            }
        }
    }

    void AddVectorToList(List<TimeVector3> vectorList, Vector3 newVector)
    {
        // Check if the list is empty or the newVector is sufficiently far from the last vector
        if (vectorList.Count == 0 || Vector3.Distance(newVector, vectorList[vectorList.Count - 1].position) >= pointPrecision)
        {
            vectorList.Add(new TimeVector3(gameRuningTime, newVector));
        }
    }
    void Update()
    {
        Debug.LogWarning("left hand pos: " + hand_l.transform.position);
        if (gamePlaymode == GamePlayMode.Left || gamePlaymode == GamePlayMode.Bilateral) {
            AddVectorToList(leftArmPosList, hand_l.transform.position);
        }
        if (gamePlaymode == GamePlayMode.Right || gamePlaymode == GamePlayMode.Bilateral) {
            AddVectorToList(rightArmPosList, hand_r.transform.position);
        }
        if (deviceConfig.hardwareType == ControllerHardwareType.OculusTouch)
        {
            //use oculus avatar position
            //if (body_JNT == null)
            //{
            //    body_JNT = getChildGameObject(LocalAvatar, "body_JNT");
            //}
            //else
            //    avatarShoulderCenterPos = body_JNT.transform.position + new Vector3(0, 0.48f, 0);
            //use oculus avatar position
            // if (body_JNT == null)
            // {
            //     body_JNT = getChildGameObject(LocalAvatar, "body_JNT");
            //     if (body_JNT != null)
            //     {
            //         avatarShoulderCenterPos = body_JNT.transform.position + avatarShoulder_body_JNT_Offset;
            //         //rightShoulderPos = avatarShoulderCenterPos + avatarBallOffset;
            //         setPopBasePosition();
            //     }
            //     waitTime_initialize = fCurrentTime;
            // }
            // else
            // {
            //     if (waitTime_initialize != -1)
            //     {
            //         if (fCurrentTime - waitTime_initialize > 2.0f)
            //         {
            //             waitTime_initialize = -1;
            //             avatarShoulderCenterPos = body_JNT.transform.position + avatarShoulder_body_JNT_Offset;
            //             //rightShoulderPos = avatarShoulderCenterPos + avatarBallOffset;
            //             setPopBasePosition();
            //         }
            //     }
            // }
            //avatarShoulderCenterPos = body_JNT.transform.position + avatarShoulder_body_JNT_Offset;
            //setPopBasePosition();
        }
        else
        {
            //use exo avatar position
            //avatarShoulderCenterPos
        }
        updateGameTime();
        update_displays();
        if (game_MenuControl != null)
        {
            if (game_MenuControl.b_showMenu == true)
                bGamePause = true;
            else
                bGamePause = false;
        }
        if (bGameRunning == false || bGamePause == true)
        {
            if (currentGOExplosion != null)
            {
                Destroy(currentGOExplosion);
            }
            return;
        }
        // useMouserControl();
        // calculateTrajectoryDistance();
        
        
        // Hidden old popclap
        // #region
        // if (i_delay_cnt < 0)
        //     i_delay_cnt = 0;
        // else
        //     i_delay_cnt--;
        // if (i_delay_cnt == 0)
        // if (b_rotateCannon)
        // {
        //     cannonRotateIndex++;
        //     cannonRotatedCnt++;
        //     if (cannonRotateIndex >= maxRotateIndex)
        //     {
        //         cannonRotateIndex = 0;
        //     }
        //     if (cannonRotatedCnt >= maxRotateIndex)
        //     {
        //         //game finished
        //         audioSource.PlayOneShot(audioclipGameFinished, 0.5F);

        //         bGameRunning = false;
        //         txtGameFinished.text = "Game Finished !";
        //         saveGameResultLog();
        //     }
        //     else
        //     {
        //         //if(audioclipCannonRotate != null)
        //         //    audioSource.PlayOneShot(audioclipCannonRotate, 0.5F);
        //     }
        //     rotateBallPosition();
        //     b_rotateCannon = false;
        //     timeBeginCannonRotate = fCurrentTime;
        //     timeoutPlay = 0; //reset timeout
        //     //if (particle != null)
        //     //    particle.Play();
        // }
        // else
        // {
        //     if (shpereIndex < max_gameObjNum)
        //     {
        //         if (fCurrentTime - timeBeginCannonRotate > max_timeCannonRotate)
        //         {
        //             launchBall();
        //         }
        //     }

        //     if (timeoutPlay > max_timeoutPlay)
        //     {
        //         timeoutPlay = 0;
        //         gotoNextGamePlay();
        //     }
        // }
        // #endregion
        stopExplosionEffect();

        MotionDataCollection();
    }

    void updateGameTime()
    {
        fCurrentTime = Time.time;
        if (bGameRunning == true && bGamePause == false)
        {
            gameRuningTime += fCurrentTime - fPreviousTime;
            if (fCurrentTime - timeBeginCannonRotate > max_timeCannonRotate)
            timeoutPlay += fCurrentTime - fPreviousTime;            
        }
        fPreviousTime = fCurrentTime;
    }

    void update_displays()
    {
        calculateResult();
        if (gamePlaymode == GamePlayMode.Bilateral)
            i_gameResult_total_balls = 2 * max_gameObjNum * maxRotateIndex;
        else
            i_gameResult_total_balls = max_gameObjNum * maxRotateIndex;
        if (txtProgress != null)
        {
            float percent = f_gameResult_touched_percent * 100;            
            txtProgress.text = "Progress " + percent.ToString("N0") + "%";
        }
        if (ImageProgress != null)
        {
            ImageProgress.fillAmount = f_gameResult_touched_percent;
        }
        if (txtGameTime != null)
            txtGameTime.text = "Time: " + gameRuningTime.ToString("N0") + "  (s)";

        if (txtTimeout != null)
        {
            float tt = max_timeoutPlay - timeoutPlay;
            txtTimeout.text = "Timeout: " + tt.ToString("N0") + " s";
        }
        if (ImageTimeout != null)
        {
            ImageTimeout.fillAmount = timeoutPlay / max_timeoutPlay;
        }
        if (txtBallTouched != null)
            txtBallTouched.text = "Ball Touched: " + i_gameResult_touched_count.ToString("N0") + " / " + i_gameResult_total_balls.ToString("N0");
        if (txtGameLevel != null)
            txtGameLevel.text = "Level: " + gameLevel.ToString() + " / " + max_gameLevel.ToString();
        if (txtGameMode != null)
        {
            if (gamePlaymode == GamePlayMode.Left)
                txtGameMode.text = "Mode: Left";
            else if (gamePlaymode == GamePlayMode.Right)
                txtGameMode.text = "Mode: Right";
            else
                if (gamePlaymode == GamePlayMode.Bilateral)
                    txtGameMode.text = "Mode: Bilateral";
        }
        if (txtGameBranch != null)
        {
            int branch = cannonRotateIndex + 1;
            txtGameBranch.text = "Config: " + branch + " / 10";
        }
        if (bGamePause == true)
            txtGameFinished.text = "";
    }
    void calculateTrajectoryDistance()
    {
        //Vector3 hand_pos_left = avatarData.GetAvatarLeftHandPosition();
        //Vector3 hand_pos_right = avatarData.GetAvatarRightHandPosition();
        Vector3 hand_pos_left = hand_l.transform.position;
        Vector3 hand_pos_right = hand_r.transform.position;

        float dist = 0;
        if(gamePlaymode == GamePlayMode.Left)
        {
            dist = 0;
            dist = Vector3.Distance(hand_pos_left, pre_hand_pos_left);
            //if (Mathf.Abs(dist) > 0.001 && Mathf.Abs(dist) < 0.5 && avatarData.IsUserTracked())
                f_gameResult_trajectory_distance_left += dist;
            if (txtTrajectory != null)
                txtTrajectory.text = "Trajectory: " + f_gameResult_trajectory_distance_left.ToString("N2") + " (m)";
        }
        else if (gamePlaymode == GamePlayMode.Right)
        {
            dist = 0;
            dist = Vector3.Distance(hand_pos_right, pre_hand_pos_right);
            //if (Mathf.Abs(dist) > 0.001 && Mathf.Abs(dist) < 0.5 && avatarData.IsUserTracked())
                f_gameResult_trajectory_distance_right += dist;
            if (txtTrajectory != null)
                txtTrajectory.text = "Trajectory: " + f_gameResult_trajectory_distance_right.ToString("N2") + " (m)";
        }
        else
        {
            dist = 0;
            dist = Vector3.Distance(hand_pos_left, pre_hand_pos_left);
            //if (Mathf.Abs(dist) > 0.001 && Mathf.Abs(dist) < 0.5 && avatarData.IsUserTracked())
                f_gameResult_trajectory_distance_left += dist;
            dist = 0;
            dist = Vector3.Distance(hand_pos_right, pre_hand_pos_right);
            //if (Mathf.Abs(dist) > 0.001 && Mathf.Abs(dist) < 0.5 && avatarData.IsUserTracked())
                f_gameResult_trajectory_distance_right += dist;
            if (txtTrajectory != null)
                txtTrajectory.text = "Trajectory left=" + f_gameResult_trajectory_distance_left.ToString("N2") + " meter\n"
                     + "Trajectory right=" + f_gameResult_trajectory_distance_right.ToString("N2") + " meter";

        }
        //speed
        leftHand_speed = (hand_pos_left - pre_hand_pos_left);
        rightHand_speed = (hand_pos_right - pre_hand_pos_right);

        pre_hand_pos_left = hand_pos_left;
        pre_hand_pos_right = hand_pos_right;

    }
    //void Awake()
    //{
    //    instance = this;
    //    gameLevel = 0;
    //    setGameLevel();
    //}

    // void useMouserControl()
    // {
    //     if (Input.GetMouseButtonDown(0))
    //     {
    //         RaycastHit hit;
    //         Ray ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
    //         if (Physics.Raycast(ray, out hit, 100.0f))
    //         {
    //             //OnCollisionEnter(hit.collider);
    //             OnTriggerEnter(hit.collider);
    //         }
    //     }
    //     Launchtest();
    // }
    void calculateResult()
    {
        f_gameResult_touched_percent = (float)i_gameResult_touched_count / (float)i_gameResult_total_balls;
        f_gameResult_progress = (float)(max_gameObjNum * cannonRotatedCnt + shpereIndex) / (float)i_gameResult_total_balls;        
        //float percent = (float)cannonRotatedCnt / (float)maxRotateIndex * 100f;
    }
    void gotoNextGamePlay()
    {
        b_rotateCannon = true;
        i_delay_cnt = 50;
        //audioSource.PlayOneShot(audioclipGameWin, 0.3F);
        //if (particle != null)
        //{
        //    particle.Stop();
        //    //pparticle.gameObject.SetActive(false);
        //}
    }
    public void OnChildTriggerEnter(Collider other)
    {
        //other.GetComponent<Rigidbody>().AddForce(-transform.up * 100);
        //float radius = 5.0f;
        //float power = 100.0f;
        //Vector3 explosionPos = new Vector3(0, 0, 0);

        //Vector3 hand_pos_right = hand_r.transform.position;
        //Vector3 explosionPos = hand_pos_right;


        //other.GetComponent<Rigidbody>().AddExplosionForce(power, explosionPos, radius, 3.0f);
        //other.GetComponent<Collider>().isTrigger = false;
        OnTriggerEnter(other);
    }
    void OnTriggerEnter(Collider other)
    //void OnCollisionEnter(Collider other)
    {
        if (bGameRunning == false || bGamePause == true)
            return;
        if (gamePlaymode == GamePlayMode.Bilateral)
            return;
        if (other.gameObject.name.Contains("CannonBall"))
        {
            if ((gamePlaymode == GamePlayMode.Left && isLeftHandCollidingwithBall(other.gameObject))
                || (gamePlaymode == GamePlayMode.Right && isRightHandCollidingwithBall(other.gameObject)))
            if (other.GetComponent<Renderer>().material.color != Color.green)
            {
                i_gameResult_touched_count++;
                other.GetComponent<Renderer>().material.color = Color.green;
                triggerExplosionEffect();
                audioSource.PlayOneShot(audioclipTouchSphere, 0.7F);
           }
            //shpereIndex++;
            //if (shpereIndex == max_gameObjNum - 1)
            ////if (shpereIndex == 1)
            //{
            //    gotoNextGamePlay();
            //}
            
        }
    }

    //private void OnTriggerStay(Collider other)
    //{
    //    if (bGameRunning == false || bGamePause == true)
    //        return;
    //    if (gamePlaymode == GamePlayMode.Bilateral)
    //    {
    //        if (other.gameObject.name.Contains("CannonBall"))
    //        {

    //            foreach (ContactPoint contact in collision.contacts)
    //            {
    //                print(contact.thisCollider.name + " hit " + contact.otherCollider.name);
    //                Debug.DrawRay(contact.point, contact.normal, Color.white);
    //            }


    //            other.gameObject.transform.localScale = other.gameObject.transform.localScale * 0.5f;
    //        }
    //    }
    //}


    private void OnTriggerStay(Collider other)
    {
        if (bGameRunning == false || bGamePause == true)
            return;
        if (other.gameObject.name.Contains("CannonBall"))
        {
            if (gamePlaymode == GamePlayMode.Bilateral)
            {
                if (isBothHandCollidingwithBall(other.gameObject))
                {
                    if (other.GetComponent<Renderer>().material.color != Color.green)
                    {
                        i_gameResult_touched_count++;
                        other.GetComponent<Renderer>().material.color = Color.green;
                        triggerExplosionEffect();
                        audioSource.PlayOneShot(audioclipTouchSphere, 0.7F);
                    }
                    other.gameObject.transform.localScale = other.gameObject.transform.localScale * handSpeedtoSizeRatio(getHandSpeed());
                }
            }
            else
            {
                if ((gamePlaymode == GamePlayMode.Left && isLeftHandCollidingwithBall(other.gameObject))
                    || (gamePlaymode == GamePlayMode.Right && isRightHandCollidingwithBall(other.gameObject)))
                    other.gameObject.transform.localScale = other.gameObject.transform.localScale * handSpeedtoSizeRatio(getHandSpeed());
            }
        }
    }
    float getHandSpeed()
    {
        float hand_speed = 0;
        //Vector3 hand_pos_left = hand_l.transform.position;
        //Vector3 hand_pos_right = hand_r.transform.position;



        if (gamePlaymode == GamePlayMode.Bilateral)
            hand_speed = (leftHand_speed + rightHand_speed).magnitude / 2.0f;
        else if (gamePlaymode == GamePlayMode.Left)
            hand_speed = leftHand_speed.magnitude;
        else if (gamePlaymode == GamePlayMode.Right)
            hand_speed = rightHand_speed.magnitude;
        return hand_speed;
    }
    float handSpeedtoSizeRatio(float hand_speed) //0-1
    {
        float ratio = 0;
        hand_speed = hand_speed * 50.0f;
        if (hand_speed > 1.0f)
            hand_speed = 1.0f;
        ratio = 1.0f - hand_speed;
        return ratio;
    }
    bool isLeftHandCollidingwithBall(GameObject ballObj)
    {
        Vector3 curPos = ballObj.transform.position;
        Vector3 hand_pos = hand_l.transform.position;
        float dist = (curPos - hand_pos).magnitude;
        float ball_radius = 0.3f;
        if (dist < ball_radius)
            return true;
        else
            return false;
    }
    bool isRightHandCollidingwithBall(GameObject ballObj)
    {
        Vector3 curPos = ballObj.transform.position;
        Vector3 hand_pos = hand_r.transform.position;
        float dist = (curPos - hand_pos).magnitude;
        float ball_radius = 0.3f;
        if (dist < ball_radius)
            return true;
        else
            return false;
    }
    bool isBothHandCollidingwithBall(GameObject ballObj)
    {
        //Vector3 curPos = ballObj.transform.position;
        ////Vector3 hand_pos_left = avatarData.GetAvatarLeftHandPosition();
        ////Vector3 hand_pos_right = avatarData.GetAvatarRightHandPosition();
        //Vector3 hand_pos_left = hand_l.transform.position;
        //Vector3 hand_pos_right = hand_r.transform.position;
        //float distLeft = (curPos - hand_pos_left).magnitude;
        //float distRight = (curPos - hand_pos_right).magnitude;
        //float ball_radius = 0.2f;
        //if (distLeft < ball_radius && distRight < ball_radius)
        //    return true;
        //else
        //    return false;
        if (isLeftHandCollidingwithBall(ballObj) && isRightHandCollidingwithBall(ballObj))
            return true;
        else
            return false;
    }

    void stopExplosionEffect()
    {
        if (currentGOExplosion != null)
        {
            if ((fCurrentTime - durationExplosion) > 1.0f)
            {
                Destroy(currentGOExplosion);
            }
        }
    }

    // void Launchtest()
    // {
    //     //if (Input.GetMouseButtonDown(0))
    //     //if (Input.GetKey(KeyCode.Return))
    //     if (Input.GetKey(KeyCode.LeftShift))
    //     {
    //         launchBall();
    //     }
    // }

    void triggerExplosionEffect()
    {
        if (currentGOExplosion != null)
        {
            Destroy(currentGOExplosion);
        }
        durationExplosion = fCurrentTime;
        currentGOExplosion = Instantiate(particleSystemsExplosion.particleSystemGO, particleSystemsExplosion.particlePosition, Quaternion.Euler(particleSystemsExplosion.particleRotation)) as GameObject;
    }
    // void launchBall()
    // {
    //     if (fCurrentTime - last_launch_time > 1.0f)
    //     {
    //         i_gameResult_lauched_count++;
    //         last_launch_time = fCurrentTime;
    //         GameObject instantiatedObj = Instantiate(CannonBallObj, cannonBallBase.transform) as GameObject;
    //         instantiatedObj.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
    //         instantiatedObj.GetComponent<Rigidbody>().useGravity = false;
    //         instantiatedObj.GetComponent<Rigidbody>().isKinematic = false;
    //         instantiatedObj.GetComponent<Rigidbody>().AddRelativeForce(Vector3.forward * appliedBallForce);
    //         //instantiatedObj.GetComponent<Rigidbody>().velocity.Set(0, -10, 0);
    //         Destroy(instantiatedObj, max_timeoutPlay);

          
    //         if (shpereIndex == 0)
    //         {
    //             audioSource.PlayOneShot(audioclipLaunchBall, 0.2f);
    //             //triggerExplosionEffect();
    //         }
    //         shpereIndex++;
    //     }
    // }
    static public GameObject getChildGameObject(GameObject ParentGameObject, string withName)
    {
        Transform[] ts = ParentGameObject.transform.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in ts)
        {
            if (t.gameObject.name == withName)
                return t.gameObject;
        }
        return null;
    }
    // void remove_mirror_obj()
    // {
   
    // }
    // void mirror_obj()
    // {
   
    // }
    // void rotateBallPosition()
    // {
    //     //launchBall();
    //     //return;
    //     shpereIndex = 0;
    //     Vector3 targetPos = Vector3.zero;
    //     //Vector3 shoulderCenterPos = new Vector3(0, -0.55f, 0);//y -1.92=floor//-0.55=shoulder center
    //     Vector3 shoulderCenterPos = avatarShoulderCenterPos;
    //     Vector3 offsetPos = Vector3.zero;
    //     float fRangeHorizonal =  0.6f * (float)(gameLevel + 1) / (float)max_gameLevel;
    //     float fRangeFront = 0.6f * (float)(gameLevel + 1) / (float)max_gameLevel;
    //     float offsetX = 0;
    //     float offsetY = 0;
    //     float offsetZ = 0;
     
    //     fRangeHorizonal = 0.6f;
    //     fRangeFront = 0.3f;

    //     appliedBallForce = 30.0f + (gameLevel / max_gameLevel) * 30;
    //     if (gamePlaymode == GamePlayMode.Left)
    //     {
    //         offsetPos = new Vector3(offsetX, offsetY, offsetZ);
    //         fRangeHorizonal = -fRangeHorizonal;
    //     }
    //     else if (gamePlaymode == GamePlayMode.Right || gamePlaymode == GamePlayMode.Bilateral)
    //     {
    //         offsetPos = new Vector3(-offsetX, offsetY, offsetZ);
    //         fRangeHorizonal = fRangeHorizonal;
    //     }
    //     float x = 0;
    //     float y = 0;
    //     float z = 0;
       
    //     x = avatarShoulderCenterPos.x;
    //     y = avatarShoulderCenterPos.y;
    //     z = avatarShoulderCenterPos.z;
       
    //     CannonObj.transform.position = new Vector3(x, CannonObj.transform.position.y, z) + avartarBallBaseOffset + ballPositionAdjustment;
    //     //if (gamePlaymode == GamePlayMode.Bilateral)
    //     //{
    //     //    if (CannonMirrorObj != null)
    //     //        CannonMirrorObj.transform.LookAt(new Vector3(-targetPos.x, targetPos.y, targetPos.z), Vector3.up);
    //     //}
    //     //setPopBasePosition();
    //     return;
    // }
    void setPopBasePosition()
    {
        if (CannonObj != null)
        {
            Vector3 newPos = new Vector3(avatarShoulderCenterPos.x, CannonObj.transform.position.y, avatarShoulderCenterPos.z);
            //Vector3 newPos = new Vector3(0, CannonObj.transform.position.y, 0);
            CannonObj.transform.position = newPos + avartarBallBaseOffset + ballPositionAdjustment;
        }
       
    }
    //public static FlowerGame Instance
    //{
    //    get
    //    {
    //        return instance;
    //    }
    //}
    public void Quit()
    {
        SceneSwitch sceneSwitch = new SceneSwitch();
        sceneSwitch.CloseScene();
    }
    
    void ResetTrailRenderer(TrailRenderer trailRenderer)
    {
        // Disable the TrailRenderer
        trailRenderer.enabled = false;

        // Enable the TrailRenderer after a short delay
        StartCoroutine(EnableTrailRendererAfterDelay(trailRenderer));
    }

    System.Collections.IEnumerator EnableTrailRendererAfterDelay(TrailRenderer trailRenderer)
    {
        // Wait for a short delay (adjust as needed)
        yield return new WaitForSeconds(0.1f);

        // Enable the TrailRenderer
        trailRenderer.Clear(); // This clears the existing trail segments
        trailRenderer.enabled = true;
    }
    void resetGame()
    {
        //gameLevel = 0;
        //setGameLevel();
        GameObject[] killOnReset = GameObject.FindGameObjectsWithTag("killOnReset");
        foreach (GameObject killMe in killOnReset)
        {
            Destroy(killMe);
        }
        gameRuningTime = 0;
        timeoutPlay = 0;
        fPreviousTime = Time.time;
        bGameRunning = true;
        i_gameResult_lauched_count = 0;
        i_gameResult_touched_count = 0;
        f_gameResult_touched_percent = 0;
        f_gameResult_trajectory_distance_left = 0;
        f_gameResult_trajectory_distance_right = 0;
        leftGameOrigin.transform.position = hand_l.transform.position;
        rightGameOrigin.transform.position = hand_r.transform.position;
        rightArmPosList.Clear();
        leftArmPosList.Clear();
        //pre_hand_pos_left = avatarData.GetAvatarLeftHandPosition();
        //pre_hand_pos_right = avatarData.GetAvatarRightHandPosition();
        pre_hand_pos_left = hand_l.transform.position;
        pre_hand_pos_right = hand_r.transform.position;
        shpereIndex = 0;
        cannonRotateIndex = 0;
        cannonRotatedCnt = 0;
        // rotateBallPosition();
        txtGameFinished.text = "";
        if (DataOculus != null)
            DataOculus.StopRecordData();
        
        lineRendererLeft.positionCount = 0;
        lineRendererRight.positionCount = 0;
        setupTargetLocations();
        if (gamePlaymode == GamePlayMode.Bilateral || gamePlaymode == GamePlayMode.Left) {
        DrawGame(lineRendererLeft, leftGameOrigin);
        }
        if (gamePlaymode == GamePlayMode.Bilateral || gamePlaymode == GamePlayMode.Right) {
        DrawGame(lineRendererRight, rightGameOrigin);
        }

        foreach (TrailRenderer tr in TrailRendererList) {
            ResetTrailRenderer(tr);
        }
        
    }
    public void resetGames()
    {
        if (deviceConfig.hardwareType == ControllerHardwareType.OculusTouch)
        {
            if (body_JNT != null)
                avatarShoulderCenterPos = body_JNT.transform.position + avatarShoulder_body_JNT_Offset;
        }
        else
        {
        }

        //rightShoulderPos = avatarShoulderCenterPos + avatarBallOffset;
        setPopBasePosition();
       resetGame();
        buttonClickSound();
    }
    public void levelUp()
    {
        gameLevel++;
        if (gameLevel > max_gameLevel)
            gameLevel = max_gameLevel;
        OpenLevelFile(gameLevel);
        // setGameLevel();
        buttonClickSound();
    }
    public void levelDown()
    {
        gameLevel--;
        if (gameLevel < 1)
            gameLevel = 1;
        OpenLevelFile(gameLevel);
        // setGameLevel();
        buttonClickSound();
    }
    // public void setGameLevel()
    // {
    //     rotateBallPosition();
    //     bGameRunning = true;
    //     resetGame();
    //     buttonClickSound();
    // }

    public void setPlayModeLeft()
    {
        gamePlaymode = GamePlayMode.Left;
        // remove_mirror_obj();
        resetGame();
        setCameraView();
        buttonClickSound();
    }
    public void setPlayModeRight()
    {
        gamePlaymode = GamePlayMode.Right;
        // remove_mirror_obj();
        setCameraView();
        resetGame();
        buttonClickSound();
    }
    public void setPlayModeBilateral()
    {
        gamePlaymode = GamePlayMode.Bilateral;
        setCameraView();
        // mirror_obj();
        resetGame();
        buttonClickSound();
    }
    public void skipGametoNext()
    {
        gotoNextGamePlay();
    }
    void setCameraView()
    {
        Vector3 rotation1 = Vector3.zero;
        Vector3 position1 = Vector3.zero;
        switch(gamePlaymode)
        {
            case GamePlayMode.Left:
                position1 = new Vector3(-2.58325f, 1.28825f, -0.5072f);
                rotation1 = new Vector3(0.395f, 44.582f, -0.507f);
                if (Camera1 != null)
                {
                    Camera1.transform.eulerAngles = rotation1;
                    Camera1.transform.position = position1;
                }
                break;
            case GamePlayMode.Right:
                position1 = new Vector3(2.58325f, 1.28825f, -0.5072f);
                rotation1 = new Vector3(0.395f, -44.582f, -0.507f);
                if (Camera1 != null)
                {
                    Camera1.transform.eulerAngles = rotation1;
                    Camera1.transform.position = position1;
                }
                break;
            case GamePlayMode.Bilateral:
                break;
        }
    }
    // public void branchUp()
    // {
    //     cannonRotateIndex++;
    //     if (cannonRotateIndex >= maxRotateIndex)
    //         cannonRotateIndex = 0;
    //     rotateBallPosition();
    //     buttonClickSound();
    // }
    // public void branchDown()
    // {
    //     cannonRotateIndex--;
    //     if (cannonRotateIndex < 0)
    //         cannonRotateIndex = maxRotateIndex;
    //     rotateBallPosition();
    //     buttonClickSound();
    // }
    void buttonClickSound()
    {
        audioSource.PlayOneShot(audioclipButtonClick, 0.7F); 
    }
    void saveGameResultLog()
    {
        gameResultLog.strGameName = strGameName;
        gameResultLog.strGameTime = txtGameTime.text;
        gameResultLog.strBallTouched = txtBallTouched.text;
        gameResultLog.strTrajectory = txtTrajectory.text;
        gameResultLog.strGameLevel = txtGameLevel.text;
        gameResultLog.strGameBranch = txtGameBranch.text;
        gameResultLog.strGameMode = txtGameMode.text;
        gameResultLog.strProgress = txtProgress.text;
        gameResultLog.SaveGameResultFile();
    }
    void initUIButtons()
    {
        dropdownPlaneSelect.onValueChanged.AddListener(OnDropdownValueChanged);
        GameObject gameGUIObj = GameObject.Find("TherapistGUI");
        if (gameGUIObj == null)
            return;
        GameObject gameGUIObj1 = getChildGameObject(gameGUIObj, "TherapistGUIPanel");
        //GameObject gameGUIObj1 = GameObject.Find("TherapistGUIPanel");
        if (gameGUIObj1 != null)
        {
            GameObject gameButtonObj = getChildGameObject(gameGUIObj1, "Button_Left");
            if (gameButtonObj != null)
                gameButtonObj.GetComponent<Button>().onClick.AddListener(setPlayModeLeft);
            gameButtonObj = getChildGameObject(gameGUIObj1, "Button_Right");
            if (gameButtonObj != null)
                gameButtonObj.GetComponent<Button>().onClick.AddListener(setPlayModeRight);
            gameButtonObj = getChildGameObject(gameGUIObj1, "Button_Bilateral");
            if (gameButtonObj != null)
                gameButtonObj.GetComponent<Button>().onClick.AddListener(setPlayModeBilateral);
            gameButtonObj = getChildGameObject(gameGUIObj1, "Button_Reset");
            if (gameButtonObj != null)
                gameButtonObj.GetComponent<Button>().onClick.AddListener(resetGames);
            gameButtonObj = getChildGameObject(gameGUIObj1, "Button_LevelUp");
            if (gameButtonObj != null)
                gameButtonObj.GetComponent<Button>().onClick.AddListener(levelUp);
            gameButtonObj = getChildGameObject(gameGUIObj1, "Button_LevelDown");
            if (gameButtonObj != null)
                gameButtonObj.GetComponent<Button>().onClick.AddListener(levelDown);
            gameButtonObj = getChildGameObject(gameGUIObj1, "Button_Exit");
            if (gameButtonObj != null)
                gameButtonObj.GetComponent<Button>().onClick.AddListener(Quit);
            gameButtonObj = getChildGameObject(gameGUIObj1, "Button_BranchUp");
            // if (gameButtonObj != null)
                // gameButtonObj.GetComponent<Button>().onClick.AddListener(branchUp);
            // gameButtonObj = getChildGameObject(gameGUIObj1, "Button_BranchDown");
            // if (gameButtonObj != null)
                // gameButtonObj.GetComponent<Button>().onClick.AddListener(branchDown);

        }
    }
    //adjust game position
    //slider value from 0 to 100, middle is 50, output range is +-0.5m
    public void moveBall_left_right(float value)
    {
        ballPositionAdjustment.x = (value - 50.0f) / 100.0f;
        //setPopBasePosition();
        //resetGame();
        resetGames();
    }
    public void moveBall_back_forth(float value)
    {
        ballPositionAdjustment.z = (value - 50.0f) / 100.0f + 0.5f; 
        //setPopBasePosition();
        //resetGame();
        resetGames();
    }
    public void moveBall_up_down(float value)
    {
        ballPositionAdjustment.y = (value - 50.0f) / 100.0f;
        //setPopBasePosition();
        //resetGame();
        resetGames();
    }
    void SaveDefaultLevelFile(int level)
    {
        switch (level)
        {
            case 1:
                ballPositionAdjustment = new Vector3(0, 0f, 0.3f);
                break;
            case 2:
                ballPositionAdjustment = new Vector3(0, 0f, 0.3f);
                break;
            case 3:
                ballPositionAdjustment = new Vector3(0, 0f, 0.3f);
                break;
            case 4:
                ballPositionAdjustment = new Vector3(0, 0f, 0.3f);
                break;
            case 5:
                ballPositionAdjustment = new Vector3(0, 0f, 0.3f);
                break;
        }
        SaveLevelFile(level);
    }
    void OpenLevelFile(int level)
    {
        StreamReader fileReader = null;
        string path = strGameName + ".level" + level + ".txt";
        if (File.Exists(path) == false)
        {
            SaveDefaultLevelFile(level);
            gameLevel = level;
            //setPopBasePosition();
            //resetGame();
            resetGames();
            return;
        }
#if !UNITY_WSA
        fileReader = new StreamReader(path);
#endif

        // read a line
        string strLine = fileReader.ReadLine();
        if (strLine == null)
        {
            fileReader.Close();
            SaveDefaultLevelFile(level);
        }
        else
        {
            int bb = 0;
            char[] delimiters1 = { ',' };
            string[] sCsvParts = strLine.Split(delimiters1);
            if (sCsvParts.Length == 5)
            {
                float xx, yy, zz;
                int index = 0;
                float.TryParse(sCsvParts[index++], out xx);
                float.TryParse(sCsvParts[index++], out yy);
                float.TryParse(sCsvParts[index++], out zz);
                ballPositionAdjustment.x = xx;
                ballPositionAdjustment.y = yy;
                ballPositionAdjustment.z = zz;
            }
            else//wrong file, creat default file
            {
                fileReader.Close();
                SaveDefaultLevelFile(level);
            }
        }
        fileReader.Close();
        gameLevel = level;
        //setPopBasePosition();
        //resetGame();
        resetGames();
    }
    void SaveLevelFile(int level)
    {
        string path = strGameName + ".level" + level + ".txt";
        if (path.Length != 0)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        using (StreamWriter writer = File.AppendText(path))
        {
            writer.WriteLine(ballPositionAdjustment.x + "," + ballPositionAdjustment.y + "," + ballPositionAdjustment.z + "," + 1 + "," + 1);
            writer.WriteLine("pos.x" + "," + "pos.y" + "," + "pos.z" + "," + "ballSize" + "," + "ballSpeed");
        }
    }
    public void SaveLevel()
    {
        SaveLevelFile(gameLevel);
    }

    public void LoadLevelDefault()
    {
        OpenLevelFile(3);
    }
    public void LoadLevel1()
    {
        OpenLevelFile(1);
    }
    public void LoadLevel2()
    {
        OpenLevelFile(2);
    }
    public void LoadLevel3()
    {
        OpenLevelFile(3);
    }
    public void LoadLevel4()
    {
        OpenLevelFile(4);
    }
    public void LoadLevel5()
    {
        OpenLevelFile(5);
    }
    public void ResetLevel()
    {
        ballPositionAdjustment.x = 0;
        ballPositionAdjustment.y = 0;
        ballPositionAdjustment.z = 0;
        //setPopBasePosition();
        //resetGame();
        resetGames();
    }
}

