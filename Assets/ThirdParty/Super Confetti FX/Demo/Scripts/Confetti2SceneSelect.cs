using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Confetti2
{

public class Confetti2SceneSelect : MonoBehaviour
{
	public bool GUIHide = false;
	public bool GUIHide2 = false;
	public bool GUIHide3 = false;
	
    public void LoadConfettiDemo01()
    {
        SceneManager.LoadScene("confettifx_01");
    }
    public void LoadConfettiDemo02()
    {
        SceneManager.LoadScene("confettifx_02");
    }
	public void LoadConfettiDemo03()
    {
        SceneManager.LoadScene("confettifx_03");
    }
	public void LoadConfettiDemo04()
    {
        SceneManager.LoadScene("confettifx_04");
    }
	public void LoadConfettiDemo05()
    {
        SceneManager.LoadScene("confettifx_05");
    }
	public void LoadConfettiDemo06()
    {
        SceneManager.LoadScene("confettifx_06");
    }
	public void LoadConfettiDemo07()
    {
        SceneManager.LoadScene("confettifx_07");
    }
	public void LoadConfettiDemo08()
    {
        SceneManager.LoadScene("confettifx_08");
    }
	public void LoadConfettiDemo09()
    {
        SceneManager.LoadScene("confettifx_09");
    }
	void Update ()
	 {
 
     if((Keyboard.current?[Key.J].wasPressedThisFrame ?? false))
	 {
         GUIHide = !GUIHide;
     
         if (GUIHide)
		 {
             GameObject.Find("CanvasSceneSelect").GetComponent<Canvas> ().enabled = false;
         }
		 else
		 {
             GameObject.Find("CanvasSceneSelect").GetComponent<Canvas> ().enabled = true;
         }
     }
	      if((Keyboard.current?[Key.K].wasPressedThisFrame ?? false))
	 {
         GUIHide2 = !GUIHide2;
     
         if (GUIHide2)
		 {
             GameObject.Find("Canvas").GetComponent<Canvas> ().enabled = false;
         }
		 else
		 {
             GameObject.Find("Canvas").GetComponent<Canvas> ().enabled = true;
         }
     }
		if((Keyboard.current?[Key.L].wasPressedThisFrame ?? false))
	 {
         GUIHide3 = !GUIHide3;
     
         if (GUIHide3)
		 {
             GameObject.Find("CanvasTips").GetComponent<Canvas> ().enabled = false;
         }
		 else
		 {
             GameObject.Find("CanvasTips").GetComponent<Canvas> ().enabled = true;
         }
     }
}
}
}