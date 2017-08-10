/*
 * Proland: a procedural landscape rendering library.
 * Copyright (c) 2008-2011 INRIA
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 * Proland is distributed under a dual-license scheme.
 * You can obtain a specific license from Inria: proland-licensing@inria.fr.
 *
 * Authors: Eric Bruneton, Antoine Begault, Guillaume Piolat.
 * Modified and ported to Unity by Justin Hawkins 2014
 * Image effect modifications by Boris Novikov 2014
 * 
 * 
 */

using UnityEngine;
using System.Collections;

public class AtmosphericPP : MonoBehaviour 
{
	private bool CP;
	private Vector3 sunRot;
	private float preprocessTime;
	//private Rect guiRect = new Rect(Screen.width/2-Screen.width*0.05f,0,Mathf.Max(300.0f, Screen.width*0.2f), 460.0f);

	public Light sunLight;
	public float _MinFogDistance = 500.0f;

	public float _Exposure = 1.0f;
	public Texture2D sunglare;
	public float earthRadius = 6360000.0f;
	public Vector3 origin = new Vector3(0,-6362000.0f,0);

	//[SerializeField]
	//ComputeShader m_writeData;
	
	//[SerializeField]
	//ComputeShader m_readData;

	//Dont change these
	const int NUM_THREADS = 8;
	const int READ = 0;
	const int WRITE = 1;

    //The radius of the planet (Rg), radius of the atmosphere (Rt)
    const float Rg = 6360000.0f;
    const float Rt = 6420000.0f;
    const float RL = 6421000.0f;

    //const float Rg = 13000.0f;
    //const float Rt = 15000.0f;
    //const float RL = 15500.0f;

    //Dimensions of the tables
    const int TRANSMITTANCE_W = 256;
	const int TRANSMITTANCE_H = 64;
	const int SKY_W = 64;
	const int SKY_H = 16;
	const int RES_R = 32;
	const int RES_MU = 128;
	const int RES_MU_S = 32;
	const int RES_NU = 8;

	//Physical settings, Mie and Rayliegh values
	const float AVERAGE_GROUND_REFLECTANCE = 0.1f;

	//Half heights for the atmosphere air density (HR) and particle density (HM)
	//This is the height in km that half the particles are found below
    [Tooltip("Height (km) at which half of the atmospheres air is found below")]
	public float HR = 8.0f;
    [Tooltip("Height (km) at which half of the particles in air are found below")]
    public float HM = 1.2f;

	//scatter coefficient for mie
	[SerializeField]
	Vector3 BETA_MSca = new Vector3(4e-3f,4e-3f,4e-3f);

	[SerializeField]
	Vector3 m_betaR = new Vector3(5.8e-3f, 1.35e-2f, 3.31e-2f);
	//Asymmetry factor for the mie phase function
	//A higher number means more light is scattered in the forward direction
	[SerializeField]
	float m_mieG = 0.85f;
	
	string m_filePath = "/Proland/Textures/Atmo";
	
	Mesh m_mesh;
	
	//RenderTexture rt_transmittance, rt_inscatter, rt_irradiance, rt_skyMap;

	RenderTexture rt_transmittanceT, rt_skyMap;
	RenderTexture rt_deltaET, rt_deltaSRT, rt_deltaSMT, rt_deltaJT;
	RenderTexture[] rt_irradianceT, rt_inscatterT;

	public ComputeShader m_copyInscatter1, m_copyInscatterN, m_copyIrradiance;
	public ComputeShader m_inscatter1, m_inscatterN, m_inscatterS;
	public ComputeShader m_irradiance1, m_irradianceN, m_transmittance;
	//public ComputeShader m_readData;
	//public Material m_skyMapMaterial;

	int m_step, m_order;
	bool m_finished = false;

	public Shader skybox_shader, inscatter_shader;
	private Material skybox_material, inscatter_material;
	private Camera m_cam;

	private Quaternion savedSunRotation;

	void Start()
	{
        // finds first directional light
		if (sunLight == null){
			foreach (Light l in GameObject.FindObjectsOfType<Light>()){
				if (l.type == LightType.Directional){
					sunLight = l;
					break;
				}
			}
		}
		sunRot = sunLight.transform.rotation.eulerAngles;
		
		skybox_material = new Material(skybox_shader);
		inscatter_material = new Material(inscatter_shader);
		m_cam = gameObject.GetComponent<Camera>();
		m_cam.depthTextureMode = DepthTextureMode.DepthNormals;

		//LoadSkyMaps();
		StartPreprocess();

		RenderSettings.skybox = skybox_material;
	}

