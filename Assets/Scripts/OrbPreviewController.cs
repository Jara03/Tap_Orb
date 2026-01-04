using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class OrbPreviewController : MonoBehaviour
{
    [Header("Scene refs")]
    [SerializeField] private Camera previewCamera;
    [SerializeField] private Transform previewRoot;
    [SerializeField] private RawImage previewImage; // optionnel si tu veux set la texture via code

    [Header("Preview settings")]
    [SerializeField] private string previewLayerName = "OrbPreview";
    [SerializeField] private float rotateSpeedDegPerSec = 18f;
    [SerializeField] private float padding = 1.25f; // > 1 = un peu d'air autour
    [SerializeField] private bool renderEveryFrame = true;
    [SerializeField] private float renderInterval = 1f / 30f; // si renderEveryFrame = false

    private GameObject currentInstance;
    private int previewLayer;
    private Coroutine renderLoop;

    private bool isInitFrame = true;

    private void Awake()
    {
        previewLayer = LayerMask.NameToLayer(previewLayerName);
        if (previewLayer < 0)
            Debug.LogError($"Layer '{previewLayerName}' introuvable. Crée-le dans Unity (Project Settings > Tags and Layers).");

        // Option perf : tu peux désactiver la caméra et rendre manuellement
        if (!renderEveryFrame && previewCamera != null)
            previewCamera.enabled = false;
        
        previewCamera.nearClipPlane = 0.1f;//Mathf.Max(0.1f, dist - radius * 3f);
        previewCamera.farClipPlane = 0.5f;// Mathf.Max(0.5f, dist - radius * 3f);
    }

    private void OnEnable()
    {
        if (!renderEveryFrame && previewCamera != null)
            renderLoop = StartCoroutine(RenderLoop());
    }

    private void OnDisable()
    {
        if (renderLoop != null) StopCoroutine(renderLoop);
        renderLoop = null;
    }

    /// <summary>
    /// Appelle cette méthode quand l'utilisateur change de modèle d'Orb.
    /// </summary>
    public void ShowOrbPrefab(GameObject orbPrefab, Color color, float size)
    {
        Clear();

        if (orbPrefab == null)
            return;

        currentInstance = Instantiate(orbPrefab, previewRoot);
        var rb = currentInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;      // stop la simulation
            rb.detectCollisions = false;
        }
        currentInstance.transform.localPosition = Vector3.zero;
        currentInstance.transform.localRotation = Quaternion.identity;
        //currentInstance.transform.localScale = Vector3.one;
        currentInstance.transform.localScale = Vector3.one * Mathf.Max(0.5f, size);

        ApplyColor(currentInstance, color);


        SetLayerRecursively(currentInstance, previewLayer);

        // Optionnel : désactiver scripts “gameplay” si besoin (pour éviter qu’ils s’exécutent dans le preview)
        // DisableNonVisualComponents(currentInstance);

        // Rotation douce
        var rot = currentInstance.AddComponent<SlowSpin>();
        rot.Speed = rotateSpeedDegPerSec;

        FrameToObject(currentInstance);

        // Rendu immédiat si caméra désactivée
        if (!renderEveryFrame && previewCamera != null)
            previewCamera.Render();
    }

    private void FrameToObject(GameObject go)
    {
        if (previewCamera == null) return;

        var bounds = CalculateBounds(go);
        if (bounds.size == Vector3.zero)
            bounds = new Bounds(go.transform.position, Vector3.one * 0.5f);

        Vector3 center = bounds.center;
        float radius = bounds.extents.magnitude;
        if (radius < 0.001f) radius = 0.5f;

        float fovRad = previewCamera.fieldOfView * Mathf.Deg2Rad;
        float dist = (radius / Mathf.Tan(fovRad * 0.5f)) * padding;

        // Place la caméra sur une diagonale "jolie"
        Vector3 dir = (new Vector3(1f, 0.75f, -1f)).normalized;

        if (isInitFrame)
        {
            previewCamera.transform.position = center + dir * dist;
            isInitFrame = false;   
        }
        else
        {
            previewCamera.transform.position = previewCamera.transform.position;
        }
        previewCamera.transform.LookAt(center);
        
    }

    private Bounds CalculateBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        return b;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private IEnumerator RenderLoop()
    {
        var wait = new WaitForSeconds(renderInterval);
        while (true)
        {
            if (previewCamera != null)
                previewCamera.Render();
            yield return wait;
        }
    }
    
    public void ShowMesh(Mesh mesh, Color color, float size)
    {
        Clear();
        if (mesh == null) return;

        // Crée un GO simple avec MeshFilter/Renderer
        currentInstance = new GameObject("OrbPreview_Mesh");
        currentInstance.transform.SetParent(previewRoot, false);
        currentInstance.transform.localPosition = Vector3.zero;
        currentInstance.transform.localRotation = Quaternion.identity;
        currentInstance.transform.localScale = Vector3.one * Mathf.Max(0.01f, size);

        var mf = currentInstance.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = currentInstance.AddComponent<MeshRenderer>();
        if (PreviewMaterial != null) mr.sharedMaterial = PreviewMaterial;

        SetLayerRecursively(currentInstance, previewLayer);

        // Spin
        var rot = currentInstance.AddComponent<SlowSpin>();
        rot.Speed = rotateSpeedDegPerSec;

        // Color via MaterialPropertyBlock (safe perf)
        ApplyColor(currentInstance, color);

        FrameToObject(currentInstance);

        if (!renderEveryFrame && previewCamera != null)
            previewCamera.Render();
    }
    
    

    public void Clear()
    {
        if (currentInstance != null) Destroy(currentInstance);
        currentInstance = null;
    }

    
    private static readonly int BaseColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int ColorId     = Shader.PropertyToID("_Color");

    private void ApplyColor(GameObject root, Color color)
    {
        var r = root.GetComponent<Renderer>();
        if (r == null) return;

        var mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColorId, color);
        mpb.SetColor(ColorId, color);
        r.SetPropertyBlock(mpb);
    }
    
    [SerializeField] public Material PreviewMaterial;

    private MeshFilter mf;
    private MeshRenderer mr;

    private MaterialPropertyBlock mpb;

    public void SetPreviewColor(Color color)
    {
        if (mr == null) return;

        mr.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColorId, color);
        mpb.SetColor(ColorId, color);
        mr.SetPropertyBlock(mpb);

        if (!renderEveryFrame && previewCamera != null)
            previewCamera.Render();
    }
    public void SetPreviewSize(float size)
    {
        if (currentInstance == null) return;

        currentInstance.transform.localScale = Vector3.one * Mathf.Max(0.5f, size);

        // important : sinon la caméra peut clip / cadrage faux
        FrameToObject(currentInstance);

        if (!renderEveryFrame && previewCamera != null)
            previewCamera.Render();
    }


    // Si tu en as besoin plus tard :
    // private void DisableNonVisualComponents(GameObject root) { ... }
}

public class SlowSpin : MonoBehaviour
{
    public float Speed = 18f;
    private void Update()
    {
        transform.Rotate(Vector3.up, Speed * Time.deltaTime, Space.World);
    }
}
