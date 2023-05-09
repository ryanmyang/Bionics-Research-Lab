//read, convert, and update sensor data to unity
//only need one script running
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SensorPlugin;

using System.Runtime.InteropServices;

public class MotionSensorManager : MonoBehaviour
{
	CSensorData sensor_data = new CSensorData();
	const int max_sensor_num = 20;
	const int POSE_NUM = 10;
	Quaternion[] quatRawSensor = new Quaternion[max_sensor_num];
	public Quaternion[] quatSensor = new Quaternion[max_sensor_num];
	//Quaternion[] quatSensor_offset = new Quaternion[max_sensor_num];
    public Quaternion[] quatSensorAveragedOffset = new Quaternion[max_sensor_num];
	Quaternion[][] quatSensorPoseOffset = new Quaternion[POSE_NUM][];
	//public Quaternion[] viewFirstQSPO = new Quaternion[max_sensor_num];
	Quaternion[][] quatSensorPoseOffsetAdjusted = new Quaternion[POSE_NUM][];

	bool[] poseOffsetInitialized = new bool[POSE_NUM];
	public float sensorBodyRotation = 0;
	public int count = 0;
    Vector3 v0 = Vector3.zero;
	static MotionSensorManager instance = null;
	public bool bAutoBodyRotation = true;
	// Use this for initialization
	void Start()
	{
		initMotionSensor();
		quatSensorPoseOffset[0][0] = Quaternion.Euler(10,10,10);
	}
	void Awake()
	{
		instance = this;
		for (int i = 0; i < POSE_NUM; i++)
		{
			quatSensorPoseOffset[i] = new Quaternion[max_sensor_num];
		}
	}
	public static MotionSensorManager Instance
	{
		get
		{
			return instance;
		}
	}
	//void Update()
	void FixedUpdate()
	{
		
		count++;
		getMotionSensorData();
		Debug.Log(quatSensor[0]);

	}
	Quaternion sensorRotToUnityRot(Quaternion rawSensorRotation)
	{
		Quaternion unitySensorRotation = Quaternion.identity;
		if ((Math.Abs(rawSensorRotation.x) + Math.Abs(rawSensorRotation.y) + Math.Abs(rawSensorRotation.z) + Math.Abs(rawSensorRotation.w)) > 0)
		{
			unitySensorRotation = new Quaternion(rawSensorRotation.y, -rawSensorRotation.z, -rawSensorRotation.x, rawSensorRotation.w);//ok 0 0 0
		}
		else
		{
			unitySensorRotation = Quaternion.identity;
		}
		return unitySensorRotation;
	}
	void getMotionSensorData()
	{
		Debug.Log("getMotionSensorData");
		float[] mydata = new float[max_sensor_num * 4];
		sensor_data.GetMotionSensorData(ref mydata);
		Debug.Log("mydata is " + mydata[1]);
		for (int i = 0; i < max_sensor_num; i++)
		{
			int dataIndex = 4 * i;
			quatRawSensor[i] = new Quaternion(mydata[dataIndex + 1], mydata[dataIndex + 2], mydata[dataIndex + 3], mydata[dataIndex + 0]);
			quatSensor[i] = sensorRotToUnityRot(quatRawSensor[i]);
			//test
			quatSensorPoseOffset[0][i] = sensorRotToUnityRot(quatRawSensor[i]);
			//viewFirstQSPO[i] = quatSensorPoseOffset[0][i];
			Debug.Log(quatSensor[0]);
		}

		if (bAutoBodyRotation)
			sensor_data.GetMotionSensorBodyRotation(ref sensorBodyRotation);
		Int32[] status = new Int32[max_sensor_num];
		sensor_data.GetMotionSensorStatus(ref status);
	}


	public void savePoseOffset(int pose)
	{
		Debug.LogError("savePoseOffset");
		if (instance== null)
        {
			Debug.LogError("No instance during savePoseOffset");
        }

		float[] mydata = new float[max_sensor_num * 4];
		sensor_data.GetMotionSensorData(ref mydata);

        // Collect raw live data and store into respective poseoffset for each sensor
		for (int i = 0; i < max_sensor_num; i++)
		{
			int dataIndex = 4 * i;
			quatRawSensor[i] = new Quaternion(mydata[dataIndex + 1], mydata[dataIndex + 2], mydata[dataIndex + 3], mydata[dataIndex + 0]);
			// Need to change 0s back to pose
			quatSensorPoseOffset[0][i] = sensorRotToUnityRot(quatRawSensor[i]);
			//viewFirstQSPO[i] = quatSensorPoseOffset[0][i];
		}

		// Marked as initialized
		poseOffsetInitialized[pose] = true;

		// Calculate new average

		// Start the average at T pose which was set originally
		Array.Copy(quatSensorPoseOffset[AvatarSensor.TPOSE], quatSensorAveragedOffset, max_sensor_num);
		Debug.LogError("quatSensorAveragedOffset updated by savePoseOffset");
		//quatSensorAveragedOffset = quatSensorPoseOffset[AvatarSensor.TPOSE];

		//// Convert Pose offset to TPose
        ///// COMENTED OUT AVERAGING FOR TESTING
		/*Vector3 cw = new Vector3(0, 0, -90);
		Vector3 ccw = new Vector3(0, 0, 90);
		switch (pose)
		{
			case AvatarSensor.TPOSE:
				quatSensorPoseOffsetAdjusted[pose] = quatSensorPoseOffset[pose];
				break;
			case AvatarSensor.NPOSE:
				quatSensorPoseOffsetAdjusted[pose] = rotateAllSensors(quatSensorPoseOffset[pose], new Vector3[] { cw, cw, cw, ccw, ccw, ccw, v0, v0, v0, v0, v0, v0, v0, v0, v0, v0, v0, v0, v0, v0 });
				break;
			case AvatarSensor.NPPOSE:
				quatSensorPoseOffsetAdjusted[pose] = rotateAllSensors(quatSensorPoseOffset[pose], new Vector3[] { ccw, ccw, ccw, cw, cw, cw, v0, v0, v0, v0, v0, v0, v0, v0, v0, v0, v0, v0, v0, v0 });
				break;
			default:
				break;
		}

		calculateAverageOffset();*/

	}