	void StartPreprocess()
	{
		ReleaseRT(true);

		rt_irradianceT = new RenderTexture[2];
		rt_inscatterT = new RenderTexture[2];

		rt_transmittanceT =  RenderTexture.GetTemporary(TRANSMITTANCE_W, TRANSMITTANCE_H, 0, RenderTextureFormat.ARGBFloat);
		rt_transmittanceT.enableRandomWrite = true;
		rt_transmittanceT.Create();
		
		rt_irradianceT[0] =  RenderTexture.GetTemporary(SKY_W, SKY_H, 0, RenderTextureFormat.ARGBFloat);
		rt_irradianceT[0].enableRandomWrite = true;
		rt_irradianceT[0].Create();
		
		rt_irradianceT[1] =  RenderTexture.GetTemporary(SKY_W, SKY_H, 0, RenderTextureFormat.ARGBFloat);
		rt_irradianceT[1].enableRandomWrite = true;
		rt_irradianceT[1].Create();
		
		rt_inscatterT[0] =  RenderTexture.GetTemporary(RES_MU_S * RES_NU, RES_MU, 0, RenderTextureFormat.ARGBFloat);
		rt_inscatterT[0].isVolume = true;
		rt_inscatterT[0].enableRandomWrite = true;
		rt_inscatterT[0].volumeDepth = RES_R;
		rt_inscatterT[0].Create();
		
		rt_inscatterT[1] =  RenderTexture.GetTemporary(RES_MU_S * RES_NU, RES_MU, 0, RenderTextureFormat.ARGBFloat);
		rt_inscatterT[1].isVolume = true;
		rt_inscatterT[1].enableRandomWrite = true;
		rt_inscatterT[1].volumeDepth = RES_R;
		rt_inscatterT[1].Create();
		
		rt_deltaET =  RenderTexture.GetTemporary(SKY_W, SKY_H, 0, RenderTextureFormat.ARGBFloat);
		rt_deltaET.enableRandomWrite = true;
		rt_deltaET.Create();
		
		rt_deltaSRT =  RenderTexture.GetTemporary(RES_MU_S * RES_NU, RES_MU, 0, RenderTextureFormat.ARGBFloat);
		rt_deltaSRT.isVolume = true;
		rt_deltaSRT.enableRandomWrite = true;
		rt_deltaSRT.volumeDepth = RES_R;
		rt_deltaSRT.Create();
		
		rt_deltaSMT =  RenderTexture.GetTemporary(RES_MU_S * RES_NU, RES_MU, 0, RenderTextureFormat.ARGBFloat);
		rt_deltaSMT.isVolume = true;
		rt_deltaSMT.enableRandomWrite = true;
		rt_deltaSMT.volumeDepth = RES_R;
		rt_deltaSMT.Create();
		
		rt_deltaJT =  RenderTexture.GetTemporary(RES_MU_S * RES_NU, RES_MU, 0, RenderTextureFormat.ARGBFloat);
		rt_deltaJT.isVolume = true;
		rt_deltaJT.enableRandomWrite = true;
		rt_deltaJT.volumeDepth = RES_R;
		rt_deltaJT.Create();

		float time = Time.realtimeSinceStartup;

		SetParameters(m_copyInscatter1);
		SetParameters(m_copyInscatterN);
		SetParameters(m_copyIrradiance);
		SetParameters(m_inscatter1);
		SetParameters(m_inscatterN);
		SetParameters(m_inscatterS);
		SetParameters(m_irradiance1);
		SetParameters(m_irradianceN);
		SetParameters(m_transmittance);

		m_finished = false;
		m_step = 0;
		m_order = 2;
		
		RTUtility.ClearColor(rt_irradianceT);

		while(!m_finished) {
			Preprocess();
		}	

		time = Time.realtimeSinceStartup-time;
		Debug.Log("preprocess finished in "+time*1000.0f+"ms");
		preprocessTime = time;

		InitUniforms(skybox_material);
		InitUniforms(inscatter_material);

		RenderSettings.skybox = skybox_material;
	}

