//read, convert, and update sensor data to unity
//only need one script running
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SensorPlugin;
using UnityEditor;
using System.Runtime.InteropServices;

[CustomPropertyDrawer(typeof(Quaternion))]
public class QuaternionPropertyDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);

		// Find the quaternion components
		SerializedProperty xProp = property.FindPropertyRelative("x");
		SerializedProperty yProp = property.FindPropertyRelative("y");
		SerializedProperty zProp = property.FindPropertyRelative("z");
		SerializedProperty wProp = property.FindPropertyRelative("w");

		// Calculate the width for each component field
		float componentWidth = position.width / 4f;

		// Draw the quaternion components side by side
		Rect componentRect = new Rect(position.x, position.y, componentWidth, position.height);
		EditorGUI.PropertyField(componentRect, xProp, GUIContent.none);
		componentRect.x += componentWidth;
		EditorGUI.PropertyField(componentRect, yProp, GUIContent.none);
		componentRect.x += componentWidth;
		EditorGUI.PropertyField(componentRect, zProp, GUIContent.none);
		componentRect.x += componentWidth;
		EditorGUI.PropertyField(componentRect, wProp, GUIContent.none);

		EditorGUI.EndProperty();
	}
}



public class MotionSensorManager : MonoBehaviour
{
	Vector4 cumalativeQuaternionComponents = new Vector4();
	CSensorData sensor_data = new CSensorData();
	const int max_sensor_num = 20;
	const int POSE_NUM = 10;
	Quaternion[] quatRawSensor = new Quaternion[max_sensor_num];
	public Quaternion[] quatSensor = new Quaternion[max_sensor_num];
	//Quaternion[] quatSensor_offset = new Quaternion[max_sensor_num];
    public Quaternion[] quatSensorAveragedOffset = new Quaternion[max_sensor_num];
	Quaternion[][] quatSensorPoseOffset = new Quaternion[POSE_NUM][];
	Quaternion[][] quatSensorPoseOffsetAdjusted = new Quaternion[POSE_NUM][];
	//public Quaternion[] viewFirstQSPAO = new Quaternion[max_sensor_num];


