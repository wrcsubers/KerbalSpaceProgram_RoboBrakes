
//=====================================================================================
//=====================================================================================
// The MIT License (MIT)
// 
// RoboBrakes - Copyright (c) 2015 WRCsubeRS
// 
// RoboBrakes - A Mod for Kerbal Space Program by Squad
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// 
//=====================================================================================
//=====================================================================================
//
// Version 0.1 - Initial Release - 07.27.2015
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Reflection;
using UnityEngine;
using KSP.IO;

namespace RoboBrakes
{
	[KSPAddon (KSPAddon.Startup.Flight, false)]
	public class RoboBrakesMono : MonoBehaviour
	{
		//============================================================================================================================================
		//Initialize Variables
		//============================================================================================================================================
		private ConfigNode RoboBrakes_SystemSettings;

		private bool FirstRun = true;

		//GUI & Displays
		//============================================================================================================================================
		private bool ShowMainGUI = false;
		//Main Window Size & Position
		//---------------------------------------------------------------------------------------------------------------------
		private static Rect RoboBrakes_MainGUI = new Rect ();
		private float MainGUI_WindowTop = 5;
		private float MainGUI_WindowLeft = ((Screen.width / 2) + 150);
		private float MainGUI_WindowWidth = 200;
		private float MainGUI_WindowHeight = 180;

		private string ActiveDisplay;
		private string RoboBrakes_GearDisplay;
		private string RoboBrakes_AeroDisplay;

		private string ArmedDisplay = "<b><color=#666666>Armed</color></b>";
		private string RBSettings_TZDisplay = "<b><color=#33CC33>Throttle 0</color></b>";
		private string RBSettings_BKDisplay = "<b><color=#33CC33>Override</color></b>";

		//Colors being used:
		//Green: #33CC33
		//Yellow: #E6E600
		//Red: red
		//Grey: #777777
		//White: white

		//Settings Window Size & Position
		//---------------------------------------------------------------------------------------------------------------------
		private bool ShowSettingsGUI = false;
		private static Rect RoboBrakes_SettingsGUI = new Rect ();
		private float SettingsGUI_WindowTop = 190;
		private float SettingsGUI_WindowLeft = ((Screen.width / 2) + 150);
		private float SettingsGUI_WindowWidth = 220;
		private float SettingsGUI_WindowHeight = 240;
		private float SelectionGridX;
		private float SelectionGridY;

		//Toolbar Textures & Setup
		//---------------------------------------------------------------------------------------------------------------------
		private ApplicationLauncherButton RoboBrakes_ToolbarButton = null;
		private Texture2D RoboBrakes_ButtonIdle = new Texture2D (38, 38, TextureFormat.ARGB32, false);
		private Texture2D RoboBrakes_ButtonArmed = new Texture2D (38, 38, TextureFormat.ARGB32, false);

		//Settings - These get loaded/saved when plugin loads/unloads
		//============================================================================================================================================
		//ActivationSettings
		private float DeactivationSpeed = 10.0f;
		private float ActivationDelay = 0.5f;
		//Brake Override Setting
		private bool RBSettings_BKeyPress = true;
		private bool RBSettings_BKeyToggle = false;
		private bool RBSettings_BKeyOff = false;
		//Throttle Zero Setting
		private bool RBSettings_ThrottleZeroing = true;
		//Enabled Parts
		private bool RoboBrakes_AUTOGEAR = true;
		private bool RoboBrakes_AUTOAERO = true;
		private bool RoboBrakes_AUTOCHUTE = true;
		private bool RoboBrakes_OVERRIDEGEAR = true;
		private bool RoboBrakes_OVERRIDEAERO = true;
		private bool RoboBrakes_OVERRIDECHUTE = false;

		//Other Variables - These get set dynamically depending on the state of things
		//============================================================================================================================================

		//Setup Timers
		//---------------------------------------------------------------------------------------------------------------------
		private static Timer Timer100 = new Timer (100);
		//Text Blinker
		private bool BlinkText;
		//Setup Multiple Timers
		private float TimerTime1 = 0.0f;
		private float TimerTime2 = 0.0f;
		private bool TimerTime2Done = true;

		//Vessel Info
		//---------------------------------------------------------------------------------------------------------------------
		private Vessel PreviousVessel;
		private float GroundSpeed;
		private bool IsLanded;
		private bool LandedNow;
		private bool LandedPrev;

		//Part Counts
		//---------------------------------------------------------------------------------------------------------------------
		private int PartCount = -1;
		private List <Part> CapablePartList = new List <Part> ();
		private int RoboBrakes_GearEnabledCount;
		private int RoboBrakes_AeroEnabledCount;
		private int RoboBrakes_ParaEnabledCount;

		//Activation Stuff
		//---------------------------------------------------------------------------------------------------------------------
		private bool RoboBrakes_ARMED = false;
		private bool RoboBrakes_ACTIVE = false;
		private bool RoboBrakes_READYFORACTIVATION;
		private bool RoboBrakes_BRAKEOVERRIDE;
		private bool RoboBrakes_HASPARTENABLED;
		private bool RoboBrakes_CHUTEREADY;
		private bool RoboBrakes_CUTCHUTE;

		//============================================================================================================================================
		//Start Running Processes
		//============================================================================================================================================
		//This function gets called only once, during the KSP loading screen.
		private void Awake ()
		{
			//Set Toolbar Textures and tell us when it's alive
			if (GameDatabase.Instance.ExistsTexture ("RoboBrakes/Textures/ToolbarButtonIdle")) {
				RoboBrakes_ButtonIdle = GameDatabase.Instance.GetTexture ("RoboBrakes/Textures/ToolbarButtonIdle", false);
				RoboBrakes_ButtonArmed = GameDatabase.Instance.GetTexture ("RoboBrakes/Textures/ToolbarButtonArmed", false);
			}
			GameEvents.onGUIApplicationLauncherReady.Add (OnGUIApplicationLauncherReady);
		}

