﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PIDController : MonoBehaviour {

	public GameObject carObj;
	public ICar car;
	public PathManager pm;

	float errA, errB;
	public float Kp = 10.0f;
	public float Kd = 10.0f;
	public float Ki = 0.1f;

	//Ks is the proportion of the current vel that
	//we use to sample ahead of the vehicles actual position.
	public float Kv = 1.0f; 

	//Ks is the proportion of the current err that
	//we use to change throtlle.
	public float Kt = 1.0f; 

	float diffErr = 0f;
	public float prevErr = 0f;
	public float steeringReq = 0.0f;
	public float throttleVal = 0.3f;
	public float totalError = 0f;
	public float absTotalError = 0f;
	public float totalAcc = 0f;
	public float totalOscilation = 0f;
	public float AccelErrFactor = 0.1f;
	public float OscilErrFactor = 10f;

	public delegate void OnEndOfPathCB();

	public OnEndOfPathCB endOfPathCB;

	bool isDriving = false;
	public bool waitForStill = true;

	public bool startOnWake = false;

	public bool brakeOnEnd = true;

	public bool doDrive = true;
	public float maxSpeed = 5.0f;

	public Text pid_steering;

	void Awake()
	{
		car = carObj.GetComponent<ICar>();
	}

	void Start()
	{
		if(startOnWake)
			StartDriving();
	}

	public void StartDriving()
	{
		if(!pm.isActiveAndEnabled || pm.path == null)
			return;

		steeringReq = 0f;
		prevErr = 0f;
		totalError = 0f;
		totalAcc = 0f;
		totalOscilation = 0f;
		absTotalError = 0f;


		pm.path.ResetActiveSpan();
		isDriving = true;
		waitForStill = false;//true;

		if(!waitForStill && doDrive)
			car.RequestThrottle(throttleVal);

		car.RestorePosRot();
	}
		
	// Update is called once per frame
	void Update () 
	{
		if(!pm.isActiveAndEnabled)
			return;

		if(!isDriving)
			return;

		if(waitForStill)
		{
			car.RequestFootBrake(1.0f);

			if(car.GetAccel().magnitude < 0.001f)
			{
				waitForStill = false;

				if(doDrive)
					car.RequestThrottle(throttleVal);
			}
			else
			{
				//don't continue until we've settled.
				return;
			}
		}

		//set the activity from the path node.
		PathNode n = pm.path.GetActiveNode();

		if(n != null && n.activity != null && n.activity.Length > 1)
		{
			car.SetActivity(n.activity);
		}
		else
		{
			car.SetActivity("image");
		}

		float err = 0.0f;

		float velMag = car.GetVelocity().magnitude;

		Vector3 samplePos = car.GetTransform().position + (car.GetTransform().forward * velMag * Kv);

		if(!pm.path.GetCrossTrackErr(samplePos, ref err))
		{
			if(brakeOnEnd)
			{
				car.RequestFootBrake(1.0f);

				if(car.GetAccel().magnitude < 0.0001f)
				{
					isDriving = false;

					if(endOfPathCB != null)
						endOfPathCB.Invoke();
				}
			}
			else
			{
				isDriving = false;
				
				if(endOfPathCB != null)
					endOfPathCB.Invoke();
			}

			return;
		}

		diffErr = err - prevErr;

		steeringReq = (-Kp * err) - (Kd * diffErr) - (Ki * totalError);

		if(doDrive)
			car.RequestSteering(steeringReq);

		if(doDrive)
		{
			if(car.GetVelocity().magnitude < maxSpeed)
				car.RequestThrottle(throttleVal);
			else
				car.RequestThrottle(0.0f);
		}
		
		if(pid_steering != null)
			pid_steering.text = string.Format("PID: {0} {1}", steeringReq, car.GetActivity());

		//accumulate total error
		totalError += err;

		//save err for next iteration.
		prevErr = err;

		float carPosErr = 0.0f;

		//accumulate error at car, not steering decision point.
		pm.path.GetCrossTrackErr(car.GetTransform().position, ref carPosErr);


		//now get a measure of overall fitness.
		//we don't with this to cancel out when it oscilates.
		absTotalError += Mathf.Abs(carPosErr) + 
		                 AccelErrFactor * car.GetAccel().magnitude;

	}
}
