using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Leap;

public class LeapManager : MonoBehaviour 
{
	// swipe directions enum
	public enum SwipeDirection
	{
		None,
		Right,
		Left,
		Up,
		Down,
		Front,
		Back
	}

	// Minimum time between gesture detections
	public float MinTimeBetweenGestures = 1f;
	
	// the debug camera, if available, tracking the fingers and hands
	public Camera DebugCamera = null;
	
	// the leap sensor prefab used to display the sensor in debug window
	public GameObject LeapSensorPrefab = null;
	
	// the line renderer prefab used to display fingers and hands in debug window
	public LineRenderer LineFingerPrefab = null;
	
	// true if finger and hand IDs and pos need to be displayed in Debug window, false otherwise
	public bool DisplayLeapData = false;
	
	// the central position for tracked fingers and hands to be displayed for the debug camera
	public Vector3 DisplayFingerPos = Vector3.zero;
	
	// scale used for displaying fingers and hands
	public float DisplayFingerScale = 5;
	
	// gui texture to be used as hand cursor
	public GUITexture handCursor;
	
	// cursor textures
	public Texture normalCursorTexture;
	public Texture selectCursorTexture;
	//public Texture touchCursorTexture;
	
	// if the leap cursor, grip and release should control mouse cursor too
	public bool controlMouseCursor = false;
	
	// gui text for debugging
	public GUIText debugText;
	
	// leap controller parameters
	private Leap.Controller leapController = null;
	private Leap.Frame leapFrame = null;
	private Int64 lastFrameID = 0;
	private Int64 leapFrameCounter = 0;
	
	// leap pointable parameters
	private Leap.Pointable leapPointable = null;
	private int leapPointableID = 0;
	private int leapPointableHandID;
	private Vector3 leapPointablePos;
	private Vector3 leapPointableDir;
	//private Quaternion leapPointableQuat;
	
	// leap hand parameters
	private Leap.Hand leapHand = null;
	private int leapHandID = 0;
	private Vector3 leapHandPos;
	private int leapHandFingersCount;
	private int fingersCountHandID;
	private float fingersCountFiltered;
	
	// hand grip and release parameters
	private bool handGripDetected = false;
	private bool handReleaseDetected = false;
	private int handGripFingersCount;
	//private Int64 handGripFrameCounter;
	
	// swipe parameters
	private Vector3 leapSwipeDir;
	private Vector3 leapSwipeSpeed;
	
	// gesture ID
	private int iCircleGestureID;
	private int iSwipeGestureID;
	private int iKeyTapGestureID;
	private int iScreenTapGestureID;
	
	// gesture progress
	private float fCircleProgress;
	private float fSwipeProgress;
	private float fKeyTapProgress;
	private float fScreenTapProgress;
	
	// Bool to keep track of whether LeapMotion has been initialized
	private bool leapInitialized = false;

	// cursor position, texture and touch status
	private Vector3 cursorNormalPos = Vector3.zero;
	private Vector3 cursorScreenPos = Vector3.zero;
//	private Pointable.Zone leapPointableZone = Pointable.Zone.ZONENONE;

	// general gesture tracking time start
	private float gestureTrackingAtTime;
	
	// The single instance of LeapManager
	private static LeapManager instance;
	
	private Dictionary<int, LineRenderer> dictFingerLines = new Dictionary<int, LineRenderer>();
	
	
	// returns the single LeapManager instance
    public static LeapManager Instance
    {
        get
        {
            return instance;
        }
    }
	
	// returns true if Leap-sensor is successfully initialized, false otherwise
	public bool IsLeapInitialized()
	{
		return leapInitialized;
	}
	
	// returns the current leap frame counter
	public Int64 GetLeapFrameCounter()
	{
		return leapFrameCounter;
	}
	
	// returns true if there is a valid pointable found, false otherwise
	public bool IsPointableValid()
	{
		return (leapPointable != null) && leapPointable.IsValid;
	}
	
	// returns the tracked leap pointable, or null if no pointable is being tracked
	public Leap.Pointable GetLeapPointable()
	{
		return leapPointable;
	}
	
	// returns the currently tracked pointable ID
	public int GetPointableID()
	{
		if((leapPointable != null) && leapPointable.IsValid)
			return leapPointableID;
		else
			return 0;
	}
	