		//Called when the flight starts or in the editor. OnStart will be called before OnUpdate or OnFixedUpdate are ever called.
		private void Start ()
		{
			RoboBrakes_SystemSettings = new ConfigNode ();
			RoboBrakes_SystemSettings = ConfigNode.Load ("GameData/RoboBrakes/Config/RoboBrakes_PluginSettings.cfg");
			if (RoboBrakes_SystemSettings != null) {
				print ("ROBOBRAKES - Settings exist! Loading Values...");
				//--------------------------------------------------------------------------------------------------
				MainGUI_WindowTop = float.Parse (RoboBrakes_SystemSettings.GetValue ("MainGUI_WindowTop"));
				MainGUI_WindowLeft = float.Parse (RoboBrakes_SystemSettings.GetValue ("MainGUI_WindowLeft"));
				SettingsGUI_WindowTop = float.Parse (RoboBrakes_SystemSettings.GetValue ("SettingsGUI_WindowTop"));
				SettingsGUI_WindowLeft = float.Parse (RoboBrakes_SystemSettings.GetValue ("SettingsGUI_WindowLeft"));
				//--------------------------------------------------------------------------------------------------
				DeactivationSpeed = float.Parse (RoboBrakes_SystemSettings.GetValue ("DeactivationSpeed"));
				ActivationDelay = float.Parse (RoboBrakes_SystemSettings.GetValue ("ActivationDelay"));
				RBSettings_BKeyPress = Boolean.Parse (RoboBrakes_SystemSettings.GetValue ("RBSettings_BKeyPress"));
				RBSettings_BKeyToggle = Boolean.Parse (RoboBrakes_SystemSettings.GetValue ("RBSettings_BKeyToggle"));
				RBSettings_BKeyOff = Boolean.Parse (RoboBrakes_SystemSettings.GetValue ("RBSettings_BKeyOff"));
				RBSettings_ThrottleZeroing = Boolean.Parse (RoboBrakes_SystemSettings.GetValue ("RBSettings_ThrottleZeroing"));
				//--------------------------------------------------------------------------------------------------
				RoboBrakes_AUTOGEAR = Boolean.Parse (RoboBrakes_SystemSettings.GetValue ("RoboBrakes_AUTOGEAR"));
				RoboBrakes_AUTOAERO = Boolean.Parse (RoboBrakes_SystemSettings.GetValue ("RoboBrakes_AUTOAERO"));
				RoboBrakes_AUTOCHUTE = Boolean.Parse (RoboBrakes_SystemSettings.GetValue ("RoboBrakes_AUTOCHUTE"));
				RoboBrakes_OVERRIDEGEAR = Boolean.Parse (RoboBrakes_SystemSettings.GetValue ("RoboBrakes_OVERRIDEGEAR"));
				RoboBrakes_OVERRIDEAERO = Boolean.Parse (RoboBrakes_SystemSettings.GetValue ("RoboBrakes_OVERRIDEAERO"));
				RoboBrakes_OVERRIDECHUTE = Boolean.Parse (RoboBrakes_SystemSettings.GetValue ("RoboBrakes_OVERRIDECHUTE"));
				//--------------------------------------------------------------------------------------------------
			} else {
				print ("ROBOBRAKES - Settings don't exist! Creating new file with built in defaults...");
				SaveSettings ();
			}
			RenderingManager.AddToPostDrawQueue (0, OnDraw);
			RoboBrakes_MainGUI = new Rect (MainGUI_WindowLeft, MainGUI_WindowTop, MainGUI_WindowWidth, MainGUI_WindowHeight); //set window position
			RoboBrakes_SettingsGUI = new Rect (SettingsGUI_WindowLeft, SettingsGUI_WindowTop, SettingsGUI_WindowWidth, SettingsGUI_WindowHeight);
			OnGUIApplicationLauncherReady ();

			//Setup Timer for Text Blinking
			Timer100.Elapsed += new ElapsedEventHandler (OnTimedEvent1);
			Timer100.Enabled = true;
		}
			
		//This is all GUI Stuff...
		//============================================================================================================================================
		//Create Toolbar Button if one doesn't already exist
		private void OnGUIApplicationLauncherReady ()
		{
			//Create the button in the KSP AppLauncher if one doesn't already exist
			if (RoboBrakes_ToolbarButton == null) {
				RoboBrakes_ToolbarButton = ApplicationLauncher.Instance.AddModApplication (
					RoboBrakes_GUISwitch, RoboBrakes_GUISwitch,
					null, null,
					null, null,
					ApplicationLauncher.AppScenes.FLIGHT,
					RoboBrakes_ButtonIdle
				);
			}
		}

		//This is the Switch to turn show/hide the main GUI
		public void RoboBrakes_GUISwitch ()
		{
			if (ShowMainGUI == false) {
				ShowMainGUI = true;
			} else {
				ShowMainGUI = !ShowMainGUI;
			}
		}

