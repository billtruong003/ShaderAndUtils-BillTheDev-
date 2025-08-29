using UnityEngine;
using System.Collections;

public class PlayManager : MonoBehaviour 
{
	public Animator[] playerGroup; 
	private string[] animClipNameGroup;
	private int currentNumber;

	// Use this for initialization
	void Start () {

		animClipNameGroup = new string[] {
            "easy_ChestCircle",
            "easy_UpdownStep",
            "hard_BasicCrossStep",
            "hard_BodywaveSide",
            "hard_BounceSlideStep",
            "hard_ChestCircle",
            "hard_Funcy",
            "hard_SoulStep",
            "hard_SoulStep3",
            "hard_SoulStep4",
            "hard_UpdownStep",
            "normal_BasicCrossStep",
            "normal_BounceSlideStep",
            "normal_ChestCircle_A",
            "normal_ChestCircle_B",
            "normal_Funcy",
            "normal_SoulStep2",
            "normal_SoulStep3",
            "normal_SoulStep4",
            "normal_UpdownStep"
        };

		currentNumber = 0;


		playerGroup = GameObject.Find ("PlayerGroup").transform.GetComponentsInChildren<Animator>();

		for (int i = 0; i < playerGroup.Length; i++)
		{
			playerGroup[i].speed = 1f;
			playerGroup[i].Play(animClipNameGroup[currentNumber]);
		}
	}
	

	void OnGUI()
	{
		// GUI 옵션
		GUIStyle textStyle = new GUIStyle();
		textStyle.fontSize = 15;
		textStyle.normal.textColor = Color.white;
		textStyle.hover.textColor = Color.red;


		//좌측이동
		if (GUI.Button(new Rect(50,50,50,50),"<"))
		{
			currentNumber--;

			if(currentNumber < 0 )
			{
				currentNumber = animClipNameGroup.Length - 1;
			}

			for(int i = 0; i < playerGroup.Length; i++)
			{
				playerGroup[i].speed = 1f;
				playerGroup[i].Play(animClipNameGroup[currentNumber]);
			}

		}
		//우측이동
		if(GUI.Button(new Rect(160,50,50,50),">"))
		{
			currentNumber++;

			if(currentNumber == animClipNameGroup.Length)
			{
				currentNumber = 0;
			}

			for(int i = 0; i < playerGroup.Length; i++)
			{
				playerGroup[i].speed = 1f;				
				playerGroup[i].Play(animClipNameGroup[currentNumber]);
			}
		}

		GUI.Label (new Rect(240, 50, 200,100), animClipNameGroup[currentNumber].ToString(), textStyle);
		
		// 현재/전체개수
		int totalCnt = animClipNameGroup.Length;
		string showMesssage = "("+(currentNumber+1) +"/" + totalCnt+")";
		GUI.Label(new Rect(240, 66, 200, 100), showMesssage);
	}
}