	void SetParameters(ComputeShader mat)
	{
		mat.SetFloat("Rg", Rg/1000.0f);
		mat.SetFloat("Rt", Rt/1000.0f);
		mat.SetFloat("RL", RL/1000.0f);
		mat.SetInt("TRANSMITTANCE_W", TRANSMITTANCE_W);
		mat.SetInt("TRANSMITTANCE_H", TRANSMITTANCE_H);
		mat.SetInt("SKY_W", SKY_W);
		mat.SetInt("SKY_H", SKY_H);
		mat.SetInt("RES_R", RES_R);
		mat.SetInt("RES_MU", RES_MU);
		mat.SetInt("RES_MU_S", RES_MU_S);
		mat.SetInt("RES_NU", RES_NU);
		mat.SetFloat("AVERAGE_GROUND_REFLECTANCE", AVERAGE_GROUND_REFLECTANCE);
		mat.SetFloat("HR", HR);
		mat.SetFloat("HM", HM);
		mat.SetVector("betaR", m_betaR);
		mat.SetVector("betaMSca", BETA_MSca);
		mat.SetVector("betaMEx", BETA_MSca / 0.9f);
		mat.SetFloat("mieG", Mathf.Clamp(m_mieG, 0.0f, 0.99f));
	}

	/*void LoadSkyMaps() 
  	{
	    skybox_material = new Material(skybox_shader);
	    m_cam = gameObject.GetComponent<Camera>();
	    m_cam.depthTextureMode = DepthTextureMode.DepthNormals;

		//Transmittance is responsible for the change in the sun color as it moves
		//The raw file is a 2D array of 32 bit floats with a range of 0 to 1
		string path = Application.dataPath + m_filePath + "/transmittance.raw";
		
		rt_transmittanceT = new RenderTexture(TRANSMITTANCE_W, TRANSMITTANCE_H, 0, RenderTextureFormat.ARGBFloat);
		rt_transmittanceT.wrapMode = TextureWrapMode.Clamp;
		rt_transmittanceT.filterMode = FilterMode.Bilinear;
		rt_transmittanceT.enableRandomWrite = true;
		rt_transmittanceT.Create();
		
		ComputeBuffer buffer = new ComputeBuffer(TRANSMITTANCE_W*TRANSMITTANCE_H, sizeof(float)*3);
		CBUtility.WriteIntoRenderTexture(rt_transmittanceT, 3, path, buffer, m_writeData);
		buffer.Release();
		
		//Iirradiance is responsible for the change in the sky color as the sun moves
		//The raw file is a 2D array of 32 bit floats with a range of 0 to 1
		path = Application.dataPath + m_filePath + "/irradiance.raw";
		
		rt_irradianceT[READ] = new RenderTexture(SKY_W, SKY_H, 0, RenderTextureFormat.ARGBFloat);
		rt_irradianceT[READ].wrapMode = TextureWrapMode.Clamp;
		rt_irradianceT[READ].filterMode = FilterMode.Bilinear;
		rt_irradianceT[READ].enableRandomWrite = true;
		rt_irradianceT[READ].Create();
		
		buffer = new ComputeBuffer(SKY_W*SKY_H, sizeof(float)*3);
		CBUtility.WriteIntoRenderTexture(rt_irradianceT[READ], 3, path, buffer, m_writeData);
		buffer.Release();
		
		//Inscatter is responsible for the change in the sky color as the sun moves
		//The raw file is a 4D array of 32 bit floats with a range of 0 to 1.589844
		//As there is not such thing as a 4D texture the data is packed into a 3D texture 
		//and the shader manually performs the sample for the 4th dimension
		path = Application.dataPath + m_filePath + "/inscatter.raw";
		
		rt_inscatterT[READ] = new RenderTexture(RES_MU_S * RES_NU, RES_MU, 0, RenderTextureFormat.ARGBFloat);
		rt_inscatterT[READ].volumeDepth = RES_R;
		rt_inscatterT[READ].wrapMode = TextureWrapMode.Clamp;
		rt_inscatterT[READ].filterMode = FilterMode.Bilinear;
		rt_inscatterT[READ].isVolume = true;
		rt_inscatterT[READ].enableRandomWrite = true;
		rt_inscatterT[READ].Create();
		
		buffer = new ComputeBuffer(RES_MU_S*RES_NU*RES_MU*RES_R, sizeof(float)*4);
		CBUtility.WriteIntoRenderTexture(rt_inscatterT[READ], 4, path, buffer, m_writeData);
		buffer.Release();
		
		InitUniforms(skybox_material);
	}
	*/

