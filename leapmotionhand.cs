using HutongGames.PlayMaker;
using UnityEngine;
using System.Collections;
using System;

namespace HutongGames.PlayMaker.Actions
{
	[ActionCategory("LeapMotion Actions")]
	[Tooltip("Allows you to get the pointable data (position, direction and rotation) from the LeapMotion sensor.")]
	
	public class GetHandData : FsmStateAction
	{
		[UIHint(UIHint.Variable)]
		[Tooltip("Variable to store the flag, if the hand data is valid or not.")]
		public FsmBool isValid;
		
		[UIHint(UIHint.Variable)]
		[Tooltip("Variable to store the position of the hand.")]
		public FsmVector3 handPosition;
		
		[UIHint(UIHint.Variable)]
		[Tooltip("Variable to store the normalized position of the cursor.")]
		public FsmVector3 cursorNormalizedPos;
		
		[UIHint(UIHint.Variable)]
		[Tooltip("Variable to store the screen position of the cursor.")]
		public FsmVector3 cursorScreenPos;
		
		private LeapManager manager;
		
		
		// called when the state becomes active
		public override void OnEnter()
		{			
			getPointableData();
		}
		
		// called before leaving the current state
		public override void OnExit ()
		{
		}
		
		public override void OnUpdate()
		{
			getPointableData();			
		}
		
		// Update is called once per frame
		private void getPointableData()
		{		
			if(manager == null)
			{
				manager = LeapManager.Instance;
			}
			
			if(manager != null && manager.IsLeapInitialized() && manager.IsHandValid())
			{
				isValid.Value = true;
				
				handPosition.Value = manager.GetHandPos();
				cursorNormalizedPos.Value = manager.GetCursorNormalizedPos();
				cursorScreenPos.Value = manager.GetCursorScreenPos();
			}
			else
			{
				isValid.Value = false;
			}
		}
	}
}