	// returns the position of the currently tracked pointable
	public Vector3 GetPointablePos()
	{
		if((leapPointable != null) && leapPointable.IsValid)
			return leapPointablePos;
		else
			return Vector3.zero;
	}
	
	// returns the direction of the currently tracked pointable
	public Vector3 GetPointableDir()
	{
		if((leapPointable != null) && leapPointable.IsValid)
			return leapPointableDir;
		else
			return Vector3.zero;
	}
	
	// returns the 3D rotation of the currently tracked pointable
	public Quaternion GetPointableQuat()
	{
		if((leapPointable != null) && leapPointable.IsValid)
			//return leapPointableQuat;
			return Quaternion.LookRotation(leapPointableDir);
		else
			return Quaternion.identity;
	}
	
	// returns true if there is a valid hand found, false otherwise
	public bool IsHandValid()
	{
		return (leapHand != null) && leapHand.IsValid;
	}
	
	// returns tracked leap hand, or null if no hand is being tracked
	public Leap.Hand GetLeapHand()
	{
		return leapHand;
	}
	
	// returns the currently tracked hand ID
	public int GetHandID()
	{
		if((leapHand != null) && leapHand.IsValid)
			return leapHandID;
		else
			return 0;
	}
	
	// returns the position of the currently tracked hand
	public Vector3 GetHandPos()
	{
		if((leapHand != null) && leapHand.IsValid)
			return leapHandPos;
		else
			return Vector3.zero;
	}
	
	// returns the count of fingers of the tracked hand
	public int GetFingersCount()
	{
		//return (int)(fingersCountFiltered + 0.5f);
		return leapHandFingersCount;
	}

	// returns true if hand grip has been detected, false otherwise
	public bool IsHandGripDetected()
	{
		return handGripDetected;
	}
	
	// returns true if hand release has been detected, false otherwise
	public bool IsHandReleaseDetected()
	{
		return handReleaseDetected;
	}
	
	// returns true if gesture Circle has been detected, false otherwise
	public bool IsGestureCircleDetected()
	{
		bool bDetected = fCircleProgress >= 1f;
		
		if(bDetected)
		{
			//iCircleGestureID = 0;
			fCircleProgress = 0f;
		}
		
		return bDetected;
	}

	// returns the ID of the last Circle gesture
	public int GetGestureCircleID()
	{
		return iCircleGestureID;
	}
	
	// returns the progress of the Circle gesture
	public float GetGestureCircleProgress()
	{
		return fCircleProgress;
	}
	
	// returns true if gesture Swipe has been detected, false otherwise
	public bool IsGestureSwipeDetected()
	{
		bool bDetected = fSwipeProgress >= 1f;
		
		if(bDetected)
		{
			//iSwipeGestureID = 0;
			fSwipeProgress = 0f;
		}
		
		return bDetected;
	}
	
	// returns the ID of the last Swipe gesture
	public int GetGestureSwipeID()
	{
		return iSwipeGestureID;
	}
	
	// returns the progress of the Swipe gesture
	public float GetGestureSwipeProgress()
	{
		return fSwipeProgress;
	}
	
	// returns the last swipe direction vector
	public Vector3 GetSwipeDirVector()
	{
		return leapSwipeDir;
	}
	
	// converts gesture direction to SwipeDirection-value
	public SwipeDirection GetSwipeDirection()
	{
		if(Mathf.Abs(leapSwipeDir.x) > Mathf.Abs(leapSwipeDir.y))
		{
			// |x| > |y|
			if(Mathf.Abs(leapSwipeDir.x) > Mathf.Abs(leapSwipeDir.z))
			{
				// x
				return leapSwipeDir.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
			}
			else if(Mathf.Abs(leapSwipeDir.x) < Mathf.Abs(leapSwipeDir.z))
			{
				// z
				return leapSwipeDir.z > 0 ? SwipeDirection.Front : SwipeDirection.Back;
			}
		}
		else if(Mathf.Abs(leapSwipeDir.x) < Mathf.Abs(leapSwipeDir.y))
		{
			// |y| > |x|
			if(Mathf.Abs(leapSwipeDir.y) > Mathf.Abs(leapSwipeDir.z))
			{
				// y
				return leapSwipeDir.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
			}
			else if(Mathf.Abs(leapSwipeDir.y) < Mathf.Abs(leapSwipeDir.z))
			{
				// z
				return leapSwipeDir.z > 0 ? SwipeDirection.Front : SwipeDirection.Back;
			}
		}
		
		return SwipeDirection.None;
	}
	
//	// returns the last swipe speed
//	public Vector3 GetSwipeSpeed()
//	{
//		return leapSwipeSpeed;
//	}
	
