Team Name: CS465-Free Solo
All tasks were completed by: Jaden Shelton



PROJECT OVERVIEW
My project demonstrates an alternate approach to different interaction techniques in Unity
since I did not have a Headset Available to me.
Part 1 is mouse interaction.
Part 2 utilizes the media pipe plug in for unity for camera hand tracking.



The object interaction that implemented so far includes:
Object selection
Object dragging
Proximity-based feedback
Gesture-based interaction (hand tracking)



HOW TO RUN THE PROJECT
Open Project in Unity
Select the root project folder included in this zip file
Go to Assets -> Scenes -> and open Main
Click "Play" in Unity Editor



Controls:
Mouse: 

Left Click: Grab / Release objects
Move Mouse: Move pointer


Keyboard:
TAB: Toggle interaction mode (Mouse / Hand Mode)



All of my scripts are in the MyScripts directory
Assets/
MyScripts/

* MouseInteractor.cs
* DragGrabbableRB.cs
* ProximityColor.cs
* InteractionManager.cs
* HandGestureLogger.cs



Mouse Interaction:
Raycasting is used to detect objects in the scene
Clicking allows grabbing and dragging objects



Object Dragging:
Objects follow the cursor smoothly using Rigidbody movement
Surface constraints prevent clipping through the table



Proximity Feedback:
Objects change color when the pointer is near



Hand Gesture (MediaPipe):
Detects hand landmarks
Logs index finger position for interaction mapping



Youtube Checkpoint 1 Progress Update:
https://youtu.be/gF5QakT8TFI

