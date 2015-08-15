
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
// Version 0.3 - Released 08.14.2015
// Version 0.2 - Released 08.03.2015
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
		private float SettingsGUI_WindowHeight = 250;
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
		//Deactivation Speed - 10 m/s by default
		private float DeactivationSpeed = 10.0f;
		//Activation Delay - 0.5 seconds by default
		private float ActivationDelay = 0.5f;
		//Brake Override Setting - Press & Hold enabled by default.
		private bool RBSettings_BKeyPress = true;
		private bool RBSettings_BKeyToggle = false;
		private bool RBSettings_BKeyOff = false;
		//Throttle Zero Setting - Enabled by default
		private bool RBSettings_ThrottleZeroing = true;
		private bool RBSettings_TZWaitForDelay = true;
		//Parachute Delay Setting - Enabled by default
		private bool RBSettings_DelayChute = true;
		//Enabled Part Classes - These get changed in settings.  ChuteOverride is disabled by default, all others are enabled.
		private bool RoboBrakes_GEARAUTO = true;
		private bool RoboBrakes_GEAROVERRIDE = true;
		private bool RoboBrakes_AEROAUTO = true;
		private bool RoboBrakes_AEROOVERRIDE = true;
		private bool RoboBrakes_CHUTEAUTO = true;
		private bool RoboBrakes_CHUTEOVERRIDE = false;

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
		private List <Part> EnabledPartList = new List <Part> ();
		private int RoboBrakes_GearEnabledCount;
		private int RoboBrakes_AeroEnabledCount;
		private int RoboBrakes_ParaEnabledCount;

		//Activation Stuff
		//---------------------------------------------------------------------------------------------------------------------
		private bool RoboBrakes_ARMED = false;
		private bool RoboBrakes_AUTOMATICBRAKE_ACTIVE;

		private bool RoboBrakes_OVERRIDEBRAKE_ACTIVE;

		private bool RoboBrakes_ACTIVE;
		private bool RoboBrakes_READYFORACTIVATION;
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
				print  ("ROBOBRAKES - Settings exist! Loading Values...");
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
				RBSettings_TZWaitForDelay = Boolean.Parse (RoboBrakes_SystemSettings.GetValue ("RBSettings_TZWaitForDelay"));
				RBSettings_DelayChute = Boolean.Parse (RoboBrakes_SystemSettings.GetValue ("RBSettings_DelayChute"));
				//--------------------------------------------------------------------------------------------------
				RoboBrakes_GEARAUTO = Boolean.Parse (RoboBrakes_SystemSettings.GetValue ("RoboBrakes_GEARAUTO"));
				RoboBrakes_AEROAUTO = Boolean.Parse (RoboBrakes_SystemSettings.GetValue ("RoboBrakes_AEROAUTO"));
				RoboBrakes_CHUTEAUTO = Boolean.Parse (RoboBrakes_SystemSettings.GetValue ("RoboBrakes_CHUTEAUTO"));
				RoboBrakes_GEAROVERRIDE = Boolean.Parse (RoboBrakes_SystemSettings.GetValue ("RoboBrakes_GEAROVERRIDE"));
				RoboBrakes_AEROOVERRIDE = Boolean.Parse (RoboBrakes_SystemSettings.GetValue ("RoboBrakes_AEROOVERRIDE"));
				RoboBrakes_CHUTEOVERRIDE = Boolean.Parse (RoboBrakes_SystemSettings.GetValue ("RoboBrakes_CHUTEOVERRIDE"));
				//--------------------------------------------------------------------------------------------------
			} else {
				print  ("ROBOBRAKES - Settings don't exist! Creating new file with built in defaults...");
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
			if (RoboBrakes_OVERRIDEBRAKE_ACTIVE == false) {
				if (RoboBrakes_ARMED == true) {
					if (RoboBrakes_AUTOMATICBRAKE_ACTIVE == true) {
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
						print  ("ROBOBRAKES - Cannot Arm System: No Eligible Parts are Enabled!");
					}
				} else {
					GUIContent ArmDisarmSwitch = new GUIContent ("<b><color=white>Arm/Disarm</color></b>");
					if (GUI.Button (new Rect (40, 20, 120, 22), ArmDisarmSwitch) == true) {
						RoboBrakes_ARMED = true;
						print  ("ROBOBRAKES - System Manually Armed!");
					}
				}
			}
			if (RoboBrakes_ARMED == true) {
				GUIContent ArmDisarmSwitch = new GUIContent ("<b><color=white>Arm/Disarm</color></b>");
				if (GUI.Button (new Rect (40, 20, 120, 22), ArmDisarmSwitch) == true) {
					RoboBrakes_ARMED = false;
					print  ("ROBOBRAKES - System Manually Disarmed!");
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
			//Brake Override Off
			if (RBSettings_BKeyOff) {
				GUIContent BSetting = new GUIContent ("<b><color=grey>Off</color></b>");
				if (GUI.Button (new Rect (130, 77, 65, 17), BSetting) == true) {
					RBSettings_BKeyOff = false;
					RBSettings_BKeyPress = true;
					RBSettings_BKeyToggle = false;
				}
				//Turn Brakes Off if they are engaged when modes are switched
				RoboBrakes_OVERRIDEBRAKE_ACTIVE = false;
			}
			//Brake Override Press/Hold
			if (RBSettings_BKeyPress) {
				GUIContent BSetting = new GUIContent ("<b><color=white>Hold</color></b>");
				if (GUI.Button (new Rect (130, 77, 65, 17), BSetting) == true) {
					RBSettings_BKeyOff = false;
					RBSettings_BKeyPress = false;
					RBSettings_BKeyToggle = true;
				}
			}
			//Brake Override Toggle
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
			//---------------------------------------------------------------------------------------------------------------------
			if (RBSettings_ThrottleZeroing == true) {
				GUI.Label (new Rect (25, 120, 120, 17), "<color=white>Delay Throttle 0: </color>");
			} else {
				//Show greyed out label if Throttle Zeroing isn't enabled
				GUI.Label (new Rect (25, 120, 120, 17), "<color=grey>Delay Throttle 0: </color>");
			}
			//Throttle Zeroing Wait for Delay On
			if (RBSettings_TZWaitForDelay == true) {
				if (RBSettings_ThrottleZeroing == true) {
					GUIContent ThrottleZSetting = new GUIContent ("<b><color=white>Yes</color></b>");
					if (GUI.Button (new Rect (130, 120, 65, 17), ThrottleZSetting) == true) {
						RBSettings_TZWaitForDelay = false;
					}
				} else {
					GUIContent ThrottleZSetting = new GUIContent ("<b><color=grey>Yes</color></b>");
					if (GUI.Button (new Rect (130, 120, 65, 17), ThrottleZSetting) == true) {
						RBSettings_TZWaitForDelay = false;
					}
				}
			}
			if (RBSettings_TZWaitForDelay == false) {
				if (RBSettings_ThrottleZeroing == true) {
					GUIContent ThrottleZSetting = new GUIContent ("<b><color=white>No</color></b>");
					if (GUI.Button (new Rect (130, 120, 65, 17), ThrottleZSetting) == true) {
						RBSettings_TZWaitForDelay = true;
					}
				} else {
					GUIContent ThrottleZSetting = new GUIContent ("<b><color=grey>No</color></b>");
					if (GUI.Button (new Rect (130, 120, 65, 17), ThrottleZSetting) == true) {
						RBSettings_TZWaitForDelay = true;
					}
				}
			}
			//Parachute Delay Toggle
			//---------------------------------------------------------------------------------------------------------------------
			if (RoboBrakes_CHUTEAUTO == true) {
				GUI.Label (new Rect (25, 142, 120, 17), "<color=white>Delay Parachute: </color>");
			} else {
				//Show greyed out label if Automatic Chute isn't enabled
				GUI.Label (new Rect (25, 142, 120, 17), "<color=grey>Delay Parachute: </color>");
			}
			//Parachute Wait for Delay On
			if (RBSettings_DelayChute == true) {
				if (RoboBrakes_CHUTEAUTO == true) {
					GUIContent ChuteDelaySetting = new GUIContent ("<b><color=white>Yes</color></b>");
					if (GUI.Button (new Rect (130, 142, 65, 17), ChuteDelaySetting) == true) {
						RBSettings_DelayChute = false;
					}
				} else {
					GUIContent ChuteDelaySetting = new GUIContent ("<b><color=grey>Yes</color></b>");
					if (GUI.Button (new Rect (130, 142, 65, 17), ChuteDelaySetting) == true) {
						RBSettings_DelayChute = false;
					}
				}
			}
			if (RBSettings_DelayChute == false) {
				if (RoboBrakes_CHUTEAUTO == true) {
					GUIContent ChuteDelaySetting = new GUIContent ("<b><color=white>No</color></b>");
					if (GUI.Button (new Rect (130, 142, 65, 17), ChuteDelaySetting) == true) {
						RBSettings_DelayChute = true;
					}
				} else {
					GUIContent ChuteDelaySetting = new GUIContent ("<b><color=grey>No</color></b>");
					if (GUI.Button (new Rect (130, 142, 65, 17), ChuteDelaySetting) == true) {
						RBSettings_DelayChute = true;
					}
				}
			}
	
			//Selection Grid
			//---------------------------------------------------------------------------------------------------------------------
			//Change these two values to move entire grid up/down/left/right
			SelectionGridX = 10;
			SelectionGridY = 165;
			GUI.Label (new Rect (SelectionGridX, SelectionGridY + 2, 200, 10), "<color=#222222>__________________________________</color>");
			GUI.Label (new Rect (SelectionGridX + 5, SelectionGridY + 20, 70, 17), "Automatic:");
			GUI.Label (new Rect (SelectionGridX + 15, SelectionGridY + 40, 70, 17), "Override:");
			GUI.Label (new Rect (SelectionGridX + 75, SelectionGridY + 5, 40, 17), "Gear");
			if (GUI.Toggle (new Rect (SelectionGridX + 82, SelectionGridY + 18, 17, 17), RoboBrakes_GEARAUTO, "") != RoboBrakes_GEARAUTO)
				RoboBrakes_GEARAUTO = !RoboBrakes_GEARAUTO;
			if (GUI.Toggle (new Rect (SelectionGridX + 82, SelectionGridY + 38, 17, 17), RoboBrakes_GEAROVERRIDE, "") != RoboBrakes_GEAROVERRIDE)
				RoboBrakes_GEAROVERRIDE = !RoboBrakes_GEAROVERRIDE;
			GUI.Label (new Rect (SelectionGridX + 118, SelectionGridY + 5, 40, 17), "Aero");
			if (GUI.Toggle (new Rect (SelectionGridX + 123, SelectionGridY + 18, 17, 17), RoboBrakes_AEROAUTO, "") != RoboBrakes_AEROAUTO)
				RoboBrakes_AEROAUTO = !RoboBrakes_AEROAUTO;
			if (GUI.Toggle (new Rect (SelectionGridX + 123, SelectionGridY + 38, 17, 17), RoboBrakes_AEROOVERRIDE, "") != RoboBrakes_AEROOVERRIDE)
				RoboBrakes_AEROOVERRIDE = !RoboBrakes_AEROOVERRIDE;
			GUI.Label (new Rect (SelectionGridX + 155, SelectionGridY + 5, 40, 17), "Chute");
			if (GUI.Toggle (new Rect (SelectionGridX + 165, SelectionGridY + 18, 17, 17), RoboBrakes_CHUTEAUTO, "") != RoboBrakes_CHUTEAUTO)
				RoboBrakes_CHUTEAUTO = !RoboBrakes_CHUTEAUTO;
			//if (GUI.Toggle (new Rect (SelectionGridX + 165, SelectionGridY + 38, 17, 17), RoboBrakes_CHUTEOVERRIDE, "") != RoboBrakes_CHUTEOVERRIDE)
			//RoboBrakes_CHUTEOVERRIDE = !RoboBrakes_CHUTEOVERRIDE;
			GUI.Label (new Rect (SelectionGridX, SelectionGridY + 56, 200, 10), "<color=#222222>__________________________________</color>");

			//Okay Button
			//---------------------------------------------------------------------------------------------------------------------
			GUIContent OkayClose = new GUIContent ("<b><color=white>Okay</color></b>");
			if (GUI.Button (new Rect (87, 228, 46, 17), OkayClose) == true) {
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
			print  ("ROBOBRAKES - Landing!");
			//Start Activation Delay Timer
			if (RoboBrakes_ARMED) {
				//Kill throttle if the delay is turned off
				if (RBSettings_TZWaitForDelay == false) {
					if (RBSettings_ThrottleZeroing == true) {
						FlightInputHandler.state.mainThrottle = 0;
					}
				}
				//Trigger Chute if the delay is turned off
				if (RBSettings_DelayChute == false) {
					RoboBrakes_CHUTEREADY = true;
				}
				//This starts TimerTime2 if the Activation Delay is greater than 0. If Activation delay is set to 0, timer is assumed to be done
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
			print  ("ROBOBRAKES - Activation " + TimerTime2.ToString ("N1") + "s after landing!");
			RoboBrakes_READYFORACTIVATION = true;
			//Trigger Chute when Timer Completes
			if (RBSettings_DelayChute == true) {
				RoboBrakes_CHUTEREADY = true;
			}
			//Set Throttle to Zero when the Timer completes, if that option is enabled
			if (RBSettings_TZWaitForDelay == true) {
				if (RBSettings_ThrottleZeroing == true) {
					FlightInputHandler.state.mainThrottle = 0;
				}
			}
		}

		private void DeactivationTrigger ()
		{
			RoboBrakes_CUTCHUTE = true;
			RoboBrakes_ARMED = false;
			RoboBrakes_AUTOMATICBRAKE_ACTIVE = false;
			RoboBrakes_READYFORACTIVATION = false;
			print  ("ROBOBRAKES - Automatically Deactivated and Disarmed at " + GroundSpeed.ToString ("N1") + "m/s!");
		}
			
		//This method runs every physics frame
		//============================================================================================================================================
		private void FixedUpdate ()
		{
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

			//---------------------------------------------------------------------------------------------------------------------
			//Check to see if we are landed
			if (FlightGlobals.ActiveVessel.Landed == true) {
				IsLanded = true;
				LandedNow = true;
			} else {
				IsLanded = false;
				LandedNow = false;
			}

			//---------------------------------------------------------------------------------------------------------------------
			//Check to see if Activation Delay has been reached
			if ((TimerTime2 >= ActivationDelay) && (TimerTime2Done == false)) {
				//Deactivate the timer
				TimerTime2Done = true;
				//Run the trigger
				TimerCompleteTrigger ();
			}
				
			//---------------------------------------------------------------------------------------------------------------------
			//Trigger for a Landing or Takeoff...
			if (LandedNow != LandedPrev) {
				LandedPrev = LandedNow;
				if (IsLanded == true) {
					LandingTrigger ();
				}
				if (IsLanded == false) {
					TakeoffTrigger ();
				}
			}
			//---------------------------------------------------------------------------------------------------------------------
			//Find our ground speed and convert to floating point
			GroundSpeed = Convert.ToSingle (FlightGlobals.ActiveVessel.srfSpeed);

			//---------------------------------------------------------------------------------------------------------------------
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
				print  ("ROBOBRAKES - Count Complete! Number of RoboBrakable parts is " + CapablePartList.Count ());
			}

			//---------------------------------------------------------------------------------------------------------------------
			//Check to see if there are any Robo Brake Capable Parts on the Vessel
			if (CapablePartList.Count () != 0) {
				//Cycle through eligible parts one-by-one
				foreach (Part CapablePart in CapablePartList) {
					//Create new Instance of ModuleRoboBrake to reference
					ModuleRoboBrakes MRB = new ModuleRoboBrakes ();
					MRB = CapablePart.FindModuleImplementing<ModuleRoboBrakes> ();
					//Check to see if RoboBrakes are enabled on this part - This checks the UI part right click KSPField
					if (MRB.RoboBrakeEnabled == true) {
						EnabledPartList.Add (CapablePart);
					}
				}
			}

			//Robo Brake Logic
			//============================================================================================================================================
			//Parameters for activating the brakes

			//---------------------------------------------------------------------------------------------------------------------
			//Override is in 'Hold' Mode
			if (RBSettings_BKeyPress == true) {
				if (Input.GetKey (KeyCode.B)) {
					RoboBrakes_OVERRIDEBRAKE_ACTIVE = true;
				} else {
					RoboBrakes_OVERRIDEBRAKE_ACTIVE = false;
				}
			}
			//---------------------------------------------------------------------------------------------------------------------
			//Override is in 'Toggle' Mode
			if (RBSettings_BKeyToggle == true) {
				if (Input.GetKeyDown (KeyCode.B)) {
					RoboBrakes_OVERRIDEBRAKE_ACTIVE = !RoboBrakes_OVERRIDEBRAKE_ACTIVE;
				}
			}

			//---------------------------------------------------------------------------------------------------------------------
			//Check to see if requirements are met for deactivation - THIS MUST GO BEFORE ACTIVATION REQUIREMENTS!
			if ((IsLanded == true) && (RoboBrakes_ARMED == true) && (RoboBrakes_AUTOMATICBRAKE_ACTIVE == true) && (GroundSpeed <= DeactivationSpeed)) {
				DeactivationTrigger ();
			}

			//---------------------------------------------------------------------------------------------------------------------
			//Check to see if requirements are met for activation
			if (IsLanded == true) {
				if (RoboBrakes_HASPARTENABLED == true) {
					if (GroundSpeed >= DeactivationSpeed) {
						if (RoboBrakes_ARMED == true && RoboBrakes_READYFORACTIVATION == true) {
							RoboBrakes_AUTOMATICBRAKE_ACTIVE = true;
						} else {
							RoboBrakes_AUTOMATICBRAKE_ACTIVE = false;
						}
					} else {
						RoboBrakes_AUTOMATICBRAKE_ACTIVE = false;
					}
				} else {
					RoboBrakes_AUTOMATICBRAKE_ACTIVE = false;
				}
			} else {
				RoboBrakes_AUTOMATICBRAKE_ACTIVE = false;
			}

			//---------------------------------------------------------------------------------------------------------------------
			//Engage each group of brakes
			EngageGear ();
			EngageAero ();
			EngageChute ();

			//---------------------------------------------------------------------------------------------------------------------
			//Disable all brakes and Disarm if no parts are enabled or found
			if ((RoboBrakes_GearEnabledCount == 0 && RoboBrakes_AeroEnabledCount == 0 && RoboBrakes_ParaEnabledCount == 0) | (CapablePartList.Count () == 0)) {
				RoboBrakes_HASPARTENABLED = false;
				RoboBrakes_ARMED = false;
				RoboBrakes_AUTOMATICBRAKE_ACTIVE = false;
				RoboBrakes_OVERRIDEBRAKE_ACTIVE = false;
			} else {
				RoboBrakes_HASPARTENABLED = true;
			}

			//Updating the GUI
			//============================================================================================================================================
			//Update Armed Status
			if (RoboBrakes_ARMED) {
				ArmedDisplay = "<b><color=#33CC33>Armed</color></b>";
			} else {
				ArmedDisplay = "<b><color=#777777>Armed</color></b>";
			}

			//Update Active Status if we are currently braking
			if (RoboBrakes_AUTOMATICBRAKE_ACTIVE | RoboBrakes_OVERRIDEBRAKE_ACTIVE) {
				ActiveDisplay = "<b><color=#33CC33>Active</color></b>";
			} else {
				ActiveDisplay = "<b><color=#777777>Active</color></b>";
			}
				
			//---------------------------------------------------------------------------------------------------------------------
			//Update Gear Display Counter
			RoboBrakes_GearDisplay = ("Enabled Gear: " + RoboBrakes_GearEnabledCount.ToString ());

			//---------------------------------------------------------------------------------------------------------------------
			//Update Aero Display Counter & Warn if Aero Modules are active and we are not in an Atmosphere
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

			//---------------------------------------------------------------------------------------------------------------------
			//Brake Override Display
			if (RBSettings_BKeyOff == true) {
				RBSettings_BKDisplay = "<b><color=#777777>Override</color></b>";
			}
			if (RBSettings_BKeyPress == true) {
				RBSettings_BKDisplay = "<b><color=#FF9900>Override</color></b>";
			}
			if (RBSettings_BKeyToggle == true) {
				RBSettings_BKDisplay = "<b><color=#33CC33>Override</color></b>";
			}

			//---------------------------------------------------------------------------------------------------------------------
			//Throttle Zero Display
			if (RBSettings_ThrottleZeroing == true) {
				RBSettings_TZDisplay = "<b><color=#33CC33>Throttle 0</color></b>";
			} else {
				RBSettings_TZDisplay = "<b><color=#777777>Throttle 0</color></b>";
			}

			//---------------------------------------------------------------------------------------------------------------------
			//Change Displays if no ENABLED parts are found
			if (RoboBrakes_GearEnabledCount == 0 && RoboBrakes_AeroEnabledCount == 0 && RoboBrakes_ParaEnabledCount == 0) {
				RoboBrakes_AeroDisplay = ("Enabled Aero: " + "<color=red>0</color>");
				RoboBrakes_GearDisplay = ("Enabled Gear: " + "<color=red>0</color>");
			}

			//---------------------------------------------------------------------------------------------------------------------
			//Default Displays if no CAPABLE Parts are found on the ship
			if ((CapablePartList.Count ()) == 0) {
				ActiveDisplay = "<b><color=#777777>Active</color></b>";
				RoboBrakes_AeroDisplay = ("Aero: No Capable Parts");
				RoboBrakes_GearDisplay = ("Gear: No Capable Parts");
				RBSettings_BKDisplay = "<b><color=#777777>Override</color></b>";
				RBSettings_TZDisplay = "<b><color=#777777>Throttle 0</color></b>";
			}

			//---------------------------------------------------------------------------------------------------------------------
			//Update the Toolbar Button depending on the current state of mod
			UpdateToolbarButton ();

			//---------------------------------------------------------------------------------------------------------------------
			//Reset Counters - This must be after everything else
			ResetCounters ();
		}

		//============================================================================================================================================
		public void EngageGear ()
		{
			//Look through each part that is enabled
			foreach (Part EnabledPart in EnabledPartList) {
				//Check to see if it is a certain module type
				//---------------------------------------------------------------------------------------------------------------------
				//Landing Gear Module
				if (EnabledPart.Modules.Contains ("ModuleLandingGear")) {
					//Create new Instance of ModuleLandingGear to reference
					ModuleLandingGear MLG = new ModuleLandingGear ();
					MLG = EnabledPart.FindModuleImplementing<ModuleLandingGear> ();
					//Increase count by 1 of enabled parts for GUI display
					RoboBrakes_GearEnabledCount++;
					//Create a list of actions that are available to this particular part & it's modules
					BaseActionList BAL = new BaseActionList (EnabledPart, MLG); 
					//Cycle throught each action
					foreach (BaseAction BA in BAL) {
						//Find the 'Brakes' action
						if (BA.guiName == "Brakes") {
							//Engage brakes (or other action) when triggered
							if ((RoboBrakes_AUTOMATICBRAKE_ACTIVE == true && RoboBrakes_GEARAUTO == true) | (RoboBrakes_OVERRIDEBRAKE_ACTIVE == true && RoboBrakes_GEAROVERRIDE == true)) {
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
				if (EnabledPart.Modules.Contains ("ModuleAdvancedLandingGear")) {
					ModuleAdvancedLandingGear MALG = new ModuleAdvancedLandingGear ();
					MALG = EnabledPart.FindModuleImplementing<ModuleAdvancedLandingGear> ();
					RoboBrakes_GearEnabledCount++;
					BaseActionList BAL = new BaseActionList (EnabledPart, MALG); 
					foreach (BaseAction BA in BAL) { 
						if (BA.guiName == "Brakes") {
							if ((RoboBrakes_AUTOMATICBRAKE_ACTIVE == true && RoboBrakes_GEARAUTO == true) | (RoboBrakes_OVERRIDEBRAKE_ACTIVE == true && RoboBrakes_GEAROVERRIDE == true)) {
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
				if (EnabledPart.Modules.Contains ("ModuleLandingGearFixed")) {
					ModuleLandingGearFixed MLGF = new ModuleLandingGearFixed ();
					MLGF = EnabledPart.FindModuleImplementing<ModuleLandingGearFixed> ();
					//These Gear are always down
					RoboBrakes_GearEnabledCount++;
					BaseActionList BAL = new BaseActionList (EnabledPart, MLGF); 
					foreach (BaseAction BA in BAL) { 
						if (BA.guiName == "Brakes") {
							if ((RoboBrakes_AUTOMATICBRAKE_ACTIVE == true && RoboBrakes_GEARAUTO == true) | (RoboBrakes_OVERRIDEBRAKE_ACTIVE == true && RoboBrakes_GEAROVERRIDE == true)) {
								KSPActionParam AP = new KSPActionParam (KSPActionGroup.None, KSPActionType.Activate); 
								BA.Invoke (AP);
							} else {
								KSPActionParam AP = new KSPActionParam (KSPActionGroup.None, KSPActionType.Deactivate); 
								BA.Invoke (AP); 
							}
						}
					}
				}
			}
		}
		//============================================================================================================================================
		public void EngageAero ()
		{
			//FAR Compatibility =)
			foreach (Part EnabledPart in EnabledPartList) {
				if (EnabledPart.Modules.Contains ("FARControllableSurface")) {
					ferram4.FARControllableSurface FCS = new ferram4.FARControllableSurface ();
					FCS = EnabledPart.FindModuleImplementing<ferram4.FARControllableSurface> ();
					if (FCS.isSpoiler) {
						RoboBrakes_AeroEnabledCount++;
						BaseActionList BAL = new BaseActionList (EnabledPart, FCS); 
						foreach (BaseAction BA in BAL) { 
							if ((RoboBrakes_AUTOMATICBRAKE_ACTIVE == true && RoboBrakes_AEROAUTO == true) | (RoboBrakes_OVERRIDEBRAKE_ACTIVE == true && RoboBrakes_AEROOVERRIDE == true)) {
								if (BA.guiName == "Activate Spoiler") {
									KSPActionParam AP = new KSPActionParam (KSPActionGroup.None, KSPActionType.Activate); 
									BA.Invoke (AP);
								}
							} else {
								if (BA.guiName == "Activate Spoiler") {
									KSPActionParam AP = new KSPActionParam (KSPActionGroup.None, KSPActionType.Deactivate); 
									BA.Invoke (AP); 
								}
							}
						}
					}
				}
			}
			foreach (Part EnabledPart in EnabledPartList) {
				//---------------------------------------------------------------------------------------------------------------------
				//Control Surface Module
				if (EnabledPart.Modules.Contains ("ModuleControlSurface")) {
					ModuleControlSurface MCS = new ModuleControlSurface ();
					MCS = EnabledPart.FindModuleImplementing<ModuleControlSurface> ();
					RoboBrakes_AeroEnabledCount++;
					BaseActionList BAL = new BaseActionList (EnabledPart, MCS); 
					foreach (BaseAction BA in BAL) { 
						if ((RoboBrakes_AUTOMATICBRAKE_ACTIVE == true && RoboBrakes_AEROAUTO == true) | (RoboBrakes_OVERRIDEBRAKE_ACTIVE == true && RoboBrakes_AEROOVERRIDE == true)) {
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
				if (EnabledPart.Modules.Contains ("ModuleAeroSurface")) {
					ModuleAeroSurface MAS = new ModuleAeroSurface ();
					MAS = EnabledPart.FindModuleImplementing<ModuleAeroSurface> ();
					RoboBrakes_AeroEnabledCount++;
					BaseActionList BAL = new BaseActionList (EnabledPart, MAS); 
					foreach (BaseAction BA in BAL) { 
						if ((RoboBrakes_AUTOMATICBRAKE_ACTIVE == true && RoboBrakes_AEROAUTO == true) | (RoboBrakes_OVERRIDEBRAKE_ACTIVE == true && RoboBrakes_AEROOVERRIDE == true)) {
							if (BA.guiName == "Extend") {
								KSPActionParam AP = new KSPActionParam (KSPActionGroup.None, KSPActionType.Activate); 
								BA.Invoke (AP);
							}
						} else {
							if (BA.guiName == "Retract") {
								KSPActionParam AP = new KSPActionParam (KSPActionGroup.None, KSPActionType.Activate); 
								BA.Invoke (AP); 
								RoboBrakes_ACTIVE = false;
							}
						}
					}
				}
			}
		}
		//============================================================================================================================================
		public void EngageChute ()
		{
			//FAR Compatibility =)
			foreach (Part EnabledPart in EnabledPartList) {
				if (EnabledPart.Modules.Contains ("RealChuteFAR")) {
					FerramAerospaceResearch.RealChuteLite.RealChuteFAR RCF = new FerramAerospaceResearch.RealChuteLite.RealChuteFAR ();
					RCF = EnabledPart.FindModuleImplementing<FerramAerospaceResearch.RealChuteLite.RealChuteFAR> ();
					RoboBrakes_ParaEnabledCount++;
					if ((RCF.deploymentState.Equals (FerramAerospaceResearch.RealChuteLite.RealChuteFAR.DeploymentStates.CUT)) && (IsLanded == false) && (RoboBrakes_CHUTEAUTO == true)) {
						//Bypassing RealChutes Repacking Method so we don't have to EVA
						RCF.part.Effect ("rcrepack");
						RCF.part.stackIcon.SetIconColor (XKCDColors.White);
						RCF.deploymentState = FerramAerospaceResearch.RealChuteLite.RealChuteFAR.DeploymentStates.STOWED;
						RCF.part.DragCubes.SetCubeWeight ("PACKED", 1);
						RCF.part.DragCubes.SetCubeWeight ("RCDEPLOYED", 0);
						print  ("ROBOBRAKES - RealChute " + EnabledPart.name + " was already Cut! Repacked Automatically!");
					}
					//Deploy Chute
					if ((RoboBrakes_CHUTEREADY == true && RoboBrakes_CHUTEAUTO == true)) {
							RoboBrakes_CHUTEREADY = false;
							//Deploy Real Chute
							RCF.ActivateRC ();
					}
					//Repack Chute
					if (RCF.deploymentState.Equals (FerramAerospaceResearch.RealChuteLite.RealChuteFAR.DeploymentStates.DEPLOYED | FerramAerospaceResearch.RealChuteLite.RealChuteFAR.DeploymentStates.PREDEPLOYED)) {
						if (RoboBrakes_CUTCHUTE == true) {
							RoboBrakes_CUTCHUTE = false;
							//Cut Real Chute
							RCF.Cut ();
							//Bypassing RealChutes Repacking Method so we don't have to EVA
							RCF.part.Effect ("rcrepack");
							RCF.part.stackIcon.SetIconColor (XKCDColors.White);
							RCF.deploymentState = FerramAerospaceResearch.RealChuteLite.RealChuteFAR.DeploymentStates.STOWED;
							RCF.part.DragCubes.SetCubeWeight ("PACKED", 1);
							RCF.part.DragCubes.SetCubeWeight ("RCDEPLOYED", 0);
							print  ("ROBOBRAKES - RealChute " + EnabledPart.name + " was Cut & Repacked Automatically!");
						}
					}
				}
			}
			foreach (Part EnabledPart in EnabledPartList) {
				//Module Parachutes
				//---------------------------------------------------------------------------------------------------------------------
				if (EnabledPart.Modules.Contains ("ModuleParachute")) {
					ModuleParachute MPA = new ModuleParachute ();
					MPA = EnabledPart.FindModuleImplementing<ModuleParachute> ();
					RoboBrakes_ParaEnabledCount++;
					//Repack the Chute automatically if it has been manually cut
					if ((MPA.deploymentState.Equals (ModuleParachute.deploymentStates.CUT)) && (IsLanded == false) && (RoboBrakes_CHUTEAUTO == true)) {
						MPA.Repack ();
						print  ("ROBOBRAKES - Chute " + EnabledPart.name + " was already Cut! Repacked Automatically!");
					}
					//Deploy Chute
					if ((RoboBrakes_AUTOMATICBRAKE_ACTIVE == true && RoboBrakes_CHUTEAUTO == true)) {
						if (RoboBrakes_CHUTEREADY == true) {
							RoboBrakes_CHUTEREADY = false;
							MPA.Deploy ();
						}
					}
					//Repack Chute
					if (MPA.deploymentState.Equals (ModuleParachute.deploymentStates.DEPLOYED)) {
						if (RoboBrakes_CUTCHUTE == true) {
							RoboBrakes_CUTCHUTE = false;
							MPA.CutParachute ();
							MPA.Repack ();
							print  ("ROBOBRAKES - Chute " + EnabledPart.name + " was Cut & Repacked Automatically!");
						}
					}
				}
			}
		}
		//============================================================================================================================================
		//Resets various counters and lists when called
		private void ResetCounters ()
		{
			RoboBrakes_AeroEnabledCount = 0;
			RoboBrakes_GearEnabledCount = 0;
			RoboBrakes_ParaEnabledCount = 0;
			EnabledPartList.Clear ();
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
			print  ("ROBOBRAKES - Bye bye...");
		}

		//============================================================================================================================================
		//Called manually if triggered by user.
		public void LoadDefaultSettings ()
		{
			MainGUI_WindowTop = 5;
			MainGUI_WindowLeft = ((Screen.width / 2) + 150);
			SettingsGUI_WindowTop = 190;
			SettingsGUI_WindowLeft = ((Screen.width / 2) + 150);
			//Deactivation Speed - 10 m/s by default
			DeactivationSpeed = 10.0f;
			//Activation Delay - 0.5 seconds by default
			ActivationDelay = 0.5f;
			//Brake Override Setting - Press & Hold enabled by default.
			RBSettings_BKeyPress = true;
			RBSettings_BKeyToggle = false;
			RBSettings_BKeyOff = false;
			//Throttle Zero Setting - Enabled by default
			RBSettings_ThrottleZeroing = true;
			RBSettings_TZWaitForDelay = true;
			//Parachute Delay Setting - Enabled by default
			RBSettings_DelayChute = true;
			//Enabled Part Classes - These get changed in settings.  ChuteOverride is disabled by default, all others are enabled.
			RoboBrakes_GEARAUTO = true;
			RoboBrakes_GEAROVERRIDE = true;
			RoboBrakes_AEROAUTO = true;
			RoboBrakes_AEROOVERRIDE = true;
			RoboBrakes_CHUTEAUTO = true;
			RoboBrakes_CHUTEOVERRIDE = false;
			SaveSettings ();
			print  ("ROBOBRAKES - Default Settings Loaded and Settings Saved!");
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
			RoboBrakes_SystemSettings.AddValue ("RBSettings_TZWaitForDelay", RBSettings_TZWaitForDelay);
			RoboBrakes_SystemSettings.AddValue ("RBSettings_DelayChute", RBSettings_DelayChute);
			//--------------------------------------------------------------------------------------------------
			RoboBrakes_SystemSettings.AddValue ("RoboBrakes_GEARAUTO", RoboBrakes_GEARAUTO);
			RoboBrakes_SystemSettings.AddValue ("RoboBrakes_AEROAUTO", RoboBrakes_AEROAUTO);
			RoboBrakes_SystemSettings.AddValue ("RoboBrakes_CHUTEAUTO", RoboBrakes_CHUTEAUTO);
			RoboBrakes_SystemSettings.AddValue ("RoboBrakes_GEAROVERRIDE", RoboBrakes_GEAROVERRIDE);
			RoboBrakes_SystemSettings.AddValue ("RoboBrakes_AEROOVERRIDE", RoboBrakes_AEROOVERRIDE);
			RoboBrakes_SystemSettings.AddValue ("RoboBrakes_CHUTEOVERRIDE", RoboBrakes_CHUTEOVERRIDE);
			//--------------------------------------------------------------------------------------------------
			RoboBrakes_SystemSettings.Save ("GameData/RoboBrakes/Config/RoboBrakes_PluginSettings.cfg", "These are the default settings unless changed by the user...");
			print  ("ROBOBRAKES - Settings Saved!");
		}
	}
}