		//Update the Toolbar Button when the status of RoboBrakes changes
		private void UpdateToolbarButton ()
		{
			if (RoboBrakes_BRAKEOVERRIDE == false) {
				if (RoboBrakes_ARMED == true) {
					if (RoboBrakes_ACTIVE == true) {
						if (BlinkText) {
							RoboBrakes_ToolbarButton.SetTexture (RoboBrakes_ButtonArmed);
						} else {
							RoboBrakes_ToolbarButton.SetTexture (RoboBrakes_ButtonIdle);
						}
					} else {
						RoboBrakes_ToolbarButton.SetTexture (RoboBrakes_ButtonArmed);
					}
				} else {
					RoboBrakes_ToolbarButton.SetTexture (RoboBrakes_ButtonIdle);
				}
			} else {
				RoboBrakes_ToolbarButton.SetTexture (RoboBrakes_ButtonArmed);
			}
		}

		//This initializes the MainGUI & SettingsGUI Windows
		private void OnDraw ()
		{
			GUI.skin.window.richText = true;
			if (FlightGlobals.ActiveVessel.state == Vessel.State.ACTIVE && ShowMainGUI == true) {
				RoboBrakes_MainGUI = GUI.Window (15844, RoboBrakes_MainGUI, MainGUI, "<b>RoboBrakes</b>");
				if (ShowSettingsGUI == true) {
					RoboBrakes_SettingsGUI = GUI.Window (15845, RoboBrakes_SettingsGUI, SettingsGUI, "<b>RoboBrakes - Settings</b>");
				}
			} else {
				//Close the SettingsGUI window automatically if MainGUI isn't open
				ShowSettingsGUI = false;
			}
		}

		//============================================================================================================================================
		//This sets up the MainGUI Window and lays out the entire GUI
		private void MainGUI (int WindowID)
		{
			GUI.skin.button.richText = true;
			GUI.skin.button.fontSize = 14;
			GUI.skin.box.fontSize = 16;
			GUI.skin.box.alignment = TextAnchor.MiddleCenter;
			GUI.skin.box.fontStyle = FontStyle.Bold;
			GUI.skin.label.alignment = TextAnchor.MiddleCenter;
			GUI.skin.label.fontStyle = FontStyle.Bold;
			//Arm/Disarm switch
			if (RoboBrakes_ARMED == false) {
				//Cannot Arm System Unless there are Capable Parts Enabled
				if (RoboBrakes_HASPARTENABLED == false) {
					GUIContent ArmDisarmSwitch = new GUIContent ("<b><color=#777777>Arm/Disarm</color></b>");
					if (GUI.Button (new Rect (40, 20, 120, 22), ArmDisarmSwitch) == true) {
						print ("ROBOBRAKES - Cannot Arm System: No Eligible Parts are Enabled!");
					}
				} else {
					GUIContent ArmDisarmSwitch = new GUIContent ("<b><color=white>Arm/Disarm</color></b>");
					if (GUI.Button (new Rect (40, 20, 120, 22), ArmDisarmSwitch) == true) {
						RoboBrakes_ARMED = true;
						ArmedDisplay = "<b><color=#33CC33>Armed</color></b>";
						print ("ROBOBRAKES - Brakes Armed!");
					}
				}
			}
			if (RoboBrakes_ARMED == true) {
				GUIContent ArmDisarmSwitch = new GUIContent ("<b><color=white>Arm/Disarm</color></b>");
				if (GUI.Button (new Rect (40, 20, 120, 22), ArmDisarmSwitch) == true) {
					ArmedDisplay = "<b><color=#777777>Armed</color></b>";
					RoboBrakes_ARMED = false;
					print ("ROBOBRAKES - Brakes Disarmed!");
				}
			}
			//Display Boxes...
			GUI.Box (new Rect (20, 45, 75, 30), ArmedDisplay);
			GUI.Box (new Rect (105, 45, 75, 30), ActiveDisplay);
			GUI.skin.box.fontSize = 12;
			GUI.Box (new Rect (25, 80, 150, 20), RoboBrakes_AeroDisplay);
			GUI.Box (new Rect (25, 103, 150, 20), RoboBrakes_GearDisplay);
			GUI.Box (new Rect (20, 128, 75, 20), RBSettings_BKDisplay);
			GUI.Box (new Rect (105, 128, 75, 20), RBSettings_TZDisplay);
			GUI.Label (new Rect (5, 148, 190, 10), "<color=#222222>__________________________________</color>");
			//---------------------------------------------------------------------------------------------------------------------
			//Show or Hide Settings
			GUI.skin.button.fontSize = 11;
			if (ShowSettingsGUI == false) {
				GUIContent Settings = new GUIContent ("<b><color=#777777>Settings</color></b>");
				if (GUI.Button (new Rect (50, 155, 100, 20), Settings) == true) {
					ShowSettingsGUI = true;
				}
			}
			if (ShowSettingsGUI == true) {
				GUIContent Settings = new GUIContent ("<b><color=white>Settings</color></b>");
				if (GUI.Button (new Rect (50, 155, 100, 20), Settings) == true) {
					ShowSettingsGUI = false;
				}
			}
			//This allows the GUI to be moved... This MUST be last!
			if (RoboBrakes_MainGUI.position.x != MainGUI_WindowLeft)
				MainGUI_WindowLeft = RoboBrakes_MainGUI.position.x;
			if (RoboBrakes_MainGUI.position.y != MainGUI_WindowTop)
				MainGUI_WindowTop = RoboBrakes_MainGUI.position.y;
			GUI.DragWindow (new Rect (0, 0, 400, 17));
		}

