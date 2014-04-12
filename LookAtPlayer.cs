using UnityEngine;
using System.Collections;


	public class LookAtPlayer : MonoBehaviour
	{
	public string url = "http://www.gunnerkrigg.com/comics/00001339.jpg";		public Camera m_Camera;
	IEnumerator  Start(){
			WWW www = new WWW(url);
			yield return www;
			renderer.material.mainTexture = www.texture;
		}
		void Update()
		{
			transform.LookAt(transform.position + m_Camera.transform.rotation * -Vector3.back,
			                 m_Camera.transform.rotation * Vector3.up);
		}
	}