using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ControlWheel))]
public class ConsolePrinter : MonoBehaviour {

	public KeyCode toggleDisplayControlWheel = KeyCode.Space;
	public KeyCode toggleThirdAction = KeyCode.Tab;
	public GameObject targeter;

	private ControlWheel controlWheel;

	void Awake(){

		this.controlWheel = GetComponent<ControlWheel> ();
		//Remember to call Init!!! This is to ensure the scripts execute in the correct order
		this.controlWheel.Init ();

		ControlWheelSegment printToConsoleSegment = new ControlWheelSegment(
			
			name: "Print To Console",

			action: 
			//If this notation is strange to you, check out this link: https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/lambda-expressions
			() => {
				Debug.Log("Hello, world!");
			},

			//Loads the Sprite from the Resources folder
			icon: Resources.Load<Sprite>("DemoConsoleLogger/1"),

			preferredPosition: ControlWheelSegment.PreferredPosition.Bottom
		);

		ControlWheelSegment incrementor = new ControlWheelSegment(

			name: "Print To Console",

			//You don't have to pass arrow functions, you can also pass references to functions
			action: this.Increment,

			//Loads the Sprite from the Resources folder
			icon: Resources.Load<Sprite>("DemoConsoleLogger/2"),

			preferredPosition: ControlWheelSegment.PreferredPosition.Top
		);

		this.controlWheel.AddControlWheelActions (new ControlWheelSegment[] {
			printToConsoleSegment,
			incrementor
		});
						

		this.controlWheel.DisplayControlWheel();
	}

	void Update(){

		//Toggles ControlWheel active
		if (Input.GetKeyDown (toggleDisplayControlWheel)) {

			if (this.controlWheel.Active == true) {
				this.controlWheel.HideControlWheel ();
			} else {
				this.controlWheel.DisplayControlWheel ();
			}
		}

		//Third Action
		if (Input.GetKeyDown (toggleThirdAction)) {
			if (thirdActionAdded == true) {
				RemoveThirdAction ();
			} else {
				AddThirdAction ();
			}
		}

		//Interaction With The Cylinder
		RaycastHit info;
		Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);

		if (Physics.Raycast (ray, out info)) {

			targeter.SetActive (true);
			targeter.transform.position = info.point;

			if (info.collider != null && info.collider.name == "Circle") {

				Vector3 location = info.transform.InverseTransformPoint (info.point);
				Vector2 locationVec2 = new Vector2 (location.x, location.z);

				this.controlWheel.HighlightSectionAtLocation (locationVec2);

				//Left click
				if (Input.GetMouseButtonDown (0)) {
					this.controlWheel.Select (locationVec2);
				}

				return;
			}

		} 

		targeter.SetActive (false);
	}
		

	//For the incrementor
	private int incrementMe;
	private void Increment(){
		incrementMe++;
		Debug.Log (string.Format("I've been incremented to {0}", incrementMe));
	}


	//For the add / remove 
	private bool thirdActionAdded = false;
	private string thirdActionName = "Remove Me!";
	private void AddThirdAction(){

		this.controlWheel.AddControlWheelAction ( new ControlWheelSegment (

			name: thirdActionName,

			action: () => {
				Debug.Log(string.Format("You can add me or remove me! Just hit {0}", toggleThirdAction));
			},

			//Loads the Sprite from the Resources folder
			icon: Resources.Load<Sprite>("DemoConsoleLogger/3")
		));

		this.thirdActionAdded = true;
	}

	private void RemoveThirdAction(){

		this.controlWheel.RemoveControlWheelAction (thirdActionName);

		this.thirdActionAdded = false;

	}

}