	void Preprocess()
	{
		if (m_step == 0) 
		{
			// computes transmittance texture T (line 1 in algorithm 4.1)
			m_transmittance.SetTexture(0, "transmittanceWrite", rt_transmittanceT);
			m_transmittance.Dispatch(0, TRANSMITTANCE_W/NUM_THREADS, TRANSMITTANCE_H/NUM_THREADS, 1);
			 
		} 
		else if (m_step == 1) 
		{
			// computes irradiance texture deltaE (line 2 in algorithm 4.1)
			m_irradiance1.SetTexture(0, "transmittanceRead", rt_transmittanceT);
			m_irradiance1.SetTexture(0, "deltaEWrite", rt_deltaET);
			m_irradiance1.Dispatch(0, SKY_W/NUM_THREADS, SKY_H/NUM_THREADS, 1);

			 
			//if(WRITE_DEBUG_TEX)
			//	SaveAs8bit(SKY_W, SKY_H, 4, "/deltaE_debug", rt_deltaET);
		} 
		else if (m_step == 2) 
		{
			// computes single scattering texture deltaS (line 3 in algorithm 4.1)
			// Rayleigh and Mie separated in deltaSR + deltaSM
			m_inscatter1.SetTexture(0, "transmittanceRead", rt_transmittanceT);
			m_inscatter1.SetTexture(0, "deltaSRWrite", rt_deltaSRT);
			m_inscatter1.SetTexture(0, "deltaSMWrite", rt_deltaSMT);
			
			//The inscatter calc's can be quite demanding for some cards so process 
			//the calc's in layers instead of the whole 3D data set.
			for(int i = 0; i < RES_R; i++) {
				m_inscatter1.SetInt("layer", i);
				m_inscatter1.Dispatch(0, (RES_MU_S*RES_NU)/NUM_THREADS, RES_MU/NUM_THREADS, 1);
			}
			 
			/*if(WRITE_DEBUG_TEX)
				SaveAs8bit(RES_MU_S*RES_NU, RES_MU*RES_R, 4, "/deltaSR_debug", rt_deltaSRT);
			
			if(WRITE_DEBUG_TEX)
				SaveAs8bit(RES_MU_S*RES_NU, RES_MU*RES_R, 4, "/deltaSM_debug", rt_deltaSMT);
			*/
		} 
		else if (m_step == 3) 
		{
			// copies deltaE into irradiance texture E (line 4 in algorithm 4.1)
			m_copyIrradiance.SetFloat("k", 0.0f);
			m_copyIrradiance.SetTexture(0, "deltaERead", rt_deltaET);
			m_copyIrradiance.SetTexture(0, "irradianceRead", rt_irradianceT[READ]);
			m_copyIrradiance.SetTexture(0, "irradianceWrite", rt_irradianceT[WRITE]);
			m_copyIrradiance.Dispatch(0, SKY_W/NUM_THREADS, SKY_H/NUM_THREADS, 1);
			
			RTUtility.Swap(rt_irradianceT);
			 
		} 
		else if (m_step == 4) 
		{
			// copies deltaS into inscatter texture S (line 5 in algorithm 4.1)
			m_copyInscatter1.SetTexture(0, "deltaSRRead", rt_deltaSRT);
			m_copyInscatter1.SetTexture(0, "deltaSMRead", rt_deltaSMT);
			m_copyInscatter1.SetTexture(0, "inscatterWrite", rt_inscatterT[WRITE]);
			
			//The inscatter calc's can be quite demanding for some cards so process 
			//the calc's in layers instead of the whole 3D data set.
			for(int i = 0; i < RES_R; i++) {
				m_copyInscatter1.SetInt("layer", i);
				m_copyInscatter1.Dispatch(0, (RES_MU_S*RES_NU)/NUM_THREADS, RES_MU/NUM_THREADS, 1);
			}
			
			RTUtility.Swap(rt_inscatterT);
			 
		} 
		else if (m_step == 5) 
		{
			// computes deltaJ (line 7 in algorithm 4.1)
			m_inscatterS.SetInt("first", (m_order == 2) ? 1 : 0);
			m_inscatterS.SetTexture(0, "transmittanceRead", rt_transmittanceT);
			m_inscatterS.SetTexture(0, "deltaERead", rt_deltaET);
			m_inscatterS.SetTexture(0, "deltaSRRead", rt_deltaSRT);
			m_inscatterS.SetTexture(0, "deltaSMRead", rt_deltaSMT);
			m_inscatterS.SetTexture(0, "deltaJWrite", rt_deltaJT);
			
			//The inscatter calc's can be quite demanding for some cards so process 
			//the calc's in layers instead of the whole 3D data set.
			for(int i = 0; i < RES_R; i++) {
				m_inscatterS.SetInt("layer", i);
				m_inscatterS.Dispatch(0, (RES_MU_S*RES_NU)/NUM_THREADS, RES_MU/NUM_THREADS, 1);
			}
			 
		} 
		else if (m_step == 6) 
		{
			// computes deltaE (line 8 in algorithm 4.1)
			m_irradianceN.SetInt("first", (m_order == 2) ? 1 : 0);
			m_irradianceN.SetTexture(0, "deltaSRRead", rt_deltaSRT);
			m_irradianceN.SetTexture(0, "deltaSMRead", rt_deltaSMT);
			m_irradianceN.SetTexture(0, "deltaEWrite", rt_deltaET);
			m_irradianceN.Dispatch(0, SKY_W/NUM_THREADS, SKY_H/NUM_THREADS, 1);
			 
		} 
		else if (m_step == 7) 
		{
			// computes deltaS (line 9 in algorithm 4.1)
			m_inscatterN.SetTexture(0, "transmittanceRead", rt_transmittanceT);
			m_inscatterN.SetTexture(0, "deltaJRead", rt_deltaJT);
			m_inscatterN.SetTexture(0, "deltaSRWrite", rt_deltaSRT);
			
			//The inscatter calc's can be quite demanding for some cards so process 
			//the calc's in layers instead of the whole 3D data set.
			for(int i = 0; i < RES_R; i++) {
				m_inscatterN.SetInt("layer", i);
				m_inscatterN.Dispatch(0, (RES_MU_S*RES_NU)/NUM_THREADS, RES_MU/NUM_THREADS, 1);
			}
			 
		} 
		else if (m_step == 8) 
		{
			// adds deltaE into irradiance texture E (line 10 in algorithm 4.1)
			m_copyIrradiance.SetFloat("k", 1.0f);
			m_copyIrradiance.SetTexture(0, "deltaERead", rt_deltaET);
			m_copyIrradiance.SetTexture(0, "irradianceRead", rt_irradianceT[READ]);
			m_copyIrradiance.SetTexture(0, "irradianceWrite", rt_irradianceT[WRITE]);
			m_copyIrradiance.Dispatch(0, SKY_W/NUM_THREADS, SKY_H/NUM_THREADS, 1);
			
			RTUtility.Swap(rt_irradianceT);
			 
		} 
		else if (m_step == 9) 
		{
			
			// adds deltaS into inscatter texture S (line 11 in algorithm 4.1)
			m_copyInscatterN.SetTexture(0, "deltaSRead", rt_deltaSRT);
			m_copyInscatterN.SetTexture(0, "inscatterRead", rt_inscatterT[READ]);
			m_copyInscatterN.SetTexture(0, "inscatterWrite", rt_inscatterT[WRITE]);
			
			//The inscatter calc's can be quite demanding for some cards so process 
			//the calc's in layers instead of the whole 3D data set.
			for(int i = 0; i < RES_R; i++) {
				m_copyInscatterN.SetInt("layer", i);
				m_copyInscatterN.Dispatch(0, (RES_MU_S*RES_NU)/NUM_THREADS, RES_MU/NUM_THREADS, 1);
			}
			
			RTUtility.Swap(rt_inscatterT);
			
			if (m_order < 4) {
				m_step = 4;
				m_order += 1;
			}
			 
		}
		else if (m_step == 10){

			ReleaseRT();
			m_finished = true;
		}
		m_step += 1;
	}