	// returns true if gesture Key-tap has been detected, false otherwise
	public bool IsGestureKeytapDetected()
	{
		bool bDetected = fKeyTapProgress >= 1f;
		
		if(bDetected)
		{
			//iKeyTapGestureID = 0;
			fKeyTapProgress = 0f;
		}
		
		return bDetected;
	}
	
	// returns the ID of the last Keytap gesture
	public int GetGestureKeytapID()
	{
		return iKeyTapGestureID;
	}
	
	// returns the progress of the Keytap gesture
	public float GetGestureKeytapProgress()
	{
		return fKeyTapProgress;
	}
	
	// returns true if gesture Screen-tap has been detected, false otherwise
	public bool IsGestureScreentapDetected()
	{
		bool bDetected = fScreenTapProgress >= 1f;
		
		if(bDetected)
		{
			//iScreenTapGestureID = 0;
			fScreenTapProgress = 0f;
		}
		
		return bDetected;
	}
	
	// returns the ID of the last Screentap gesture
	public int GetGestureScreentapID()
	{
		return iScreenTapGestureID;
	}
	
	// returns the progress of the Screentap gesture
	public float GetGestureScreentapProgress()
	{
		return fScreenTapProgress;
	}
	
	// returns the cursor position in normalized coordinates
	public Vector3 GetCursorNormalizedPos()
	{
		if((leapHand != null) && leapHand.IsValid)
			return cursorNormalPos;
		else
			return Vector3.zero;
	}
	
	// returns the cursor position in screen coordinates
	public Vector3 GetCursorScreenPos()
	{
		if((leapHand != null) && leapHand.IsValid)
			return cursorScreenPos;
		else
			return Vector3.zero;
	}
	
//	// returns the touch status of the pointable
//	public Pointable.Zone GetPointableTouchStatus()
//	{
//		if((leapPointable != null) && leapPointable.IsValid)
//			return leapPointableZone;
//		else
//			return Pointable.Zone.ZONENONE;
//	}


	//----------------------------------- end of public functions --------------------------------------//
	
	void Awake()
	{
		//debugText = GameObject.Find("DebugText");
		//handCursor = GameObject.Find("HandCursor");
		
		if(LeapSensorPrefab)
		{
			Instantiate(LeapSensorPrefab, DisplayFingerPos, Quaternion.identity);
		}
		
		// ensure the needed dlls are in place
		if(CheckLibsPresence())
		{
			// reload the same level
			Application.LoadLevel(Application.loadedLevel);
		}
	}

	void Start()
	{
		try 
		{
			leapController = new Leap.Controller();
			
//			if(leapController.Devices.Count == 0)
//				throw new Exception("Please connect the LeapMotion sensor!");

			leapController.EnableGesture(Gesture.GestureType.TYPECIRCLE);
			leapController.EnableGesture(Gesture.GestureType.TYPEKEYTAP);
			leapController.EnableGesture(Gesture.GestureType.TYPESCREENTAP);
			leapController.EnableGesture(Gesture.GestureType.TYPESWIPE);
			
			instance = this;
			leapInitialized = true;
			
			DontDestroyOnLoad(gameObject);
			
			if(DebugCamera)
			{
				DontDestroyOnLoad(DebugCamera.gameObject);
			}
			
			// show the ready-message
			string sMessage = leapController.Devices.Count > 0 ? "Ready." : "Please make sure the Leap-sensor is connected.";
			Debug.Log(sMessage);
			
//			if(debugText != null)
//				debugText.guiText.text = sMessage;
		}
		catch(System.TypeInitializationException ex)
		{
			Debug.LogError(ex.ToString());
			if(debugText != null)
				debugText.guiText.text = "Please check the LeapMotion installation.";
		}
		catch (System.Exception ex) 
		{
			Debug.LogError(ex.ToString());
			if(debugText != null)
				debugText.guiText.text = ex.Message;
		}
	}
	