		//Settings GUI
		//============================================================================================================================================
		private void SettingsGUI (int WindowID)
		{
			GUI.skin.label.alignment = TextAnchor.MiddleLeft;
			GUI.skin.label.fontSize = 11;
			//Deactivation Speed Slider
			//---------------------------------------------------------------------------------------------------------------------
			if (DeactivationSpeed < 1) {
				GUI.Label (new Rect (25, 20, 170, 17), "Deactivation Speed: Full Stop");
			} else {
				GUI.Label (new Rect (25, 20, 170, 17), "Deactivation Speed: " + DeactivationSpeed + " m/s");
			}
			DeactivationSpeed = GUI.HorizontalSlider (new Rect (25, 35, 170, 10), Mathf.Floor (DeactivationSpeed), 0.25f, 25f);
			//Activation Delay Time Slider
			//---------------------------------------------------------------------------------------------------------------------
			if (ActivationDelay == 0.0) {
				GUI.Label (new Rect (25, 47, 170, 17), "Activation Delay: Instant");
			} else {
				GUI.Label (new Rect (25, 47, 170, 17), "Activation Delay: " + ActivationDelay + " s");
			}
			ActivationDelay = GUI.HorizontalSlider (new Rect (25, 62, 170, 10), ((Mathf.Round (ActivationDelay * 10)) / 10), 0.0f, 5.0f);

			//Brake Override Settings - Each press of the button toggles between Off, Press, Toggle
			//---------------------------------------------------------------------------------------------------------------------
			GUI.Label (new Rect (25, 77, 100, 17), "Brake Override: ");
			//B Key Off
			if (RBSettings_BKeyOff) {
				GUIContent BSetting = new GUIContent ("<b><color=grey>Off</color></b>");
				if (GUI.Button (new Rect (130, 77, 65, 17), BSetting) == true) {
					RBSettings_BKeyOff = false;
					RBSettings_BKeyPress = true;
					RBSettings_BKeyToggle = false;
				}
				//Turn Brakes Off if they are engaged when modes are switched
				RoboBrakes_BRAKEOVERRIDE = false;
			}
			//B Key Press/Hold
			if (RBSettings_BKeyPress) {
				GUIContent BSetting = new GUIContent ("<b><color=white>Hold</color></b>");
				if (GUI.Button (new Rect (130, 77, 65, 17), BSetting) == true) {
					RBSettings_BKeyOff = false;
					RBSettings_BKeyPress = false;
					RBSettings_BKeyToggle = true;
				}
			}
			//B Key Toggle
			if (RBSettings_BKeyToggle) {
				GUIContent BSetting = new GUIContent ("<b><color=white>Toggle</color></b>");
				if (GUI.Button (new Rect (130, 77, 65, 17), BSetting) == true) {
					RBSettings_BKeyOff = true;
					RBSettings_BKeyPress = false;
					RBSettings_BKeyToggle = false;
				}
			}

			//Throttle Zeroing Setting - Each press of the button toggles between On & Off
			//---------------------------------------------------------------------------------------------------------------------
			GUI.Label (new Rect (25, 98, 120, 17), "Throttle Zeroing: ");
			//Throttle Zeroing On
			if (RBSettings_ThrottleZeroing == true) {
				GUIContent ThrottleZSetting = new GUIContent ("<b><color=white>On</color></b>");
				if (GUI.Button (new Rect (130, 98, 65, 17), ThrottleZSetting) == true) {
					RBSettings_ThrottleZeroing = false;
				}
			}
			//Throttle Zeroing Off
			if (RBSettings_ThrottleZeroing == false) {
				GUIContent ThrottleZSetting = new GUIContent ("<b><color=grey>Off</color></b>");
				if (GUI.Button (new Rect (130, 98, 65, 17), ThrottleZSetting) == true) {
					RBSettings_ThrottleZeroing = true;
				}
			}
	
			//Selection Grid
			//---------------------------------------------------------------------------------------------------------------------
			SelectionGridX = 10;
			SelectionGridY = 155;
			GUI.Label (new Rect (SelectionGridX, SelectionGridY+2, 200, 10), "<color=#222222>__________________________________</color>");
			GUI.Label (new Rect (SelectionGridX+5, SelectionGridY+20, 70, 17), "Automatic:");
			GUI.Label (new Rect (SelectionGridX+15, SelectionGridY+40, 70, 17), "Override:");
			GUI.Label (new Rect (SelectionGridX+75, SelectionGridY+5, 40, 17), "Gear");
			if (GUI.Toggle (new Rect (SelectionGridX+82, SelectionGridY+18, 17, 17), RoboBrakes_AUTOGEAR,"") != RoboBrakes_AUTOGEAR) RoboBrakes_AUTOGEAR = !RoboBrakes_AUTOGEAR;
			if (GUI.Toggle (new Rect (SelectionGridX+82, SelectionGridY+38, 17, 17), RoboBrakes_OVERRIDEGEAR,"") != RoboBrakes_OVERRIDEGEAR) RoboBrakes_OVERRIDEGEAR = !RoboBrakes_OVERRIDEGEAR;
			GUI.Label (new Rect (SelectionGridX+118, SelectionGridY+5, 40, 17), "Aero");
			if (GUI.Toggle (new Rect (SelectionGridX+123, SelectionGridY+18, 17, 17), RoboBrakes_AUTOAERO,"") != RoboBrakes_AUTOAERO) RoboBrakes_AUTOAERO = !RoboBrakes_AUTOAERO;
			if (GUI.Toggle (new Rect (SelectionGridX+123, SelectionGridY+38, 17, 17), RoboBrakes_OVERRIDEAERO,"") != RoboBrakes_OVERRIDEAERO) RoboBrakes_OVERRIDEAERO = !RoboBrakes_OVERRIDEAERO;
			GUI.Label (new Rect (SelectionGridX+155, SelectionGridY+5, 40, 17), "Chute");
			if (GUI.Toggle (new Rect (SelectionGridX+165, SelectionGridY+18, 17, 17), RoboBrakes_AUTOCHUTE,"") != RoboBrakes_AUTOCHUTE) RoboBrakes_AUTOCHUTE = !RoboBrakes_AUTOCHUTE;
			if (GUI.Toggle (new Rect (SelectionGridX+165, SelectionGridY+38, 17, 17), RoboBrakes_OVERRIDECHUTE, "") != RoboBrakes_OVERRIDECHUTE) RoboBrakes_OVERRIDECHUTE = !RoboBrakes_OVERRIDECHUTE;
			GUI.Label (new Rect (SelectionGridX, SelectionGridY+56, 200, 10), "<color=#222222>__________________________________</color>");

			//Okay Button
			//---------------------------------------------------------------------------------------------------------------------
			GUIContent OkayClose = new GUIContent ("<b><color=white>Okay</color></b>");
			if (GUI.Button (new Rect (87, 218, 46, 17), OkayClose) == true) {
				ShowSettingsGUI = false;
				SaveSettings ();
			}

			//This allows the GUI to be moved... This MUST be last!
			//---------------------------------------------------------------------------------------------------------------------
			if (RoboBrakes_SettingsGUI.position.x != SettingsGUI_WindowLeft)
				SettingsGUI_WindowLeft = RoboBrakes_SettingsGUI.position.x;
			if (RoboBrakes_SettingsGUI.position.y != SettingsGUI_WindowTop)
				SettingsGUI_WindowTop = RoboBrakes_SettingsGUI.position.y;
			GUI.DragWindow (new Rect (0, 0, 400, 17));
		}
			
