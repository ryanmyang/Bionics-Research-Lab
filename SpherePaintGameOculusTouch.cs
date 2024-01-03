using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using UnityEditor;


[RequireComponent(typeof(AudioSource))]
public class SpherePaintGameOculusTouch : MonoBehaviour {
    string strGameName = "SpherePaint";
    DeviceConfig deviceConfig;
    public ControllerHardwareType controllerHardwareType;
    private GameObject hand_l, hand_r, body_JNT, LocalAvatar;
    public AudioClip audioclipTouchSphere;
    public AudioClip audioclipGameWin;
    public AudioClip audioclipGameFinished;
    public AudioClip audioclipButtonClick;
    AudioSource audioSource;
    public Camera sceneCamera;
    public bool bGameRunning = true;
    public bool bGamePause = false;
    protected static FlowerGame instance = null;
    int gameLevel = 1;
    const int max_gameLevel = 5;
    //float radius_touch = 0.1f;
    enum GamePlayMode { Left, Right, Bilateral };
    GamePlayMode gamePlaymode = GamePlayMode.Right;
    private GameObject[] sphereObj;
    private GameObject spheresAllObj;
    int shpereIndex = 0;
    int flowerRotateIndex = 0;
    int flowerRotatedCnt = 0;//how many times rotated? if> max then end of game
    const int maxRotateIndex = 10; //for each level, how many times to play, each play may have same or different configuration
    //float sphereScale = 1.0f;
    const int gameObjDimX = 7;
    const int gameObjDimY = 10;

    int max_gameObjNum = gameObjDimX * gameObjDimY;

    // const int max_gameObjNum = gameObjDimX * gameObjDimY;
    // changed by ryan
    GameMenuControl game_MenuControl;
    bool b_rotateFlower = false;
    int i_delay_cnt = 0;
    //public ParticleSystem particle;

    //private float fStartTime = 0f;
    private float fCurrentTime = 0f;
    private float fPreviousTime = 0f;
    public float gameRuningTime = 0f;
    //fCurrentTime = Time.time;
    //            string sRelTime = string.Format("{0:F3}", (fCurrentTime - fStartTime));

    int i_gameResult_touched_count = 0;
    int i_gameResult_total_balls;
    //changed by ryan
    float f_gameResult_touched_percent = 0;
    float f_gameResult_progress = 0;
    float f_gameResult_trajectory_distance_left = 0;
    float f_gameResult_trajectory_distance_right = 0;
    Vector3 pre_hand_pos_left = Vector3.zero;
    Vector3 pre_hand_pos_right = Vector3.zero;
    //AvatarData avatarData = null;
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
    float timeoutPlay = 0;
    float max_timeoutPlay = 60;//s
    public Text txtTimeout;
    public Image ImageTimeout;
    public Text txtGameFinished;
    //
    private Camera Camera_Exo_FP;
    //bilateral
    Vector3 flower_rot_angle = Vector3.zero;
    private GameObject sphereObjMirror;
    int left_touching, right_touching;
    string left_touch_ObjName = "";
    string right_touch_ObjName = "";
    const int max_touching_duration = 10;

    public Vector3 avatarShoulderCenterPos = new Vector3(0, 1.40f, 0);//oculus avatar
    float waitTime_initialize;
    Vector3 avatarShoulder_body_JNT_Offset = new Vector3(0, 0.48f, 0);
    //log
    DataGameResultLog gameResultLog;
    DataOculusTouch DataOculus;
    public bool b_enableMotionDataCollection = true;
    public GameObject spherePrefab;
    Vector3[] sphereCoords;

    public List<Vector3> touched_sphere_coords_left = new List<Vector3>();
    public List<Vector3> touched_sphere_polar_coords_left = new List<Vector3>();
    public List<Vector3> touched_sphere_coords_right = new List<Vector3>();
    public List<Vector3> touched_sphere_polar_coords_right = new List<Vector3>();

    GameObject[] leftSphereObjs = null;
    GameObject[] rightSphereObjs = null;
    Vector3[] cartesianSphereCoords = null;
    int numCoordsInSphere = 0;

    [SerializeField]
    private bool exportBoolean;

    [SerializeField]
    private bool leftArmSphereActive;
    [SerializeField]
    private bool rightArmSphereActive;

    [SerializeField]
    private Material sphereMaterial;
    GameObject leftOriginSphere = null;
    GameObject rightOriginSphere = null;

    public float generation_radius = 0.8f;
    public int divisions_per_rad = 6;

    // private bool paintingModeOn = false;


    public bool ExportBoolean
    {
        get { return exportBoolean; }
        set
        {
            
                exportBoolean = value;

                // Check if the boolean is true and then perform your action
                if (exportBoolean)
                {
                    // Your logic here
                    Debug.LogWarning("Exporting!");
                    if (touched_sphere_coords_left.Count > 0) {
                        System.DateTime currentDateTime = System.DateTime.Now;

                         // Format the DateTime object as MM_DD_HH_SS
                        string formattedTime = currentDateTime.ToString("MM_dd_HH_ss");
                        ExportToJSON(touched_sphere_coords_left,$"{formattedTime}_touched_sphere_coords_left.json");
                        ExportToJSON(touched_sphere_polar_coords_left,$"{formattedTime}_touched_sphere_polar_left.json");
                    }
                    if (touched_sphere_coords_right.Count > 0) {
                        System.DateTime currentDateTime = System.DateTime.Now;

                         // Format the DateTime object as MM_DD_HH_SS
                        string formattedTime = currentDateTime.ToString("MM_dd_HH_ss");
                        ExportToJSON(touched_sphere_coords_right,$"{formattedTime}_touched_sphere_coords_right.json");
                        ExportToJSON(touched_sphere_polar_coords_right,$"{formattedTime}_touched_sphere_polar_right.json");
                    }
                    exportBoolean = false;

                }
            
        }
    }