	void OnApplicationQuit()
	{
		leapPointable = null;
		leapFrame = null;
		
		if(leapController != null)
		{
			leapController.Dispose();
			leapController = null;
		}
		
		leapInitialized = false;
		instance = null;
	}
	
	void Update() 
	{
		if(leapInitialized && leapController != null)
		{
			Leap.Frame frame = leapController.Frame();
			
			if(frame.IsValid && (frame.Id != lastFrameID))
			{
				leapFrame = frame;
				lastFrameID = leapFrame.Id;
				leapFrameCounter++;
				
//				// fix unfinished leap gesture progress
//				if(fCircleProgress > 0f && fCircleProgress < 1f)
//					fCircleProgress = 0f;
//				if(fSwipeProgress > 0f && fSwipeProgress < 1f)
//					fSwipeProgress = 0f;
//				if(fKeyTapProgress > 0f && fKeyTapProgress < 1f)
//					fKeyTapProgress = 0f;
//				if(fScreenTapProgress > 0f && fScreenTapProgress < 1f)
//					fScreenTapProgress = 0f;
				
				// get the prime hand
				leapHand = leapFrame.Hand(leapHandID);
				if(leapHand == null || !leapHand.IsValid)
				{
					leapHand = GetFrontmostHand();
					leapHandID = leapHand != null && leapHand.IsValid ? leapHand.Id : 0;
				}
				
				// get the prime pointable
				leapPointable = leapFrame.Pointable(leapPointableID);
				if(leapPointable == null || !leapPointable.IsValid)
				{
					leapPointable = leapHand.IsValid ? GetPointingFigner(leapHand) : leapFrame.Pointables.Frontmost;

					leapPointableID = leapPointable != null && leapPointable.IsValid ? leapPointable.Id : 0;
					leapPointableHandID = leapPointable != null && leapPointable.IsValid && leapPointable.Hand.IsValid ? leapPointable.Hand.Id : 0;
				}

				if(leapPointable != null && leapPointable.IsValid && 
					leapPointable.Hand != null && leapPointable.Hand.IsValid &&
					leapHand != null && leapHand.IsValid && leapPointable.Hand.Id == leapHand.Id)
				{
					leapPointablePos = LeapToUnity(leapPointable.StabilizedTipPosition, true);
					leapPointableDir = LeapToUnity(leapPointable.Direction, false);
					//leapPointableQuat = Quaternion.LookRotation(leapPointableDir);
				}
				else 
				{
					leapPointable = null;

					leapPointableID = 0;
					leapPointableHandID = 0;
				}
					
				Leap.Vector stabilizedPosition = Leap.Vector.Zero;
				if(leapHandID != 0)
				{
					leapHandPos = LeapToUnity(leapHand.StabilizedPalmPosition, true);
					stabilizedPosition = leapHand.StabilizedPalmPosition;
					
					leapHandFingersCount = leapHand.Fingers.Count;
					
					bool bCurrentHandGrip = handGripDetected;
					handGripDetected = !isHandOpen(leapHand);
					handReleaseDetected = !handGripDetected;
					
					if(controlMouseCursor)
					{
						if(!bCurrentHandGrip && handGripDetected)
						{
							MouseControl.MouseDrag();
						}
						else if(bCurrentHandGrip && handReleaseDetected)
						{
							MouseControl.MouseRelease();
						}
					}
				}
				else
				{
					if(controlMouseCursor && handGripDetected)
					{
						MouseControl.MouseRelease();
					}
					
					leapHandFingersCount = 0;
					handGripDetected = false;
					handReleaseDetected = false;
				}
				
				// estimate the cursor coordinates
				if(stabilizedPosition.MagnitudeSquared != 0f)
				{
				    Leap.InteractionBox iBox = frame.InteractionBox;
				    Leap.Vector normalizedPosition = iBox.NormalizePoint(stabilizedPosition);
					
				    cursorNormalPos.x = normalizedPosition.x;
				    cursorNormalPos.y = normalizedPosition.y;
					cursorScreenPos.x = cursorNormalPos.x * UnityEngine.Screen.width;
					cursorScreenPos.y = cursorNormalPos.y * UnityEngine.Screen.height;
					
					if(controlMouseCursor)
					{
						MouseControl.MouseMove(cursorNormalPos);
					}
				}
				
				// Gesture analysis
				GestureList gestures = frame.Gestures ();
				for (int i = 0; i < gestures.Count; i++) 
				{
					Gesture gesture = gestures[i];
					
					if(Time.realtimeSinceStartup < gestureTrackingAtTime)
						continue;
					
					switch (gesture.Type) 
					{
						case Gesture.GestureType.TYPECIRCLE:
							CircleGesture circle = new CircleGesture(gesture);
						
							if(iCircleGestureID != circle.Id && 
								circle.State == Gesture.GestureState.STATESTOP)
							{
								iCircleGestureID = circle.Id;
								fCircleProgress = 1f;
							
								gestureTrackingAtTime = Time.realtimeSinceStartup + MinTimeBetweenGestures;
							}
							else if(circle.Progress < 1f)
							{
								fCircleProgress = circle.Progress;
							}
							break;
						
						case Gesture.GestureType.TYPESWIPE:
							SwipeGesture swipe = new SwipeGesture(gesture);
						
							if(iSwipeGestureID != swipe.Id &&
								swipe.State == Gesture.GestureState.STATESTOP)
							{
								iSwipeGestureID = swipe.Id;
								fSwipeProgress = 1f;  // swipe.Progress
							
								gestureTrackingAtTime = Time.realtimeSinceStartup + MinTimeBetweenGestures;
							
								leapSwipeDir = LeapToUnity(swipe.Direction, false);
								leapSwipeSpeed = LeapToUnity(swipe.Position - swipe.StartPosition, true);
							
								if(swipe.DurationSeconds != 0)
									leapSwipeSpeed /= swipe.DurationSeconds;
								else
									leapSwipeSpeed = Vector3.zero;
							}
							else if(swipe.State != Gesture.GestureState.STATESTOP)
							{
								fSwipeProgress = 0.5f;
							}
							break;
						
						case Gesture.GestureType.TYPEKEYTAP:
							KeyTapGesture keytap = new KeyTapGesture (gesture);
						
//							if(iKeyTapGestureID != keytap.Id && 
//								keytap.State == Gesture.GestureState.STATESTOP)
							{
								iKeyTapGestureID = keytap.Id;
								fKeyTapProgress = 1f;
							
								gestureTrackingAtTime = Time.realtimeSinceStartup + MinTimeBetweenGestures;
							}
//							else if(keytap.Progress < 1f)
//							{
//								fKeyTapProgress = keytap.Progress;
//							}
							break;
						
						case Gesture.GestureType.TYPESCREENTAP:
							ScreenTapGesture screentap = new ScreenTapGesture (gesture);
						
//							if(iScreenTapGestureID != screentap.Id && 
//								screentap.State == Gesture.GestureState.STATESTOP)
							{
								iScreenTapGestureID = screentap.Id;
								fScreenTapProgress = 1f;
							
								gestureTrackingAtTime = Time.realtimeSinceStartup + MinTimeBetweenGestures;
							}
//							else if(screentap.Progress < 1f)
//							{
//								fScreenTapProgress = screentap.Progress;
//							}
							break;
						
						default:
							Debug.LogError("Unknown gesture type.");
							break;
					}
				}

				if(DebugCamera)
				{
					DoDisplayFingers();
				}
			}
		}
	}
	
	
	void OnGUI()
	{	
		// display the cursor status and position
		if(handCursor != null)
		{
			Texture texture = null;
			
			if(handGripDetected)
			{
				texture = selectCursorTexture;
			}
			
			if(texture == null)
			{
				texture = normalCursorTexture;
			}
			
			if(texture != null)
			{
				handCursor.guiTexture.texture = texture;
				handCursor.transform.position = Vector3.Lerp(handCursor.transform.position, cursorNormalPos, 3 * Time.deltaTime);
			}
		}
		
		if(leapFrame != null && DebugCamera)
		{
			string sDebug = String.Empty;
			Rect rectDebugCamera = DebugCamera.pixelRect;
			Rect rectDebugFinger = new Rect(0, 0, 100, 100);
			
			if(DisplayLeapData)
			{
				// show finger Ids
				foreach(Pointable finger in leapFrame.Pointables)
				{
					if(finger.IsValid)
					{
//						if(finger.Id == leapPointableID)
//							sDebug = "<b>" + finger.Id.ToString() + "</b>";
//						else if((finger.Id == leapHandLFingerId) || (finger.Id == leapHandRFingerId))
//							sDebug = "<i>" + finger.Id.ToString() + "</i>";
//						else
							sDebug = finger.Id.ToString();
						
						Vector3 vFingerPos = LeapToUnity(finger.StabilizedTipPosition, true) * DisplayFingerScale + DisplayFingerPos;
						Vector3 vScreenPos = DebugCamera.WorldToScreenPoint(vFingerPos);
						//vScreenPos.y = UnityEngine.Screen.height - vScreenPos.y;
						
						rectDebugFinger.x = vScreenPos.x;
						rectDebugFinger.y = UnityEngine.Screen.height - vScreenPos.y; //vScreenPos.y;
						
						if(rectDebugCamera.Contains(vScreenPos))
							GUI.Label(rectDebugFinger, sDebug);
					}
				}
				
				// show hand Ids
				int primaryHandId = leapHand != null && leapHand.IsValid ? leapHand.Id : 0;
				
				foreach(Hand hand in leapFrame.Hands)
				{
					if(hand.IsValid)
					{
						if(hand.Id == primaryHandId)
							sDebug = "<b>" + hand.Id.ToString() + "</b>";
						else
							sDebug = hand.Id.ToString();
						
						Leap.Vector handBase = hand.StabilizedPalmPosition + (-hand.Direction * 100);
						Vector3 vHandPos = LeapToUnity(handBase, true) * DisplayFingerScale + DisplayFingerPos;
						Vector3 vScreenPos = DebugCamera.WorldToScreenPoint(vHandPos);
						//vScreenPos.y = UnityEngine.Screen.height - vScreenPos.y;
						
						rectDebugFinger.x = vScreenPos.x;
						rectDebugFinger.y = UnityEngine.Screen.height - vScreenPos.y; //vScreenPos.y;
						
						if(rectDebugCamera.Contains(vScreenPos))
							GUI.Label(rectDebugFinger, sDebug);
					}
				}
				
//				// show finger Ids
//				List<int> alFingerIds = new List<int>(dictFingerLines.Keys);
//				alFingerIds.Sort();
//				
//				foreach(int fingerId in alFingerIds)
//				{
//					if(fingerId == leapPointableID)
//						sDebug += "<b>" + fingerId.ToString() + "</b> ";
//					else
//						sDebug += fingerId.ToString() + " ";
//				}
			
				Rect rectCamera = DebugCamera.pixelRect;
				rectCamera.x += 10;
				rectCamera.y = UnityEngine.Screen.height - rectCamera.height + 10;
				
	//			GUI.Label(rectCamera, sDebug);
	//			alFingerIds.Clear();
				
				// show pointable and hand info
	//			rectCamera.y += 20;
				if(leapPointableID != 0)
				{
					sDebug = "Pointable " + leapPointableID + "/" + leapPointableHandID + ": " + leapPointablePos.ToString();
					GUI.Label(rectCamera, sDebug);
				}
	
				rectCamera.y += 20;
				if(leapHandID != 0)
				{
					sDebug = "Hand " + leapHandID + "/" + leapHandFingersCount + ": " + leapHandPos.ToString();
					GUI.Label(rectCamera, sDebug);
				}
				
				// show cursor coordinates
	//			rectCamera.y += 20;
	//			sDebug = "Cursor: " + cursorNormalPos;
	//			GUI.Label(rectCamera, sDebug);
	
				// show the grip/release status
				rectCamera.y += 20;
				if(leapFrameCounter != 0)
				{
					//sDebug = "Grip " + leapHandLFingerId + "/" + leapHandRFingerId + ": " + handGripDetected;
					sDebug = "Grip: " + handGripDetected;
					GUI.Label(rectCamera, sDebug);
				}
			}
			
		}
	}
	