	void ReleaseRT(bool Full = false)
	{
		RenderTexture.active = null;

		if (rt_deltaET != null)
			RenderTexture.ReleaseTemporary(rt_deltaET);
		if (rt_deltaSRT != null)
			RenderTexture.ReleaseTemporary(rt_deltaSRT);
		if (rt_deltaSMT != null)
			RenderTexture.ReleaseTemporary(rt_deltaSMT);
		if (rt_deltaJT != null)
			RenderTexture.ReleaseTemporary(rt_deltaJT);
		if (rt_inscatterT != null)
			RenderTexture.ReleaseTemporary(rt_inscatterT[WRITE]);	
		if (rt_irradianceT != null)
			RenderTexture.ReleaseTemporary(rt_irradianceT[WRITE]);
		if (Full)
		{
			if (rt_transmittanceT != null)
				RenderTexture.ReleaseTemporary(rt_transmittanceT);
			if (rt_inscatterT != null)
				RenderTexture.ReleaseTemporary(rt_inscatterT[READ]);	
			if (rt_irradianceT != null)
				RenderTexture.ReleaseTemporary(rt_irradianceT[READ]);
		}

		/*if (rt_deltaET != null)
			rt_deltaET.Release();
		if (rt_deltaSRT != null)
			rt_deltaSRT.Release();
		if (rt_deltaSMT != null)
			rt_deltaSMT.Release();
		if (rt_deltaJT != null)
			rt_deltaJT.Release();
		if (rt_inscatterT != null)
			rt_inscatterT[WRITE].Release();	
		if (rt_irradianceT != null)
			rt_irradianceT[WRITE].Release();
		if (Full)
		{
			if (rt_transmittanceT != null)
				rt_transmittanceT.Release();
			if (rt_inscatterT != null)
				rt_inscatterT[READ].Release();	
			if (rt_irradianceT != null)
				rt_irradianceT[READ].Release();
		}
		*/
	}