    void calculateAverageOffset()
    {
		Debug.LogError("avgoffset");
		Vector3[][] vectors = new Vector3[POSE_NUM][];
		Quaternion[][] quaternions = quatSensorPoseOffsetAdjusted;
		Vector3[] averageVectors = new Vector3[max_sensor_num];

		// Iterate through all, transform into t pose based on pose

		// Convert formats to prepare for averaging
		for (int i = 0; i < POSE_NUM; i++)
        {
            for(int j = 0; i < max_sensor_num; i++)
            {
				// Step 1: normalize all input quaternions
				quaternions[i][j] = Quaternion.Normalize(quaternions[i][j]);
				// Step 2: convert each quaternion to 3D vector representation
				vectors[i][j] = new Vector3(quaternions[i][j].x, quaternions[i][j].y, quaternions[i][j].z);
			}


		}

		// Iterate through each sensor and average
		for (int i = 0; i < max_sensor_num; i++) {
			// Step 3: calculate average vector
			Vector3 sum = Vector3.zero;
			int count = 0;
			for (int j = 0; j < POSE_NUM; j++)
			{
				if (!poseOffsetInitialized[j]) { continue; }
				count++;
				sum += vectors[j][i];

			}
			averageVectors[i] = sum / count;
			// Step 4: normalize average vector
			averageVectors[i].Normalize();
			// Step 5: convert average vector back to quaternion
			Quaternion averageQuaternion = new Quaternion(averageVectors[i].x, averageVectors[i].y, averageVectors[i].z, 0);
			averageQuaternion.w = Mathf.Sqrt(1 - averageQuaternion.x * averageQuaternion.x - averageQuaternion.y * averageQuaternion.y - averageQuaternion.z * averageQuaternion.z);
			quatSensorAveragedOffset[i] = averageQuaternion;
			Debug.LogError("averageoffset() updated averagedOffset");

		}

		// The resulting averageQuaternion should now represent the average or mean quaternion of the input quaternions.


		////// End untested gpt



	}

    // NOTE: ASSUMING MAX SENSORNUM MEANS THE COUNT, NOT THE MAX INDEX

        // This function takes in an array of quaternions and uses an array of vector3s (With its values representing angles, NOT x,y,z components like for an actual vector3) to adjust the quaternions
    Quaternion[] rotateAllSensors(Quaternion[] inputQuat, Vector3[] angles)
    {
        if (angles.Length != max_sensor_num)
        {
			throw new Exception("Incorrect number of angles provided to MotionSensorManager's rotateAllSensors function");
		}

		Quaternion[] result = new Quaternion[max_sensor_num];
        for(int i = 0; i < max_sensor_num; i++)
        {
			Quaternion currentRotation = inputQuat[i];  // get the current rotation of an object in the scene
			Quaternion rotationDelta = Quaternion.Euler(angles[i]);  // create a quaternion representing the desired rotation delta

			Quaternion newRotation = rotationDelta * currentRotation;  // rotate the current rotation by the desired rotation delta

			result[i] = newRotation;
		}

		return result;
        
	}


	//void loadMotionSensorOffset()
	//{
	//	float[] mydata = new float[max_sensor_num * 4];
	//	sensor_data.GetMotionSensorOffset(ref mydata);
	//	for (int i = 0; i < max_sensor_num; i++)
	//	{
	//    	int dataIndex = 4 * i;
	//    	Quaternion QuatRawOffset = new Quaternion(mydata[dataIndex + 1], mydata[dataIndex + 2], mydata[dataIndex + 3], mydata[dataIndex + 0]);
	//    	quatSensor_offset[i] = sensorRotToUnityRot(QuatRawOffset);
	//	}
	//}

	public void resetSensorPose()
	{
		sensor_data.ResetMotionSensorOffset();
	}

	void initMotionSensor()
	{
		Debug.LogError("initMotionSensor");
		for (int i = 0; i < max_sensor_num; i++)
		{
			quatSensor[i] = Quaternion.identity;
			if (instance == null)
			{
				Debug.LogError("No instance during savePoseOffset");
			}
			this.quatSensorPoseOffset[0][i] = Quaternion.identity;
		}
		sensor_data.OpenMotionSensorData();
        // Assume start is in tpose for initial tracking
		savePoseOffset(AvatarSensor.TPOSE);

		// Copy Tpose offset into averaged offset to start
		Array.Copy(quatSensorPoseOffset[AvatarSensor.TPOSE], quatSensorAveragedOffset, max_sensor_num);

		//quatSensorAveragedOffset = quatSensorPoseOffset[AvatarSensor.TPOSE];
		//Debug.LogError("quatSensorAveragedOffset updated by initMotionSensor");



	}
	public Quaternion getSensorRotation(int sensorID)
	{
		Quaternion quatRot = Quaternion.identity;
		//Quaternion quatRot = quatSensor[sensorID] * Quaternion.Inverse(Quaternion.AngleAxis(sensorBodyRotation, Vector3.up) * quatSensor_offset[sensorID]);
		quatRot = Quaternion.AngleAxis(sensorBodyRotation, Vector3.up) * quatSensor[sensorID] * Quaternion.Inverse(Quaternion.AngleAxis(sensorBodyRotation, Vector3.up) * quatSensorAveragedOffset[sensorID]);
		return quatRot;
	}
}
