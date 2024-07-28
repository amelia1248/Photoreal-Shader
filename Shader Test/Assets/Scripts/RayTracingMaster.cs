using UnityEngine;

public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader RayTracingShader;  // Create Shader object in Unity
    private RenderTexture _target;  // Allows Shader to be assigned to GameObjects? A texture that can be rendered to.
    private Camera _camera; // Generate some camera rays
    public Texture SkyboxTexture;
    private uint _currentSample = 0;
    private Material _addMaterial;


    private void Awake(){
        _camera = GetComponent<Camera>();
    }

    private void SetShaderParameters(){
        // In our shader, we define matrices, Ray structure, and a function 
        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);    // Setting Skybox Texture
        RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
    }
    
    /// <summary>
    /// Initializing the RenderTexture that we target.
    /// </summary>
    private void InitRenderTexture(){
        // If the _target attribute is not equal to either dimension of the current screen in Unity
        // Using Unity scripting API documentation to access target's attributes
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height){
            if (_target != null){
                _target.Release();  // If _target exists, we release without being collected for garbage
            }
            // Initialize new target for ray tracing
            // But first, reset currentSamples
            _currentSample = 0;  // Note: fix?
            _target = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;   // Enables random access write into random texture
            _target.Create();   // Actually creates the RenderTexture
        }
    }
    

    // Update function detects camera transform changes
    private void Update()
    {
        if (transform.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
        }
    }



    /// <summary>
    /// Write to Unity's raw interface, executes Compute Shader to the screen
    /// </summary>
    /// <param name="destination"></param>
    private void Render(RenderTexture destination){
        // Ensure current target for rendering
        InitRenderTexture();

        RayTracingShader.SetTexture(0, "Result", _target);   // Sets a texture parameter for our ComputeShader
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);  // Executes our Compute Shader to the screen

        // Added code for PROGRESSIVE SAMPLING
        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        _addMaterial.SetFloat("_Sample", _currentSample);
        Graphics.Blit(_target, destination, _addMaterial);    // Raw interface to Unity's drawing function... blit copipes pixel data from a texture to a render texture
        _currentSample++;
    }


    /// <summary>
    /// A method that determines which texture our Shader is applied to.
    /// </summary>
    /// <param name="source"></param> Texture the shader comes from.
    /// <param name="destination"></param> Texture the shader affects. 
    private void OnRenderImage(RenderTexture source, RenderTexture destination){
        SetShaderParameters();
        Render(destination);
    }

}