		//Triggers and Actions for Activating RoboBrakes
		//============================================================================================================================================

		//This is a trigger to run actions ONE TIME upon Landing...
		private void LandingTrigger ()
		{
			//Start Activation Delay Timer
			if (RoboBrakes_ARMED) {
				//This starts TimerTime2 if the Activation Delay is greater than 0
				if (ActivationDelay > 0) {
					TimerTime2Done = false;
				} else {
					TimerCompleteTrigger ();
				}
			}
		}
		
		//This is a trigger to run actions ONE TIME upon Takeoff...
		private void TakeoffTrigger ()
		{
			//Reset the Ready for Activation trigger upon taking off
			RoboBrakes_READYFORACTIVATION = false;
		}

		//Timer Events for Module, these are for various things...
		private void OnTimedEvent1 (object source, ElapsedEventArgs e)
		{
			//TimerTime1 for Blink Text - This runs continuously
			TimerTime1 += 0.1f;
			//Blink Text every 0.4 seconds
			if (TimerTime1 == 0.4f) {
				TimerTime1 = 0.0f;
				BlinkText = !BlinkText;
			}

			//TimerTime2 for Activation Delay - Run only once when activated by LandingTrigger()
			if (TimerTime2Done == false) {
				TimerTime2 += 0.1f;
			} else {
				//Auto Reset Timer to 0 if it is not running
				TimerTime2 = 0.0f;
			}
		}

		//Triggered by completion of the Activation Delay timer
		private void TimerCompleteTrigger ()
		{
			print ("ROBOBRAKES - Brakes are Ready for Activation " + TimerTime2 + "s after landing!");
			RoboBrakes_READYFORACTIVATION = true;
			RoboBrakes_CHUTEREADY = true;
			//Set Throttle to Zero when the Timer completes, if that option is enabled
			if (RBSettings_ThrottleZeroing == true) {
				FlightInputHandler.state.mainThrottle = 0;
			}
		}

		private void DeactivationTrigger ()
		{
			RoboBrakes_CUTCHUTE = true;
		}