	void Update()
	{
		Quaternion newSunRotation = sunLight.transform.rotation;
		if (newSunRotation != savedSunRotation)
		{
			savedSunRotation = newSunRotation;

			SetUniforms(skybox_material);
		}
	}

	void OnRenderImage(RenderTexture src, RenderTexture dst)
	{
		SetUniforms(inscatter_material);

		inscatter_material.SetFloat("_MinDistance", _MinFogDistance);

		inscatter_material.SetMatrix("_Globals_CameraToWorld", GetComponent<Camera>().cameraToWorldMatrix);
		
		inscatter_material.SetFloat("TanHalfFOV", Mathf.Tan(
			GetComponent<Camera>().fieldOfView * 0.5f * Mathf.Deg2Rad));
		inscatter_material.SetFloat("ViewAspect", GetComponent<Camera>().aspect);

		Graphics.Blit(src, dst, inscatter_material);
	}

	public void SetUniforms(Material mat)
	{	
		if(mat == null) return;

		Transform sun = sunLight.transform;
		mat.SetVector("_Sun_WorldSunDir", sun.forward );
		mat.SetVector("_Globals_WorldCameraPos", transform.position );

		mat.SetFloat("_Sun_Intensity", 70.0f*sunLight.intensity);
		mat.SetColor("_Sun_Color", sunLight.color);

		mat.SetVector("_Globals_Origin", origin );
		mat.SetFloat("_Exposure", _Exposure);

	}
	
	public void InitUniforms(Material mat)
	{		

		if(mat == null) return;

		mat.SetVector("betaR", m_betaR / 1000.0f);
		mat.SetFloat("mieG", Mathf.Clamp(m_mieG, 0.0f, 0.99f));
		mat.SetTexture("_Sky_Transmittance", rt_transmittanceT);
		mat.SetTexture("_Sky_Inscatter", rt_inscatterT[READ]);
		mat.SetTexture("_Sky_Irradiance", rt_irradianceT[READ]);

		mat.SetFloat("scale",Rg /  earthRadius);
		mat.SetFloat("Rg", Rg);
		mat.SetFloat("Rt", Rt);
		mat.SetFloat("RL", RL);
		mat.SetFloat("TRANSMITTANCE_W", TRANSMITTANCE_W);
		mat.SetFloat("TRANSMITTANCE_H", TRANSMITTANCE_H);
		mat.SetFloat("SKY_W", SKY_W);
		mat.SetFloat("SKY_H", SKY_H);
		mat.SetFloat("RES_R", RES_R);
		mat.SetFloat("RES_MU", RES_MU);
		mat.SetFloat("RES_MU_S", RES_MU_S);
		mat.SetFloat("RES_NU", RES_NU);
		mat.SetFloat("AVERAGE_GROUND_REFLECTANCE", AVERAGE_GROUND_REFLECTANCE);
		mat.SetFloat("HR", HR * 1000.0f);
		mat.SetFloat("HM", HM * 1000.0f);
		mat.SetVector("betaMSca", BETA_MSca / 1000.0f);
		mat.SetVector("betaMEx", (BETA_MSca / 1000.0f) / 0.9f);
	}