	private void ShowCameraWindow(int windowID) 
	{
	}
	
	
	private void DoDisplayFingers()
	{
		if(leapFrame == null || !leapFrame.IsValid || !LineFingerPrefab) 
			return;
		
		List<int> alFingerIds = new List<int>();
		
		foreach(Pointable finger in leapFrame.Pointables)
		{
			if(finger.IsValid)
			{
				alFingerIds.Add(finger.Id);
				
				LineRenderer line = null;
				if(dictFingerLines.ContainsKey(finger.Id))
				{
					line = dictFingerLines[finger.Id];
				}
				
				if(line == null)
				{
					line = Instantiate(LineFingerPrefab) as LineRenderer;
					dictFingerLines[finger.Id] = line;
				}
				
				Leap.Vector fingerBase = finger.StabilizedTipPosition + (-finger.Direction * finger.Length);
				
				if(finger.Hand == null || !finger.Hand.IsValid)
					line.SetVertexCount(2);
				else
					line.SetVertexCount(4);
				
				line.SetPosition(0, LeapToUnity(finger.StabilizedTipPosition, true) * DisplayFingerScale + DisplayFingerPos);
				line.SetPosition(1, LeapToUnity(fingerBase, true) * DisplayFingerScale + DisplayFingerPos);
				
				if(finger.Hand != null && finger.Hand.IsValid)
				{
					Leap.Hand hand = finger.Hand;
					Leap.Vector handBase = hand.StabilizedPalmPosition + (-hand.Direction * 100);

					line.SetPosition(2, LeapToUnity(hand.StabilizedPalmPosition, true) * DisplayFingerScale + DisplayFingerPos);
					line.SetPosition(3, LeapToUnity(handBase, true) * DisplayFingerScale + DisplayFingerPos);
				}
			}
		}
		
		// cleapup fingers list
		List<int> alLostFingeIds = new List<int>();
		foreach(int fingerId in dictFingerLines.Keys)
		{
			if(!alFingerIds.Contains(fingerId))
			{
				alLostFingeIds.Add(fingerId);
			}
		}
		
		foreach(int fingerId in alLostFingeIds)
		{
			//Debug.Log("Destroying " + fingerId);
			
			try 
			{
				GameObject go = dictFingerLines[fingerId].gameObject;
				dictFingerLines.Remove(fingerId);
				
				Destroy(go);
			} 
			catch (Exception) 
			{
				// do nothing
			}
		}
		
		alFingerIds.Clear();
		alLostFingeIds.Clear();
	}
	
	
	// converts leap vector to unity vector
	private Vector3 LeapToUnity(Leap.Vector leapVector, bool bScaled)	
	{
		if(bScaled)
			return new Vector3(leapVector.x, leapVector.y, -leapVector.z) * .001f; 
		else
			return new Vector3(leapVector.x, leapVector.y, -leapVector.z); 
	}
	
	
	///////////////////////////////////////////////////////////////////////////////////////
	// The following functions are taken from: LeapMotion Controller Unity C# Boilerplate
	// Author: Daniel Plemmons
	///////////////////////////////////////////////////////////////////////////////////////
	