	bool[] poseCalibrationInitialized = new bool[POSE_NUM];
	bool[] poseCalibrationDisabled = new bool[POSE_NUM];

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
			quatSensorPoseOffsetAdjusted[i] = new Quaternion[max_sensor_num];
		}
		//viewFirstQSPAO = quatSensorPoseOffsetAdjusted[0];
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
			//quatSensorPoseOffset[0][i] = sensorRotToUnityRot(quatRawSensor[i]);
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
		Debug.LogError("TPOSE SENSOR 0 ADJUSTED IS " + quatSensorPoseOffsetAdjusted[0][0]);
		Debug.LogError("savePoseOffset: " + pose);
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
			quatSensorPoseOffset[pose][i] = sensorRotToUnityRot(quatRawSensor[i]);
			//viewFirstQSPO[i] = quatSensorPoseOffset[0][i];
		}

		// Marked as initialized
		poseCalibrationInitialized[pose] = true;

		// Calculate new average

		// Start the average at T pose which was set originally
		//Array.Copy(quatSensorPoseOffset[AvatarSensor.TPOSE], quatSensorAveragedOffset, max_sensor_num);
		//Debug.LogError("quatSensorAveragedOffset updated by savePoseOffset");
		//quatSensorAveragedOffset = quatSensorPoseOffset[AvatarSensor.TPOSE];

		//// Convert Pose offset to TPose
        ///// COMENTED OUT AVERAGING FOR TESTING
		Vector3 cw = new Vector3(0, 0, -90);
		Vector3 ccw = new Vector3(0, 0, 90);
		switch (pose)
		{
			case AvatarSensor.TPOSE:
				quatSensorPoseOffsetAdjusted[pose] = quatSensorPoseOffset[pose];
				break;
			case AvatarSensor.NPOSE:
				quatSensorPoseOffsetAdjusted[pose] = rotateAllSensors(quatSensorPoseOffset[pose], new Vector3[] { ccw, ccw, ccw, cw, cw, cw, v0, v0, v0, v0, v0, v0, v0, v0, v0, v0, v0, v0, v0, v0 });
				break;
			case AvatarSensor.NPPOSE:
				quatSensorPoseOffsetAdjusted[pose] = rotateAllSensors(quatSensorPoseOffset[pose], new Vector3[] { ccw, ccw, ccw, cw, cw, cw, v0, v0, v0, v0, v0, v0, v0, v0, v0, v0, v0, v0, v0, v0 });
				break;
			default:
				break;
		}
		Debug.LogError("quatsensorposeoffset adjusted in savepose for pose " + pose + ", offset just before avg: " + quatSensorPoseOffsetAdjusted[pose][0]);
		calculateAverageOffset();

	}

    void calculateAverageOffset()
    {
		/*Debug.LogError("avgoffset");
		Vector3[][] vectors = new Vector3[POSE_NUM][];
		Quaternion[][] quaternions = new Quaternion[POSE_NUM][];
		Vector3[] averageVectors = new Vector3[max_sensor_num];

		// Initialize 2D lists
		for (int i = 0; i < POSE_NUM; i++)
		{
			vectors[i] = new Vector3[max_sensor_num];
			quaternions[i] = new Quaternion[max_sensor_num];
			// Copy quatSensorPoseOffsetAdjusted to quaternions
			for (int j = 0; j < max_sensor_num; j++)
            {
				//if (quaternions[i][j]==null) { Debug.LogError("Quaternions null"); } else { Debug.LogError("not null" + i + " " + j + " " + quaternions[i][j]); }

				quaternions[i][j] = quatSensorPoseOffsetAdjusted[i][j];
            }
		}
		// Iterate through all, transform into t pose based on pose

		// Convert formats to prepare for averaging
		for (int i = 0; i < POSE_NUM; i++)
        {
            for(int j = 0; j < max_sensor_num; j++)
            {
				// Step 1: normalize all input quaternions
				quaternions[i][j] = Quaternion.Normalize(quaternions[i][j]);
				// Step 2: convert each quaternion to 3D vector representation
				vectors[i][j] = new Vector3(quaternions[i][j].x, quaternions[i][j].y, quaternions[i][j].z);
			}

			// NOTE SHOULD BE ABLE TO COMBINE THESE TWO 
		}*/

		// Get an average value for each sensor
		for (int i = 0; i < max_sensor_num; i++) {
			List<Quaternion> quatsToAvg = new List<Quaternion>();
			// Skip all values that have not been initialized or are disabled
			for (int j = 0; j < POSE_NUM; j++)
			{
				if (!poseCalibrationInitialized[j] || poseCalibrationDisabled[j] ) { continue; }
				quatsToAvg.Add(quatSensorPoseOffsetAdjusted[j][i]);
				if (i <= 5) { Debug.LogError("Pose " + j + " sensor " + i + "offsetadjusted: " + quatSensorPoseOffsetAdjusted[j][i]); }

			}

			quatSensorAveragedOffset[i] = AverageQuaternion(quatsToAvg.ToArray());
			//Test prints
			if (i <= 5)
			{
				string quaternionArrayString = string.Join(", ", System.Array.ConvertAll(quatsToAvg.ToArray(), q => q.ToString()));
				Debug.LogError("Testing averaging of list: " + quaternionArrayString + " | RESULT: " + AverageQuaternion(quatsToAvg.ToArray()));
				
				Debug.LogError("updated averagedOffset sensor " + i + " to " + quatSensorAveragedOffset[i]);
			}

		}

		// The resulting averageQuaternion should now represent the average or mean quaternion of the input quaternions.



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
		//Array.Copy(quatSensorPoseOffset[AvatarSensor.TPOSE], quatSensorAveragedOffset, max_sensor_num);

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


	// AVERAGE QUATERNION CODE TAKEN FROM https://forum.unity.com/threads/average-quaternions.86898/
	public static Quaternion AverageQuaternion(Quaternion[] quats)
	{
		if (quats.Length == 0)
		{
			return Quaternion.identity;
		}

		Vector4 cumulative = new Vector4(0, 0, 0, 0);

		foreach (Quaternion quat in quats)
		{
			AverageQuaternion_Internal(ref cumulative, quat, quats[0]);
		}

		float addDet = 1f / (float)quats.Length;
		float x = cumulative.x * addDet;
		float y = cumulative.y * addDet;
		float z = cumulative.z * addDet;
		float w = cumulative.w * addDet;
		//note: if speed is an issue, you can skip the normalization step
		return NormalizeQuaternion(new Quaternion(x, y, z, w));
	}

	//Get an average (mean) from more then two quaternions (with two, slerp would be used).
	//Note: this only works if all the quaternions are relatively close together.
	//Usage:
	//-Cumulative is an external Vector4 which holds all the added x y z and w components.
	//-newRotation is the next rotation to be added to the average pool
	//-firstRotation is the first quaternion of the array to be averaged
	//-addAmount holds the total amount of quaternions which are currently added
	static void AverageQuaternion_Internal(ref Vector4 cumulative, Quaternion newRotation, Quaternion firstRotation)
	{
		//Before we add the new rotation to the average (mean), we have to check whether the quaternion has to be inverted. Because
		//q and -q are the same rotation, but cannot be averaged, we have to make sure they are all the same.
		if (!AreQuaternionsClose(newRotation, firstRotation))
		{
			newRotation = InverseSignQuaternion(newRotation);
		}

		//Average the values
		cumulative.w += newRotation.w;
		cumulative.x += newRotation.x;
		cumulative.y += newRotation.y;
		cumulative.z += newRotation.z;
	}

	public static Quaternion NormalizeQuaternion(Quaternion quat)
	{
		float lengthD = 1.0f / Mathf.Sqrt(quat.w * quat.w + quat.x * quat.x + quat.y * quat.y + quat.z * quat.z);
		quat.x *= lengthD;
		quat.y *= lengthD;
		quat.z *= lengthD;
		quat.w *= lengthD;
		return quat;
	}

	//Changes the sign of the quaternion components. This is not the same as the inverse.
	public static Quaternion InverseSignQuaternion(Quaternion q)
	{
		return new Quaternion(-q.x, -q.y, -q.z, -q.w);
	}

	//Returns true if the two input quaternions are close to each other. This can
	//be used to check whether or not one of two quaternions which are supposed to
	//be very similar but has its component signs reversed (q has the same rotation as
	//-q)
	public static bool AreQuaternionsClose(Quaternion q1, Quaternion q2)
	{
		float dot = Quaternion.Dot(q1, q2);

		if (dot < 0.0f)
		{
			return false;
		}

		else
		{
			return true;
		}
	}
	// END EVERAGE QUATERNION CODE

}
	
