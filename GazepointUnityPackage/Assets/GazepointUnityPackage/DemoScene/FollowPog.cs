/*========================================================================================
    MyMonoBehaviour
	
    ... Description goes here ...
	
    Copyright 2018 Erick Fernandez de Arteaga. All rights reserved.
        https://www.linkedin.com/in/erick-fda
        https://github.com/erick-fda
        https://bitbucket.org/erick-fda
	
========================================================================================*/

using UnityEngine;
using GazepointUnity;

public class FollowPog : MonoBehaviour
{
	/*----------------------------------------------------------------------------------------
		Instance Fields
	----------------------------------------------------------------------------------------*/
	[SerializeField] private GazepointClient _eyeTracker;
    
	/*----------------------------------------------------------------------------------------
		Instance Properties
	----------------------------------------------------------------------------------------*/
	
    
	/*----------------------------------------------------------------------------------------
		MonoBehaviour Methods
	----------------------------------------------------------------------------------------*/    
    private void Update()
    {
        Debug.Log(string.Format("BPOGX: {0}, BPOGY: {1}, BPOGV: {2}", _eyeTracker.BestPogX, 
            _eyeTracker.BestPogY, _eyeTracker.BestPogValid));

        float newX = Screen.width * _eyeTracker.BestPogX;
        float newY = Screen.height * _eyeTracker.BestPogY; // TODO Flip

        gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(newX, newY);
    }
    
	/*----------------------------------------------------------------------------------------
		Instance Methods
	----------------------------------------------------------------------------------------*/
	
}