	/*
	 * Gets the frontmost detected hand in the scene. 
	 * Returns Leap.Hand.Invalid if no hands are being tracked.
	 */
	private Hand GetFrontmostHand()
	{
		float minZ = float.MaxValue;
		Hand forwardHand = Hand.Invalid;

		foreach(Hand hand in leapFrame.Hands)
		{
			if(hand.PalmPosition.z < minZ)
			{
				minZ = hand.PalmPosition.z;
				forwardHand = hand;
			}
		}

		return forwardHand;
	}
	
	
	/*
	 * Gets the most likely pointable to be pointing on the given hand. 
	 * Returns Finger.Invalid if no such finger exists.
	 */
	private Pointable GetPointingFigner(Hand hand)
	{
		Pointable forwardPointable = Pointable.Invalid;
		List<Pointable> forwardPointables = forwardFacingPointables(hand);
		
		if(forwardPointables.Count > 0)
		{
			
			float minZ = float.MaxValue;
			
			foreach(Pointable pointable in forwardPointables)
			{
				if(pointable.TipPosition.z < minZ)
				{
					minZ = pointable.TipPosition.z;
					forwardPointable = pointable;
				}
			}
		}
		
		return forwardPointable;
	}

	/*
	 * Returns a list of pointables whose position is in front 
	 * of the hand (relative to the hand direction). 
	 * 
	 * This is most useful in trying to lower the chances 
	 * of detecting a thumb (though not a perfect method).
	 */
	private List<Pointable> forwardFacingPointables(Hand hand)
	{
		List<Pointable> forwardPointables = new List<Pointable>();
		
		foreach(Pointable pointable in hand.Pointables)
		{
			if(isForwardRelativeToHand(pointable, hand)) 
			{ 
				forwardPointables.Add(pointable); 
			}
		}
		
		return forwardPointables;
	}

