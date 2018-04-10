# VRControlWheel
A Segmented Control Wheel Designed For VR

## Quick start 
Download the Zip for this repo, and copy the ControlWheel script into your Unity Project.

## Example Scene 
To view the example scene and example script ConsolePrinter example, download this repo as a Zip and extract. With Unity, click Open Project, and navigate to where you extracted the Zip. Once the project is open, you can view and run the exmamle scene. Feel free to play around with the scene and ConsolePrinter example.

## How It Works 
The Control Wheel is a circular, segmented action selector. It is especially useful for VR, where button inputs are limited and non-diegetic UI is more appropriate.

You can attach the ControlWheel component to any GameObject. It can be controlled by another, more user-specific script by calling: 
 - 	AddControlWheelAction / AddControlWheelActions		(one / many ControlWheelSegments)
 -  RemoveControlWheelAction							            (string name)
 -  DisplayControlWheel									              ()
 -  HideControlWheel									                ()
 -  HighlightSectionAtLocation							          (Vector2 locationInCircle)
 - 	Select												                    (Vector2 locationInCircle)
     - Calls the associated action for the segment found by locationInCirlce
 
 ### Preferred Position:
If a segment is initialised with a preferred postion, the ControlWheel will tyr its best to honour the segment's position. However, for an odd number of segments, the segment might not appear where intended. Also, if two segments have the same preferred position, the one added first will always be assigned the position and any subsequent will be treated as if they had no preferred position.