		//This method runs every physics frame
		//============================================================================================================================================
		private void FixedUpdate ()
		{
			//This allows the brakes to be over-riden.  This is determined by the 'B Key Setting'.
			//This is if B-Key is in 'Hold' Mode
			if (RBSettings_BKeyPress == true) {
				if (Input.GetKey (KeyCode.B)) {
					RoboBrakes_BRAKEOVERRIDE = true;
				} else {
					RoboBrakes_BRAKEOVERRIDE = false;
				}
			}
			//This is if B-Key is in 'Toggle' Mode
			if (RBSettings_BKeyToggle == true) {
				if (Input.GetKeyDown (KeyCode.B)) {
					RoboBrakes_BRAKEOVERRIDE = !RoboBrakes_BRAKEOVERRIDE;
				}
			}

			//Vessel Info
			//============================================================================================================================================
			//Check to see if we are the current vessel, otherwise destroy this instance
			if (FlightGlobals.ActiveVessel != PreviousVessel) {
				if (FirstRun == true) {
					PreviousVessel = FlightGlobals.ActiveVessel;
				} else {
					OnDestroy ();
				}
			}

			//Check to see if we are landed
			if (FlightGlobals.ActiveVessel.Landed == true) {
				IsLanded = true;
				LandedNow = true;
			} else {
				IsLanded = false;
			}

			//Trigger for a Landing or Takeoff...
			if (LandedNow != LandedPrev) {
				LandedPrev = LandedNow;
				if (IsLanded == true) {
					LandingTrigger ();
					print ("ROBOBRAKES - Landing!");
				}
				if (IsLanded == false) {
					TakeoffTrigger ();
					print ("ROBOBRAKES - Takeoff!");
				}
			}

			//Find our ground speed and convert to floating point
			GroundSpeed = Convert.ToSingle (FlightGlobals.ActiveVessel.srfSpeed);

			//Count all parts on the ship and recount if total vessel parts change
			if (PartCount != FlightGlobals.ActiveVessel.parts.Count ()) {
				PartCount = FlightGlobals.ActiveVessel.parts.Count ();
				//Clear Current List
				CapablePartList.Clear ();
				//Add parts with Robo Brake Part Module to main list
				foreach (Part RoboBrakeCapablePart in FlightGlobals.ActiveVessel.parts) {
					if (RoboBrakeCapablePart.Modules.Contains ("ModuleRoboBrakes")) {
						CapablePartList.Add (RoboBrakeCapablePart);
					}
				}
				print ("ROBOBRAKES - Count Complete! Number of RoboBrakable parts is " + CapablePartList.Count ());
			}

			if ((IsLanded == true) && (RoboBrakes_ARMED == true) && (RoboBrakes_ACTIVE == true) && (GroundSpeed < DeactivationSpeed)) {
				DeactivationTrigger ();
			}

			//Robo Brake Logic
			//============================================================================================================================================
			//Parameters for activating the brakes

			//Check to see if the Activation Delay timer is complete...
			if (TimerTime2 >= ActivationDelay && TimerTime2Done == false) {
				TimerTime2Done = true;
				TimerCompleteTrigger ();
			}

			//Well this is a mess... But it works
			if (RoboBrakes_BRAKEOVERRIDE == false) {
				//Check all of this if were not manually overiding the brakes
				if (IsLanded == true) {
					if (RoboBrakes_HASPARTENABLED == true) {
						if (GroundSpeed > DeactivationSpeed) {
							if (RoboBrakes_ARMED == true && RoboBrakes_READYFORACTIVATION == true) {
								RoboBrakes_ACTIVE = true;
							} else {
								RoboBrakes_ACTIVE = false;
							}
						} else {
							RoboBrakes_ACTIVE = false;
						}
					} else {
						RoboBrakes_ACTIVE = false;
						RoboBrakes_ARMED = false;
					}
				} else {
					RoboBrakes_ACTIVE = false;
					LandedNow = false;
				}
				//Brake OVERRIDE bypass
			} else {
				RoboBrakes_ACTIVE = true;
			}

			//Activating the brakes on each part
			//Check to see if there are any Robo Brake Capable Parts on the Vessel
			if (CapablePartList.Count () != 0) {
				//Cycle through eligible parts one-by-one
				foreach (Part SinglePart in CapablePartList) {
					//Create new Instance of ModuleRoboBrake to reference
					ModuleRoboBrakes MRB = new ModuleRoboBrakes ();
					MRB = SinglePart.FindModuleImplementing<ModuleRoboBrakes> ();
					//Check to see if RoboBrakes are enabled on this part - This checks the UI part right click KSPField
					if (MRB.RoboBrakeEnabled == true) {
						//---------------------------------------------------------------------------------------------------------------------
						//Check to see if this is a landing gear (or other type for units after this)
						if (SinglePart.Modules.Contains ("ModuleLandingGear")) {
							//Create new Instance of ModuleLandingGear to reference
							ModuleLandingGear MLG = new ModuleLandingGear ();
							MLG = SinglePart.FindModuleImplementing<ModuleLandingGear> ();
							//Increase count by 1 of enabled parts for GUI display
							RoboBrakes_GearEnabledCount++;
							//Create a list of actions that are available to this particular part & it's modules
							BaseActionList BAL = new BaseActionList (SinglePart, MLG); 
							//Cycle throught each action
							foreach (BaseAction BA in BAL) {
								//Find the 'Brakes' action
								if (BA.guiName == "Brakes") {
									//Engage brakes (or other action) when triggered
									if (RoboBrakes_ACTIVE == true) {
										//Add to list of parts to print
										
										//Create Action Parameter for enabling brakes (or other action) - I don't full understand this, but it is needed to activate the action
										KSPActionParam AP = new KSPActionParam (KSPActionGroup.None, KSPActionType.Activate);
										//Perform said action
										BA.Invoke (AP);
									} else {
										//Create Action Parameter for disabling brakes (or other action)
										KSPActionParam AP = new KSPActionParam (KSPActionGroup.None, KSPActionType.Deactivate);
										//Perform said action
										BA.Invoke (AP); 
									}
								}
							}
						}
						//---------------------------------------------------------------------------------------------------------------------
						//Advanced Landing Gear Module
						if (SinglePart.Modules.Contains ("ModuleAdvancedLandingGear")) {
							ModuleAdvancedLandingGear MALG = new ModuleAdvancedLandingGear ();
							MALG = SinglePart.FindModuleImplementing<ModuleAdvancedLandingGear> ();
							RoboBrakes_GearEnabledCount++;
							BaseActionList BAL = new BaseActionList (SinglePart, MALG); 
							foreach (BaseAction BA in BAL) { 
								if (BA.guiName == "Brakes") {
									if (RoboBrakes_ACTIVE == true) {
										
										KSPActionParam AP = new KSPActionParam (KSPActionGroup.None, KSPActionType.Activate); 
										BA.Invoke (AP); 
									} else {
										KSPActionParam AP = new KSPActionParam (KSPActionGroup.None, KSPActionType.Deactivate); 
										BA.Invoke (AP); 
									}
								}
							}
						}
						//---------------------------------------------------------------------------------------------------------------------
						//Fixed Landing Gear Module
						if (SinglePart.Modules.Contains ("ModuleLandingGearFixed")) {
							ModuleLandingGearFixed MLGF = new ModuleLandingGearFixed ();
							MLGF = SinglePart.FindModuleImplementing<ModuleLandingGearFixed> ();
							//These Gear are always down
							RoboBrakes_GearEnabledCount++;
							BaseActionList BAL = new BaseActionList (SinglePart, MLGF); 
							foreach (BaseAction BA in BAL) { 
								if (BA.guiName == "Brakes") {
									if (RoboBrakes_ACTIVE == true) {
										
										KSPActionParam AP = new KSPActionParam (KSPActionGroup.None, KSPActionType.Activate); 
										BA.Invoke (AP); 
									} else {
										KSPActionParam AP = new KSPActionParam (KSPActionGroup.None, KSPActionType.Deactivate); 
										BA.Invoke (AP); 
									}
								}
							}
						}
						//---------------------------------------------------------------------------------------------------------------------
						//Aero Brakes do not get/need activation if there isn't an atmosphere to brake against...
						//Control Surface Module
						if (FlightGlobals.ActiveVessel.atmDensity > 0) {
							if (SinglePart.Modules.Contains ("ModuleControlSurface")) {
								ModuleControlSurface MCS = new ModuleControlSurface ();
								MCS = SinglePart.FindModuleImplementing<ModuleControlSurface> ();
								RoboBrakes_AeroEnabledCount++;
								BaseActionList BAL = new BaseActionList (SinglePart, MCS); 
								foreach (BaseAction BA in BAL) { 
									if (RoboBrakes_ACTIVE == true) {
										if (BA.guiName == "Extend") {
											
											KSPActionParam AP = new KSPActionParam (KSPActionGroup.None, KSPActionType.Activate); 
											BA.Invoke (AP); 
										}
									} else {
										if (BA.guiName == "Retract") {
											KSPActionParam AP = new KSPActionParam (KSPActionGroup.None, KSPActionType.Activate); 
											BA.Invoke (AP); 
										}
									}
								}
							}
							//---------------------------------------------------------------------------------------------------------------------
							//Module Aero Surface
							if (SinglePart.Modules.Contains ("ModuleAeroSurface")) {
								ModuleAeroSurface MAS = new ModuleAeroSurface ();
								MAS = SinglePart.FindModuleImplementing<ModuleAeroSurface> ();
								RoboBrakes_AeroEnabledCount++;
								BaseActionList BAL = new BaseActionList (SinglePart, MAS); 
								foreach (BaseAction BA in BAL) { 
									if (RoboBrakes_ACTIVE == true) {
										if (BA.guiName == "Extend") {
											
											KSPActionParam AP = new KSPActionParam (KSPActionGroup.None, KSPActionType.Activate); 
											BA.Invoke (AP); 
										}
									} else {
										if (BA.guiName == "Retract") {
											KSPActionParam AP = new KSPActionParam (KSPActionGroup.None, KSPActionType.Activate); 
											BA.Invoke (AP); 
										}
									}
								}
							}
							//---------------------------------------------------------------------------------------------------------------------
							//Module Parachutes
							if (SinglePart.Modules.Contains ("ModuleParachute")) {
								ModuleParachute MPA = new ModuleParachute ();
								MPA = SinglePart.FindModuleImplementing<ModuleParachute> ();
								RoboBrakes_ParaEnabledCount++;
								BaseActionList BAL = new BaseActionList (SinglePart, MPA); 
								foreach (BaseAction BA in BAL) { 
									if (RoboBrakes_ACTIVE == true) {
										if (RoboBrakes_CHUTEREADY == true) {
											RoboBrakes_CHUTEREADY = false;
											
											MPA.Deploy ();
										}
									}
									if (MPA.deploymentState.Equals (ModuleParachute.deploymentStates.DEPLOYED)) {
										if (RoboBrakes_CUTCHUTE == true) {
											RoboBrakes_CUTCHUTE = false;
											MPA.CutParachute ();
											MPA.Repack ();
											print ("ROBOBRAKES - Chute " + SinglePart.name + " Cut & Repacked Automatically!");
										}
									}
								}
							}
							//---------------------------------------------------------------------------------------------------------------------
						}
					}
				}
				//Updating the GUI
				//============================================================================================================================================
				//Update Active Status if we are currently braking
				if (TimerTime2Done == true | RoboBrakes_BRAKEOVERRIDE == true) {
					if (RoboBrakes_ACTIVE == true) {
						ActiveDisplay = "<b><color=#33CC33>Active</color></b>";
					} else {
						ActiveDisplay = "<b><color=#777777>Active</color></b>";
					}
				} else {
					ActiveDisplay = "<b><color=#E6E600>Active</color></b>";
				}

				//Update Gear Display Counter
				RoboBrakes_GearDisplay = ("Enabled Gear: " + RoboBrakes_GearEnabledCount.ToString ());

				//Warn if Aero Modules are active and we are not in an Atmosphere
				if (FlightGlobals.ActiveVessel.atmDensity > 0) {
					//if Parachutes are enabled add to Aero Counter Display
					if (RoboBrakes_ParaEnabledCount != 0) {
						RoboBrakes_AeroDisplay = ("Enabled Aero: " + RoboBrakes_AeroEnabledCount.ToString () + " + " + RoboBrakes_ParaEnabledCount.ToString ());
					} else {
						RoboBrakes_AeroDisplay = ("Enabled Aero: " + RoboBrakes_AeroEnabledCount.ToString ());
					}
				} else {
					RoboBrakes_AeroDisplay = ("No Atmosphere!");
				}

				//Does this Vessel have at least one Enabled Part?
				if (RoboBrakes_GearEnabledCount == 0 && RoboBrakes_AeroEnabledCount == 0 && RoboBrakes_ParaEnabledCount == 0) {
					RoboBrakes_HASPARTENABLED = false;
					RoboBrakes_ARMED = false;
					RoboBrakes_ACTIVE = false;
					RoboBrakes_AeroDisplay = ("Enabled Aero: " + "<color=red>0</color>");
					RoboBrakes_GearDisplay = ("Enabled Gear: " + "<color=red>0</color>");
				} else {
					RoboBrakes_HASPARTENABLED = true;
				}

				//Throttle Zero Display
				if (RBSettings_ThrottleZeroing == true) {
					RBSettings_TZDisplay = "<b><color=#33CC33>Throttle 0</color></b>";
				} else {
					RBSettings_TZDisplay = "<b><color=#777777>Throttle 0</color></b>";
				}

				//Brake Override Display
				if (RBSettings_BKeyOff == true){
					RBSettings_BKDisplay = "<b><color=#777777>Override</color></b>";
				}
				if (RBSettings_BKeyPress == true) {
					RBSettings_BKDisplay = "<b><color=#FF9900>Override</color></b>";
				}
				if (RBSettings_BKeyToggle == true) {
					RBSettings_BKDisplay = "<b><color=#33CC33>Override</color></b>";
				}
			}

			//Default Displays if no Parts are available
			if ((CapablePartList.Count ()) == 0) {
				RoboBrakes_HASPARTENABLED = false;
				RoboBrakes_ARMED = false;
				RoboBrakes_ACTIVE = false;
				ActiveDisplay = "<b><color=#777777>Active</color></b>";
				RoboBrakes_AeroDisplay = ("Aero: No Capable Parts");
				RoboBrakes_GearDisplay = ("Gear: No Capable Parts");
				RBSettings_BKDisplay = "<b><color=#777777>'B'</color></b>";
				RBSettings_TZDisplay = "<b><color=#777777>TZ</color></b>";
			}

			//Reset Counters - These must be after everything else
			RoboBrakes_AeroEnabledCount = 0;
			RoboBrakes_GearEnabledCount = 0;
			RoboBrakes_ParaEnabledCount = 0;
			//Update the Toolbar Button depending on the current state of mod
			UpdateToolbarButton ();
		}