	void OnDestroy()
	{
		ReleaseRT(true);
	}

	//void OnGUI()
	//{
	//	CP = GUILayout.Toggle(CP,"ControlPanel");
	//	if (CP)
	//		guiRect = GUI.Window(1, guiRect, GUIControlPanel, "ControlPanel");
	//}

	//void GUIControlPanel(int windowID)
	//{
	//	GUILayout.Label("Scatter coefficient for rayleigh:");
	//	GUILayout.BeginHorizontal();
	//	//GUILayout.Label("BetaR:");
	//	m_betaR.x = float.Parse(GUILayout.TextField(m_betaR.x.ToString()) ); 
	//	m_betaR.y = float.Parse(GUILayout.TextField(m_betaR.y.ToString()) ); 
	//	m_betaR.z = float.Parse(GUILayout.TextField(m_betaR.z.ToString()) ); 
	//	GUILayout.EndHorizontal();

	//	GUILayout.Label("Half heights for the atmosphere air density (HR) and particle density (HM) in km");

	//	GUILayout.BeginHorizontal();
	//	GUILayout.Label("HR:");
	//	HR = float.Parse(GUILayout.TextField(HR.ToString()) ); 
	//	GUILayout.EndHorizontal();
		
	//	GUILayout.BeginHorizontal();
	//	GUILayout.Label("HM:");
	//	HM = float.Parse(GUILayout.TextField(HM.ToString()) ); 
	//	GUILayout.EndHorizontal();

	//	GUILayout.Label("Scatter coefficient for mie:");
	//	GUILayout.BeginHorizontal();
	//	//GUILayout.Label("BETA_MSca:");
	//	BETA_MSca.x = float.Parse(GUILayout.TextField(BETA_MSca.x.ToString()) ); 
	//	BETA_MSca.y = float.Parse(GUILayout.TextField(BETA_MSca.y.ToString()) ); 
	//	BETA_MSca.z = float.Parse(GUILayout.TextField(BETA_MSca.z.ToString()) ); 
	//	GUILayout.EndHorizontal();

	//	GUILayout.Label("Asymmetry factor for the mie phase function:");
	//	GUILayout.BeginHorizontal();
	//	GUILayout.Label("MieG:");
	//	m_mieG = float.Parse(GUILayout.TextField(m_mieG.ToString()) ); 
	//	GUILayout.EndHorizontal();

	//	GUILayout.Label("preprocessTime:"+preprocessTime*1000.0f+"ms");
	//	if (GUILayout.Button("Re-calculate"))
	//	{
	//		StartPreprocess();
	//	}

	//	GUILayout.BeginHorizontal();
	//	GUILayout.Label("MinInscatterDistance:");
	//	_MinFogDistance = float.Parse(GUILayout.TextField(_MinFogDistance.ToString()) ); 
	//	GUILayout.EndHorizontal();

	//	//GUILayout.BeginHorizontal();
	//	GUILayout.Label("Height:");
	//	Vector3 pos = Camera.main.transform.position;
	//	pos.y = GUILayout.HorizontalSlider(Camera.main.transform.position.y , 0, 500000.0f, GUILayout.MinWidth(Screen.width));
	//	Camera.main.transform.position = pos;
	//	//GUILayout.EndHorizontal();

	//	GUILayout.Label("SUN rotation:");
	//	//Vector3 erot = sunLight.transform.rotation.eulerAngles;
	//	sunRot.x = GUILayout.HorizontalSlider( sunRot.x , 0, 360.0f);
	//	sunRot.y = GUILayout.HorizontalSlider( sunRot.y , 0, 360.0f);
	//	//erot.z = GUILayout.HorizontalSlider( erot.z , 0, 360.0f);
	//	sunLight.transform.rotation = Quaternion.Euler(sunRot);

	//	if (GUILayout.Button("Exit"))
	//		Application.Quit();

	//	GUI.DragWindow(new Rect(0, 0, 10000, 10000));
	//}
}