    private void ExportToJSON(List<Vector3> list, string fileName)
    {
        string folderName = "data_exports";
        string folderPath = Path.Combine("Assets", folderName);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", folderName);
        }
        string filePath = Path.Combine(folderPath, fileName);   
        string jsonData = JsonUtility.ToJson(new VectorListContainer(list));

        File.WriteAllText(filePath, jsonData);
    }

    [System.Serializable]
    private class VectorListContainer
    {
        public List<Vector3> vectorList;

        public VectorListContainer(List<Vector3> list)
        {
            vectorList = list;
        }
    }

    private void OnValidate()
    {
        // This will be called when the script is loaded or values are modified in the Inspector
        ExportBoolean = exportBoolean;
    }
    //from ryan
    void Start()
    {
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
        //        avatarData = new AvatarData()
        // CONFIGURE SPHERE GENERATION HERE

        Debug.LogWarning("Before generation");
        // Spherical coordinate distribution system (No longer using)
        // const int thetaSub = 24;
        // const int elevSub = 24;
        // const float rad = 1;
        // const int radSub = 6;
        // max_gameObjNum = (thetaSub + 1) * (elevSub + 1) * (radSub + 1);
        // i_gameResult_total_balls = max_gameObjNum * maxRotateIndex;
        // Array.Resize( ref sphereCoords, max_gameObjNum );
        // sphereCoords = GenerateSphericalCoordinates(thetaSub, elevSub, rad, radSub);
        leftOriginSphere = GameObject.Find("SphereLeftOrigin");
        rightOriginSphere = GameObject.Find("SphereRightOrigin");
        
        

        float generation_side_length = generation_radius * 2;
        max_gameObjNum = (1+ divisions_per_rad*2 ) * (1+ divisions_per_rad*2) * (1+ divisions_per_rad*2);
        // Array.Resize( ref sphereCoords, max_gameObjNum ); //going to use new vector3 array instead
        Debug.LogWarning("Before cartesianspherecoords");
        leftArmSphereActive = false;
        rightArmSphereActive = false;
        if (gamePlaymode == GamePlayMode.Bilateral) {
            leftArmSphereActive = true;
            rightArmSphereActive = true;
        }
        else if (gamePlaymode== GamePlayMode.Left) {
            leftArmSphereActive = true;
        }
        else if (gamePlaymode== GamePlayMode.Right) {
            rightArmSphereActive = true;
        }
        Debug.LogWarning("GamePlaymode: " + gamePlaymode);
        // Reworked Process:
        // 1: Declare gameobj list for each sphere to generate (Moved to global scope)
        
        // 2: Generate cartesian sphere coordinates we will use to lay out the game objects, storing the number of coords in this layout
        numCoordsInSphere = -1;
        cartesianSphereCoords = GenerateCartesianSphereCoordinates(generation_radius, divisions_per_rad, ref numCoordsInSphere);
        // Create Sphere Wall should generate the proper number of spheres based on the coordinates it is given, and centered on the given center object
        // Moved to resetGame


        Debug.LogWarning("Done with Ryan's setup");
        
        resetGame();
        Debug.LogWarning("Just finished resetgame");
        audioSource = GetComponent<AudioSource>();
        game_MenuControl = GameMenuControl.Instance;
        //log
        gameResultLog = new DataGameResultLog();
        gameResultLog.strGameName = strGameName;
        gameResultLog.StartGameResultFile();
        //ui
        Debug.LogWarning("About to call initUIButtons");
        initUIButtons();
        //data
        if (deviceConfig.hardwareType == ControllerHardwareType.OculusTouch)
        {
            DataOculus = new DataOculusTouch();
            DataOculus.gameName = strGameName;
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
    void Awake()
    {
        

    }
    void mirror_obj()
    {
        Vector3 pos = spheresAllObj.transform.position;
        Quaternion rot = spheresAllObj.transform.rotation;
        sphereObjMirror = Instantiate(spheresAllObj);
        Vector3 scale2;
        scale2.x = -sphereObjMirror.transform.localScale.x;
        scale2.y = sphereObjMirror.transform.localScale.y;
        scale2.z = sphereObjMirror.transform.localScale.z;
        sphereObjMirror.transform.localScale = scale2;
        pos.x = -pos.x;
        sphereObjMirror.transform.position = pos + new Vector3(avatarShoulderCenterPos.x * 2, 0, 0);
        //sphereObjMirror.transform.eulerAngles = new Vector3(flower_rot_angle.x, flower_rot_angle.y - 180, flower_rot_angle.z);
        //sphereObjMirror.transform.eulerAngles = new Vector3(flower_rot_angle.x, -flower_rot_angle.y, 180 - flower_rot_angle.z);
    }
    void Update()
    {
        if (deviceConfig.hardwareType == ControllerHardwareType.OculusTouch)
        {           //use oculus avatar position
            if (body_JNT == null)
            {
                body_JNT = getChildGameObject(LocalAvatar, "body_JNT");
                if (body_JNT != null)
                {
                    avatarShoulderCenterPos = body_JNT.transform.position + avatarShoulder_body_JNT_Offset;
                    handleSetSpherePositions();
                }
                waitTime_initialize = fCurrentTime;
            }
            else
            {
                if (waitTime_initialize != -1)
                {
                    if (fCurrentTime - waitTime_initialize > 2.0f)
                    {
                        waitTime_initialize = -1;
                        avatarShoulderCenterPos = body_JNT.transform.position + avatarShoulder_body_JNT_Offset;
                        handleSetSpherePositions();
                    }
                }
            }
        }
        else
        {
            //use exo avatar position
            //avatarShoulderCenterPos
        }
        updateGameTime();
        update_displays();
        showSpheres();
        //return;

        // if (game_MenuControl.b_showMenu == true)
        //     // bGamePause = true;
        //     // Debug.Log("bGamePause=true commented out by Ryan");
        // else
        //     bGamePause = false;

        if (bGameRunning == false || bGamePause == true)
            return;
        useMouserControl();
        calculateTrajectoryDistance();
        if (i_delay_cnt < 0)
            i_delay_cnt = 0;
        else
            i_delay_cnt--;
        if (i_delay_cnt == 0)
            if (b_rotateFlower)
            {
                flowerRotateIndex++;
                flowerRotatedCnt++;
                if (flowerRotateIndex >= maxRotateIndex)
                {
                    flowerRotateIndex = 0;
                }
                if (flowerRotatedCnt >= maxRotateIndex)
                {
                    //game finished
                    audioSource.PlayOneShot(audioclipGameFinished, 0.5F);
                    bGameRunning = false;
                    txtGameFinished.text = "Game Finished !";
                    saveGameResultLog();
                }
                rotateFlower();
                b_rotateFlower = false;
                timeoutPlay = 0; //reset timeout
                //if (particle != null)
                //    particle.Play();
            }
            else
            {
                if (timeoutPlay > max_timeoutPlay)
                {
                    timeoutPlay = 0;
                    max_timeoutPlay = 60;
                    gotoNextGamePlay();
                }
            }
        //bilateral touching
        if (left_touching-- < 0)
        {
            left_touching = 0;
            left_touch_ObjName = "";
        }
        if (right_touching-- < 0)
        {
            right_touching = 0;
            right_touch_ObjName = "";
        }
        MotionDataCollection();
    }
    void updateGameTime()
    {
        fCurrentTime = Time.time;
        if (bGameRunning == true && bGamePause == false)
        {
            gameRuningTime += fCurrentTime - fPreviousTime;
            timeoutPlay += fCurrentTime - fPreviousTime;            
        }
        fPreviousTime = fCurrentTime;


    }
    void update_displays()
    {
        calculateResult();
        if (txtProgress != null)
        {
            //float percent = f_gameResult_touched_percent * 100;
            float percent = f_gameResult_progress * 100;
            txtProgress.text = "Progress " + percent.ToString("N0") + "%";
        }
        if (ImageProgress != null)
        {
            //ImageProgress.fillAmount = f_gameResult_touched_percent;
            ImageProgress.fillAmount = f_gameResult_progress;
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
            int branch = flowerRotateIndex + 1;
            txtGameBranch.text = "Board: " + branch + " / 10";
        }
        if (bGamePause == true)
            txtGameFinished.text = "";
    }
    void calculateTrajectoryDistance()
    {
        //   Vector3 hand_pos_left = avatarData.GetAvatarLeftHandPosition();
        //   Vector3 hand_pos_right = avatarData.GetAvatarRightHandPosition();
        Vector3 hand_pos_left = hand_l.transform.position;
        Vector3 hand_pos_right = hand_r.transform.position;
        float dist = 0;
        if(gamePlaymode == GamePlayMode.Left)
        {
            dist = 0;
            dist = Vector3.Distance(hand_pos_left, pre_hand_pos_left);
       //     if (Mathf.Abs(dist) > 0.001 && Mathf.Abs(dist) < 0.5 && avatarData.IsUserTracked())
                f_gameResult_trajectory_distance_left += dist;
            if (txtTrajectory != null)
                txtTrajectory.text = "Trajectory: " + f_gameResult_trajectory_distance_left.ToString("N2") + " (m)";
        }
        else if (gamePlaymode == GamePlayMode.Right)
        {
            dist = 0;
            dist = Vector3.Distance(hand_pos_right, pre_hand_pos_right);
        //    if (Mathf.Abs(dist) > 0.001 && Mathf.Abs(dist) < 0.5 && avatarData.IsUserTracked())
                f_gameResult_trajectory_distance_right += dist;
            if (txtTrajectory != null)
                txtTrajectory.text = "Trajectory: " + f_gameResult_trajectory_distance_right.ToString("N2") + " (m)";
        }
        else
        {
            dist = 0;
            dist = Vector3.Distance(hand_pos_left, pre_hand_pos_left);
         //   if (Mathf.Abs(dist) > 0.001 && Mathf.Abs(dist) < 0.5 && avatarData.IsUserTracked())
                f_gameResult_trajectory_distance_left += dist;
            dist = 0;
            dist = Vector3.Distance(hand_pos_right, pre_hand_pos_right);
         //   if (Mathf.Abs(dist) > 0.001 && Mathf.Abs(dist) < 0.5 && avatarData.IsUserTracked())
                f_gameResult_trajectory_distance_right += dist;
            if (txtTrajectory != null)
                txtTrajectory.text = "Trajectory left=" + f_gameResult_trajectory_distance_left.ToString("N2") + " meter\n"
                     + "Trajectory right=" + f_gameResult_trajectory_distance_right.ToString("N2") + " meter";


        }
        pre_hand_pos_left = hand_pos_left;
        pre_hand_pos_right = hand_pos_right;

    }
    //void Awake()
    //{
    //    instance = this;
    //    gameLevel = 0;
    //    setGameLevel();
    //}

    //in bilateral mode, two #0 balls are overlapped, difficult to use mouse to touch
    //so if one #0 ball is touched ,then two #0 balls are touched
    void mouserHitFistBallBilateral(Collider hit)
    {
        if (gamePlaymode == GamePlayMode.Bilateral)
        {
            if (hit.gameObject.name == ("Sphere" + 0))
            {
                left_touching = max_touching_duration;
                right_touching = max_touching_duration;
            }
        }
    }
    void useMouserControl()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 100.0f))
            {
                mouserHitFistBallBilateral(hit.collider);
                OnTriggerEnter(hit.collider);
            }
        }

    }
    void calculateResult()
    {
        f_gameResult_touched_percent = (float)i_gameResult_touched_count / (float)i_gameResult_total_balls;
        f_gameResult_progress = f_gameResult_touched_percent;
        //f_gameResult_progress = (float)(max_gameObjNum * flowerRotatedCnt + shpereIndex) / (float)i_gameResult_total_balls;
        //f_gameResult_progress = (float)flowerRotatedCnt / (float)maxRotateIndex; 

    }
    void gotoNextGamePlay()
    {
        b_rotateFlower = true;
        i_delay_cnt = 50;
        audioSource.PlayOneShot(audioclipGameWin, 0.3F);
        //if (particle != null)
        //{
        //    particle.Stop();
        //    //pparticle.gameObject.SetActive(false);
        //}
    }
    //void OnTriggerEnter(Collider other)
    //{
    //    if (bGameRunning == false || bGamePause == true)
    //        return;
    //    if (other.gameObject.name == ("Sphere" + shpereIndex))
    //    {
    //        i_gameResult_touched_count++;
    //        calculateResult();
    //        sphereObj[shpereIndex].GetComponent<Renderer>().material.color = Color.red;
    //        other.GetComponent<Renderer>().material.color = Color.red;
    //        shpereIndex++;
    //        if (shpereIndex == max_gameObjNum)
    //        //if (shpereIndex == 1)
    //        {
    //            gotoNextGamePlay();
    //        }
    //        audioSource.PlayOneShot(audioclipTouchSphere, 0.7F);
    //    }         
    //}

    void OnTriggerStay(Collider other)
    {

        if (bGameRunning == false || bGamePause == true)
            return;
        if (gamePlaymode == GamePlayMode.Bilateral)
        {
            //if (other.gameObject.name == "touched")
            //    return;
            if (other.gameObject.GetComponent<Renderer>().material.color == Color.red)//touched
                return;
            if (other.gameObject.name.Contains("Sphere"))
            {
                if (other.gameObject.transform.parent.name.Contains("Clone"))
                {
                    left_touching = max_touching_duration;
                    left_touch_ObjName = other.gameObject.name;
                }
                else
                {
                    right_touching = max_touching_duration;
                    right_touch_ObjName = other.gameObject.name;
                }
                //both left and right are touching, then change to green color//good for kinect
                if (left_touching > 0 && right_touching > 0)
                {
                    if (string.Equals(left_touch_ObjName, right_touch_ObjName))
                       
                    {

                        left_touching = 0;
                        right_touching = 0;
                        if (other.GetComponent<Renderer>().material.color != Color.red)
                        {
                            foreach(GameObject obj in GameObject.FindObjectsOfType<GameObject>())//change color for both matching balls
                            {
                                if(obj.name == right_touch_ObjName)
                                {
                                    obj.GetComponent<Renderer>().material.color = Color.red;
                                    obj.SetActive(false);
                                }
                            }
                            //other.GetComponent<Renderer>().material.color = Color.red;
                            i_gameResult_touched_count++;
                            //obj.name = "touched";
                            shpereIndex++;
                            if (shpereIndex == max_gameObjNum)
                                gotoNextGamePlay();
                            audioSource.PlayOneShot(audioclipTouchSphere, 0.7F);

                        }
                    }
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.LogWarning("Triggerenter");
        if (bGameRunning == false || bGamePause == true)
            return;
        timeoutPlay = 0;
        max_timeoutPlay = 10;

        // Ryan's notes: This is the touch area
        // if (gamePlaymode != GamePlayMode.Bilateral)
        // {
            Debug.LogWarning("Other name: " + other.gameObject.name);
            if (other.gameObject.name.Contains("Sphere"))
            {
                Debug.LogWarning("Triggerenter on a sphere");
                if (other.GetComponent<Renderer>().material.color != Color.red)
                {
                    i_gameResult_touched_count++;
                    if (other.gameObject.name.Contains("Right"))
                    {
                        touched_sphere_coords_right.Add(other.GetComponent<SphereData>().sphereCoords);
                        touched_sphere_polar_coords_right.Add(CartesianToSpherical(other.GetComponent<SphereData>().sphereCoords));
                    }
                    if (other.gameObject.name.Contains("Left"))
                    {
                        touched_sphere_coords_left.Add(other.GetComponent<SphereData>().sphereCoords);
                        touched_sphere_polar_coords_left.Add(CartesianToSpherical(other.GetComponent<SphereData>().sphereCoords));
                    }
                    
                    calculateResult();
                    other.GetComponent<Renderer>().material.color = Color.red;
                    other.gameObject.SetActive(false);
                    //other.name = "touched";
                    shpereIndex++;
                    if (shpereIndex == max_gameObjNum)
                        gotoNextGamePlay();
                    audioSource.PlayOneShot(audioclipTouchSphere, 0.7F);
                }
 

            }
        // }
        // else
        // {
            //if (other.gameObject.name.Contains("Sphere"))
            //{
            //    if (other.gameObject.transform.parent.name.Contains("Clone"))
            //    {
            //        left_touching = max_touching_duration;
            //    }
            //    else
            //    {
            //        right_touching = max_touching_duration;
            //    }
            //    //both left and right are touching, then change to green color//good for mouse click
            //    if (left_touching > 0 && right_touching > 0)
            //    {
            //        left_touching = 0;
            //        right_touching = 0;
            //        {
            //            i_gameResult_touched_count++;
            //            calculateResult();
            //            other.GetComponent<Renderer>().material.color = Color.red;
            //            //sphereObj[shpereIndex].GetComponent<Renderer>().material.color = Color.red;
            //            shpereIndex++;
            //            if (shpereIndex == max_gameObjNum)
            //                gotoNextGamePlay();
            //            audioSource.PlayOneShot(audioclipTouchSphere, 0.7F);
            //        }
            //    }
            //}
        // }
    }
    public void OnChildTriggerEnter(Collider other)
    {
        OnTriggerEnter(other);
    }
    public void OnChildTriggerStay(Collider other)
    {
        OnTriggerStay(other);
    }
    void rotateFlower()
    {
        shpereIndex = 0;
        Vector3 rotation = Vector3.zero;
        switch (flowerRotateIndex)
        {
            case 0:
                rotation = new Vector3(0, 0, 0);
                break;
            case 1:
                rotation = new Vector3(0, 0, 30);
                break;
            case 2:
                rotation = new Vector3(0, 0, 60);
                break;
            case 3:
                rotation = new Vector3(0, 0, -30);
                break;
            case 4:
                rotation = new Vector3(0, 0, -60);
                break;
            case 5:
                rotation = new Vector3(0, -30, 0);
                break;
            case 6:
                rotation = new Vector3(0, -60, 0);
                break;
            case 7:
                rotation = new Vector3(0, 45, 0);
                break;
            case 8:
                rotation = new Vector3(0, -60, 45);
                break;
            case 9:
                rotation = new Vector3(0, -60, -45);
                break;
        }
        if (gamePlaymode == GamePlayMode.Left)
            //rotation = new Vector3(rotation.x, -rotation.y, rotation.z - 180);
            rotation = new Vector3(rotation.x, -rotation.y, 180 - rotation.z);
        flower_rot_angle = rotation;

        handleSetSpherePositions();

        if (sphereObjMirror != null)
            Destroy(sphereObjMirror);
        if (gamePlaymode == GamePlayMode.Bilateral)
            mirror_obj();

        if (spheresAllObj != null)
            spheresAllObj.transform.eulerAngles = rotation;
        if (sphereObjMirror != null)
        {
            //rotation = new Vector3(rotation.x, -rotation.y, 180 - rotation.z);
            rotation = new Vector3(rotation.x, -rotation.y, - rotation.z);
            sphereObjMirror.transform.eulerAngles = rotation;
        }

        //setSphereColor();
        //flowerRotateIndex++; 
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
    //void setShpereScale()
    //{
    //    for (int i = 0; i < max_gameObjNum; i++)
    //    {
    //        if (i == 0)
    //        {
    //            sphereObj[i].transform.localScale *= (sphereScale * 0.05f) / sphereObj[i].transform.lossyScale.x;
    //        }
    //        else if (i == max_gameObjNum - 1)
    //        {
    //            sphereObj[i].transform.localScale *= (sphereScale * 0.05f) / sphereObj[i].transform.lossyScale.x;
    //        }
    //        else
    //        {
    //            sphereObj[i].transform.localScale *= (sphereScale * 0.03f) / sphereObj[i].transform.lossyScale.x;
    //        }
    //    }
    //}
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
    //void destorySphereWall()
    //{
    //    //for (int i = 0; i < gameObjDimX; i++)
    //    //{
    //    //    for (int j = 0; j < gameObjDimY; j++)
    //    //    {
    //    //        if (i == 0 && j == 0)
    //    //            continue;
    //    //        Destroy(sphereObj[i + j * gameObjDimX]);
    //    //    }
    //    //}
    //}
    void createSphereWall(ref GameObject[] objs,int num, GameObject centerObj)
    {
        Debug.LogWarning("Entered createSphereWall");
        objs = new GameObject[num];
        objs[0] = centerObj;
        for (int i = 1;i< num; i++)
        {
            objs[i] = Instantiate(spherePrefab, centerObj.transform) as GameObject;
            objs[i].name = centerObj.name + i;
            objs[i].tag = "Sphere";
        }
    }

    void showSpheres()
    {
        return;//// always show
        bool b_show = false;
        if (bGameRunning == true)
            b_show = !game_MenuControl.b_showMenu;
        if (gameLevel < 2)
            b_show = true;
        if (spheresAllObj != null)
        {
            spheresAllObj.SetActive(b_show);
        }
        if (sphereObjMirror != null)
        {
            sphereObjMirror.SetActive(b_show);
        }
    }
    public Vector3 paintBoardCenterPosition = new Vector3(0.2f, 0, 0);
    public float globalScale = 1.0f;
    void setSpherePosition(Vector3[] coordArr, GameObject[] objArr = null)
    {
        // Debug.LogWarning("setSpherePosition entered. objarr is null? : " + (objArr==null).ToString());
        setSpherePositions(coordArr, objArr);
        return;
        // Changed to spherical by ryan


        Vector3 centerPosition = new Vector3(0,0,0);
        spheresAllObj.transform.rotation = Quaternion.identity;
        /*switch (gameLevel)
        {
            case 0:
                globalScale = 0.25f;
                paintBoardCenterPosition = new Vector3(0.2f, -0.2f, 0);
                break;
            case 1:
                globalScale = 0.35f;
                paintBoardCenterPosition = new Vector3(0.2f, -0.1f, 0);
                break;
            case 2:
                globalScale = 0.50f;
                paintBoardCenterPosition = new Vector3(0.2f, -0.1f, 0);
                break;
            case 3:
                globalScale = 0.65f;
                paintBoardCenterPosition = new Vector3(0.2f, 0f, 0);
                break;
            case 4:
            default:
                globalScale = 0.8f;
                paintBoardCenterPosition = new Vector3(0.2f, 0f, 0);
                break;
        }*/
        centerPosition = paintBoardCenterPosition;
        if (gamePlaymode == GamePlayMode.Left)
            centerPosition.x = -paintBoardCenterPosition.x;

        {
            spheresAllObj.transform.position = avatarShoulderCenterPos + new Vector3(0, 0.2f, 0.5f) + centerPosition;
            Vector3 pos1 = spheresAllObj.transform.position;
            float gapX = 0.1f * globalScale;
            float gapY = 0.1f * globalScale;
            Vector3 centerOffset1 = new Vector3(-gapX * gameObjDimX / 2, -gapY * gameObjDimY / 2, 0);
            for (int i = 0; i < gameObjDimX; i++)
            {

                for (int j = 0; j < gameObjDimY; j++)
                {
                    Vector3 posNew;
                    //if (i % 2 == 0)
                    //    posNew.y = gapY / 2 + pos1.y + gapY * j;
                    //else
                    posNew.y = pos1.y + gapY * j;
                    posNew.x = pos1.x + gapX * i;
                    posNew.z = pos1.z;
                    sphereObj[i * gameObjDimY + j].transform.position = posNew + centerOffset1;
                    sphereObj[i * gameObjDimY + j].GetComponent<Renderer>().material = sphereMaterial;
                    Color currentColor = sphereObj[i * gameObjDimY + j].GetComponent<Renderer>().material.color;
                    currentColor.a = 130;
                    sphereObj[i * gameObjDimY + j].GetComponent<Renderer>().material.color = currentColor;
                    //sphereObj[i].name = "Sphere" + i;
                    //sphereObj[i * gameObjDimY + j].transform.localScale.Set(2.5f, 2.5f ,2.5f);
                }
            }
            Vector3 scale1;
            float scalex = globalScale * 0.1f / spheresAllObj.transform.lossyScale.x;
            float scaley = globalScale * 0.1f / spheresAllObj.transform.lossyScale.y;
            float scalez = globalScale * 0.1f / spheresAllObj.transform.lossyScale.z;
            scale1 = new Vector3(scalex, scaley, scalez);
            spheresAllObj.transform.localScale *= scalex;
        }
        if (gamePlaymode == GamePlayMode.Bilateral)
        {
            if (sphereObjMirror != null)
                Destroy(sphereObjMirror);
            mirror_obj();
        }
        return;
    }


    public static void SphericalToCartesian(float radius, float polar, float elevation, out Vector3 outCart){
            float a = radius * Mathf.Cos(elevation);
            outCart.x = a * Mathf.Cos(polar);
            outCart.y = radius * Mathf.Sin(elevation);
            outCart.z = a * Mathf.Sin(polar);
        }

        
    public static Vector3 CartesianToSpherical(Vector3 cartCoords) {
        if (cartCoords.x == 0)
            cartCoords.x = Mathf.Epsilon;

        float radius = Mathf.Sqrt((cartCoords.x * cartCoords.x)
                            + (cartCoords.y * cartCoords.y)
                            + (cartCoords.z * cartCoords.z));
        float polar = Mathf.Atan(cartCoords.z / cartCoords.x);
        if (cartCoords.x < 0)
            polar += Mathf.PI;
        float elevation = Mathf.Acos(cartCoords.y / radius);

        return new Vector3(radius, polar, elevation);
    }
    Vector3[]  GenerateCartesianSphereCoordinates(float r, int dpu, ref int size) {
        Debug.LogWarning("GenerateCartesianSphereCoordinates start | r: " + r + " dpu: " + dpu);
        float dpu_float = (float)dpu;

        List<Vector3> l = new List<Vector3>();
        for (float i = -r; i <= r; i+=(r/dpu_float)) {
            for (float j = -r; j <= r; j+=(r/dpu_float)) {
                for (float k = -r; k <= r; k+= (r/dpu_float)) {
                    // Debug.LogWarning("i: " + i + "j: " + j+ "k: " + k);
                    l.Add(new Vector3(i,j,k));
                }   
            }
        }
        //HERE
        for (int i = l.Count - 1; i >= 0; i--) {
            if (l[i].magnitude > r) {
                l.RemoveAt(i);
            }
        }       
        Debug.LogWarning($"[{string.Join(",", l)}]");
        
        Vector3[] retArr = l.ToArray();
        size = retArr.Length;
        return retArr;
    }
    Vector3[] GenerateSphericalCoordinates(int thetaSubdivisions, int elevationSubdivisions, float radius, int radiusSubdivisions)
    {
        int numPoints = (thetaSubdivisions + 1) * (elevationSubdivisions + 1) * (radiusSubdivisions + 1); // Add +1 to radiusSubdivisions
        Vector3[] sphericalCoordinates = new Vector3[numPoints];

        int index = 0;
        for (int r = 0; r <= radiusSubdivisions; r++) // Change r < radiusSubdivisions to r <= radiusSubdivisions
        {
            float normalizedRadius = r / (float)radiusSubdivisions;
            float currentRadius = radius * normalizedRadius;

            for (int theta = 0; theta <= thetaSubdivisions; theta++)
            {
                float normalizedTheta = theta / (float)thetaSubdivisions;
                float thetaAngle = normalizedTheta * 2 * Mathf.PI;

                for (int elevation = 0; elevation <= elevationSubdivisions; elevation++)
                {
                    float normalizedElevation = elevation / (float)elevationSubdivisions;
                    float elevationAngle = normalizedElevation * 2 * Mathf.PI;

                    

                    float a = currentRadius * Mathf.Cos(elevationAngle);
                    float x = a * Mathf.Cos(thetaAngle);
                    float y = currentRadius * Mathf.Sin(elevationAngle);
                    float z = a * Mathf.Sin(thetaAngle);
                    //Use below and uncomment above to use polar only
                    // sphericalCoordinates[index] = new Vector3(thetaAngle, elevationAngle, currentRadius);
                    sphericalCoordinates[index] = new Vector3(x, y, z);
                    index++;
                }
            }
        }


        Debug.LogWarning("Spherical Coords");
        Debug.LogWarning($"[{string.Join(",", sphericalCoordinates)}]");
        return sphericalCoordinates;
    }

    // setSpherePositions uses the coords parameter to set the positions of the spheres in sphereObj
    void setSpherePositions(Vector3[] coords, GameObject[] objArr)
    {
        Vector3 centerPosition = new Vector3(0, 0, 0);
        objArr[0].transform.rotation = Quaternion.identity;


        centerPosition = paintBoardCenterPosition;
        if (gamePlaymode == GamePlayMode.Left)
            centerPosition.x = -paintBoardCenterPosition.x;

        // spheresAllObj.transform.position = avatarShoulderCenterPos + new Vector3(0, 0.2f, 0.5f) + centerPosition;


        for (int i = 1; i < coords.Length; i++)
        {
        
            Vector3 spherePosition = coords[i];
            objArr[i].transform.position = objArr[0].transform.position + spherePosition;
            objArr[i].GetComponent<Renderer>().material = sphereMaterial;
                    //             Color currentColor = sphereObj[i].GetComponent<Renderer>().material.color;
                    // currentColor.a = 130f;
                    // sphereObj[i].GetComponent<Renderer>().material.color = currentColor;
            objArr[i].GetComponent<SphereData>().sphereCoords = spherePosition;
        }
    }

    private void handleSetSpherePositions() {
        if (gamePlaymode == GamePlayMode.Left || gamePlaymode == GamePlayMode.Bilateral ) {
            setSpherePosition(cartesianSphereCoords, leftSphereObjs); 
        }
        if (gamePlaymode == GamePlayMode.Right || gamePlaymode == GamePlayMode.Bilateral ) {
            setSpherePosition(cartesianSphereCoords, rightSphereObjs); 
        } 
    }

    private void resetGame()
    {
        Debug.LogWarning("resetgame start"); 
        //gameLevel = 3;
        //setGameLevel();
        gameRuningTime = 0;
        timeoutPlay = 0;
        fPreviousTime = Time.time;
        bGameRunning = true;
        i_gameResult_touched_count = 0;
        f_gameResult_touched_percent = 0;
        f_gameResult_progress = 0;
        f_gameResult_trajectory_distance_left = 0;
        f_gameResult_trajectory_distance_right = 0;
        touched_sphere_coords_left.Clear();
        touched_sphere_polar_coords_left.Clear();
        touched_sphere_coords_right.Clear();
        touched_sphere_polar_coords_right.Clear();
        //pre_hand_pos_left = avatarData.GetAvatarLeftHandPosition();
        //pre_hand_pos_right = avatarData.GetAvatarRightHandPosition();
        pre_hand_pos_left = hand_l.transform.position;
        pre_hand_pos_right = hand_r.transform.position;
        shpereIndex = 0;
        flowerRotatedCnt = 0;
        flowerRotateIndex = 0;
        Debug.LogWarning("resetgame start2"); 

        
        Debug.LogWarning("About to run generation in reset function");
        // Kill children if deactivated
            for(int i = leftOriginSphere.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(leftOriginSphere.transform.GetChild(i).gameObject);
                }
            for(int i = rightOriginSphere.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(rightOriginSphere.transform.GetChild(i).gameObject);
                }
         if (leftArmSphereActive ) {
           Debug.LogWarning("Generating left"); 
        createSphereWall(ref leftSphereObjs, numCoordsInSphere, leftOriginSphere);
        }

        if (rightArmSphereActive ) {
            Debug.LogWarning("Generating right"); 
        createSphereWall(ref rightSphereObjs, numCoordsInSphere, rightOriginSphere);
        // Set sphere position should apply coord vector to object vector
        }
        Debug.LogWarning("Just ran generation in reset function");

        rotateFlower();
        Debug.LogWarning("resetgame start3"); 

        txtGameFinished.text = "";
        //if(sphereObjMirror != null)
        //   Destroy(sphereObjMirror);
        //if(gamePlaymode == GamePlayMode.Bilateral)
        //    i_gameResult_total_balls = 2 * max_gameObjNum * maxRotateIndex;
        //else
        //    i_gameResult_total_balls = max_gameObjNum * maxRotateIndex;
        if (DataOculus != null)
            DataOculus.StopRecordData();
        

        
       

        
        
    }
    public void resetGames()
    {
        //flowerRotateIndex = 0;
        if (deviceConfig.hardwareType == ControllerHardwareType.OculusTouch)
        {
            avatarShoulderCenterPos = body_JNT.transform.position + avatarShoulder_body_JNT_Offset;
        }
        else
        {
        }
        resetGame();
        buttonClickSound();
    }
    public void levelUp()
    {
        gameLevel++;
        if (gameLevel > max_gameLevel)
            gameLevel = max_gameLevel;
        OpenLevelFile(gameLevel);
        setGameLevel();
        buttonClickSound();
    }
    public void levelDown()
    {
        gameLevel--;
        if (gameLevel < 1)
            gameLevel = 1;
        OpenLevelFile(gameLevel);
        setGameLevel();
        buttonClickSound();
    }

    public void branchUp()
    {
        flowerRotateIndex++;
        if (flowerRotateIndex >= maxRotateIndex)
            flowerRotateIndex = 0;
        rotateFlower();
        timeoutPlay = 0;
        buttonClickSound();
    }
    public void branchDown()
    {
        flowerRotateIndex--;
        if (flowerRotateIndex < 0)
            flowerRotateIndex = maxRotateIndex - 1;
        rotateFlower();
        timeoutPlay = 0;
        buttonClickSound();
    }
    public void setGameLevel()
    {
        resetGame();
        //radius_touch = (float)(max_gameLevel - gameLevel) / (float)max_gameLevel / 2.0f + 0.1f;
        //sphereScale *= ((float)(max_gameLevel - gameLevel) / (float)max_gameLevel + 1.0f);
        //setShpereScale();
        //if (sphereObjMirror != null)
        //    Destroy(sphereObjMirror);
        handleSetSpherePositions();
        //if(gamePlaymode == GamePlayMode.Bilateral)
        //    mirror_obj();
    }

    public void setPlayModeLeft()
    {
        // Debug.LogWarning("PLAYMODE LEFT");
        gamePlaymode = GamePlayMode.Left;
        leftArmSphereActive = true;
        rightArmSphereActive = false;
        resetGame();
        buttonClickSound();
    }
    public void setPlayModeRight()
    {
        gamePlaymode = GamePlayMode.Right;
        leftArmSphereActive = false;
        rightArmSphereActive = true;
        resetGame();
        buttonClickSound();
    }
    public void setPlayModeBilateral()
    {
        gamePlaymode = GamePlayMode.Bilateral;
        leftArmSphereActive = true;
        rightArmSphereActive = true;
        resetGame();
        //mirror_obj();
        buttonClickSound();
    }
    public void skipGametoNext()
    {
        gotoNextGamePlay();
    }
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
        Debug.LogWarning("initUIButtons"); 
        GameObject gameGUIObj = GameObject.Find("TherapistGUI");
        if (gameGUIObj == null) {
         Debug.LogWarning("Could not find GUI Object");
            return;
        }
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
            if (gameButtonObj != null)
                gameButtonObj.GetComponent<Button>().onClick.AddListener(branchUp);
            gameButtonObj = getChildGameObject(gameGUIObj1, "Button_BranchDown");
            if (gameButtonObj != null)
                gameButtonObj.GetComponent<Button>().onClick.AddListener(branchDown);
            gameButtonObj = getChildGameObject(gameGUIObj1, "Dropdown_ViewSelect");
            if (gameButtonObj != null)
                gameButtonObj.GetComponent<Dropdown>().onValueChanged.AddListener(OnViewSelectChanged);
            // gameButtonObj = getChildGameObject(gameGUIObj1, "Button_PaintMode");
            // if (gameButtonObj != null)
            //     gameButtonObj.GetComponent<Button>().onClick.AddListener(swapPaintMode);
        }
    }

    // private void swapPaintMode() {
    //     paintingModeOn = !paintingModeOn;
    // }

    public void OnViewSelectChanged(int index) {
        setCameraView(index);
    }

    public void setCameraView(int index) {
        Vector3 posCamera = new Vector3(.0f, 1.72f, -1.436f);
        Vector3 rotCamera = new Vector3(10.0f, 0.0f, 0.0f); ;
        Vector3 avatarPos = new Vector3(0, 0, 0);
        avatarPos = transform.position;

        switch(index)
        {
            case 0:
                posCamera = new Vector3(0.0f, 1.72f, -2f);
                rotCamera = new Vector3(10.0f, 0.0f, 0.0f);
                break;
            case 1:
              posCamera = new Vector3(2.0f, 1.5f, 0.0f);
                rotCamera = new Vector3(10.0f, -90.0f, 0.0f);
                break;
            case 2:
                posCamera = new Vector3(-2.0f, 1.5f, 0.0f);
                rotCamera = new Vector3(10.0f, 90.0f, 0.0f);
                break;
            case 3:
                posCamera = new Vector3(0.0f, 3.5f, 0.0f);
                rotCamera = new Vector3(80.0f, 0.0f, 0.0f);
                break;
            case 4:
                posCamera = new Vector3(0.0f, 1.72f, 2f);
                rotCamera = new Vector3(10.0f, 180.0f, 0.0f);
                break;
        }
        // Debug.LogWarning("Setting Position to: " + (avatarPos + posCamera).ToString());
        sceneCamera.transform.position = avatarPos + posCamera;
        sceneCamera.transform.eulerAngles = rotCamera;
    }


//adjust game position
//slider value from 0 to 100, middle is 50, output range is +-0.5m
public void moveDialPad_left_right(float value)
{
        paintBoardCenterPosition.x = (value - 50.0f) / 100.0f + 0.3f;
        handleSetSpherePositions();
}
public void moveDialPad_back_forth(float value)
{
        paintBoardCenterPosition.z = (value - 50.0f) / 100.0f;
    handleSetSpherePositions();
}
public void moveDialPad_up_down(float value)
{
        paintBoardCenterPosition.y = (value - 50.0f) / 100.0f;
    handleSetSpherePositions();
}
    public void changeBoardSize_big_small(float value)
    {
        float boardSizeRatio = (value) / 100.0f;
        if (boardSizeRatio < 0.1f) boardSizeRatio = 0.1f;
        else if (boardSizeRatio > 0.8f) boardSizeRatio = 0.8f;
        globalScale = boardSizeRatio;
        handleSetSpherePositions();

    }
    void SaveDefaultLevelFile(int level)
    {
        switch (level)
        {
            case 1:
                globalScale = 0.25f;
                paintBoardCenterPosition = new Vector3(0.2f, -0.2f, 0);
                break;
            case 2:
                globalScale = 0.35f;
                paintBoardCenterPosition = new Vector3(0.2f, -0.1f, 0);
                break;
            case 3:
                globalScale = 0.50f;
                paintBoardCenterPosition = new Vector3(0.2f, -0.1f, 0);
                break;
            case 4:
                globalScale = 0.65f;
                paintBoardCenterPosition = new Vector3(0.2f, 0f, 0);
                break;
            case 5:
                globalScale = 0.65f;
                paintBoardCenterPosition = new Vector3(0.2f, 0f, 0);
                break;
            default:
                globalScale = 0.65f;
                paintBoardCenterPosition = new Vector3(0.2f, 0f, 0);
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
        //setShperePosition(flowerRotateIndex);
        //setGameLevel();
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
        if (sCsvParts.Length == 4)
        {
            float xx, yy, zz;
            float a1;
            int index = 0;
            float.TryParse(sCsvParts[index++], out xx);
            float.TryParse(sCsvParts[index++], out yy);
            float.TryParse(sCsvParts[index++], out zz);
            float.TryParse(sCsvParts[index++], out a1);
                paintBoardCenterPosition.x = xx;
                paintBoardCenterPosition.y = yy;
                paintBoardCenterPosition.z = zz;
            globalScale = a1;
        }
        else//wrong file, creat default file
        {
            fileReader.Close();
            SaveDefaultLevelFile(level);
        }
    }
    fileReader.Close();
    gameLevel = level;
    //setShperePosition(flowerRotateIndex);
    //setGameLevel();
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
        writer.WriteLine(paintBoardCenterPosition.x + "," + paintBoardCenterPosition.y + "," + paintBoardCenterPosition.z + "," + globalScale);
        writer.WriteLine("pos.x" + "," + "pos.y" + "," + "pos.z" + "," + "scale");
    }
}

public void SaveLevel()
{
    SaveLevelFile(gameLevel);
}

public void LoadLevelDefault()
{
    OpenLevelFile(2);
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
        globalScale = 0.35f;
        paintBoardCenterPosition = new Vector3(0.2f, -0.1f, 0);
        handleSetSpherePositions();
        setGameLevel();
        resetGames();
}
}