		//============================================================================================================================================
		//Called when the game is leaving the scene or exiting. Perform any clean up work here...
		internal void OnDestroy ()
		{
			SaveSettings ();
			RenderingManager.RemoveFromPostDrawQueue (0, OnDraw);
			GameEvents.onGUIApplicationLauncherReady.Remove (OnGUIApplicationLauncherReady);
			Timer100.Dispose ();
			//Remove Toolbar Button
			if (RoboBrakes_ToolbarButton != null) {
				ApplicationLauncher.Instance.RemoveModApplication (RoboBrakes_ToolbarButton);
			}
			print ("ROBOBRAKES - Bye bye...");
		}
		//============================================================================================================================================
		//Called when OnDestroy runs or manually if triggered
		public void SaveSettings ()
		{
			RoboBrakes_SystemSettings = new ConfigNode ();
			RoboBrakes_SystemSettings.AddValue ("MainGUI_WindowTop", MainGUI_WindowTop);
			RoboBrakes_SystemSettings.AddValue ("MainGUI_WindowLeft", MainGUI_WindowLeft);
			RoboBrakes_SystemSettings.AddValue ("SettingsGUI_WindowTop", SettingsGUI_WindowTop);
			RoboBrakes_SystemSettings.AddValue ("SettingsGUI_WindowLeft", SettingsGUI_WindowLeft);
			//--------------------------------------------------------------------------------------------------
			RoboBrakes_SystemSettings.AddValue ("DeactivationSpeed", DeactivationSpeed);
			RoboBrakes_SystemSettings.AddValue ("ActivationDelay", ActivationDelay);
			RoboBrakes_SystemSettings.AddValue ("RBSettings_BKeyPress", RBSettings_BKeyPress);
			RoboBrakes_SystemSettings.AddValue ("RBSettings_BKeyToggle", RBSettings_BKeyToggle);
			RoboBrakes_SystemSettings.AddValue ("RBSettings_BKeyOff", RBSettings_BKeyOff);
			RoboBrakes_SystemSettings.AddValue ("RBSettings_ThrottleZeroing", RBSettings_ThrottleZeroing);
			//--------------------------------------------------------------------------------------------------
			RoboBrakes_SystemSettings.AddValue ("RoboBrakes_AUTOGEAR", RoboBrakes_AUTOGEAR);
			RoboBrakes_SystemSettings.AddValue ("RoboBrakes_AUTOAERO", RoboBrakes_AUTOAERO);
			RoboBrakes_SystemSettings.AddValue ("RoboBrakes_AUTOCHUTE", RoboBrakes_AUTOCHUTE);
			RoboBrakes_SystemSettings.AddValue ("RoboBrakes_OVERRIDEGEAR", RoboBrakes_OVERRIDEGEAR);
			RoboBrakes_SystemSettings.AddValue ("RoboBrakes_OVERRIDEAERO", RoboBrakes_OVERRIDEAERO);
			RoboBrakes_SystemSettings.AddValue ("RoboBrakes_OVERRIDECHUTE", RoboBrakes_OVERRIDECHUTE);
			//--------------------------------------------------------------------------------------------------
			RoboBrakes_SystemSettings.Save ("GameData/RoboBrakes/Config/RoboBrakes_PluginSettings.cfg", "These are the default settings unless changed by the user...");
			print ("ROBOBRAKES - Settings Saved!");
		}
	}
}

