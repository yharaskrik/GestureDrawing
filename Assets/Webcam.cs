using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class Webcam : MonoBehaviour 
{
	public WebCamDevice     myDevice;       // the webcam
	public WebCamTexture    camFeed;        // texture to process the webcam feed
	public Texture2D        camWindow;      // texture to display webcam feed
	public Texture2D        pixelWindow;    // texture to display color objects

	// target colours to recognize
	public Color            target1;
	public Color            target2;
	public Color target3;

	// thresholds for color variance between target color and display color in range [0.0-1.0]
	// recommend 0.1 as starting point (higher value captures more info)
	public float            threshold1 = 0.05f;
	public float            threshold2;
	public float threshold3;

	// Keeping track of lines being drawn
	private List<int> blackSpots;

	private int previousSpot = -1;

	public Color32[]        data;           // array that stores colour data from the webcam upon update

	// Use this for initialization
	void Start () 
	{
		// in case the machine has multiple devices, this loop searches for available webcam devices and prints out name of webcam being used
		WebCamDevice[] devices = WebCamTexture.devices;
		for (int i = 0; i < devices.Length; i++) 
		{
			Debug.Log (devices [i].name);
			myDevice = devices [i];         // currently set to the last device
		}

		// sets up camFeed as input feed from webcam and displays feed onto camWindo
		camFeed = new WebCamTexture (myDevice.name);
		camFeed.Play ();
		camWindow = new Texture2D (camFeed.width, camFeed.height);
		camWindow.SetPixels32 (camFeed.GetPixels32 ());
		camWindow.Apply();

		// creates pixelWindow to display colour tracking
		// note that actual feed is much bigger than what we display (saves processing)
		pixelWindow = new Texture2D (camFeed.width/8, camFeed.height/8);    // sets the size for pixelWindow

//		target1 = new Color (0.765f, 0.298f, 0.498f, 0.000f);
//		target1 = new Color(0.847f, 0.322f, 0.322f, 0.000f);
//		target1 = new Color (0.259f, 0.978f, 0.978f, 0.000f);
//		target2 = new Color (0.910f, 0.922f, 0.453f, 0.000f);

		//Sleeve colors
		target1 = new Color (0.188f, 0.608f, 0.263f, 0.000f);
		target2 = new Color (0.047f, 0.490f, 0.514f, 0.000f);
		target3 = new Color (0.725f, 0.443f, 0.227f, 0.000f);
		threshold2 = 0.01f;
		threshold1 = 0.04f;
		threshold3 = 0.01f;

		blackSpots = new List<int> ();

		Debug.Log (target1);

		Debug.Log ("size of camFeed = "     + camFeed.width     + ", " + camFeed.height);
		Debug.Log ("size of camWindow = "   + camWindow.width   + ", " + camWindow.height);
		Debug.Log ("size of pixelWindow = " + pixelWindow.width + ", " + pixelWindow.height);
	}

	// Update is called once per frame
	void Update () 
	{
		pixelWindow = new Texture2D (camFeed.width/8, camFeed.height/8);

		// gets the updated pixel data
		data = camWindow.GetPixels32 ();
		Array.Reverse (data);

		// if the data has been collected
		if(data.Length == 14400)   // use the data size that's shown in inspector
		{
			int totalXTarget1 = 0;
			int totalYTarget1 = 0;

			int countTarget1 = 0;

			bool target1Found = false;
			bool target2Found = false;
			bool target3Found = false;

			List<int> newSpots = new List<int> ();

			// goes through the data array
			for (int xy = 0; xy < data.Length; xy++) 
			{
				
				// checks if pixel colour matches the target colours within the selected threshold
				// if match, do something, else, change the pixel colour to white
				// note: to use one target set the target color to white and threshold to 0
				if (ColorSqrDistance(target1, data[xy]) < threshold1)
				{
					int xValueTarget1 = xy % camWindow.width;
					int yValueTarget1 = xy / camWindow.width;

					target1Found = true;

					totalXTarget1 += xValueTarget1;
					totalYTarget1 += yValueTarget1;
					countTarget1++;

					if (previousSpot == -1) {
						previousSpot = xy;
					} else {
						int previousX = xy % camWindow.width;
						int previousY = xy / camWindow.width;

						if (xy > 0 & xy < 14400)
							newSpots.Add (xy);

						int maxX = previousY * camWindow.width;
						int minX = (previousY - 1) * camWindow.width;
						int newSpot;
						if (previousX - 1 < maxX) {
							newSpot = ((previousY - 1) * camWindow.width + previousX - 1);
							if (newSpot > 0 & newSpot < 14400)
								newSpots.Add (newSpot);
						}
						if (previousX + 1 > minX) {
							newSpot = ((previousY - 1) * camWindow.width + previousX + 1);
							if (newSpot > 0 & newSpot < 14400)
								newSpots.Add (newSpot);
						}
						if (previousY + 1 < camWindow.height) {
							newSpot = ((previousY - 1) * camWindow.width + previousX + 1);
							if (newSpot > 0 & newSpot < 14400)
								newSpots.Add (newSpot);
						}
						if (previousY - 1 >= 0) {
							newSpot = ((previousY - 1) * camWindow.width + previousX - 1);
							if (newSpot > 0 & newSpot < 14400)
								newSpots.Add (newSpot);
						}
						
					}

				}
				else if(ColorSqrDistance(target2, data[xy]) < threshold2)
				{
					target2Found = true;

				}
				else if (ColorSqrDistance(target3, data[xy]) < threshold3)
				{
					data [xy] = target3;
					target3Found = true;
				}
				else 
				{
					data [xy] = Color.white;
				}
			}
			// If either the main brush or the clear screen target are not found. Clear the brush strokes
			if (!target2Found | !target1Found) {
				foreach (int xy in blackSpots)
					data [xy] = Color.white;
				blackSpots.Clear ();
			}
			// If all three are found, draw brush strokes
			else if (target1Found & target2Found & target3Found) {

				int averageXTarget1 = totalXTarget1 / countTarget1;
				int averageYTarget1 = totalYTarget1 / countTarget1;
				previousSpot = ((averageYTarget1 - 1) * camWindow.width) + averageXTarget1;
				if (previousSpot > 0)
					blackSpots.Add (previousSpot);

				Debug.Log (previousSpot);

				blackSpots.AddRange (newSpots);
				foreach (int xy in blackSpots) {
//					Debug.Log (xy);
					data [xy] = Color.black; 
				}

			} 
			// If any other combination , keep the brush strokes on screen but do not draw
			else {
				foreach (int xy in blackSpots) {
					//					Debug.Log (xy);
					data [xy] = Color.black; 
				}
			}

			pixelWindow.SetPixels32 (data);

			// apply color changes to texture
			pixelWindow.Apply ();
		}

		// update the texture showing webcam feed
		camWindow = new Texture2D (camFeed.width, camFeed.height);
		camWindow.SetPixels32 (camFeed.GetPixels32 ());
		camWindow.Apply ();
		TextureScale.Bilinear (camWindow, camWindow.width/8, camWindow.height/8);   // rescales texture
	}

	void OnGUI()
	{
		// draws textures onto the screen
		GUI.DrawTexture (new Rect (0,   0, (camWindow.width*2), (camWindow.height*2)), camWindow);     
		GUI.DrawTexture (new Rect (400, 0, (camWindow.width*4), (camWindow.height*4)), pixelWindow);


//		Debug.Log (target1);
//		Debug.Log (target2);
		Debug.Log(target3);
	}

	// compares two colours' squared distance
	float ColorSqrDistance(Color c1, Color c2) 
	{
		return ((c2.r - c1.r) * (c2.r - c1.r) + (c2.b - c1.b) * (c2.b - c1.b) + (c2.g - c1.g) * (c2.g - c1.g));
	}
}