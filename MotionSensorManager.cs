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
    Quaternion[] quatRawSensor = new Quaternion[max_sensor_num];
    Quaternion[] quatSensor = new Quaternion[max_sensor_num];
    Quaternion[] quatSensor_offset = new Quaternion[max_sensor_num];
    public float sensorBodyRotation = 0;
    public int count = 0;
    static MotionSensorManager instance = null;
    public bool bAutoBodyRotation = true;
    // Use this for initialization
    void Start()
    {
        initMotionSensor();
    }
    void Awake()
    {
        instance = this;
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
        float[] mydata = new float[max_sensor_num * 4]; 
        sensor_data.GetMotionSensorData(ref mydata);
        for (int i = 0; i < max_sensor_num; i++)
        {
            int dataIndex = 4 * i;
            quatRawSensor[i] = new Quaternion(mydata[dataIndex + 1], mydata[dataIndex + 2], mydata[dataIndex + 3], mydata[dataIndex + 0]);
            quatSensor[i] = sensorRotToUnityRot(quatRawSensor[i]);
        }
        loadMotionSensorOffset();
        if (bAutoBodyRotation)
            sensor_data.GetMotionSensorBodyRotation(ref sensorBodyRotation);
        Int32[] status = new Int32[max_sensor_num];
        sensor_data.GetMotionSensorStatus(ref status);
    }
    void loadMotionSensorOffset()
    {
        float[] mydata = new float[max_sensor_num * 4];
        sensor_data.GetMotionSensorOffset(ref mydata);
        for (int i = 0; i < max_sensor_num; i++)
        {
            int dataIndex = 4 * i;
            Quaternion QuatRawOffset = new Quaternion(mydata[dataIndex + 1], mydata[dataIndex + 2], mydata[dataIndex + 3], mydata[dataIndex + 0]);
            quatSensor_offset[i] = sensorRotToUnityRot(QuatRawOffset);
        }
    }

    public void resetSensorPose()
    {
        sensor_data.ResetMotionSensorOffset();
    }

    void initMotionSensor()
    {
        for (int i = 0; i < max_sensor_num; i++)
        {
            quatSensor[i] = Quaternion.identity;
            quatSensor_offset[i] = Quaternion.identity;
        }
        sensor_data.OpenMotionSensorData();
        loadMotionSensorOffset();

    }
    public Quaternion getSensorRotation(int sensorID)
    {
        Quaternion quatRot = Quaternion.identity;
        //Quaternion quatRot = quatSensor[sensorID] * Quaternion.Inverse(Quaternion.AngleAxis(sensorBodyRotation, Vector3.up) * quatSensor_offset[sensorID]);
        quatRot = Quaternion.AngleAxis(sensorBodyRotation, Vector3.up) * quatSensor[sensorID] * Quaternion.Inverse(Quaternion.AngleAxis(sensorBodyRotation, Vector3.up) * quatSensor_offset[sensorID]);
        return quatRot;
    }
}