	private bool isForwardRelativeToHand(Pointable item, Hand hand)
	{
		Vector3 vHandToFinger = (LeapToUnity(item.TipPosition, true) - LeapToUnity(hand.PalmPosition, true)).normalized;
		float fDotProduct = Vector3.Dot(vHandToFinger, LeapToUnity(hand.Direction, false));
		
		return fDotProduct > 0.7f;
	}
	
	
	/*
	 * Returns whether or not the given hand is open.
	 */
	private bool isHandOpen(Hand hand)
	{
		return hand.Fingers.Count > 2;
	}
	

	// copies the needed libraries in the project directory
	private bool CheckLibsPresence()
	{
		bool bOneCopied = false, bAllCopied = true;
		
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
		if(!File.Exists("Leap.dll"))
		{
			Debug.Log("Copying Leap library...");
			TextAsset textRes = Resources.Load("Leap.dll", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				using (FileStream fileStream = new FileStream ("Leap.dll", FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write (textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bOneCopied = File.Exists("Leap.dll");
				bAllCopied = bAllCopied && bOneCopied;
				
				if(bOneCopied)
					Debug.Log("Copied Leap library.");
			}
		}

		if(!File.Exists("LeapCSharp.dll"))
		{
			Debug.Log("Copying LeapCSharp library...");
			TextAsset textRes = Resources.Load("LeapCSharp.dll", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				using (FileStream fileStream = new FileStream ("LeapCSharp.dll", FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write (textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bOneCopied = File.Exists("LeapCSharp.dll");
				bAllCopied = bAllCopied && bOneCopied;
				
				if(bOneCopied)
					Debug.Log("Copied LeapCSharp library.");
			}
		}

		if(!File.Exists("msvcp100.dll"))
		{
			Debug.Log("Copying msvcp100 library...");
			TextAsset textRes = Resources.Load("msvcp100.dll", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				using (FileStream fileStream = new FileStream ("msvcp100.dll", FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write (textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bOneCopied = File.Exists("msvcp100.dll");
				bAllCopied = bAllCopied && bOneCopied;
				
				if(bOneCopied)
					Debug.Log("Copied msvcp100 library.");
			}
		}
		
		if(!File.Exists("msvcr100.dll"))
		{
			Debug.Log("Copying msvcr100 library...");
			TextAsset textRes = Resources.Load("msvcr100.dll", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				using (FileStream fileStream = new FileStream ("msvcr100.dll", FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write (textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bOneCopied = File.Exists("msvcr100.dll");
				bAllCopied = bAllCopied && bOneCopied;
				
				if(bOneCopied)
					Debug.Log("Copied msvcr100 library.");
			}
		}
#endif
		
#if UNITY_EDITOR || UNITY_STANDALONE_OSX
		if(!File.Exists("libLeap.dylib"))
		{
			Debug.Log("Copying Leap library...");
			TextAsset textRes = Resources.Load("libLeap.dylib", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				using (FileStream fileStream = new FileStream ("libLeap.dylib", FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write (textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bOneCopied = File.Exists("libLeap.dylib");
				bAllCopied = bAllCopied && bOneCopied;
				
				if(bOneCopied)
					Debug.Log("Copied Leap library.");
			}
		}

		if(!File.Exists("libLeapCSharp.dylib"))
		{
			Debug.Log("Copying LeapCSharp library...");
			TextAsset textRes = Resources.Load("libLeapCSharp.dylib", typeof(TextAsset)) as TextAsset;
			
			if(textRes != null)
			{
				using (FileStream fileStream = new FileStream ("libLeapCSharp.dylib", FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					fileStream.Write (textRes.bytes, 0, textRes.bytes.Length);
				}
				
				bOneCopied = File.Exists("libLeapCSharp.dylib");
				bAllCopied = bAllCopied && bOneCopied;
				
				if(bOneCopied)
					Debug.Log("Copied LeapCSharp library.");
			}
		}
#endif

		return bOneCopied && bAllCopied;
	}
	
}
